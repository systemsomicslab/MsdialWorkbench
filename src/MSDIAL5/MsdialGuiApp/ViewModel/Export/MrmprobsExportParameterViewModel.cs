using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Export
{
    internal sealed class MrmprobsExportParameterViewModel : ViewModelBase
    {
        public MrmprobsExportParameterViewModel(MrmprobsExportParameterModel model)
        {
            Ms1Tolerance = model.ToReactivePropertyAsSynchronized(
                m => m.MpMs1Tolerance,
                op => op.Select(p => p.ToString()),
                op => op.Select(p => float.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => Ms1Tolerance)
                .AddTo(Disposables);
            Ms2Tolerance = model.ToReactivePropertyAsSynchronized(
                m => m.MpMs2Tolerance,
                op => op.Select(p => p.ToString()),
                op => op.Select(p => float.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => Ms2Tolerance)
                .AddTo(Disposables);
            RtTolerance = model.ToReactivePropertyAsSynchronized(
                m => m.MpRtTolerance,
                op => op.Select(p => p.ToString()),
                op => op.Select(p => float.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => RtTolerance)
                .AddTo(Disposables);
            TopN = model.ToReactivePropertyAsSynchronized(
                m => m.MpTopN,
                op => op.Select(p => p.ToString()),
                op => op.Select(p => int.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => TopN)
                .AddTo(Disposables);
            ExportOtherCandidates = model.ToReactivePropertySlimAsSynchronized(m => m.MpIsExportOtherCandidates).AddTo(Disposables);
            IdentificationScoreCutOff = model.ToReactivePropertyAsSynchronized(
                m => m.MpIdentificationScoreCutOff,
                op => op.Select(p => p.ToString()),
                op => op.Select(p => float.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => IdentificationScoreCutOff)
                .AddTo(Disposables);
            IncludeMsLevel1 = model.ToReactivePropertySlimAsSynchronized(m => m.MpIsIncludeMsLevel1).AddTo(Disposables);
            UseMs1LevelForQuant = model.ToReactivePropertySlimAsSynchronized(m => m.MpIsUseMs1LevelForQuant).AddTo(Disposables);
            FocusedSpotOutput = model.ToReactivePropertySlimAsSynchronized(m => m.MpIsFocusedSpotOutput).AddTo(Disposables);
            ReferenceBaseOutput = model.ToReactivePropertySlimAsSynchronized(m => m.MpIsReferenceBaseOutput).AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                Ms1Tolerance.ObserveHasErrors,
                Ms2Tolerance.ObserveHasErrors,
                RtTolerance.ObserveHasErrors,
                TopN.ObserveHasErrors,
                ExportOtherCandidates
                    .Select(p => p ? IdentificationScoreCutOff.ObserveHasErrors : Observable.Return(false))
                    .Switch(),
            }.CombineLatestValuesAreAllFalse().Inverse()
            .ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        [RegularExpression(@"\d*\.?\d+")]
        [Range(1e-5, double.MaxValue, ErrorMessage = "Tolerance must be positive")]
        public ReactiveProperty<string> Ms1Tolerance { get; }
        [RegularExpression(@"\d*\.?\d+")]
        [Range(1e-5, double.MaxValue, ErrorMessage = "Tolerance must be positive")]
        public ReactiveProperty<string> Ms2Tolerance { get; }
        [RegularExpression(@"\d*\.?\d+")]
        [Range(1e-5, double.MaxValue, ErrorMessage = "Tolerance must be positive")]
        public ReactiveProperty<string> RtTolerance { get; }
        [RegularExpression(@"\d+")]
        [Range(0, int.MaxValue, ErrorMessage = "Number of peaks must be positive")]
        public ReactiveProperty<string> TopN { get; }
        public ReactivePropertySlim<bool> ExportOtherCandidates { get; }
        [RegularExpression(@"\d*\.?\d+")]
        [Range(0, 100, ErrorMessage = "Identification score must be between 0 and 100")]
        public ReactiveProperty<string> IdentificationScoreCutOff { get; }
        public ReactivePropertySlim<bool> IncludeMsLevel1 { get; }
        public ReactivePropertySlim<bool> UseMs1LevelForQuant { get; }
        public ReactivePropertySlim<bool> FocusedSpotOutput { get; }
        public ReactivePropertySlim<bool> ReferenceBaseOutput { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }
}
