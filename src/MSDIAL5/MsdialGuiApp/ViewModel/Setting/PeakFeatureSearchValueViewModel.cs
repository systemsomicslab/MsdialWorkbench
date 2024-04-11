using CompMs.App.Msdial.Model.Setting;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
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
    internal sealed class PeakFeatureSearchValueViewModel : ViewModelBase {
        private readonly PeakFeatureSearchValueModel _searchValueModel;
        private Subject<Unit> _commitTrigger;

        public PeakFeatureSearchValueViewModel(PeakFeatureSearchValueModel searchValueModel) {
            _searchValueModel = searchValueModel ?? throw new ArgumentNullException(nameof(searchValueModel));
            _commitTrigger = new Subject<Unit>().AddTo(Disposables);

            Title = searchValueModel.ToReactivePropertySlimAsSynchronized(
                m => m.Title,
                op => op,
                op => CommitAsObservable.WithLatestFrom(op, (_, p) => p))
                .AddTo(Disposables);
            Mass = searchValueModel.ToReactivePropertyAsSynchronized(
                m => m.Mass,
                op => op.Select(m => m.ToString()),
                op => CommitAsObservable.WithLatestFrom(op, (_, p) => double.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => Mass)
                .AddTo(Disposables);
            MassTolerance = searchValueModel.ToReactivePropertyAsSynchronized(
                m => m.MassTolerance,
                op => op.Select(m => m.ToString()),
                op => CommitAsObservable.WithLatestFrom(op, (_, p) => double.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => MassTolerance)
                .AddTo(Disposables);
            Time = searchValueModel.ToReactivePropertyAsSynchronized(
                m => m.TimeMin,
                op => op.Select(m => m.ToString()),
                op => CommitAsObservable.WithLatestFrom(op, (_, p) => double.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => Time)
                .AddTo(Disposables);
            TimeTolerance = searchValueModel.ToReactivePropertyAsSynchronized(
                m => m.TimeMax,
                op => op.Select(m => m.ToString()),
                op => CommitAsObservable.WithLatestFrom(op, (_, p) => double.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => TimeTolerance)
                .AddTo(Disposables);
            AbsoluteIntensityCutoff = searchValueModel.ToReactivePropertyAsSynchronized(
                m => m.AbsoluteIntensityCutoff,
                op => op.Select(m => m.ToString()),
                op => CommitAsObservable.WithLatestFrom(op, (_, p) => double.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => AbsoluteIntensityCutoff)
                .AddTo(Disposables);
            RelativeIntensityCutoff = searchValueModel.ToReactivePropertyAsSynchronized(
                m => m.RelativeIntensityCutoff,
                op => op.Select(m => m.ToString()),
                op => CommitAsObservable.WithLatestFrom(op, (_, p) => double.Parse(p)),
                ignoreValidationErrorValue: true)
                .SetValidateAttribute(() => RelativeIntensityCutoff)
                .AddTo(Disposables);
            PeakFeatureSearchType = searchValueModel.ToReactivePropertySlimAsSynchronized(
                m => m.PeakFeatureSearchType,
                op => op,
                op => CommitAsObservable.WithLatestFrom(op, (_, p) => p))
                .AddTo(Disposables);

            HasErrors = new[]
            {
                Mass.ObserveHasErrors,
                MassTolerance.ObserveHasErrors,
                Time.ObserveHasErrors,
                TimeTolerance.ObserveHasErrors,
                AbsoluteIntensityCutoff.ObserveHasErrors,
                RelativeIntensityCutoff.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);
        }

        public PeakFeatureSearchValueViewModel(PeakFeatureSearchValue searchValue) : this(new PeakFeatureSearchValueModel(searchValue)) {

        }

        public ReactivePropertySlim<string> Title { get; }

        [Required(ErrorMessage = "Mass is required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+")]
        public ReactiveProperty<string> Mass { get; }

        [Required(ErrorMessage = "Mass tolerance is required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+")]
        public ReactiveProperty<string> MassTolerance { get; }

        [Required(ErrorMessage = "Time is required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+")]
        public ReactiveProperty<string> Time { get; }

        [Required(ErrorMessage = "Time tolerance is required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+")]
        public ReactiveProperty<string> TimeTolerance { get; }

        [Required(ErrorMessage = "Absolute intensity cutoff is required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+")]
        public ReactiveProperty<string> AbsoluteIntensityCutoff { get; }

        [Required(ErrorMessage = "Relative intensity cutoff is required.")]
        [RegularExpression("[0-9]*\\.?[0-9]+")]
        public ReactiveProperty<string> RelativeIntensityCutoff { get; }

        public ReactivePropertySlim<PeakFeatureSearchType> PeakFeatureSearchType { get; }

        public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

        public IObservable<Unit> CommitAsObservable => _commitTrigger.Where(_ => !HasErrors.Value).ToUnit();

        public void Commit() {
            _commitTrigger.OnNext(Unit.Default);
        }
    }
}
