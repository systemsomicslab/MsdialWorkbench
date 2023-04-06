using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.CommonMVVM;
using Reactive.Bindings.Extensions;
using System;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsAnalysisViewModel : ViewModelBase, IAnalysisResultViewModel
    {
        private readonly GcmsAnalysisModel _model;

        public GcmsAnalysisViewModel(GcmsAnalysisModel model) {
            _model = model;
            PeakPlotViewModel = new SpectrumFeaturePlotViewModel(model.PeakPlotModel).AddTo(Disposables);
        }

        public SpectrumFeaturePlotViewModel PeakPlotViewModel { get; }

        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel => null;

        public Ms2ChromatogramsViewModel Ms2ChromatogramsViewModel => null;

        public IResultModel Model => _model;

        public ViewModelBase[] PeakDetailViewModels => new ViewModelBase[0];

        public ICommand ShowIonTableCommand => null;

        public ICommand SetUnknownCommand => null;

        public UndoManagerViewModel UndoManagerViewModel => null;
    }
}
