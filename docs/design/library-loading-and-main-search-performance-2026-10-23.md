# Library storage, loading and main Compound Search performance

Performance baseline: frozen upstream `6b73c6231c1acb93e19b59dc07f944089a4f7ff9`. Submission integration: upstream `7cf9b568b9382aa27ca6d925e0d15a3ea0e3523a`. Final local review: 2026-10-31.

## Problem and scope

Large molecular reference libraries spend substantial time and memory constructing mutable peak objects. The complete real GC library used here contains 394,054 records / 47,515,867 peaks in a 460.67 MiB text MSP. These costs affect initial library import, saved-project reopening, persistence and scoring that materializes reference spectra. Repeating the same GC Compound Search previously scores its candidate set again.

The changes cover molecular library storage/I/O, numeric scoring access, main Compound Search reuse and cancellation, and GC console persistence. They do not change scientific scoring formulas, accumulation order, tolerances, weights, structural annotation rules or the number of returned results in the covered comparisons. No dependency, raw acquisition reader or framework change is included.

## Implementation and affected files

| Area | Main files | Behavior |
| --- | --- | --- |
| Exact in-memory representation | `CommonStandard/Components/MoleculeMsReference.cs`, `SpectrumPeakData.cs` | Eligible ordinary peaks use numeric arrays. Two nonnegative integral values up to 65,535 share four bytes; fractional, signed-zero and nonfinite values retain exact doubles. Annotated, derived and nondefault peaks retain complete objects. |
| Library persistence | `CommonStandard/MessagePack/LargeListMessagePack.cs`; `MsdialCore/DataObj/CompactLibraryResolver.cs`, `MoleculeDataBase.cs`, `MsdialDataStorage.cs` | Save targets 16 MiB blocks inside the existing entry; load reuses buffers up to 64 MiB. Old giant blocks retain the measured legacy allocation cadence. |
| Import/export and provenance | `CommonStandard/Parser/MspFileParcer.cs`; `MsdialCore/Utility/LibraryHandler.cs`, `DataObj/LibraryFingerprint.cs` | MSDIAL5 imports opt into compaction, including selected lipid `.lbm`/`.lbm2` records. Exports and fingerprints read numeric views. The common one-argument MSP parser retains its default behavior. |
| Shared scoring | `CommonStandard/Algorithm/Scoring/MsScanMatching.cs`, `Algorithm/Function/FrameBinnedSpectrum.cs`; `MsdialCore/Algorithm/Annotation/MsReferenceScorer.cs` | Access numeric peaks without expanding reference libraries; preserve arithmetic and stable ties. |
| GC main search | `MsdialGcMsApi/Algorithm/CalculateMatchScore.cs`; GUI `GcmsCompoundSearchCache.cs`, analysis/alignment use cases and `Search/ReferenceSpectrumState.cs` | Keep one validated query per calculator. Validate parameters, query, candidate order/membership and scored reference content; copy result records for manual annotation. No top-100 truncation. |
| LC main search | `MsdialLcMsApi/Algorithm/Annotation/LcmsMspAnnotator.cs`; GUI `Search/CompoundSearcher.cs`; `IAnnotationQueryFactory.cs` | Built-in LC MSP searches perform full scoring every call and stable ordering once. Ordinary annotator callers retain sorted results; derived/custom annotators retain their original route. No persistent LC result cache. |
| Cancellation | Query/query-factory and built-in annotation files under Core, LC, DIMS, IMMS and LC-IMMS; GUI main search model and use cases | Cancellation belongs to a query; candidate loops check it, and closed/cancelled searches suppress late publication. Existing overloads remain callable. |
| GC console save | `tests/MSDIAL5/MsdialCoreTestApp/Process/GcmsProcess.cs` | Await project saving, report dataset-save failure and avoid persisting the same GC library again as a duplicate legacy list. This project is the console application despite its folder name. |

The submission contains 42 production files, 20 test/fixture files and this report. Project-wide paths beginning above at CommonStandard, MsdialCore, MsdialGcMsApi, MsdialLcMsApi and GUI are under `src/Common/` or `src/MSDIAL5/`.

## Data and compatibility contracts

All reference metadata remains available, including SMILES, InChIKey, formula, adduct, RT/RI/CCS, isotope profiles, identifiers and links. Compaction changes only the representation of eligible mass/intensity pairs. Public `Spectrum` access materializes editable objects; subsequent edits are preserved. Rich fragment comments, required-fragment flags and other nondefault peak data use the original object/formatter route.

