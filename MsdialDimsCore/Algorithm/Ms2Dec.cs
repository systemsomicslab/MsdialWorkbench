using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialDimsCore.Algorithm
{
    public class Ms2Dec
    {
        double initialProgress = 30d, progressMax = 30d;

        public Ms2Dec(double initialProgress, double progressMax) {
            this.initialProgress = initialProgress;
            this.progressMax = progressMax;
        }

        public List<MSDecResult> GetMS2DecResults(
            IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures,
            MsdialDimsParameter param, ChromatogramPeaksDataSummaryDto summary,
            double targetCE = -1,
            Action<int> reportAction = null, System.Threading.CancellationToken token = default) {

            var msdecResults = new List<MSDecResult>();
            foreach (var peak in chromPeakFeatures) {
                var result = GetMS2DecResult(spectrumList, peak, param, summary, targetCE);
                result.ScanID = peak.PeakID;
                msdecResults.Add(result);
                ReportProgress.Show(initialProgress, progressMax, result.ScanID, chromPeakFeatures.Count, reportAction);
            }
            return msdecResults;
        }

        private static MSDecResult GetMS2DecResult(
            IReadOnlyList<RawSpectrum> spectrumList, ChromatogramPeakFeature chromPeakFeature,
            MsdialDimsParameter param, ChromatogramPeaksDataSummaryDto summary,
            double targetCE = -1) {

            var targetSpecID = DataAccess.GetTargetCEIndexForMS2RawSpectrum(chromPeakFeature, targetCE);

            var cSpectrum = DataAccess.GetCentroidMassSpectra(
                spectrumList, param.MS2DataType, targetSpecID, param.AmplitudeCutoff,
                param.Ms2MassRangeBegin, param.Ms2MassRangeEnd);
            if (cSpectrum.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

            var curatedSpectra = new List<SpectrumPeak>();
            var precursorMz = chromPeakFeature.Mass;
            var threshold = Math.Max(param.AmplitudeCutoff, 0.1); // TODO: remove magic number

            foreach (var peak in cSpectrum.Where(n => n.Intensity > threshold)) { //preparing MS/MS chromatograms -> peaklistList
                if (param.RemoveAfterPrecursor && precursorMz + param.KeptIsotopeRange < peak.Mass) continue;
                curatedSpectra.Add(peak);
            }
            if (curatedSpectra.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

            // if (param.AcquisitionType == CompMs.Common.Enum.AcquisitionType.DDA)
            return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
        }
    }
}
