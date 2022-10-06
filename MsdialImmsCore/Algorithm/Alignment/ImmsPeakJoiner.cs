using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Alignment
{
    public class ImmsPeakJoiner : IPeakJoiner
    {
        private static readonly IComparer<IMSScanProperty> Comparer;
        private readonly double mzTol, mzFactor, driftTol, driftFactor;

        private readonly double mzBucket, driftBucket;
        private readonly int mzWidth, driftWidth;

        static ImmsPeakJoiner() {
            Comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.DriftComparer);
        }

        public ImmsPeakJoiner(double mzTol, double mzFactor, double driftTol, double driftFactor) {
            this.mzTol = mzTol;
            this.mzFactor = mzFactor;
            this.driftTol = driftTol;
            this.driftFactor = driftFactor;

            this.mzBucket = mzTol * 2;
            this.mzWidth = (int)Math.Ceiling(this.mzTol / this.mzBucket);
            this.driftBucket = driftTol * 2;
            this.driftWidth = (int)Math.Ceiling(this.driftTol / this.driftBucket);
        }

        public List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {
            var master = GetMasterList(analysisFiles, referenceId, accessor);
            var spots = JoinAll(master, analysisFiles, accessor);
            return spots;
        }

        private List<IMSScanProperty> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
            if (referenceFile == null) return new List<IMSScanProperty>();

            var master = accessor.GetMSScanProperties(referenceFile)
                                 .GroupBy(prop => ((int)Math.Ceiling(prop.ChromXs.Drift.Value / driftBucket), (int)Math.Ceiling(prop.PrecursorMz / mzBucket)))
                                 .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId)
                    continue;
                var target = accessor.GetMSScanProperties(analysisFile);
                MergeChromatogramPeaks(master, target);
            }

            return master.Values.SelectMany(props => props).ToList();
        }

        public void MergeChromatogramPeaks(IDictionary<(int, int), List<IMSScanProperty>> master, IEnumerable<IMSScanProperty> targets) {
            foreach (var target in targets) {
                SetToMaster(master, target);
            }
        }

        private bool SetToMaster(IDictionary<(int, int), List<IMSScanProperty>> master, IMSScanProperty target) {
            var mzTarget = (int)Math.Ceiling(target.PrecursorMz / mzBucket);
            var driftTarget = (int)Math.Ceiling(target.ChromXs.Drift.Value / driftBucket);
            for(int driftIdc = driftTarget - driftWidth; driftIdc <= driftTarget + driftWidth; driftIdc++) { // in many case, driftIdc is from driftTarget - 1 to driftTarget + 1
                for(int mzIdc = mzTarget - mzWidth; mzIdc <= mzTarget + mzWidth; mzIdc++) { // in many case, mzIdc is from mzTarget - 1 to mzTarget + 1
                    if (master.ContainsKey((driftIdc, mzIdc))) {
                        foreach (var candidate in master[(driftIdc, mzIdc)]) {
                            if (IsSimilarTo(candidate, target)) {
                                return false;
                            }
                        }
                    }
                }
            }
            if (!master.ContainsKey((driftTarget, mzTarget)))
                master[(driftTarget, mzTarget)] = new List<IMSScanProperty>();
            master[(driftTarget, mzTarget)].Add(target);
            return true;
        }

        private bool IsSimilarTo(IMSScanProperty x, IMSScanProperty y) {
            return Math.Abs(x.PrecursorMz - y.PrecursorMz) <= mzTol
                && Math.Abs(x.ChromXs.Drift.Value - y.ChromXs.Drift.Value) <= driftTol;
        }

        private List<AlignmentSpotProperty> JoinAll(List<IMSScanProperty> master, IReadOnlyList<AnalysisFileBean> analysisFiles, DataAccessor accessor) {
            master = master.OrderBy(prop => (prop.PrecursorMz, prop.ChromXs.Drift.Value)).ToList();
            var result = GetSpots(master, analysisFiles);
            
            foreach (var analysisFile in analysisFiles) {
                var chromatogram = accessor.GetMSScanProperties(analysisFile);
                AlignPeaksToMaster(result, master, chromatogram, analysisFile.AnalysisFileId);
            }
            
            return result;
        }

        private static List<AlignmentSpotProperty> GetSpots(IReadOnlyCollection<IMSScanProperty> masters, IEnumerable<AnalysisFileBean> analysisFiles) {
            var masterId = 0;
            return InitSpots(masters, analysisFiles, ref masterId);
        }

        private static List<AlignmentSpotProperty> InitSpots(IEnumerable<IMSScanProperty> scanProps,
            IEnumerable<AnalysisFileBean> analysisFiles, ref int masterId, int parentId = -1) {

            if (scanProps == null) return new List<AlignmentSpotProperty>();

            var spots = new List<AlignmentSpotProperty>();
            foreach ((var scanProp, var localId) in scanProps.WithIndex()) {
                var spot = new AlignmentSpotProperty
                {
                    MasterAlignmentID = masterId++,
                    AlignmentID = localId,
                    ParentAlignmentID = parentId,
                    TimesCenter = scanProp.ChromXs,
                    MassCenter = scanProp.PrecursorMz,
                    IonMode = scanProp.IonMode,
                };
                spot.InternalStandardAlignmentID = -1;

                var peaks = new List<AlignmentChromPeakFeature>();
                foreach (var file in analysisFiles) {
                    peaks.Add(new AlignmentChromPeakFeature
                    {
                        MasterPeakID = -1,
                        PeakID = -1,
                        FileID = file.AnalysisFileId,
                        FileName = file.AnalysisFileName,
                        IonMode = scanProp.IonMode,
                    });
                }
                spot.AlignedPeakProperties = peaks;

                if (scanProp is ChromatogramPeakFeature chrom)
                    spot.AlignmentDriftSpotFeatures = InitSpots(chrom.DriftChromFeatures, analysisFiles, ref masterId, spot.AlignmentID);

                spots.Add(spot);
            }

            return spots;
        }

        public void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
            var n = masters.Count;
            var maxMatchs = new double[n];
            var dummy = new ChromatogramPeakFeature(); // dummy instance for binary search.

            foreach (var target in targets) {
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                dummy.ChromXs = new ChromXs(target.ChromXs.Drift.Value - driftTol, ChromXType.Drift, ChromXUnit.Msec);
                dummy.PrecursorMz = target.PrecursorMz - mzTol;
                var lo = SearchCollection.LowerBound(masters, dummy, Comparer);
                for (var i = lo; i < n; i++) {
                    if (target.ChromXs.Drift.Value + driftTol < masters[i].ChromXs.Drift.Value)
                        break;
                    if (!IsSimilarTo(masters[i], target))
                        continue;
                    var factor = GetSimilality(masters[i], target);
                    if (factor > maxMatchs[i] && factor > matchFactor) {
                        matchIdx = i;
                        maxMatchs[i] = matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue)
                    DataObjConverter.SetAlignmentChromPeakFeatureFromChromatogramPeakFeature(spots[matchIdx.Value].AlignedPeakProperties[fileId], target as ChromatogramPeakFeature);
            }
        }

        private double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            return mzFactor * Math.Exp(-.5 * Math.Pow((x.PrecursorMz - y.PrecursorMz) / mzTol, 2))
                + driftFactor * Math.Exp(-.5 * Math.Pow((x.ChromXs.Drift.Value - y.ChromXs.Drift.Value) / driftTol, 2));
        }
    }
}
