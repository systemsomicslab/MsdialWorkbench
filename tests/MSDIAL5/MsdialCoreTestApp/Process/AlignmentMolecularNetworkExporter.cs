using CompMs.Common.Algorithm.Function;
using CompMs.Common.Components;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Globalization;
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
        network.Root.edges.AddRange(MolecularNetworking.GenerateFeatureLinkedEdges(spots, spots.ToDictionary(spot => spot.MasterAlignmentID, spot => spot.PeakCharacter)));
        if (parameter.MnIsExportIonCorrelation && fileCount >= 6) {
            network.Root.edges.AddRange(MolecularNetworking.GenerateEdgesByIonValues(spots, parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode));
        }
        ExportNodeTable(network, Path.Combine(outputFolder, "node.txt"));
        ExportEdgeTable(network, Path.Combine(outputFolder, "edge.txt"));
    }

    private static void ExportNodeTable(MolecularNetworkInstance network, string nodeFile) {
        using var writer = new StreamWriter(nodeFile, false, new UTF8Encoding(false));
        writer.WriteLine("ID\tMetaboliteName\tRt\tMz\tFormula\tOntology\tInChIKey\tSMILES\tSpectrum");
        foreach (var nodeObject in network.Root.nodes) {
            var node = nodeObject.data;
            writer.WriteLine(String.Join("\t", new[] {
                node.id.ToString(CultureInfo.InvariantCulture),
                Sanitize(node.Name),
                Sanitize(node.Rt),
                Sanitize(node.Mz),
                Sanitize(node.Formula),
                Sanitize(node.Ontology),
                Sanitize(node.InChiKey),
                Sanitize(node.Smiles),
                FormatSpectrum(node.MSMS),
            }));
        }
    }

    private static string FormatSpectrum(IReadOnlyList<List<double>> spectrum) {
        if (spectrum is null) {
            return String.Empty;
        }
        return String.Join(";", spectrum
            .Where(peak => peak is { Count: >= 2 })
            .Select(peak => $"{peak[0].ToString("G17", CultureInfo.InvariantCulture)},{peak[1].ToString("G17", CultureInfo.InvariantCulture)}"));
    }

    private static string Sanitize(string value) => (value ?? String.Empty)
        .Replace('\t', ' ')
        .Replace('\r', ' ')
        .Replace('\n', ' ');

    private static void ExportEdgeTable(MolecularNetworkInstance network, string edgeFile) {
        using var writer = new StreamWriter(edgeFile, false, Encoding.ASCII);
        writer.WriteLine("SourceID\tTargetID\tScore\tMatchPeakCount");
        foreach (var edge in network.Root.edges.Select(edge => edge.data)) {
            writer.WriteLine($"{edge.source}\t{edge.target}\t{edge.score}\t{edge.matchpeakcount}");
        }
    }
}
