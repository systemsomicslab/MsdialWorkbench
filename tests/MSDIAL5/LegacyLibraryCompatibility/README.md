# Historical library reader check

This opt-in Windows/.NET Framework 4.8 integration check compiles the same source
against two independently built Core outputs. It tests actual historical readers;
it does not recreate their DTOs or load both MessagePack versions into one process.

Build the candidate and an unmodified upstream checkout at
`6b73c623` using `Debug vendor unsupported`, target `net48`. Keep the historical
checkout's dependency versions (including MessagePack **1.7.3.7**). Then run:

```powershell
python tests/MSDIAL5/LegacyLibraryCompatibility/run.py `
  --current-bins "$candidate/src/MSDIAL5/MsdialCore/bin/Debug vendor unsupported/net48" `
  --legacy-bins "$historical/src/MSDIAL5/MsdialCore/bin/Debug vendor unsupported/net48" `
  --output C:/temp/library-compatibility
```

The candidate writes synthetic `.dbs` and `.msp2` library data. Both current and
historical processes read them; the historical process saves them again, and the
current process reads that output. The harness checks every serialized reference
field and the exact mass/intensity double bits, including integer boundaries,
float-exact and double-only fractions, negative zero, infinities, NaN payloads,
10,000 deterministic random bit patterns, annotated peaks, and null/empty spectra.
Each process compares decoded records with a separate standard-encoded reference
using its own serializer. Logs include the loaded MessagePack assembly version.

This validates the shared project-library persistence routes, not an entire
released GUI project-opening workflow. Synthetic `.dbs` files contain one library
and no annotators. Annotator restoration and edited project libraries are covered
separately by `LegacyProjectLibraryTests` with a frozen upstream fixture. Historical
assemblies are not downloaded or redistributed by this test; this check is not
part of the ordinary `dotnet test` run or CI until those assemblies are provisioned.
