using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Export; 

public sealed class AlignmentGnpsExporter {
    public string GnpsTableFilePath { get; private set; } = string.Empty;
    public string GnpsMgfFilePath { get; private set; } = string.Empty;
    public string GnpsEdgeFilePath { get; private set; } = string.Empty;

    public AlignmentGnpsExporter(string directory, string alignmentFileName) {
        var dt = DateTime.Now;
        var timestamp = dt.ToString("yyyyMMddHHmm");
        GnpsTableFilePath = Path.Combine(directory, $"{alignmentFileName}_GNPSTable_{timestamp}.txt");
        GnpsMgfFilePath = Path.Combine(directory, $"{alignmentFileName}_GNPSSpectra_{timestamp}.mgf");
        GnpsEdgeFilePath = Path.Combine(directory, $"{alignmentFileName}_GNPSEdges_{timestamp}.txt");
    }

    public void Export(
        IReadOnlyList<AlignmentSpotProperty> spots,
        IReadOnlyList<MSDecResult> msdecResults,
        IReadOnlyList<AnalysisFileBean> files,
        MulticlassFileMetaAccessor fileMetaAccessor,
        IMetadataAccessor metaAccessor,
        IQuantValueAccessor quantAccessor,
        IReadOnlyList<StatsValue> stats) {
        // Implement the export logic for GNPS format here
        // This is a placeholder implementation

        if (spots is not { Count: > 0 }) {
            throw new ArgumentException("No spots to export.");
        }

        var flattenedSpots = new List<AlignmentSpotProperty>();
        var alignedMsdecResults = new List<MSDecResult>();

        for (int i = 0; i < spots.Count; i++) {
            var spot = spots[i];
            var msdec = msdecResults[i];

            if (spot.IsMultiLayeredData()) {
                foreach (var subSpot in spot.AlignmentDriftSpotFeatures) {
                    if (subSpot.IsMsmsAssigned && subSpot.MasterAlignmentID > 0) {
                        flattenedSpots.Add(subSpot);
                        alignedMsdecResults.Add(msdec); // Assume same msdecResult applies to subspots
                    }
                }
            }
            else {
                if (spot.IsMsmsAssigned && spot.MasterAlignmentID > 0) {
                    flattenedSpots.Add(spot);
                    alignedMsdecResults.Add(msdec);
                }
            }
        }

        // table export
        using (var tableStream = File.Open(GnpsTableFilePath, FileMode.Create, FileAccess.Write)) {
            ExportGnpsTable(tableStream, files, fileMetaAccessor, metaAccessor, quantAccessor, stats, flattenedSpots, alignedMsdecResults);
        }

        // mgf export
        using (var mgfStream = File.Open(GnpsMgfFilePath, FileMode.Create, FileAccess.Write)) {
            ExportGnpsMgf(mgfStream, flattenedSpots, alignedMsdecResults);
        }

        // edge export
        var edges = BuildGnpsEdges(spots);

        var filename = Path.GetFileNameWithoutExtension(GnpsEdgeFilePath);
        var directory = Path.GetDirectoryName(GnpsEdgeFilePath);

        var edge_peakshape = Path.Combine(directory, filename + "_peakshape.csv");
        using (var stream = File.Open(edge_peakshape, FileMode.Create, FileAccess.Write)) {
            ExportPeakShapeEdges(stream, edges);
        }
        var edge_ioncorrelation = Path.Combine(directory, filename + "_ioncorrelation.csv");
        using (var stream = File.Open(edge_ioncorrelation, FileMode.Create, FileAccess.Write)) {
            ExportIonCorrelationEdges(stream, edges);
        }
        var edge_insource = Path.Combine(directory, filename + "_insource.csv");
        using (var stream = File.Open(edge_insource, FileMode.Create, FileAccess.Write)) {
            ExportInsourceEdges(stream, edges);
        }
        var edge_adduct = Path.Combine(directory, filename + "_adduct.csv");
        using (var stream = File.Open(edge_adduct, FileMode.Create, FileAccess.Write)) {
            ExportAdductEdges(stream, edges);
        }
    }

    private void ExportGnpsTable(Stream stream, IReadOnlyList<AnalysisFileBean> files, MulticlassFileMetaAccessor fileMetaAccessor, IMetadataAccessor metaAccessor, IQuantValueAccessor quantAccessor, IReadOnlyList<StatsValue> stats, List<AlignmentSpotProperty> flattenedSpots, List<MSDecResult> alignedMsdecResults) {
        var csvExporter = new AlignmentCSVExporter();
        csvExporter.Export(
            stream,
            flattenedSpots,
            alignedMsdecResults,
            files,
            fileMetaAccessor,
            metaAccessor,
            quantAccessor,
            stats);
    }

    private void ExportGnpsMgf(Stream mgfStream, List<AlignmentSpotProperty> flattenedSpots, List<MSDecResult> alignedMsdecResults) {
        var mgfExporter = new AlignmentMgfExporter();
        mgfExporter.BatchExport(
            mgfStream,
            flattenedSpots,
            alignedMsdecResults);
    }

