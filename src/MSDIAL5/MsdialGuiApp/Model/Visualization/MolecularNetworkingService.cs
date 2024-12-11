using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.Function;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Visualization;

public sealed class MolecularNetworkingService
{
    private IMessageBroker _broker;
    private PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter _filter;
    private AlignmentFileBeanModel _alignmentFileModel;

    public AlignmentSpotSource AlignmentSpotSource { get; }

    public void Export(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering, bool cutByExcelLimit) {
        var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
        network.ExportNodeEdgeFiles(parameter.ExportFolderPath, cutByExcelLimit);
    }

    public void Show(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering, NetworkVisualizationType networkPresentationType, string cytoscapeUrl) {
        var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
        switch (networkPresentationType) {
            case NetworkVisualizationType.Cytoscape:
                try {
                    CytoscapeMolecularNetworkClient.CreateAsync(network, cytoscapeUrl).Wait();
                }
                catch {
                    // ignore
                    System.Diagnostics.Debug.WriteLine("Failed to connect to Cytoscape.");
                }
                break;
            case NetworkVisualizationType.CytoscapeJs:
                CytoscapejsModel.SendToCytoscapeJs(network);
                break;
        }
    }

    private MolecularNetworkInstance GetMolecularNetworkInstance(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
        if (AlignmentSpotSource.Spots is null) {
            return new MolecularNetworkInstance(new CompMs.Common.DataObj.NodeEdge.RootObject());
        }
        var publisher = new TaskProgressPublisher(_broker, $"Exporting MN results in {parameter.ExportFolderPath}");
        using (publisher.Start()) {
            IReadOnlyList<AlignmentSpotPropertyModel> spots = AlignmentSpotSource.Spots.Items;
            if (useCurrentFiltering) {
                spots = _filter.Filter(spots).ToList();
            }
            var peaks = _alignmentFileModel.LoadMSDecResults();

            var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
            var builder = new MoleculerNetworkingBase();
            var network = builder.GetMolecularNetworkInstance(spots, peaks, query, progressRate => publisher.Progress(progressRate, $"Exporting MN results in {parameter.ExportFolderPath}"));
            var rootObj = network.Root;

            var ionfeature_edges = MolecularNetworking.GenerateFeatureLinkedEdges(spots, spots.ToDictionary(s => s.MasterAlignmentID, s => s.innerModel.PeakCharacter));
            rootObj.edges.AddRange(ionfeature_edges);

            if (parameter.MnIsExportIonCorrelation && _alignmentFileModel.CountRawFiles >= 6) {
                var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(spots.Select(s => s.innerModel).ToList(), parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode);
                rootObj.edges.AddRange(ion_edges);
            }
            return network;
        }
    }
}
