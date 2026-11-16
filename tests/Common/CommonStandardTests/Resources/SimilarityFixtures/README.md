# Similarity parity fixtures, format version 1

These synthetic and public fixtures are shared unchanged with WS3 C# tests.
Each JSON case file has format, format_version, kind and cases. The manifest
pins the SHA256 and case count of all four case files (not itself or README).
UTF-8, ensure_ascii=True, indent=1, LF, trailing newline; no timestamps.
The repository's .gitattributes disables text conversion for this directory,
preserving the pinned bytes even with core.autocrlf=true. fixtures-check
reports CRLF line endings explicitly before checking hashes.

Run `python -m msdial_spectrum_catalog.similarity_mn_tools fixtures-check
similarity-fixtures`. `fixtures-write DIR` rebuilds synthetic definitions and
all expected values. Public inputs come from the committed kernels-vs20.json;
no library is needed to regenerate. For initial selection only, use
`fixtures-write DIR --vs20-msp PATH`, which validates the adjacent
zenodo-record.json and the full file MD5 before reading records.

Public source: MSMS-Public_all-neg-VS20.msp, MD5
2cc3f3843d7c139e807611553b966202. Attribution: MS-DIAL public mass spectral
library, Zenodo doi:10.5281/zenodo.21904103,
https://doi.org/10.5281/zenodo.21904103, CC BY 4.0
(https://creativecommons.org/licenses/by/4.0/). The selection rule is the first
16 records in file order having a finite positive PRECURSORMZ and at least
three finite nonnegative-m/z, positive-intensity peaks; pair consecutive
selected records into eight pairs. Record indices are zero-based and stored
in each case. Only precursor and peaks are retained. Precursors and m/z are
rounded to five decimal places; intensities to six significant digits before
any computation. Source indices and normalized inputs make the selection
auditable. Source records remain otherwise unchanged in place.

Inputs are raw. Apply the prep in each expected kernel_id before scoring;
prep=none sorts stably by m/z only. msdial_pairs indices refer to these
processed lists; e says which argument has the e role. Compare scores with
abs_tol (default 1e-9), null as not comparable, and string NaN as IEEE NaN.
Compare matched counts only when present: optimal assignment can tie.
Implementations are attached to each expectation; optimal and prep cases
using precwin/topk are Python-only until C# supports them. swap_invariant
false permits the documented equal-precursor MS-DIAL role asymmetry.
The nonfinite-weight modified-cosine guard (NaN score, zero assigned pairs)
is Python-only too, including MN-prepped all-zero inputs. Entropy returns
null if either spectrum has nonpositive total intensity or any nonfinite intensity;
this not-comparable rule also applies after MN zero/zero normalization.
MN preprocessing rejects nonfinite input intensities with ValueError before
filtering, regardless of peak order; finite all-zero inputs still produce NaN.
prep.json uses a prep_id query without a base or tol. kernel-ids.json maps
input to canonical or error. All kernel identities are used verbatim across
languages. Cases with zero intensities also pin legacy NaN behavior.

The synthetic fixture definitions and generator are LGPL-3.0-or-later, as is
the repository; public VS20 inputs retain CC BY 4.0. No experimental record
is a fixture. See docs/similarity_kernels.md for formulas and source lines.
