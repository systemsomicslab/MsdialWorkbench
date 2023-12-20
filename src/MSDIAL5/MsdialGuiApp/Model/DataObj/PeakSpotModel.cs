using CompMs.MsdialCore.MSDec;

namespace CompMs.App.Msdial.Model.DataObj
{
    internal sealed class PeakSpotModel
    {
        public PeakSpotModel(ChromatogramPeakFeatureModel peakSpot, MSDecResult msdec)
        {
            PeakSpot = peakSpot;
            MSDecResult = msdec;
        }

        public IPeakSpotModel PeakSpot { get; }

        public MSDecResult MSDecResult { get; }
    }
}
