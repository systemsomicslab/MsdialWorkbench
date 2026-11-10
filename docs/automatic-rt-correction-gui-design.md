# Automatic alignment RT correction in the MS-DIAL 5 GUI

Status: implemented on `feat/gui-automatic-rt-correction`.
Findings it closes: CONF-2, CONF-17, CONF-18, CONF-55, CONF-56 of the final review of the
automatic alignment RT correction (MsdialWorkbench #810, #826, #845, #876).

## 1. Goal

The automatic alignment RT correction fits, per analysis file, a monotonic map from that
file's measured RT axis (the *original* axis) onto the RT axis of one reference file (the
*corrected* or *aligned* axis). It is used for alignment only. Until now only the Console
built it. The GUI re-aligned without it while the project still said it ran (CONF-2), and the
GUI's curation windows mixed the two axes (CONF-17, CONF-18).

The GUI now applies the correction exactly as the Console does, through the same library
code, and every place that shows or edits a value says, and respects, the axis that value is
on.

## 2. Axis contract

| Data | Field | Axis |
|---|---|---|
| Per-file peak list (`.pai2`, `.mdpeak`, `ChromatogramPeakFeature`) | `ChromXsTop/Left/Right` | original (measured) |
| Per-file annotation (`MsScanMatchResult`) | `RtSimilarity`, `IsRtMatch`, the confidence code | original, of the file that was annotated |
| Raw scan pointers anywhere (`MS1RawSpectrumIdTop`, `MS2RawSpectrumID` ...) | scan index | original (they index the raw file) |
| Aligned peak (`AlignmentChromPeakFeature`, one per file per spot) | `ChromXsTop/Left/Right` | corrected |
| Gap-filled aligned peak | `ChromXsTop/Left/Right` | corrected (filled on the original axis, then corrected) |
| Spot (`AlignmentSpotProperty`) | `TimesCenter` (mean of the aligned tops) | corrected |
| Spot annotation copied from the representative peak | RT match evidence | original axis of the representative file |
| Serialized spot EIC (`.EIC.aef`, `ChromatogramPeakInfo`) | chromatogram points and `ChromXsTop/Left/Right` | original (`LcmsDataAccessor.AccumulateChromatogram` restores them) |
| Correction map (new, `<alignment>.rtcorrection.tsv`) | `Original RT`, `Corrected RT` per control point | both, by name |
| Audit TSVs (`automatic_alignment_rt_correction_*.tsv`) | `Original RT`, `Reference RT` | both, by name (unchanged) |
| Text exports `.mdalign`, matrices | `Average Rt(min)`, per-file `RT` | corrected |
| mzTab-M | `retention_time_in_seconds` (SMF), RT columns | corrected |
| `.mdprovenance.tsv` | `rt_min` | corrected; new appended `rt_original_min` is original |

A file the correction left uncorrected (too few anchors, a Blank with interpolation off) has
no model: both axes are the same for it. The reference file's model passes through its own
anchors and is the identity there.

Every model the correction builds is strictly increasing
(`PiecewiseLinearAlignmentRetentionTimeCorrectionModel` refuses non-monotonic control
points; a `BlendedAlignmentRetentionTimeCorrectionModel` is a convex mix of two increasing
maps), so `Correct` and `Restore` are inverse bijections. The map file reader rebuilds the
models through the same constructors, so a map that is not monotonic is refused on load.

## 3. Shared correction building (Console and GUI)

New: `MsdialLcMsApi/Algorithm/Alignment/AutomaticAlignmentRtCorrectionWorkflow.cs`.
The Console's code is moved here with its logic unchanged; `LcmsProcess` calls it.