The compact writer emits existing MessagePack keys, omits only trailing default peak fields and chooses shorter number encodings only when they preserve the original double bits. ZIP compression remains enabled. File bytes change; the unchanged frozen `6b73c623` reader restores the same complete content in the cross-reader checks. Standard-formatter tests separately cover complete content and numeric edge cases. This is not a certification of every released reader version. New code reads fixed upstream-generated fixtures, historical layouts, legacy sidecars, null/empty/mixed spectra and extended peak layouts.

The block size does not create extra files and is a target, not a maximum for a single oversized record. Buffer reuse is explicit for owned-value library readers; the default generic reader remains safe for formatters returning borrowed byte segments. A decoding exception leaves the existing database collection intact.

An old project benefits from compact in-memory loading immediately. One normal save writes the smaller encoding for future opens; raw-data reanalysis and project recreation are unnecessary. Opening alone does not rewrite the library. A separately saved copy of the full historical GC project retains both libraries, both alignment runs, all 83 features (29/44/10), tags and every complete search result. All 41 original historical project/data files still match their preserved copies by SHA-256 after this review.

## Measurement method

Windows laptop, i7-1160G7, about 15.8 GiB RAM, AC connected, x64/net48 `Debug vendor unsupported` builds with optimization enabled on both sides. Loaded-assembly manifests verify hashes and enabled JIT optimization; the baseline runtime matches the earlier frozen upstream runtime. These comparisons use the implementation based on the frozen baseline; integration against `7cf9b568` is checked separately. Probes run serially, use fresh processes and alternate version order. Disk caches are not cleared. Values below are medians, not per-call guarantees.

The 2026-10-31 GC subset uses three pairs and a 1.5 GiB process limit with system-memory headroom checks. Each process saves three times; save values are medians of those per-process medians. Constructed LC probes use a 512 MiB limit. Real LC uses six pairs: three additional pairs investigate variable first-call times, and all six are included. Earlier 100,000-reference and full GC search pairs use a separate 3 GiB budget and are labelled separately.

Project load timers measure `ProjectDataStorage.LoadReportingFailuresAsync`, including persisted libraries. They exclude model/chart construction, first-sample opening, rendering and interaction. Retained managed memory, process working set and virtual-memory commit are distinct metrics. Content hashing and test-only collection occur outside timed operations. Private spectra, acquired data, generated projects and diagnostic probes are excluded from the submission.

## Fresh GC subset comparison — 2026-10-31

| Operation, identical input | Upstream | Optimized | Observation |
| --- | ---: | ---: | ---: |
| Import 50,000 real GC references | 3.729 s | 2.713 s | 27.3% less time |
| Retained managed imported library | 341.09 MiB | 70.06 MiB | 79.5% less memory |
| Process peak working set during import/save probe | 1046.52 MiB | 282.15 MiB | 73.0% less memory |
| Save imported library | 0.942 s | 0.362 s | 61.6% less time |
| Saved library stream | 27,887,274 bytes | 14,912,671 bytes | 46.5% less space |
| Load identical upstream-created 50,000-reference project | 3.345 s | 2.970 s | 11.2% less time |
| Retained managed storage of that project | 683.44 MiB | 160.74 MiB | 76.5% less memory |

Every import and saved/reloaded representation retains the approved full-content digest. All three saves in each of the six processes are stable; complete source metadata is retained. These are library I/O and storage measurements, not whole GUI project-creation/opening times. The saved stream is LZ4-encoded library data, not the whole project archive or its raw-data files.

## Larger GC evidence — 2026-10-30

The unchanged final production source also has three fresh-process upstream/current pairs at 100,000 real references:

| Operation | Upstream | Optimized |
| --- | ---: | ---: |
| Import | 8.285 s | 6.007 s |
| Retained managed imported library | 800.97 MiB | 147.88 MiB |
| Save | 2.229 s | 0.813 s |
| Saved library stream | 66,940,848 bytes | 36,064,741 bytes |
| GC first search, all 64,902 results | 1.177 s | 1.041 s |
| Repeated identical GC query | 1.190 s | 0.02385 s |

For the upstream full-library search probe only, every reference's metadata and all potentially scored spectra remain; unscored spectra are omitted to fit the laptop. The optimized search retains all spectra. Both return the same 64,902 complete ordered records. This upstream process is not a full-library memory benchmark. Repeated optimized calls include 265–303 ms outliers in that paired run; their cause was not instrumented, and the median is not a guaranteed call duration.

