# MsdialWorkbench contents

## MS-DIAL - software for untargeted metabolomics and lipidomics
The program supports data processings for any type of chromatography / scan type mass spectrometry data, and the assembly is licensed under the CC-BY 4.0.

## MS-FINDER - software for structure elucidation of unknown spectra with hydrogen rearrangement (HR) rules
The program supports molecular formula prediction, metabolie class prediction, and structure elucidation for EI-MS and MS/MS spectra, and the assembly is licensed under the CC-BY 4.0.

## From e-mail to GitHub (for open science)
If you would like to discuss with us (for feedback, bug reports, and questions),
we would appreciate it if you could do it on
https://github.com/systemsomicslab/MsdialWorkbench/issues
or
https://github.com/systemsomicslab/MsdialWorkbench/discussions
(instead of e-mail).

However, if the discussion is something that cannot be done openly by any means, please email msdial-jp-groups@go.tuat.ac.jp.

# How to build MS-DIAL5 Desktop Application (for Windows)

## Installing Visual Studio and cloning MsdialWorkbench source code
1. Download and install [Visual Studio Community 2022](https://visualstudio.microsoft.com/). (In the `Workloads` selection, choose `.NET desktop development`. )
2. Git clone this repo with `git clone https://github.com/mtbinfo-team/MsdialWorkbench`.

## Building MsdialWorkbench with Visual Studio
3. Double click `MsdialWorkbench.sln` in the cloned repo.
4. Right-click on `MsdialWorkbench` in the Solution Explorer.
5. Click `Manage NuGet Packages for Solution...`.
6. Add the `Assemblies` folder in this repo to the **Package source:**.
7. Select `Debug vendor unsupported` from the `Solution Configurations` pull-down menu.
8. Select `MsdialGuiApp` from the `Startup Projects` pull-down menu.
9. Click `▶ MsdialGuiApp` button on the right side of 8.

### Important Note
The 'Debug/Release vendor unsupported' version is a special configuration designed for the purpose of source code distribution.
Due to licensing restrictions, this version cannot read proprietary data formats from mass spectrometry manufacturers.
However, the [release versions](https://github.com/systemsomicslab/MsdialWorkbench/releases) distributed with official releases can read these proprietary formats.
For the 'Debug/Release vendor unsupported' version, only the following formats are supported: **abf**(Reifycs), **cdf**(NetCDF), and **mzml**.
If you convert your data into one of these formats, you can still analyze it using this configuration.
Other than the data reading capability, there are no differences between this configuration and the release versions.

# Developers
Lead developer: Hiroshi Tsugawa (TUAT/RIKEN) 

Current main developers: Hiroshi Tsugawa (TUAT/RIKEN), Mikiko Takahashi (RIKEN), Yuki Matsuzawa (TUAT) and Bujinlkham Buyantogtokh (TUAT)

Past developers: Diego Pedrosa (UC Davis), Ipputa Tada (SOKENDAI)

# Usage
See the tutorial page: https://systemsomicslab.github.io/mtbinfo.github.io/

# About LBM file in MS-DIAL project
The LBM (*.LBM2) file contains the in silico MS/MS spectra of lipids.
There are currently three files named with 'FiehnO (Oliver Fiehn laboratory)', 'AritaM (Makoto Arita laboratory)', and 'SaitoK (Kazuki Saito laboratory)'.
These files contain the same MS/MS spectra information but have different predicted retention times which were optimized for the indivisual method.
One of the '.LBM' files which contains lipid's in silico MS/MS should be also in the same folder as 'MSDIAL.exe' for Lipidomics project. 

# Further
MRMPROBS software suite is sutable for targeted metabolomics and lipidomics, and it also supports MRM/SRM data.
http://prime.psc.riken.jp/compms/mrmprobs/main.html


# Source code license
The source code is licensed under GNU LESSER GENERAL PUBLIC LICENSE (LGPL) version 3.
See LGPL.txt for full text of the license.
This software uses third-party software.
A full list of third-party software licenses in MsdialWorkbench is in the file THIRD-PARTY-LICENSE-README.txt.

