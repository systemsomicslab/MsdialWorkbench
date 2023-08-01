using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Gcms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Gcms
{
    internal sealed class GcmsAnalysisViewModel : ViewModelBase, IAnalysisResultViewModel
    {
        private readonly GcmsAnalysisModel _model;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;

        public GcmsAnalysisViewModel(GcmsAnalysisModel model, IWindowService<PeakSpotTableViewModelBase> peakSpotTableService, FocusControlManager focusControlManager) {
            _model = model;
            _peakSpotTableService = peakSpotTableService;
            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            PeakPlotViewModel = new SpectrumFeaturePlotViewModel(model.PeakPlotModel).AddTo(Disposables);
            EicViewModel = new EicViewModel(_model.EicModel, horizontalAxis: PeakPlotViewModel.HorizontalAxis as IAxisManager<double>).AddTo(Disposables);
            var (rawDecSpectraViewFocusAction, rawDecSpectraViewFocused) = focusControlManager.Request();
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(model.RawDecSpectrumModel, rawDecSpectraViewFocusAction, rawDecSpectraViewFocused).AddTo(Disposables);
            RawPurifiedSpectrumsViewModel = new RawPurifiedSpectrumsViewModel(model.RawPurifiedSpectrumsModel).AddTo(Disposables);
            var (eiChromatogramsViewFocusAction, eiChromatogramsViewFocused) = focusControlManager.Request();
            EiChromatogramsViewModel = new EiChromatogramsViewModel(model.EiChromatogramsModel, model.NumberOfEIChromatograms, null, eiChromatogramsViewFocusAction, eiChromatogramsViewFocused).AddTo(Disposables);
            SurveyScanViewModel = new SurveyScanViewModel(model.SurveyScanModel, horizontalAxis: PeakPlotViewModel.VerticalAxis as IAxisManager<double>).AddTo(Disposables);
            
            var peakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            var compoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            var matchResultCandidatesViewModel = new MatchResultCandidatesViewModel(model.MatchResultCandidatesModel).AddTo(Disposables);
            var moleculeStructureViewModel = new MoleculeStructureViewModel(model.MoleculeStructureModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { peakInformationViewModel, compoundDetailViewModel, moleculeStructureViewModel, matchResultCandidatesViewModel, };

            SetUnknownCommand = model.CanSetUnknown.ToReactiveCommand().WithSubscribe(model.SetUnknown).AddTo(Disposables);
            UndoManagerViewModel = new UndoManagerViewModel(model.UndoManager).AddTo(Disposables);
            PeakTableViewModel = new GcmsAnalysisPeakTableViewModel(model.PeakTableModel, Observable.Return(model.EicLoader), PeakSpotNavigatorViewModel, SetUnknownCommand, UndoManagerViewModel).AddTo(Disposables);
        }

        public IResultModel Model => _model;

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }

        public SpectrumFeaturePlotViewModel PeakPlotViewModel { get; }
        public EicViewModel EicViewModel { get; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; }
        public RawPurifiedSpectrumsViewModel RawPurifiedSpectrumsViewModel { get; }

        public Ms2ChromatogramsViewModel Ms2ChromatogramsViewModel => null;

        public EiChromatogramsViewModel EiChromatogramsViewModel { get; }
        public SurveyScanViewModel SurveyScanViewModel { get; }

        public ViewModelBase[] PeakDetailViewModels { get; }

        public ICommand ShowIonTableCommand => _showIonTableCommand ?? (_showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand _showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(PeakTableViewModel);
        }


        public ICommand SetUnknownCommand { get; }

        public UndoManagerViewModel UndoManagerViewModel { get; }
        public GcmsAnalysisPeakTableViewModel PeakTableViewModel { get; }
    }
}
