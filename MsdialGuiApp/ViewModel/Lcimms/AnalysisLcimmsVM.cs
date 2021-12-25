using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    class AnalysisLcimmsVM : AnalysisFileViewModel
    {

        public AnalysisLcimmsVM(
            LcimmsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
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

            this.model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;

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

            DriftMin = this.model.DriftMin;
            DriftMax = this.model.DriftMax;
            DriftLower = new ReactiveProperty<double>(DriftMin).AddTo(Disposables);
            DriftUpper = new ReactiveProperty<double>(DriftMax).AddTo(Disposables);
            DriftLower.SetValidateNotifyError(v => v < DriftMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftUpper.Value ? "Too large" : null);
            DriftUpper.SetValidateNotifyError(v => v < DriftLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftMax ? "Too large" : null);

            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            new[]
            {
                MassLower.ToUnit(),
                MassUpper.ToUnit(),
                RtLower.ToUnit(),
                RtUpper.ToUnit(),
                DriftLower.ToUnit(),
                DriftUpper.ToUnit(),
                CommentFilterKeyword.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                DisplayFilters.ToUnit(),
                AmplitudeLowerValue.ToUnit(),
                AmplitudeUpperValue.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnDispatcher()
            .Subscribe(_ => Ms1PeaksView?.Refresh())
            .AddTo(Disposables);

            var rtHAxis = this.model.RtMzPlotModel
                .ObserveProperty(m => m.HorizontalRange)
                .ToReactiveAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
            var dtHAxis = this.model.DtMzPlotModel
                .ObserveProperty(m => m.HorizontalRange)
                .ToReactiveAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
            var vAxis = this.model.RtMzPlotModel
                .ObserveProperty(m => m.VerticalRange)
                .ToReactiveAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);

            RtMzPlotViewModel = new AnalysisPeakPlotViewModel(
                this.model.RtMzPlotModel,
                brushSource: Observable.Return(this.model.Brush),
                horizontalAxis: rtHAxis,
                verticalAxis: vAxis).AddTo(Disposables);
            RtEicViewModel = new EicViewModel(
                this.model.RtEicModel,
                horizontalAxis: rtHAxis).AddTo(Disposables);

            DtMzPlotViewModel = new AnalysisPeakPlotViewModel(
                this.model.DtMzPlotModel,
                brushSource: Observable.Return(this.model.Brush),
                horizontalAxis: dtHAxis,
                verticalAxis: vAxis).AddTo(Disposables);
            DtEicViewModel = new EicViewModel(
                this.model.DtEicModel,
                horizontalAxis: dtHAxis).AddTo(Disposables);

            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
                this.model.parameter.ProjectParam.SpectrumCommentToColorBytes
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                ),
                item => item.ToString(),
                Colors.Blue);

            var lowerSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               this.model.parameter.ProjectParam.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Red);

            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(this.model.Ms2SpectrumModel,
                upperSpectrumBrushSource: Observable.Return(upperSpecBrush),
                lowerSpectrumBrushSource: Observable.Return(lowerSpecBrush)).AddTo(Disposables);
            SurveyScanViewModel = new SurveyScanViewModel(
                this.model.SurveyScanModel,
                horizontalAxis: vAxis).AddTo(Disposables);
            // PeakSpotTableViewModelBase = new LcimmsAnalysisViewModel

            SearchCompoundCommand = new[]
            {
                Target.Select(t => t?.InnerModel != null),
                model.MsdecResult.Select(r => r != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(SearchCompound)
            .AddTo(Disposables);

            Ms1PeaksView.Filter += PeakFilter;
        }

        private readonly LcimmsAnalysisModel model;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public AnalysisPeakPlotViewModel RtMzPlotViewModel { get; private set; }
        public EicViewModel RtEicViewModel { get; private set; }
        public AnalysisPeakPlotViewModel DtMzPlotViewModel { get; private set; }
        public EicViewModel DtEicViewModel { get; private set; }
        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel { get; private set; }
        public SurveyScanViewModel SurveyScanViewModel { get; private set; }

        public bool RefMatchedChecked => ReadDisplayFilters(DisplayFilter.RefMatched);
        public bool SuggestedChecked => ReadDisplayFilters(DisplayFilter.Suggested);
        public bool UnknownChecked => ReadDisplayFilters(DisplayFilter.Unknown);
        public bool Ms2AcquiredChecked => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
        public bool MolecularIonChecked => ReadDisplayFilters(DisplayFilter.MolecularIon);
        public bool CcsChecked => ReadDisplayFilters(DisplayFilter.CcsMatched);
        public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);
        public bool ManuallyModifiedChecked => ReadDisplayFilters(DisplayFilter.ManuallyModified);

        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }
        public double RtMin { get; }
        public double RtMax { get; }
        public ReactiveProperty<double> RtLower { get; }
        public ReactiveProperty<double> RtUpper { get; }
        public double DriftMin { get; }
        public double DriftMax { get; }
        public ReactiveProperty<double> DriftLower { get; }
        public ReactiveProperty<double> DriftUpper { get; }


        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureModel peak) {
                return AnnotationFilter(peak)
                    && AmplitudeFilter(peak)
                    && MzFilter(peak)
                    && RtFilter(peak)
                    && DriftFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0)
                    && (!ManuallyModifiedChecked || peak.InnerModel.IsManuallyModifiedForAnnotation)
                    && MetaboliteFilter(peak, MetaboliteFilterKeywords.Value)
                    && CommentFilter(peak, CommentFilterKeywords.Value);
            }
            return false;
        }

        bool AnnotationFilter(ChromatogramPeakFeatureModel peak) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && peak.IsRefMatched(model.DataBaseMapper)
                || SuggestedChecked && peak.IsSuggested(model.DataBaseMapper)
                || UnknownChecked && peak.IsUnknown;
        }

        bool MzFilter(ChromatogramPeakFeatureModel peak) {
            return MassLower.Value <= peak.Mass && peak.Mass <= MassUpper.Value;
        }

        bool RtFilter(ChromatogramPeakFeatureModel peak) {
            return RtLower.Value <= peak.InnerModel.ChromXs.RT.Value && peak.InnerModel.ChromXs.RT.Value <= RtUpper.Value;
        }

        bool DriftFilter(ChromatogramPeakFeatureModel peak) {
            return DriftLower.Value <= peak.InnerModel.ChromXs.Drift.Value && peak.InnerModel.ChromXs.Drift.Value <= DriftUpper.Value;
        }

        public DelegateCommand<IAxisManager> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<IAxisManager>(FocusByID));
        private DelegateCommand<IAxisManager> focusByIDCommand;

        private void FocusByID(IAxisManager axis) {
            model.FocusByID(axis);
        }

        public DelegateCommand<IAxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<IAxisManager>(FocusByMz));
        private DelegateCommand<IAxisManager> focusByMzCommand;
        private void FocusByMz(IAxisManager axis) {
            model.FocusByMz(axis);
        }

        public ReactiveCommand SearchCompoundCommand { get; }

        public void SearchCompound() {
            using (var model = new CompoundSearchModel<ChromatogramPeakFeature>(
                this.model.AnalysisFile,
                Target.Value.InnerModel,
                this.model.MsdecResult.Value,
                null,
                null))
            using (var vm = new CompoundSearchVM(model)) {
                compoundSearchService.ShowDialog(vm);
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            // peakSpotTableService.Show(PeakTableViewModel);
        }
    }
}