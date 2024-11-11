using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;

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
            spectrumMs1ViewModel = new SingleSpectrumViewModel(model.SpectrumModelMs1).AddTo(Disposables);
            spectrumMs2ViewModel = new SingleSpectrumViewModel(model.SpectrumModelMs2).AddTo(Disposables);
        }

        public InternalMsFinderMetaboliteList InternalMsFinderMetaboliteList { get; }
        public SingleSpectrumViewModel spectrumMs1ViewModel {  get; }
        public SingleSpectrumViewModel spectrumMs2ViewModel { get; }
    }
}
