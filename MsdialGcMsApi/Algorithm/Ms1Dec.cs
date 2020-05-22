using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialGcMsApi.Algorithm {
    public class Ms1Dec {
        public List<MSDecResult> GetMS2DecResults(List<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures,
            MsdialGcmsParameter param, ChromatogramPeaksDataSummary summary, Action<int> reportAction, System.Threading.CancellationToken token) {
            return MSDecHandler.GetMSDecResults(spectrumList, chromPeakFeatures, param, reportAction);
        }
    }
}
