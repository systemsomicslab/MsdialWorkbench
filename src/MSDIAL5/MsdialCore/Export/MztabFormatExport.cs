﻿using System;
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
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.Common.Components;



namespace CompMs.MsdialCore.Export
{
    public class MztabFormatExport : BaseMetadataAccessor
    {
        public MztabFormatExport(IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer, ParameterBase parameter) : base(refer, parameter)
        {
        }

        public readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> refer;
        public readonly ParameterBase parameter;

        public void MztabFormatExporterCore(
            IReadOnlyList<AlignmentSpotProperty> spots,
            IReadOnlyList<MSDecResult> msdecResults,
            IReadOnlyList<AnalysisFileBean> files,
            IMetadataAccessor metadataAccessor,
            IQuantValueAccessor quantAccessor,
            string outfile)
        {
            var meta = parameter;

            var database = SetDatabaseList(meta); // database(library) list

            var exportFileName = outfile + ".mztab";
            var mztabId = Path.GetFileNameWithoutExtension(exportFileName); // as filename
            var exportType = Path.GetFileNameWithoutExtension(outfile).Split('_')[0];//

            var MtdTable = GenerateMtdMetaTableDataList(mztabId, meta, [.. spots], [.. files], database);

            //SML section
            var SmlTable = new List<string>() { string.Join("\t", GenerateSmlHeader(meta, [.. files])) };
            foreach (var spot in spots)
            {
                SmlTable.Add(SmlSpotLineData(spot, meta, quantAccessor));
            }
            //SML section end

            //SMF section
            //SMF header
            var smfHeaders = new List<string>() {
                "SFH","SMF_ID","SME_ID_REFS","SME_ID_REF_ambiguity_code","adduct_ion","isotopomer","exp_mass_to_charge",
                  "charge","retention_time_in_seconds","retention_time_in_seconds_start","retention_time_in_seconds_end"
                };
            for (int i = 0; i < files.Count; i++)
            {
                smfHeaders.Add("abundance_assay[" + (i + 1) + "]");
            }
            if (meta.MachineCategory == MachineCategory.IMMS || meta.MachineCategory == MachineCategory.LCIMMS)
            {
                smfHeaders.Add("opt_global_Mobility");
                smfHeaders.Add("opt_global_CCS_values");
            }
            if (meta.IsNormalizeIS || meta.IsNormalizeSplash)
            {
                smfHeaders.Add("opt_global_internalStanderdSMLID");
                smfHeaders.Add("opt_global_internalStanderdMetaboliteName");
            }
            //SMF header end
            //SMF data
            var smfTable = new List<string>() { string.Join("\t", smfHeaders) };
            foreach (var spot in spots)
            {
                smfTable.Add(SmfSpotLineData(spot, meta, quantAccessor));
            }
            //SMF data end
            //SMF section end

            //SME section
            //SME header
            var ms2Match = new List<bool>(spots.Select(n => n.IsMsmsAssigned));
            if (!ms2Match.Contains(true)) { return; }



            using (StreamWriter sw = new StreamWriter(exportFileName, false, Encoding.ASCII))
            {
                var idConfidenceManual = "null";
                var idConfidenceMeasure = new List<string>(); //  must be fixed order!!
                var idConfidenceDefault = "null";

                ;
                var headersSME01 = new List<string>() {
                    "SEH","SME_ID","evidence_input_id","database_identifier","chemical_formula","smiles","inchi",
                      "chemical_name","uri","derivatized_form","adduct_ion","exp_mass_to_charge","charge", "theoretical_mass_to_charge",
                      "spectra_ref","identification_method","ms_level"
                    };
                for (int i = 0; i < headersSME01.Count; i++) sw.Write(headersSME01[i] + "\t");
                for (int i = 0; i < idConfidenceMeasure.Count; i++) sw.Write("id_confidence_measure[" + (i + 1) + "]" + "\t");
                sw.Write("rank");
                sw.WriteLine("");
                //SME header end
                //SME data
                foreach (var spot in spots)
                {
                    WriteMztabSMEData(sw,
                                 spot,
                                 //AlignedDriftSpotPropertyBean driftSpot, 
                                 meta,
                                 database,
                                 files,
                                 idConfidenceDefault,
                                 idConfidenceManual
                                //List<MspFormatCompoundInformationBean> mspDB,
                                //List<PostIdentificatioinReferenceBean> textDB,
                                );
                    sw.WriteLine("");
                }
                //SMF section end
                sw.WriteLine("");
            }
        }

