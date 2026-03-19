using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class MobilitySettingViewModel : ViewModelBase, ISettingViewModel
    {
        private readonly MobilitySettingModel model;

        public MobilitySettingViewModel(MobilitySettingModel model, IObservable<bool> isEnabled) {
            this.model = model;
            IsReadOnly = model.IsReadOnly;
            IonMobilityType = model.ToReactivePropertySlimAsSynchronized(m => m.IonMobilityType).AddTo(Disposables);

            IsTims = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.Tims).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsDtims = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.Dtims).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsTwims = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.Twims).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var TimsInfoImported = IsTims;
            var DtimsInfoImported = new[]
            {
                IsDtims,
                CalibrationInfoCollection
                    .Select(info => new[] {
                        info.ObserveProperty(i => i.AgilentBeta).Select(u => u == -1),
                        info.ObserveProperty(i => i.AgilentTFix).Select(u => u == -1),
                    }.CombineLatestValuesAreAllTrue())
                    .CombineLatestValuesAreAllFalse(),
            }.CombineLatestValuesAreAllTrue();
            var TwimsInfoImported = new[]
            {
                IsTwims,
                CalibrationInfoCollection
                    .Select(info => new[] {
                        info.ObserveProperty(i => i.WatersCoefficient).Select(u => u == -1),
                        info.ObserveProperty(i => i.WatersT0).Select(u => u == -1),
                        info.ObserveProperty(i => i.WatersExponent).Select(u => u == -1),
                    }.CombineLatestValuesAreAllTrue())
                    .CombineLatestValuesAreAllFalse(),
            }.CombineLatestValuesAreAllTrue();

            IsEnabled = isEnabled.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            IsAllCalibrantDataImported = new[]
            {
                TimsInfoImported,
                DtimsInfoImported,
                TwimsInfoImported,
            }.CombineLatestValuesAreAnyTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            IsAllCalibrantDataImported.Subscribe(x => model.IsAllCalibrantDataImported = x).AddTo(Disposables);

            ObserveHasErrors = Observable.Return(false).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            ObserveChanges = new[]
            {
                IonMobilityType.ToUnit(),
                CalibrationInfoCollection.Select(vm => vm.PropertyChangedAsObservable()).Merge().ToUnit(),
            }.Merge();

            decide = new Subject<Unit>().AddTo(Disposables);
            ObserveChangeAfterDecision = new[]
            {
                ObserveChanges.TakeFirstAfterEach(decide).ToConstant(true),
                decide.ToConstant(false),
            }.Merge()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public ReactivePropertySlim<IonMobilityType> IonMobilityType { get; }
        public ReadOnlyReactivePropertySlim<bool> IsTims { get; }
        public ReadOnlyReactivePropertySlim<bool> IsDtims { get; }
        public ReadOnlyReactivePropertySlim<bool> IsTwims { get; }
        public ReadOnlyReactivePropertySlim<bool> IsAllCalibrantDataImported { get; }

        public ReadOnlyCollection<CcsCalibrationInfoVS> CalibrationInfoCollection => model.CalibrationInfoCollection;

        public bool IsReadOnly { get; }

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