The first complete 460.67 MiB text import still takes 40.00 s in one optimized sample. There is no completed full upstream import within the same test budget, so no full-text import speed ratio is claimed.

With the same optimized reader, an independent copy of the historical complete GC project changes from 17.73 s storage-load median before a normal save to 9.65 s after saving, and its library archive changes from 293,159,486 to 177,385,048 bytes (39.5% smaller). This isolates saved layout, not upstream versus optimized software. All library/feature/tag/result digests match before and after saving.

## Full-project follow-up — 2026-10-31

A complete GC project already saved with the optimized writer loads storage in **7.650 / 7.272 / 8.708 s**, median **7.650 s**, retaining 773.93 MiB of managed storage. A separate no-window model probe reaches its first sample at 9.512 s. Neither timer asserts rendering or readiness for native-window interaction.

The fresh full-library main search retains all 64,902 results, first 1.328 s, repeated 37.7–438.0 ms including one high-latency outlier of unmeasured cause. This is a single follow-up process, not a new paired speed ratio.

A separate manual upstream attempt to open the untouched historical project fails after more than one minute. A Windows low-virtual-memory event reports 5,492,822,016 bytes (about 5.12 GiB) for MS-DIAL; this is the event's virtual-memory figure, not a sampled physical working set. The following shutdown-stage rendering exception does not identify the allocation that caused the pressure. This corroborates memory pressure but supplies neither a completed load time nor an upstream/optimized timing ratio. The historical and newly saved projects have independent saved libraries; their shared raw inputs are unchanged.

## LC results and tradeoffs — 2026-10-31

The real LC positive-DDA project contains two samples, one blank and 3,842 references. The harness selects 20 stored features per file by their existing representative score and queries their deconvoluted MS2 spectra. It supplies an empty raw-MS1 spectrum and an ion feature with isotope weight zero to both versions. All 60 queries / 207 complete ordered records agree with frozen upstream in every one of the twelve processes for those inputs. This checks the stored-feature MS2 search path; it does not validate a populated raw-MS1 isotope workflow or a complete native-window acquisition/annotation run.

| Real LC metric, six pairs | Upstream | Optimized |
| --- | ---: | ---: |
| Project storage load | 811.31 ms | 851.50 ms |
| First query in a fresh process | 49.96 ms | 74.99 ms |
| Sum of the 60 first calls | 56.39 ms | 89.82 ms |
| Sum of their repeated calls | 6.11 ms | 6.69 ms |

The small real LC project does **not** establish a time improvement. The first query costs about 25 ms more in this review; initialization/JIT work is included and has not been isolated as the sole cause. The current first-query range is 51.37–82.08 ms, versus 43.05–58.70 ms upstream. Complete scientific records agree for the stated inputs. No persistent LC result cache is present.

Constructed LC spectra have 24 peaks per reference and compare every complete ordered result. Each column lists upstream / optimized:

| LC workload | First search | Repeated search | Retained managed library |
| --- | ---: | ---: | ---: |
| 100,000 references / 5,001 candidates | 120.31 / 127.40 ms | 31.83 / 32.60 ms | 278.47 / 88.39 MiB |
| 50,000 references / 50,000 candidates | 411.71 / 389.71 ms | 335.50 / 323.84 ms | 139.24 / 44.20 MiB |

Both constructed libraries retain about 68.3% less managed memory. The dense case is modestly faster; the 5,001-candidate case is slightly slower in this run. Larger data does not guarantee proportional scoring speedup: candidate count, spectrum content, metadata and initialization matter. A separate earlier real LC save comparison reduces the archive from 2,248,060 to 1,668,622 bytes (25.8%), while retaining the same complete ordered results.

## Correctness checks and limits

