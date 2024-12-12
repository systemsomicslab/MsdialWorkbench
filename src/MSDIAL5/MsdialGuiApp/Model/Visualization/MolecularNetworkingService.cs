using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.Function;
using CompMs.MsdialCore.Algorithm;
using Reactive.Bindings;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Visualization;

public sealed class MolecularNetworkingService
{
    private readonly AlignmentFileBeanModel _alignmentFileModel;
    private readonly IReadOnlyList<AlignmentSpotPropertyModel> _spots;
    private readonly IReadOnlyReactiveProperty<AlignmentSpotPropertyModel?> _target;
    private readonly IMessageBroker _broker;
    private readonly PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter _filter;
    private IReadOnlyReactiveProperty<IBarItemsLoader?>? _loader;
    private FileClassPropertiesModel? _classProperties;

    public MolecularNetworkingService(AlignmentFileBeanModel alignmentFileModel, IMessageBroker broker, PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter filter, IReadOnlyList<AlignmentSpotPropertyModel> spots, IReadOnlyReactiveProperty<AlignmentSpotPropertyModel?> target) {
        _alignmentFileModel = alignmentFileModel;
        _spots = spots;
        _target = target;
        _broker = broker;
        _filter = filter;
    }

    public void SetLoaderAndClassProperties(IReadOnlyReactiveProperty<IBarItemsLoader?> loader, FileClassPropertiesModel classProperties) {
        _loader = loader;
        _classProperties = classProperties;
    }

    public void Export(MolecularNetworkingParameter parameter) {
        var publisher = new TaskProgressPublisher(_broker, $"Exporting MN results in {parameter.BaseParameter.ExportFolderPath}");
        using (publisher.Start()) {
            var network = GetMolecularNetworkInstance(parameter, progressRate => publisher.Progress(progressRate, $"Exporting MN results in {parameter.BaseParameter.ExportFolderPath}"));
            network.ExportNodeEdgeFiles(parameter.BaseParameter.ExportFolderPath, parameter.CutByExcelLimit);
        }
    }

    public void Show(MolecularNetworkingParameter parameter) {
        var publisher = new TaskProgressPublisher(_broker, $"Preparing network");
        using (publisher.Start()) {
            var network = GetMolecularNetworkInstance(parameter, progressRate => publisher.Progress(progressRate, $"Preparing network {parameter.BaseParameter.ExportFolderPath}"));
            switch (parameter.NetworkPresentationType) {
                case NetworkVisualizationType.Cytoscape:
                    try {
                        CytoscapeMolecularNetworkClient.CreateAsync(network, parameter.CyRestApiUrl).Wait();
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

    private MolecularNetworkInstance GetMolecularNetworkInstance(MolecularNetworkingParameter parameter, Action<double>? notification) {
        var loader = _loader?.Value;
        var spots = _spots;
        if (parameter.UseCurrentFiltering) {
            spots = _filter.Filter(spots).ToList();
        }
        
        var peaks = Task.WhenAll(spots.Select(s => _alignmentFileModel.LoadMSDecResultByIndexAsync(s.MasterAlignmentID)).ToList()).Result;
        var id2spot = spots.ToDictionary(spot => spot.MasterAlignmentID);

        var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter.BaseParameter);
        var builder = new MoleculerNetworkingBase();
        var network = builder.GetMolecularNetworkInstance(spots, peaks!, query, notification);
        var rootObj = network.Root;

        if (loader is not null && _classProperties is not null) {
            foreach (var node in rootObj.nodes) {
                node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(id2spot[node.data.id], loader, _classProperties.ClassToColor);
            }
        }

        var ionfeature_edges = MolecularNetworking.GenerateFeatureLinkedEdges(spots, spots.ToDictionary(s => s.MasterAlignmentID, s => s.innerModel.PeakCharacter));
        rootObj.edges.AddRange(ionfeature_edges);

        if (parameter.BaseParameter.MnIsExportIonCorrelation && _alignmentFileModel.CountRawFiles >= 6) {
            var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(spots.Select(s => s.innerModel).ToList(), parameter.BaseParameter.MnIonCorrelationSimilarityCutOff, parameter.BaseParameter.MaxEdgeNumberPerNode);
            rootObj.edges.AddRange(ion_edges);
        }
        return network;
    }

    public void ShowForTargetSpot(MolecularNetworkingParameter parameter) {
        var publisher = new TaskProgressPublisher(_broker, $"Preparing network");
        using (publisher.Start()) {
            var network = GetMolecularNetworkInstanceForTargetSpot(parameter, progressRate => publisher.Progress(progressRate, $"Preparing MN results {parameter.BaseParameter.ExportFolderPath}"));
            CytoscapejsModel.SendToCytoscapeJs(network);
        }
    }

    private MolecularNetworkInstance GetMolecularNetworkInstanceForTargetSpot(MolecularNetworkingParameter parameter, Action<double>? notification) {
        if (parameter.BaseParameter.MaxEdgeNumberPerNode == 0) {
            parameter.BaseParameter.MinimumPeakMatch = 3;
            parameter.BaseParameter.MaxEdgeNumberPerNode = 6;
            parameter.BaseParameter.MaxPrecursorDifference = 400;
        }
        if (_target.Value is not { } targetSpot) {
            return new MolecularNetworkInstance(new CompMs.Common.DataObj.NodeEdge.RootObject());
        }

        var spots = _spots;
        if (parameter.UseCurrentFiltering) {
            spots = _filter.Filter(spots).ToList();
        }

        var loader = _loader?.Value;

        var peaks = _alignmentFileModel.LoadMSDecResults();
        var id2spot = spots.ToDictionary(spot => spot.MasterAlignmentID);
      
        var targetPeak = peaks[targetSpot.MasterAlignmentID];
        peaks = spots.Select(s => peaks[s.MasterAlignmentID]).ToList();

        var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter.BaseParameter);
        var builder = new MoleculerNetworkingBase();
        var network = builder.GetMoleculerNetworkInstanceForTargetSpot(targetSpot, targetPeak, spots, peaks, query, notification);
        var rootObj = network.Root;

        if (loader is not null && _classProperties is not null) {
            for (int i = 0; i < rootObj.nodes.Count; i++) {
                var node = rootObj.nodes[i];
                node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(id2spot[node.data.id], loader, _classProperties.ClassToColor);
            }
        }

        var ionfeature_edges = MolecularNetworking.GenerateFeatureLinkedEdges([targetSpot], spots.ToDictionary(s => s.MasterAlignmentID, s => s.innerModel.PeakCharacter));
        rootObj.edges.AddRange(ionfeature_edges);

        if (parameter.BaseParameter.MnIsExportIonCorrelation && _alignmentFileModel.CountRawFiles >= 6) {
            var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(spots.Select(s => s.innerModel).ToList(), parameter.BaseParameter.MnIonCorrelationSimilarityCutOff, parameter.BaseParameter.MaxEdgeNumberPerNode);
            rootObj.edges.AddRange(ion_edges.Where(e => e.data.source == targetSpot.MasterAlignmentID || e.data.target == targetSpot.MasterAlignmentID));
        }

        return network;
    }
}
