using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.View.Normalize;
using CompMs.App.Msdial.ViewModel.Normalize;
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
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
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

            MassMin = Model.MassMin;
            MassMax = Model.MassMax;
            MassLower = new ReactiveProperty<double>(MassMin).AddTo(Disposables);
            MassUpper = new ReactiveProperty<double>(MassMax).AddTo(Disposables);
            MassLower.SetValidateNotifyError(v => v < MassMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassUpper.Value ? "Too small" : null);
            MassUpper.SetValidateNotifyError(v => v < MassLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassMax ? "Too large" : null);

            MetaboliteFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            MetaboliteFilterKeywords = MetaboliteFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CommentFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            CommentFilterKeywords = CommentFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            new[]
            {
                MassLower.ToUnit(),
                MassUpper.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                CommentFilterKeyword.ToUnit(),
                DisplayFilters.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnDispatcher()
            .Subscribe(_ => Ms1Spots.Refresh())
            .AddTo(Disposables);

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
                Observable.Return(Model.BarItemsLoader),
                MassLower, MassUpper,
                MetaboliteFilterKeyword,
                CommentFilterKeyword)
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

        public DimsAlignmentModel Model { get; }

        public ICollectionView Ms1Spots {
            get => ms1Spots;
            set {
                var old = ms1Spots;
                if (SetProperty(ref ms1Spots, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Spots != null) ms1Spots.Filter += PeakFilter;
                }
            }
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

        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }

        public ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> MetaboliteFilterKeywords { get; }
        public ReactivePropertySlim<string> CommentFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> CommentFilterKeywords { get; }

        public bool RefMatchedChecked {
            get => ReadDisplayFilters(DisplayFilter.RefMatched);
            set {
                if (ReadDisplayFilters(DisplayFilter.RefMatched) != value) {
                    displayFilters.Write(DisplayFilter.RefMatched, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool SuggestedChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Suggested);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Suggested) != value) {
                    displayFilters.Write(DisplayFilter.Suggested, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool UnknownChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Unknown);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Unknown) != value) {
                    displayFilters.Write(DisplayFilter.Unknown, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool Ms2AcquiredChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Ms2Acquired);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Ms2Acquired) != value) {
                    displayFilters.Write(DisplayFilter.Ms2Acquired, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool MolecularIonChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.MolecularIon);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.MolecularIon) != value) {
                    displayFilters.Write(DisplayFilter.MolecularIon, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool BlankFilterChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Blank);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Blank) != value) {
                    displayFilters.Write(DisplayFilter.Blank, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool ManuallyModifiedChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.ManuallyModified);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.ManuallyModified) != value) {
                    displayFilters.Write(DisplayFilter.ManuallyModified, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set {
                if (SetProperty(ref displayFilters, value))
                    Ms1Spots?.Refresh();
            }
        }
        private DisplayFilter displayFilters = 0;

        bool PeakFilter(object obj) {
            if (obj is AlignmentSpotPropertyModel spot) {
                return AnnotationFilter(spot)
                    && MzFilter(spot)
                    && (!Ms2AcquiredChecked || spot.IsMsmsAssigned)
                    && (!MolecularIonChecked || spot.IsBaseIsotopeIon)
                    && (!BlankFilterChecked || spot.IsBlankFiltered)
                    && (!ManuallyModifiedChecked || spot.innerModel.IsManuallyModifiedForAnnotation)
                    && MetaboliteFilter(spot, MetaboliteFilterKeywords.Value)
                    && CommentFilter(spot, CommentFilterKeywords.Value);
            }
            return false;
        }

        bool AnnotationFilter(AlignmentSpotPropertyModel spot) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && spot.IsRefMatched(Model.DataBaseMapper)
                || SuggestedChecked && spot.IsSuggested(Model.DataBaseMapper)
                || UnknownChecked && spot.IsUnknown;
        }

        bool MzFilter(AlignmentSpotPropertyModel spot) {
            return MassLower.Value <= spot.MassCenter
                && spot.MassCenter <= MassUpper.Value;
        }

        bool CommentFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Comment.Contains(keyword));
        }

        bool MetaboliteFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Name.Contains(keyword));
        }

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

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        public DelegateCommand<Window> NormalizeCommand => normalizeCommand ?? (normalizeCommand = new DelegateCommand<Window>(Normalize));

        private DelegateCommand<Window> normalizeCommand;

        private void Normalize(Window owner) {
            var parameter = Model.Parameter;
            using (var vm = new NormalizationSetViewModel(Model.Container, Model.DataBaseMapper, parameter, Model.DataBaseMapper)) {
                var view = new NormalizationSetView
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                view.ShowDialog();
            }
        }

        private bool ReadDisplayFilters(DisplayFilter flags) {
            return (flags & DisplayFilters) != 0;
        }
    }
}
