using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Annotation {
    public class LcmsFastaAnnotator : ProteomicsStandardRestorableBase, ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> {

        private static readonly IComparer<IMSScanProperty> comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);
        private readonly IMatchResultRefer<PeptideMsReference, MsScanMatchResult> ReferObject;
        public LcmsFastaAnnotator(ShotgunProteomicsDB reference, MsRefSearchParameterBase msrefSearchParameter, ProteomicsParameter proteomicsParameter,
            string annotatorID, SourceType type) : base(reference, msrefSearchParameter, proteomicsParameter, annotatorID, type) {
            PeptideMsRef.Sort(comparer);
        }

        public MsScanMatchResult Annotate(IPepAnnotationQuery query) {
            var msrefParam = query.MsRefSearchParameter ?? MsRefSearchParameter;
            var proteomicsParam = query.ProteomicsParameter ?? ProteomicsParameter;
            return FindCandidatesCore(query.Property, query.Scan, query.Isotopes, msrefParam, proteomicsParam).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IPepAnnotationQuery query) {
            var parameter = query.MsRefSearchParameter ?? MsRefSearchParameter;
            var proteomicsParam = query.ProteomicsParameter ?? ProteomicsParameter;
            return FindCandidatesCore(query.Property, query.Scan, query.Isotopes, parameter, proteomicsParam);
        }

        private List<MsScanMatchResult> FindCandidatesCore(
       IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, 
       MsRefSearchParameterBase msrefSearchParam, ProteomicsParameter proteomicsParam) {
            (var lo, var hi) = SearchBoundIndex(property, PeptideMsRef, msrefSearchParam.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(hi - lo);
            for (var i = lo; i < hi; i++) {
                var candidate = PeptideMsRef[i];
                if (msrefSearchParam.IsUseTimeForAnnotationFiltering
                    && Math.Abs(property.ChromXs.RT.Value - candidate.ChromXs.RT.Value) > msrefSearchParam.RtTolerance) {
                    continue;
                }
                var result = CalculateScoreCore(property, scan, candidate, msrefSearchParam, proteomicsParam, this.SourceType, this.Key);
                result.LibraryIDWhenOrdered = i;
                ValidateCore(result, property, scan, candidate, msrefSearchParam, proteomicsParam);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IPepAnnotationQuery query, PeptideMsReference reference) {
            var parameter = query.MsRefSearchParameter ?? MsRefSearchParameter;
            var proteomicsParam = query.ProteomicsParameter ?? ProteomicsParameter;
            var result = CalculateScoreCore(query.Property, query.Scan, reference, parameter, proteomicsParam, this.SourceType, this.Key);
            ValidateCore(result, query.Property, query.Scan, reference, parameter, proteomicsParam);
            return result;
        }

        private static MsScanMatchResult CalculateScoreCore(
            IMSIonProperty property, IMSScanProperty scan, 
            PeptideMsReference reference, 
            MsRefSearchParameterBase msSearchParam, ProteomicsParameter proteomicsParam, SourceType type, string annotatorID) {

            var result = MsScanMatching.CompareMS2ScanProperties(scan, reference, msSearchParam, Common.Enum.TargetOmics.Proteomics, -1,
                null, null, proteomicsParam.AndromedaDelta, proteomicsParam.AndromedaMaxPeaks);

            result.Source = type;
            result.AnnotatorID = annotatorID;

            return result;
        }

        private static (int lo, int hi) SearchBoundIndex(IMSIonProperty property, IReadOnlyList<PeptideMsReference> pepDB, double ms1Tolerance) {

            ms1Tolerance = CalculateMassTolerance(ms1Tolerance, property.PrecursorMz);
            var dummy = new MSScanProperty { PrecursorMz = property.PrecursorMz - ms1Tolerance };
            var lo = SearchCollection.LowerBound(pepDB, dummy, comparer);
            dummy.PrecursorMz = property.PrecursorMz + ms1Tolerance;
            var hi = SearchCollection.UpperBound(pepDB, dummy, lo, pepDB.Count, comparer);
            return (lo, hi);
        }

        public List<PeptideMsReference> Search(IPepAnnotationQuery query) {
            var parameter = query.MsRefSearchParameter ?? MsRefSearchParameter;
            (var lo, var hi) = SearchBoundIndex(query.Property, PeptideMsRef, parameter.Ms1Tolerance);
            return PeptideMsRef.GetRange(lo, hi - lo);
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        public void Validate(MsScanMatchResult result, IPepAnnotationQuery query, PeptideMsReference reference) {
            var parameter = query.MsRefSearchParameter ?? MsRefSearchParameter;
            var proteomicsParam = query.ProteomicsParameter ?? ProteomicsParameter;
            ValidateCore(result, query.Property, query.Scan, reference, parameter, proteomicsParam);
        }

        private static void ValidateCore(
            MsScanMatchResult result,
            IMSIonProperty property, IMSScanProperty scan,
            PeptideMsReference reference,
            MsRefSearchParameterBase msrefSearchParam,
            ProteomicsParameter proteomicsParam) {

            ValidateBase(result, property, reference, msrefSearchParam, proteomicsParam);
        }

        private static void ValidateBase(MsScanMatchResult result, IMSIonProperty property, PeptideMsReference reference,
            MsRefSearchParameterBase parameter, ProteomicsParameter proteomicsParam) {
            result.IsSpectrumMatch = result.WeightedDotProduct >= parameter.WeightedDotProductCutOff
                && result.SimpleDotProduct >= parameter.SimpleDotProductCutOff
                && result.ReverseDotProduct >= parameter.ReverseDotProductCutOff
                && result.MatchedPeaksPercentage >= parameter.MatchedPeaksPercentageCutOff
                && result.MatchedPeaksCount >= parameter.MinimumSpectrumMatch
                && result.AndromedaScore >= parameter.AndromedaScoreCutOff;

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;
            result.IsRtMatch = Math.Abs(property.ChromXs.RT.Value - reference.ChromXs.RT.Value) <= parameter.RtTolerance;
        }


        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.Argmax(result => result.TotalScore);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = MsRefSearchParameter;
            }
            var filtered = new List<MsScanMatchResult>();
            foreach (var result in results) {
                if (Ms2Filtering(result, parameter)) {
                    continue;
                }
                if (result.TotalScore < parameter.TotalScoreCutoff) {
                    continue;
                }
                filtered.Add(result);
            }
            return filtered;
        }

        private static bool Ms2Filtering(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            if (!result.IsPrecursorMzMatch && !result.IsSpectrumMatch) {
                return false;
            }
            if (result.WeightedDotProduct < parameter.WeightedDotProductCutOff
                || result.SimpleDotProduct < parameter.SimpleDotProductCutOff
                || result.ReverseDotProduct < parameter.ReverseDotProductCutOff
                || result.MatchedPeaksPercentage < parameter.MatchedPeaksPercentageCutOff
                || result.MatchedPeaksCount < parameter.MinimumSpectrumMatch
                || result.AndromedaScore < parameter.AndromedaScoreCutOff) {
                return false;
            }
            return true;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return FilterByThreshold(results, parameter)
                .Where(result => result.IsPrecursorMzMatch && result.IsSpectrumMatch)
                .ToList();
        }

        public override PeptideMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public double CalculateAnnotatedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            throw new NotImplementedException();
        }

        public double CalculateSuggestedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            throw new NotImplementedException();
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            throw new NotImplementedException();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            throw new NotImplementedException();
        }
    }
}

