# MS-DIAL - software for untargeted metabolomics using GC-MS, LC-MS, LC-MS/MS (data dependent/independent acquisition)

This is optimized for chromatography- and scan type mass spectrometry-based untargeted metabolomics, and is licensed under the CC-BY 4.0.
This program is managed by RIKEN and UC Davis teams. Please contact Hiroshi Tsugawa (hiroshi.tsugawa@riken.jp) for feedback, bug reports, and questions.

# Developers
Lead developer: Hiroshi Tsugawa
Main developers: Hiroshi Tsugawa (RIKEN), Ipputa Tada (SOKENDAI & NIG), Diego Pedrosa (UC Davis)
Main contributors: 

# Usage
See the tutorial page: http://prime.psc.riken.jp/Metabolomics_Software/MS-DIAL/index.html
And there is FAQ section as well under: http://prime.psc.riken.jp/Metabolomics_Software/MS-DIAL/index3.html
All of DLL files must be in the same folder as 'MSDIAL.exe'.

# About LBM file
The LBM (*.LBM) file contains the in silico MS/MS spectra of lipids (LipidBlast fork).
There are currently three files named with 'FiehnO (Oliver Fiehn laboratory)', 'AritaM (Makoto Arita laboratory)', and 'SaitoK (Kazuki Saito laboratory)'.
These files contain the same MS/MS spectra information but have different predicted retention times which were optimized for the indivisual method.
One of the '.LBM' files which contains lipid's in silico MS/MS (LipidBlast fork) should be also in the same folder as 'MSDIAL.exe' for Lipidomics project. 

# Further
For MRM (or SRM) data, MRMPROBS software is one of the options which was optimized for MRM based widely targeted metabolomics.
http://prime.psc.riken.jp/Metabolomics_Software/MRMPROBS/index.html

For structure elucidation for unknown metabolites, try to use MS-FINDER software where you can annotate unknown EI-MS or MS/MS peaks.
http://prime.psc.riken.jp/Metabolomics_Software/MS-FINDER/index.html

# License
This software uses third-party software.
A full list of their licenses can be found in the file THIRD-PARTY-LICENSE-README.txt.




