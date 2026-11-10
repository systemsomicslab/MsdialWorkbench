# ADR 0003: Representing an annotation score that was never computed

## Status

Accepted. Implemented for the analysis (`.mdpeak`) and alignment (`.mdalign`)
text exports. GC-MS `.mdscan`, mzTab-M and `ResultExport` still use their own
conventions and are out of scope; they are recorded under Known divergences
below.

## Context

MS-DIAL distinguishes four annotation outcomes for an exported feature:

| Outcome | Name written | Meaning |
| --- | --- | --- |
| reference match | no prefix | a product-ion spectrum was compared and passed the criteria |
| low score | `low score: ` | a product-ion spectrum was compared and failed the criteria |
| precursor only | `no MS2: ` | no product-ion spectrum existed, so nothing was compared |
| unknown | `Unknown` | no candidate at all |

`MsdialCore/Utility/DataAccess.cs` `SetMoleculeMsPropertyAsSuggested` writes
`"no MS2: "` when `MS2RawSpectrumID < 0` and `"low score: "` otherwise.

The two text exports disagreed about the third row. Measured on the checked-out
FastLC demo output for a `no MS2: ` row:

```text
.mdalign  Simple/Weighted/Reverse dot product = null
.mdpeak   Simple/Weighted/Reverse dot product = 0.000
```

Both files wrote `Matched peaks count = -1.00` and
`Matched peaks percentage = -1.00` for the same row, which is an explicit
not-applicable marker: a count cannot be negative.

### Why the two exports differed

Two sibling base classes each declare their own `ValueOrNull` helper, and the
two call sites differ by one null-conditional operator, so overload resolution
picks a different helper in each.

| | `.mdpeak` | `.mdalign` |
| --- | --- | --- |
| Base class | `BaseAnalysisMetadataAccessor` | `BaseMetadataAccessor` |
| Call | `ValueOrNull(matchResult?.SimpleDotProduct, "F3")` | `ValueOrNull(matchResult.SimpleDotProduct, "F3")` |
| Argument type | `float?` | `float` |
| Overload bound | `ValueOrNull(double?, string)` | `ValueOrNull(float, string)` |
| Body | `value?.ToString(format) ?? "null"` | `Math.Abs(value) > 1e-10 ? value.ToString(format) : "null"` |
| Result for 0 | `0.000` | `null` |

Neither value was a decision about representation. `.mdpeak` wrote `null` only
when the whole match result was null, which happens for `Unknown` because
`BaseAnalysisMetadataAccessor` applies `NullIfUnknown`. `.mdalign` wrote `null`
for any value within 1e-10 of zero, whatever produced it.

### Why the value was 0 in the first place

`MsScanMatching.GetSimpleDotProduct`, `GetWeightedDotProduct`,
`GetReverseDotProduct` and `GetSpectralEntropySimilarity` return `-1` when
`IsComparedAvailable` is false, that is when either the measured or the
reference spectrum is empty. `GetMatchedPeaksScores` returns `{-1, -1}` under
exactly the same condition, which is where the `-1.00` in the matched-peak
columns comes from. The `-1` is one sentinel, written by one condition.

Before the squared-metrics rename in #589 (`4b39b845e`, released as
5.5.250625) `WeightedDotProduct` and its siblings were plain fields and the
scorers assigned that `-1` directly, so both exports printed `-1.000` and
agreed with the matched-peak columns. #589 moved the storage to
`SquaredWeightedDotProduct` and made the dot products derived:

```csharp
public float WeightedDotProduct {
    get => (float)Math.Sqrt(Math.Max(SquaredWeightedDotProduct, 0f));
    set => SquaredWeightedDotProduct = value * value;
}
```

`Math.Max(..., 0f)` turns the sentinel into 0. From that release on, `.mdpeak`
reported a score for a comparison that never happened, and `.mdalign`'s eps
rule reported `null` for a score that a real comparison had produced as 0. The
raw `-1` still survives in the squared fields, so the fact was recoverable.

### The eps rule loses real measurements

`.mdalign`'s rule is lossy in the opposite direction. On the same demo output,
alignment ID 264:

