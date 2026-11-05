# MsdialGuiAppUiTests

UI tests that start a built `MSDIAL.exe`, drive it through UI Automation (FlaUI) and fail when the
application crashes. They exist to take over the "does it fall over somewhere odd" part of the
pre-release check.

## Running

The build under test and the data are always passed explicitly. Copy `ui.runsettings.example`,
edit the paths and pass the copy:

```
dotnet test tests/MSDIAL5/MsdialGuiAppUiTests --settings path/to/ui.runsettings
```

or give single values on the command line:

```
dotnet test tests/MSDIAL5/MsdialGuiAppUiTests -- "TestRunParameters.Parameter(name=\"MsdialExe\", value=\"C:\path\to\MSDIAL.exe\")"
```

| Parameter     | Meaning                                                    |
|---------------|------------------------------------------------------------|
| `MsdialExe`   | The `MSDIAL.exe` under test.                               |
| `DemoDataDir` | The demo data the console checks use (`LcmsScenarioTests`). |
| `DatasetsFile` | The JSON file listing the `LcmsScenarioTests` datasets. Keep it outside the repository. |
| `HandsOnDataDir` | The unpacked hands-on data (`ProjectMetadataTests`, `HandsOnPart1Tests`, `HandsOnPart2Tests`). |
| `Datasets`    | Optional. Comma-separated `LcmsScenarioTests` datasets to run; the others report Inconclusive. |

A test whose parameters are missing reports Inconclusive instead of failing, so the
solution-wide `dotnet test` in CI skips these tests.

The tests take over the mouse and keyboard while they run. Leave the machine alone, or run them in
a VM.

## What counts as a crash

MS-DIAL catches unhandled exceptions itself (`App.xaml.cs`), shows an "Unexpected exception
occured." dialog and keeps running, so the process staying alive proves nothing. A test fails when
any of these happens:

- that dialog appears,
- a new `MSDIAL-crash-*.log` is written to `%LOCALAPPDATA%\MSDIAL\crash` or `%TEMP%\MSDIAL\crash`,
- the process exits on its own,
- the process stops responding.

Scenario tests also fail when a dialog opens that the step did not expect (an error message such
as "No peak information", or a question nobody will answer), quoting its text.

On failure a screenshot of the desktop, a dump of the UI Automation tree and any new crash report
are attached to the test result.

## Tests

| Test | Needs | Takes | What it does |
|------|-------|-------|--------------|
| `StartupTests` | `MsdialExe` | seconds | Starts MS-DIAL and closes it. |
| `LcmsScenarioTests` | `MsdialExe`, `DemoDataDir`, `DatasetsFile` | 1–12 min per dataset | For each vendor dataset: creates an LC-MS project, processes it, opens every file and the alignment, exports the alignment result, closes, then loads the project again and opens the results once more. |
| `ProjectMetadataTests` | `MsdialExe`, `HandsOnDataDir` | ~7 min | Enters the project metadata (instrument type, instrument, authors, license, collision energy, comment) in the project wizard, checks it in Option > Project property setting, saves, reopens, edits and saves it there, checks that Cancel discards an edit, and checks the MassBank record export carries it. |
| `HandsOnPart1Tests` | `MsdialExe`, `HandsOnDataDir` | ~4 min | Part 1 of the hands-on guide: annotates `data1\pos` with the MassBank MSP and exports the molecular network. |
| `HandsOnPart2Tests` | `MsdialExe`, `HandsOnDataDir` | ~12 min | Part 2: lipidomics of `data2\neg` with a blank and sample classes, the EquiSPLASH text library, blank removal, SPLASH normalization and the raw and normalized exports, compared with the guide's expected result. Needs one lipid library (`.lbm*`) beside `MSDIAL.exe`. |

`HandsOnDataDir` is the unpacked `metabolomics_hands-on.zip` of the MS-DIAL metabolomics hands-on,
with its `data1.zip` and `data2.zip` unpacked inside it. The tests take the `.wiff` files they find
in `data1\pos` and `data2\neg`.

### Scenario datasets

`LcmsScenarioTests` runs the datasets listed in the JSON file `DatasetsFile` names, one test result
per dataset. The file stays outside the repository so the demo data's file names are not
published; `lcms-datasets.example.json` shows the format:

| Field | Meaning |
|-------|---------|
| `name` | The name in the test results and in the `Datasets` parameter. |
| `folder` | The folder holding the files, relative to `DemoDataDir` (or absolute). |
| `polarity` | `Positive` or `Negative`. |
| `minimumPeakHeight` | Set below the intensities in the data; one above every peak leaves a file empty and MS-DIAL stops with "No peak information". |
| `files` | The measurement files as a user picks them: a file, or for a run stored as a folder (Agilent .d, Waters .raw), that folder. |

To add a vendor or change a setting, edit that file; the code does not change. To run only some of
the datasets, name them in the `Datasets` parameter; the others report Inconclusive:

```
dotnet test tests/MSDIAL5/MsdialGuiAppUiTests --settings path/to/ui.runsettings -- "TestRunParameters.Parameter(name=\"Datasets\", value=\"Sciex WIFF,Agilent D\")"
```

The datasets are read when the tests run, not when they are discovered, so Test Explorer shows
`CreatesProcessesExportsAndReopensAProject` once and the results list each dataset.

The scenario copies the raw files (with companions such as `.wiff.scan`) to
`%TEMP%\MsdialUiTests\<dataset>-<time>` and processes them there, so the demo data folder is never
written to. The folder is deleted when the test passes and kept when it fails.

Runs that a vendor stores as a folder (Agilent .d, Waters .raw) cannot be picked in a file dialog;
the test opens File Explorer and drags them onto the file list, as a user does. Keep the mouse
still while it runs.

## Writing tests

- Find controls by `AutomationId`. A control with an `x:Name` already has one; otherwise add
  `AutomationProperties.AutomationId` in the XAML, named the same way (`Button_NewProject`).
- Type into text boxes with `Controls.Enter`, not by setting `Text`: the value reaches the view
  model only when the box loses focus.
- Wait with `MsdialSession.WaitFor`/`WaitForDialog`/`Settle`, never `Thread.Sleep`, so a crash in
  the meantime is noticed and blamed on the right step.
