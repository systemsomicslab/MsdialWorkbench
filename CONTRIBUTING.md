# How to contribute to MS-DIAL / MS-FINDER

I'm really glad you're reading this, because we need volunteer developers to help this project come to fruition.

## Building the latest MS-DIAL

The best way to understand how to build this repository is by reading the build recipe in
[`.github/workflows/dotnet_pr.yml`](.github/workflows/dotnet_pr.yml) (pull requests) and
[`.github/workflows/dotnet_master.yml`](.github/workflows/dotnet_master.yml) (master).

Additionally, if you're more interested in learning how to build with Visual Studio and obtaining its environment rather than command line build reproducibility, please refer to our YouTube video https://www.youtube.com/watch?app=desktop&v=5eDAsISjj2I .

And it's important to note that our build artifacts come in two types: those that use the follwoing SDK from the MS vendors and those that do not.

- AB Sciex WiffReader and Data API Libraries
- Agilent MHDAC and MIDAC Libraries
- Bruker CXT, Baf2Sql, and TDF Development Kit Libraries
- Shimadzu DataReader and IoModule Libraries
- Thermo Scientific RawFileReader Library
- Waters MassLynxRaw Library

The code that uses the SDKs contains confidential information like license keys and cannot be made public, so it's not available for everyone to use.
The version we have made public does not utilize the MS vendor's SDK.
Please be aware that it only supports input in the mzML format for raw data.

Also, MS-DIAL primarily comes in two versions: version 4 series and version 5 series.
However, only the version 5 series of MS-DIAL has reproducible builds that can be guaranteed openly.

## Testing

The tests are MSTest projects under `tests/`. CI runs them in the `test` job of
[`dotnet_pr.yml`](.github/workflows/dotnet_pr.yml) and [`dotnet_master.yml`](.github/workflows/dotnet_master.yml):
one step for the whole solution and a second step, `Test LC-MS integration`, for the `Integration` tests.
Use `--configuration "Debug vendor unsupported"` (or `"Release vendor unsupported"`) as CI does.

```powershell
dotnet test --configuration "Debug vendor unsupported"
```

A `dotnet test` given no `--filter` leaves out the categories marked "no" below; the default filter
is set in the root `Directory.Build.props`. Any `--filter` replaces it, and `-p:RunAllTests=true`
runs everything. Visual Studio's Test Explorer does not read it and lists every test.

| Category | Marks | Plain `dotnet test` | Run only these |
|---|---|---|---|
| `UI` | Tests that put a real window on screen: WPF view tests, and tests that drive a built `MSDIAL.exe` (those need run parameters) | no | `dotnet test <project> --filter "TestCategory=UI"` |
| `ReleaseCheck` | Release checks that process demo data with a built `MSDIALCUI.exe` (need run parameters) | no | `dotnet test tests/MSDIAL5/MsdialConsoleAppReleaseTests --filter "TestCategory=ReleaseCheck" -- <run parameters>` |
| `Integration` | Generate mzML and run LC-MS processing end to end; slower | yes | `dotnet test tests/MSDIAL5/MsdialCoreTestAppTests --filter "TestCategory=Integration"` |
| `PinnedDefect` | Pin behaviour known to be wrong; they pass now and are meant to fail once it is fixed | yes | `dotnet test --filter "TestCategory=PinnedDefect"` |

A test that needs run parameters reports Inconclusive when they are not given. How to pass them,
and what each test covers, is in the project READMEs:

- [`tests/MSDIAL5/MsdialGuiAppTests/README.md`](tests/MSDIAL5/MsdialGuiAppTests/README.md) (`UI` view tests)
- [`tests/MSDIAL5/MsdialGuiAppUiTests/README.md`](tests/MSDIAL5/MsdialGuiAppUiTests/README.md) (`UI` tests of `MSDIAL.exe`)
- [`tests/MSDIAL5/MsdialConsoleAppReleaseTests/README.md`](tests/MSDIAL5/MsdialConsoleAppReleaseTests/README.md) (`ReleaseCheck`)
- [`tests/MSDIAL5/MsdialCoreTestAppTests/README.md`](tests/MSDIAL5/MsdialCoreTestAppTests/README.md) (`Integration`)

Give a new test one of these categories only when it fits one of them; a test that needs neither a
window, a built executable nor external data needs no category. Timing measurements belong under
`benchmark/`, not in a test. A new category that should be left out by default goes into the filter
in `Directory.Build.props` and into this table.

## Did you find a bug?

Feel free to create a GitHub issue on https://github.com/systemsomicslab/MsdialWorkbench/issues .

## Do you have questions about MS-DIAL / MS-FINDER ?

Feel free to create a GitHub discussion on https://github.com/systemsomicslab/MsdialWorkbench/discussions .

## Technical Topics
- In this project, we primarily utilize the frameworks of .NET Framework 4.7.2, .NET Core 3.1, and .NET 6. The .NET class libraries adhere at least to the specifications of .NET Standard 2.0 unless there is a specific reason to deviate. Currently, the main MsdialGuiApp project can be built using .NET Framework 4.7.2.
- This project can be coded in Visual Studio Code (VSCode), but the GUI part may have limitations in terms of preview functionality within VSCode. If you intend to contribute to the GUI part of the code and would like to have access to a preview, we recommend using Visual Studio.
- In this project, we are using the WPF (Windows Presentation Foundation) UI framework for GUI implementation. The GUI is designed for Windows exclusivity due to inherent limitations in WPF.
- This project primarily adopts the MVVM pattern, utilizing packages such as ReactiveExtensions and ReactiveProperty. However, MVVM frameworks like Prism or Livet are not used in this project. If you are interested in learning more about MVVM, please refer to Microsoft's official documentation at https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm.
