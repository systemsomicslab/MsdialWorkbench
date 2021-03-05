using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment
{
    public abstract class GcmsPeakJoiner : IPeakJoiner
    {
        public static GcmsPeakJoiner CreateRTJoiner(RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, double mzTol, double rtTol) {
            return new GcmsRTPeakJoiner(riCompoundType, msMatchParam, mzTol, rtTol);
        }

        public static GcmsPeakJoiner CreateRIJoiner(RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, double mzTol, double riTol) {
            return new GcmsRIPeakJoiner(riCompoundType, msMatchParam, mzTol, riTol);
        }

        protected readonly AlignmentIndexType indextype;
        protected readonly IComparer<IMSScanProperty> comparer;
        protected readonly RiCompoundType riCompoundType;
        protected readonly MsRefSearchParameterBase msMatchParam;

        protected GcmsPeakJoiner(AlignmentIndexType indextype, RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, IComparer<IMSScanProperty> comparer) {
            this.indextype = indextype;
            this.comparer = comparer;
            this.riCompoundType = riCompoundType;
            this.msMatchParam = msMatchParam;
        }

        public List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

            var master = GetMasterList(analysisFiles, referenceId, accessor);
            var spots = JoinAll(master, analysisFiles, accessor);
            return spots;
        }

        protected abstract List<IMSScanProperty> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor);
        protected abstract List<AlignmentSpotProperty> JoinAll(List<IMSScanProperty> master, IReadOnlyList<AnalysisFileBean> analysisFiles, DataAccessor accessor);

        protected bool IsSimilarTo(IMSScanProperty x, IMSScanProperty y) {
            var result = MsScanMatching.CompareEIMSScanProperties(x, y, this.msMatchParam, indextype == AlignmentIndexType.RI);
            var isRetentionMatch = indextype == AlignmentIndexType.RI ? result.IsRiMatch : result.IsRtMatch;
            return result.IsSpectrumMatch && isRetentionMatch;
        }

        protected double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            var result = MsScanMatching.CompareEIMSScanProperties(x, y, this.msMatchParam, indextype == AlignmentIndexType.RI);
            return result.TotalScore;
        }

        protected static List<AlignmentSpotProperty> GetSpots(IReadOnlyCollection<IMSScanProperty> masters, IEnumerable<AnalysisFileBean> analysisFiles) {
            var masterId = 0;
            return InitSpots(masters, analysisFiles, ref masterId);
        }

        protected static List<AlignmentSpotProperty> InitSpots(IEnumerable<IMSScanProperty> scanProps,
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
                };
                spot.InternalStandardAlignmentID = spot.MasterAlignmentID;

                var peaks = new List<AlignmentChromPeakFeature>();
                foreach (var file in analysisFiles) {
                    peaks.Add(new AlignmentChromPeakFeature
                    {
                        MasterPeakID = -1,
                        PeakID = -1,
                        FileID = file.AnalysisFileId,
                        FileName = file.AnalysisFileName,
                    });
                }
                spot.AlignedPeakProperties = peaks;

                if (scanProp is ChromatogramPeakFeature chrom)
                    spot.AlignmentDriftSpotFeatures = InitSpots(chrom.DriftChromFeatures, analysisFiles, ref masterId, spot.AlignmentID);
                else
                    spot.AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>();

                spots.Add(spot);
            }

            return spots;
        }
    }

    public class GcmsRTPeakJoiner : GcmsPeakJoiner
    {
        private readonly double mzTol, rtTol;
        private readonly double mzBucket, rtBucket;
        private readonly int mzWidth, rtWidth;

        public GcmsRTPeakJoiner(RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, double mzTol, double rtTol)
            : base(AlignmentIndexType.RT, riCompoundType, msMatchParam, ChromXsComparer.RTComparer) {

            this.mzTol = mzTol;
            this.mzBucket = mzTol * 2;
            this.mzWidth = (int)Math.Ceiling(this.mzTol / this.mzBucket);
            this.rtTol = rtTol;
            this.rtBucket = rtTol * 2;
            this.rtWidth = (int)Math.Ceiling(this.rtTol / this.rtBucket);
        }

        protected override List<IMSScanProperty> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
            if (referenceFile == null) return new List<IMSScanProperty>();

            var master = accessor.GetMSScanProperties(referenceFile)
                                 .GroupBy(prop => ((int)Math.Ceiling(prop.ChromXs.RT.Value / rtBucket), (int)Math.Ceiling(prop.PrecursorMz / mzBucket)))
                                 .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId)
                    continue;
                var target = accessor.GetMSScanProperties(analysisFile);
                MergeChromatogramPeaks(master, target);
            }

            return master.Values.SelectMany(props => props).ToList();
        }

        private void MergeChromatogramPeaks(IDictionary<(int, int), List<IMSScanProperty>> master, IEnumerable<IMSScanProperty> targets) {
            foreach (var target in targets) {
                SetToMaster(master, target);
            }
        }

        private bool SetToMaster(IDictionary<(int, int), List<IMSScanProperty>> master, IMSScanProperty target) {
            var mzTarget = (int)Math.Ceiling(target.PrecursorMz / mzBucket);
            var rtTarget = (int)Math.Ceiling(target.ChromXs.RT.Value / rtBucket);
            for(int rtIdc = rtTarget - rtWidth; rtIdc <= rtTarget + rtWidth; rtIdc++) { // in many case, rtIdc is from rtTarget - 1 to rtTarget + 1
                for(int mzIdc = mzTarget - mzWidth; mzIdc <= mzTarget + mzWidth; mzIdc++) { // in many case, mzIdc is from mzTarget - 1 to mzTarget + 1
                    if (master.ContainsKey((rtIdc, mzIdc))) {
                        foreach (var candidate in master[(rtIdc, mzIdc)]) {
                            if (IsSimilarTo(candidate, target)) {
                                return false;
                            }
                        }
                    }
                }
            }
            if (!master.ContainsKey((rtTarget, mzTarget)))
                master[(rtTarget, mzTarget)] = new List<IMSScanProperty>();
            master[(rtTarget, mzTarget)].Add(target);
            return true;
        }

        protected override List<AlignmentSpotProperty> JoinAll(List<IMSScanProperty> master, IReadOnlyList<AnalysisFileBean> analysisFiles, DataAccessor accessor) {
            var result = GetSpots(master, analysisFiles);
            
            foreach (var analysisFile in analysisFiles) {
                var chromatogram = accessor.GetMSScanProperties(analysisFile);
                AlignPeaksToMaster(result, master, chromatogram, analysisFile.AnalysisFileId);
            }
            
            return result;
        }

        private void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
            var n = masters.Count;
            var maxMatchs = new double[n];
            var dummy = new ChromatogramPeakFeature(); // dummy instance for binary search.

            foreach (var target in targets) {
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                dummy.ChromXs = new ChromXs(target.ChromXs.RT.Value - rtTol, ChromXType.RT, ChromXUnit.Min);
                dummy.PrecursorMz = target.PrecursorMz - mzTol;
                var lo = SearchCollection.LowerBound(masters, dummy, comparer);
                for (var i = lo; i < n; i++) {
                    if (target.ChromXs.RT.Value + rtTol < masters[i].ChromXs.RT.Value)
                        break;
                    if (!IsSimilarTo(masters[i], target))
                        continue;
                    var factor = GetSimilality(masters[i], target);
                    if (factor > maxMatchs[i] && factor > matchFactor) {
                        matchIdx = i;
                        matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue)
                    DataObjConverter.SetAlignmentChromPeakFeatureFromMSDecResult(spots[matchIdx.Value].AlignedPeakProperties[fileId], target as MSDecResult);
            }
        }
    }

    public class GcmsRIPeakJoiner : GcmsPeakJoiner
    {
        private readonly double mzTol, riTol;
        private readonly double mzBucket, riBucket;
        private readonly int mzWidth, riWidth;

        public GcmsRIPeakJoiner(RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, double mzTol, double riTol)
            :base(AlignmentIndexType.RI, riCompoundType, msMatchParam, ChromXsComparer.RIComparer) {
            this.mzTol = mzTol;
            this.mzBucket = mzTol * 2;
            this.mzWidth = (int)Math.Ceiling(this.mzTol / this.mzBucket);
            this.riTol = riTol;
            this.riBucket = riTol * 2;
            this.riWidth = (int)Math.Ceiling(this.riTol / this.riBucket);
        }

        protected override List<IMSScanProperty> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
            if (referenceFile == null) return new List<IMSScanProperty>();

            var master = accessor.GetMSScanProperties(referenceFile)
                                 .GroupBy(prop => ((int)Math.Ceiling(prop.ChromXs.RI.Value / riBucket), (int)Math.Ceiling(prop.PrecursorMz / mzBucket)))
                                 .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId)
                    continue;
                var target = accessor.GetMSScanProperties(analysisFile);
                MergeChromatogramPeaks(master, target);
            }

            return master.Values.SelectMany(props => props).ToList();
        }

        private void MergeChromatogramPeaks(IDictionary<(int, int), List<IMSScanProperty>> master, IEnumerable<IMSScanProperty> targets) {
            foreach (var target in targets) {
                SetToMaster(master, target);
            }
        }

        private bool SetToMaster(IDictionary<(int, int), List<IMSScanProperty>> master, IMSScanProperty target) {
            var mzTarget = (int)Math.Ceiling(target.PrecursorMz / mzBucket);
            var riTarget = (int)Math.Ceiling(target.ChromXs.RI.Value / riBucket);
            for(int riIdc = riTarget - riWidth; riIdc <= riTarget + riWidth; riIdc++) { // in many case, riIdc is from riTarget - 1 to riTarget + 1
                for(int mzIdc = mzTarget - mzWidth; mzIdc <= mzTarget + mzWidth; mzIdc++) { // in many case, mzIdc is from mzTarget - 1 to mzTarget + 1
                    if (master.ContainsKey((riIdc, mzIdc))) {
                        foreach (var candidate in master[(riIdc, mzIdc)]) {
                            if (IsSimilarTo(candidate, target)) {
                                return false;
                            }
                        }
                    }
                }
            }
            if (!master.ContainsKey((riTarget, mzTarget)))
                master[(riTarget, mzTarget)] = new List<IMSScanProperty>();
            master[(riTarget, mzTarget)].Add(target);
            return true;
        }

        protected override List<AlignmentSpotProperty> JoinAll(List<IMSScanProperty> master, IReadOnlyList<AnalysisFileBean> analysisFiles, DataAccessor accessor) {
            var result = GetSpots(master, analysisFiles);
            
            foreach (var analysisFile in analysisFiles) {
                var chromatogram = accessor.GetMSScanProperties(analysisFile);
                AlignPeaksToMaster(result, master, chromatogram, analysisFile.AnalysisFileId);
            }
            
            return result;
        }

        private void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
            var n = masters.Count;
            var maxMatchs = new double[n];
            var dummy = new ChromatogramPeakFeature(); // dummy instance for binary search.

            foreach (var target in targets) {
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                dummy.ChromXs = new ChromXs(target.ChromXs.RI.Value - riTol, ChromXType.RI, ChromXUnit.None);
                dummy.PrecursorMz = target.PrecursorMz - mzTol;
                var lo = SearchCollection.LowerBound(masters, dummy, comparer);
                for (var i = lo; i < n; i++) {
                    if (target.ChromXs.RI.Value + riTol < masters[i].ChromXs.RI.Value)
                        break;
                    if (!IsSimilarTo(masters[i], target))
                        continue;
                    var factor = GetSimilality(masters[i], target);
                    if (factor > maxMatchs[i] && factor > matchFactor) {
                        matchIdx = i;
                        matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue)
                    DataObjConverter.SetAlignmentChromPeakFeatureFromMSDecResult(spots[matchIdx.Value].AlignedPeakProperties[fileId], target as MSDecResult);
            }
        }
    }
}
