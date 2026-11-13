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
