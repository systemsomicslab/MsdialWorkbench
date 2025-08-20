using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export
{
    public sealed class MztabFormatExporter
    {
        public MztabFormatExporter(DataBaseStorage dataBaseStorage)
        {
            _dataBaseStorage = dataBaseStorage;

            _annotatorID2DataBaseID = new Dictionary<string, string>();
            foreach (var db in dataBaseStorage.MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    _annotatorID2DataBaseID.Add(pair.AnnotatorID, db.DataBaseID);
                }
            }
            foreach (var db in dataBaseStorage.ProteomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    _annotatorID2DataBaseID.Add(pair.AnnotatorID, db.DataBaseID);
                }
            }
            foreach (var db in dataBaseStorage.EadLipidomicsDatabases) {
                foreach (var pair in db.Pairs) {
                    _annotatorID2DataBaseID.Add(pair.AnnotatorID, db.DataBaseID);
                }
            }
        }

        private const string mztabVersion = "2.0.0-M";
        private const string mtdPrefix = "MTD";
        private const string smlPrefix = "SML";
        private const string commentPrefix = "COM";

        private static readonly List<string> cvItem1 = new() { "MS", "PSI-MS controlled vocabulary", "4.1.192", "https://www.ebi.ac.uk/ols/ontologies/ms" };
        private static readonly List<string> cvItem2 = new() { "UO", "Units of Measurement Ontology", "2023-05-25", "http://purl.obolibrary.org/obo/uo.owl" };
        private readonly DataBaseStorage _dataBaseStorage;
        private const string idConfidenceDefault = "[,, MS-DIAL algorithm matching score, ]";
        private const string idConfidenceManual = "[MS, MS:1001058, quality estimation by manual validation, ]";
        private const string quantificationMethod = "[MS, MS:1002019, Label-free raw feature quantitation, ]";

        private const string smallMoleculeIdentificationReliability = "[MS, MS:1003032, compound identification confidence code in MS-DIAL, ]"; // new define on psi-ms.obo

        public string Separator { get; } = "\t";

        private readonly Dictionary<string, string> _annotatorID2DataBaseID;

        public void MztabFormatExporterCore(
            Stream stream,
            IReadOnlyList<AlignmentSpotProperty> spots,
            IReadOnlyList<MSDecResult> msdecResults,
            IReadOnlyList<AnalysisFileBean> files,
            IMetadataAccessor metaAccessor,
            IQuantValueAccessor quantAccessor,
            IReadOnlyList<StatsValue> stats,
            string outfile
        )
        {
            var exportFileName = Path.GetFileNameWithoutExtension(outfile);
            var mztabId = exportFileName; // as filename
            var meta = (metaAccessor as BaseMetadataAccessor).Parameter;
            using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true);

            //set common parameter
            var idConfidenceMeasure = SetIdConfidenceMeasure(meta.MachineCategory, idConfidenceDefault); //  must be fixed order!!
            var manualAssigned = new List<bool>(spots.Select(n => n.IsManuallyModifiedForAnnotation));
            if (manualAssigned.Contains(true))
            {
                idConfidenceMeasure.Add(idConfidenceMeasure.Count + 1, idConfidenceManual);
            }
            var database = SetDatabaseList(meta, spots); // database(library) list<list<string>>
            var RawFileMetadataDic = SetRawFileMetadataDic(files, meta.IonMode);
            // 
            var AnalysisFileClassDic = files
                    .Select(file => file.AnalysisFileClass)
                    .Distinct()
                    .Select((cls, idx) => new { Key = idx + 1, Value = cls })
                    .ToDictionary(x => x.Key, x => x.Value);

            var internalStandardDic = SetStandardDic(spots);
            //MTD section
            WriteMtdSection(sw, mztabId, meta, [.. spots], RawFileMetadataDic, AnalysisFileClassDic, idConfidenceMeasure, database);
            sw.WriteLine();

            //SML section
            var SmlDataHeader = WriteSmlHeader(sw, meta, RawFileMetadataDic, AnalysisFileClassDic);
            //SML data

            foreach (var spot in spots)
            {
                var metadata = metaAccessor.GetContent(spot, msdecResults[spot.MasterAlignmentID]);
                WriteSmlDataLine(
                    sw, spot, meta, metadata, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                    database, SmlDataHeader, internalStandardDic
                    );
                foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>())
                {
                    WriteSmlDataLine(
                        sw, driftSpot, meta, metadata, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                        database, SmlDataHeader, internalStandardDic
                        );
                }
            }

            sw.WriteLine();

            //SMF section
            var SmfDataHeader = WriteSmfHeader(sw, meta, RawFileMetadataDic);
            //SMF data
            foreach (var spot in spots)
            {
                var metadata = metaAccessor.GetContent(spot, msdecResults[spot.MasterAlignmentID]);
                WriteSmfDataLine(
                    sw, spot, meta, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                    SmfDataHeader, internalStandardDic, metadata
                    );
                foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>())
                {
                    WriteSmfDataLine(
                        sw, driftSpot, meta, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                        SmfDataHeader, internalStandardDic, metadata
                        );
                }
            }
            sw.WriteLine();

            //SME section
            var ms2Match = new List<bool>(spots.Select(n => n.IsMsmsAssigned));
            if (!ms2Match.Contains(true)) { return; }
            //SME header
            WriteSmeHeader(sw, idConfidenceMeasure);
            ////SME data
            foreach (var spot in spots)
            {
                if (spot.IsMsmsAssigned != true) { continue; }
                if (spot.MatchResults.IsTextDbBasedRepresentative == true) { continue; }

                if (spot.Name == "") { continue; }
                if (spot.IsBlankFilteredByPostCurator) { continue; }
                if (meta.IsNormalizeSplash && spot.InternalStandardAlignmentID == -1)
                {
                    continue;
                }
                if (meta.IsNormalizeIS && spot.InternalStandardAlignmentID == -1)
                {
                    continue;
                }
                //if (analysisParamForLC.IsNormalizeSplash && splashQuant == 0 && alignedSpots[i].InternalStandardAlignmentID != -1) { return; }
                //else if (analysisParamForLC.IsNormalizeSplash && splashQuant == 1 && alignedSpots[i].InternalStandardAlignmentID == -1) { return; }

                var metadata = metaAccessor.GetContent(spot, msdecResults[spot.MasterAlignmentID]);
                if(metadata["Metabolite name"].Contains("no MS2")){ continue; }

                WriteSmeDataLine(
                    sw, spot, meta, msdecResults[spot.MasterAlignmentID],
                    database, RawFileMetadataDic, idConfidenceMeasure, files, metadata
                        );
                foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>())
                {
                    if (!driftSpot.IsMsmsAssigned) { continue; }

                    WriteSmeDataLine(
                        sw, driftSpot, meta, msdecResults[spot.MasterAlignmentID],
                        database, RawFileMetadataDic, idConfidenceMeasure, files, metadata
                            );
                }
                sw.WriteLine("");
            }
            sw.WriteLine("");
        }

        private void WriteSmlDataLine(
            StreamWriter sw,
            AlignmentSpotProperty spot,
            ParameterBase meta,
            IReadOnlyDictionary<string, string> metadata,
            IQuantValueAccessor quantAccessor,
            IReadOnlyList<StatsValue> stats,
            IReadOnlyDictionary<int, RawFileMetadata> RawFileMetadataDic,
            IReadOnlyDictionary<int, string> AnalysisFileClassDic,
            IReadOnlyList<Database> database,
            IReadOnlyList<string> SmlDataHeader,
            IReadOnlyDictionary<int, string> internalStandardDic
            )
        {
            var matchResult = spot.MatchResults.Representative;

            var inchi = "null";
            var smlID = metadata["Alignment ID"];
            var smfIDrefs = metadata["Alignment ID"];
            var chemicalName = metadata["Metabolite name"];
            chemicalName = chemicalName.Split('|')[chemicalName.Split('|').Length - 1];
            var databaseIdentifier = "null";
            var LibraryID = spot?.MatchResults?.Representative.LibraryID;
            var rep = spot?.MatchResults?.Representative;
            if (rep != null &&
                rep.AnnotatorID != null &&
                _annotatorID2DataBaseID.TryGetValue(rep.AnnotatorID, out var databaseID) &&
                !string.IsNullOrEmpty(rep.Name))
            {
                databaseIdentifier = _annotatorID2DataBaseID[rep.AnnotatorID!] + ":" + rep.Name.Split('|').Last();
            }

            var chemicalFormula = metadata["Formula"];
            var smiles = metadata["SMILES"];

            var uri = "null";

            var reliability = metadata["Annotation tag (VS1.0)"]; // as msiLevel
            var bestIdConfidenceMeasure = "null";
            var theoreticalNeutralMass = "null";

            var adductIons = SetAdductTypeString(metadata["Adduct type"]?.ToString() ?? "null");

            if (LibraryID != null && LibraryID > 0 && rep.Source == SourceType.TextDB)
            {
                reliability = "annotated by user-defined text library";
                bestIdConfidenceMeasure = idConfidenceDefault;
            }
            else if (chemicalName != "Unknown")
            {
                if (spot.Formula != null || spot.Formula.Mass != 0)
                {
                    theoreticalNeutralMass = Math.Round(spot.Formula.Mass, 4).ToString(); //// need neutral mass. null ok
                }

                bestIdConfidenceMeasure = idConfidenceDefault;
            }
            else
            {
                chemicalName = "null";
            }

            if (spot.IsManuallyModifiedForAnnotation)
            {
                bestIdConfidenceMeasure = idConfidenceManual;
            }

            var score = spot.MatchResults.Representative.TotalScore;
            var totalScore = score.ToString();
            //var totalScore = score > 0 ? score > 1 ? "100" : Math.Round(score * 100, 1).ToString() : "null";

            var LineMetaData = new List<string>() {
                smlPrefix,smlID.ToString(), smfIDrefs.ToString(), databaseIdentifier,
                chemicalFormula, smiles, inchi,chemicalName, uri ,theoreticalNeutralMass,
                adductIons, reliability.ToString(),bestIdConfidenceMeasure,totalScore,
            };
            LineMetaData = LineMetaData.Select(item => string.IsNullOrEmpty(item) ? "null" : item).ToList();

            var quantValues = quantAccessor.GetQuantValues(spot);

            var statValues = stats.Select(stat => quantAccessor.GetStatsValues(spot, stat)).ToList();
            var LineData = new List<string>();
            LineData.AddRange(SetDataValues(quantValues, statValues, SmlDataHeader, RawFileMetadataDic, AnalysisFileClassDic));
            if (meta.MachineCategory == MachineCategory.IMMS || meta.MachineCategory == MachineCategory.LCIMMS || meta.MachineCategory == MachineCategory.IDIMS)
            {
                LineData.AddRange(SetIMValues(spot));
            }
            if (meta.IsNormalizedMatrixExport && (meta.IsNormalizeIS || meta.IsNormalizeSplash))
            {
                LineData.AddRange(SetNormalizedData(spot, internalStandardDic));
            }
            sw.WriteLine(string.Join(Separator, LineMetaData)
            + Separator
            + string.Join(Separator, LineData));
        }

        private void WriteSmfDataLine(
            StreamWriter sw,
            AlignmentSpotProperty spot,
            ParameterBase meta,
            IQuantValueAccessor quantAccessor,
            IReadOnlyList<StatsValue> stats,
            IReadOnlyDictionary<int, RawFileMetadata> RawFileMetadataDic,
            IReadOnlyDictionary<int, string> AnalysisFileClassDic,
            IReadOnlyList<string> SmfDataHeader,
            IReadOnlyDictionary<int, string> internalStandardDic,
            IReadOnlyDictionary<string, string> metadata
            )
        {
            var smfPrefix = "SMF";

            var matchResult = spot.MatchResults.Representative;

            var id = -1;
            var smfID = metadata["Alignment ID"];
            var smeIDrefs = "null";

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

            var adductIons = spot.AdductType.AdductIonName ?? "null";
            if (adductIons != "null" && adductIons.Length > 2 && adductIons.Substring(adductIons.Length - 2, 1) == "]")
            {
                adductIons = adductIons.Substring(0, adductIons.IndexOf("]") + 1) + "1" + adductIons.Substring(adductIons.Length - 1, 1);
            }

            var charge = spot.AdductType.ChargeNumber.ToString();
            if (spot.IonMode == IonMode.Negative)
            {
                charge = "-" + charge;
            }

            if (spot.Name is not null 
                && spot.Name != "Unknown"
                && spot.Name != "null"
                && spot.Name != ""
                && !metadata["Metabolite name"].Contains("no MS2")
                && spot.MatchResults.IsTextDbBasedRepresentative != true)
            {
                smeIDrefs = smfID.ToString();
            }

            var LineMetaData = new List<string>() {
                        smfPrefix,smfID.ToString(), smeIDrefs.ToString(), smeIDrefAmbiguity_code,
                            adductIons, isotopomer, expMassToCharge, charge , retentionTime.ToString(),retentionTimeStart.ToString(),retentionTimeEnd.ToString()
                        };
            LineMetaData = LineMetaData.Select(item => string.IsNullOrEmpty(item) ? "null" : item).ToList();

            var quantValues = quantAccessor.GetQuantValues(spot);

            var statValues = stats.Select(stat => quantAccessor.GetStatsValues(spot, stat)).ToList();
            var LineData = SetDataValues(quantValues, statValues, SmfDataHeader, RawFileMetadataDic, AnalysisFileClassDic).ToList();
            if (meta.MachineCategory == MachineCategory.IMMS || meta.MachineCategory == MachineCategory.LCIMMS || meta.MachineCategory == MachineCategory.IDIMS)
            {
                LineData.AddRange(SetIMValues(spot));
            }
            if (meta.IsNormalizeIS || meta.IsNormalizeSplash)
            {
                LineData.AddRange(SetNormalizedData(spot, internalStandardDic));
            }
            sw.WriteLine(string.Join(Separator, LineMetaData) + Separator + string.Join(Separator, LineData));
        }

        public void WriteSmeDataLine(
            StreamWriter sw,
            AlignmentSpotProperty spot,
            ParameterBase param,
            MSDecResult msdec,
            IReadOnlyList<Database> database,
            IReadOnlyDictionary<int, RawFileMetadata> RawFileMetadataDic,
            IReadOnlyDictionary<int, string> idConfidenceMeasure,
            IReadOnlyList<AnalysisFileBean> analysisFiles,
            IReadOnlyDictionary<string, string> metadata
            )
        {
            var smePrefix = "SME";
            var smeID = metadata["Alignment ID"];
            var evidenceInputID = metadata["Alignment ID"]; ; // need to consider
            var inchi = "null";
            var uri = "null";
            var adductIons = SetAdductTypeString(metadata["Adduct type"]?.ToString() ?? "null");

            var expMassToCharge = spot.MassCenter.ToString(); // 
            var derivatizedForm = "null";
            var identificationMethod = idConfidenceDefault;
            var manualCurationScore = "null";
            if (spot.IsManuallyModifiedForAnnotation == true)
            {
                manualCurationScore = "100";
                identificationMethod = idConfidenceManual;
            }

            var charge = spot.AdductType.ChargeNumber.ToString();

            if (param.IonMode == IonMode.Negative)
            {
                charge = "-" + spot.AdductType.ChargeNumber.ToString();
            }

            var properties = spot.AlignedPeakProperties;
            var repName = spot.MatchResults.Representative.Name.Split('|').Last();
            var repLibraryID = spot.MatchResults.Representative.LibraryID;
            var chemicalFormula = metadata["Formula"];
            var smiles = metadata["SMILES"];
            var theoreticalMassToCharge = metadata.TryGetValue("Reference m/z", out var refmz) ? refmz : "0";
            var spectraRefList = new List<string>();  //  multiple files
            for (int i = 0; i < properties.Count; i++)
            {
                if (properties[i].PeakID < 0) continue;
                if (!properties[i].IsMsmsAssigned) continue;
                if (properties[i].MatchResults.Representative.Name != repName
                    || properties[i].MatchResults.Representative.LibraryID != repLibraryID)
                { continue; }

                /// to get file id, peak id in aligned spots

                var ms1ScanID = properties[i].MS1RawSpectrumIdTop;
                var ms2ScanID = properties[i].MS2RawSpectrumID;
                /////

                var ms1ScanIDString = "ms1scanID";
                var ms2ScanIDString = "ms2scanID";

                var ScanIDString = ms1ScanIDString + "=" + ms1ScanID + " " + ms2ScanIDString + "=" + ms2ScanID;

                //switch (RawFileMetadataDic[i+1].Id_format_cv)
                //{
                //    case ("[MS, MS:1000770, WIFF nativeID format, ]"):
                //        ScanIDString = "scanId=" + ms2ScanID;
                //        break;
                //    case ("[MS, MS:1001508, Agilent MassHunter nativeID format, ]"):
                //        ScanIDString = "scanId=" + ms2ScanID;
                //        break;
                //    case ("[MS, MS:1000776, scan number only nativeID format, ]"):
                //    case ("[MS, MS:1000526, Waters raw format, ]"):
                //    case ("[MS, MS:1000768, Thermo nativeID format, ]"):
                //    case ("[MS, MS:1000929, Shimadzu Biotech nativeID format, ]"):
                //        ScanIDString = "scan=" + ms2ScanID;
                //        break; 
                //    default:
                //        ScanIDString = ms1ScanIDString + "=" + ms1ScanID + " " + ms2ScanIDString + "=" + ms2ScanID;
                //        break;
                //}
                spectraRefList.Add("ms_run[" + (i + 1) + "]:" + ScanIDString);
            }

            var spectraRef = spectraRefList.Count > 0 ? string.Join("| ", spectraRefList) : "null";

            var msLevel = "[MS, MS:1000511, ms level, 1]";
            if (spot.IsMsmsAssigned == true)
            {
                msLevel = "[MS, MS:1000511, ms level, 2]";
            }

            var rank = "1"; // need to consider


            var databaseIdentifier = "null";
            var rep = spot?.MatchResults?.Representative;
            if (rep != null &&
                rep.AnnotatorID != null &&
                _annotatorID2DataBaseID.TryGetValue(rep.AnnotatorID, out var databaseID) &&
                !string.IsNullOrEmpty(rep.Name))
            {
                databaseIdentifier = _annotatorID2DataBaseID[rep.AnnotatorID!] + ":" + rep.Name.Split('|').Last();
            }

            var SmeLine = new List<string>() {
                    smePrefix,smeID.ToString(), evidenceInputID.ToString(), databaseIdentifier,
                    chemicalFormula, smiles, inchi, repName,uri,derivatizedForm,adductIons,expMassToCharge,charge,theoreticalMassToCharge.ToString(),
                    spectraRef, identificationMethod, msLevel
                    };

            SmeLine.AddRange(SetExportScoreList(idConfidenceMeasure, spot.MatchResults.Representative, manualCurationScore));

            SmeLine.Add(rank);
            sw.Write(String.Join("\t", SmeLine.Select(item => string.IsNullOrEmpty(item) ? "null" : item).ToList()) + "\t");

        }

        internal void WriteMtdSection(
            StreamWriter sw,
            string mzTabId,
            ParameterBase meta,
            IReadOnlyList<AlignmentSpotProperty> spots,
            IReadOnlyDictionary<int, RawFileMetadata> RawFileMetadataDic,
            IReadOnlyDictionary<int, string> AnalysisFileClassDic,
            IReadOnlyDictionary<int, string> idConfidenceMeasure,
            IReadOnlyList<Database> database
            )
        {
            //Meta data section 
            var ionAbundanceUnits = spots.Select(spot => spot.IonAbundanceUnit).Distinct().ToList();
            var ionMobilityType = "";


            if (meta.GetType().Name == "MsdialLcImMsParameter" || meta.GetType().Name == "MsdialImmsParameter")
            {
                var ionMobilityProp = meta.GetType().GetProperty("IonMobilityType");
                if (ionMobilityProp != null)
                {
                    var value = ionMobilityProp.GetValue(meta);
                    ionMobilityType = value?.ToString();
                }
            }


            //common parameter

            var software = "[MS, MS:1003082, MS-DIAL, " + meta.MsdialVersionNumber + "]";

            var cvList = new List<List<string>>(); // cv list

            cvList.Add(cvItem1);
            if (meta.MachineCategory == MachineCategory.IMMS && ionMobilityType != "TIMS")
            {
                cvList.Add(cvItem2);
            }
            if (meta.IsNormalizedMatrixExport && meta.IsNormalizeSplash)
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

            if (RawFileMetadataDic.Values
               .Select(meta => meta.AnalysisFileExtention)
               .ToList()
               .Contains(".CDF"))
            {
                cvList.Add(["EDAM", "Bioscientific data analysis ontology", "20-06-2020", "http://edamontology.org/"]);
            }

            // add data section
            var mtdTable = new List<string>();

            //mtdTable.Add("COM\tMeta data section");
            mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "mzTab-version", mztabVersion }));
            mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "mzTab-ID", mzTabId }));
            mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "software[1]", software }));
            //mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "software[2]", mztabExporter }));

            for (int i = 0; i < RawFileMetadataDic.Count; i++)
            {
                var id = i + 1;
                var RawFileMetadataDicItem = RawFileMetadataDic.Where(item => item.Value.Id == id).ToList()[0].Value;
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, RawFileMetadataDicItem.Run + "-location", RawFileMetadataDicItem.FileLocation })); // filePath
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, RawFileMetadataDicItem.Run + "-format", RawFileMetadataDicItem.Format_cv }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, RawFileMetadataDicItem.Run + "-id_format", RawFileMetadataDicItem.Id_format_cv }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, RawFileMetadataDicItem.Run + "-scan_polarity[1]", RawFileMetadataDicItem.Scan_polarity_cv }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, RawFileMetadataDicItem.Assay, RawFileMetadataDicItem.Assay_ref })); //fileName
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, RawFileMetadataDicItem.Assay + "-ms_run_ref", RawFileMetadataDicItem.Run }));
            }

            foreach (var AnalysisFileClass in AnalysisFileClassDic)
            {
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "study_variable[" + AnalysisFileClass.Key + "]", AnalysisFileClass.Value }));

                var studyVariableAssays = RawFileMetadataDic
                   .Where(item => item.Value.AnalysisClass == AnalysisFileClass.Value)
                   .Select(item => item.Value.Assay)
                   .ToList();
                studyVariableAssays.Sort();

                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "study_variable[" + AnalysisFileClass.Key + "]-assay_refs", string.Join("| ", studyVariableAssays) }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "study_variable[" + AnalysisFileClass.Key + "]-description", AnalysisFileClass.Value }));
            }

            for (int i = 0; i < cvList.Count; i++)
            {
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "cv[" + (i + 1) + "]-label", cvList[i][0] }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "cv[" + (i + 1) + "]-full_name", cvList[i][1] }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "cv[" + (i + 1) + "]-version", cvList[i][2] }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "cv[" + (i + 1) + "]-uri", cvList[i][3] }));
            }

            for (int i = 0; i < database.Count; i++)
            {
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "database[" + (i + 1) + "]", database[i].Metadata }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "database[" + (i + 1) + "]-prefix", database[i].AnnotatorID }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "database[" + (i + 1) + "]-version", database[i].Filename }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "database[" + (i + 1) + "]-uri", database[i].Uri }));
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

            if (meta.IsNormalizedMatrixExport)
            {
                quantCvStringList.Add("[,,Normalised Abundance, ]");
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
            }

            foreach (var quantCvString in quantCvStringList)
            {
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "small_molecule-quantification_unit", quantCvString }));
            }
            foreach (var quantCvString in quantCvStringList)
            {
                mtdTable.Add(String.Join(Separator, new string[] { mtdPrefix, "small_molecule_feature-quantification_unit", quantCvString }));
            }
            if (normalizedCommentList.Count > 0)
            {
                foreach (var normalizedComment in normalizedCommentList)
                {
                    mtdTable.Add(string.Join(Separator, new string[] { commentPrefix, normalizedComment }));
                }
            }

            mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "small_molecule-identification_reliability", smallMoleculeIdentificationReliability }));

            for (int i = 0; i < idConfidenceMeasure.Count; i++)
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "id_confidence_measure[" + (i + 1) + "]", idConfidenceMeasure[i + 1] }));

            mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "quantification_method", quantificationMethod }));

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

                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "colunit-small_molecule", optMobilityUnit }));
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, "colunit-small_molecule_feature", optMobilityUnit }));
                mtdTable.Add(string.Join(Separator, new string[] { commentPrefix, optMobilityComment }));


                if (meta.InstrumentType != null || meta.InstrumentType != "")
                {
                    mtdTable.Add(string.Join(Separator, new string[] { commentPrefix, "InstrumentType: " + string.Join(", ", meta.Authors) }));
                }
                if (meta.Instrument != null || meta.Instrument != "")
                {
                    mtdTable.Add(string.Join(Separator, new string[] { commentPrefix, "Instrument: " + string.Join(", ", meta.Authors) }));
                }
                if(meta.Authors != null || meta.Authors != "")
                {
                    mtdTable.Add(string.Join(Separator, new string[] { commentPrefix, "Authors: " + string.Join(", ", meta.Authors) }));
                }
                if (meta.License != null || meta.License != "")
                {
                    mtdTable.Add(string.Join(Separator, new string[] { commentPrefix, "License: " + string.Join(", ", meta.Authors) }));
                }
                if (meta.CollisionEnergy != null || meta.CollisionEnergy != "")
                {
                    mtdTable.Add(string.Join(Separator, new string[] { commentPrefix, "Collision Energy: " + string.Join(", ", meta.Authors) }));
                }
                if (meta.Comment != null || meta.Comment != "")
                {
                    mtdTable.Add(string.Join(Separator, new string[] { commentPrefix, "Comment: " + string.Join(", ", meta.Authors) }));
                }
            }
            sw.WriteLine(string.Join(System.Environment.NewLine, mtdTable));
        }

        private List<string> WriteSmlHeader(StreamWriter sw, ParameterBase meta, IReadOnlyDictionary<int, RawFileMetadata> RawFileMetadataDic,
            Dictionary<int, string> AnalysisFileClassDic)
        {

            var SmlHeaderMeta = new List<string>()
                {
                    "SMH","SML_ID","SMF_ID_REFS","database_identifier",
                    "chemical_formula","smiles","inchi",
                    "chemical_name","uri","theoretical_neutral_mass","adduct_ions",
                    "reliability","best_id_confidence_measure",
                    "best_id_confidence_value"
                 };
            var SmlDataHeader = new List<string>();

            foreach (var item in RawFileMetadataDic)
            {
                SmlDataHeader.Add("abundance_assay[" + item.Key + "]");
            }
            foreach (var item in AnalysisFileClassDic)
            {
                SmlDataHeader.Add("abundance_study_variable[" + item.Key + "]");
            }
            foreach (var item in AnalysisFileClassDic)
            {
                SmlDataHeader.Add("abundance_variation_study_variable[" + item.Key + "]");
            }

            if (meta.MachineCategory == MachineCategory.IMMS || meta.MachineCategory == MachineCategory.LCIMMS || meta.MachineCategory == MachineCategory.IDIMS)
            {
                SmlDataHeader.Add("opt_global_Mobility");
                SmlDataHeader.Add("opt_global_CCS_values");
            }
            if (meta.IsNormalizedMatrixExport && (meta.IsNormalizeIS || meta.IsNormalizeSplash))
            {
                SmlDataHeader.Add("opt_global_internalStanderdSMLID");
                SmlDataHeader.Add("opt_global_internalStanderdMetaboliteName");
            }
            sw.WriteLine(string.Join(Separator, SmlHeaderMeta) + Separator + string.Join(Separator, SmlDataHeader));
            return SmlDataHeader;
        }
        private List<string> WriteSmfHeader(StreamWriter sw, ParameterBase meta, IReadOnlyDictionary<int, RawFileMetadata> RawFileMetadataDic)
        {
            var smfHeaderMeta = new List<string>() {
                "SFH","SMF_ID","SME_ID_REFS","SME_ID_REF_ambiguity_code","adduct_ion","isotopomer","exp_mass_to_charge",
                  "charge","retention_time_in_seconds","retention_time_in_seconds_start","retention_time_in_seconds_end"
                };
            var SmfDataHeader = new List<string>();
            foreach (var item in RawFileMetadataDic)
            {
                SmfDataHeader.Add("abundance_assay[" + item.Key + "]");
            }

            if (meta.MachineCategory == MachineCategory.IMMS || meta.MachineCategory == MachineCategory.LCIMMS || meta.MachineCategory == MachineCategory.IDIMS)
            {
                SmfDataHeader.Add("opt_global_Mobility");
                SmfDataHeader.Add("opt_global_CCS_values");
            }
            if (meta.IsNormalizeIS || meta.IsNormalizeSplash)
            {
                SmfDataHeader.Add("opt_global_internalStanderdSMLID");
                SmfDataHeader.Add("opt_global_internalStanderdMetaboliteName");
            }
            sw.WriteLine(string.Join(Separator, smfHeaderMeta) + Separator + string.Join(Separator, SmfDataHeader));
            return SmfDataHeader;
        }
        private void WriteSmeHeader(StreamWriter sw, IReadOnlyDictionary<int, string> idConfidenceMeasure)
        {
            var SmeHeader = new List<string>() {
                    "SEH","SME_ID","evidence_input_id","database_identifier","chemical_formula","smiles","inchi",
                      "chemical_name","uri","derivatized_form","adduct_ion","exp_mass_to_charge","charge", "theoretical_mass_to_charge",
                      "spectra_ref","identification_method","ms_level"
                    };
            for (int i = 0; i < idConfidenceMeasure.Count; i++)
            {
                SmeHeader.Add("id_confidence_measure[" + (i + 1) + "]");
            }
            SmeHeader.Add("rank");
            sw.WriteLine(string.Join(Separator, SmeHeader));
        }
        private static IReadOnlyList<string> SetDataValues(
            IReadOnlyDictionary<string, string> quantValues,
            IReadOnlyList<Dictionary<string, string>> statValues,
            IReadOnlyList<string> SmlDataHeader,
            IReadOnlyDictionary<int, RawFileMetadata> RawFileMetadataDic,
            IReadOnlyDictionary<int, string> AnalysisFileClassDic
            )
        {
            var dataValues = new List<string>();
            foreach (string header in SmlDataHeader)
            {
                if (header.Contains("abundance_assay["))
                {
                    var keyString = header.Replace("abundance_assay[", "").Replace("]", "");
                    if (int.TryParse(keyString, out int key))
                    {
                        var quantValue = quantValues[RawFileMetadataDic[key].Assay_ref];
                        if (string.IsNullOrEmpty(quantValue))
                        {
                            quantValue = "null";
                        }
                        dataValues.Add(quantValue);
                    }
                    else
                    {
                        dataValues.Add("null");
                    }
                }
                else if (header.Contains("abundance_study_variable["))
                {
                    var keyString = header.Replace("abundance_study_variable[", "").Replace("]", "");
                    if (int.TryParse(keyString, out int key))
                    {
                        var statValue = statValues[0][AnalysisFileClassDic[key]];
                        if (string.IsNullOrEmpty(statValue))
                        {
                            statValue = "null";
                        }
                        dataValues.Add(statValue);
                    }
                    else
                    {
                        dataValues.Add("null");
                    }
                }
                else if (header.Contains("abundance_variation_study_variable["))
                {
                    var keyString = header.Replace("abundance_variation_study_variable[", "").Replace("]", "");
                    if (int.TryParse(keyString, out int key))
                    {
                        var statValue = statValues[1][AnalysisFileClassDic[key]];
                        if (string.IsNullOrEmpty(statValue))
                        {
                            statValue = "null";
                        }
                        dataValues.Add(statValue);
                    }
                    else
                    {
                        dataValues.Add("null");
                    }
                }
            }
            return dataValues;
        }
        private static List<string> SetIMValues(
            AlignmentSpotProperty spot
        )
        {
            return new List<string>() { spot.TimesCenter.Drift.Value.ToString(), spot.CollisionCrossSection.ToString() };
        }
        private static List<string> SetNormalizedData(
            AlignmentSpotProperty spot,
            IReadOnlyDictionary<int, string> internalStandardDic
        )
        {
            return new List<string>() { spot.InternalStandardAlignmentID.ToString(), internalStandardDic[spot.InternalStandardAlignmentID] };
        }
        private static IReadOnlyDictionary<int, string> SetStandardDic(
        IReadOnlyList<AlignmentSpotProperty> spots
        )
        {
            var StandardDic = new Dictionary<int, string>();
            var StandardId = spots.Select(s => s.InternalStandardAlignmentID).Distinct().ToList();
            foreach (var id in StandardId)
            {
                if (id == -1)
                {
                    StandardDic.Add(id, "null");
                    continue;
                }
                var nameItem = spots.Where(item => item.AlignmentID == id).FirstOrDefault();
                if (nameItem == null)
                {
                    continue;
                }
                StandardDic.Add(id, ValueOrNull(nameItem.Name));
            }
            return StandardDic;
        }

        private static Dictionary<int, string> SetIdConfidenceMeasure(MachineCategory machineCategory, string idConfidenceDefault)
        {
            var idConfidenceMeasure = new Dictionary<int, string>();
            List<string> measures;
            switch (machineCategory)
            {
                case MachineCategory.GCMS:
                    measures = new List<string> {
                        idConfidenceDefault,
                        "[,, Retention time similarity, ]",
                        "[,, Retention index similarity, ]",
                        "[,, Simple dot product, ]",
                        "[,, Weighted dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Matched peaks count, ]",
                        "[,, Matched peaks percentage, ]"
                    };
                    break;
                case MachineCategory.LCMS:
                    measures = new List<string> {
                        idConfidenceDefault,
                        "[,, Retention time similarity, ]",
                        "[,, m/z similarity, ]",
                        "[,, Simple dot product, ]",
                        "[,, Weighted dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Matched peaks count, ]",
                        "[,, Matched peaks percentage, ]",
                    };
                    break;
                case MachineCategory.IMMS:
                    measures = new List<string> {
                        idConfidenceDefault,
                        "[,, CCS similarity, ]",
                        "[,, m/z similarity, ]",
                        "[,, Simple dot product, ]",
                        "[,, Weighted dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Matched peaks count, ]",
                        "[,, Matched peaks percentage, ]",
                    };
                    break;
                case MachineCategory.LCIMMS:
                    measures = new List<string> {
                        idConfidenceDefault,
                        "[,, Retention time similarity, ]",
                        "[,, CCS similarity, ]",
                        "[,, m/z similarity, ]",
                        "[,, Simple dot product, ]",
                        "[,, Weighted dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Matched peaks count, ]",
                        "[,, Matched peaks percentage, ]",
                    };
                    break;
                case MachineCategory.IFMS:
                    measures = new List<string> {
                        idConfidenceDefault,
                        "[,, m/z similarity, ]",
                        "[,, Simple dot product, ]",
                        "[,, Weighted dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Matched peaks count, ]",
                        "[,, Matched peaks percentage, ]"
                    };
                    break;
                case MachineCategory.IIMMS:
                    measures = new List<string> {
                        idConfidenceDefault,
                        "[,, CCS similarity, ]",
                        "[,, Weighted dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Matched peaks count, ]",
                        "[,, Matched peaks percentage, ]"
                    };
                    break;
                case MachineCategory.IDIMS:
                    measures = new List<string> {
                        idConfidenceDefault,
                        "[,, CCS similarity, ]",
                        "[,, Weighted dot product, ]",
                        "[,, Reverse dot product, ]",
                        "[,, Matched peaks count, ]",
                        "[,, Matched peaks percentage, ]"
                    };
                    break;
                default:
                    measures = new List<string> {
                        idConfidenceDefault,
                        null
                    };
                    break;
            }
            for (int i = 0; i < measures.Count; i++)
            {
                idConfidenceMeasure[i + 1] = measures[i];
            }
            return idConfidenceMeasure;
        }

        private IReadOnlyList<Database> SetDatabaseList(ParameterBase meta, IReadOnlyList<AlignmentSpotProperty> spots)
        {
            var database = new List<Database>();

            foreach (var db in _dataBaseStorage.MetabolomicsDataBases)
            {
                switch (db.DataBase.DataBaseSource)
                {
                    case DataBaseSource.Msp:
                        database.Add(new Database
                        {
                            AnnotatorID = db.DataBase.Id,
                            Metadata = "[,, User-defined MSP library file, ]",
                            Type = "null",
                            Filename = ValueOrNull(Path.GetFileName(db.DataBase.DataBaseSourceFilePath)),
                            Uri = "file://" + db.DataBase.DataBaseSourceFilePath.Replace("\\", "/").Replace(" ", "%20") ?? "null"
                        });
                        break;

                    case DataBaseSource.Lbm:
                        database.Add(new Database
                        {
                            AnnotatorID = db.DataBase.Id,
                            Metadata = "[,, MS-DIAL LipidsMsMs database, ]",
                            Type = "null",
                            Filename = ValueOrNull(Path.GetFileName(db.DataBase.DataBaseSourceFilePath)),
                            Uri = "file://" + db.DataBase.DataBaseSourceFilePath.Replace("\\", "/").Replace(" ", "%20") ?? "null"
                        });
                        break;
                    case DataBaseSource.Text:
                        database.Add(new Database
                        {
                            AnnotatorID = db.DataBase.Id,
                            Metadata = "[,, User-defined rt-mz text library, ]",
                            Type = "null",
                            Filename = ValueOrNull(Path.GetFileName(db.DataBase.DataBaseSourceFilePath)),
                            Uri = "file://" + db.DataBase.DataBaseSourceFilePath.Replace("\\", "/").Replace(" ", "%20") ?? "null"
                        });
                        break;
                    case DataBaseSource.EieioLipid:
                    case DataBaseSource.EidLipid:
                    case DataBaseSource.OadLipid:
                        database.Add(new Database
                        {
                            AnnotatorID = db.DataBase.Id,
                            Metadata = "[,, Database of lipids generated by MS-DIAL algorithms, ]",
                            Type = "null",
                            Filename = "Unknown",
                            Uri = "null",
                        });
                        break;
                }
            }

            foreach (var db in _dataBaseStorage.ProteomicsDataBases) {
                database.Add(new Database
                {
                    AnnotatorID = db.DataBase.Id,
                    Metadata = "[,, Database of peptides, ]",
                    Type = "null",
                    Filename = ValueOrNull(Path.GetFileName(db.DataBase.FastaFile)),
                    Uri = "file://" + db.DataBase.FastaFile.Replace("\\", "/").Replace(" ", "%20") ?? "null"
                });
            }

            foreach (var db in _dataBaseStorage.EadLipidomicsDatabases) {
                database.Add(new Database
                {
                    AnnotatorID = db.DataBase.Id,
                    Metadata = "[,, Database of lipids generated by MS-DIAL algorithms, ]",
                    Type = "null",
                    Filename = "Unknown",
                    Uri = "null",
                });
            }

            if (database.Count == 0) {
                database.Add(new Database()
                {
                    AnnotatorID = "null",
                    Metadata = "[,, no database, null ]",
                    Type = "null",
                    Filename = "Unknown",
                    Uri = "null"
                }); // no database
            }

            return database;
        }

        private static List<string> SetExportScoreList(
            IReadOnlyDictionary<int, string> idConfidenceMeasure,
            MsScanMatchResult matchResult,
            string manualCurationScore
            )
        {
            var scoreList = new List<string>();

            var ResultScoreDic = new Dictionary<string, string>();
            ResultScoreDic.Add(idConfidenceDefault, ValueOrNull(matchResult.TotalScore.ToString()));
            ResultScoreDic.Add("[,, Retention time similarity, ]", ValueOrNull(matchResult.RtSimilarity.ToString()));
            ResultScoreDic.Add("[,, Simple dot product, ]", ValueOrNull(matchResult.SimpleDotProduct.ToString()));
            ResultScoreDic.Add("[,, Reverse dot product, ]", ValueOrNull(matchResult.ReverseDotProduct.ToString()));
            ResultScoreDic.Add("[,, Weighted dot product, ]", ValueOrNull(matchResult.WeightedDotProduct.ToString()));
            ResultScoreDic.Add("[,, Matched peaks count, ]", ValueOrNull(matchResult.MatchedPeaksCount.ToString()));
            ResultScoreDic.Add("[,, Matched peaks percentage, ]", ValueOrNull(matchResult.MatchedPeaksPercentage.ToString()));
            ResultScoreDic.Add("[,, Retention index similarity, ]", ValueOrNull(matchResult.RiSimilarity.ToString()));
            ResultScoreDic.Add("[,, CCS similarity, ]", ValueOrNull(matchResult.CcsSimilarity.ToString()));
            ResultScoreDic.Add("[,, m/z similarity, ]", ValueOrNull(matchResult.AcurateMassSimilarity.ToString()));
            ResultScoreDic.Add(idConfidenceManual, manualCurationScore);

            for (int i = 0; i < idConfidenceMeasure.Count; i++)
            {
                var idConfidence = idConfidenceMeasure[i + 1];
                if (ResultScoreDic.ContainsKey(idConfidence))
                {
                    scoreList.Add(ResultScoreDic[idConfidence]);
                }
                else
                {
                    scoreList.Add("null");
                }
            }
            return scoreList;
        }

        private static IReadOnlyDictionary<int, RawFileMetadata> SetRawFileMetadataDic(IReadOnlyList<AnalysisFileBean> files, IonMode ionMode)
        {
            var fileMetadataDic = new Dictionary<int, RawFileMetadata>();
            var msRunLocation = new List<string>(files.Select(file => file.AnalysisFilePath));
            for (int i = 0; i < files.Count; i++)
            {
                var analysisFilePath = files[i].AnalysisFilePath;
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
                    default:
                        msRunFormat = "[,, Unknown file format, ]";
                        msRunIDFormat = "[,, Unknown file format Datapoint Number, ]";
                        break;
                }
                msRunIDFormat = "[,, MS-DIAL set Datapoint Number, ]";
                var id = files[i].AnalysisFileId + 1;
                fileMetadataDic.Add(id, new RawFileMetadata()
                {
                    Id = id,
                    Assay = "assay[" + id + "]",
                    Assay_ref = files[i].AnalysisFileName,
                    Run = "ms_run[" + id + "]",
                    FileLocation = "file://" + msRunLocation[i].Replace("\\", "/").Replace(" ", "%20"),
                    Format_cv = msRunFormat,
                    Id_format_cv = msRunIDFormat,
                    Scan_polarity = ionMode.ToString(),
                    Scan_polarity_cv = ionMode.ToString() == "Positive" ? "[MS,MS:1000130,positive scan,]" : "[MS, MS:1000129, negative scan, ]",
                    AnalysisFileExtention = analysisFileExtention,
                    AnalysisClass = files[i].AnalysisFileClass,
                    AnalysisFileId = files[i].AnalysisFileId
                });
            }
            return fileMetadataDic;
        }

        static string SetAdductTypeString(string adductIons)
        {
            if (adductIons != "null" && adductIons.Length > 2 && adductIons.Substring(adductIons.Length - 2, 1) == "]")
            {
                return adductIons.Substring(0, adductIons.IndexOf("]") + 1) + "1" + adductIons.Substring(adductIons.Length - 1, 1);
            }
            return "null";
        }

        static string UnknownIfEmpty(string value) => string.IsNullOrEmpty(value) ? "Unknown" : value;
        static string ValueOrNull(string value) => string.IsNullOrEmpty(value) ? "null" : value;


        public class Database
        {
            public string Metadata { get; set; }
            public string AnnotatorID { get; set; }
            public string Type { get; set; }
            public DataBaseSource Source { get; set; }
            public string Filename { get; set; }
            public string Uri { get; set; }
        }

        public class RawFileMetadata
        {
            public int Id { get; set; }
            public string Run { get; set; }
            public string FileLocation { get; set; }
            public string Format_cv { get; set; }
            public string Id_format_cv { get; set; }
            public string Scan_polarity { get; set; }
            public string Scan_polarity_cv { get; set; }
            public string Assay { get; set; }
            public string Assay_ref { get; set; }
            public string AnalysisFileExtention { get; set; }
            public string AnalysisClass { get; set; }

            public int AnalysisFileId { get; set; }
        }
    }
}