    private static List<GnpsEdge> BuildGnpsEdges(IReadOnlyList<AlignmentSpotProperty> spots) {
        var isIonMobility = spots[0].IsMultiLayeredData();
        var edges = new List<GnpsEdge>();

        if (isIonMobility) {
            foreach (var alignedSpot in spots) {
                if (alignedSpot.MasterAlignmentID == 0) continue;
                if (alignedSpot.PeakCharacter != null && !alignedSpot.PeakCharacter.PeakLinks.IsEmptyOrNull()) {
                    foreach (var peak in alignedSpot.PeakCharacter.PeakLinks) {
                        var linkedID = peak.LinkedPeakID;
                        var linkedProp = peak.Character;
                        var linkedSpot = spots[linkedID];
                        if (linkedSpot.MasterAlignmentID == 0) continue;

                        var sourceID = Math.Min(alignedSpot.AlignmentID, linkedSpot.AlignmentID);
                        var targetID = Math.Max(alignedSpot.AlignmentID, linkedSpot.AlignmentID);
                        var sourceMasterID = Math.Min(alignedSpot.MasterAlignmentID, linkedSpot.MasterAlignmentID);
                        var targetMasterID = Math.Max(alignedSpot.MasterAlignmentID, linkedSpot.MasterAlignmentID);
                        var type = "Chromatogram-based annotation";
                        var annotation = "Similar chromatogram";
                        var score = 1.0;
                        var mzdiff = Math.Round(Math.Abs(spots[sourceID].MassCenter - spots[targetID].MassCenter), 5);

                        if (linkedProp == PeakLinkFeatureEnum.CorrelSimilar) {
                            type = "Alignment-based annotation";
                            annotation = "Ion correlation among samples";
                            foreach (var corr in alignedSpot.AlignmentSpotVariableCorrelations.OrEmptyIfNull()) {
                                if (corr.CorrelateAlignmentID == linkedSpot.AlignmentID) {
                                    score = Math.Round(corr.CorrelationScore, 3);
                                }
                            }
                        }
                        else if (linkedProp == PeakLinkFeatureEnum.Adduct) {
                            type = "Adduct annotation";
                            annotation = spots[sourceID].AdductType.ToString() + "_" + spots[targetID].AdductType.ToString() +
                                "_dm/z" + mzdiff;
                        }
                        else if (linkedProp == PeakLinkFeatureEnum.FoundInUpperMsMs && alignedSpot.MassCenter < linkedSpot.MassCenter) {
                            type = "MS2-based annotation";
                            annotation = "The precursor ion in higher m/z's MS/MS; " + "_dm/z" + mzdiff;
                        }

                        var uniquestring = sourceMasterID + "_" + targetMasterID + "_" + annotation;
                        var uniquestringShort = sourceMasterID + "_" + targetMasterID;
                        edges.Add(new GnpsEdge()
                        {
                            SourceID = sourceMasterID,
                            TargetID = targetMasterID,
                            Annotation = annotation,
                            Type = type,
                            Score = score.ToString(),
                            EdgeID = uniquestring,
                            EdgeIdShort = uniquestringShort
                        });

                        foreach (var drift in alignedSpot.AlignmentDriftSpotFeatures) {
                            sourceMasterID = Math.Min(drift.MasterAlignmentID, alignedSpot.MasterAlignmentID);
                            targetMasterID = Math.Max(drift.MasterAlignmentID, alignedSpot.MasterAlignmentID);
                            type = "RT-Mobility link";
                            annotation = "Parent " + alignedSpot.MasterAlignmentID + "_Mobility " + drift.MasterAlignmentID;
                            uniquestring = sourceMasterID + "_" + targetMasterID + "_" + annotation;
                            uniquestringShort = sourceMasterID + "_" + targetMasterID + "_" + annotation;
                            edges.Add(new GnpsEdge()
                            {
                                SourceID = sourceMasterID,
                                TargetID = targetMasterID,
                                Annotation = annotation,
                                Type = type,
                                Score = score.ToString(),
                                EdgeID = uniquestring,
                                EdgeIdShort = uniquestringShort
                            });
                        }
                    }
                }
            }
            //if (edges.Count > 0) {
            //    edges = edges.GroupBy(n => n.EdgeID).Select(g => g.First()).ToList();
            //}
        }
        else {
            //From the second
            foreach (var alignedSpot in spots) {
                if (alignedSpot.AlignmentID == 0) continue;
                if (alignedSpot.PeakCharacter != null && !alignedSpot.PeakCharacter.PeakLinks.IsEmptyOrNull()) {
                    foreach (var peak in alignedSpot.PeakCharacter.PeakLinks) {
                        var linkedID = peak.LinkedPeakID;
                        var linkedProp = peak.Character;
                        var linkedSpot = spots[linkedID];
                        if (linkedSpot.AlignmentID == 0) continue;

                        var sourceID = Math.Min(alignedSpot.AlignmentID, linkedSpot.AlignmentID);
                        var targetID = Math.Max(alignedSpot.AlignmentID, linkedSpot.AlignmentID);
                        var type = "Chromatogram-based annotation";
                        var annotation = "Similar chromatogram";
                        var score = 1.0;
                        var mzdiff = Math.Round(Math.Abs(spots[sourceID].MassCenter - spots[targetID].MassCenter), 5);

                        if (linkedProp == PeakLinkFeatureEnum.CorrelSimilar) {
                            type = "Alignment-based annotation";
                            annotation = "Ion correlation among samples";
                            foreach (var corr in alignedSpot.AlignmentSpotVariableCorrelations.OrEmptyIfNull()) {
                                if (corr.CorrelateAlignmentID == linkedSpot.AlignmentID) {
                                    score = Math.Round(corr.CorrelationScore, 3);
                                }
                            }
                        }
                        else if (linkedProp == PeakLinkFeatureEnum.Adduct) {
                            type = "Adduct annotation";
                            annotation = spots[sourceID].AdductType.ToString() + " " + spots[targetID].AdductType.ToString() +
                                " dm/z" + mzdiff;
                        }
                        else if (linkedProp == PeakLinkFeatureEnum.FoundInUpperMsMs && alignedSpot.MassCenter < linkedSpot.MassCenter) {
                            type = "MS2-based annotation";
                            annotation = "The precursor ion in higher m/z's MS/MS; " + "_dm/z" + mzdiff;
                        }

                        var uniquestring = sourceID + "_" + targetID + "_" + annotation;
                        var uniquestringShort = sourceID + "_" + targetID;
                        edges.Add(new GnpsEdge()
                        {
                            SourceID = sourceID,
                            TargetID = targetID,
                            Annotation = annotation,
                            Type = type,
                            Score = score.ToString(),
                            EdgeID = uniquestring,
                            EdgeIdShort = uniquestringShort
                        });
                    }
                }
            }
        }

        if (edges.Count > 0) {
            edges = edges.GroupBy(n => n.EdgeID).Select(g => g.First()).ToList();
        }

        return edges;
    }

