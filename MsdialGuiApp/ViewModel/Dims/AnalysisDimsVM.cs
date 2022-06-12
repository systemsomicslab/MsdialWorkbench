using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Core.Base;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    internal sealed class AnalysisDimsVM : AnalysisFileViewModel
    {
        private readonly DimsAnalysisModel _model;
        private readonly IWindowService<CompoundSearchVM> _compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;

        public AnalysisDimsVM(
            DimsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager)
            : base(model) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            _model = model;
            _compoundSearchService = compoundSearchService;
            _peakSpotTableService = peakSpotTableService;

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            var (focusAction, focused) = focusControlManager.Request();
            PlotViewModel = new AnalysisPeakPlotViewModel(_model.PlotModel, focusAction, focused).AddTo(Disposables);
            EicViewModel = new EicViewModel(_model.EicModel, horizontalAxis: PlotViewModel.HorizontalAxis).AddTo(Disposables);
            
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(_model.Ms2SpectrumModel).AddTo(Disposables);
            PeakTableViewModel = new DimsAnalysisPeakTableViewModel(
                _model.PeakTableModel,
                Observable.Return(_model.EicLoader),
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword,
                PeakSpotNavigatorViewModel.IsEditting)
                .AddTo(Disposables);

            SearchCompoundCommand = new[]
            {
                Target.Select(t => t?.InnerModel != null),
                _model.MsdecResult.Select(r => r != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(SearchCompound)
            .AddTo(Disposables);
        }

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public AnalysisPeakPlotViewModel PlotViewModel { get; }

        public EicViewModel EicViewModel { get; }

        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; }

        public DimsAnalysisPeakTableViewModel PeakTableViewModel { get; }

        public int FocusID {
            get => focusID;
            set => SetProperty(ref focusID, value);
        }
        private int focusID;

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        public DelegateCommand<IAxisManager> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<IAxisManager>(FocusByID));
        private DelegateCommand<IAxisManager> focusByIDCommand;

        private void FocusByID(IAxisManager axis) {
            _model.FocusById(axis, FocusID);
        }

        public DelegateCommand<IAxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<IAxisManager>(FocusByMz));
        private DelegateCommand<IAxisManager> focusByMzCommand;

        private void FocusByMz(IAxisManager axis) {
            _model.FocusByMz(axis, FocusMz);
        }

        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using (var model = _model.BuildCompoundSearchModel())
            using (var vm = new CompoundSearchVM(model)) {
                if (_compoundSearchService.ShowDialog(vm) == true) {
                    _model.Target.Value.RaisePropertyChanged();
                    Ms1PeaksView?.Refresh();
                }
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(PeakTableViewModel);
        }

        public DelegateCommand<Window> SaveMs2SpectrumCommand => saveMs2SpectrumCommand ?? (saveMs2SpectrumCommand = new DelegateCommand<Window>(SaveSpectra, CanSaveSpectra));
        private DelegateCommand<Window> saveMs2SpectrumCommand;

        private void SaveSpectra(Window owner)
        {
            var sfd = new SaveFileDialog
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp|MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf|MSFINDER format(*.mat)|*.mat;|SIRIUS format(*.ms)|*.ms", 
                RestoreDirectory = true,
                AddExtension = true,
            };

            if (sfd.ShowDialog(owner) == true)
            {
                var filename = sfd.FileName;
                _model.SaveSpectra(filename);
            }
        }

        private bool CanSaveSpectra(Window owner)
        {
            return _model.CanSaveSpectra();
        }

        public DelegateCommand CopyMs2SpectrumCommand => copyMs2SpectrumCommand ?? (copyMs2SpectrumCommand = new DelegateCommand(_model.CopySpectrum, _model.CanSaveSpectra));
        private DelegateCommand copyMs2SpectrumCommand;
    }
}
