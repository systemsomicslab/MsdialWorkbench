using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public sealed class GcmsPeakDetectionSettingViewModel : ViewModelBase, ISettingViewModel
    {
        private readonly GcmsPeakDetectionSettingModel _model;
        private readonly Subject<Unit> _decide;

        public GcmsPeakDetectionSettingViewModel(GcmsPeakDetectionSettingModel model, IObservable<bool> isEnabled) {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            MinimumAmplitude = model.PeakPickSettingModel.ToReactivePropertyAsSynchronized(
                m => m.MinimumAmplitude,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MinimumAmplitude).AddTo(Disposables);

            IsAccurateMS = model.ToReactivePropertySlimAsSynchronized(
                m => m.AccuracyType,
                op => op.Select(t => t == AccuracyType.IsAccurate),
                op => op.Select(p => p ? AccuracyType.IsAccurate : AccuracyType.IsNominal)).AddTo(Disposables);
                
            MassSliceWidth = model.PeakPickSettingModel.ToReactivePropertyAsSynchronized(
                m => m.MassSliceWidth,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MassSliceWidth).AddTo(Disposables);

            MassAccuracy = model.PeakPickSettingModel.ToReactivePropertyAsSynchronized(
                m => m.CentroidMs1Tolerance,
                m => m.ToString(),
                vm => float.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => MassAccuracy).AddTo(Disposables);

            SmoothingMethod = model.PeakPickSettingModel
                .ToReactivePropertySlimAsSynchronized(m => m.SmoothingMethod).AddTo(Disposables);

            SmoothingLevel = model.PeakPickSettingModel.ToReactivePropertyAsSynchronized(
                m => m.SmoothingLevel,
                m => m.ToString(),
                vm => int.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => SmoothingLevel).AddTo(Disposables);

            AveragePeakWidth = model.PeakPickSettingModel.ToReactivePropertyAsSynchronized(
                m => m.MinimumDatapoints,
                m => m.ToString(),
                vm => double.Parse(vm),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => AveragePeakWidth).AddTo(Disposables);

            ExcludedMassList = model.PeakPickSettingModel.ExcludedMzQueries
                .ToReadOnlyReactiveCollection(m => new MzSearchQueryViewModel(m)).AddTo(Disposables);

            AddCommand = new[]
            {
                this.ObserveProperty(vm => vm.NewMass).ToUnit(),
                this.ObserveProperty(vm => vm.NewTolerance).ToUnit(),
            }.Merge()
            .Select(_ => !ContainsError(nameof(NewMass)) && !ContainsError(nameof(NewTolerance)))
            .ToReactiveCommand()
            .WithSubscribe(() => model.PeakPickSettingModel.AddQuery(NewMass, NewTolerance))
            .AddTo(Disposables);

            RemoveCommand = this.ObserveProperty(vm => vm.SelectedQuery)
                .Select(q => q != null)
                .ToReactiveCommand()
                .WithSubscribe(() => model.PeakPickSettingModel.RemoveQuery(SelectedQuery!.Model))
                .AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                MinimumAmplitude.ObserveHasErrors,
                new[]
                {
                    IsAccurateMS,
                    new[]
                    {
                        MassSliceWidth.ObserveHasErrors,
                        MassAccuracy.ObserveHasErrors,
                    }.CombineLatestValuesAreAnyTrue(),
                }.CombineLatestValuesAreAllTrue(),
                SmoothingLevel.ObserveHasErrors,
                AveragePeakWidth.ObserveHasErrors,
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

            ObserveChanges = new[]
            {
                MinimumAmplitude.ToUnit(),
                IsAccurateMS.ToUnit(),
                MassSliceWidth.ToUnit(),
                MassAccuracy.ToUnit(),
                SmoothingMethod.ToUnit(),
                SmoothingLevel.ToUnit(),
                AveragePeakWidth.ToUnit(),
                ExcludedMassList.ObserveElementPropertyChanged().ToUnit(),
            }.Merge();

            _decide = new Subject<Unit>().AddTo(Disposables);
            ObserveChangeAfterDecision = Observable.Merge(new[]
            {
                ObserveChanges.TakeFirstAfterEach(_decide).ToConstant(true),
                _decide.ToConstant(false)
            })
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public bool IsReadOnly => _model.IsReadOnly;

        [Required(ErrorMessage = "Minimum amplitude is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Amplitude should be positive value.")]
        public ReactiveProperty<string> MinimumAmplitude { get; }

        public ReactivePropertySlim<bool> IsAccurateMS { get; }

        [Required(ErrorMessage = "Mass slice width is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, int.MaxValue, ErrorMessage = "Width should be positive value.")]
        public ReactiveProperty<string> MassSliceWidth { get; }

        [Required(ErrorMessage = "Mass accuracy for centroiding is required.")]
        [RegularExpression(@"\d*\.?\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, int.MaxValue, ErrorMessage = "Width should be positive value.")]
        public ReactiveProperty<string> MassAccuracy { get; }

        public ReactivePropertySlim<SmoothingMethod> SmoothingMethod { get; }

        [Required(ErrorMessage = "Smoothing level is required.")]
        [RegularExpression(@"\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Amplitude should be positive value.")]
        public ReactiveProperty<string> SmoothingLevel { get; }

        [Required(ErrorMessage = "Average peak width is required.")]
        [RegularExpression(@"\d\.?\d*", ErrorMessage = "Invalid character entered.")]
        [Range(1, int.MaxValue, ErrorMessage = "Data points should be positive value.")]
        public ReactiveProperty<string> AveragePeakWidth { get; }

        public ReadOnlyReactiveCollection<MzSearchQueryViewModel> ExcludedMassList { get; }

        public MzSearchQueryViewModel? SelectedQuery {
            get => _selectedQuery;
            set => SetProperty(ref _selectedQuery, value);
        }
        private MzSearchQueryViewModel? _selectedQuery;

        [RegularExpression(@"\d\.?\d*", ErrorMessage = "Invalid character entered.")]
        [Range(0, int.MaxValue, ErrorMessage = "Data points should be positive value.")]
        public double NewMass {
            get => _newMass;
            set => SetProperty(ref _newMass, value);
        }
        private double _newMass;

        [RegularExpression(@"0?\.\d+", ErrorMessage = "Invalid character entered.")]
        [Range(0.0001, int.MaxValue, ErrorMessage = "Width should be positive value.")]
        public double NewTolerance {
            get => _newTolerance;
            set => SetProperty(ref _newTolerance, value);
        }
        private double _newTolerance;

        public ReactiveCommand AddCommand { get; }

        public ReactiveCommand RemoveCommand { get; }

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
