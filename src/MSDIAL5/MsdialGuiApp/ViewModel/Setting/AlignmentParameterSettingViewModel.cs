using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class AlignmentParameterSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public AlignmentParameterSettingViewModel(AlignmentParameterSettingModel model, IObservable<bool> isEnabled) {
            Model = model;
            IsReadOnly = model.IsReadOnly;

            AlignmentResultFileName = Model.ToReactivePropertyAsSynchronized(m => m.AlignmentResultFileName, ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => AlignmentResultFileName)
                .AddTo(Disposables);
            ReferenceFile = Model.ToReactivePropertyAsSynchronized(m => m.ReferenceFile, ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => ReferenceFile)
                .AddTo(Disposables);
            EqualityParameterSettings = Model.EqualityParameterSettings
                .ToReadOnlyReactiveCollection(PeakEqualityParameterSettingViewModelFactory.Create)
                .AddTo(Disposables);
            PeakCountFilter = Model.ToReactivePropertyAsSynchronized(
                m => m.PeakCountFilter,
                m => (m * 100).ToString(),
                vm => float.Parse(vm) / 100,
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => PeakCountFilter)
                .AddTo(Disposables);
            NPercentDetectedInOneGroup = Model.ToReactivePropertyAsSynchronized(
                m => m.NPercentDetectedInOneGroup,
                m => (m * 100).ToString(),
                vm => float.Parse(vm) / 100,
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => NPercentDetectedInOneGroup)
                .AddTo(Disposables);
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = Model.ToReactivePropertySlimAsSynchronized(m => m.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange).AddTo(Disposables);
            BlankFiltering = Model.ToReactivePropertySlimAsSynchronized(m => m.BlankFiltering).AddTo(Disposables);
            FoldChangeForBlankFiltering = Model.ToReactivePropertyAsSynchronized(
                m => m.FoldChangeForBlankFiltering,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => FoldChangeForBlankFiltering)
                .AddTo(Disposables);
            IsKeepRefMatchedMetaboliteFeatures = Model.ToReactivePropertySlimAsSynchronized(m => m.IsKeepRefMatchedMetaboliteFeatures).AddTo(Disposables);
            IsKeepSuggestedMetaboliteFeatures = Model.ToReactivePropertySlimAsSynchronized(m => m.IsKeepSuggestedMetaboliteFeatures).AddTo(Disposables);
            IsKeepRemovableFeaturesAndAssignedTagForChecking = Model.ToReactivePropertySlimAsSynchronized(m => m.IsKeepRemovableFeaturesAndAssignedTagForChecking).AddTo(Disposables);
            IsForceInsertForGapFilling = Model.ToReactivePropertySlimAsSynchronized(m => m.IsForceInsertForGapFilling).AddTo(Disposables);
            ShouldRunAlignment = Model.ToReactivePropertySlimAsSynchronized(m => m.ShouldRunAlignment).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                AlignmentResultFileName.ObserveHasErrors,
                ReferenceFile.ObserveHasErrors,
                EqualityParameterSettings.Select(vm => vm?.ObserveHasErrors ?? Observable.Return(false)).CombineLatestValuesAreAnyTrue(),
                PeakCountFilter.ObserveHasErrors,
                NPercentDetectedInOneGroup.ObserveHasErrors,
                FoldChangeForBlankFiltering.ObserveHasErrors,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                AlignmentResultFileName.ToUnit(),
                ReferenceFile.ToUnit(),
                EqualityParameterSettings.Select(vm => vm?.ObserveChanges ?? Observable.Never<Unit>()).Merge(),
                PeakCountFilter.ToUnit(),
                NPercentDetectedInOneGroup.ToUnit(),
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange.ToUnit(),
                BlankFiltering.ToUnit(),
                FoldChangeForBlankFiltering.ToUnit(),
                IsKeepRefMatchedMetaboliteFeatures.ToUnit(),
                IsKeepSuggestedMetaboliteFeatures.ToUnit(),
                IsKeepRemovableFeaturesAndAssignedTagForChecking.ToUnit(),
                IsForceInsertForGapFilling.ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            var changes = ObserveChanges.TakeFirstAfterEach(decide);
            ObserveChangeAfterDecision = new[]
            {
                changes.ToConstant(true),
                decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public AlignmentParameterSettingModel Model { get; }

        public bool IsReadOnly { get; }

        [Required(ErrorMessage = "Result file name required.")]
        public ReactiveProperty<string> AlignmentResultFileName { get; }

        public ReadOnlyCollection<AnalysisFileBean> AnalysisFiles => Model.AnalysisFiles;

        [Required(ErrorMessage = "Reference file is required.")]
        public ReactiveProperty<AnalysisFileBean> ReferenceFile { get; }

        public ReadOnlyReactiveCollection<PeakEqualityParameterSettingViewModel?> EqualityParameterSettings { get; }

        [Required(ErrorMessage = "Peak count filter required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format.")]
        [Range(0, 100, ErrorMessage = "Filter value should be positive value.")]
        public ReactiveProperty<string> PeakCountFilter { get; }

        [Required(ErrorMessage = "N% detected threshold required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid format.")]
        [Range(0, 100, ErrorMessage = "Threshold should be positive value.")]
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

        public ReactivePropertySlim<bool> ShouldRunAlignment { get; }

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        public IObservable<Unit> ObserveChanges { get; }

        private readonly Subject<Unit> decide;
        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected) {
            decide.OnNext(Unit.Default);
            return null;
        }
    }
}
