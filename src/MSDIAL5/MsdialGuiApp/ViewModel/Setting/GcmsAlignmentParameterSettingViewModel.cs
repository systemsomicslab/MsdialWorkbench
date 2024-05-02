using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    internal sealed class GcmsAlignmentParameterSettingViewModel : ViewModelBase, ISettingViewModel
    {
        private readonly GcmsAlignmentParameterSettingModel _model;
        private readonly Subject<Unit> _decide;

        public GcmsAlignmentParameterSettingViewModel(GcmsAlignmentParameterSettingModel model, IObservable<bool> isEnabled) {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            IsReadOnly = model.IsReadOnly;

            AlignmentResultFileName = model.ToReactivePropertyAsSynchronized(m => m.AlignmentResultFileName, ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => AlignmentResultFileName)
                .AddTo(Disposables);
            ReferenceFile = model.ToReactivePropertyAsSynchronized(m => m.ReferenceFile, ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => ReferenceFile)
                .AddTo(Disposables);
            RtEqualityParameterSetting = PeakEqualityParameterSettingViewModelFactory.Create(model.RtEqualityParameter)?.AddTo(Disposables);
            RiEqualityParameterSetting = PeakEqualityParameterSettingViewModelFactory.Create(model.RiEqualityParameter)?.AddTo(Disposables);
            EiEqualityParameterSetting = PeakEqualityParameterSettingViewModelFactory.Create(model.EiEqualityParameter)?.AddTo(Disposables);
            PeakCountFilter = model.ToReactivePropertyAsSynchronized(
                m => m.PeakCountFilter,
                m => (m * 100).ToString(),
                vm => float.Parse(vm) / 100,
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => PeakCountFilter)
                .AddTo(Disposables);
            NPercentDetectedInOneGroup = model.ToReactivePropertyAsSynchronized(
                m => m.NPercentDetectedInOneGroup,
                m => (m * 100).ToString(),
                vm => float.Parse(vm) / 100,
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => NPercentDetectedInOneGroup)
                .AddTo(Disposables);
            IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = model.ToReactivePropertySlimAsSynchronized(m => m.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange).AddTo(Disposables);
            BlankFiltering = model.ToReactivePropertySlimAsSynchronized(m => m.BlankFiltering).AddTo(Disposables);
            FoldChangeForBlankFiltering = model.ToReactivePropertyAsSynchronized(
                m => m.FoldChangeForBlankFiltering,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => FoldChangeForBlankFiltering)
                .AddTo(Disposables);
            IsKeepRefMatchedMetaboliteFeatures = model.ToReactivePropertySlimAsSynchronized(m => m.IsKeepRefMatchedMetaboliteFeatures).AddTo(Disposables);
            IsKeepRemovableFeaturesAndAssignedTagForChecking = model.ToReactivePropertySlimAsSynchronized(m => m.IsKeepRemovableFeaturesAndAssignedTagForChecking).AddTo(Disposables);
            IsForceInsertForGapFilling = model.ToReactivePropertySlimAsSynchronized(m => m.IsForceInsertForGapFilling).AddTo(Disposables);
            ShouldRunAlignment = model.ToReactivePropertySlimAsSynchronized(m => m.ShouldRunAlignment).AddTo(Disposables);

            IndexType = model.ToReactivePropertySlimAsSynchronized(m => m.IndexType).AddTo(Disposables);
            UseRI = model.ToReactivePropertySlimAsSynchronized(
                m => m.IndexType,
                op => op.Select(p => p == AlignmentIndexType.RI),
                op => op.Where(p => p).ToConstant(AlignmentIndexType.RI)
            ).AddTo(Disposables);
            UseRT = model.ToReactivePropertySlimAsSynchronized(
                m => m.IndexType,
                op => op.Select(p => p == AlignmentIndexType.RT),
                op => op.Where(p => p).ToConstant(AlignmentIndexType.RT)
            ).AddTo(Disposables);
            IsIdentificationOnlyPerformedForAlignmentFile = model.ToReactivePropertySlimAsSynchronized(m => m.IsIdentificationOnlyPerformedForAlignmentFile).AddTo(Disposables);
            IsRepresentativeQuantMassBasedOnBasePeakMz = model.ToReactivePropertySlimAsSynchronized(m => m.IsRepresentativeQuantMassBasedOnBasePeakMz).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                AlignmentResultFileName.ObserveHasErrors,
                ReferenceFile.ObserveHasErrors,
                RtEqualityParameterSetting?.ObserveHasErrors ?? Observable.Return(false),
                RiEqualityParameterSetting?.ObserveHasErrors ?? Observable.Return(false),
                EiEqualityParameterSetting?.ObserveHasErrors ?? Observable.Return(false),
                PeakCountFilter.ObserveHasErrors,
                FoldChangeForBlankFiltering.ObserveHasErrors,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            ObserveChanges = new[]
            {
                AlignmentResultFileName.ToUnit(),
                ReferenceFile.ToUnit(),
                RtEqualityParameterSetting?.ObserveChanges ?? Observable.Never<Unit>(),
                RiEqualityParameterSetting?.ObserveChanges ?? Observable.Never<Unit>(),
                EiEqualityParameterSetting?.ObserveChanges ?? Observable.Never<Unit>(),
                PeakCountFilter.ToUnit(),
                IsRemoveFeatureBasedOnBlankPeakHeightFoldChange.ToUnit(),
                BlankFiltering.ToUnit(),
                FoldChangeForBlankFiltering.ToUnit(),
                IsKeepRefMatchedMetaboliteFeatures.ToUnit(),
                IsKeepRemovableFeaturesAndAssignedTagForChecking.ToUnit(),
                IsForceInsertForGapFilling.ToUnit(),
            }.Merge();

            _decide = new Subject<Unit>().AddTo(Disposables);
            var changes = ObserveChanges.TakeFirstAfterEach(_decide);
            ObserveChangeAfterDecision = new[]
            {
                changes.ToConstant(true),
                _decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public bool IsReadOnly { get; }

        [Required(ErrorMessage = "Result file name required.")]
        public ReactiveProperty<string> AlignmentResultFileName { get; }

        public ReadOnlyCollection<AnalysisFileBeanModel> AnalysisFiles => _model.AnalysisFiles;

        [Required(ErrorMessage = "Reference file is required.")]
        public ReactiveProperty<AnalysisFileBeanModel> ReferenceFile { get; }

        public ReactivePropertySlim<AlignmentIndexType> IndexType { get; }
        public ReactivePropertySlim<bool> UseRI { get; }
        public ReactivePropertySlim<bool> UseRT { get; }

        public PeakEqualityParameterSettingViewModel? RtEqualityParameterSetting { get; }
        public PeakEqualityParameterSettingViewModel? RiEqualityParameterSetting { get; }
        public PeakEqualityParameterSettingViewModel? EiEqualityParameterSetting { get; }

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
        public ReactivePropertySlim<bool> IsKeepRemovableFeaturesAndAssignedTagForChecking { get; }
        public ReactivePropertySlim<bool> IsForceInsertForGapFilling { get; }
        public ReactivePropertySlim<bool> IsIdentificationOnlyPerformedForAlignmentFile { get; }
        public ReactivePropertySlim<bool> IsRepresentativeQuantMassBasedOnBasePeakMz { get; }

        public ReactivePropertySlim<bool> ShouldRunAlignment { get; }

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        public IObservable<Unit> ObserveChanges { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected) {
            _decide.OnNext(Unit.Default);
            return null;
        }
    }
}