```text
Metabolite name  low score: NAGly 11:0
MS/MS assigned   True
Simple 0.007   Weighted 0.230   Reverse null
Matched peaks count null   Matched peaks percentage null
```

A product-ion spectrum was acquired and compared. The reverse dot product and
the matched-peak count really were 0, and `.mdalign` discarded them. `.mdpeak`
reported the same class of row correctly as `0.000` / `0.00` (peak ID 106,
`low score: LPA 13:1`). So "null means never computed" described neither
export before this change.

## Decision

A score column is written as `null` when and only when the score was never
computed. A score that was computed is written as its value, including an
exact 0.

"Compared against the reference and scored zero" is a weaker claim than "no
comparison was possible", and only the first is a measurement. A consumer that
reads `0.000` as a value concludes a spectral comparison was performed and
returned zero similarity, which materially overstates the evidence for a
precursor-only suggestion.

Both exports now take the five shared score columns from one place,
`MsdialCore/Export/AnnotationScoreFormat.cs`, so they cannot drift apart
again. The columns are Simple, Weighted and Reverse dot product, Matched peaks
count and Matched peaks percentage. `.mdpeak`'s Enhanced dot product and
Spectrum entropy follow the same rule; no concrete accessor currently keeps
them in its header, but the base class computes them.

`Matched peaks count` and `Matched peaks percentage` move from `-1.00` to
`null` for the same reason. `-1` was already a not-applicable marker rather
than a measurement, so this changes the notation, not the meaning, and it
removes the situation where one row expressed "not applicable" two different
ways.

### How "never computed" is decided

`MsScanMatchResult.IsSpectrumComparisonPerformed` holds the result-side test:

- an `Unknown` result never had a candidate to compare,
- a `TextDB` result has no reference spectrum, so `LcmsTextDBAnnotator` and its
  siblings score only m/z, RT and isotopes and leave every spectral field at
  the default 0,
- any of the five raw spectral fields being negative is the `-1` sentinel. The
  squared fields are read rather than the clamped dot products, because that is
  where the sentinel survives.

`AnnotationScoreFormat` adds one observation-side test. `DimsMspAnnotator`
scores through `Ms2MatchCalculator`, which detects the `-1` and returns
`Ms2MatchResult.Empty`, all zeros, so the sentinel never reaches the match
result on that path. The formatter therefore also treats an entirely unset
score block as uncomputed when the peak or spot carries no product-ion
spectrum at all: `ChromatogramPeakFeature.IsMsmsContained` for `.mdpeak` and
`AlignmentSpotProperty.IsMsmsAssigned` for `.mdalign`. This test can never
discard a computed score, because a computed score implies a spectrum was
present, and it is only consulted when the block carries no evidence of a
comparison.

## Consequences

Downstream-visible change, by outcome. `->` marks a changed cell.

| Outcome | `.mdpeak` dot products | `.mdpeak` matched peaks | `.mdalign` dot products | `.mdalign` matched peaks |
| --- | --- | --- | --- | --- |
| reference match | value | value | value | value |
| low score, non-zero | value | value | value | value |
| low score, computed 0 | `0.000` | `0.00` | `null` -> `0.000` | `null` -> `0.00` |
| precursor only | `0.000` -> `null` | `-1.00` -> `null` | `null` | `-1.00` -> `null` |
| text database | `0.000` -> `null` | `0.00` -> `null` | `null` | `null` |
| unknown | `null` | `null` | `null` | `null` |

### Consumers

- `msdial_spectrum_catalog` `ingest.py` normalizes a negative matched-peak
  value to `NULL`, and an exact `0.0` dot product on a `precursor_only` row to
  `NULL`. Both normalizations become no-ops against a Console that includes
  this change, and both keep working against an older one. No change required.
- `msdial_interactive_app` `workflow.py` `parse_mdpeak` and `parse_mdscan`
  depend on the `-1` sentinel. `msp_candidate_count` is the number of rows
  where all five values are non-null, and `msp_scored_count` the subset where
  all are also `>= 0`, which made a precursor-only row a candidate but not
  scored. With `null` in those cells the two counts collapse to the same
  number. Interactive derives the candidate count from the annotated name
  instead, which reproduces today's number against both an older and a newer
  Console.
