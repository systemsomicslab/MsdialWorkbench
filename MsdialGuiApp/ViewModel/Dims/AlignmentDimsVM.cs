using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.View.Normalize;
using CompMs.App.Msdial.ViewModel.Normalize;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    internal class AlignmentDimsVM : AlignmentFileViewModel
    {
        private readonly DimsAlignmentModel _model;
        private readonly IWindowService<CompoundSearchVM> _compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> _peakSpotTableService;
        private readonly IMessageBroker _broker;

        public AlignmentDimsVM(
            DimsAlignmentModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker broker)
            : base(model) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            _model = model;
            _compoundSearchService = compoundSearchService;
            _peakSpotTableService = peakSpotTableService;
            _broker = broker;

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            Ms1Spots = CollectionViewSource.GetDefaultView(_model.Ms1Spots);

            var peakPlotViewFocus = new Subject<Unit>().AddTo(Disposables);
            var peakPlotViewFocused = peakPlotViewFocus.Select(_ => true);
            PlotViewModel = new Chart.AlignmentPeakPlotViewModel(_model.PlotModel, focus: () => peakPlotViewFocus.OnNext(Unit.Default), isFocused: peakPlotViewFocused).AddTo(Disposables);

            Ms2SpectrumViewModel = new Chart.MsSpectrumViewModel(_model.Ms2SpectrumModel).AddTo(Disposables);
            AlignmentEicViewModel = new Chart.AlignmentEicViewModel(_model.AlignmentEicModel).AddTo(Disposables);
            BarChartViewModel = new Chart.BarChartViewModel(_model.BarChartModel).AddTo(Disposables);
            AlignmentSpotTableViewModel = new DimsAlignmentSpotTableViewModel(
                _model.AlignmentSpotTableModel,
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword)
                .AddTo(Disposables);

            SearchCompoundCommand = _model.CanSeachCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);
        }

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }

        public ICollectionView Ms1Spots { get; }
        public override ICollectionView PeakSpotsView => Ms1Spots;

        public Chart.AlignmentPeakPlotViewModel PlotViewModel { get; }
        public Chart.MsSpectrumViewModel Ms2SpectrumViewModel { get; }
        public Chart.AlignmentEicViewModel AlignmentEicViewModel { get; }
        public Chart.BarChartViewModel BarChartViewModel { get; }
        public DimsAlignmentSpotTableViewModel AlignmentSpotTableViewModel { get; }

        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using (var model = _model.BuildCompoundSearchModel())
            using (var vm = new CompoundSearchVM(model)) {
                if (_compoundSearchService.ShowDialog(vm) == true) {
                    _model.Target.Value.RaisePropertyChanged();
                    Ms1Spots?.Refresh();
                }
            }
        }

        public DelegateCommand SaveMs2SpectrumCommand => _saveMs2SpectrumCommand ?? (_saveMs2SpectrumCommand = new DelegateCommand(SaveSpectra, _model.CanSaveSpectra));
        private DelegateCommand _saveMs2SpectrumCommand;

        private void SaveSpectra()
        {
            var request = new SaveFileNameRequest(_model.SaveSpectra)
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp|MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf|MSFINDER format(*.mat)|*.mat;|SIRIUS format(*.ms)|*.ms",
                RestoreDirectory = true,
                AddExtension = true,
            };
            _broker.Publish(request);
        }

        public DelegateCommand CopyMs2SpectrumCommand => _copyMs2SpectrumCommand ?? (_copyMs2SpectrumCommand = new DelegateCommand(_model.CopySpectrum, _model.CanSaveSpectra));
        private DelegateCommand _copyMs2SpectrumCommand;

        public DelegateCommand ShowIonTableCommand => _showIonTableCommand ?? (_showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand _showIonTableCommand;

        private void ShowIonTable() {
            _peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        public DelegateCommand<Window> NormalizeCommand => _normalizeCommand ?? (_normalizeCommand = new DelegateCommand<Window>(Normalize));
        private DelegateCommand<Window> _normalizeCommand;

        private void Normalize(Window owner) {
            using (var model = _model.BuildNormalizeSetModel())
            using (var vm = new NormalizationSetViewModel(model)) {
                var view = new NormalizationSetView
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                view.ShowDialog();
            }
        }
    }
}
