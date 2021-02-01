using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class Ms2Dec
    {
        public double InitialProgress { get; set; } = 30.0;
        public double ProgressMax { get; set; } = 30.0;

        public Ms2Dec(double initialProgress, double progressMax) {
            InitialProgress = initialProgress;
            ProgressMax = progressMax;
        }

        public List<MSDecResult> GetMS2DecResults(
            List<RawSpectrum> spectrumList,
            List<ChromatogramPeakFeature> chromPeakFeatures,
            MsdialImmsParameter parameter,
            ChromatogramPeaksDataSummary summary,
            double targetCE,
            Action<int> reportAction,
            System.Threading.CancellationToken toke = default) {

            return new List<MSDecResult>();
        }
    }
}
