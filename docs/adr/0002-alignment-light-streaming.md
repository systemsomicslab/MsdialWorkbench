# ADR 0002: Alignment-light streaming export path

## Status

Accepted for incremental implementation. Phase 1 is implemented for LC-MS
Console.

## Context

Large-scale LC-MS projects with thousands to tens of thousands of samples can
run out of memory during alignment. The current alignment path keeps a large
object graph in memory:

- one `AlignmentSpotProperty` per aligned feature,
- each `AlignmentSpotProperty.AlignedPeakProperties` contains one
  `AlignmentChromPeakFeature` per analysis file,
- additional GUI chromatogram cache data may be serialized for later project
  browsing.

The first alignment-light change disables GUI chromatogram serialization when
the console method file contains:

```txt
Alignment light mode: True
```

Validation on the FastLC WIFF demo confirmed that `mdalign`, `mdmsp`, and
`mzTab` text outputs are unchanged except for the expected mzTab ID line, while
the GUI alignment object serialization path is skipped. Alignment `.dcl`
spectra are still written because they represent the aligned MSDecResult set
used by spectrum-aware text exports and downstream spectrum reuse.

The next target is to avoid holding all per-file aligned peak objects in memory
when only text exports are required.

## Current Dependency Map

### Join and gap filling

`LcmsPeakJoiner` currently allocates the full per-file peak list during spot
initialization:

- `LcmsPeakJoiner.InitSpots`
- `AlignmentSpotProperty.AlignedPeakProperties`
- `AlignmentChromPeakFeature` dummy rows for every file

`PeakAligner.CollectPeakSpots` then visits files one by one and mutates the
matching file entry for each spot:

- `Filler.NeedsGapFill`
- `Filler.GapFill`
- `DataObjConverter.SetRepresentativeFileID`
- `DataObjConverter.SetRepresentativeProperty`

The gap filler needs, for each spot:

- detected peaks across files to estimate center and peak width,
- the current file entry to update missing peak values,
- estimated noise from detected peaks.

### Filtering and refining

Filtering uses per-file values:

- `PeakCountFilter`: count of detected peaks.
- `QcFilter`: QC file detection.
- `DetectedNumberFilter`: per-class detection count.
- `BlankFilter`: blank/sample peak-height summaries.

`LcmsAlignmentRefiner` uses per-file values for:

- representative peak selection,
- fold-change and ANOVA,
- blank filtering,
- adduct/isotope/representative peak links,
- ion abundance correlation links.

The ion abundance correlation step is especially expensive because it compares
peak-height vectors between RT-neighboring spots.

### Text export

`AlignmentCSVExporter` and `MztabFormatExporter` mostly consume per-file
values through `IQuantValueAccessor`.

For LC-MS console defaults, the required per-file values are:

| Export need | Source field |
| --- | --- |
| Height matrix | `PeakHeightTop` |
| Area matrix | `PeakAreaAboveZero` |
| Normalized height | `NormalizedPeakHeight` |
| Normalized area | `NormalizedPeakAreaAboveZero` |
| Peak ID matrix | `MasterPeakID` |
| RT matrix | `ChromXsTop.RT.Value` |
| Mass matrix | `Mass` |
| S/N matrix | `PeakShape.SignalToNoise` |
| MS/MS presence | `MS2RawSpectrumID` or `MS2RawSpectrumID2CE` |
| mzTab spectra_ref | `MS1RawSpectrumIdTop`, `MS2RawSpectrumID`, `FileID` |
| representative MSDec lookup | representative `MasterPeakID` or `MSDecResultIdUsed` |

Spot-level metadata can remain in `AlignmentSpotProperty`:

- `AlignmentID`, `MasterAlignmentID`, `RepresentativeFileID`
- `TimesCenter`, `TimesMin`, `TimesMax`
- `MassCenter`, `MassMin`, `MassMax`
- annotation fields and `MatchResults`
- `HeightAverage`, `HeightMin`, `HeightMax`
- `FillParcentage`, `PeakWidthAverage`
- S/N and estimated-noise summaries
- adduct, formula, ontology, comment, internal standard IDs

## Decision

Introduce an LC-MS console-only alignment-light path that separates:

1. spot-level metadata retained in memory,
2. per-file aligned peak values stored in a compact row/chunk store,
3. text exporters that read per-file values on demand.

Do not attempt to preserve full GUI-compatible `.arf2` project data in the
first streaming implementation. The initial success criterion is stable
generation of `mdalign`, `mdmsp`, and `mzTab`.

In alignment-light mode, the Console treats `-p` as incompatible with the light
text-only contract and skips project saving with a console message.

## Proposed Design

### Compact peak row

Create a compact DTO for the text-export-required subset:

