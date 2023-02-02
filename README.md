# MsdialWorkbench contents

## MS-DIAL - software for untargeted metabolomics and lipidomics
The program supports data processings for any type of chromatography / scan type mass spectrometry data, and the assembly is licensed under the CC-BY 4.0.
Please contact Hiroshi Tsugawa (hiroshi.tsugawa@riken.jp) for feedback, bug reports, and questions.

## MS-FINDER - software for structure elucidation of unknown spectra with hydrogen rearrangement (HR) rules
The program supports molecular formula prediction, metabolie class prediction, and structure elucidation for EI-MS and MS/MS spectra, and the assembly is licensed under the CC-BY 4.0.
Please contact Hiroshi Tsugawa (hiroshi.tsugawa@riken.jp) for feedback, bug reports, and questions.

# How to build MS-DIAL5 Desktop Application (for Windows)

1. Download and install [Visual Studio Community 2022](https://visualstudio.microsoft.com/). (In the `Workloads` selection, choose `.NET desktop development`. )
2. Clone this repo.
3. Double click `MsdialWorkbench.sln` in the cloned repo.
4. Right-click on `MsdialWorkbench` in the Solution Explorer.
5. Click `Manage NuGet Packages for Solution...`.
6. Add the `Assemblies` folder in this repo to the **Package source:**.
7. Select `Debug vendor unsupported` from the `Solution Configurations` pull-down menu.
8. Select `MsdialGuiApp` from the `Startup Projects` pull-down menu.
9. Click `▶ MsdialGuiApp` button.

# Developers
Lead developer: Hiroshi Tsugawa (RIKEN) 
Current main developers: Hiroshi Tsugawa (RIKEN), Ipputa Tada (SOKENDAI), and Yuki Matsuzawa (RIKEN)
Past developers: Diego Pedrosa (UC Davis)

# Usage
See the tutorial page: https://mtbinfo-team.github.io/mtbinfo.github.io/

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




