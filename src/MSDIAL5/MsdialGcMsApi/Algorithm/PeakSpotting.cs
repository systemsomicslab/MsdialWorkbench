using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialGcMsApi.Algorithm {
    public sealed class PeakSpotting {
        private readonly IupacDatabase _iupacDB;
        private readonly MsdialGcmsParameter _parameter;

        public PeakSpotting(IupacDatabase iupacDB, MsdialGcmsParameter parameter) {
            _iupacDB = iupacDB;
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
        }

        public List<ChromatogramPeakFeature> Run(AnalysisFileBean analysisFile, IDataProvider provider, ReportProgress reporter, CancellationToken token) {
            var coreProcess = new PeakSpottingCore(_parameter);
            var chromatogramRange = new ChromatogramRange(_parameter.RetentionTimeBegin, _parameter.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            var chromPeakFeatures = coreProcess.Execute3DFeatureDetection(analysisFile, provider, _parameter.NumThreads, token, reporter, chromatogramRange);
            if (_parameter.MachineCategory == Common.Enum.MachineCategory.GCGCMS) {
                var orderedPeaks = chromPeakFeatures.OrderBy(p => p.PeakFeature.PeakHeightTop).ToList();
                var topPeaks = new Dictionary<ChromatogramPeakFeature, List<ChromatogramPeakFeature>>();
                foreach (var peak in orderedPeaks) {
                    var merged = false;
                    foreach (var kvp in topPeaks) {
                        var (topPeak, childs) = (kvp.Key, kvp.Value);
                        if (IsNearBy(peak, topPeak)) {
                            childs.Add(peak);
                            merged = true;
                            break;
                        }
                    }
                    if (!merged) {
                        topPeaks.Add(peak, [peak]);
                    }
                }

                chromPeakFeatures = topPeaks.Select(peaks => {
                    var topPeak = peaks.Key;
                    topPeak.PeakFeature.PeakAreaAboveZero = peaks.Value.Select(peak => peak.PeakFeature.PeakAreaAboveZero).Sum();
                    topPeak.PeakFeature.PeakAreaAboveBaseline = peaks.Value.Select(peak => peak.PeakFeature.PeakAreaAboveBaseline).Sum();
                    return topPeak;
                }).OrderBy(p => p.MasterPeakID)
                .Select((p, i) => {
                    p.MasterPeakID = i;
                    return p;
                }).ToList();
            }
            IsotopeEstimator.Process(chromPeakFeatures, _parameter, _iupacDB);
            return chromPeakFeatures;
        }

        private bool IsNearBy(IChromatogramPeakFeature p1, IChromatogramPeakFeature p2) {
            return Math.Abs(p1.Mass - p2.Mass) <= _parameter.CentroidMs1Tolerance
                && Math.Abs(p1.ChromXsTop.RT.Value - p2.ChromXsTop.RT.Value) <= _parameter.FirstColumnRetentionTimeTolerance
                && Math.Abs(p1.ChromXsTop.RT.Value % _parameter.ModulationTime - p2.ChromXsTop.RT.Value % _parameter.ModulationTime) <= _parameter.RetentionTimeAlignmentTolerance;
        }
    }
}