| Check | Final evidence |
| --- | --- |
| Upstream-integrated processing suites | 1,989 passed / 17 inherited skips / 0 failed: Core 911, GUI 441, console 288, LC 156, GC 20, DIMS 47, IMMS 65, LC-IMMS 61. |
| Shared Common code | 949 selected cases plus the explicitly selected small serialization row: 950 nonduplicated passes. Total 2,939; overlapping earlier executions are not added. |
| Desktop builds | Optimized net48, net481 and net472, zero errors; inherited warnings remain. |
| Frozen scoring oracle | Fractional, integral and edge-intensity runs each pass 6,400 exact metric/complete-EI comparisons: 19,200 total. Signed zero, NaN payloads, infinities and negative intensities with finite masses are included. |
| Structural annotation checks | Same 20 rich EAD records and 32 peptide records match frozen upstream in each oracle run; repeated executions are not new distinct cases. |
| Storage contracts | Fixed upstream-generated fixtures, full old-reader canonical content, exact double bits, rich-peak bytes, null/empty/mixed/extended layouts, bounded allocation, concurrent reads/writes and borrowed-buffer safety. |
| Project compatibility | Five-mode complete dataset round trips; repeated saves across integral/fractional edits; SMILES/InChIKey/formula/required-fragment preservation; full historical GC content and real LC results. |
| Search lifecycle | GC analysis/alignment full results and stable ties, parameter/query/reference/candidate invalidation, manual-score isolation, concurrent reference viewing, cancellation and delayed completion. LC full-result/parameter comparisons and built-in cross-mode cancellation. |
| Console persistence | Canonical GC database retained without duplicate legacy content; save completion and reported failure covered. |

Seventeen inherited skips are 15 external lipid-RT reference cases and two ignored LC cases. The generic near-2-GiB payload and million-row cases are outside this fresh laptop run; an earlier bounded million-row pass remains separate evidence. Native library test-discovery warnings do not correspond to failed test cases.

The raw large-list framing has no total-count/footer check: both readers accept a prefix ending exactly after a complete block. This diagnostic bypasses the project archive; decoding-error preservation is not arbitrary-corruption detection. Crash/disk-full recovery is not fault-injected and saving is not claimed to be a whole-project atomic transaction.

Remote CI, automated native-window interaction, and real IM/LC-IM/imaging/protein/EAD acquisition workflows remain unverified. Molecular storage benefits depend on eligible spectra; peptide-specific stores and rich generated spectra do not automatically receive integer packing. Mode-specific scientific rules are preserved in the tests/oracles, not certified for every raw acquisition by those checks.

## Public reproduction

The GUI test project contains bounded constructed GC and LC benchmarks with no private-data requirement. After the repository's normal dependency restore, the following commands use the same local build settings as the validation runs. The BclBuildImported property avoids an inherited desktop reference-package injection in this environment; it does not change checked-in framework or package configuration. Set the corresponding environment variable and select the test explicitly:

```powershell
$testFlags = @('--no-restore', '-c', 'Debug vendor unsupported', '-p:Optimize=true', '-p:UseSharedCompilation=false', '-p:ConcurrentBuild=false', '-p:BuildInParallel=false', '-p:DownloadLibraries=false', '-p:DownloadLibrary=false', '-p:BclBuildImported=Ignore', '-m:1', '-nr:false')
$env:MSDIAL_RUN_SYNTHETIC_GC_BENCHMARK = '1'
dotnet test tests/MSDIAL5/MsdialGuiAppTests/MsdialGuiAppTests.csproj @testFlags --filter 'FullyQualifiedName~BoundedSyntheticGcBenchmarkIncludesReferenceValidationAndViewerExposure'
$env:MSDIAL_RUN_SYNTHETIC_LC_BENCHMARK = '1'
dotnet test tests/MSDIAL5/MsdialGuiAppTests/MsdialGuiAppTests.csproj @testFlags --filter 'FullyQualifiedName~BoundedSyntheticBenchmarkReportsFirstAndRepeatedFullSearches'
```

These public benchmarks exercise the optimized production paths and verify complete results; they do not embed a frozen upstream assembly or assert a machine-dependent speed ratio. To reproduce the real-data pairs, run each version separately on identical complete MSP records and copies of the same project, record assembly identities, alternate order across fresh processes, time storage/import/save/search separately, and compare complete canonical content and ordered scores. Use bounded subsets if the full upstream library exceeds available memory.

## Per-process samples and content checks

The real GC subsets contain the first 50,000 / 100,000 complete source MSP records, with their metadata unchanged. Values below retain every successful sample from this final review; timings are rounded for display. The additional LC pairs are included rather than replacing the initial slower observations.

<details>
<summary>Fresh GC measurements (milliseconds; memory in MiB)</summary>