- The GUI reads `MsScanMatchResult` directly and is unaffected.

### Known divergences left in place

These are recorded, not fixed, to keep this change scoped:

- GC-MS `.mdscan` (`GcmsAnalysisMetadataAccessor`) writes the string `-1`
  through its own `NegativeIfNull` helper when the match result is null, which
  is a third convention.
- GC-MS `.mdalign` (`GcmsAlignmentMetadataAccessor`) overrides
  `Fragment presence %` as `MatchedPeaksPercentage * 100`, so the sentinel
  renders as `-100.0`. Its Simple, Weighted and Reverse dot product and
  Matched peaks count columns come from `BaseMetadataAccessor` and therefore do
  follow this ADR.
- mzTab-M (`MztabFormatExport.cs`) writes `matchResult.SimpleDotProduct` with
  no not-applicable handling, so `id_confidence_measure` reads `0` for a
  precursor-only row.
- `ResultExport.cs` formats the same properties as `{0:0.00}`, a fifth
  convention.
- `BaseAnalysisMetadataAccessor.NullIfNegative` is declared and unused. It is
  the treatment this ADR adopts, arrived at independently.
- A DI-MS row whose reference spectrum was empty while the peak did carry a
  product-ion spectrum still reports `0.000`, because `Ms2MatchCalculator`
  discarded the sentinel and the observation-side test does not fire. Fixing
  that means preserving the sentinel in `Ms2MatchResult`, which changes DI-MS
  scoring input and needs its own validation.

### Separate finding, not addressed here

`MsReferenceScorer.CalculateScore` guards the MS/MS term of the total score
with `result.WeightedDotProduct >= 0 && result.SimpleDotProduct >= 0 &&
result.ReverseDotProduct >= 0`. Since #589 clamped those getters to 0 the
guard can no longer fail, so the MS/MS term is now always added, including for
a precursor-only candidate whose `MatchedPeaksPercentage` is `-1`. That is why
`Total score` reads `-0.143` for a `no MS2: ` row instead of being computed
from mass and RT similarity alone. This changes candidate ranking, not just
formatting, so it is a scoring question with its own validation and is
deliberately left out of this change. `IsSpectrumComparisonPerformed` gives
that guard a correct expression when it is addressed.

## Validation

### Formatting precision

Centralising the columns moved the formatting boundary, and that had to be corrected before the
demo diff was clean. `.mdpeak` used to bind `ValueOrNull(double?, string)`, so the stored `Single`
was widened to `double` before `ToString("F3")`. `.mdalign` bound a `float` overload. .NET
Framework formats a `Single` through a 7-significant-digit intermediate, so a stored
`0.59349995851516724` becomes `0.5935` and `"F3"` then rounds it up to `0.594`, while the widened
`double` correctly rounds down to `0.593`.

A first implementation took a `Func<MsScanMatchResult, float>` and changed two `Simple dot product`
cells of the demo, `0.593 -> 0.594` in `20230406_feces_1_NEG.mdpeak` peak 1013 and `0.403 -> 0.404`
in `20230407_plasma_3_NEG.mdpeak` peak 92. The selector is therefore
`Func<MsScanMatchResult, double>`, which is both the accurate rounding of the stored value and the
one that leaves `.mdpeak` unchanged apart from this ADR's decision. Measured against the float
variant on the same demo, the `double` selector changes only those two cells and leaves `.mdalign`
byte-identical, so nothing was traded away for it.

### Builds

Console built from this branch, Release/net48, at
`tests/MSDIAL5/MsdialCoreTestApp/bin/Release/net48/MSDIALCUI.exe`, the path MS-DIAL Interactive
uses for a local source build. `src/MSDIAL5/MsdialGuiApp` also builds Release/net48 with
`-p:SkipLibraryDownload=true`; without that switch the build fails on an external Zenodo download
returning 403, which is not a compilation failure.

A baseline Console was built the same way from `origin/master` in a separate worktree, so that
every differing cell is attributable to this change rather than to any other difference.

### Determinism control

The baseline Console ran the demo twice. All seven `.mdpeak` files and the `.mdalign` were
byte-identical between the two runs, and only the timestamp-derived `MTD mzTab-ID` line of the
mzTab differed. The pipeline is deterministic on this dataset, so no observed difference is run
noise.

