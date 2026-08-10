using CompMs.App.Msdial.Model.Imaging;
using CompMs.CommonMVVM;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class RoiPeakSummaryViewModel : ViewModelBase
    {
        public RoiPeakSummaryViewModel(RoiPeakSummaryModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            AccumulatedIntensity = model.ObserveProperty(m => m.AccumulatedIntensity).ToReadOnlyReactivePropertySlim(initialValue: null).AddTo(Disposables);
            IsAccumulatedIntensityLoading = model.ObserveProperty(m => m.IsAccumulatedIntensityLoading).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public RoiPeakSummaryModel Model { get; }

        public ReadOnlyReactivePropertySlim<double?> AccumulatedIntensity { get; }
        public ReadOnlyReactivePropertySlim<bool> IsAccumulatedIntensityLoading { get; }

        public Task EnsureCalculateAccumulatedIntensity() {
            return Model.EnsureCalculateAccumulatedIntensityAsync();
        }
    }
}