```csharp
internal readonly record struct AlignmentLightPeakRow(
    int SpotId,
    int FileId,
    string FileName,
    int MasterPeakId,
    int PeakId,
    double Height,
    double Area,
    double NormalizedHeight,
    double NormalizedArea,
    double Rt,
    double Mz,
    float SignalToNoise,
    int Ms1RawSpectrumIdTop,
    int Ms2RawSpectrumId,
    bool IsMsmsAssigned);
```

This is intentionally smaller than `AlignmentChromPeakFeature` and avoids nested
objects such as `ChromXs`, `IonFeatureCharacter`, `ChromatogramPeakShape`,
annotation dictionaries, and match result containers for every file.

### Fixed-size matrix store

Add an internal alignment-light store:

```csharp
internal interface IAlignmentLightPeakStore : IDisposable {
    void Initialize(int spotCount, IReadOnlyList<AnalysisFileBean> files);
    void WriteSpotPeaks(int spotId, IReadOnlyList<AlignmentChromPeakFeature> peaks);
    IReadOnlyList<AlignmentLightPeakRow> ReadSpotPeaks(int spotId);
}
```

The current implementation uses a binary temporary file as a fixed-size matrix:

- physical layout: `spotCount x fileCount`
- offset: `(physicalSpotId * fileCount + fileIndex) * recordSize`
- record size is fixed because file names are not written per cell
- `AnalysisFileBean` supplies file names and file IDs during reads
- refined/reordered spot IDs are handled by a small logical-to-physical ID map

This avoids one `List<long>` offset list per spot and avoids repeating file
name strings for every spot/sample cell.

### Export adapter

Add a quant accessor that reads from the chunk store:

```csharp
internal sealed class AlignmentLightQuantValueAccessor : IQuantValueAccessor {
    // ReadSpotPeaks(spot.AlignmentID) and produce the same dictionaries as
    // LegacyQuantValueAccessor.
}
```

Then `AlignmentCSVExporter` and `MztabFormatExporter` can be reused at first.

### Refiner boundary

The hardest question is when to clear `AlignedPeakProperties`.

Recommended first streaming phase:

1. Keep full `AlignedPeakProperties` through join, filtering, gap filling, and
   refiner.
2. After `Refiner.Refine`, write per-file rows to the chunk store.
3. Replace each spot's `AlignedPeakProperties` with a minimal representative
   row or clear it after all existing metadata and representative MSDec results
   are resolved.
4. Export text through `AlignmentLightQuantValueAccessor`.

This reduces memory during export and project packing, but not during join and
gap filling. It is the lowest-risk bridge because it should preserve current
text output exactly.

Recommended second streaming phase:

1. Replace `LcmsPeakJoiner.InitSpots` full dummy peak allocation with a compact
   matrix or file-wise sparse map.
2. Run gap filling file-by-file and write final rows directly to the chunk
   store.
3. Compute spot-level summaries incrementally.
4. Keep optional expensive features, such as ion-abundance correlation links,
   behind a console flag.

## Implementation Phases

### Phase 1: export-time detachment

Goal: prove that text exporters can work from a chunk store and that full
`AlignedPeakProperties` can be dropped before project packing.

Expected output:

- `mdalign` identical to baseline.
- `mzTab` identical except filename-derived metadata.
- `mdmsp` identical.
- no GUI EIC cache, `.arf2` alignment object, alignment peak-property object
  file, or project file in alignment-light mode.
- alignment `.dcl` is still written.

Implemented bridge:

- `AlignmentLightPeakStore` writes compact per-file peak rows to a temporary
  fixed-size binary matrix.
- `AlignmentLightQuantValueAccessor` lets existing text exporters read
  quantitative values from that store.
- LC-MS Console skips GUI chromatogram serialization, alignment object
  serialization, and `-p` project saving when `Alignment light mode: True`.
- LC-MS Console still writes alignment representative spectra `.dcl`.
- LC-MS Console forces ion-abundance correlation links off in alignment-light
  mode.

### Phase 2: refiner-light mode

Goal: avoid expensive optional cross-spot vector work.

Candidate switches:

```txt
Alignment light correlation links: False
Alignment light isotope/adduct links: True
Alignment light blank filtering: True
```

The default can preserve existing behavior for validation; large-scale users can
turn off correlation links if needed.

### Phase 3: join/gap-fill streaming

Goal: remove the main `spots x files` object graph.

Implemented prototype:

- LC-MS Console uses `LcmsAlignmentLightRunner` when `Alignment light mode:
  True`.
- `AlignmentLightPeakStore` is initialized once the master peak count is known,
  then stores rows in a fixed-size `spot x file` binary matrix.
- The runner builds an LC-MS master peak list, then visits analysis files in a
  detected-peak pass and writes matched rows directly to `AlignmentLightPeakStore`.
- Gap filling runs file-by-file for retained spots; gap-filled rows are also
  written directly to the compact store.
- `AlignmentSpotProperty.AlignedPeakProperties` is reduced to a single
  representative peak per spot. The full `spots x files` peak object graph is
  no longer retained in memory in the light path.
