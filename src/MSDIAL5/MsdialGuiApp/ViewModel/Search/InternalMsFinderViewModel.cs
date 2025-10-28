using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;

namespace CompMs.App.Msdial.ViewModel.Search {
    internal sealed class InternalMsFinderViewModel : ViewModelBase {
        private readonly IMessageBroker _broker;

        public InternalMsFinderViewModel(InternalMsFinder model, IMessageBroker broker) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            Disposables.Add(model);
            _broker = broker;

            SpectrumMs1ViewModel = new SingleSpectrumViewModel(model.SpectrumModelMs1, broker).AddTo(Disposables);
            SpectrumMs2ViewModel = new SingleSpectrumViewModel(model.SpectrumModelMs2, broker).AddTo(Disposables);
            MsSpectrumViewModel = new MsSpectrumViewModel(model.RefMs2SpectrumModel).AddTo(Disposables);
            MoleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel);
        }
        public InternalMsFinder Model { get; }
        public SingleSpectrumViewModel SpectrumMs1ViewModel {  get; }
        public SingleSpectrumViewModel SpectrumMs2ViewModel { get; }
        public MsSpectrumViewModel MsSpectrumViewModel { get; }
        public MoleculeStructureViewModel MoleculeStructureViewModel { get; }
    }
}
