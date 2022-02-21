using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class PeakDetectionSettingViewModel : ViewModelBase
    {
        public PeakDetectionSettingViewModel(PeakDetectionSettingModel model) {
            Model = model;

            MinimumAmplitude = Model.ToReactivePropertyAsSynchronized(
                m => m.MinimumAmplitude,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => MinimumAmplitude).AddTo(Disposables);

            MassSliceWidth = Model.ToReactivePropertyAsSynchronized(
                m => m.MassSliceWidth,
                m => m.ToString(),
                vm => float.Parse(vm)
            ).SetValidateAttribute(() => MassSliceWidth).AddTo(Disposables);

            SmoothingMethod = new ReactivePropertySlim<SmoothingMethod>(Model.SmoothingMethod).AddTo(Disposables);

            SmoothingLevel = Model.ToReactivePropertyAsSynchronized(
                m => m.SmoothingLevel,
                m => m.ToString(),
                vm => int.Parse(vm)
            ).SetValidateAttribute(() => SmoothingLevel).AddTo(Disposables);

            MinimumDatapoints = Model.ToReactivePropertyAsSynchronized(
                m => m.MinimumDatapoints,
                m => m.ToString(),
                vm => double.Parse(vm)
            ).SetValidateAttribute(() => MinimumDatapoints).AddTo(Disposables);

            ExcludedMassList = Model.ExcludedMassList.ToReadOnlyReactiveCollection(m => new MzSearchQueryViewModel(m)).AddTo(Disposables);

            AddCommand = new[]
            {
                this.ObserveProperty(vm => vm.NewMass).ToUnit(),
                this.ObserveProperty(vm => vm.NewTolerance).ToUnit(),
            }.Merge()
            .Select(_ => !ContainsError(nameof(NewMass)) && !ContainsError(nameof(NewTolerance)))
            .ToReactiveCommand()
            .WithSubscribe(() => Model.AddQuery(NewMass, NewTolerance))
            .AddTo(Disposables);

            RemoveCommand = this.ObserveProperty(vm => vm.SelectedQuery)
                .Select(q => q != null)
                .ToReactiveCommand()
                .WithSubscribe(() => Model.RemoveQuery(SelectedQuery.Model))
                .AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                MinimumAmplitude.ObserveHasErrors,
                MassSliceWidth.ObserveHasErrors,
                SmoothingLevel.ObserveHasErrors,
                MinimumDatapoints.ObserveHasErrors,
                new[]
                {
                    ExcludedMassList.ObserveElementPropertyChanged().ToUnit(),
                    ExcludedMassList.CollectionChangedAsObservable().ToUnit(),
                }.Merge()
                .Select(_ => ExcludedMassList.Any(vm => vm.HasValidationErrors)),
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public PeakDetectionSettingModel Model { get; }

        [Required(ErrorMessage = "Minimum amplitude is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Amplitude should be positive value.")]
        public ReactiveProperty<string> MinimumAmplitude { get; }

        [Required(ErrorMessage = "Mass slice width is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, int.MaxValue, ErrorMessage = "Width should be positive value.")]
        public ReactiveProperty<string> MassSliceWidth { get; }

        public ReactivePropertySlim<SmoothingMethod> SmoothingMethod { get; }

        [Required(ErrorMessage = "Smoothing level is required.")]
        [RegularExpression(@"\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Amplitude should be positive value.")]
        public ReactiveProperty<string> SmoothingLevel { get; }

        [Required(ErrorMessage = "Minimum datapoint is required.")]
        [RegularExpression(@"\d\.?\d*", ErrorMessage = "Invalid character entered.")]
        [Range(1, int.MaxValue, ErrorMessage = "Data points should be positive value.")]
        public ReactiveProperty<string> MinimumDatapoints { get; }

        public ReadOnlyReactiveCollection<MzSearchQueryViewModel> ExcludedMassList { get; }

        public MzSearchQueryViewModel SelectedQuery {
            get => selectedQuery;
            set => SetProperty(ref selectedQuery, value);
        }
        private MzSearchQueryViewModel selectedQuery;

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }

        [RegularExpression(@"\d\.?\d*", ErrorMessage = "Invalid character entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Data points should be positive value.")]
        public double NewMass {
            get => newMass;
            set => SetProperty(ref newMass, value);
        }
        private double newMass;

        [RegularExpression(@"0?\.\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, int.MaxValue, ErrorMessage = "Width should be positive value.")]
        public double NewTolerance {
            get => newTolerance;
            set => SetProperty(ref newTolerance, value);
        }
        private double newTolerance;

        public ReactiveCommand AddCommand { get; }

        public ReactiveCommand RemoveCommand { get; }
    }
}
