using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.Function;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Visualization;

public sealed class MolecularNetworkingService
{
    private readonly AlignmentFileBeanModel _alignmentFileModel;
    private readonly IReadOnlyList<AlignmentSpotPropertyModel> _spots;
    private readonly IMessageBroker _broker;
    private readonly PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter _filter;
    private IReadOnlyReactiveProperty<IBarItemsLoader?>? _loader;
    private FileClassPropertiesModel? _classProperties;

    public MolecularNetworkingService(AlignmentFileBeanModel alignmentFileModel, IMessageBroker broker, PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter filter, IReadOnlyList<AlignmentSpotPropertyModel> spots) {
        _alignmentFileModel = alignmentFileModel;
        _spots = spots;
        _broker = broker;
        _filter = filter;
    }

    public void SetLoaderAndClassProperties(IReadOnlyReactiveProperty<IBarItemsLoader?> loader, FileClassPropertiesModel classProperties) {
        _loader = loader;
        _classProperties = classProperties;
    }

    public void Export(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering, bool cutByExcelLimit) {
        var publisher = new TaskProgressPublisher(_broker, $"Exporting MN results in {parameter.ExportFolderPath}");
        using (publisher.Start()) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering, progressRate => publisher.Progress(progressRate, $"Exporting MN results in {parameter.ExportFolderPath}"));
            network.ExportNodeEdgeFiles(parameter.ExportFolderPath, cutByExcelLimit);
        }
    }

    public void Show(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering, NetworkVisualizationType networkPresentationType, string cytoscapeUrl) {
        var publisher = new TaskProgressPublisher(_broker, $"Preparing network");
        using (publisher.Start()) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering, null);
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
    }

    private MolecularNetworkInstance GetMolecularNetworkInstance(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering, Action<double>? notification) {
        var loader = _loader?.Value;
        var spots = _spots;
        if (useCurrentFiltering) {
            spots = _filter.Filter(spots).ToList();
        }
        var peaks = _alignmentFileModel.LoadMSDecResults();

        var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
        var builder = new MoleculerNetworkingBase();
        var network = builder.GetMolecularNetworkInstance(spots, peaks, query, notification);
        var rootObj = network.Root;

        if (loader is not null && _classProperties is not null) {
            for (int i = 0; i < spots.Count; i++) {
                var node = rootObj.nodes[i];
                var spot = spots[i];
                node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(spot, loader, _classProperties.ClassToColor);
            }
        }

        var ionfeature_edges = MolecularNetworking.GenerateFeatureLinkedEdges(spots, spots.ToDictionary(s => s.MasterAlignmentID, s => s.innerModel.PeakCharacter));
        rootObj.edges.AddRange(ionfeature_edges);

        if (parameter.MnIsExportIonCorrelation && _alignmentFileModel.CountRawFiles >= 6) {
            var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(spots.Select(s => s.innerModel).ToList(), parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode);
            rootObj.edges.AddRange(ion_edges);
        }
        return network;
    }
}
