using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class ChromatogramPeakWrapper
    {
        public double Intensity => _innerModel.Intensity;
        public double? ChromXValue => _innerModel.ChromXs?.Value;

        private readonly IChromatogramPeak _innerModel;
        public ChromatogramPeakWrapper(IChromatogramPeak peak) {
            _innerModel = peak;
        }

        public PeakItem ConvertToPeakItem() => new PeakItem(_innerModel);
    }

}
