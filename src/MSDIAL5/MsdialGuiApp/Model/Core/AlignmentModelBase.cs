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
        private readonly AlignmentFileBeanModel _alignmentFileModel;

        public AlignmentModelBase(AlignmentFileBeanModel alignmentFileModel) {
            _alignmentFileModel = alignmentFileModel ?? throw new ArgumentNullException(nameof(alignmentFileModel));
            Container = alignmentFileModel.LoadAlignmentResultAsync().Result;
            if (Container == null) {
                MessageBox.Show("No aligned spot information."); // TODO: Move to view.
                Container = new AlignmentResultContainer
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

        public virtual Task SaveAsync() {
            return _alignmentFileModel.SaveAlignmentResultAsync(Container);
        }

        public abstract void SearchFragment();
        public abstract void InvokeMsfinder();
        public void RunMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter) {
            var broker = MessageBroker.Default;
            var task = TaskNotification.Start($"Exporting MN results in {parameter.ExportFolderPath}");
            broker.Publish(task);

            var spots = Container.AlignmentSpotProperties;
            var peaks = _alignmentFileModel.LoadMSDecResults();

            void notify(double counter) {
                broker.Publish(task.Progress(counter, $"Exporting MN results in {parameter.ExportFolderPath}"));
            }

            var nodes = MoleculerNetworkingBase.GetSimpleNodes(spots, peaks);
            var edges = MoleculerNetworkingBase.GenerateEdgesBySpectralSimilarity(
                spots, peaks, parameter.MsmsSimilarityCalc, parameter.MnMassTolerance,
                parameter.MnAbsoluteAbundanceCutOff, parameter.MnRelativeAbundanceCutOff, parameter.MnSpectrumSimilarityCutOff,
                parameter.MinimumPeakMatch, parameter.MaxEdgeNumberPerNode, parameter.MaxPrecursorDifference, parameter.MaxPrecursorDifferenceAsPercent, notify);
            
            if (parameter.MnIsExportIonCorrelation) {
                var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(spots, parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode);
                foreach (var ion in ion_edges) edges.Add(ion);
            }

            MoleculerNetworkingBase.ExportNodesEdgesFiles(parameter.ExportFolderPath, nodes, edges);
            broker.Publish(task.End());
        }

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