| Version / round | Import | Three saves | Imported managed | Process peak | Old-project load | Old-project managed |
| --- | ---: | --- | ---: | ---: | ---: | ---: |
| upstream / 1 | 5545.251 | 1821.764, 1071.310, 1384.693 | 341.090 | 1044.379 | 5015.502 | 683.441 |
| upstream / 2 | 3574.169 | 1016.174, 658.071, 906.516 | 341.090 | 1046.520 | 3290.837 | 683.441 |
| upstream / 3 | 3728.970 | 1038.157, 653.213, 941.837 | 341.090 | 1047.184 | 3344.738 | 683.441 |
| current / 1 | 2696.859 | 506.629, 355.860, 406.907 | 70.056 | 302.473 | 2885.460 | 160.742 |
| current / 2 | 3109.692 | 481.991, 356.415, 362.042 | 70.056 | 282.148 | 2970.000 | 160.742 |
| current / 3 | 2712.697 | 461.422, 328.100, 326.735 | 70.056 | 279.570 | 3170.842 | 160.742 |

</details>

<details>
<summary>Real LC: all six pairs (milliseconds)</summary>

| Version / round | Storage load | First query | Sum of 60 first calls | Sum of 60 repeated calls |
| --- | ---: | ---: | ---: | ---: |
| upstream / 1 | 889.136 | 58.697 | 65.147 | 7.528 |
| upstream / 2 | 817.592 | 46.225 | 51.848 | 7.798 |
| upstream / 3 | 805.020 | 52.663 | 58.434 | 7.054 |
| upstream / 4 | 834.228 | 49.358 | 55.172 | 5.158 |
| upstream / 5 | 789.294 | 43.049 | 51.555 | 4.796 |
| upstream / 6 | 783.266 | 50.560 | 57.604 | 4.162 |
| current / 1 | 784.053 | 81.128 | 87.950 | 19.903 |
| current / 2 | 861.062 | 82.075 | 99.467 | 6.737 |
| current / 3 | 908.178 | 51.372 | 69.903 | 6.347 |
| current / 4 | 979.759 | 73.183 | 80.300 | 6.652 |
| current / 5 | 841.941 | 73.748 | 91.689 | 6.436 |
| current / 6 | 789.840 | 76.240 | 94.423 | 8.331 |

</details>

<details>
<summary>Constructed LC: all first and repeated calls (milliseconds)</summary>

| Candidates / version / round | First | Five repeated full searches |
| --- | ---: | --- |
| 5,001 / upstream / 1 | 120.308 | 46.119, 41.282, 37.535, 28.787, 31.149 |
| 5,001 / upstream / 2 | 107.612 | 34.677, 31.769, 30.310, 29.436, 36.449 |
| 5,001 / upstream / 3 | 124.544 | 43.149, 31.830, 30.999, 28.540, 32.335 |
| 5,001 / current / 1 | 127.396 | 32.864, 32.103, 31.584, 31.040, 29.644 |
| 5,001 / current / 2 | 120.456 | 33.820, 32.601, 30.178, 32.122, 35.036 |
| 5,001 / current / 3 | 134.177 | 50.319, 32.150, 33.506, 30.966, 39.540 |
| 50,000 / upstream / 1 | 415.925 | 392.133, 335.502, 323.060, 331.061, 337.040 |
| 50,000 / upstream / 2 | 411.712 | 349.728, 333.607, 317.618, 319.174, 474.839 |
| 50,000 / upstream / 3 | 397.571 | 360.379, 327.905, 330.815, 340.666, 363.089 |
| 50,000 / current / 1 | 377.322 | 325.332, 309.597, 422.161, 315.766, 369.235 |
| 50,000 / current / 2 | 398.002 | 428.496, 323.840, 310.879, 308.322, 356.141 |
| 50,000 / current / 3 | 389.713 | 335.181, 318.998, 312.844, 314.306, 367.604 |

</details>

Fresh cross-reader verification opens both newly saved 50,000-reference streams with both unchanged upstream and optimized readers. All four produce the same 50,000 complete records and canonical digest. This is a content check; the four individual load times are not a paired speed benchmark.

| Complete-content check | SHA-256 |
| --- | --- |
| 50,000 real GC references | `D6901EA5B8B0340A780BABC73CDD912D06FF21648B35CEFCEB9151D98652ED76` |
| Full GC library | `4916F3B04ECDD03918E57DD88461C3C80468D843C884F55F27B3B8D790679C2E` |
| Full GC ordered results | `C660212314A814707790125A799AD33F17EF81E3ED99ECC0BFA78C502E66F742` |
| Real LC ordered records | `5DF9FD579AA0A60F62C3F5EF982C9CA36479EE2466A22B02837B9753F5A52F9A` |
