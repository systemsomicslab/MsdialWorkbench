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
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment
{
    public abstract class GcmsPeakJoiner : IPeakJoiner
    {
        public static GcmsPeakJoiner CreateRTJoiner(RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, AlignmentBaseParameter alignmentParameter) {
            return new GcmsRTPeakJoiner(riCompoundType, msMatchParam, alignmentParameter);
        }

        public static GcmsPeakJoiner CreateRIJoiner(RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, double riTol, AlignmentBaseParameter alignmentParameter) {
            return new GcmsRIPeakJoiner(riCompoundType, msMatchParam, riTol, alignmentParameter);
        }

        protected readonly AlignmentIndexType indextype;
        protected readonly IComparer<IMSScanProperty> comparer;
        private readonly AlignmentBaseParameter _alignmentParameter;
        protected readonly RiCompoundType riCompoundType;
        protected readonly MsRefSearchParameterBase msMatchParam;

        protected GcmsPeakJoiner(AlignmentIndexType indextype, RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, IComparer<IMSScanProperty> comparer, AlignmentBaseParameter alignmentParameter) {
            this.indextype = indextype;
            this.comparer = comparer;
            _alignmentParameter = alignmentParameter ?? throw new ArgumentNullException(nameof(alignmentParameter));
            this.riCompoundType = riCompoundType;
            this.msMatchParam = msMatchParam;
        }

        public List<AlignmentSpotProperty> Join(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId, DataAccessor accessor) {

            var master = GetMasterList(analysisFiles, referenceId);
            var spots = JoinAll(master, analysisFiles);
            return spots;
        }

        protected abstract List<SpectrumFeature> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId);
        protected abstract List<AlignmentSpotProperty> JoinAll(List<SpectrumFeature> master, IReadOnlyList<AnalysisFileBean> analysisFiles);

        protected bool IsSimilarTo(SpectrumFeature x, SpectrumFeature y) {
            var result = MsScanMatching.CompareEIMSScanProperties(x.AnnotatedMSDecResult.MSDecResult, y.AnnotatedMSDecResult.MSDecResult, msMatchParam, _alignmentParameter.Ms1AlignmentFactor, _alignmentParameter.RetentionTimeAlignmentFactor, indextype == AlignmentIndexType.RI);
            var isRetentionMatch = indextype == AlignmentIndexType.RI ? result.IsRiMatch : result.IsRtMatch;
            return result.TotalScore > _alignmentParameter.Ms1AlignmentTolerance && isRetentionMatch;
        }

        protected double GetSimilality(SpectrumFeature x, SpectrumFeature y) {
            var result = MsScanMatching.CompareEIMSScanProperties(x.AnnotatedMSDecResult.MSDecResult, y.AnnotatedMSDecResult.MSDecResult, msMatchParam, _alignmentParameter.Ms1AlignmentFactor, _alignmentParameter.RetentionTimeAlignmentFactor, indextype == AlignmentIndexType.RI);
            return result.TotalScore;
        }

        protected List<AlignmentSpotProperty> GetSpots(IReadOnlyCollection<SpectrumFeature> masters, IEnumerable<AnalysisFileBean> analysisFiles, ChromXType mainType) {
            var masterId = 0;
            return InitSpots(masters, analysisFiles, mainType, ref masterId);
        }

        protected List<AlignmentSpotProperty> InitSpots(IEnumerable<SpectrumFeature> scanProps, IEnumerable<AnalysisFileBean> analysisFiles, ChromXType mainType, ref int masterId) {
            if (scanProps == null) return new List<AlignmentSpotProperty>();

            var spots = new List<AlignmentSpotProperty>();
            foreach ((var scanProp, var localId) in scanProps.WithIndex()) {
                var spot = new AlignmentSpotProperty
                {
                    MasterAlignmentID = masterId++,
                    AlignmentID = localId,
                    TimesCenter = scanProp.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop,
                    MassCenter = scanProp.QuantifiedChromatogramPeak.PeakFeature.Mass,
                    IonMode = scanProp.AnnotatedMSDecResult.MSDecResult.IonMode,
                    InternalStandardAlignmentID = -1
                };
                spot.TimesCenter.MainType = mainType;

                var peaks = new List<AlignmentChromPeakFeature>();
                foreach (var file in analysisFiles) {
                    peaks.Add(new AlignmentChromPeakFeature
                    {
                        MasterPeakID = -1,
                        PeakID = -1,
                        FileID = file.AnalysisFileId,
                        FileName = file.AnalysisFileName,
                        IonMode = scanProp.AnnotatedMSDecResult.MSDecResult.IonMode,
                    });
                }
                spot.AlignedPeakProperties = peaks;
                spot.AlignmentDriftSpotFeatures = new List<AlignmentSpotProperty>();
                spots.Add(spot);
            }

            return spots;
        }
    }

    internal sealed class GcmsRTPeakJoiner : GcmsPeakJoiner
    {
        private readonly double _rtTol;
        private readonly double _rtBucket;
        private readonly int _rtWidth;

        public GcmsRTPeakJoiner(RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, AlignmentBaseParameter alignmentParameter)
            : base(AlignmentIndexType.RT, riCompoundType, msMatchParam, ChromXsComparer.RTComparer, alignmentParameter) {
            _rtTol = alignmentParameter.RetentionTimeAlignmentTolerance;
            _rtBucket = alignmentParameter.RetentionTimeAlignmentTolerance * 2;
            _rtWidth = (int)Math.Ceiling(this._rtTol / this._rtBucket);
        }

        protected override List<SpectrumFeature> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceID) {
            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceID);
            if (referenceFile is null) {
                return new List<SpectrumFeature>(0);
            }
            var spectrumFeatures = referenceFile.LoadSpectrumFeatures();
            var master = spectrumFeatures.Items
                .GroupBy(prop => (int)Math.Ceiling(prop.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value / _rtBucket))
                .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceID) {
                    continue;
                }
                var target = analysisFile.LoadSpectrumFeatures();
                MergeSpectrumFeatures(master, target);
            }
            return master.Values.SelectMany(props => props).OrderBy(prop => (prop.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value, prop.QuantifiedChromatogramPeak.PeakFeature.Mass)).ToList();
        }

        private void MergeSpectrumFeatures(IDictionary<int, List<SpectrumFeature>> master, SpectrumFeatureCollection targets) {
            foreach (var target in targets.Items) {
                SetToMaster(master, target);
            }
        }

        private bool SetToMaster(IDictionary<int, List<SpectrumFeature>> master, SpectrumFeature target) {
            var rtTarget = (int)Math.Ceiling(target.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value / _rtBucket);
            for(int rtIdc = rtTarget - _rtWidth; rtIdc <= rtTarget + _rtWidth; rtIdc++) { // in many case, rtIdc is from rtTarget - 1 to rtTarget + 1
                if (master.ContainsKey(rtIdc)) {
                    foreach (var candidate in master[rtIdc]) {
                        if (IsSimilarTo(candidate, target)) {
                            return false;
                        }
                    }
                }
            }
            if (!master.ContainsKey(rtTarget)) {
                master[rtTarget] = new List<SpectrumFeature>();
            }
            master[rtTarget].Add(target);
            return true;
        }

        protected override List<AlignmentSpotProperty> JoinAll(List<SpectrumFeature> master, IReadOnlyList<AnalysisFileBean> analysisFiles) {
            var result = GetSpots(master, analysisFiles, ChromXType.RT);
            foreach (var analysisFile in analysisFiles) {
                var spectrumFeatures = analysisFile.LoadSpectrumFeatures();
                AlignPeaksToMaster(result, master, spectrumFeatures.Items, analysisFile.AnalysisFileId);
            }
            return result;
        }

        private void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<SpectrumFeature> masters, IReadOnlyList<SpectrumFeature> targets, int fileId) {
            var n = masters.Count;
            var maxMatchs = new double[n];

            foreach (var target in targets) {
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                var lo = SearchCollection.LowerBound(masters, (target.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value - _rtTol, target.QuantifiedChromatogramPeak.PeakFeature.Mass), (s, p) => (s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value, s.QuantifiedChromatogramPeak.PeakFeature.Mass).CompareTo(p));
                for (var i = lo; i < n; i++) {
                    if (target.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value + _rtTol < masters[i].QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RT.Value) {
                        break;
                    }
                    if (!IsSimilarTo(masters[i], target)) {
                        continue;
                    }
                    var factor = GetSimilality(masters[i], target);
                    if (factor > maxMatchs[i] && factor > matchFactor) {
                        matchIdx = i;
                        maxMatchs[i] = matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue) {
                    DataObjConverter.SetAlignmentChromPeakFeatureFromSpectrumFeature(spots[matchIdx.Value].AlignedPeakProperties[fileId], target, ChromXType.RT);
                }
            }
        }
    }

    internal sealed class GcmsRIPeakJoiner : GcmsPeakJoiner
    {
        private readonly double _riTol;
        private readonly double _riBucket;
        private readonly int _riWidth;

        public GcmsRIPeakJoiner(RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam, double riTol, AlignmentBaseParameter alignmentParameter)
            : base(AlignmentIndexType.RI, riCompoundType, msMatchParam, ChromXsComparer.RIComparer, alignmentParameter) {
            _riTol = riTol;
            _riBucket = riTol * 2;
            _riWidth = (int)Math.Ceiling(_riTol / _riBucket);
        }

        protected override List<SpectrumFeature> GetMasterList(IReadOnlyList<AnalysisFileBean> analysisFiles, int referenceId) {

            var referenceFile = analysisFiles.FirstOrDefault(file => file.AnalysisFileId == referenceId);
            if (referenceFile is null) {
                return new List<SpectrumFeature>(0);
            }

            var master = referenceFile.LoadSpectrumFeatures().Items
                .GroupBy(spectrum => (int)Math.Ceiling(spectrum.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value / _riBucket))
                 .ToDictionary(group => group.Key, group => group.ToList());

            foreach (var analysisFile in analysisFiles) {
                if (analysisFile.AnalysisFileId == referenceFile.AnalysisFileId)
                    continue;
                var target = analysisFile.LoadSpectrumFeatures();
                MergeChromatogramPeaks(master, target.Items);
            }

            return master.Values.SelectMany(props => props).ToList();
        }

        private void MergeChromatogramPeaks(IDictionary<int, List<SpectrumFeature>> master, IEnumerable<SpectrumFeature> targets) {
            foreach (var target in targets) {
                SetToMaster(master, target);
            }
        }

        private bool SetToMaster(IDictionary<int, List<SpectrumFeature>> master, SpectrumFeature target) {
            var riTarget = (int)Math.Ceiling(target.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value / _riBucket);
            for(int riIdc = riTarget - _riWidth; riIdc <= riTarget + _riWidth; riIdc++) { // in many case, riIdc is from riTarget - 1 to riTarget + 1
                if (master.ContainsKey(riIdc)) {
                    foreach (var candidate in master[riIdc]) {
                        if (IsSimilarTo(candidate, target)) {
                            return false;
                        }
                    }
                }
            }
            if (!master.ContainsKey(riTarget)) {
                master[riTarget] = new List<SpectrumFeature>();
            }
            master[riTarget].Add(target);
            return true;
        }

        protected override List<AlignmentSpotProperty> JoinAll(List<SpectrumFeature> master, IReadOnlyList<AnalysisFileBean> analysisFiles) {
            var result = GetSpots(master, analysisFiles, ChromXType.RI);
            
            foreach (var analysisFile in analysisFiles) {
                var spectrums = analysisFile.LoadSpectrumFeatures();
                AlignPeaksToMaster(result, master, spectrums.Items, analysisFile.AnalysisFileId);
            }
            
            return result;
        }

        private void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<SpectrumFeature> masters, IReadOnlyList<SpectrumFeature> targets, int fileId) {
            var maxMatchs = new double[masters.Count];
            foreach (var target in targets) {
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                var lo = SearchCollection.LowerBound(masters, (target.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value - _riTol, target.QuantifiedChromatogramPeak.PeakFeature.Mass), (s, p) => (s.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value, s.QuantifiedChromatogramPeak.PeakFeature.Mass).CompareTo(p));
                for (var i = lo; i < masters.Count; i++) {
                    if (target.QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value + _riTol < masters[i].QuantifiedChromatogramPeak.PeakFeature.ChromXsTop.RI.Value) {
                        break;
                    }
                    if (!IsSimilarTo(masters[i], target)) {
                        continue;
                    }
                    var factor = GetSimilality(masters[i], target);
                    if (factor > maxMatchs[i] && factor > matchFactor) {
                        matchIdx = i;
                        matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue) {
                    DataObjConverter.SetAlignmentChromPeakFeatureFromSpectrumFeature(spots[matchIdx.Value].AlignedPeakProperties[fileId], target, ChromXType.RI);
                }
            }
        }
    }
}