| Was in `LcmsProcess` | Now |
|---|---|
| "user-defined and automatic RT correction cannot both run" check | `AutomaticAlignmentRtCorrectionWorkflow.ExclusiveWithLegacyRtCorrection` |
| `ValidateParameter`, anchor library `Load` + `Check`, `ReportAnchorLibrary`, the "library set but not used" warning | `PrepareInputs` (returns the library rows and the library's name and SHA-256) |
| `ReadRawFile` | `ReadRawFile` |
| `BuildAutomaticRtCorrection` (remove the earlier audit, `Build`, `WriteAudit`, the partial audit on failure) | `Build` |
| `ReportAutomaticRtCorrection` | `Report` |

Messages go through `IAutomaticRtCorrectionReporter` (`Info`, `Error`). The Console's reporter
writes to stdout and stderr exactly as before; the GUI's collects the lines and shows them.
The Console-only checks (`Together with alignment`, the preview-only switch, the scratch
folder of the preview) stay in `LcmsProcess`.

`AutomaticAlignmentRetentionTimeCorrection.cs` is not changed (another branch is editing its
internals). `AlignmentRetentionTimeCorrection.cs` gains read-only accessors on
`BlendedAlignmentRetentionTimeCorrectionModel` (`Left`, `Right`, `RightWeight`) so the map
can be written.

### Persisted correction map

`AlignmentRetentionTimeCorrectionMapFile` writes and reads
`<directory of the alignment result>/<alignment file name>.rtcorrection.tsv`:

```
# MS-DIAL automatic alignment RT correction map
# Format version	1
# Reference file ID	2
# Reference file name	sample_C1
File ID	File name	Model	Node	Parent node	Right weight	Original RT (min)	Corrected RT (min)	Anchor ID	m/z	Quality score
```

One `Piecewise` row per control point, a `Blended` row per blend (with its two child nodes),
nothing for an uncorrected file. Doubles are written with `G17`, so a reloaded model returns
the same `Correct` and `Restore` values bit for bit. The Console writes it beside the
alignment result whenever it saves one with the correction (not in light mode, which saves
no alignment objects). The GUI writes it after every corrected alignment and deletes a stale
one before an uncorrected alignment into the same name.

## 4. GUI alignment

`LcmsMethodModel.ProcessAlignment`:

1. If `Execute automatic RT correction` is on: the exclusivity check and `PrepareInputs`
   (strict library) run before anything is read. A refusal is shown and nothing is aligned.
2. In the progress task: `Build` with peaks from each file's `.pai2` and raw readings from the
   GUI's data provider (`StandardDataProviderFactory`, the same reader the Console uses),
   `run = "Run"`. The audit goes to `<project folder>/<alignment file name>_automatic_rt_correction/`
   (the Console's three file names, so the readers that read the Console's audit read it).
3. `AlignmentReferenceFileID` is set to the correction's reference, as the Console does.
4. `LcmsAlignmentProcessFactory.AlignmentRtCorrection` = the correction.
5. The map is written beside the alignment result.
6. The report lines (reference, uncorrected files, unused anchors, exclusions that matched
   nothing) are shown once processing ends, as a warning when any line is a warning.

A failed correction stops the alignment and says why; the partial audit is written as the
Console writes it. The `_param_` file the GUI saves after processing now describes what ran.

## 5. GUI settings

A new section "Automatic alignment RT correction" in the alignment settings page, shown for
LC-MS projects only (`AutomaticRtCorrectionSettingModel` / `...ViewModel`):

- Execute; anchor mode (Automatic, UserOnly, Hybrid); anchor library (text box and Browse...);
  user anchor peak selection mode (HighestIntensity, ClosestToReferenceRt, Weighted) and RT
  weight; reference file (Automatic, or one file); local support window; minimum anchors.
- Advanced (expander): RT bin width, match RT tolerance, maximum anchors, minimum sample
  coverage, intensity quantile, maximum peak width quantile, minimum S/N, minimum Gaussian
  similarity, minimum ideal slope, outlier MAD threshold, reference centrality weight, Blank
  interpolation by analytical order.

Validation on commit uses the library code the Console uses: `ValidateParameter`, the strict
seven-column `AutomaticRtCorrectionAnchorLibrary.Load` + `Check` for UserOnly and Hybrid, and
the exclusivity with the user-defined RT correction. Values are loaded from the project's
parameter, so a Console-made project shows its own settings; nothing is written back unless
the person runs alignment.

No preview-only switch. The Console preview exists to review anchors before a long run
writes per-file files beside the raw data. In the GUI the peaks are already picked when
alignment runs, re-alignment is cheap, every alignment writes its own audit folder, and the
review and approval of anchors lives in MS-DIAL Interactive.

## 6. Curation windows

New: `MsdialLcMsApi/Algorithm/Alignment/AlignedPeakRtAxisMap.cs` and `AlignmentRtAxis.cs`.

`AlignmentRtAxis` is loaded once per alignment result:

- the map file exists: exact per-file maps (`Source = CorrectionMap`; an uncorrected file is
  `Uncorrected`, identity);
- no map file and the project says the correction ran (a Console project made before this
  change, or a GUI alignment that silently skipped it): per peak, a local map through the
  serialized EIC's original-axis bounds and the aligned peak's bounds
  (`Source = StoredPeakBounds`). This is exact at the peak's own left, top and right and
  linear between. It is refused (`Source = Unavailable`) for an aligned peak already edited
  by hand, whose bounds no longer correspond to the stored ones;
- otherwise: `NotCorrected`, identity, and every window runs its old code path.

`AlignedChromatograms` carries the spot's maps (`AlignedSpotRtAxes`), built by
`AlignmentEicLoader` from the EIC it already reads.

| Window | Shows | Integrates on | Writes back |
|---|---|---|---|
| Alignment EIC chart | original-axis EICs, area from `ToOriginal(aligned left/right)` with the map, else the stored bounds | - | - |
| Overlay EIC (`AlignedPeakCorrectionWinLegacy`) | "Original" and "Picking" panels: EIC with markers at `ToOriginal(aligned bounds)`; "Aligned" panel: each EIC shifted by `ToOriginal(aligned top) - TimesCenter`, so its top sits at the spot RT | the shifted EIC, then shifted back to the original axis | `ChromXs* = ToAligned(original value)`, heights and areas as integrated; `TimesCenter` = mean of the aligned tops |
| Sample table thumbnails and manual peak pick | EIC with markers at `ToOriginal(aligned bounds)` | the EIC (original) | `ChromXs* = ToAligned(...)`; the not-detected branch keeps `TimesCenter` (aligned) |

"Update" without an edit changes nothing: the overlay window already writes nothing until a
range is chosen; the manual peak pick now remembers whether the person changed the peak,
and when the axis is corrected an unchanged dialog leaves height, area, S/N, the bounds and
the manual flag as they were. For a project without the correction the dialog keeps its old
behaviour (it re-integrates); see open question 1.

A file whose map is `Unavailable` is drawn at its stored bounds and is never written: the
window says why.

Axis labels: when the axis is corrected the panels say "Original RT" or "Aligned RT", the
alignment spot plot says "Aligned retention time (reference file axis)", and the alignment
EIC keeps its existing "Original retention time ... (before alignment correction)".

## 7. Export labelling (CONF-55, CONF-56)

Columns are appended, never renamed, and nothing changes for a run without the correction.

- `.mdalign`, the matrix exports (Console and GUI): the first, otherwise empty, cell of the
  first header line holds the note "RT axis: aligned RTs are on the RT axis of reference
  file X (automatic alignment RT correction); peak lists and RT-match evidence are on each
  file's measured RT axis". Readers that find the `Class` cell (Interactive's
  `describe_export`) or skip the four header lines are unaffected.
