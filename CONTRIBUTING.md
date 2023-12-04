# How to contribute to MS-DIAL / MS-FINDER

I'm really glad you're reading this, because we need volunteer developers to help this project come to fruition.

## Building the latest MS-DIAL

The best way to understand how to build this repository is by reading the build recipe in 
https://github.com/systemsomicslab/MsdialWorkbench/blob/master/.github/workflows/dotnet_test.yml .

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

To conduct tests, please refer to section `test:` of GitHub Actions.
(https://github.com/systemsomicslab/MsdialWorkbench/blob/master/.github/workflows/dotnet_test.yml#L24)

Note that MS-DIAL is Windows desktop application primarily designed for GUI operations, so we apologize that the unit testing aspect for partial functionalities is not currently set up for trial.

## Did you find a bug?

Feel free to create a GitHub issue on https://github.com/systemsomicslab/MsdialWorkbench/issues .

## Do you have questions about MS-DIAL / MS-FINDER ?

Feel free to create a GitHub discussion on https://github.com/systemsomicslab/MsdialWorkbench/discussions .

## Technical Topics
- In this project, we primarily utilize the frameworks of .NET Framework 4.7.2, .NET Core 3.1, and .NET 6. The .NET class libraries adhere at least to the specifications of .NET Standard 2.0 unless there is a specific reason to deviate. Currently, the main MsdialGuiApp project can be built using .NET Framework 4.7.2.
- This project can be coded in Visual Studio Code (VSCode), but the GUI part may have limitations in terms of preview functionality within VSCode. If you intend to contribute to the GUI part of the code and would like to have access to a preview, we recommend using Visual Studio.
- In this project, we are using the WPF (Windows Presentation Foundation) UI framework for GUI implementation. The GUI is designed for Windows exclusivity due to inherent limitations in WPF.
- This project primarily adopts the MVVM pattern, utilizing packages such as ReactiveExtensions and ReactiveProperty. However, MVVM frameworks like Prism or Livet are not used in this project. If you are interested in learning more about MVVM, please refer to Microsoft's official documentation at https://learn.microsoft.com/en-us/dotnet/architecture/maui/mvvm.
