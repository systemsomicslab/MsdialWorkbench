using System;
using System.Collections.Generic;
using System.Linq;

using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public abstract class GapFiller
    {
        protected abstract double AxTol { get; }
        protected SmoothingMethod smoothingMethod;
        protected int smoothingLevel;
        protected bool isForceInsert; 

        public GapFiller(SmoothingMethod smoothingMethod, int smoothingLevel, bool isForceInsert) {
            this.smoothingMethod = smoothingMethod;
            this.smoothingLevel = smoothingLevel;
            this.isForceInsert = isForceInsert;
        }

        public GapFiller(ParameterBase param) : this(param.SmoothingMethod, param.SmoothingLevel, param.IsForceInsertForGapFilling) { }

        public void GapFill(IDataProvider provider, AlignmentSpotProperty spot, int fileID) {
            GapFill(provider.LoadMs1Spectrums(), spot, fileID);
        }

        public void GapFill(IReadOnlyList<RawSpectrum> spectra, AlignmentSpotProperty spot, int fileID) {
            var peaks = spot.AlignedPeakProperties;
            var filtered = peaks.Where(peak => peak.PeakID >= 0);
            var chromXCenter = GetCenter(filtered);
            var peakWidth = GetPeakWidth(filtered);
            var peaklist = GetPeaks(spectra, chromXCenter, peakWidth, fileID, smoothingMethod, smoothingLevel);

            var target = peaks.First(peak => peak?.FileID == fileID);
            GapFillCore(peaklist, chromXCenter, AxTol, target);
        }

        protected abstract ChromXs GetCenter(IEnumerable<AlignmentChromPeakFeature> peaks); // TODO: change this to run only once per spot
        protected abstract double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks);

        protected void GapFillCore(
            List<ChromatogramPeak> peaklist, ChromXs center, double axTol,
            AlignmentChromPeakFeature alignmentChromPeakFeature
            ) {

            if (peaklist == null || peaklist.Count == 0) {
                SetDefaultValueToAlignmentChromPeakFeature(alignmentChromPeakFeature);
                return;
            }
            var result = alignmentChromPeakFeature;
            result.PeakID = -2;

            var centralAx = center.Value;
            (var candidates, var minId) = GetPeakTopCandidates(peaklist, centralAx, axTol);

            var isForceInsert = this.isForceInsert;
            (var id, var leftId, var rightId) = GetPeakRange(candidates, peaklist, minId, centralAx, isForceInsert);
            if (id == -1 || leftId == -1 || rightId == -1) {
                SetDefaultValueToAlignmentChromPeakFeature(result);
                return;
            }

            SetAlignmentChromPeakFeature(result, peaklist, id, leftId, rightId);
        }

        protected abstract List<ChromatogramPeak> GetPeaks(
            IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID,
            SmoothingMethod smoothingMethod, int smoothingLevel);

        protected virtual (List<ChromatogramPeak>, int) GetPeakTopCandidates(List<ChromatogramPeak> sPeaklist, double centralAx, double axTol) {
            var candidates = new List<ChromatogramPeak>();
            var minId = -1;
            var minDiff = double.MaxValue;

            var start = SearchCollection.LowerBound(sPeaklist, new ChromatogramPeak { ChromXs = new ChromXs(centralAx - axTol) }, (a, b) => a.ChromXs.Value.CompareTo(b.ChromXs.Value));
            for (int i = start; i < sPeaklist.Count; i++) {
                if (i - 2 < 0 || i + 2 >= sPeaklist.Count) continue;
                if (sPeaklist[i].ChromXs.Value < centralAx - axTol) continue;
                if (centralAx + axTol < sPeaklist[i].ChromXs.Value) break;

                if (   sPeaklist[i-2].Intensity <= sPeaklist[i-1].Intensity && sPeaklist[i-1].Intensity <= sPeaklist[i].Intensity && sPeaklist[i].Intensity > sPeaklist[i+1].Intensity
                    || sPeaklist[i-1].Intensity < sPeaklist[i].Intensity && sPeaklist[i].Intensity >= sPeaklist[i+1].Intensity && sPeaklist[i+1].Intensity >= sPeaklist[i+2].Intensity) {
                    candidates.Add(sPeaklist[i]);
                }

                var diff = Math.Abs(sPeaklist[i].ChromXs.Value - centralAx);
                if (diff < minDiff) {
                    minDiff = diff;
                    minId = i;
                }
            }

            if (minId == -1) minId = sPeaklist.Count / 2;

            return (candidates, minId);
        }

        protected virtual (int, int, int) GetPeakRange(List<ChromatogramPeak> candidates, List<ChromatogramPeak> sPeaklist, int minId, double centralAx, bool isForceInsert) {
            int id, leftId, rightId;

            if (candidates.Count == 0) {
                if (!isForceInsert) return (-1, -1, -1);
                var range = 5;

                id = minId;
                leftId = Math.Max(id - 1, 0);
                rightId = Math.Min(id + 1, sPeaklist.Count - 1);

                var limit = Math.Max(id - range, 0);
                while (limit < leftId) {
                    if (sPeaklist[leftId - 1].Intensity > sPeaklist[leftId].Intensity) break;
                    --leftId;
                }
                limit = Math.Min(id + range, sPeaklist.Count - 1);
                while (rightId < limit) {
                    if (sPeaklist[rightId].Intensity < sPeaklist[rightId + 1].Intensity) break;
                    ++rightId;
                }
            }
            else {
                id = candidates.Argmin(cand => Math.Abs(cand.ChromXs.Value - centralAx)).ID;

                var margin = 2;

                leftId = Math.Max(id - margin, 0);
                while (0 < leftId) {
                    if (sPeaklist[leftId - 1].Intensity >= sPeaklist[leftId].Intensity) break;
                    --leftId;
                }

                rightId = Math.Min(id + margin, sPeaklist.Count - 1);
                while (rightId < sPeaklist.Count - 1) {
                    if (sPeaklist[rightId].Intensity <= sPeaklist[rightId + 1].Intensity) break;
                    ++rightId;
                }

                if (!isForceInsert && (id - leftId < 2 || rightId - id < 2)) return (-1, -1, -1);

                for(int i = leftId + 1; i <= rightId - 1; i++) {
                    if (sPeaklist[i - 1].Intensity <= sPeaklist[i].Intensity && sPeaklist[i].Intensity > sPeaklist[i + 1].Intensity
                        || sPeaklist[i - 1].Intensity < sPeaklist[i].Intensity && sPeaklist[i].Intensity >= sPeaklist[i + 1].Intensity) {
                        if (sPeaklist[id].Intensity < sPeaklist[i].Intensity) {
                            id = i;
                        }
                    }
                }
            }

            return (id, leftId, rightId);
        }

        private static void SetAlignmentChromPeakFeature(AlignmentChromPeakFeature result, List<ChromatogramPeak> sPeaklist, int id, int leftId, int rightId) {
            double peakAreaAboveZero = 0d;
            for (int i = leftId; i < rightId; i++)
                peakAreaAboveZero += (sPeaklist[i].Intensity + sPeaklist[i + 1].Intensity) / 2 * (sPeaklist[i + 1].Mass - sPeaklist[i].Mass);

            System.Diagnostics.Debug.Assert(sPeaklist[id].ChromXs != null);
            System.Diagnostics.Debug.Assert(sPeaklist[leftId].ChromXs != null);
            System.Diagnostics.Debug.Assert(sPeaklist[rightId].ChromXs != null);

            result.Mass = sPeaklist[id].Mass;
            result.ChromScanIdTop = id;
            result.ChromScanIdLeft = leftId;
            result.ChromScanIdRight = rightId;
            result.ChromXsTop = sPeaklist[id].ChromXs;
            result.ChromXsLeft = sPeaklist[leftId].ChromXs;
            result.ChromXsRight = sPeaklist[rightId].ChromXs;
            result.PeakHeightTop = sPeaklist[id].Intensity;
            result.PeakHeightLeft = sPeaklist[leftId].Intensity;
            result.PeakHeightRight = sPeaklist[rightId].Intensity;
            result.PeakAreaAboveZero = peakAreaAboveZero;
        }

        private static void SetDefaultValueToAlignmentChromPeakFeature(AlignmentChromPeakFeature result) {
            result.Mass = -1;
            result.ChromScanIdTop = -1;
            result.ChromScanIdLeft = -1;
            result.ChromScanIdRight = -1;
            result.ChromXsTop = new ChromXs(0, ChromXType.Mz, ChromXUnit.Mz);
            result.ChromXsLeft = new ChromXs(0, ChromXType.Mz, ChromXUnit.Mz);
            result.ChromXsRight = new ChromXs(0, ChromXType.Mz, ChromXUnit.Mz);
            result.PeakHeightTop = 0;
            result.PeakHeightLeft = 0;
            result.PeakHeightRight = 0;
            result.PeakAreaAboveZero = 0;
        }
    }
}