        public static List<string> GenerateMtdMetaTableDataList(
            string mzTabId,
            ParameterBase meta,
            List<AlignmentSpotProperty> spots,
            List<AnalysisFileBean> files,
            List<List<string>> database
            )
        {
            //Meta data section 
            var ionAbundanceUnits = spots.Select(spot => spot.IonAbundanceUnit).Distinct().ToList();
            var ionMobilityType = "";

            //switch (meta)
            //{
            //    case MsdialLcImMsParameter lcimmsParameter:
            //        ionMobilityType = lcimmsParameter.IonMobilityType.ToString();
            //        break;
            //    case MsdialImmsParameter immsParameter:
            //        ionMobilityType = immsParameter.IonMobilityType.ToString();
            //        break;
            //}

            //common parameter
            var mtdPrefix = "MTD";
            var commentPrefix = "COM";
            var mztabVersion = "2.0.0-M";
            var mztabExporter = "[MS, MS:1003082, MS-DIAL, " + meta.MsdialVersionNumber + "]";
            var software = "[MS, MS:1003082, MS-DIAL, " + meta.MsdialVersionNumber + "]";
            var quantificationMethod = "[MS, MS:1002019, Label-free raw feature quantitation, ]";

            var cvList = new List<List<string>>(); // cv list
            var cvItem1 = new List<string>() { "MS", "PSI-MS controlled vocabulary", "4.1.192", "https://www.ebi.ac.uk/ols/ontologies/ms" };
            cvList.Add(cvItem1);
            var cvItem2 = new List<string>() { "UO", "Units of Measurement Ontology", "2023-05-25", "http://purl.obolibrary.org/obo/uo.owl" };
            if (meta.MachineCategory == MachineCategory.IMMS && ionMobilityType != "TIMS")
            {
                cvList.Add(cvItem2);
            }
            if (meta.IsNormalizeSplash)
            {
                if (ionAbundanceUnits.Contains(IonAbundanceUnit.pmol)
                    || ionAbundanceUnits.Contains(IonAbundanceUnit.fmol)
                    || ionAbundanceUnits.Contains(IonAbundanceUnit.pg)
                    || ionAbundanceUnits.Contains(IonAbundanceUnit.ng)
                    )
                {
                    cvList.Add(cvItem2);
                }
            }

            var analysisFilePath = files[0].AnalysisFilePath;
            var analysisFileExtention = Path.GetExtension(analysisFilePath).ToUpper();

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
                    cvList.Add(["EDAM", "Bioscientific data analysis ontology", "20-06-2020", "http://edamontology.org/"]);
                    break;
                case (".MZML"):
                    msRunFormat = "[MS, MS:1000584, mzML format, ]";
                    msRunIDFormat = "[MS, MS:1000776, scan number only nativeID format, ]";
                    break;
                case (".RAW"):
                    var isDirectory = System.IO.File.GetAttributes(analysisFilePath).HasFlag(FileAttributes.Directory);
                    if (isDirectory)
                    {
                        msRunFormat = "[MS, MS:1000526, Waters raw format, ]";
                        msRunIDFormat = "[MS, MS:1000769, Waters nativeID format, ]";
                    }
                    else
                    {
                        msRunFormat = "[MS, MS:1000563, Thermo RAW format, ]";
                        msRunIDFormat = "[MS, MS:1000768, Thermo nativeID format, ]";
                    }
                    break;
                case (".LRP"):
                    msRunFormat = "[,, LRP file, ]";
                    msRunIDFormat = "[,, LRP file Datapoint Number, ]";
                    break;
                case (".HMD"):
                    msRunFormat = "[,, Hive HMD file, ]";
                    msRunIDFormat = "[,, Hive HMD file Datapoint Number, ]";
                    break;
                case (".MZB"):
                    msRunFormat = "[,, Hive mzB file, ]";
                    msRunIDFormat = "[,, Hive mzB file Datapoint Number, ]";
                    break;
                case (".LCD"):
                    msRunFormat = "[MS, MS:1003009, Shimadzu Biotech LCD format, ]";
                    msRunIDFormat = "[MS, MS:1000929, Shimadzu Biotech nativeID format, ]";
                    break;
                case (".QGD"):
                    msRunFormat = "[,, Shimadzu GC/MS format, ]";
                    msRunIDFormat = "[,, Shimadzu GC/MS format file Datapoint Number, ]";
                    break;
            }
            ;


