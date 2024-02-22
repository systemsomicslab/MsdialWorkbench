using CompMs.Common.Interfaces;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class PeakSpotModel
    {
        public PeakSpotModel(IPeakSpotModel peakSpot, IMSScanProperty msscan)
        {
            PeakSpot = peakSpot;
            MsScan = msscan;
        }

        public IPeakSpotModel PeakSpot { get; }
        public IMSScanProperty MsScan { get; }
    }
}
