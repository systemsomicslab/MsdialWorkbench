using CompMs.Common.Interfaces;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public abstract class PeakJoiner
    {
        public virtual List<IMSScanProperty> MergeChromatogramPeaks(List<IMSScanProperty> masters, List<IMSScanProperty> targets) {
            var merged = new List<IMSScanProperty>(masters);
            foreach (var target in targets) {
                if (!merged.Any(m => Equals(m, target))) {
                    merged.Add(target);
                }
            }
            return merged;
        }
        public virtual void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
            var n = masters.Count;
            var maxMatchs = new double[n];

            foreach (var target in targets) {
                // TODO: check tolerance
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                for (var i = 0; i < n; i++) {
                    if (!Equals(masters[i], target))
                        continue;
                    var factor = GetSimilality(masters[i], target);
                    if (factor > maxMatchs[i] && factor > matchFactor) {
                        matchIdx = i;
                        matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue)
                    DataObjConverter.SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(spots[matchIdx.Value].AlignedPeakProperties[fileId], target as ChromatogramPeakFeature);
            }
        }

        protected abstract bool Equals(IMSScanProperty x, IMSScanProperty y);
        protected abstract double GetSimilality(IMSScanProperty x, IMSScanProperty y);
    }
}
