# Compact spectral library memory: isolated PR1 measurements

Measured on 2026-11-13. This compares only PR1 with its exact unchanged base,
`c7830d66e1dbbf9077858f4a4154413f48940400`. It does not reuse the numbers from
the combined #859 patch, which also changed disk encoding and search caching.

## Scope and method

- Intel Core i7-1160G7, 15.79 GiB physical RAM, Windows 11 Pro 10.0.26300.
- .NET Framework 4.8, x64; SDK 10.0.402; MessagePack 3.1.7 on both sides.
- Both Core/Common builds use `Debug vendor unsupported` with `Optimize=true`.
  The candidate was explicitly rebuilt. The same optimized harness executable
  and MessagePack binary were used with each version's matching dependencies.
- Each data case has 30,000 synthetic references, 64 peaks each (1,920,000 peaks).
  Integer masses/intensities fit exactly in 16 bits. Fractional values retain
  doubles. These labels describe value storage, not instrument resolution.
  Peaks have default metadata. No acquisition data is included.
- Three serial pairs per case in fresh processes, alternating execution order.
  One-record warm-up precedes measurement. Import/save/load are medians of three
  process measurements. Scoring uses the median of two warmed full-library
  passes per process, then the median across processes. First-pass values and
  every individual measurement are retained in the raw data.
- Retained memory is the managed-heap increase after forced collection, measured
  separately after import and load. It is not total application RAM. Collections
  are outside timed operations. Filesystem caching and background load are not
  controlled; particularly small timing differences should be treated as noise.

## Results

Lower is better. Changes are `(candidate / baseline - 1) * 100`.

| Case | Operation | Baseline | PR1 | Change |
| --- | --- | ---: | ---: | ---: |
| Integer | Retained library after MSP import | 201.67 MiB | 32.29 MiB | -84.0% |
| Integer | Retained library after archive load | 205.54 MiB | 36.16 MiB | -82.4% |
| Integer | MSP import | 1876.12 ms | 1480.47 ms | -21.1% |
| Integer | Archive save | 1225.78 ms | 1213.12 ms | -1.0% |
| Integer | Archive load | 1451.73 ms | 1587.50 ms | +9.4% |
| Integer | EI numeric scoring, all 30,000 | 250.39 ms | 228.85 ms | -8.6% |
| Integer | LC metabolomics scoring, all 30,000 | 426.00 ms | 421.12 ms | -1.1% |
| Integer | Whole-harness peak working set | 630.39 MiB | 459.90 MiB | -27.0% |
| Fractional | Retained library after MSP import | 201.67 MiB | 54.27 MiB | -73.1% |
| Fractional | Retained library after archive load | 205.54 MiB | 58.14 MiB | -71.7% |
| Fractional | MSP import | 3078.93 ms | 2322.01 ms | -24.6% |
| Fractional | Archive save | 1308.24 ms | 1170.44 ms | -10.5% |
| Fractional | Archive load | 1499.27 ms | 1277.46 ms | -14.8% |
| Fractional | EI numeric scoring, all 30,000 | 287.26 ms | 219.55 ms | -23.6% |
| Fractional | LC metabolomics scoring, all 30,000 | 453.63 ms | 413.03 ms | -9.0% |
| Fractional | Whole-harness peak working set | 621.56 MiB | 477.70 MiB | -23.1% |

The clearest result is the retained-memory reduction: **84.0% for integer
spectra and 73.1% for fractional spectra** after MSP import. For this workload,
MSP import also took 21.1% and 24.6% less time. The numeric representation does
not change scoring formulas.

**Regression:** integer archive load took 9.4% longer in the median
(1,451.73 to 1,587.50 ms). Its three candidate times were 1,269.27, 1,599.83
and 1,587.50 ms; baseline times were 1,446.52, 1,475.64 and 1,451.73 ms.
Compaction adds work during load, but these measurements do not isolate its
cost from GC and machine load. This PR does not claim a uniform loading or
search-speed improvement. Integer LC scoring and archive save differ by only
about 1%, which is not persuasive evidence of an improvement.

On the candidate load path, the existing formatter first restores peak objects
from the unchanged archive. Compaction then checks every eligible integer mass
and intensity before packing them. Once a fractional peak rules out packing,
subsequent peaks still receive metadata checks but skip integer eligibility
checks and retain double values. This is a plausible source of extra integer
work, not a measured explanation of the 9.4% median difference. The first
integer pair favored PR1; more load-only repetitions and phase timings would be
needed to separate compaction cost from GC, filesystem cache and host load.

The peak working set includes import, three passes of each score path, save,
load and an extra verification save. Its 27.0% / 23.1% reduction is a whole-
harness observation, not a prediction for peak RAM in the GUI.

## Result and serialization checks

All twelve processes completed. For each data case, the runner required:

- Identical input hashes and complete serialized EI/LC result hashes across
  both versions and all three runs. The result arrays retain library order.
- Identical saved archive bytes across versions, and byte-identical save /
  load / resave within each process. Both cases produced 20,886,329-byte
  archives on both versions: **there is no disk-size reduction or new format**.
- All 30,000 eligible candidate references remain compact after import,
  scoring, save and load; the checks do not invoke the public `Spectrum` getter.

| Case | Input SHA-256 | Archive SHA-256 |
| --- | --- | --- |
| Integer | `b4ce72be26dcca59756f0e82797a92bc735336e6f1fa4e96d16dc5aea5624ba8` | `0cf12a12de15d8b71ba86697a62d00d78b9cffc687d039bae588d0305a5e390a` |
| Fractional | `1f899da2db06dc2cd07ab9f2bfe70660a9f1b956f6da58101c82f7212ab3cd78` | `2b3c11c66160b98ef18c24be228fb1490ae4eaa6a980d62595bb3da663dc10c3` |

These hashes check this benchmark's data; they do not replace metadata, floating-
point edge-case and legacy-reader compatibility tests in the PR.

## Reproduction and limits

The [harness and run instructions](../../benchmark/CompMs/CompactLibraryBenchmark/README.md)
are checked in together with [raw measurements and binary/source identities](../../benchmark/CompMs/CompactLibraryBenchmark/results/2026-11-13.json).

The host had limited available commit memory. One unrestricted candidate rebuild
ran out of memory; a subsequent serial compiler run with a 512 MiB .NET GC heap
limit succeeded. That limit applied only to compilation, not to the benchmark
processes. All measured processes used the same normal runtime settings.

This is a bounded synthetic experiment with three pairs, not a statistical
guarantee or a representative real-project benchmark. Compact eligibility and
the fraction of memory occupied by spectra affect the benefit. Annotated peaks
retain objects; consumers that request the public mutable list expand that
reference. Whole-project RAM savings can therefore be smaller.

Candidate retrieval, cancellation, compound-search window/loading behavior, GC
search caching, lipid structure validation and raw-data processing were not
timed. The LC benchmark calls `MsReferenceScorer.CalculateScore` for metabolomics;
it is not an end-to-end compound search. Released old GUI validation with a
real project remains manual work; independent legacy-reader checks are reported
separately in the PR.
