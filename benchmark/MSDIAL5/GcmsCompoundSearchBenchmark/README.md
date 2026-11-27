# GcmsCompoundSearchBenchmark

Times the GC-MS compound search of the main window against a synthetic library of 10,000 and
50,000 references (24 peaks each, all inside the RI window of the query): `Uncached` scores every
reference, `Cached` repeats the search of the same spot, which the search cache answers.

It replaces a test that timed the same thing behind an environment variable. That the cached and
uncached searches return the same results is a test in `MsdialGuiAppTests`
(`GcmsMainCompoundSearchTests`).

```powershell
dotnet run -c "Release vendor unsupported" --project benchmark/MSDIAL5/GcmsCompoundSearchBenchmark -- --filter "*"
```

The benchmark runs in process (`[InProcess]`), so it uses the MsdialGuiApp the project was built
against.
