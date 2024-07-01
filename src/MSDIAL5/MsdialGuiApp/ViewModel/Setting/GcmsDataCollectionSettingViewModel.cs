using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public sealed class GcmsDataCollectionSettingViewModel : ViewModelBase, ISettingViewModel
    {
        private readonly Subject<Unit> _decide;
        private readonly GcmsDataCollectionSettingModel _model;


        public GcmsDataCollectionSettingViewModel(GcmsDataCollectionSettingModel model, IObservable<bool> isEnabled) {
            _model = model ?? throw new ArgumentNullException(nameof(model));

            IsReadOnly = model.IsReadOnly;

            MassRange = DataCollectionRangeSettingViewModelFactory.Create(model.MassRange)?.AddTo(Disposables);
            RtRange = DataCollectionRangeSettingViewModelFactory.Create(model.RtRange)?.AddTo(Disposables);
            NumberOfThreads = model.ToReactivePropertyAsSynchronized(
                m => m.NumberOfThreads,
                m => m.ToString(),
                vm => Math.Max(1, Math.Min(Environment.ProcessorCount, int.Parse(vm))),
                ignoreValidationErrorValue: true
            ).SetValidateAttribute(() => NumberOfThreads).AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new[]
            {
                MassRange?.ObserveHasErrors ?? Observable.Return(false),
                RtRange?.ObserveHasErrors ?? Observable.Return(false),
                NumberOfThreads.ObserveHasErrors,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var observeChanges = new[]
            {
                MassRange?.PropertyChangedAsObservable().ToUnit() ?? Observable.Never<Unit>(),
                RtRange?.PropertyChangedAsObservable().ToUnit() ?? Observable.Never<Unit>(),
                NumberOfThreads.ToUnit(),
            }.Merge().Publish();
            Disposables.Add(observeChanges.Connect());
            ObserveChanges = observeChanges;

            _decide = new Subject<Unit>().AddTo(Disposables);
            var change = ObserveChanges.TakeFirstAfterEach(_decide);
            ObserveChangeAfterDecision = new[]
            {
                change.ToConstant(true),
                _decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public bool IsReadOnly { get; }

        public DataCollectionRangeSettingViewModel? MassRange { get; }
        public DataCollectionRangeSettingViewModel? RtRange { get; }

        [Required(ErrorMessage = "Number of threads is required.")]
        [RegularExpression(@"\d+", ErrorMessage = "Invalid character entered.")]
        public ReactiveProperty<string> NumberOfThreads { get; }

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
