using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.Function;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core
{
    public abstract class AlignmentModelBase : BindableBase, IAlignmentModel, IDisposable
    {
        protected readonly AlignmentFileBeanModel _alignmentFileModel;
        private readonly PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter _filter;
        private readonly IMessageBroker _broker;

        public AlignmentModelBase(AlignmentFileBeanModel alignmentFileModel, PeakSpotFiltering<AlignmentSpotPropertyModel> peakSpotFiltering, PeakFilterModel peakFilterModel, IMatchResultEvaluator<AlignmentSpotPropertyModel> evaluator, IMessageBroker broker) {
            _alignmentFileModel = alignmentFileModel ?? throw new ArgumentNullException(nameof(alignmentFileModel));
            _broker = broker;
            _container = alignmentFileModel.LoadAlignmentResultAsync().Result;
            if (_container == null) {
                MessageBox.Show("No aligned spot information."); // TODO: Move to view.
                _container = new AlignmentResultContainer
                {
                    AlignmentSpotProperties = new ObservableCollection<AlignmentSpotProperty>(),
                };
            }

            _filter = peakSpotFiltering.CreateFilter(peakFilterModel, evaluator);
        }

        public AlignmentResultContainer Container {
            get => _container;
            private set => SetProperty(ref _container, value);
        }
        private AlignmentResultContainer _container;

        public abstract AlignmentSpotSource AlignmentSpotSource { get; }

        public virtual Task SaveAsync() {
            return _alignmentFileModel.SaveAlignmentResultAsync(Container);
        }

        public abstract void SearchFragment();
        public abstract void InvokeMsfinder();

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

                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Exporting MN results in {parameter.ExportFolderPath}");
                }

                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMolecularNetworkInstance(spots, peaks, query, notify);
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

        public virtual void ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
            network.ExportNodeEdgeFiles(parameter.ExportFolderPath);
        }

        public virtual void InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
            CytoscapejsModel.SendToCytoscapeJs(network);
        }

        public abstract void InvokeMoleculerNetworkingForTargetSpot();

        protected readonly CompositeDisposable Disposables = new CompositeDisposable();

        // IAlignmentModel interface
        AlignmentFileBeanModel IAlignmentModel.AlignmentFile => _alignmentFileModel;
        AlignmentResultContainer IAlignmentModel.AlignmentResult => Container;


        // IDisposable interface
        private bool _disposedValue;

        protected virtual void Dispose(bool disposing) {
            if (!_disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects)
                    Disposables.Dispose();
                }
                _disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
