using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm
{
    public sealed class ChromFeatureSummarizer {
        public ChromFeatureSummarizer() {

        }

        public ChromatogramPeaksDataSummary Summarize(IDataProvider provider, IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures) {
            return ChromatogramPeaksDataSummary.Summarize(provider, chromPeakFeatures ?? new List<ChromatogramPeakFeature>(0));
        }

        /// <summary>
        /// This method is to do 2 things,
        /// 1) to get the summary of peak detections including the average peak width, retention time, height, etc..
        /// 2) to get the 'insurance' model peak which will be used as the model peak in MS2Dec algorithm in the case that any model peaks cannot be found from the focused MS/MS spectrum.
        /// </summary>
        public static ChromatogramPeaksDataSummaryDto GetChromFeaturesSummary(IDataProvider provider, IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures) {
            return new ChromFeatureSummarizer().Summarize(provider, chromPeakFeatures).ConvertToDto();
        }
    }
}
