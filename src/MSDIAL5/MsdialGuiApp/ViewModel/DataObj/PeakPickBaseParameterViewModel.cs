using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;

namespace CompMs.App.Msdial.ViewModel.DataObj
{
    public class PeakPickBaseParameterViewModel : ViewModelBase
    {
        private readonly PeakPickBaseParameterModel _peakPickBaseParameterModel;

        public ReactivePropertySlim<SmoothingMethod> SmoothingMethod { get; }

        [Required(ErrorMessage = "Smoothing level required.")]
        [RegularExpression("[1-9][0-9]*", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> SmoothingLevel { get; }

        [Required(ErrorMessage = "Minimum amplitude required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MinimumAmplitude { get; }

        [Required(ErrorMessage = "Minimum datapoints required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MinimumDatapoints { get; }

        [Required(ErrorMessage = "Mass slicing width required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MassSliceWidth { get; }

        [Required(ErrorMessage = "Retention time begin required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> RetentionTimeBegin { get; }

        [Required(ErrorMessage = "Retention time end required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> RetentionTimeEnd { get; }

        [Required(ErrorMessage = "Ms1 range begin required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MassRangeBegin { get; }

        [Required(ErrorMessage = "Ms1 range end required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MassRangeEnd { get; }

        [Required(ErrorMessage = "Ms2 range begin required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> Ms2MassRangeBegin { get; }

        [Required(ErrorMessage = "Ms2 range end required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> Ms2MassRangeEnd { get; }

        [Required(ErrorMessage = "Ms1 centroid tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> CentroidMs1Tolerance { get; }

        [Required(ErrorMessage = "Ms2 centroid tolerance required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> CentroidMs2Tolerance { get; }

        [Required(ErrorMessage = "Max charge number required.")]
        [RegularExpression("[1-9]+[0-9]*", ErrorMessage = "Invalid format.")]
        public ReactiveProperty<string> MaxChargeNumber { get; }

        public ReactivePropertySlim<bool> IsBrClConsideredForIsotopes { get; }

        public ReadOnlyReactiveCollection<MzSearchQueryViewModel> ExcludedMassList { get; }

        public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

        public PeakPickBaseParameterViewModel(PeakPickBaseParameterModel peakPickBaseParameterModel) {
            _peakPickBaseParameterModel = peakPickBaseParameterModel;

            SmoothingMethod = peakPickBaseParameterModel
                .ToReactivePropertySlimAsSynchronized(m => m.SmoothingMethod)
                .AddTo(Disposables);
            SmoothingLevel = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.SmoothingLevel,
                    m => m.ToString(),
                    vm => int.Parse(vm)
                )
                .SetValidateAttribute(() => SmoothingLevel)
                .AddTo(Disposables);
            MinimumAmplitude = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.MinimumAmplitude,
                    m => m.ToString(),
                    vm => double.Parse(vm)
                )
                .SetValidateAttribute(() => MinimumAmplitude)
                .AddTo(Disposables);
            MinimumDatapoints = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.MinimumDatapoints,
                    m => m.ToString(),
                    vm => double.Parse(vm)
                )
                .SetValidateAttribute(() => MinimumDatapoints)
                .AddTo(Disposables);
            MassSliceWidth = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.MassSliceWidth,
                    m => m.ToString(),
                    vm => float.Parse(vm)
                )
                .SetValidateAttribute(() => MassSliceWidth)
                .AddTo(Disposables);
            RetentionTimeBegin = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.RetentionTimeBegin,
                    m => m.ToString(),
                    vm => float.Parse(vm)
                )
                .SetValidateAttribute(() => RetentionTimeBegin)
                .AddTo(Disposables);
            RetentionTimeEnd = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.RetentionTimeEnd,
                    m => m.ToString(),
                    vm => float.Parse(vm)
                )
                .SetValidateAttribute(() => RetentionTimeEnd)
                .AddTo(Disposables);
            MassRangeBegin = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.MassRangeBegin,
                    m => m.ToString(),
                    vm => float.Parse(vm)
                )
                .SetValidateAttribute(() => MassRangeBegin)
                .AddTo(Disposables);
            MassRangeEnd = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.MassRangeEnd,
                    m => m.ToString(),
                    vm => float.Parse(vm)
                )
                .SetValidateAttribute(() => MassRangeEnd)
                .AddTo(Disposables);
            Ms2MassRangeBegin = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.Ms2MassRangeBegin,
                    m => m.ToString(),
                    vm => float.Parse(vm)
                )
                .SetValidateAttribute(() => Ms2MassRangeBegin)
                .AddTo(Disposables);
            Ms2MassRangeEnd = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.Ms2MassRangeEnd,
                    m => m.ToString(),
                    vm => float.Parse(vm)
                )
                .SetValidateAttribute(() => Ms2MassRangeEnd)
                .AddTo(Disposables);
            CentroidMs1Tolerance = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.CentroidMs1Tolerance,
                    m => m.ToString(),
                    vm => float.Parse(vm)
                )
                .SetValidateAttribute(() => CentroidMs1Tolerance)
                .AddTo(Disposables);
            CentroidMs2Tolerance = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.CentroidMs2Tolerance,
                    m => m.ToString(),
                    vm => float.Parse(vm)
                )
                .SetValidateAttribute(() => CentroidMs2Tolerance)
                .AddTo(Disposables);
            MaxChargeNumber = peakPickBaseParameterModel
                .ToReactivePropertyAsSynchronized(
                    m => m.MaxChargeNumber,
                    m => m.ToString(),
                    vm => int.Parse(vm)
                )
                .SetValidateAttribute(() => MaxChargeNumber)
                .AddTo(Disposables);
            IsBrClConsideredForIsotopes = peakPickBaseParameterModel
                .ToReactivePropertySlimAsSynchronized(m => m.IsBrClConsideredForIsotopes)
                .AddTo(Disposables);
            ExcludedMassList = peakPickBaseParameterModel.ExcludedMzQueries
                .ToReadOnlyReactiveCollection(query => new MzSearchQueryViewModel(query))
                .AddTo(Disposables);

            HasErrors = new[]
            {
                SmoothingLevel.ObserveHasErrors,
                MinimumAmplitude.ObserveHasErrors,
                MinimumDatapoints.ObserveHasErrors,
                MassSliceWidth.ObserveHasErrors,
                RetentionTimeBegin.ObserveHasErrors,
                RetentionTimeEnd.ObserveHasErrors,
                MassRangeBegin.ObserveHasErrors,
                MassRangeEnd.ObserveHasErrors,
                Ms2MassRangeBegin.ObserveHasErrors,
                Ms2MassRangeEnd.ObserveHasErrors,
                CentroidMs1Tolerance.ObserveHasErrors,
                CentroidMs2Tolerance.ObserveHasErrors,
                MaxChargeNumber.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim();
        }
    }
}
