using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public abstract class DataCollectionRangeSettingViewModel : ViewModelBase
    {
        private readonly IDataCollectionRangeSetting model;

        public DataCollectionRangeSettingViewModel(IDataCollectionRangeSetting model) {
            this.model = model;

            Begin = model.ToReactivePropertyAsSynchronized(
                m => m.Begin,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => Begin).AddTo(Disposables);

            End = model.ToReactivePropertyAsSynchronized(
                m => m.End,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => End).AddTo(Disposables);

            NeedAccumulation = new ReadOnlyReactivePropertySlim<bool>(Observable.Return(model.NeedAccumulation)).AddTo(Disposables);

            AccumulatedRange = model.ToReactivePropertyAsSynchronized(
                m => m.AccumulatedRange,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => AccumulatedRange).AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                Begin.ObserveHasErrors,
                End.ObserveHasErrors,
                new[]
                {
                    NeedAccumulation,
                    AccumulatedRange.ObserveHasErrors,
                }.CombineLatestValuesAreAllTrue()
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        [Required(ErrorMessage = "Range begin value is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, float.MaxValue, ErrorMessage = "Range should start positive value.")]
        public ReactiveProperty<string> Begin { get; }

        [Required(ErrorMessage = "Range end value is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, float.MaxValue, ErrorMessage = "Range should end positive value.")]
        public ReactiveProperty<string> End { get; }

        public ReadOnlyReactivePropertySlim<bool> NeedAccumulation { get; }

        [Required(ErrorMessage = "Accumulation range is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, float.MaxValue, ErrorMessage = "Accumulation range should be positive value.")]
        public ReactiveProperty<string> AccumulatedRange { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
    }

    public class RetentionTimeDataCollectionRangeSettingViewModel : DataCollectionRangeSettingViewModel
    {
        public RetentionTimeDataCollectionRangeSettingViewModel(RetentionTimeCollectionRangeSetting model) : base(model) {

        }
    }

    public class DriftTimeDataCollectionRangeSettingViewModel : DataCollectionRangeSettingViewModel
    {
        public DriftTimeDataCollectionRangeSettingViewModel(DriftTimeCollectionRangeSetting model) : base(model) {

        }
    }

    public class Ms1DataCollectionRangeSettingViewModel : DataCollectionRangeSettingViewModel
    {
        public Ms1DataCollectionRangeSettingViewModel(Ms1CollectionRangeSetting model) : base(model) {

        }
    }

    public class Ms2DataCollectionRangeSettingViewModel : DataCollectionRangeSettingViewModel
    {
        public Ms2DataCollectionRangeSettingViewModel(Ms2CollectionRangeSetting model) : base(model) {

        }
    }

    public static class DataCollectionRangeSettingViewModelFactory
    {
        public static DataCollectionRangeSettingViewModel? Create(IDataCollectionRangeSetting model) {
            switch (model) {
                case RetentionTimeCollectionRangeSetting m:
                    return new RetentionTimeDataCollectionRangeSettingViewModel(m);
                case DriftTimeCollectionRangeSetting m:
                    return new DriftTimeDataCollectionRangeSettingViewModel(m);
                case Ms1CollectionRangeSetting m:
                    return new Ms1DataCollectionRangeSettingViewModel(m);
                case Ms2CollectionRangeSetting m:
                    return new Ms2DataCollectionRangeSettingViewModel(m);
                default:
                    return null;
            }
        }
    }
}
