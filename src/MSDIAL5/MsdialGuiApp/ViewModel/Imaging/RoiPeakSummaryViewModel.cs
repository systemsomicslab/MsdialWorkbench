using CompMs.App.Msdial.Model.Imaging;
using CompMs.CommonMVVM;
using System;

namespace CompMs.App.Msdial.ViewModel.Imaging
{
    internal sealed class RoiPeakSummaryViewModel : ViewModelBase
    {
        public RoiPeakSummaryViewModel(RoiPeakSummaryModel model) {
            Model = model ?? throw new ArgumentNullException(nameof(model));
        }

        public RoiPeakSummaryModel Model { get; }

        public double AccumulatedIntensity => Model.AccumulatedIntensity;
    }
}
