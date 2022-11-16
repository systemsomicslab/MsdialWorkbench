using CompMs.Common.Interfaces;
using System;

namespace CompMs.App.Msdial.Model.DataObj
{
    [Obsolete("Use PeakItem instead of ChromatogramPeakWrapper")]
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
