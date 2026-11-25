# MsdialConsoleAppReleaseTests

Release checks for the console application (`MSDIALCUI.exe`): each test processes demo data from one
vendor format and processing mode and fails unless the run finishes with peaks in every file.

## Running

The executable under test, the demo data and the list of cases are always passed explicitly:

```
dotnet test tests/MSDIAL5/MsdialConsoleAppReleaseTests -- "TestRunParameters.Parameter(name=\"ConsoleExe\", value=\"C:\path\to\MSDIALCUI.exe\")" "TestRunParameters.Parameter(name=\"DemoDataDir\", value=\"D:\msdial_demofiles\")" "TestRunParameters.Parameter(name=\"CasesFile\", value=\"C:\path\to\console-cases.json\")"
```

| Parameter     | Meaning                                                            |
|---------------|--------------------------------------------------------------------|
| `ConsoleExe`  | The `MSDIALCUI.exe` under test, for example an unpacked release zip. |
| `DemoDataDir` | The demo data folder.                                              |
| `CasesFile`   | The JSON file listing the cases (see [Cases](#cases)). Keep it outside the repository. |
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

The cases are listed in the JSON file `CasesFile` names, one test result per case. The file stays
outside the repository so the demo data's file names are not published;
`console-cases.example.json` shows the format:

| Field | Meaning |
|-------|---------|
| `name` | The name in the test results and in the `Cases` parameter. |
| `mode` | The console command: `lcms`, `gcms`, `dims`, `imms` or `lcimms`. |
| `ionMode` | `Positive` or `Negative`. |
| `rawData` | The measurement files relative to `DemoDataDir`, as a user puts them in the input folder: a file, or for a run stored as a folder (Agilent and Bruker .d, Waters .raw), that folder. List companions such as a `.wiff.scan` too. |
| `method` | Optional. Method file entries that replace the template's or are added to it, by their names in the method file. `{DemoDataDir}` in a value is replaced with the demo data folder. |
| `riCompoundFile` | Optional. A retention index compound file relative to `DemoDataDir`; the run writes an RI dictionary that maps every input file to it, as GC-MS with RI needs. |
| `knownIssue` | Optional. Why the case is expected to fail. |

To add a case or change a setting, edit that file; the code does not change.

A case with a `knownIssue` reports Inconclusive with the reason instead of running. Remove the
`knownIssue` once the cause is fixed, so the case guards against it coming back.

The cases are read when the tests run, not when they are discovered, so Test Explorer shows
`ProcessesDemoData` once and the results list each case.