    private static void ExportPeakShapeEdges(Stream stream, List<GnpsEdge> edges) {
        using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: true);
        //Header
        var header = new List<string>() { "ID1", "ID2", "EdgeType", "Score", "Annotation" };
        sw.WriteLine(String.Join(",", header.ToArray()));
        foreach (var edge in edges.Where(n => n.Type == "Chromatogram-based annotation")) {
            var field = new List<string>() { edge.SourceID.ToString(), edge.TargetID.ToString(), edge.Type, edge.Score, edge.Annotation };
            sw.WriteLine(String.Join(",", field));
        }
    }

    private static void ExportIonCorrelationEdges(Stream stream, List<GnpsEdge> edges) {
        using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: true);
        //Header
        var header = new List<string>() { "ID1", "ID2", "EdgeType", "Score", "Annotation" };
        sw.WriteLine(String.Join(",", header.ToArray()));
        foreach (var edge in edges.Where(n => n.Type == "Alignment-based annotation")) {
            var field = new List<string>() { edge.SourceID.ToString(), edge.TargetID.ToString(), edge.Type, edge.Score, edge.Annotation };
            sw.WriteLine(String.Join(",", field));
        }
    }

    private static void ExportInsourceEdges(Stream stream, List<GnpsEdge> edges) {
        using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: true);
        //Header
        var header = new List<string>() { "ID1", "ID2", "EdgeType", "Score", "Annotation" };
        sw.WriteLine(String.Join(",", header.ToArray()));
        foreach (var edge in edges.Where(n => n.Type == "MS2-based annotation")) {
            var field = new List<string>() { edge.SourceID.ToString(), edge.TargetID.ToString(), edge.Type, edge.Score, edge.Annotation };
            sw.WriteLine(String.Join(",", field));
        }
    }

    private static void ExportAdductEdges(Stream stream, List<GnpsEdge> edges) {
        using var sw = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: true);
        //Header
        var header = new List<string>() { "ID1", "ID2", "EdgeType", "Score", "Annotation" };
        sw.WriteLine(String.Join(",", header.ToArray()));
        foreach (var edge in edges.Where(n => n.Type == "Adduct annotation")) {
            var field = new List<string>() { edge.SourceID.ToString(), edge.TargetID.ToString(), edge.Type, edge.Score, edge.Annotation };
            sw.WriteLine(String.Join(",", field));
        }
    }

    private sealed class GnpsEdge {
        public int SourceID { get; set; }
        public int TargetID { get; set; }
        public string Type { get; set; }
        public string Score { get; set; }
        public string Annotation { get; set; }
        public string EdgeID { get; set; }
        public string EdgeIdShort { get; set; }
    }
}
