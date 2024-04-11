using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class AdductIonSettingViewModel : ViewModelBase, ISettingViewModel
    {
        public AdductIonSettingViewModel(IAdductIonSettingModel model, IObservable<bool> isEnabled) {
            Model = model;
            IsReadOnly = model.IsReadOnly;

            AdductIons = Model.AdductIons.ToReadOnlyReactiveCollection(m => new AdductIonVM(m)).AddTo(Disposables);
            SelectedAdduct = new ReactivePropertySlim<AdductIonVM>().AddTo(Disposables);
            UserDefinedAdductName = Model.ToReactivePropertySlimAsSynchronized(m => m.UserDefinedAdductName).AddTo(Disposables);
            UserDefinedAdduct = UserDefinedAdductName.Throttle(TimeSpan.FromMilliseconds(100)).Select(_ => Model.UserDefinedAdduct).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            AddCommand = UserDefinedAdduct
                .Select(vm => vm?.FormatCheck ?? false)
                .ToReactiveCommand()
                .WithSubscribe(Model.AddAdductIon)
                .AddTo(Disposables);

            RemoveCommand = new ReactiveCommand().AddTo(Disposables);
            RemoveCommand.WithLatestFrom(SelectedAdduct)
                .Select(p => p.Second?.Model)
                .SkipNull()
                .Subscribe(Model.RemoveAdductIon)
                .AddTo(Disposables);

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveHasErrors = new ReadOnlyReactivePropertySlim<bool>(Observable.Return(false)).AddTo(Disposables);
            ObserveChanges = AdductIons.ObserveElementPropertyChanged().ToUnit();
            decide = new Subject<Unit>().AddTo(Disposables);
            var changes = ObserveChanges.TakeFirstAfterEach(decide);
            ObserveChangeAfterDecision = new[]
            {
                changes.ToConstant(true),
                decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public IAdductIonSettingModel Model { get; }

        public bool IsReadOnly { get; }

        public ReadOnlyReactiveCollection<AdductIonVM> AdductIons { get; }

        public ReactivePropertySlim<AdductIonVM> SelectedAdduct { get; }

        public ReactivePropertySlim<string> UserDefinedAdductName { get; }

        public ReadOnlyReactivePropertySlim<AdductIon?> UserDefinedAdduct { get; }

        public ReactiveCommand AddCommand { get; }

        public ReactiveCommand RemoveCommand { get; }

        public ReadOnlyReactivePropertySlim<bool> IsEnabled { get; }

        public ReadOnlyReactivePropertySlim<bool> ObserveHasErrors { get; }
        IObservable<bool> ISettingViewModel.ObserveHasErrors => ObserveHasErrors;

        public IObservable<Unit> ObserveChanges { get; }

        private readonly Subject<Unit> decide;
        public ReadOnlyReactivePropertySlim<bool> ObserveChangeAfterDecision { get; }
        IObservable<bool> ISettingViewModel.ObserveChangeAfterDecision => ObserveChangeAfterDecision;

        public ISettingViewModel? Next(ISettingViewModel selected) {
            decide.OnNext(Unit.Default);
            return null;
        }
    }
}
