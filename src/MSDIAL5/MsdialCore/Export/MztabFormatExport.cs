using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Annotation;
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
        private readonly AlignmentLightPeakStore? _lightPeakStore;

        public MztabFormatExporter(DataBaseStorage dataBaseStorage, AlignmentLightPeakStore? lightPeakStore = null)
        {
            _dataBaseStorage = dataBaseStorage;
            _lightPeakStore = lightPeakStore;

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
            if (_lightPeakStore is not null) {
                MztabFormatExporterCoreWithTemporarySectionSpooling(stream, spots, msdecResults, files, metaAccessor, quantAccessor, stats, outfile);
                return;
            }

            var exportFileName = Path.GetFileNameWithoutExtension(outfile);
            var mztabId = exportFileName; // as filename
            var baseAccessor = metaAccessor as BaseMetadataAccessor;
            var meta = baseAccessor.Parameter;
            // The accessor resolves a reference only for the representative match result. A row for
            // any other candidate needs the same lookup applied to that candidate, so the refer
            // itself is borrowed rather than the already-resolved metadata.
            var refer = baseAccessor.Refer;
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
            WriteMtdSection(sw, mztabId, meta, spots, RawFileMetadataDic, AnalysisFileClassDic, idConfidenceMeasure, database);
            sw.WriteLine();

            //SML section
            //SML Header
            var hasComment = spots.Any(s => !string.IsNullOrEmpty(s.Comment));
            var hasMs2 = spots.Any(n => n.IsMsmsAssigned);
            var SmlDataHeader = WriteSmlHeader(sw, meta, RawFileMetadataDic, AnalysisFileClassDic,hasComment,hasMs2);
            //SML data

            foreach (var spot in spots)
            {
                var metadata = metaAccessor.GetContent(spot, msdecResults[spot.MasterAlignmentID]);
                WriteSmlDataLine(
                    sw, spot, meta, metadata, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                    database, SmlDataHeader, internalStandardDic, hasComment,hasMs2
                    );
                foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>())
                {
                    WriteSmlDataLine(
                        sw, driftSpot, meta, metadata, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                        database, SmlDataHeader, internalStandardDic, hasComment, hasMs2
                        );
                }
            }

            sw.WriteLine();

            // The SMF section is written before the SME section but has to reference it, so the evidence
            // rows are laid out first. Sourcing the references from this table rather than re-deriving
            // them in the SMF writer also means a reference cannot survive a row that was never written:
            // the two conditions used to be written out separately and did not agree, the SMF side
            // omitting the MS/MS-assigned, blank-filtered and internal-standard tests.
            var smeGroups = AssignSmeGroups(spots, meta);

            //SMF section
            var SmfDataHeader = WriteSmfHeader(sw, meta, RawFileMetadataDic);
            //SMF data
            foreach (var spot in spots)
            {
                var metadata = metaAccessor.GetContent(spot, msdecResults[spot.MasterAlignmentID]);
                WriteSmfDataLine(
                    sw, spot, meta, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                    SmfDataHeader, internalStandardDic, metadata, GroupOf(smeGroups, spot)
                    );
                foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>())
                {
                    WriteSmfDataLine(
                        sw, driftSpot, meta, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                        SmfDataHeader, internalStandardDic, metadata, GroupOf(smeGroups, driftSpot)
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
                WriteSmeDataLines(sw, spot, meta, refer, RawFileMetadataDic, idConfidenceMeasure, GroupOf(smeGroups, spot));
                foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>())
                {
                    WriteSmeDataLines(sw, driftSpot, meta, refer, RawFileMetadataDic, idConfidenceMeasure, GroupOf(smeGroups, driftSpot));
                }
            }
            sw.WriteLine("");
        }

        private void MztabFormatExporterCoreWithTemporarySectionSpooling(
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
            var baseAccessor = metaAccessor as BaseMetadataAccessor;
            var meta = baseAccessor.Parameter;
            // The accessor resolves a reference only for the representative match result. A row for
            // any other candidate needs the same lookup applied to that candidate, so the refer
            // itself is borrowed rather than the already-resolved metadata.
            var refer = baseAccessor.Refer;
            using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 1024, leaveOpen: true);

            var idConfidenceMeasure = SetIdConfidenceMeasure(meta.MachineCategory, idConfidenceDefault);
            var manualAssigned = new List<bool>(spots.Select(n => n.IsManuallyModifiedForAnnotation));
            if (manualAssigned.Contains(true))
            {
                idConfidenceMeasure.Add(idConfidenceMeasure.Count + 1, idConfidenceManual);
            }
            var database = SetDatabaseList(meta, spots);
            var RawFileMetadataDic = SetRawFileMetadataDic(files, meta.IonMode);
            var AnalysisFileClassDic = files
                    .Select(file => file.AnalysisFileClass)
                    .Distinct()
                    .Select((cls, idx) => new { Key = idx + 1, Value = cls })
                    .ToDictionary(x => x.Key, x => x.Value);

            var internalStandardDic = SetStandardDic(spots);
            var smeGroups = AssignSmeGroups(spots, meta);
            WriteMtdSection(sw, mztabId, meta, spots, RawFileMetadataDic, AnalysisFileClassDic, idConfidenceMeasure, database);
            sw.WriteLine();

            var hasComment = spots.Any(s => !string.IsNullOrEmpty(s.Comment));
            var hasMs2 = spots.Any(n => n.IsMsmsAssigned);
            var SmlDataHeader = WriteSmlHeader(sw, meta, RawFileMetadataDic, AnalysisFileClassDic, hasComment, hasMs2);

            var smfTemp = Path.GetTempFileName();
            var smeTemp = Path.GetTempFileName();
            try {
                using (var smfWriter = new StreamWriter(File.Open(smfTemp, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.ASCII))
                using (var smeWriter = new StreamWriter(File.Open(smeTemp, FileMode.Create, FileAccess.Write, FileShare.Read), Encoding.ASCII)) {
                    var SmfDataHeader = WriteSmfHeader(smfWriter, meta, RawFileMetadataDic);
                    if (hasMs2) {
                        WriteSmeHeader(smeWriter, idConfidenceMeasure);
                    }

                    foreach (var spot in spots)
                    {
                        var msdec = msdecResults[spot.MasterAlignmentID];
                        var metadata = metaAccessor.GetContent(spot, msdec);
                        WriteSmlDataLine(
                            sw, spot, meta, metadata, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                            database, SmlDataHeader, internalStandardDic, hasComment, hasMs2
                            );
                        WriteSmfDataLine(
                            smfWriter, spot, meta, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                            SmfDataHeader, internalStandardDic, metadata, GroupOf(smeGroups, spot)
                            );
                        WriteSmeDataLines(smeWriter, spot, meta, refer, RawFileMetadataDic, idConfidenceMeasure, GroupOf(smeGroups, spot));
                        foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>())
                        {
                            WriteSmlDataLine(
                                sw, driftSpot, meta, metadata, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                                database, SmlDataHeader, internalStandardDic, hasComment, hasMs2
                                );
                            WriteSmfDataLine(
                                smfWriter, driftSpot, meta, quantAccessor, stats, RawFileMetadataDic, AnalysisFileClassDic,
                                SmfDataHeader, internalStandardDic, metadata, GroupOf(smeGroups, driftSpot)
                                );
                            WriteSmeDataLines(smeWriter, driftSpot, meta, refer, RawFileMetadataDic, idConfidenceMeasure, GroupOf(smeGroups, driftSpot));
                        }
                    }
                }

                sw.WriteLine();
                ReplayTemporarySection(sw, smfTemp);
                sw.WriteLine();
                if (!hasMs2) { return; }
                ReplayTemporarySection(sw, smeTemp);
                sw.WriteLine("");
            }
            finally {
                TryDeleteTemporaryFile(smfTemp);
                TryDeleteTemporaryFile(smeTemp);
            }
        }

        /// <summary>
        /// Whether a spot contributes evidence rows at all.
        /// </summary>
        /// <remarks>
        /// The "no MS2" test used to read the metadata dictionary, whose "Metabolite name" entry is the
        /// spot name with an empty one replaced by "Unknown". Reading the spot directly is the same test
        /// once the empty name is excluded above, and it lets the assignment pass run without building a
        /// metadata dictionary per spot.
        /// </remarks>
        private static bool ShouldWriteSmeLine(AlignmentSpotProperty spot, ParameterBase meta) {
            if (spot.IsMsmsAssigned != true) { return false; }
            if (spot.IsManuallyModifiedForAnnotation == true) { return false; }
            if (spot.MatchResults.IsTextDbBasedRepresentative == true) { return false; }
            if (string.IsNullOrEmpty(spot.Name)) { return false; }
            if (spot.IsBlankFilteredByPostCurator) { return false; }
            if (meta.IsNormalizeSplash && spot.InternalStandardAlignmentID == -1) { return false; }
            if (meta.IsNormalizeIS && spot.InternalStandardAlignmentID == -1) { return false; }
            return !spot.Name.Contains("no MS2");
        }

        /// <summary>
        /// The evidence rows one spot contributes: its annotation candidates, best first, paired with the
        /// file-unique SME identifiers they will be written under.
        /// </summary>
        private sealed class SmeGroup
        {
            public SmeGroup(IReadOnlyList<MsScanMatchResult> candidates, int firstSmeId) {
                Candidates = candidates;
                FirstSmeId = firstSmeId;
            }

            public IReadOnlyList<MsScanMatchResult> Candidates { get; }

            /// <summary>The identifier of the rank 1 row; the rest follow it consecutively.</summary>
            public int FirstSmeId { get; }

            public int SmeId(int index) => FirstSmeId + index;

            /// <summary>
            /// The mzTab-M ambiguity code for the SMF row that references this group: 1 when the group
            /// holds alternative identifications of the same feature, and null when there is nothing to
            /// be ambiguous about. Code 2, several evidence streams for one molecule, does not apply
            /// because every row here comes from the same input spectrum.
            /// </summary>
            public string AmbiguityCode => Candidates.Count > 1 ? "1" : "null";

            public string SmeIdRefs => string.Join("|", Enumerable.Range(0, Candidates.Count).Select(i => SmeId(i).ToString()));
        }

        /// <summary>
        /// Lays out the SME section ahead of writing, so the SMF rows can reference identifiers that are
        /// guaranteed to exist and the ranks within one input spectrum are consecutive.
        /// </summary>
        /// <remarks>
        /// Keyed by spot instance rather than by alignment identifier: a drift spot of an ion-mobility run
        /// is written with its parent's metadata dictionary, so the identifier alone does not distinguish
        /// the two.
        /// </remarks>
        private static Dictionary<AlignmentSpotProperty, SmeGroup> AssignSmeGroups(
            IReadOnlyList<AlignmentSpotProperty> spots,
            ParameterBase meta) {
            var groups = new Dictionary<AlignmentSpotProperty, SmeGroup>();
            var nextSmeId = 1;
            foreach (var spot in spots) {
                if (!ShouldWriteSmeLine(spot, meta)) { continue; }
                nextSmeId = Assign(groups, spot, nextSmeId);
                foreach (var driftSpot in spot.AlignmentDriftSpotFeatures ?? Enumerable.Empty<AlignmentSpotProperty>()) {
                    if (!driftSpot.IsMsmsAssigned) { continue; }
                    nextSmeId = Assign(groups, driftSpot, nextSmeId);
                }
            }
            return groups;
        }

        private static int Assign(Dictionary<AlignmentSpotProperty, SmeGroup> groups, AlignmentSpotProperty spot, int nextSmeId) {
            var candidates = AnnotationCandidates.Of(spot.MatchResults);
            if (candidates.Count == 0) {
                return nextSmeId;
            }
            groups[spot] = new SmeGroup(candidates, nextSmeId);
            return nextSmeId + candidates.Count;
        }

        private static SmeGroup? GroupOf(Dictionary<AlignmentSpotProperty, SmeGroup> groups, AlignmentSpotProperty spot)
            => groups.TryGetValue(spot, out var group) ? group : null;

        private static void ReplayTemporarySection(StreamWriter sw, string path) {
            using var reader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read), Encoding.ASCII);
            string line;
            while ((line = reader.ReadLine()) != null) {
                sw.WriteLine(line);
            }
        }

        private static void TryDeleteTemporaryFile(string path) {
            try {
                File.Delete(path);
            }
            catch {
                // Temporary export spools are best-effort cleanup files.
            }
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
            IReadOnlyDictionary<int, string> internalStandardDic,
            bool hasComment,
            bool hasMs2
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
                !string.IsNullOrEmpty(rep.Name))
            {
                if(_annotatorID2DataBaseID.TryGetValue(rep.AnnotatorID, out var databaseID))
                {
                    databaseIdentifier = _annotatorID2DataBaseID[rep.AnnotatorID!] + ":" + rep.Name.Split('|').Last();
                }
                else
                {
                    if (rep.AnnotatorID == "MS-FINDER")
                    {
                        databaseIdentifier = "MS-FINDER:" + rep.Name.Split('|').Last();
                    }
                }
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
                if (spot.Formula is { Mass: > 0d } formula)
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
            if (hasMs2)
            {
                LineData.AddRange(SetMsmsPresence(spot));  // add 20251208
            }
            LineData.AddRange(SetSpectrumMatch(matchResult));  // add 20251208
            if (hasComment)
            {
                LineData.Add(string.IsNullOrEmpty(spot.Comment) ? "null" : spot.Comment);
            }
            LineData.AddRange(SetOntology(spot));  // add 20260127
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
            IReadOnlyDictionary<string, string> metadata,
            SmeGroup? smeGroup
            )
        {
            var smfPrefix = "SMF";

            var matchResult = spot.MatchResults.Representative;

            var smfID = metadata["Alignment ID"];
            // Taken from the evidence layout rather than re-derived here, so the reference set and the
            // rows that actually get written cannot disagree.
            var smeIDrefs = smeGroup?.SmeIdRefs ?? "null";
            var smeIDrefAmbiguity_code = smeGroup?.AmbiguityCode ?? "null";
            var isotopomer = "null";
            var expMassToCharge = spot.MassCenter.ToString();

            var retentionTime = "null";
            var retentionTimeStart = "null";
            var retentionTimeEnd = "null";
            if (spot.TimesCenter.RT.Value > 0.0)
            {
                retentionTime = (spot.TimesCenter.RT.Value * 60.0).ToString();
                retentionTimeStart = (spot.TimesMin.RT.Value * 60.0).ToString();
                retentionTimeEnd = (spot.TimesMax.RT.Value *60.0).ToString();
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

        /// <summary>
        /// Writes one evidence row per annotation candidate of a spot.
        /// </summary>
        /// <remarks>
        /// mzTab-M already has a way to say "A or B": evidence rows that share an evidence_input_id came
        /// from the same input spectrum, rank orders them, and the feature row that references them
        /// carries ambiguity code 1. MS-DIAL keeps up to NUMBER_OF_ANNOTATION_RESULTS threshold-passing
        /// candidates per annotator and alignment carries them into the spot, so the alternatives existed
        /// all along and were discarded at the file boundary, leaving every identification looking
        /// unambiguous. Rank 1 is the representative and its columns are unchanged.
        /// </remarks>
        private void WriteSmeDataLines(
            StreamWriter sw,
            AlignmentSpotProperty spot,
            ParameterBase param,
            IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>? refer,
            IReadOnlyDictionary<int, RawFileMetadata> RawFileMetadataDic,
            IReadOnlyDictionary<int, string> idConfidenceMeasure,
            SmeGroup? smeGroup
            )
        {
            if (smeGroup is null) {
                return;
            }
            for (int index = 0; index < smeGroup.Candidates.Count; index++) {
                WriteSmeDataLine(
                    sw, spot, param, refer, RawFileMetadataDic, idConfidenceMeasure,
                    smeGroup.Candidates[index], smeGroup.SmeId(index), index + 1);
            }
        }

        private void WriteSmeDataLine(
            StreamWriter sw,
            AlignmentSpotProperty spot,
            ParameterBase param,
            IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>? refer,
            IReadOnlyDictionary<int, RawFileMetadata> RawFileMetadataDic,
            IReadOnlyDictionary<int, string> idConfidenceMeasure,
            MsScanMatchResult candidate,
            int smeID,
            int rank
            )
        {
            var smePrefix = "SME";
            // The spot this row describes, not the parent whose metadata dictionary the caller reused, so
            // an ion-mobility drift row is not grouped with its parent as an alternative for one input.
            var evidenceInputID = spot.MasterAlignmentID;
            var reference = refer?.Refer(candidate);
            var inchi = "null";
            var uri = "null";
            var adductIons = SetAdductTypeString(spot.AdductType?.AdductIonName ?? "null");

            var expMassToCharge = spot.MassCenter.ToString(); // 
            var derivatizedForm = "null";
            var identificationMethod = idConfidenceDefault;
            var manualCurationScore = "null";
            if (candidate.IsManuallyModified)
            {
                manualCurationScore = "100";
                identificationMethod = idConfidenceManual;
            }

            var charge = spot.AdductType.ChargeNumber.ToString();

            if (param.IonMode == IonMode.Negative)
            {
                charge = "-" + spot.AdductType.ChargeNumber.ToString();
            }

            var repName = (candidate.Name ?? string.Empty).Split('|').Last();
            var repLibraryID = candidate.LibraryID;
            var chemicalFormula = ValueOrNull(reference?.Formula?.FormulaString);
            var smiles = ValueOrNull(reference?.SMILES);
            // Resolved from this candidate rather than read from the representative's metadata. A
            // reference that does not resolve is the mzTab null token; it used to be the number 0, which
            // is not a mass.
            var theoreticalMassToCharge = reference is null ? "null" : reference.PrecursorMz.ToString("F5");
            // The member spectra whose own top annotation is this candidate. A lower-ranked alternative
            // usually has none, and reports null: no file independently chose it. That the alternatives
            // were scored against the same query spectrum is already stated by evidence_input_id.
            var spectraRefList = new List<string>();  //  multiple files
            if (_lightPeakStore is null) {
                var properties = spot.AlignedPeakProperties;
                for (int i = 0; i < properties.Count; i++)
                {
                    if (properties[i].PeakID < 0) continue;
                    if (!properties[i].IsMsmsAssigned) continue;
                    if (properties[i].MatchResults.Representative.LibraryID != repLibraryID)
                    { continue; }

                    AddSpectraRef(spectraRefList, i + 1, properties[i].MS1RawSpectrumIdTop, properties[i].MS2RawSpectrumID);
                }
            }
            else {
                foreach (var peak in _lightPeakStore.ReadSpotPeaks(spot.MasterAlignmentID)) {
                    if (peak.PeakID < 0) continue;
                    if (!peak.IsMsmsAssigned) continue;
                    if (peak.RepresentativeLibraryID != repLibraryID) continue;
                    AddSpectraRef(spectraRefList, peak.FileID + 1, peak.MS1RawSpectrumIdTop, peak.MS2RawSpectrumID);
                }
            }

            var spectraRef = spectraRefList.Count > 0 ? string.Join("| ", spectraRefList) : "null";

            var msLevel = "[MS, MS:1000511, ms level, 1]";
            if (spot.IsMsmsAssigned == true)
            {
                msLevel = "[MS, MS:1000511, ms level, 2]";
            }

            var databaseIdentifier = "null";
            if (candidate.AnnotatorID != null &&
                _annotatorID2DataBaseID.TryGetValue(candidate.AnnotatorID, out var databaseID) &&
                !string.IsNullOrEmpty(candidate.Name))
            {
                databaseIdentifier = databaseID + ":" + candidate.Name.Split('|').Last();
            }

            var SmeLine = new List<string>() {
                    smePrefix,smeID.ToString(), evidenceInputID.ToString(), databaseIdentifier,
                    chemicalFormula, smiles, inchi, repName,uri,derivatizedForm,adductIons,expMassToCharge,charge,theoreticalMassToCharge.ToString(),
                    spectraRef, identificationMethod, msLevel
                    };

            SmeLine.AddRange(SetExportScoreList(idConfidenceMeasure, candidate, manualCurationScore));

            SmeLine.Add(rank.ToString());
            // One line per evidence row. The previous form wrote the row without a terminator and let the
            // caller close it, which appended a trailing separator to every line and would have run the
            // candidates of one spot together on a single physical line.
            sw.WriteLine(String.Join("	", SmeLine.Select(item => string.IsNullOrEmpty(item) ? "null" : item).ToList()));
        }

        private static void AddSpectraRef(List<string> spectraRefList, int msRunID, int ms1ScanID, int ms2ScanID) {
            var ms1ScanIDString = "ms1scanID";
            var ms2ScanIDString = "ms2scanID";
            var ScanIDString = ms1ScanIDString + "=" + ms1ScanID + " " + ms2ScanIDString + "=" + ms2ScanID;
            spectraRefList.Add("ms_run[" + msRunID + "]:" + ScanIDString);
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
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, RawFileMetadataDicItem.Assay + "-custom[1]", RawFileMetadataDicItem.AnalysisBatch }));// add 20260127 This output will no longer pass through the validator.
                mtdTable.Add(string.Join(Separator, new string[] { mtdPrefix, RawFileMetadataDicItem.Assay + "-custom[2]", RawFileMetadataDicItem.AnalysisFileAnalyticalOrder }));// add 20260127 This output will no longer pass through the validator.
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
            Dictionary<int, string> AnalysisFileClassDic, bool hasComment, bool hasMs2)
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
            if (hasMs2)
            {
                SmlDataHeader.Add("opt_global_ms2_presence");
            }
            SmlDataHeader.Add("opt_global_spectrum_matched");
            if (hasComment)
            {
                SmlDataHeader.Add("opt_global_user_comment");
            }
            SmlDataHeader.Add("opt_global_Ontology");

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
        private static List<string> SetMsmsPresence(
            AlignmentSpotProperty spot
        )
        {
            return new List<string>() { spot.IsMsmsAssigned.ToString() };
        }
        private static List<string> SetSpectrumMatch(
            MsScanMatchResult matchResult
        )
        {
            return new List<string>() { (matchResult?.IsSpectrumMatch ?? false).ToString() };
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
        private static List<string> SetOntology(
            AlignmentSpotProperty spot
        )
        {
            return new List<string>() { ValueOrNull(spot.Ontology) };
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
                    AnalysisFileId = files[i].AnalysisFileId,
                    AnalysisBatch = "[MS,MS:4000088,batch label," + files[i].AnalysisBatch.ToString() + "]",
                    AnalysisFileAnalyticalOrder = "[MS,MS:4000089,injection sequence label," + files[i].AnalysisFileAnalyticalOrder.ToString() + "]",
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
        static string ValueOrNull(string? value) => string.IsNullOrEmpty(value) ? "null" : value;


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
            public string AnalysisFileAnalyticalOrder { get; set; }
            public string AnalysisBatch { get; set; } 

        }
    }
}
