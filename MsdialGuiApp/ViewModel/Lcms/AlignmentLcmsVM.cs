using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.View.Normalize;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Normalize;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Parameter;
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

namespace CompMs.App.Msdial.ViewModel.Lcms
{
    class AlignmentLcmsVM : AlignmentFileViewModel
    {
        public AlignmentLcmsVM(
            LcmsAlignmentModel model,
            IWindowService<ViewModel.CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService, 
            IObservable<ParameterBase> parameter)
            : base(model) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }

            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (proteomicsTableService is null) {
                throw new ArgumentNullException(nameof(proteomicsTableService));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            this.model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            this.proteomicsTableService = proteomicsTableService;

            Target = this.model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Target.Where(x => !(x is null)).Subscribe(t => {
                FocusID = t.MasterAlignmentID;
                FocusRt = t.TimesCenter;
                FocusMz = t.MassCenter;
            }).AddTo(Disposables);
            Brushes = this.model.Brushes.AsReadOnly();
            SelectedBrush = this.model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);

            MassMin = this.model.MassMin;
            MassMax = this.model.MassMax;
            MassLower = new ReactiveProperty<double>(MassMin).AddTo(Disposables);
            MassUpper = new ReactiveProperty<double>(MassMax).AddTo(Disposables);
            MassLower.SetValidateNotifyError(v => v < MassMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassUpper.Value ? "Too large" : null);
            MassUpper.SetValidateNotifyError(v => v < MassLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassMax ? "Too large" : null);

            RtMin = this.model.RtMin;
            RtMax = this.model.RtMax;
            RtLower = new ReactiveProperty<double>(RtMin).AddTo(Disposables);
            RtUpper = new ReactiveProperty<double>(RtMax).AddTo(Disposables);
            RtLower.SetValidateNotifyError(v => v < RtMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > RtUpper.Value ? "Too large" : null);
            RtUpper.SetValidateNotifyError(v => v < RtLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > RtMax ? "Too large" : null);
            ProteinFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            ProteinFilterKeywords = ProteinFilterKeyword.Select(w => w.Split())
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            MetaboliteFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            MetaboliteFilterKeywords = MetaboliteFilterKeyword.Select(w => w.Split())
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CommentFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            CommentFilterKeywords = CommentFilterKeyword.Select(w => w.Split())
                .ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            new[]
            {
                MassLower.ToUnit(),
                MassUpper.ToUnit(),
                RtLower.ToUnit(),
                RtUpper.ToUnit(),
                ProteinFilterKeywords.ToUnit(),
                MetaboliteFilterKeywords.ToUnit(),
                CommentFilterKeywords.ToUnit(),
                DisplayFilters.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnDispatcher()
            .Subscribe(_ => Ms1Spots?.Refresh())
            .AddTo(Disposables);

            var classBrush = parameter.Select(p => new KeyBrushMapper<BarItem, string>(
                p.ProjectParam.ClassnameToColorBytes
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                ),
                item => item.Class,
                Colors.Blue));

            Ms1Spots = CollectionViewSource.GetDefaultView(this.model.Ms1Spots);

