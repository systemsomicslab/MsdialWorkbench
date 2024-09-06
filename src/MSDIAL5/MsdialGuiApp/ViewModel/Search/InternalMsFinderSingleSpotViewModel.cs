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

        public InternalMsFinderSingleSpot _model { get; }
        private readonly IMessageBroker _broker;

        public InternalMsFinderSingleSpotViewModel(InternalMsFinderSingleSpot model, IMessageBroker broker) {
            try {
                _model = model ?? throw new ArgumentNullException(nameof(model));
                Disposables.Add(model);
                _broker = broker;

                spectrumMs1ViewModel = new SingleSpectrumViewModel(model.spectrumModelMs1).AddTo(Disposables);
                spectrumMs2ViewModel = new SingleSpectrumViewModel(model.spectrumModelMs2).AddTo(Disposables);
                if (model.MoleculeStructureModel != null) {
                    MoleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.ToString());
            }
        }
        public SingleSpectrumViewModel spectrumMs1ViewModel { get; }
        public SingleSpectrumViewModel spectrumMs2ViewModel { get; }
        public MsSpectrumViewModel msSpectrumViewModel { get; }
        public MoleculeStructureViewModel MoleculeStructureViewModel { get; }
    }
}
