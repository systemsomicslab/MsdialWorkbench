using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.Function;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core {
    public abstract class AnalysisModelBase : BindableBase, IAnalysisModel, IDisposable
    {
        private readonly ChromatogramPeakFeatureCollection _peakCollection;
        private readonly MolecularSpectrumNetworkingBaseParameter _molecularSpectrumNetworkingParameter;
        private readonly PeakSpotFiltering<ChromatogramPeakFeatureModel>.PeakSpotFilter _filter;
        private readonly IMessageBroker _broker;

        public AnalysisModelBase(AnalysisFileBeanModel analysisFileModel, MolecularSpectrumNetworkingBaseParameter molecularSpectrumNetworkingParameter, PeakSpotFiltering<ChromatogramPeakFeatureModel> peakSpotFiltering, PeakFilterModel peakFilterModel, IMatchResultEvaluator<ChromatogramPeakFeatureModel> evaluator, IMessageBroker broker) {
            AnalysisFileModel = analysisFileModel;
            _molecularSpectrumNetworkingParameter = molecularSpectrumNetworkingParameter;
            _broker = broker;
            var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(analysisFileModel.PeakAreaBeanInformationFilePath);
            _peakCollection = new ChromatogramPeakFeatureCollection(peaks);
            Ms1Peaks = new ObservableCollection<ChromatogramPeakFeatureModel>(
                peaks.Select(peak => new ChromatogramPeakFeatureModel(peak).AddTo(Disposables))
            );
            if (Ms1Peaks.IsEmptyOrNull()) {
                MessageBox.Show("No peak information. Check your polarity setting.");
            }
            _filter = peakSpotFiltering.CreateFilter(peakFilterModel, evaluator);

            Target = new ReactivePropertySlim<ChromatogramPeakFeatureModel?>().AddTo(Disposables);

            var loader = new MsDecSpectrumFromFileLoader(analysisFileModel);
            MsdecResult = Target
                .DefaultIfNull(loader.LoadMSDecResult)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            CanSearchCompound = new[]
            {
                Target.Select(t => t is null || t.InnerModel is null),
                MsdecResult.Select(r => r is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public AnalysisFileBeanModel AnalysisFileModel { get; }

        public ObservableCollection<ChromatogramPeakFeatureModel> Ms1Peaks { get; }

        public ReactivePropertySlim<ChromatogramPeakFeatureModel?> Target { get; }

        public ReadOnlyReactivePropertySlim<MSDecResult?> MsdecResult { get; }

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }

        public abstract void SearchFragment();
        public abstract void InvokeMsfinder();
        public void ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
            network.ExportNodeEdgeFiles(parameter.ExportFolderPath);
        }

        public void InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
            CytoscapejsModel.SendToCytoscapeJs(network);
        }

        public void InvokeMoleculerNetworkingForTargetSpot() {
            var network = GetMolecularNetworkingInstanceForTargetSpot(_molecularSpectrumNetworkingParameter);
            if (network is null) {
                _broker.Publish(new ShortMessageRequest("Failed to calculate molecular network.\nPlease check selected peak spot."));
                return;
            }
            CytoscapejsModel.SendToCytoscapeJs(network);
        }

        private MolecularNetworkInstance GetMolecularNetworkInstance(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var publisher = new TaskProgressPublisher(_broker, $"Exporting MN results in {parameter.ExportFolderPath}");
            using (publisher.Start()) {
                IReadOnlyList<ChromatogramPeakFeatureModel> spots = Ms1Peaks;
                if (useCurrentFiltering) {
                    spots = _filter.Filter(spots).ToList();
                }
                var loader = AnalysisFileModel.MSDecLoader;
                var peaks = loader.LoadMSDecResults();

                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Exporting MN results in {parameter.ExportFolderPath}");
                }

                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMolecularNetworkInstance(spots, peaks, query, notify);
                var rootObj = network.Root;

                var ionfeature_edges = MolecularNetworking.GenerateFeatureLinkedEdges(spots, spots.ToDictionary(s => s.MasterPeakID, s => s.InnerModel.PeakCharacter));
                rootObj.edges.AddRange(ionfeature_edges);

                for (int i = 0; i < rootObj.nodes.Count; i++) {
                    var node = rootObj.nodes[i];
                    node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(spots[i], AnalysisFileModel.AnalysisFileName);
                }

                return network;
            }
        }

        private MolecularNetworkInstance? GetMolecularNetworkingInstanceForTargetSpot(MolecularSpectrumNetworkingBaseParameter parameter) {
            if (Target.Value is not ChromatogramPeakFeatureModel targetSpot) {
                return null;
            }
            if (parameter.MaxEdgeNumberPerNode == 0) {
                parameter.MinimumPeakMatch = 3;
                parameter.MaxEdgeNumberPerNode = 6;
                parameter.MaxPrecursorDifference = 400;
            }
            var publisher = new TaskProgressPublisher(_broker, $"Preparing MN results");
            using (publisher.Start()) {
                var spots = Ms1Peaks;
                var peaks = AnalysisFileModel.MSDecLoader.LoadMSDecResults();

                var targetPeak = peaks[targetSpot.MasterPeakID];

                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Preparing MN results");
                }
                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMoleculerNetworkInstanceForTargetSpot(targetSpot, targetPeak, spots, peaks, query, notify);
                var rootObj = network.Root;

                for (int i = 0; i < rootObj.nodes.Count; i++) {
                    var node = rootObj.nodes[i];
                    node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(spots[node.data.id], AnalysisFileModel.AnalysisFileName);
                }

                return network;
            }
        }

        public Task SaveAsync(CancellationToken token) {
            return _peakCollection.SerializeAsync(AnalysisFileModel.File, token);
        }

        // IDisposable fields and methods
        protected CompositeDisposable Disposables = new CompositeDisposable();
        private bool disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    Disposables.Dispose();
                }
                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        
    }
}
