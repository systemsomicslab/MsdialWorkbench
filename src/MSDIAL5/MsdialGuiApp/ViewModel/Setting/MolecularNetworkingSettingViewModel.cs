using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.Setting;

internal sealed class MolecularNetworkingSettingViewModel : ViewModelBase
{
    private readonly MolecularNetworkingSettingModel _model;

    public MolecularNetworkingSettingViewModel(MolecularNetworkingSettingModel model) {
        _model = model;

        IsAlignSpotViewSelected = model.ToReactivePropertySlimAsSynchronized(m => m.IsAlignSpotViewSelected).AddTo(Disposables);

        RtTolerance = model.ToReactivePropertyAsSynchronized(
            m => m.RtTolerance,
            m => m.ToString(),
            vm => double.Parse(vm),
            ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => RtTolerance).AddTo(Disposables);

        IonCorrelationSimilarityCutoff = model.ToReactivePropertyAsSynchronized(
            m => m.IonCorrelationSimilarityCutOff,
            m => m.ToString(),
            vm => double.Parse(vm),
            ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => IonCorrelationSimilarityCutoff).AddTo(Disposables);

        SpectrumSimilarityCutOff = model.ToReactivePropertyAsSynchronized(
            m => m.SpectrumSimilarityCutOff,
            m => m.ToString(),
            vm => double.Parse(vm),
            ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => SpectrumSimilarityCutOff).AddTo(Disposables);

        RelativeAbundanceCutoff = model.ToReactivePropertyAsSynchronized(
            m => m.RelativeAbundanceCutoff,
            m => m.ToString(),
            vm => double.Parse(vm),
            ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => RelativeAbundanceCutoff).AddTo(Disposables);

        AbsoluteAbundanceCutoff = model.ToReactivePropertyAsSynchronized(
            m => m.AbsluteAbundanceCutoff,
            m => m.ToString(),
            vm => double.Parse(vm),
            ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => AbsoluteAbundanceCutoff).AddTo(Disposables);

        MassTolerance = model.ToReactivePropertyAsSynchronized(
            m => m.MassTolerance,
            m => m.ToString(),
            vm => double.Parse(vm),
            ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => MassTolerance).AddTo(Disposables);

        MinimumPeakMatch = model.ToReactivePropertyAsSynchronized(
           m => m.MinimumPeakMatch,
           m => m.ToString(),
           vm => double.Parse(vm),
           ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => MinimumPeakMatch).AddTo(Disposables);

        MaxEdgeNumberPerNode = model.ToReactivePropertyAsSynchronized(
           m => m.MaxEdgeNumberPerNode,
           m => m.ToString(),
           vm => double.Parse(vm),
           ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => MaxEdgeNumberPerNode).AddTo(Disposables);

        MaxPrecursorDifference = model.ToReactivePropertyAsSynchronized(
           m => m.MaxPrecursorDifference,
           m => m.ToString(),
           vm => double.Parse(vm),
           ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => MaxPrecursorDifference).AddTo(Disposables);

        MaxPrecursorDifferenceAsPercent = model.ToReactivePropertyAsSynchronized(
           m => m.MaxPrecursorDifferenceAsPercent,
           m => m.ToString(),
           vm => double.Parse(vm),
           ignoreValidationErrorValue: true
        ).SetValidateAttribute(() => MaxPrecursorDifferenceAsPercent).AddTo(Disposables);

        IsExportIonCorrelation = model.ToReactivePropertySlimAsSynchronized(m => m.IsExportIonCorrelation).AddTo(Disposables);

        MsmsSimilarityCalc = model.ToReactivePropertySlimAsSynchronized(m => m.MsmsSimilarityCalc).AddTo(Disposables);

        UseCurrentFiltering = model.ToReactivePropertySlimAsSynchronized(m => m.UseCurrentFiltering).AddTo(Disposables);

        ObserveHasErrors = new[]
        {
            RtTolerance.ObserveHasErrors,
            IonCorrelationSimilarityCutoff.ObserveHasErrors,
            SpectrumSimilarityCutOff.ObserveHasErrors,
            RelativeAbundanceCutoff.ObserveHasErrors,
            AbsoluteAbundanceCutoff.ObserveHasErrors,
            MassTolerance.ObserveHasErrors,
            MinimumPeakMatch.ObserveHasErrors,
            MaxEdgeNumberPerNode.ObserveHasErrors,
            MaxPrecursorDifference.ObserveHasErrors,
            MaxPrecursorDifferenceAsPercent.ObserveHasErrors,
        }.CombineLatestValuesAreAllFalse()
        .Inverse()
        .ToReadOnlyReactivePropertySlim()
        .AddTo(Disposables);
    }

    public ReactivePropertySlim<bool> IsAlignSpotViewSelected { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, double.MaxValue, ErrorMessage = "Tolerance should be positive value.")]
    public ReactiveProperty<string> RtTolerance { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, 100d, ErrorMessage = "Similarity cutoff value must fall within the range of 0 to 100.")]
    public ReactiveProperty<string> IonCorrelationSimilarityCutoff { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, 100d, ErrorMessage = "Similarity cutoff value must fall within the range of 0 to 100.")]
    public ReactiveProperty<string> SpectrumSimilarityCutOff { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, 100d, ErrorMessage = "The cutoff value must fall within the range of 0 to 100.")]
    public ReactiveProperty<string> RelativeAbundanceCutoff { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, double.MaxValue, ErrorMessage = "Cut-off value should be positive value.")]
    public ReactiveProperty<string> AbsoluteAbundanceCutoff { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, double.MaxValue, ErrorMessage = "Tolerance should be positive value.")]
    public ReactiveProperty<string> MassTolerance { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, double.MaxValue, ErrorMessage = "Peak match count should be positive value.")]
    public ReactiveProperty<string> MinimumPeakMatch { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, int.MaxValue, ErrorMessage = "The number should be positive value.")]
    public ReactiveProperty<string> MaxEdgeNumberPerNode { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, double.MaxValue, ErrorMessage = "The difference value should be positive value.")]
    public ReactiveProperty<string> MaxPrecursorDifference { get; }

    [Required(ErrorMessage = "Required field")]
    [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
    [Range(0d, double.MaxValue, ErrorMessage = "The percent value should be positive value.")]
    public ReactiveProperty<string> MaxPrecursorDifferenceAsPercent { get; }

    public ReactivePropertySlim<bool> IsExportIonCorrelation { get; }

    public ReactivePropertySlim<MsmsSimilarityCalc> MsmsSimilarityCalc { get; }

    public ReactivePropertySlim<bool> UseCurrentFiltering { get; }

    public ReadOnlyReactivePropertySlim<bool> AvailableFileResult => _model.AvailableFileResult;
    public ReadOnlyReactivePropertySlim<bool> AvailableAlignmentResult => _model.AvailableAlignmentResult;
    public ReadOnlyReactivePropertySlim<bool> AvailableIonEdgeExport => _model.AvailableIonEdge;

    public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
}
