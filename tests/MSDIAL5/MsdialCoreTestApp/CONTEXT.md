# MsdialCoreTestApp

This context covers the MS-DIAL console test application used to exercise core LC/MS workflows, especially EIC handling in the console test app.

## Molecular networking inputs

The `msn` command accepts MSP libraries as before. It also accepts MS-DIAL binary peak lists when the corresponding deconvolution spectra are supplied with `--dcl`:

```text
MSDIALCUI msn -i <analysis.pai2> -dcl <analysis.dcl> -o <output.pairs> -m <mn-parameter.txt>
MSDIALCUI msn -i <alignment.arf2> -dcl <alignment.dcl> --alignment -o <output.pairs> -m <mn-parameter.txt>
MSDIALCUI msn -i <project.mddata> -o <output.pairs> -m <mn-parameter.txt>
```

Analysis peak lists use their MS-DIAL peak identifiers to locate spectra in the dcl file. Alignment results use `MasterAlignmentID`, which is the index of the alignment dcl record. A binary peak-list input without `--dcl` is rejected rather than being interpreted as an MSP file.

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
