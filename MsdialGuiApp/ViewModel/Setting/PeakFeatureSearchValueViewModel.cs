using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Setting {
    public class PeakFeatureSearchValueViewModel : ViewModelBase {
        public PeakFeatureSearchValue PeakFeatureSearchValue { get; }
        public ReactiveProperty<string> Title { get; }
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
        public ReactiveProperty<PeakFeatureSearchType> PeakFeatureSearchType { get; }

        public Subject<Unit> CommitTrigger { get; } = new Subject<Unit>();

        public ReadOnlyReactivePropertySlim<bool> HasErrors { get; }

        public IObservable<Unit> CommitAsObservable => CommitTrigger.Where(_ => !HasErrors.Value).ToUnit();

        public PeakFeatureSearchValueViewModel(PeakFeatureSearchValue searchValue) {
            PeakFeatureSearchValue = searchValue;
            
            Title = new ReactiveProperty<string>(PeakFeatureSearchValue.Title)
                .SetValidateAttribute(() => Title)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(Title, (_, x) => x)
                .Subscribe(x => PeakFeatureSearchValue.Title = x)
                .AddTo(Disposables);

            Mass = new ReactiveProperty<string>(PeakFeatureSearchValue.Mass.ToString())
                .SetValidateAttribute(() => Mass)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(Mass, (_, x) => x)
                .Subscribe(x => PeakFeatureSearchValue.Mass = double.Parse(x))
                .AddTo(Disposables);

            MassTolerance = new ReactiveProperty<string>(PeakFeatureSearchValue.MassTolerance.ToString())
                .SetValidateAttribute(() => MassTolerance)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(MassTolerance, (_, x) => x)
                .Subscribe(x => PeakFeatureSearchValue.MassTolerance = double.Parse(x))
                .AddTo(Disposables);

            Time = new ReactiveProperty<string>(PeakFeatureSearchValue.Time.ToString())
                .SetValidateAttribute(() => Time)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(Time, (_, x) => x)
                .Subscribe(x => PeakFeatureSearchValue.Time = double.Parse(x))
                .AddTo(Disposables);

            TimeTolerance = new ReactiveProperty<string>(PeakFeatureSearchValue.TimeTolerance.ToString())
                .SetValidateAttribute(() => TimeTolerance)
                .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(TimeTolerance, (_, x) => x)
                .Subscribe(x => PeakFeatureSearchValue.TimeTolerance = double.Parse(x))
                .AddTo(Disposables);

            AbsoluteIntensityCutoff = new ReactiveProperty<string>(PeakFeatureSearchValue.AbsoluteIntensityCutoff.ToString())
              .SetValidateAttribute(() => AbsoluteIntensityCutoff)
              .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(AbsoluteIntensityCutoff, (_, x) => x)
                .Subscribe(x => PeakFeatureSearchValue.AbsoluteIntensityCutoff = double.Parse(x))
                .AddTo(Disposables);

            RelativeIntensityCutoff = new ReactiveProperty<string>(PeakFeatureSearchValue.RelativeIntensityCutoff.ToString())
             .SetValidateAttribute(() => RelativeIntensityCutoff)
             .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(RelativeIntensityCutoff, (_, x) => x)
                .Subscribe(x => PeakFeatureSearchValue.RelativeIntensityCutoff = double.Parse(x))
                .AddTo(Disposables);

            PeakFeatureSearchType = new ReactiveProperty<PeakFeatureSearchType>(PeakFeatureSearchValue.PeakFeatureSearchType)
            .SetValidateAttribute(() => PeakFeatureSearchType)
            .AddTo(Disposables);
            CommitAsObservable
                .WithLatestFrom(PeakFeatureSearchType, (_, x) => x)
                .Subscribe(x => PeakFeatureSearchValue.PeakFeatureSearchType = x)
                .AddTo(Disposables);

            HasErrors = new[]
            {
                Title.ObserveHasErrors,
                Mass.ObserveHasErrors,
                MassTolerance.ObserveHasErrors,
                Time.ObserveHasErrors,
                TimeTolerance.ObserveHasErrors,
                AbsoluteIntensityCutoff.ObserveHasErrors,
                RelativeIntensityCutoff.ObserveHasErrors,
                PeakFeatureSearchType.ObserveHasErrors,
            }.CombineLatestValuesAreAllFalse()
            .Inverse()
            .ToReadOnlyReactivePropertySlim(true)
            .AddTo(Disposables);
        }

        public void Commit() {
            CommitTrigger.OnNext(Unit.Default);
        }
    }
}
