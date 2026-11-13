# Compact library benchmark

A bounded comparison of the shared MSDIAL5 library import, numeric scoring and
library persistence paths. It uses deterministic synthetic MSP data and the same
executable against either version's assemblies. It does not measure the GUI,
candidate retrieval, search caches, lipid structure validation or raw-data analysis.

The recorded comparison is in
[the performance report](../../../docs/design/compact-library-memory-performance-2026-11-13.md).

## Build both versions

Requires Windows, the .NET SDK, .NET Framework 4.8 and Python 3. Use separate clean
baseline and candidate directories. The recorded baseline is
`c7830d66e1dbbf9077858f4a4154413f48940400`; apply this PR to that baseline for the
candidate. Keep both versions' build configuration and package versions identical.

For each directory, run:

```powershell
dotnet build src/MSDIAL5/MsdialCore/MsdialCore.csproj -t:Rebuild `
  -c "Debug vendor unsupported" -f net48 -m:1 `
  /p:Optimize=true /p:UseSharedCompilation=false /p:WarningLevel=0
```

Build the harness once from the candidate directory, pointing it at the baseline's
`src/MSDIAL5/MsdialCore/bin/Debug vendor unsupported/net48` directory:

```powershell
dotnet build benchmark/CompMs/CompactLibraryBenchmark/CompactLibraryBenchmark.csproj `
  -c Release -m:1 /p:UseSharedCompilation=false `
  "-p:LibraryAssemblyDirectory=$baselineBins" -o $baselineHarness
Copy-Item "$baselineBins/*.dll" $baselineHarness
New-Item -ItemType Directory -Force $candidateHarness | Out-Null
Copy-Item "$baselineHarness/*" $candidateHarness
Copy-Item "$candidateBins/*.dll" $candidateHarness -Force
```

Set the four variables to absolute directories for your checkouts and harness
outputs. Copying all Core output DLLs keeps each process's dependencies together.
Both harness directories must contain the identical `CompactLibraryBenchmark.exe`.
Do not mix baseline and candidate Core/Common assemblies.

## Run

```powershell
python benchmark/CompMs/CompactLibraryBenchmark/run.py `
  --baseline "$baselineHarness/CompactLibraryBenchmark.exe" `
  --candidate "$candidateHarness/CompactLibraryBenchmark.exe" `
  --output C:/temp/compact-library-results --records 30000 --pairs 3
```

Each version runs in a fresh x64 process, serially. Orders alternate baseline /
candidate, candidate / baseline, baseline / candidate. The runner generates two
MSP libraries of 30,000 records and 64 peaks per record. Integer peaks fit exactly
in unsigned 16-bit values; fractional peaks retain double precision. These are
storage cases, not representative GC/LC acquisitions. All peaks have default
metadata; richer metadata uses the existing objects and is covered by tests.

The executable warms parsing, persistence and scoring with one record. Import,
save and load are timed once per process. Each scoring path runs three full
30,000-candidate passes; the report summarizes the median of each process's last
two passes, then takes the median across three processes. First-pass times remain
in the JSON. Forced collection is outside the timed operations.

`retainedManagedBytes` is the increase in `GC.GetTotalMemory(true)` after import.
The imported library is out of scope and collected before separately measuring
load and its retained memory. The process peak working set covers the entire
harness, including its extra verification save, and is not application peak RAM.

The runner requires identical input, saved archive and complete serialized EI/LC
result hashes across versions and runs. Each process also verifies byte-identical
load/save round trips. Reflection checks that all eligible candidate spectra stay
compact after import, scoring, save and load without invoking the public mutable
`Spectrum` getter. Logs and JSON remain in the output directory; large intermediate
archives are deleted after their hashes are recorded.

No private data or multi-gigabyte stress input is needed. Leave enough memory for
the baseline process (about 0.6 GiB peak working set in the recorded run). Timing
is sensitive to memory pressure, background load and filesystem caching; three
pairs are a local observation, not a statistical guarantee.
