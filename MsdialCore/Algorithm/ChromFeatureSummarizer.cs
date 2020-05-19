using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm {
    public sealed class ChromFeatureSummarizer {
        /// <summary>
        /// This method is to do 2 things,
        /// 1) to get the summary of peak detections including the average peak width, retention time, height, etc..
        /// 2) to get the 'insurance' model peak which will be used as the model peak in MS2Dec algorithm in the case that any model peaks cannot be found from the focused MS/MS spectrum.
        /// </summary>
        public static ChromatogramPeaksDataSummary GetChromFeaturesSummary(List<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures, ParameterBase param) {
            if (chromPeakFeatures == null || chromPeakFeatures.Count == 0) return null;
            var summary = new ChromatogramPeaksDataSummary();

            SetBasicDataSummary(summary, spectrumList, param);
            SetChromPeakFeatureSummary(summary, chromPeakFeatures);

            return summary;
        }

        private static void SetBasicDataSummary(ChromatogramPeaksDataSummary summary, List<RawSpectrum> spectrumList, ParameterBase param) {
            int minScanNumber = (int)spectrumList[0].ScanNumber, maxScanNumber = (int)spectrumList[spectrumList.Count - 1].ScanNumber;
            float minRT = (float)spectrumList[0].ScanStartTime, maxRT = (float)spectrumList[spectrumList.Count - 1].ScanStartTime;
            double minDriftTime = double.MaxValue, maxDriftTime = double.MinValue;
            double minMz = double.MaxValue, maxMz = double.MinValue, minIntensity = double.MaxValue, maxIntensity = double.MinValue;


            for (int i = 0; i < spectrumList.Count; i++) {
                if (minMz > spectrumList[i].LowestObservedMz) minMz = spectrumList[i].LowestObservedMz;
                if (maxMz < spectrumList[i].HighestObservedMz) maxMz = spectrumList[i].HighestObservedMz;
                if (minIntensity > spectrumList[i].MinIntensity) minIntensity = spectrumList[i].MinIntensity;
                if (maxIntensity < spectrumList[i].BasePeakIntensity) maxIntensity = spectrumList[i].BasePeakIntensity;
                if (minDriftTime > spectrumList[i].DriftTime) minDriftTime = spectrumList[i].DriftTime;
                if (maxDriftTime < spectrumList[i].DriftTime) maxDriftTime = spectrumList[i].DriftTime;
            }

            summary.MinScanNumber = minScanNumber;
            summary.MaxScanNumber = maxScanNumber;
            summary.MinRetentionTime = minRT;
            summary.MaxRetentionTime = maxRT;
            summary.MinMass = (float)minMz;
            summary.MaxMass = (float)maxMz;
            summary.MinIntensity = (int)minIntensity;
            summary.MaxIntensity = (int)maxIntensity;
            summary.MinDriftTime = (float)minDriftTime;
            summary.MaxDriftTime = (float)maxDriftTime;
        }

        private static void SetChromPeakFeatureSummary(ChromatogramPeaksDataSummary summary, List<ChromatogramPeakFeature> chromPeakFeatures) {
            if (!chromPeakFeatures.IsNotEmptyOrNull()) return;

            var peakWidthArray = chromPeakFeatures.Select(feature => feature.PeakWidth()).OrderBy(item => item).ToArray();
            var peakHeightArray = chromPeakFeatures.Select(feature => feature.PeakHeightTop).OrderBy(item => item).ToArray();
            var peaktopRtArray = chromPeakFeatures.Select(feature => feature.ChromXs.Value).OrderBy(item => item).ToArray();
        
            summary.MinPeakWidth = (float)peakWidthArray[0];
            summary.AveragePeakWidth = (float)peakWidthArray.Average();
            summary.MedianPeakWidth = (float)BasicMathematics.Median(peakWidthArray);
            summary.MaxPeakWidth = (float)peakWidthArray[peakWidthArray.Length - 1];
            summary.StdevPeakWidth = (float)BasicMathematics.Stdev(peakWidthArray);

            summary.MinPeakHeight = (float)peakHeightArray[0];
            summary.AveragePeakHeight = (float)peakHeightArray.Average();
            summary.MedianPeakHeight = (float)BasicMathematics.Median(peakHeightArray);
            summary.MaxPeakHeight = (float)peakHeightArray[peakHeightArray.Length - 1];
            summary.StdevPeakHeight = (float)BasicMathematics.Stdev(peakHeightArray);

            summary.MinPeakTopRT = (float)peaktopRtArray[0];
            summary.AverageminPeakTopRT = (float)peaktopRtArray.Average();
            summary.MedianminPeakTopRT = (float)BasicMathematics.Median(peaktopRtArray);
            summary.MaxPeakTopRT = (float)peaktopRtArray[peaktopRtArray.Length - 1];
            summary.StdevPeakTopRT = (float)BasicMathematics.Stdev(peaktopRtArray);
        }
    }
}
