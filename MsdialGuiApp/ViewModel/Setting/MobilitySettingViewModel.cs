using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Setting
{
    public class MobilitySettingViewModel : ViewModelBase
    {
        private readonly MobilitySettingModel model;

        public MobilitySettingViewModel(MobilitySettingModel model) {
            this.model = model;
            IonMobilityType = model.ToReactivePropertySlimAsSynchronized(m => m.IonMobilityType).AddTo(Disposables);

            IsTims = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.Tims).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsDtims = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.Dtims).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            IsTwims = IonMobilityType.Select(t => t == CompMs.Common.Enum.IonMobilityType.Twims).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var TimsInfoImported = IsTims;
            var DtimsInfoImported = new[]
            {
                IsDtims,
                CalibrationInfoCollection
                    .Select(info => info.ObserveProperty(i => i.AgilentBeta).Select(b => b > -1))
                    .CombineLatestValuesAreAllFalse(),
                CalibrationInfoCollection
                    .Select(info => info.ObserveProperty(i => i.AgilentTFix).Select(t => t > -1))
                    .CombineLatestValuesAreAllFalse(),
            }.CombineLatestValuesAreAllTrue();
            var TwimsInfoImported = new[]
            {
                IsTwims,
                CalibrationInfoCollection
                    .Select(info => info.ObserveProperty(i => i.WatersCoefficient).Select(c => c > -1))
                    .CombineLatestValuesAreAllFalse(),
                CalibrationInfoCollection
                    .Select(info => info.ObserveProperty(i => i.WatersT0).Select(t => t > -1))
                    .CombineLatestValuesAreAllFalse(),
                CalibrationInfoCollection
                    .Select(info => info.ObserveProperty(i => i.WatersExponent).Select(e => e > -1))
                    .CombineLatestValuesAreAllFalse(),
            }.CombineLatestValuesAreAllTrue();
            IsAllCalibrantDataImported = new[]
            {
                TimsInfoImported,
                DtimsInfoImported,
                TwimsInfoImported,
            }.CombineLatest(xs => xs.Any(x => x))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

        public ReactivePropertySlim<IonMobilityType> IonMobilityType { get; }
        public ReadOnlyReactivePropertySlim<bool> IsTims { get; }
        public ReadOnlyReactivePropertySlim<bool> IsDtims { get; }
        public ReadOnlyReactivePropertySlim<bool> IsTwims { get; }
        public ReadOnlyReactivePropertySlim<bool> IsAllCalibrantDataImported { get; }

        public ReadOnlyCollection<CcsCalibrationInfoVS> CalibrationInfoCollection => model.CalibrationInfoCollection;
    }
}
