using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialGcMsApi.Algorithm {
    public class Ms1Dec {
        public double InitialProgress { get; set; } = 30.0;
        public double ProgressMax { get; set; } = 30.0;
        public Ms1Dec(double InitialProgress, double ProgressMax) {
            this.InitialProgress = InitialProgress;
            this.ProgressMax = ProgressMax;
        }

        public List<MSDecResult> GetMSDecResults(List<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures,
            MsdialGcmsParameter param, ChromatogramPeaksDataSummaryDto summary, Action<int> reportAction, System.Threading.CancellationToken token) {
            return MSDecHandler.GetMSDecResults(spectrumList, chromPeakFeatures, param, reportAction, InitialProgress, ProgressMax);
        }
    }
}
