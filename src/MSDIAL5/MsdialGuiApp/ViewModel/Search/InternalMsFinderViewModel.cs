using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Search
{
    internal sealed class InternalMsFinderViewModel : ViewModelBase {
        private InternalMsFinder _model;
        private readonly IMessageBroker _broker;

        public InternalMsFinderViewModel(InternalMsFinder model, IMessageBroker broker) {
            _model = model;
            Disposables.Add(model);
            _broker = broker;

            InternalMsFinderMetaboliteList = model.InternalMsFinderMetaboliteList;
            spectrumMs1ViewModel = new SingleSpectrumViewModel(model.spectrumModelMs1).AddTo(Disposables);
            spectrumMs2ViewModel = new SingleSpectrumViewModel(model.spectrumModelMs2).AddTo(Disposables);

            LoadAsyncCommand = new DelegateCommand(LoadAsync);
        }

        public InternalMsFinderMetaboliteList InternalMsFinderMetaboliteList { get; }
        public SingleSpectrumViewModel spectrumMs1ViewModel {  get; }
        public SingleSpectrumViewModel spectrumMs2ViewModel { get; }

        public DelegateCommand LoadAsyncCommand { get; }

        private void LoadAsync()  {
            Mouse.OverrideCursor = Cursors.Wait;

            Mouse.OverrideCursor = null;
        }
    }
}
