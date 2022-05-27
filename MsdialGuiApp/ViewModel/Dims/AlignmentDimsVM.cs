using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.View.Normalize;
using CompMs.App.Msdial.ViewModel.Normalize;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    class AlignmentDimsVM : AlignmentFileViewModel
    {
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

            Model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            _broker = broker;
            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            Ms1Spots = CollectionViewSource.GetDefaultView(Model.Ms1Spots);

            Brushes = Model.Brushes.AsReadOnly();
            SelectedBrush = Model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);
            var classBrush = new KeyBrushMapper<BarItem, string>(
                Model.Parameter.ProjectParam.ClassnameToColorBytes
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                ),
                item => item.Class,
                Colors.Blue);

            PlotViewModel = new Chart.AlignmentPeakPlotViewModel(Model.PlotModel, brushSource: SelectedBrush).AddTo(Disposables);

            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               model.Parameter.ProjectParam.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Blue);

            var lowerSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               model.Parameter.ProjectParam.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Red);

            Ms2SpectrumViewModel = new Chart.MsSpectrumViewModel(Model.Ms2SpectrumModel,
                upperSpectrumBrushSource: Observable.Return(upperSpecBrush),
                lowerSpectrumBrushSource: Observable.Return(lowerSpecBrush)).AddTo(Disposables);
            AlignmentEicViewModel = new Chart.AlignmentEicViewModel(Model.AlignmentEicModel).AddTo(Disposables);
            BarChartViewModel = new Chart.BarChartViewModel(Model.BarChartModel, brushSource: Observable.Return(classBrush)).AddTo(Disposables);
            AlignmentSpotTableViewModel = new DimsAlignmentSpotTableViewModel(
                Model.AlignmentSpotTableModel,
                PeakSpotNavigatorViewModel.MzLowerValue,
                PeakSpotNavigatorViewModel.MzUpperValue,
                PeakSpotNavigatorViewModel.MetaboliteFilterKeyword,
                PeakSpotNavigatorViewModel.CommentFilterKeyword)
                .AddTo(Disposables);

            SearchCompoundCommand = new[] {
                Model.Target.Select(t => t?.innerModel != null),
                Model.MsdecResult.Select(r => r != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(SearchCompound)
            .AddTo(Disposables);
        }

        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IMessageBroker _broker;

        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public DimsAlignmentModel Model { get; }

        public ICollectionView Ms1Spots {
            get => ms1Spots;
            set => SetProperty(ref ms1Spots, value);
        }
        private ICollectionView ms1Spots;

        public override ICollectionView PeakSpotsView => ms1Spots;

        public Chart.AlignmentPeakPlotViewModel PlotViewModel {
            get => plotViewModel;
            private set => SetProperty(ref plotViewModel, value);
        }
        private Chart.AlignmentPeakPlotViewModel plotViewModel;

        public Chart.MsSpectrumViewModel Ms2SpectrumViewModel {
            get => ms2SpectrumViewModel;
            private set => SetProperty(ref ms2SpectrumViewModel, value);
        }
        private Chart.MsSpectrumViewModel ms2SpectrumViewModel;

        public Chart.AlignmentEicViewModel AlignmentEicViewModel {
            get => alignmentEicViewModel;
            private set => SetProperty(ref alignmentEicViewModel, value);
        }
        private Chart.AlignmentEicViewModel alignmentEicViewModel;

        public Chart.BarChartViewModel BarChartViewModel {
            get => barChartViewModel;
            private set => SetProperty(ref barChartViewModel, value);
        }
        private Chart.BarChartViewModel barChartViewModel;

        public DimsAlignmentSpotTableViewModel AlignmentSpotTableViewModel {
            get => alignmentSpotTableViewModel;
            private set => SetProperty(ref alignmentSpotTableViewModel, value);
        }
        private DimsAlignmentSpotTableViewModel alignmentSpotTableViewModel;

        public ReactivePropertySlim<IBrushMapper<AlignmentSpotPropertyModel>> SelectedBrush { get; }

        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using (var model = new CompoundSearchModel<AlignmentSpotProperty>(
                Model.AlignmentFile,
                Model.Target.Value.innerModel,
                Model.MsdecResult.Value,
                null,
                Model.AnnotatorContainers))
            using (var vm = new CompoundSearchVM(model)) {
                if (compoundSearchService.ShowDialog(vm) == true) {
                    Model.Target.Value.RaisePropertyChanged();
                    Ms1Spots?.Refresh();
                }
            }
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
                Model.SaveSpectra(filename);
            }
        }

        private bool CanSaveSpectra(Window owner)
        {
            return Model.CanSaveSpectra();
        }

        public DelegateCommand CopyMs2SpectrumCommand => copyMs2SpectrumCommand ?? (copyMs2SpectrumCommand = new DelegateCommand(Model.CopySpectrum, Model.CanSaveSpectra));
        private DelegateCommand copyMs2SpectrumCommand;

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        public DelegateCommand<Window> NormalizeCommand => normalizeCommand ?? (normalizeCommand = new DelegateCommand<Window>(Normalize));

        private DelegateCommand<Window> normalizeCommand;

        private void Normalize(Window owner) {
            var parameter = Model.Parameter;
            using (var vm = new NormalizationSetViewModel(Model.Container, Model.DataBaseMapper, Model.MatchResultEvaluator, parameter, _broker)) {
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