            var idConfidenceDefault = "[,, MS-DIAL algorithm matching score, ]";
            var idConfidenceMeasure = new List<string>(); //  must be fixed order!!
            switch (meta.MachineCategory)
            {
                case MachineCategory.LCMS:
                    idConfidenceMeasure.AddRange(new[]{
                        idConfidenceDefault,
                        "[,, Retention time similarity, ]",
                        "[,, Dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Fragment presence (%), ]"
                    });
                    break;
                case MachineCategory.IFMS:
                    idConfidenceMeasure.AddRange(new[]{
                    idConfidenceDefault,
                        "[,, Simple dot product, ]",
                        "[,, Weighted dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Matched peaks count, ]",
                        "[,, Matched peaks percentage, ]"
                    });
                    break;
                case MachineCategory.IMMS:
                    idConfidenceMeasure.AddRange(new[]{
                    idConfidenceDefault,
                        "[,, CCS similarity, ]",
                        "[,, Dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Fragment presence (%), ]"
                    });
                    break;
                case MachineCategory.LCIMMS:
                    idConfidenceMeasure.AddRange(new[]{
                    idConfidenceDefault,
                        "[,, Retention time similarity, ]",
                        "[,, CCS similarity, ]",
                        "[,, Dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Fragment presence (%), ]"
                    });
                    break;
                case MachineCategory.GCMS:
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

            // add data section
            var mtdTable = new List<string>();

            //mtdTable.Add("COM\tMeta data section");
            mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "mzTab-version", mztabVersion }));
            mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "mzTab-ID", mzTabId }));
            mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "software[1]", software }));
            mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "software[2]", mztabExporter }));

            var msRunLocation = new List<string>(files.Select(file => file.AnalysisFilePath));
            for (int i = 0; i < files.Count; i++)
            {
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-location", "file://" + msRunLocation[i].Replace("\\", "/").Replace(" ", "%20") })); // filePath
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-format", msRunFormat }));
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-id_format", msRunIDFormat }));

                var ionMode = meta.IonMode.ToString();
                if (ionMode == "Positive")
                {
                    mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-scan_polarity[1]", "[MS,MS:1000130,positive scan,]" }));
                }
                else if (ionMode == "Negative")
                {
                    mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "ms_run[" + (i + 1) + "]-scan_polarity[1]", "[MS, MS:1000129, negative scan, ]" }));
                }
            }
                ;
            var assay = new List<string>(files.Select(file => file.AnalysisFileName));
            for (int i = 0; i < assay.Count; i++)
            {
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "assay[" + (i + 1) + "]", assay[i] })); //fileName
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "assay[" + (i + 1) + "]-ms_run_ref", "ms_run[" + (i + 1) + "]" }));
            }
                ;

            var studyVariable = new List<string>(meta.ClassnameToOrder.Keys);  // Class ID
            var studyVariableAssayRef = new List<string>(files.Select(file => file.AnalysisFileClass));
            //var studyVariableDescription = "sample";

            for (int i = 0; i < studyVariable.Count; i++)
            {
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]", studyVariable[i] }));

                var studyVariableAssayRefGroup = new List<string>();

                for (int j = 0; j < files.Count; j++)
                {
                    if (studyVariableAssayRef[j] == studyVariable[i])
                    {
                        studyVariableAssayRefGroup.Add("assay[" + (j + 1) + "] ");
                    }
                }
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]-assay_refs", string.Join("| ", studyVariableAssayRefGroup) }));
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "study_variable[" + (i + 1) + "]-description", studyVariable[i] }));
            }

            for (int i = 0; i < cvList.Count; i++)
            {
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-label", cvList[i][0] }));
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-full_name", cvList[i][1] }));
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-version", cvList[i][2] }));
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "cv[" + (i + 1) + "]-uri", cvList[i][3] }));
            }

            for (int i = 0; i < database.Count; i++)
            {
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]", database[i][0] }));
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]-prefix", database[i][1] }));
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]-version", database[i][2] }));
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "database[" + (i + 1) + "]-uri", database[i][3] }));
            }

            var normalizedCommentList = new List<string>();
            var quantCvStringList = new List<string>();

            var exportType = meta.DataExportParam;
            if (exportType.IsHeightMatrixExport)
            {
                quantCvStringList.Add("[,,precursor intensity (peak height), ]");
            }
            if (exportType.IsPeakAreaMatrixExport)
            {
                quantCvStringList.Add("[,,XIC area,]");
            }
            if (exportType.IsNormalizedMatrixExport)
            {
                quantCvStringList.Add("[,,Normalised Abundance, ]");
            }

            if (meta.IsNormalizeSplash)
            {
                foreach (var ionAbundanceUnit in ionAbundanceUnits)
                {
                    switch (ionAbundanceUnit)
                    {
                        case IonAbundanceUnit.pmol:
                            quantCvStringList.Add("[UO,UO:0000066,picomolar, ]");
                            break;
                        case IonAbundanceUnit.fmol:
                            quantCvStringList.Add("[UO,UO:0000073,femtomolar, ]");
                            break;

                        case IonAbundanceUnit.pg:
                            quantCvStringList.Add("[UO,UO:0000025,picogram, ]");
                            break;

                        case IonAbundanceUnit.ng:
                            quantCvStringList.Add("[UO,UO:0000024,nanogram, ]");
                            break;

                        case IonAbundanceUnit.nmol_per_microL_plasma:
                            quantCvStringList.Add("[,, nmol/microliter plasma, ]");
                            break;

                        case IonAbundanceUnit.pmol_per_microL_plasma:
                            quantCvStringList.Add("[,, pmol/microliter plasma, ]");
                            break;

                        case IonAbundanceUnit.fmol_per_microL_plasma:
                            quantCvStringList.Add("[,, fmol/microliter plasma, ]");
                            break;

                        case IonAbundanceUnit.nmol_per_mg_tissue:
                            quantCvStringList.Add("[,, nmol/mg tissue, ]");
                            break;

                        case IonAbundanceUnit.pmol_per_mg_tissue:
                            quantCvStringList.Add("[,, pmol/mg tissue, ]");
                            break;

                        case IonAbundanceUnit.fmol_per_mg_tissue:
                            quantCvStringList.Add("[,, fmol/mg tissue, ]");
                            break;
                        case IonAbundanceUnit.nmol_per_10E6_cells:
                            quantCvStringList.Add("[,, nmol/10^6 cells, ]");
                            break;

                        case IonAbundanceUnit.pmol_per_10E6_cells:
                            quantCvStringList.Add("[,, pmol/10^6 cells, ]");
                            break;

                        case IonAbundanceUnit.fmol_per_10E6_cells:
                            quantCvStringList.Add("[,, fmol/10^6 cells, ]");
                            break;
                    }
                }
            }
            if (meta.IsNormalizeIS)
            {
                normalizedCommentList.Add("Data is normalized by internal standerd SML ID(alighnment ID)");
            }
            else if (meta.IsNormalizeLowess)
            {
                normalizedCommentList.Add("Data is normalized by LOWESS method");
            }
            else if (meta.IsNormalizeIsLowess)
            {
                normalizedCommentList.Add("Data is normalized by internal standard peak area with LOWESS method");
            }
            else if (meta.IsNormalizeTic)
            {
                normalizedCommentList.Add("Data is normalized by TIC");
            }
            else if (meta.IsNormalizeMTic)
            {
                normalizedCommentList.Add("Data is normalized by MTIC");
            }
            foreach (var ionAbundanceUnit in ionAbundanceUnits)
            {
                switch (ionAbundanceUnit)
                {
                    case IonAbundanceUnit.NormalizedByInternalStandardPeakHeight:
                        quantCvStringList.Add("[,, Peak intensity/IS peak, ]");
                        break;
                    case IonAbundanceUnit.NormalizedByQcPeakHeight:
                        quantCvStringList.Add("[,, Peak intensity/QC peak, ]");
                        break;
                    case IonAbundanceUnit.NormalizedByMaxPeakOnTIC:
                        quantCvStringList.Add("[,, Peak intensity/TIC, ]");
                        break;
                    case IonAbundanceUnit.NormalizedByMaxPeakOnNamedPeaks:
                        quantCvStringList.Add("[,, Peak intensity/MTIC, ]");
                        break;
                }
            }

            foreach (var quantCvString in quantCvStringList)
            {
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "small_molecule-quantification_unit", quantCvString }));
            }
            //mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "small_molecule_feature-quantification_unit", quantCvString }));
            if (normalizedCommentList.Count > 0)
            {
                foreach (var normalizedComment in normalizedCommentList)
                {
                    mtdTable.Add(String.Join("\t", new string[] { commentPrefix, normalizedComment }));
                }
            }

            mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "small_molecule-identification_reliability", smallMoleculeIdentificationReliability }));

            for (int i = 0; i < idConfidenceMeasure.Count; i++)
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "id_confidence_measure[" + (i + 1) + "]", idConfidenceMeasure[i] }));

            mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "quantification_method", quantificationMethod }));

            if (meta.MachineCategory == MachineCategory.IMMS)
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

                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "colunit-small_molecule", optMobilityUnit }));
                mtdTable.Add(String.Join("\t", new string[] { mtdPrefix, "colunit-small_molecule_feature", optMobilityUnit }));
                mtdTable.Add(String.Join("\t", new string[] { commentPrefix, optMobilityComment }));
            }
            return mtdTable;
        }


        public static List<string> GenerateSmlHeader(ParameterBase meta, List<AnalysisFileBean> files)
        {
            var SmlHeader = new List<string>()
                {
                    "SMH","SML_ID","SMF_ID_REFS","database_identifier",
                    "chemical_formula","smiles","inchi",
                    "chemical_name","uri","theoretical_neutral_mass","adduct_ions",
                    "reliability","best_id_confidence_measure",
                    "best_id_confidence_value"
                 };
            for (int i = 0; i < files.Count; i++) SmlHeader.Add("abundance_assay[" + (i + 1) + "]" + "\t");
            for (int i = 0; i < files.Count; i++) SmlHeader.Add("abundance_study_variable[" + (i + 1) + "]" + "\t");
            for (int i = 0; i < files.Count; i++) SmlHeader.Add("abundance_variation_study_variable[" + (i + 1) + "]" + "\t");

            // if want to add optional column, header discribe here 
            if (meta.MachineCategory == MachineCategory.IMMS || meta.MachineCategory == MachineCategory.LCIMMS)
            {
                SmlHeader.Add("opt_global_Mobility" + "\t" + "opt_global_CCS_values" + "\t");
            }
            if (meta.IsNormalizeIS || meta.IsNormalizeSplash)
            {
                SmlHeader.Add("opt_global_internalStanderdSMLID" + "\t"
                       + "opt_global_internalStanderdMetaboliteName" + "\t"
                       );
            }
            return SmlHeader;
        }

        public static string SmlSpotLineData(
            AlignmentSpotProperty spot,
            ParameterBase meta,
            IQuantValueAccessor quantAccessor,
            List<List<string>> database = null
            )
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

            var idConfidenceDefault = "[,, MS-DIAL algorithm matching score, ]";
            var reliability = DataAccess.GetAnnotationCode(matchResult, meta).ToString(); // as msiLevel
            var bestIdConfidenceMeasure = "null";
            var idConfidenceManual = "null";
            var theoreticalNeutralMass = "null";

            var textLibId = spot.MatchResults.TextDbID;
            var mspLibraryID = spot.MatchResults.MspID;
            var refRtString = "null";
            var refMzString = "null";

            var adductIons = spot.AdductType.ToString();
            if (adductIons.Substring(adductIons.Length - 2, 1) == "]")
            {
                adductIons = adductIons.Substring(0, adductIons.IndexOf("]") + 1) + "1" + adductIons.Substring(adductIons.Length - 1, 1);
            }

            if (textLibId >= 0 && meta.TextDBFilePath != "")
            {
                //if (textDB[textLibId].MetaboliteName != null)
                //{
                //    chemicalName = spot.Name;
                //    chemicalNameDB = textDB[textLibId].MetaboliteName;
                //}
                //if (textDB[textLibId].Formula != null && textDB[textLibId].Formula.FormulaString != null && textDB[textLibId].Formula.FormulaString != string.Empty)
                //    chemicalFormula = textDB[textLibId].Formula.FormulaString;
                //if (textDB[textLibId].Smiles != null && textDB[textLibId].Smiles != string.Empty)
                //    smiles = textDB[textLibId].Smiles;
                //databaseIdentifier = database[2][1] + ": " + chemicalNameDB;
                //reliability = getMsiLevel(alignedSpot, refRt, param).ToString();
                reliability = "annotated by user-defined text library";
                bestIdConfidenceMeasure = idConfidenceDefault;
            }
            else if (meta.MspFilePath != "" && spot.Name == "")
            {
                chemicalName = spot.Name;
                chemicalNameDB = spot.Name;
                chemicalFormula = spot.Formula.ToString();
                smiles = spot.SMILES;
                //if (spot.Name.Contains("|"))
                //{
                //    chemicalNameDB = chemicalNameDB.Split('|')[chemicalNameDB.Split('|').Length - 1];
                //}


                databaseIdentifier = database[0][1] + ": " + chemicalNameDB;
                theoreticalNeutralMass = Math.Round(spot.Formula.Mass, 4).ToString(); //// need neutral mass. null ok

                reliability = DataAccess.GetAnnotationCode(matchResult, meta).ToString();
                bestIdConfidenceMeasure = idConfidenceDefault;
            }

            if (idConfidenceManual != "" && spot.IsManuallyModifiedForAnnotation == true)
            {
                bestIdConfidenceMeasure = idConfidenceManual;
            }

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
            var quantValues = quantAccessor.GetQuantValues(spot);
            return string.Join("\t", metadata2) + "\t" + string.Join("\t", quantValues);

        }

        public static string SmfSpotLineData(
            AlignmentSpotProperty spot,
            ParameterBase param,
            IQuantValueAccessor quantAccessor)
        {
            var matchResult = spot.MatchResults.Representative;

            var id = -1;
            var smfPrefix = "SMF";
            var smeIDrefs = "null";
            var smfID = spot.AlignmentID;

            var smeIDrefAmbiguity_code = "null";
            var isotopomer = "null";
            var isManuallyModified = "false";
            var expMassToCharge = spot.MassCenter.ToString();

            var retentionTime = "null";
            var retentionTimeStart = "null";
            var retentionTimeEnd = "null";
            if (spot.TimesCenter.RT.Value > 0.0)
            {
                retentionTime = spot.TimesCenter.RT.Value.ToString();
                retentionTimeStart = spot.TimesMin.RT.Value.ToString();
                retentionTimeEnd = spot.TimesMax.RT.Value.ToString();
            }

            //var charge = alignedSpot.ChargeNumber.ToString();
            var charge = "1";

            var adductIons = spot.AdductType.ToString();
            if (adductIons.Substring(adductIons.Length - 2, 1) == "]")
            {
                adductIons = adductIons.Substring(0, adductIons.IndexOf("]") + 1) + "1" + adductIons.Substring(adductIons.Length - 1, 1);
            }

            //var charge = alignedSpot.ChargeNumber.ToString();

            if (param.IonMode == IonMode.Negative)
            {
                charge = "-" + charge.ToString();
            }

            if (spot.IsMsmsAssigned)
            {
                //smeIDrefs = spot.AlignedDriftSpots[0].MasterID.ToString();
                smeIDrefs = smfID.ToString();
            }

            var metadata2 = new List<string>();

            var metadata = new List<string>() {
                        smfPrefix,smfID.ToString(), smeIDrefs.ToString(), smeIDrefAmbiguity_code,
                            adductIons, isotopomer, expMassToCharge, charge , retentionTime.ToString(),retentionTimeStart.ToString(),retentionTimeEnd.ToString()
                        };
            foreach (string item in metadata)
            {
                var metadataMember = item;
                if (metadataMember == "")
                {
                    metadataMember = "null";
                }
                metadata2.Add(metadataMember);
            }
            //sw.Write(String.Join("\t", metadata2) + "\t");
            //var quantValues = quantAccessor.GetQuantValues(spot);
            //sw.Write(string.Join("\t", quantValues));

            return string.Join("\t", metadata2) + "\t" + string.Join("\t", quantAccessor.GetQuantValues(spot));
        }

        public void WriteMztabSMEData(StreamWriter sw,
            AlignmentSpotProperty Spot,
            //AlignedDriftSpotPropertyBean driftSpot, 
            ParameterBase param,
            List<List<string>> database,
            IReadOnlyList<AnalysisFileBean> files,
            string idConfidenceDefault,
            string idConfidenceManual
            //List<MspFormatCompoundInformationBean> mspDB,
            //List<PostIdentificatioinReferenceBean> textDB,
            )
        {


        }

        private static List<List<string>> SetDatabaseList(ParameterBase meta)
        {
            var database = new List<List<string>>();
            var defaultDatabase = new List<string>();

            if (meta.MspFilePath != "" && meta.MspFilePath != null)
            {
                database.Add(new List<string>() { "[,, User-defined MSP library file, ]", "MSP", Path.GetFileName(meta.MspFilePath), "file://" + meta.MspFilePath.Replace("\\", "/").Replace(" ", "%20") }); // 
            }
            if (meta.LbmFilePath != "" && meta.LbmFilePath != null)
            {
                database.Add(new List<string>() { "[,, MS-DIAL LipidsMsMs database, ]", "lbm", Path.GetFileName(meta.LbmFilePath), "file://" + meta.LbmFilePath.Replace("\\", "/").Replace(" ", "%20") }); // 
            }
            if (meta.TextDBFilePath != "" && meta.TextDBFilePath != null)
            {
                database.Add(new List<string>() { "[,, User-defined rt-mz text library, ]", "USR", "Unknown", "file://" + meta.TextDBFilePath.Replace("\\", "/").Replace(" ", "%20") });
            }


            if (database.Count() == 0)
            {
                database.Add(new List<string>() { "[,, no database, null ]", "null", "Unknown", "null" }); // no database
            }
            else
            {
                database.Add(new List<string>() { "[,, Unknown database, null ]", "null", "Unknown", "null" }); //unmatched database
            }
            return database;
        }
    }
}