- Spot-level filtering no longer keeps a `HashSet<int>` of detected file IDs
  per spot. It keeps aggregate QC and class detection counts instead.
- The LC-MS spot-level refiner keeps the current priority order for
  reference-matched and high-abundance features, but ion-abundance correlation
  links remain disabled.
- The representative alignment `.dcl` is still written from the selected
  representative per-file deconvolution results.
- Representative alignment MSDec results are written to `.dcl` from a streaming
  enumerable and exposed to existing exporters through a file-backed
  `IReadOnlyList<MSDecResult>`. This avoids holding the full representative
  alignment MSDec list in memory during light-mode text export.
- The file-backed MSDec list keeps a small bounded LRU cache. This preserves
  lazy file access while avoiding repeated deserialization when an exporter asks
  for the same alignment MSDec result again immediately.
- Light-mode export forces a full GC between alignment text exporters. The
  file-backed MSDec list reduces retained memory, but current exporters
  intentionally reread MSDec objects for separate `mdalign`, `mdmsp`, and
  `mzTab` passes; the GC boundary prevents these temporary allocations from
  accumulating across exporter stages.
- Light-mode mzTab export writes the SML section directly and spools SMF/SME
  sections to temporary text files during the same spot pass. This preserves
  mzTab section ordering while reducing repeated spot metadata and MSDec reads.

FastLC validation after the prototype:

- full alignment retained 908 LC-MS alignment spots.
- alignment-light streaming retained 908 LC-MS alignment spots.
- `mdalign`: same line count and identical SHA256 hash as full output.
- `mzTab`: same line count and identical SHA256 hash after normalizing the
  filename-derived `MTD mzTab-ID`.
- `mdmsp`: same line count and identical SHA256 hash as full output.
- light mode generated the alignment `.dcl`, but did not generate a GUI project
  file.

The only remaining non-identical text detail is the expected filename-derived
`MTD mzTab-ID` value.

Additional performance validation after MS1/raw-read reuse and fixed-size
matrix storage:

| Input | Mode | Elapsed | Peak private memory |
| --- | --- | ---: | ---: |
| 7 FastLC files | Full | 112.3 s | 854.2 MB |
| 7 FastLC files | Light | 113.2 s | 837.7 MB |
| 14 duplicated rows | Full | 223.8 s | 2568.4 MB |
| 14 duplicated rows | Light | 216.8 s | 2386.1 MB |

Additional validation after file-backed alignment MSDec access:

| Input | Mode | Elapsed | Peak private memory | Output comparison |
| --- | --- | ---: | ---: | --- |
| 7 FastLC files | Light | 110.9 s | 795.4 MB | `mdalign`/`mdmsp` byte-identical; `mzTab` identical after normalizing `MTD mzTab-ID` |
| 14 duplicated rows | Light | 217.7 s | 2509.0 MB | `mdalign`/`mdmsp` byte-identical; `mzTab` identical after normalizing `MTD mzTab-ID` |

Additional validation after light-mode mzTab section spooling and bounded
file-backed MSDec cache:

| Input | Mode | Elapsed | Peak private memory | Output comparison |
| --- | --- | ---: | ---: | --- |
| 7 FastLC files | Light | 110.1 s | 837.3 MB | `mdalign`/`mdmsp` byte-identical; `mzTab` identical after normalizing `MTD mzTab-ID` |
| 14 duplicated rows | Light | 212.6 s | 2473.5 MB | `mdalign`/`mdmsp` byte-identical; `mzTab` identical after normalizing `MTD mzTab-ID` |

The small demo is not representative of thousands of files, but it confirms
that the light path now has equal text output and lower peak memory than the
full baseline in the sample-count direction. The 14-row file-backed MSDec run
is slightly higher than the matrix-store-only light run because current
exporters still read MSDec objects in separate `mdalign`, `mdmsp`, and `mzTab`
passes. The mzTab spooler removes repeated reads inside the mzTab pass itself,
improving elapsed time in the 14-row validation while preserving text output.

## Risks

- `AlignmentRefiner` currently expects full per-file peak objects for multiple
  behaviors. Clearing too early will change annotations and links.
- `MztabFormatExporter.WriteSmeDataLine` uses per-file MS/MS scan IDs for
  `spectra_ref`; the chunk store must retain these fields.
- Normalization exports require normalized values. If normalization is enabled
  before export, the chunk store must capture post-normalization values.
- Full GUI project compatibility cannot be guaranteed once per-file peak
  objects are detached.
- The streaming prototype currently targets LC-MS only. Other processes need
  separate join/gap-fill implementations.

## Validation Plan

Use the FastLC WIFF demo:

- baseline console mode,
- alignment-light mode,
- compare SHA256 for `mdalign` and `mdmsp`,
- compare `mzTab` after normalizing filename-derived `mzTab-ID`,
- verify no `.EIC.aef` is created in alignment-light mode.

Then repeat with a larger synthetic CSV that references duplicated demo files
to measure peak memory and runtime behavior.