            PlotViewModel = new AlignmentPeakPlotViewModel(this.model.PlotModel, SelectedBrush).AddTo(Disposables);

            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               model.Parameter.ProjectParam.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Blue);

            //var lowerSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
            //   model.Parameter.ProjectParam.SpectrumCommentToColorBytes
            //   .ToDictionary(
            //       kvp => kvp.Key,
            //       kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
            //   ),
            //   item => item.ToString(),
            //   Colors.Red);
            var lowerSpecBrush = new DelegateBrushMapper<SpectrumComment>(
                comment => {
                    var commentString = comment.ToString();
                    if (model.Parameter.ProjectParam.SpectrumCommentToColorBytes.TryGetValue(commentString, out var color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else if ((comment & SpectrumComment.doublebond) == SpectrumComment.doublebond
                        && model.Parameter.ProjectParam.SpectrumCommentToColorBytes.TryGetValue(SpectrumComment.doublebond.ToString(), out color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else {
                        return Colors.Red;
                    }
                },
                true);

            Ms2SpectrumViewModel = new MsSpectrumViewModel(this.model.Ms2SpectrumModel,
                upperSpectrumBrushSource: Observable.Return(upperSpecBrush),
                lowerSpectrumBrushSource: Observable.Return(lowerSpecBrush)).AddTo(Disposables);
            BarChartViewModel = new BarChartViewModel(this.model.BarChartModel, brushSource: classBrush).AddTo(Disposables);
            AlignmentEicViewModel = new AlignmentEicViewModel(this.model.AlignmentEicModel).AddTo(Disposables);
            AlignmentSpotTableViewModel = new LcmsAlignmentSpotTableViewModel(
                this.model.AlignmentSpotTableModel,
                Observable.Return(model.BarItemsLoader),
                MassLower,
                MassUpper,
                RtLower,
                RtUpper,
                MetaboliteFilterKeyword,
                CommentFilterKeyword)
                .AddTo(Disposables);
            ProteomicsAlignmentTableViewModel = new LcmsProteomicsAlignmentTableViewModel(
                this.model.AlignmentSpotTableModel,
                Observable.Return(model.BarItemsLoader),
                MassLower,
                MassUpper,
                RtLower,
                RtUpper,
                ProteinFilterKeyword,
                MetaboliteFilterKeyword,
                CommentFilterKeyword)
                .AddTo(Disposables);

            SearchCompoundCommand = this.model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);
        }

        private readonly LcmsAlignmentModel model;
        private readonly IWindowService<ViewModel.CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IWindowService<PeakSpotTableViewModelBase> proteomicsTableService;

        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }
        public ReactivePropertySlim<IBrushMapper<AlignmentSpotPropertyModel>> SelectedBrush { get; }

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

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public AlignmentPeakPlotViewModel PlotViewModel { get; }
        public MsSpectrumViewModel Ms2SpectrumViewModel { get; }
        public BarChartViewModel BarChartViewModel { get; }
        public AlignmentEicViewModel AlignmentEicViewModel { get; }
        public LcmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel { get; }
        public LcmsProteomicsAlignmentTableViewModel ProteomicsAlignmentTableViewModel { get; }

        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }

        public double RtMin { get; }
        public double RtMax { get; }
        public ReactiveProperty<double> RtLower { get; }
        public ReactiveProperty<double> RtUpper { get; }
        public ReactivePropertySlim<string> ProteinFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> ProteinFilterKeywords { get; }
        public ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> MetaboliteFilterKeywords { get; }
        public ReactivePropertySlim<string> CommentFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> CommentFilterKeywords { get; }

        public bool RefMatchedChecked {
            get => ReadDisplayFilters(DisplayFilter.RefMatched);
            set => SetDisplayFilters(DisplayFilter.RefMatched, value);
        }

        public bool SuggestedChecked {
            get => ReadDisplayFilters(DisplayFilter.Suggested);
            set => SetDisplayFilters(DisplayFilter.Suggested, value);
        }

        public bool UnknownChecked {
            get => ReadDisplayFilters(DisplayFilter.Unknown);
            set => SetDisplayFilters(DisplayFilter.Unknown, value);
        }

        public bool Ms2AcquiredChecked {
            get => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
            set => SetDisplayFilters(DisplayFilter.Ms2Acquired, value);
        }

        public bool MolecularIonChecked {
            get => ReadDisplayFilters(DisplayFilter.MolecularIon);
            set => SetDisplayFilters(DisplayFilter.MolecularIon, value);
        }

        public bool UniquesIonsChecked {
            get => ReadDisplayFilters(DisplayFilter.UniqueIons);
            set => SetDisplayFilters(DisplayFilter.UniqueIons, value);
        }

        public bool BlankFilterChecked {
            get => ReadDisplayFilters(DisplayFilter.Blank);
            set => SetDisplayFilters(DisplayFilter.Blank, value);
        }

        public bool ManuallyModifiedChecked {
            get => ReadDisplayFilters(DisplayFilter.ManuallyModified);
            set => SetDisplayFilters(DisplayFilter.ManuallyModified, value);
        }

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set {
                if (SetProperty(ref displayFilters, value))
                    Ms1Spots?.Refresh();
            }
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        private bool ReadDisplayFilters(DisplayFilter flags) {
            return (flags & DisplayFilters) != 0;
        }

        private void WriteDisplayFilters(DisplayFilter flags, bool value) {
            displayFilters.Write(flags, value);
        }

        private bool SetDisplayFilters(DisplayFilter flags, bool value) {
            if (ReadDisplayFilters(flags) != value) {
                WriteDisplayFilters(flags, value);
                OnPropertyChanged(nameof(DisplayFilters));
                return true;
            }
            return false;
        }

        bool PeakFilter(object obj) {
            if (obj is AlignmentSpotPropertyModel spot) {
                return AnnotationFilter(spot)
                    && MzFilter(spot)
                    && RtFilter(spot)
                    && (!Ms2AcquiredChecked || spot.IsMsmsAssigned)
                    && (!MolecularIonChecked || spot.IsBaseIsotopeIon)
                    && (!BlankFilterChecked || !spot.IsBlankFiltered)
                    && (!UniquesIonsChecked || spot.IsFragmentQueryExisted)
                    && (!ManuallyModifiedChecked || spot.innerModel.IsManuallyModifiedForAnnotation)
                    && ProteinFilter(spot, ProteinFilterKeywords.Value)
                    && MetaboliteFilter(spot, MetaboliteFilterKeywords.Value)
                    && CommentFilter(spot, CommentFilterKeywords.Value);
            }
            return false;
        }

        bool AnnotationFilter(AlignmentSpotPropertyModel spot) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && spot.IsRefMatched(model.MatchResultEvaluator)
                || SuggestedChecked && spot.IsSuggested(model.MatchResultEvaluator)
                || UnknownChecked && spot.IsUnknown;
        }

        bool MzFilter(AlignmentSpotPropertyModel spot) {
            return MassLower.Value <= spot.MassCenter
                && spot.MassCenter <= MassUpper.Value;
        }

        bool RtFilter(AlignmentSpotPropertyModel spot) {
            return RtLower.Value <= spot.TimesCenter
                && spot.TimesCenter <= RtUpper.Value;
        }

        bool ProteinFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Protein?.Contains(keyword) ?? true);
        }

        bool MetaboliteFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Name.Contains(keyword));
        }

        bool CommentFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Comment.Contains(keyword));
        }

        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using (var csm = model.CreateCompoundSearchModel()) {
                if (csm is null) {
                    return;
                }
                using (var vm = new LcmsCompoundSearchViewModel(csm)) {
                    compoundSearchService.ShowDialog(vm);
                }
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            if (model.Parameter.TargetOmics == CompMs.Common.Enum.TargetOmics.Proteomics) {
                proteomicsTableService.Show(ProteomicsAlignmentTableViewModel);
            }
            else {
                peakSpotTableService.Show(AlignmentSpotTableViewModel);
            }
        }

        public int FocusID {
            get => focusID;
            set => SetProperty(ref focusID, value);
        }
        private int focusID;

        public double FocusRt {
            get => focusRt;
            set => SetProperty(ref focusRt, value);
        }
        private double focusRt;

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        public DelegateCommand FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand(FocusByID));
        private DelegateCommand focusByIDCommand;

        private void FocusByID() {
            var focus = model.Ms1Spots.FirstOrDefault(peak => peak.innerModel.MasterAlignmentID == FocusID);
            if (focus is null) {
                return;
            }
            Ms1Spots.MoveCurrentTo(focus);
            PlotViewModel?.HorizontalAxis?.Focus(focus.TimesCenter - RtTol, focus.TimesCenter + RtTol);
            PlotViewModel?.VerticalAxis?.Focus(focus.MassCenter - MzTol, focus.MassCenter + MzTol);
        }

        public DelegateCommand FocusByRtCommand => focusByRtCommand ?? (focusByRtCommand = new DelegateCommand(FocusByRt));
        private DelegateCommand focusByRtCommand;

        private static readonly double RtTol = 0.5;
        private void FocusByRt() {
            PlotViewModel?.HorizontalAxis?.Focus(FocusRt - RtTol, FocusRt + RtTol);
        }

        public DelegateCommand FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand(FocusByMz));
        private DelegateCommand focusByMzCommand;

        private static readonly double MzTol = 20;
        private void FocusByMz() {
            PlotViewModel?.VerticalAxis?.Focus(FocusMz - MzTol, FocusMz + MzTol);
        }

        private void SaveSpectra(Window owner) {
            var sfd = new SaveFileDialog {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp|MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf|MSFINDER format(*.mat)|*.mat;|SIRIUS format(*.ms)|*.ms",
                RestoreDirectory = true,
                AddExtension = true,
            };

            if (sfd.ShowDialog(owner) == true) {
                var filename = sfd.FileName;
                this.model.SaveSpectra(filename);
            }
        }

        private bool CanSaveSpectra(Window owner) {
            return this.model.CanSaveSpectra();
        }
        public void SaveProject() {
            model.SaveProject();
        }

        public DelegateCommand<Window> NormalizeCommand => normalizeCommand ?? (normalizeCommand = new DelegateCommand<Window>(Normalize));

        private DelegateCommand<Window> normalizeCommand;

        private void Normalize(Window owner) {
            var parameter = model.Parameter;
            using (var vm = new NormalizationSetViewModel(model.Container, model.DataBaseMapper, model.MatchResultEvaluator, parameter)) {
                var view = new NormalizationSetView {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                view.ShowDialog();
            }
        }
    }
}
