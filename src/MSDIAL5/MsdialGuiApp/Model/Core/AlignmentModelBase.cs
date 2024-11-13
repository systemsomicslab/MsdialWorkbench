using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.Function;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core
{
    public abstract class AlignmentModelBase : BindableBase, IAlignmentModel, IDisposable
    {
        protected readonly AlignmentFileBeanModel _alignmentFileModel;
        private readonly IMessageBroker _broker;

        public AlignmentModelBase(AlignmentFileBeanModel alignmentFileModel, IMessageBroker broker) {
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

        private MolecularNetworkInstance GetMolecularNetworkInstance(MolecularSpectrumNetworkingBaseParameter parameter) {
            var publisher = new TaskProgressPublisher(_broker, $"Exporting MN results in {parameter.ExportFolderPath}");
            using (publisher.Start()) {
                var spots = Container.AlignmentSpotProperties;
                var peaks = _alignmentFileModel.LoadMSDecResults();

                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Exporting MN results in {parameter.ExportFolderPath}");
                }

                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMolecularNetworkInstance(spots, peaks, query, notify);
                var rootObj = network.Root;
                if (parameter.MnIsExportIonCorrelation && _alignmentFileModel.CountRawFiles >= 6) {
                    var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(spots, parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode);
                    rootObj.edges.AddRange(ion_edges);
                }
                return network;
            }
        }

        public virtual void ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter) {
            var network = GetMolecularNetworkInstance(parameter);
            network.ExportNodeEdgeFiles(parameter.ExportFolderPath);
        }

        public virtual void InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter) {
            var network = GetMolecularNetworkInstance(parameter);
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
