using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Search {
    internal class InternalMsFinderSingleSpotViewModel : ViewModelBase {

        public InternalMsFinderSingleSpot? _model { get; }
        private readonly IMessageBroker? _broker;

        public InternalMsFinderSingleSpotViewModel(InternalMsFinderSingleSpot model, IMessageBroker broker) {
            try {
                _model = model ?? throw new ArgumentNullException(nameof(model));
                Disposables.Add(model);
                _broker = broker;
                SpectrumMs1ViewModel = new SingleSpectrumViewModel(model.SpectrumModelMs1, broker).AddTo(Disposables);
                SpectrumMs2ViewModel = new SingleSpectrumViewModel(model.SpectrumModelMs2, broker).AddTo(Disposables);
                
                if (model.MoleculeStructureModel is not null) {
                    MoleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
                }
                if (model.RefMs2SpectrumModel is not null) {
                    MsSpectrumViewModel = new MsSpectrumViewModel(model.RefMs2SpectrumModel).AddTo(Disposables);
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
        }
        public SingleSpectrumViewModel? SpectrumMs1ViewModel { get; }
        public SingleSpectrumViewModel? SpectrumMs2ViewModel { get; }
        public MsSpectrumViewModel? MsSpectrumViewModel { get; }
        public MoleculeStructureViewModel? MoleculeStructureViewModel { get; }
    }
}