- mzTab-M: one more `MTD custom[n]` parameter, `[,, MS-DIAL retention time axis, ...]`, with
  the same text (no commas). `custom[n]` is the slot mzTab-M defines for this.
- `.mdprovenance.tsv`: an appended `rt_original_min` column, `Restore(file, rt_min)`, so a
  row's RT can be checked against its raw scan ID.

## 8. Risks and how they are covered

| Risk | Cover |
|---|---|
| The refactor changes Console output | Before/after Console runs (Hybrid and UserOnly) on real data compared byte for byte, excluding only the intended label lines |
| GUI and Console differ in correction | The GUI calls the same `Build` with the same inputs; a programmatic run on a Console project reproduces its audit and map |
| A reloaded map differs from the computed one | `G17` doubles and the same model constructors; unit test compares `Correct`/`Restore` over a grid |
| Curation writes the wrong axis | Unit tests of the conversions; GUI-model tests of overlay and manual-pick write-back and of Update without edit |
| Projects without the correction change | Every new path is gated on `AlignmentRtAxis.IsCorrected`; existing tests unchanged |
| Merge with `fix/rt-correction-review-followups` | That branch edits `AutomaticAlignmentRetentionTimeCorrection.cs` internals and `LcmsProcess.cs`; this one does not touch the former and moves code out of the latter, so a conflict in `LcmsProcess.cs` (stale `.rtc` handling, the batch summary column) is expected and mechanical |
| Interactive restores a project without the new map file | The fallback local map keeps the windows correct for unedited peaks; Interactive should keep `*.rtcorrection.tsv` (follow-up there) |

## 9. Open questions for the author

1. Should "Update" without an edit leave quantities alone in every project, not only in
   corrected ones? Today it re-integrates with the legacy integrator and marks the peak as
   manually modified.
2. Should the GUI let the person keep their own alignment reference file while the
   automatic correction runs? The Console replaces it with the correction's reference; the
   GUI now does the same.

## 10. Expected merge conflict with `fix/rt-correction-review-followups`

That branch (commit ee64ba200) changes `LcmsProcess.cs` in three places this branch also
touches:

- the top of `Run` (it refuses unreadable automatic RT keys just before the exclusivity check,
  which here calls `AutomaticAlignmentRtCorrectionWorkflow.ExclusiveWithLegacyRtCorrection`);
- `ReportAutomaticRtCorrection`, where it prints `result.Warnings`: that method now lives in
  `AutomaticAlignmentRtCorrectionWorkflow.Report`, and the loop belongs there so that the GUI
  shows the warnings too;
- `ExecuteAsync`, where it says which file replaced the alignment reference: this branch adds
  the RT axis note on the next lines. `RunForAlignment` should say the same line for the GUI.

`AutomaticAlignmentRetentionTimeCorrection.cs` is not touched here, so its new summary column
("Analysis batch") reaches the GUI's audit through the shared `WriteAudit` unchanged.
