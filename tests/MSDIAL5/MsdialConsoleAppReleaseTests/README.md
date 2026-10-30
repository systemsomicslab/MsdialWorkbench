# MsdialConsoleAppReleaseTests

Release checks for the console application (`MSDIALCUI.exe`): each test processes demo data from one
vendor format and processing mode and fails unless the run finishes with peaks in every file.

## Running

The executable under test and the demo data are always passed explicitly:

```
dotnet test tests/MSDIAL5/MsdialConsoleAppReleaseTests -- "TestRunParameters.Parameter(name=\"ConsoleExe\", value=\"C:\path\to\MSDIALCUI.exe\")" "TestRunParameters.Parameter(name=\"DemoDataDir\", value=\"D:\msdial_demofiles\")"
```

| Parameter     | Meaning                                                            |
|---------------|--------------------------------------------------------------------|
| `ConsoleExe`  | The `MSDIALCUI.exe` under test, for example an unpacked release zip. |
| `DemoDataDir` | The demo data folder.                                              |
| `Cases`       | Optional. Comma-separated case names to run (`sciex_wiff2,gcms_netcdf`); the rest report Inconclusive. |

A test whose parameters are missing reports Inconclusive, so the solution-wide `dotnet test` in CI
skips these tests. The whole set takes about an hour; it does not use the mouse or keyboard.

## What a case does

1. Copies its raw files (and companions such as `.wiff.scan`) to
   `%TEMP%\MsdialConsoleReleaseTests\<case>-<time>\in`. The console writes intermediate files next to
   the raw data, so the demo data folder is never processed in place.
2. Writes the method file with the console's own `template <mode> --ionmode ...` and applies the
   entries the case lists. The built-in defaults are therefore tested as shipped.
3. Runs `<mode> -i in -o out -m method.txt -p` from the case's folder, not the executable's: users
   start the console from wherever their data is.
4. Checks the result:
   - exit code 0 within an hour, and no exception on standard error;
   - a peak list (`.mdpeak`, or `.mdscan` for GC-MS) with at least one peak for every input file;
   - with more than one input, an alignment result (`AlignResult-*.mdalign`) with at least one spot.

   A run can exit 0 and still find nothing, for one when the minimum peak height is above every
   peak in a file, which is why the peak lists are read.

The folder is deleted when the case passes and kept when it fails; the method file and the console's
output are attached to the test result.

## Cases

The list is `ProcessingTests.Cases`: Thermo, SCIEX (WIFF, WIFF2, EAD), Waters, Agilent, Shimadzu,
Bruker, mzML and Hive for LC-MS; Bruker timsTOF for LC-IM-MS, DIMS and IMMS; netCDF, Agilent
MassHunter, Shimadzu, Thermo GC-QE and ABF for GC-MS.

A case with a `KnownIssue` reports Inconclusive with the reason instead of running. Remove the
`KnownIssue` once the cause is fixed, so the case guards against it coming back.
