using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class AlignmentParameterSettingViewModel : ViewModelBase
    {
        public AlignmentParameterSettingViewModel(AlignmentParameterSettingModel model) {
            Model = model;

            AlignmentResultFileName = Model.ToReactivePropertyAsSynchronized(m => m.AlignmentResultFileName)
                .SetValidateAttribute(() => AlignmentResultFileName)
                .AddTo(Disposables);
            ReferenceFile = Model.ToReactivePropertyAsSynchronized(m => m.ReferenceFile)
                .SetValidateAttribute(() => ReferenceFile)
                .AddTo(Disposables);
            EqualityParameterSettings = Model.EqualityParameterSettings
                .Select(PeakEqualityParameterSettingViewModelFactory.Create)
                .ToList().AsReadOnly();
            PeakCountFilter = Model.ToReactivePropertyAsSynchronized(
                m => m.PeakCountFilter,
                m => m.ToString(),
                vm => float.Parse(vm))
                .SetValidateAttribute(() => PeakCountFilter)
                .AddTo(Disposables);
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = Model.ToReactivePropertySlimAsSynchronized(m => m.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange).AddTo(Disposables);
            BlankFiltering = Model.ToReactivePropertySlimAsSynchronized(m => m.BlankFiltering).AddTo(Disposables);
            FoldChangeForBlankFiltering = Model.ToReactivePropertyAsSynchronized(
                m => m.FoldChangeForBlankFiltering,
                m => m.ToString(),
                vm => float.Parse(vm))
                .SetValidateAttribute(() => FoldChangeForBlankFiltering)
                .AddTo(Disposables);
            IsKeepRefMatchedMetaboliteFeatures = Model.ToReactivePropertySlimAsSynchronized(m => m.IsKeepRefMatchedMetaboliteFeatures).AddTo(Disposables);
            IsKeepSuggestedMetaboliteFeatures = Model.ToReactivePropertySlimAsSynchronized(m => m.IsKeepSuggestedMetaboliteFeatures).AddTo(Disposables);
            IsKeepRemovableFeaturesAndAssignedTagForChecking = Model.ToReactivePropertySlimAsSynchronized(m => m.IsKeepRemovableFeaturesAndAssignedTagForChecking).AddTo(Disposables);
            IsForceInsertForGapFilling = Model.ToReactivePropertySlimAsSynchronized(m => m.IsForceInsertForGapFilling).AddTo(Disposables);
        }

        public AlignmentParameterSettingModel Model { get; }

        [Required(ErrorMessage = "Result file name required.")]
        public ReactiveProperty<string> AlignmentResultFileName { get; }

        public ReadOnlyCollection<AnalysisFileBean> AnalysisFiles => Model.AnalysisFiles;

        [Required(ErrorMessage = "Reference file is required.")]
        public ReactiveProperty<AnalysisFileBean> ReferenceFile { get; }

        public ReadOnlyCollection<PeakEqualityParameterSettingViewModel> EqualityParameterSettings { get; }

        [Required(ErrorMessage = "Peak count filter required.")]
        [RegularExpression(@"0?\.\d+", ErrorMessage = "Invalid format.")]
        [Range(0, 1, ErrorMessage = "Filter value should be positive value.")]
        public ReactiveProperty<string> PeakCountFilter { get; }

        [Required(ErrorMessage = "N% detected threshold required.")]
        [RegularExpression(@"0?\.\d+", ErrorMessage = "Invalid format.")]
        [Range(0, 1, ErrorMessage = "Threshold should be positive value.")]
        public ReactiveProperty<string> NPercentDetectedInOneGroup { get; }

        public ReactivePropertySlim<bool> IsRemoveFeatureBasedOnBlankPeakHeightFoldChange { get; }

        public ReactivePropertySlim<BlankFiltering> BlankFiltering { get; }

        [Required(ErrorMessage = "Fold change value for blank filtering required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format.")]
        [Range(0, float.MaxValue, ErrorMessage = "Fold change value should be positive value.")]
        public ReactiveProperty<string> FoldChangeForBlankFiltering { get; }

        public ReactivePropertySlim<bool> IsKeepRefMatchedMetaboliteFeatures { get; }

        public ReactivePropertySlim<bool> IsKeepSuggestedMetaboliteFeatures { get; }

        public ReactivePropertySlim<bool> IsKeepRemovableFeaturesAndAssignedTagForChecking { get; }

        public ReactivePropertySlim<bool> IsForceInsertForGapFilling { get; }
    }
}
