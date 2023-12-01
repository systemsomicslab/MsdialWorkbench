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

## Coding conventions
- We use XXXXX for XXXXX.
- We avoid logic in views, putting XXXXX into XXXXX.
- We ALWAYS put spaces after XXXXXX items and method parameters(FILL_ME_THE_EXAMPLE), around operators (FILL_ME_THE_EXAMPLE), and around hash arrows.
- We indent using two spaces (soft tabs).
