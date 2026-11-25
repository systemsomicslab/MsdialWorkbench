# Source of these fixtures

Copied unchanged from the MS-DIAL spectrum catalog, `similarity-fixtures/`, owned by that repository:

- Repository: systemsomicslab/msdial_spectrum_catalog
- Commit: 99d3e1a503c87a25c50ee24a135c379aa0cd231d (origin/main, "Merge pull request #5 from systemsomicslab/feature/similarity-mn-kernels", 2026-11-13)
- Copied with `git show <commit>:similarity-fixtures/<file>`, so the bytes are the committed blobs.

Do not edit these files here. To update them, copy a newer commit the same way and record it above.
`SimilarityFixtureParityTests` checks every case file's sha256 against both `manifest.json` and this table.

| file | sha256 |
|---|---|
| manifest.json | 2cfecee74b152d45f0dce46514a3f68e37f36cad2ed364d7d3eafef4c03d1102 |
| kernel-ids.json | 9052c50afd17055f1ef341eb7181a44144100561f23002a22459d0325e5c1369 |
| prep.json | 5366fe1245915be6cf64caf45796df56b1c575f877a0309c17b732639e56ddd4 |
| kernels.json | f3d702c09b0c1e1cc68571f66ae06f7b90ed2fe393325171931824e52d4a10b0 |
| kernels-vs20.json | f4158b6f14ed8d7ae383bd291eac853736a1ba20fdc285acae784b6516ed4c70 |
| README.md | dec05d12a72a38529e24726d8eb92351fa63fde1254658c26580e8371331ae50 |

`kernels-vs20.json` holds normalised inputs from the public MS-DIAL library MSMS-Public_all-neg-VS20.msp
(doi:10.5281/zenodo.21904103, CC BY 4.0); see README.md for the attribution and selection rule.
