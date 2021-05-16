using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialCore.MSDec;
using System.Collections.ObjectModel;
using CompMs.MsdialCore.Algorithm.Annotation;

namespace CompMs.MsdialCore.Export
{
    public class MztabFormatExport
    {

        public static void MztabExporter(
            string outfile,
            IReadOnlyList<AlignmentSpotProperty> spots,
            IReadOnlyList<MSDecResult> msdecResults,
            IReadOnlyList<AnalysisFileBean> files,
            ParameterBase parameter,
            IQuantValueAccessor quantAccessor)
        {
            var meta = parameter;
            //var meta = metaFormatter.GetContent(spots[0], msdecResults[0]);

            var mzTabExporterVerNo = "1.10";
            var machineCategory = meta.MachineCategory.ToString();
            var exportFileName = outfile + ".mzTab.txt";
            var mztabId = Path.GetFileNameWithoutExtension(exportFileName); // as filename
            var exportType = outfile.Split('_')[0];//
            var ionAbundanceUnit = "";//!!!
            var ionMobilityType = "";//!!!

            //Meta data section 
            //common parameter
            var mtdPrefix = "MTD";
            var commentPrefix = "COM";
            var mztabVersion = "2.0.0-M";
            var mzTabExporterName = "MS-DIAL mzTab exporter";
            var mztabExporter = "[,, " + mzTabExporterName + ", " + mzTabExporterVerNo + "]";
            var software = "[MS, MS:1003082, MS-DIAL, " + meta.MsdialVersionNumber + "]";
            var quantificationMethod = "[MS, MS:1002019, Label-free raw feature quantitation, ]";

            var cvList = new List<List<string>>(); // cv list
            var cvItem1 = new List<string>() { "MS", "PSI-MS controlled vocabulary", "20-06-2018", "https://www.ebi.ac.uk/ols/ontologies/ms" };
            cvList.Add(cvItem1);
            var cvItem2 = new List<string>() { "UO", "Units of Measurement Ontology", "2017-09-25", "http://purl.obolibrary.org/obo/uo.owl" };
            if (machineCategory == "IMMS" && ionMobilityType != "TIMS")
            {
                cvList.Add(cvItem2);
            }
            if (meta.IsNormalizeSplash)
            {
                if (ionAbundanceUnit.Contains("pmol") || ionAbundanceUnit.Contains("fmol") || ionAbundanceUnit.Contains("pg") || ionAbundanceUnit.Contains("ng"))
                {
                    cvList.Add(cvItem2);
                }
            }

            var analysisFileExtention = Path.GetExtension(files[0].AnalysisFilePath).ToUpper();

            var msRunFormat = ""; // analysed file format
            var msRunIDFormat = ""; // analysed file Datapoint Number
            switch (analysisFileExtention)
            {
                case (".ABF"):
                    msRunFormat = "[,, ABF(Analysis Base File) file, ]";
                    msRunIDFormat = "[,, ABF file Datapoint Number, ]";
                    break;
                case (".IBF"):
                    msRunFormat = "[,, IBF file, ]";
                    msRunIDFormat = "[,, IBF file Datapoint Number, ]";
                    break;
                case (".WIFF"):
                case (".WIFF2"):
                    msRunFormat = "[MS, MS:1000562, ABI WIFF format, ]";
                    msRunIDFormat = "[MS, MS:1000770, WIFF nativeID format, ]";
                    break;
                case (".D"):
                    msRunFormat = "[MS, MS:1001509, Agilent MassHunter format, ]";
                    msRunIDFormat = "[MS, MS:1001508, Agilent MassHunter nativeID format, ]";
                    break;
                case (".CDF"):
                    msRunFormat = "[EDAM, format:3650, netCDF, ]";
                    msRunIDFormat = "[MS, MS:1000776, scan number only nativeID format, ]";
                    var cvItem3 = new List<string>() { "EDAM", "Bioscientific data analysis ontology", "20-06-2020", "http://edamontology.org/" };
                    cvList.Add(cvItem3);

                    break;
                case (".MZML"):
                    msRunFormat = "[MS, MS:1000584, mzML format, ]";
                    msRunIDFormat = "[MS, MS:1000776, scan number only nativeID format, ]";
                    break;
                    //case (".RAW"):
                    //    msRunFormat = "[MS, MS:1000563, Thermo RAW format, ]";
                    //    msRunIDFormat = "[MS, MS:1000768, Thermo nativeID format, ]";
                    //    msRunFormat = "[MS, MS:1000526, Waters raw format, ]";
                    //    msRunIDFormat = "[MS, MS:1000769, Waters nativeID format, ]";
                    //    break;
            };

            var database = new List<List<string>>();
            var defaultDatabase = new List<string>();
            var databaseItem = new List<string>();
            var libraryFileName = Path.GetFileNameWithoutExtension(meta.MspFilePath);
            var libraryFileExtension = Path.GetExtension(libraryFileName);
            if (libraryFileExtension == ".msp" || libraryFileExtension == ".MSP")
            {
                defaultDatabase = new List<string>() { "[,, User-defined MSP library file, ]", "MSP", "Unknown", "file://" + libraryFileName.Replace("\\", "/").Replace(" ", "%20") }; // 
            }
            else if (libraryFileExtension == ".lbm" || libraryFileExtension == ".LBM" || libraryFileExtension == ".lbm2" || libraryFileExtension == ".LBM2")
            {
                var lbmVer = libraryFileName;
                defaultDatabase = new List<string>() { "[,, MS-DIAL LipidsMsMs database, ]", "lbm", lbmVer, "file://" + libraryFileName.Replace("\\", "/").Replace(" ", "%20") }; // 
            }
            else
            {
                defaultDatabase = new List<string>() { "[,, Unknown database, null ]", "null", "Unknown", "null" }; // no database
            };
            database.Add(defaultDatabase);

            var unmatchedDBName = new List<string>() { "[,, no database, null ]", "null", "Unknown", "null" }; //  
            database.Add(unmatchedDBName);
            if (meta.TextDBFilePath != "" && meta.TextDBFilePath != null)
            {
                databaseItem = new List<string>() { "[,, User-defined rt-mz text library, ]", "USR", "Unknown", "file://" + meta.TextDBFilePath.Replace("\\", "/").Replace(" ", "%20") }; // post identification setting file
                database.Add(databaseItem);
            }

            var idConfidenceDefault = "[,, MS-DIAL algorithm matching score, ]";
            var idConfidenceMeasure = new List<string>();
            switch (machineCategory)//  must be fixed order!!
            {
                case "LCMS":
                case "IMS":
                case "IFMS":
                    idConfidenceMeasure.AddRange(new[]{
                    idConfidenceDefault, "[,, Retention time similarity, ]",
                        "[,, Dot product, ]", "[,, Reverse dot product, ]",
                        "[,, Fragment presence (%), ]"
                    });
                    break;
                case "IMMS":
                case "LCIMMS":
                    idConfidenceMeasure.AddRange(new[]{
                    idConfidenceDefault, "[,, Retention time similarity, ]", "[,, CCS similarity, ]",
                        "[,, Dot product, ]", "[,, Reverse dot product, ]",
                        "[,, Fragment presence (%), ]"
                    });
                    break;
                case "GCMS":
                    idConfidenceMeasure.AddRange(new[]{
                    idConfidenceDefault, "[,, Retention time similarity, ]",
                        "[,, Retention index similarity, ]", "[,, Total spectrum similarity, ]",
                        "[,, Dot product, ]", "[,, Reverse dot product, ]", "[,, Fragment presence (%), ]"
                    });
                    break;
            }
            var idConfidenceManual = "";
            // if manual curation is true
            var manuallyAnnotation = new List<bool>(spots.Select(spot => spot.IsManuallyModifiedForAnnotation));
            if (manuallyAnnotation.Contains(true))
            {
                idConfidenceManual = "[MS, MS:1001058, quality estimation by manual validation, ]";
                //idConfidenceMeasure.Add(idConfidenceManual); //if using manual cration score 
            }

            var smallMoleculeIdentificationReliability = "[MS, MS:1003032, compound identification confidence code in MS-DIAL, ]"; // new define on psi-ms.obo

            // write line section
            using (StreamWriter sw = new StreamWriter(exportFileName, false, Encoding.ASCII))
            {

                //sw.WriteLine("COM\tMeta data section");
                sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "mzTab-version", mztabVersion }));
                sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "mzTab-ID", mztabId }));
                sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "software[1]", software }));
                sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "software[2]", mztabExporter }));

                var msRunLocation = new List<string>(files.Select(file => file.AnalysisFilePath));
                for (int i = 0; i < files.Count; i++)
                {
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-location", "file://" + msRunLocation[i].Replace("\\", "/").Replace(" ", "%20") })); // filePath
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-format", msRunFormat }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-id_format", msRunIDFormat }));

                    var ionMode = meta.IonMode.ToString();
                    if (ionMode == "Positive")
                    {
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-scan_polarity[1]", "[MS,MS:1000130,positive scan,]" }));
                    }
                    else if (ionMode == "Negative")
                    {
                        sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-scan_polarity[1]", "[MS, MS:1000129, negative scan, ]" }));
                    }
                };
                var assay = new List<string>(files.Select(file => file.AnalysisFileName));
                for (int i = 0; i < assay.Count; i++)
                {
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "assay[" + (i + 1) + "]", assay[i] })); //fileName
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "assay[" + (i + 1) + "]-ms_run_ref", "ms_run[" + (i + 1) + "]" }));
                };

                var studyVariable = new List<string>(meta.ClassnameToOrder.Keys);  // Class ID
                var studyVariableAssayRef = new List<string>(files.Select(file => file.AnalysisFileClass));
                //var studyVariableDescription = "sample";

                for (int i = 0; i < studyVariable.Count; i++)
                {
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]", studyVariable[i] }));

                    var studyVariableAssayRefGroup = new List<string>();

                    for (int j = 0; j < files.Count; j++)
                    {
                        if (studyVariableAssayRef[j] == studyVariable[i])
                        {
                            studyVariableAssayRefGroup.Add("assay[" + (j + 1) + "] ");
                        };
                    };
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]-assay_refs", string.Join("| ", studyVariableAssayRefGroup) }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]-description", studyVariable[i] }));
                }

                for (int i = 0; i < cvList.Count; i++)
                {
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-label", cvList[i][0] }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-full_name", cvList[i][1] }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-version", cvList[i][2] }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-uri", cvList[i][3] }));
                }

                for (int i = 0; i < database.Count; i++)
                {
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]", database[i][0] }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]-prefix", database[i][1] }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]-version", database[i][2] }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]-uri", database[i][3] }));
                }

                var quantCvString = "";
                var normalizedComment = "";
                switch (exportType)
                {
                    case "Height":
                        quantCvString = "[,,precursor intensity (peak height), ]";
                        break;
                    case "Area":
                        quantCvString = "[,,XIC area,]";
                        break;
                    case "NormalizedArea":
                    case "NormalizedHeight":
                        quantCvString = "[,,Normalised Abundance, ]";
                        if (meta.IsNormalizeSplash)
                        {
                            if (exportType.Contains("Height"))
                            {
                                quantCvString = "[,,precursor intensity (peak height), ]";
                            }
                            else if (exportType.Contains("Area"))
                            {
                                quantCvString = "[,,XIC area,]";

                            }

                            normalizedComment = "Data is normalized by SPLASH internal standard";
                            switch (ionAbundanceUnit)
                            {
                                case "pmol":
                                    quantCvString = "[UO,UO:0000066,picomolar, ]";
                                    return;

                                case "fmol":
                                    quantCvString = "[UO,UO:0000073,femtomolar, ]";
                                    return;

                                case "pg":
                                    quantCvString = "[UO,UO:0000025,picogram, ]";
                                    return;

                                case "ng":
                                    quantCvString = "[UO,UO:0000024,nanogram, ]";
                                    return;

                                case "nmol_per_microL_plasma":
                                    quantCvString = "[,, nmol/microliter plasma, ]";
                                    return;

                                case "pmol_per_microL_plasma":
                                    quantCvString = "[,, pmol/microliter plasma, ]";
                                    return;

                                case "fmol_per_microL_plasma":
                                    quantCvString = "[,, fmol/microliter plasma, ]";
                                    return;

                                case "nmol_per_mg_tissue":
                                    quantCvString = "[,, nmol/mg tissue, ]";
                                    return;

                                case "pmol_per_mg_tissue":
                                    quantCvString = "[,, pmol/mg tissue, ]";
                                    return;

                                case "fmol_per_mg_tissue":
                                    quantCvString = "[,, fmol/mg tissue, ]";
                                    return;

                                case "nmol_per_10E6_cells":
                                    quantCvString = "[,, nmol/10^6 cells, ]";
                                    return;

                                case "pmol_per_10E6_cells":
                                    quantCvString = "[,, pmol/10^6 cells, ]";
                                    return;

                                case "fmol_per_10E6_cells":
                                    quantCvString = "[,, fmol/10^6 cells, ]";
                                    return;
                            }
                        }
                        if (meta.IsNormalizeIS)
                        {
                            normalizedComment = "Data is normalized by internal standerd SML ID(alighnment ID)";
                        }

                        break;
                }
                sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "small_molecule-quantification_unit", quantCvString }));
                sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "small_molecule_feature-quantification_unit", quantCvString }));
                if (normalizedComment != "")
                {
                    sw.WriteLine(String.Join("\t", new string[] { commentPrefix, normalizedComment }));
                }

                sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "small_molecule-identification_reliability", smallMoleculeIdentificationReliability }));

                for (int i = 0; i < idConfidenceMeasure.Count; i++)
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "id_confidence_measure[" + (i + 1) + "]", idConfidenceMeasure[i] }));

                sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "quantification_method", quantificationMethod }));

                if (machineCategory == "IMMS")
                {
                    var optMobilityUnit = "";
                    var optMobilityComment = "";
                    switch (ionMobilityType)
                    {
                        case "TIMS":
                            optMobilityUnit = "opt_global_Mobility=[,, 1/k0,]";
                            optMobilityComment = "Ion Mobility type = Trapped Ion Mobility Spectrometry";
                            break;
                        case "DTIMS":
                            optMobilityUnit = "opt_global_Mobility=[UO, UO: 0000028, millisecond,]";
                            optMobilityComment = "Ion Mobility type = Drift-Time Ion Mobility Spectrometry";
                            break;
                        case "TWIMS":
                            optMobilityUnit = "opt_global_Mobility=[UO, UO: 0000028, millisecond,]";
                            optMobilityComment = "Ion Mobility type = Travelling-Wave Ion Mobility Spectrometry";
                            break;
                    }

                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "colunit-small_molecule", optMobilityUnit }));
                    sw.WriteLine(String.Join("\t", new string[] { mtdPrefix, "colunit-small_molecule_feature", optMobilityUnit }));
                    sw.WriteLine(String.Join("\t", new string[] { commentPrefix, optMobilityComment }));
                }

                sw.WriteLine("");

                //SML section
                //Header
                var smlHeaders = new List<string>()
                {
                    "SMH","SML_ID","SMF_ID_REFS","database_identifier",
                    "chemical_formula","smiles","inchi",
                    "chemical_name","uri","theoretical_neutral_mass","adduct_ions",
                    "reliability","best_id_confidence_measure",
                    "best_id_confidence_value"
                 };
                for (int i = 0; i < smlHeaders.Count; i++) sw.Write(smlHeaders[i] + "\t");
                for (int i = 0; i < files.Count; i++) sw.Write("abundance_assay[" + (i + 1) + "]" + "\t");
                for (int i = 0; i < files.Count; i++) sw.Write("abundance_study_variable[" + (i + 1) + "]" + "\t");
                for (int i = 0; i < files.Count; i++) sw.Write("abundance_variation_study_variable[" + (i + 1) + "]" + "\t");

                // if want to add optional column, header discribe here 
                if (machineCategory == "IMMS" || machineCategory == "LCIMMS")
                {
                    sw.Write("opt_global_Mobility" + "\t" + "opt_global_CCS_values" + "\t");
                }
                if (meta.IsNormalizeIS || meta.IsNormalizeSplash)
                {
                    sw.Write("opt_global_internalStanderdSMLID" + "\t"
                           + "opt_global_internalStanderdMetaboliteName" + "\t"
                           );
                }
                //Header end
                sw.WriteLine("");
                //SML data
                var isNormalized = exportType.Contains("Normalized") ? true : false;

                foreach (var spot in spots)
                {
                    WriteMztabSMLData(sw, spot, meta, quantAccessor, database,idConfidenceDefault, idConfidenceManual);
                    sw.WriteLine("");
                }
                //SML section end
                sw.WriteLine("");
                sw.WriteLine("");
                //SMF section
                //header
                var smfHeaders = new List<string>() {
                "SFH","SMF_ID","SME_ID_REFS","SME_ID_REF_ambiguity_code","adduct_ion","isotopomer","exp_mass_to_charge",
                  "charge","retention_time_in_seconds","retention_time_in_seconds_start","retention_time_in_seconds_end"
                };
                for (int i = 0; i < smfHeaders.Count; i++) sw.Write(smfHeaders[i] + "\t");
                for (int i = 0; i < files.Count; i++) sw.Write("abundance_assay[" + (i + 1) + "]" + "\t");

                // add optional column, header discribe here 
                if (machineCategory == "IMMS" || machineCategory == "LCIMMS")
                {
                    sw.Write("opt_global_Mobility" + "\t" + "opt_global_CCS_values" + "\t");
                }
                // opt_global_internalStanderdSMLID, opt_global_internalStanderdMetaboliteName
                if (meta.IsNormalizeIS || meta.IsNormalizeSplash)
                {
                    sw.Write("opt_global_internalStanderdSMLID" + "\t"
                           + "opt_global_internalStanderdMetaboliteName" + "\t"
                           );
                }

                //
                sw.WriteLine("");
                //header end
                //SMF data
                foreach (var spot in spots)
                {
                    WriteMztabSMFData(sw, spot, meta);
                    sw.WriteLine("");
                }



            }
        }


        public static void WriteMztabSMLData(StreamWriter sw,
            AlignmentSpotProperty spot,
            ParameterBase param,
            IQuantValueAccessor quantAccessor,
            List<List<string>> database,
            string idConfidenceDefault, string idConfidenceManual)
        {
            var matchResult = spot.MatchResults.Representative;


            var inchi = "null";
            var smlPrefix = "SML";
            var smlID = spot.AlignmentID;
            var smfIDrefs = spot.AlignmentID;
            var databaseIdentifier = "null";
            var chemicalFormula = "null";
            var smiles = "null";
            var chemicalName = "null";
            var chemicalNameDB = "null";

            var uri = "null";
            var reliability = DataAccess.GetAnnotationCode(matchResult, param).ToString(); // as msiLevel
            var bestIdConfidenceMeasure = "null";
            var theoreticalNeutralMass = "null";

            //var mspDB = List<MspFormatCompoundInformationBean>;
            //var textDB = List<PostIdentificatioinReferenceBean>;

            var textLibId = spot.MatchResults.TextDbID;
            var mspLibraryID = spot.MatchResults.MspID;
            var refRtString = "null";
            var refMzString = "null";

            var adductIons = spot.AdductType.ToString();
            if (adductIons.Substring(adductIons.Length - 2, 1) == "]")
            {
                adductIons = adductIons.Substring(0, adductIons.IndexOf("]") + 1) + "1" + adductIons.Substring(adductIons.Length - 1, 1);
            }


            //if (textLibId >= 0 && textDB != null && textDB.Count != 0)
            //{
            //    var refRt = textDB[textLibId].RetentionTime;
            //    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
            //    refMzString = Math.Round(textDB[textLibId].AccurateMass, 5).ToString();
            //    if (textDB[textLibId].MetaboliteName != null)
            //    {
            //        chemicalName = spot.Name;
            //        chemicalNameDB = textDB[textLibId].MetaboliteName;
            //    }
            //    if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
            //        chemicalFormula = textDB[textLibId].Formula.FormulaString;
            //    if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
            //        smiles = textDB[textLibId].Smiles;
            //    databaseIdentifier = database[2][1] + ": " + chemicalNameDB;
            //    //reliability = getMsiLevel(alignedSpot, refRt, param).ToString();
            //    reliability = "annotated by user-defined text library";
            //    bestIdConfidenceMeasure = idConfidenceDefault;

            //}
            //else if (mspLibraryID >= 0 && mspDB != null && mspDB.Count != 0)
            //{
            //    var refRt = mspDB[mspLibraryID].RetentionTime;
            //    if (refRt > 0) refRtString = Math.Round(refRt, 3).ToString();
            //    refMzString = Math.Round(mspDB[mspLibraryID].PrecursorMz, 5).ToString();
            //    chemicalName = spot.Name;
            //    chemicalNameDB = mspDB[mspLibraryID].Name;
            //    chemicalFormula = mspDB[mspLibraryID].Formula;
            //    smiles = mspDB[mspLibraryID].Smiles;
            //    if (mspDB[mspLibraryID].CompoundClass != null && mspDB[mspLibraryID].CompoundClass != string.Empty && chemicalName.Contains("|"))
            //    {
            //        chemicalName = chemicalName.Split('|')[chemicalName.Split('|').Length - 1];
            //    }

            //    databaseIdentifier = database[0][1] + ": " + chemicalNameDB;
            //    // theoreticalNeutralMass = mspDB[mspID].PrecursorMz.ToString(); //// need neutral mass. null ok
            //    var matchResult = spot.MatchResults.Representative;

            //    reliability = DataAccess.GetAnnotationCode(matchResult, param).ToString();
            //    bestIdConfidenceMeasure = idConfidenceDefault;
            //}

            if (idConfidenceManual != "" && spot.IsManuallyModifiedForAnnotation == true)
            {
                bestIdConfidenceMeasure = idConfidenceManual;
            };

            var score = spot.MatchResults.Representative.TotalScore;
            var totalScore = score > 0 ? score > 1 ? "100" : Math.Round(score * 100, 1).ToString() : "null";

            var metadata = new List<string>() {
                smlPrefix,smlID.ToString(), smfIDrefs.ToString(), databaseIdentifier,
                chemicalFormula, smiles, inchi,chemicalName, uri ,theoreticalNeutralMass,
                adductIons, reliability.ToString(),bestIdConfidenceMeasure,totalScore,
            };
            var metadata2 = new List<string>();
            foreach (string item in metadata)
            {
                var metadataMember = item;
                if (metadataMember == "")
                {
                    metadataMember = "null";
                }
                metadata2.Add(metadataMember);
            }
            sw.Write(String.Join("\t", metadata2) + "\t");
            var quantValues = quantAccessor.GetQuantValues(spot);
            sw.Write(string.Join("\t", quantValues));

        }


        public static void WriteMztabSMFData(StreamWriter sw,
            AlignmentSpotProperty spot,
            ParameterBase param)
        {

        }

  
        //public static void WriteDataMztab(StreamWriter sw, ObservableCollection<AlignedPeakPropertyBean> peaks, string exportType, bool replaceZeroToHalf, float nonZeroMin)
        //{
        //    for (int i = 0; i < peaks.Count; i++)
        //    {
        //        var spotValue = GetSpotValue(peaks[i], exportType);

        //        //converting
        //        if (replaceZeroToHalf && (exportType == "Height" || exportType == "Normalized" || exportType == "Area"))
        //        {
        //            double doublevalue = 0.0;
        //            double.TryParse(spotValue, out doublevalue);
        //            if (doublevalue == 0)
        //                doublevalue = nonZeroMin * 0.1;
        //            spotValue = doublevalue.ToString();
        //        }

        //        if (i == peaks.Count - 1)
        //            sw.Write(spotValue);
        //        else
        //            sw.Write(spotValue + "\t");
        //    }
        //}


    }
}