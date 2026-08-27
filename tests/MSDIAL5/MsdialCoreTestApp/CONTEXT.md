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

Alignment results are the default for `.mddata` input. Use `--analysis-file` to select an analysis file by its `AnalysisFileName` stored in the project or the peak-list filename; this also switches project processing from alignment to that analysis file. When multiple analysis files are available, the option is required to select one. Binary input type is detected from the `.pai/.pai2` or `.arf/.arf2` extension. A binary peak-list input without `--spectrum` is rejected rather than being interpreted as an MSP file. The short form is `-s`. The current implementation reads dcl files; the option name intentionally describes the input role rather than that storage format.

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
