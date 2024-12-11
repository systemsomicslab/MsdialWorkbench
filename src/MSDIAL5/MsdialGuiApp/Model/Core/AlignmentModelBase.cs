using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Algorithm.Annotation;
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

        public abstract void ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering, bool cutByExcelLimit);

        public abstract void InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering, NetworkVisualizationType networkPresentationType, string cytoscapeUrl);

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
