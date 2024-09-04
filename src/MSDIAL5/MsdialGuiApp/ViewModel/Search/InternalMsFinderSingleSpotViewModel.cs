using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

namespace CompMs.App.Msdial.ViewModel.Search {
    internal class InternalMsFinderSingleSpotViewModel : ViewModelBase {

        public InternalMsFinderSingleSpot _model { get; }
        private readonly IMessageBroker _broker;

        public InternalMsFinderSingleSpotViewModel(InternalMsFinderSingleSpot model, IMessageBroker broker) {
            _model = model;
            Disposables.Add(model);
            _broker = broker;

            spectrumMs1ViewModel = new SingleSpectrumViewModel(model.spectrumModelMs1).AddTo(Disposables);
            spectrumMs2ViewModel = new SingleSpectrumViewModel(model.spectrumModelMs2).AddTo(Disposables);
        }

        public SingleSpectrumViewModel spectrumMs1ViewModel { get; }
        public SingleSpectrumViewModel spectrumMs2ViewModel { get; }
    }
}
