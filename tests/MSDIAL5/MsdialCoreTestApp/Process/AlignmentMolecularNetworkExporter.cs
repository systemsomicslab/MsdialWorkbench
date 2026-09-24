using CompMs.Common.Algorithm.Function;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process;

public static class AlignmentMolecularNetworkExporter
{
    public static void Export(IReadOnlyList<AlignmentSpotProperty> spots, IReadOnlyList<MSDecResult> spectra,
        MolecularSpectrumNetworkingBaseParameter parameter, int fileCount, string outputFolder) {
        if (spots.Count != spectra.Count) {
            throw new ArgumentException("Each alignment spot must have one representative spectrum.", nameof(spectra));
        }
        var query = new MolecularNetworkingQuery {
            MsmsSimilarityCalc = parameter.MsmsSimilarityCalc,
            MassTolerance = parameter.MnMassTolerance,
            AbsoluteAbundanceCutOff = parameter.MnAbsoluteAbundanceCutOff,
            RelativeAbundanceCutOff = parameter.MnRelativeAbundanceCutOff,
            SpectrumSimilarityCutOff = parameter.MnSpectrumSimilarityCutOff,
            MinimumPeakMatch = parameter.MinimumPeakMatch,
            MaxEdgeNumberPerNode = parameter.MaxEdgeNumberPerNode,
            MaxPrecursorDifference = parameter.MaxPrecursorDifference,
            MaxPrecursorDifferenceAsPercent = parameter.MaxPrecursorDifferenceAsPercent,
        };
        // The networking algorithm refines spectra in place. Preserve the alignment spectra.
        var scans = spectra.Select((spectrum, index) => new MSScanProperty {
            ScanID = spots[index].MasterAlignmentID,
            PrecursorMz = spots[index].MassCenter,
            Spectrum = spectrum.Spectrum.Select(peak => new SpectrumPeak(peak.Mass, peak.Intensity)).ToList(),
        }).ToList();
        Directory.CreateDirectory(outputFolder);
        var network = new MoleculerNetworkingBase().GetMolecularNetworkInstance(spots, scans, query, _ => { }, temporaryDirectory: outputFolder);
        // Equal or zero peak heights have no size range in the shared builder.
        foreach (var node in network.Root.nodes) {
            if (node.data.Size < 20 || node.data.Size > 120) {
                node.data.Size = 20;
            }
        }
        network.Root.edges.AddRange(MolecularNetworking.GenerateFeatureLinkedEdges(spots, spots.ToDictionary(spot => spot.MasterAlignmentID, spot => spot.PeakCharacter)));
        if (parameter.MnIsExportIonCorrelation && fileCount >= 6) {
            network.Root.edges.AddRange(MolecularNetworking.GenerateEdgesByIonValues(spots, parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode));
        }
        network.ExportNodeTable(Path.Combine(outputFolder, "node.txt"));
        ExportEdgeTable(network, Path.Combine(outputFolder, "edge.txt"));
    }

    private static void ExportEdgeTable(MolecularNetworkInstance network, string edgeFile) {
        using var writer = new StreamWriter(edgeFile, false, Encoding.ASCII);
        writer.WriteLine("SourceID\tTargetID\tScore\tMatchPeakCount");
        foreach (var edge in network.Root.edges.Select(edge => edge.data)) {
            writer.WriteLine($"{edge.source}\t{edge.target}\t{edge.score}\t{edge.matchpeakcount}");
        }
    }
}
