# MsdialCoreTestApp

This context covers the MS-DIAL console test application used to exercise core LC/MS workflows, especially EIC handling in the console test app.

## Molecular networking inputs

The `msn` command accepts MSP libraries as before. It also accepts MS-DIAL binary peak lists when the corresponding MS/MS spectra are supplied with `--spectrum`:

```text
MSDIALCUI msn -i <analysis.pai2> --spectrum <analysis.dcl> -o <output.pairs> -m <mn-parameter.txt>
MSDIALCUI msn -i <alignment.arf2> -s <alignment.dcl> -o <output.pairs> -m <mn-parameter.txt>
MSDIALCUI msn -i <project.mddata> -o <output.pairs> -m <mn-parameter.txt>
MSDIALCUI msn -i <project.mddata> --analysis-file <analysis-name> -o <output.pairs> -m <mn-parameter.txt>
```

Alignment results are the default for `.mddata` input. Use `--analysis-file` to select an analysis file by its `AnalysisFileName` stored in the project or the peak-list filename; this also switches project processing from alignment to that analysis file. When multiple analysis files are available, the option is required to select one. Binary input type is detected from the `.pai/.pai2` or `.arf/.arf2` extension. A binary peak-list input without `--spectrum` is rejected rather than being interpreted as an MSP file, and `--spectrum` with an MSP input is rejected too. The short form is `-s`. The current implementation reads dcl files; the option name intentionally describes the input role rather than that storage format.

A project records peak lists and alignment results as `.pai` and `.arf`, but they are written to disk as `.pai2` and `.arf2`; `msn` accepts either name. Binary input is networked like an MSP file: every pair of spectra in the selected ion mode is scored once, the node IDs are the master peak or master alignment IDs, and `MaxEdgeNumberPerNode` is not applied. With `MsmsSimilarityCalc: All` the header names the Bonanza, modified dot product, and cosine score columns.

For MSP input, each row's IDs are the scored records' `DatabaseUniqueIdentifier`, or else their 0-based ordinal in the unfiltered file. `msn` exits 0 on success, 1 on a runtime failure, and 2 when it does not start because of a method-file value it cannot use (an unknown `MsmsSimilarityCalc`, an unreadable number) or a `--targetFile` that does not exist or does not apply to the input. Every output file is written as `<output>.partial` and renamed when it is complete, so a failed run leaves no file that looks finished. Progress goes to standard error as whole lines.

## Molecular networking kernels

`MsmsSimilarityCalc` is one of `ModDotLegacy` (the old `ModDot`, still accepted with a notice), `Bonanza` (the default), `Cosine`, `All` and `ModifiedCosine`. Every score `msn --format v2` writes, and every spectral edge the GUI builds, is named by a **kernel identity** in the shared grammar of the MS-DIAL spectrum catalog: `<base>?<key>=<value>&…`, keys sorted, every key present, numbers in plain decimal. With the default method-file values:

```text
msdial.moddot.legacy?prep=mn&prep.above=0.05&prep.abs=0&prep.frame=1&prep.maxmz=10000&prep.minmz=0&prep.pow=0.5&prep.precwin=0&prep.rel=1&prep.scale=100&prep.topk=0&prep.topkwin=50&tol=0.025
msdial.bonanza.v1?…the same keys…
msdial.cosine_matched.legacy?…the same keys…   (the fourth value of All)
gnps.modified_cosine?assign=greedy&ipow=1&mzpow=0&prep=mn&prep.above=0.025&prep.abs=0&prep.frame=0&prep.maxmz=10000&prep.minmz=0&prep.pow=1&prep.precwin=17&prep.rel=1&prep.scale=100&prep.topk=0&prep.topkwin=50&tol=0.025
```

**ModDotLegacy is not the GNPS modified cosine.** It sums over the matched pairs only, so a single matched pair scores 1.0. `ModifiedCosine` is the GNPS-normalised modified cosine (matchms 0.34.0 `ModifiedCosineGreedy`): full spectrum norms, inclusive tolerance, peaks within `MnPrecursorWindow` Da (default 17) of the precursor removed, no 1-Da framing, intensities raised to `MnIntensityPower` (default 1). `Cosine` (a squared simple dot product) has no registered identity and is available in v1 only.

## `msn --format v2`

```text
MSDIALCUI msn -i <query.msp> [-t <target.msp>] -o <out.tsv> -m <mn-parameter.txt> --ionmode Negative --format v2 [--all-pairs] [--kernels ModifiedCosine,ModDotLegacy,Bonanza] [--redact-identity none|query|target|both] [--threads N]
```

- `#` lines first: console version and commit, mode, input file names and sha256 (never a path), the kernel identities, the cutoffs in force, excluded records by reason, redaction.
- Columns: `q_ordinal q_peak_id q_db_id q_name q_inchikey q_precursor_mz q_adduct`, the same for `t_`, `precursor_delta` (q − t), then `score[<identity>]` and `matched[<identity>]` per kernel. The ordinal is the record's 0-based index in the unfiltered file; `peak_id` is `PEAKID` from the COMMENT (the catalog join key) or, for binary input, the spot ID.
- `--all-pairs` writes every pair within `MaxPrecursorDifference`, ignoring `MinimumPeakMatch` and the cutoff. Otherwise those apply to the first kernel.
- `--redact-identity` blanks names, InChIKeys and database IDs and keeps ordinals and PEAKIDs; in self mode any value blanks both sides. MS-DIAL alignment `.mdmsp` names can carry private-library hits, so redact them when the output is shared.
- Rows are in (query, target) order and the file is byte-identical for any `--threads`.
- The MSP reader parses precursor m/z, peak m/z and intensity in single precision; an implementation that recomputes scores from the same MSP must do the same.

## Language

**Peak**:
A detected feature from an MS-DIAL project or alignment result.
_Avoid_: Feature, spot, hit

**EIC**:
Extracted ion chromatogram.
In this context, the same EIC output is produced through two use cases: from raw measurement data with a user-specified m/z, or from an analyzed project by referencing detected peaks.
_Avoid_: TIC, base peak chromatogram

**Project-based EIC**:
An EIC produced from an analyzed project by referencing a detected peak.
_Avoid_: Raw-data EIC, m/z-specified EIC

**Raw-data EIC**:
An EIC produced directly from measurement data using a user-specified m/z.
_Avoid_: Project-based EIC, peak-referenced EIC

**RT correction**:
Retention time correction used to adjust chromatogram peaks across files before downstream export or alignment.
_Avoid_: Retention shift, time warping

**Method file**:
The plain-text `Key: value` file passed to a processing command with `--method`. It holds the processing parameters for one run.
_Avoid_: Parameter file, config file

**Method file template**:
A method file written by the `template` command. It holds the built-in defaults for one processing mode, and every key in it is one the reader applies.
_Avoid_: Sample method file, example parameters