The checked-in demo output in
`msdial_spectrum_catalog/validation/console_fastlc_demo` is byte-identical to the baseline run for
all seven `.mdpeak` files and the `.mdalign`, so it is reproducible from `origin/master` with the
demo's own `method.txt` and the diff against it has no confounds.

### Run A: the demo's own method.txt, LBM annotation only

Seven SCIEX WIFF LC-MS/MS negative lipidomics files, 13,193 `.mdpeak` rows and 2,512 `.mdalign`
rows:

```text
MSDIALCUI.exe lcms -i <demo>\analysis_files.csv -o <out> -m <demo>\method.txt
```

| Artefact | Changed cells |
| --- | --- |
| `.mdpeak` x7 | 3,419 precursor-only rows x 5 score columns. Dot products `0.000` -> `null`, matched-peak columns `-1.00` -> `null` |
| `.mdalign` | 1,021 precursor-only rows, matched-peak columns `-1.00` -> `null`. 68 Simple, 74 Weighted, 74 Reverse, 74 Matched peaks count and 71 Matched peaks percentage cells of low-score rows recovered as `0.000` / `0.00`. One reference-matched row recovered a Weighted dot product |
| `.mdmsp` x7 per file, `.mdmsp` alignment, `.qa.tsv` | byte-identical |
| `.mzTab` | only the timestamp-derived `MTD mzTab-ID` line |

No cell outside the five score columns changed in any artefact.

The recovered reference-matched row is the clearest case for the decision. Alignment ID 803,
`RIKEN N-VS1 ID-2003 from Mouse_Plasma_ApoEKO_N_F1EPA`, has `MS/MS assigned = True`,
`MS/MS matched = True`, annotation tag 430, a 235-peak MS/MS spectrum, Simple 0.021, Reverse 0.866
and Matched peaks count 1.00. Its Weighted dot product really was computed and really was 0. The
eps rule reported it as `null`, indistinguishable from a row that was never compared.

### Run B: the same method plus an MSP and a text database

`Msp file path` set to `lib\MSMS-Public_all-neg-VS19.msp` and `Text DB file path` to
`lib\20200121_MsdialTxtDB_Neg_EquiSPLASH_rapid.txt`, to reach the text-database case with real
data. Same shape of result, plus 2 to 6 text-database rows per `.mdpeak`, for example
`LPC 18:1(d7)` and `SM 18:1;2O/18:1(d9)` at annotation tag 530, moving from `0.000` / `0.00` to
`null`. Those rows carry a product-ion spectrum, so only the TextDB test in
`IsSpectrumComparisonPerformed` catches them.

### Downstream ingest

Both Run A outputs were ingested into a scratch `msdial_spectrum_catalog` database:

| `annotation_kind` | rows | baseline | after this change |
| --- | --- | --- | --- |
| `precursor_only` | 4,440 | all dot products and matched-peak values NULL | unchanged |
| `low_score` | 824 | 68 NULL simple dot products, 74 NULL matched-peak counts | 0 NULL; those become 0.0 |
| `msms_matched` | 2,246 | unchanged | unchanged |

The catalog already normalized `precursor_only` exact-`0.0` dot products and negative matched-peak
values to NULL, so that normalization becomes a no-op and its output is unchanged. The `low_score`
figures are the measurements the eps rule was discarding; `score_convention` is now populated for
those 68 rows instead of NULL.

### Tests

`tests/MSDIAL5/MsdialCoreTests/Export/AnnotationScoreRepresentationTests.cs` asserts the
representation of all four annotation outcomes plus a text-database annotation in both formats,
that the two formats agree cell for cell on every outcome, and that the sentinel is visible only in
the squared fields.

Suites run: MsdialCoreTests 302, CommonStandardTests 833, MsdialLcMsApiTests 66, MsdialDimsCoreTests
33, MsdialImmsCoreTests 56, MsdialLcImMsApiTests 52, MsdialGcMsApiTests 6, MsdialCoreTestAppTests 9.
All pass. The DI-MS residue noted above is reasoned from source and covered by a unit test, not by
an end-to-end DI-MS run, because this campaign has no DI-MS fixture.
