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
    public class LcmsFastaAnnotator : 
        ProteomicsStandardRestorableBase, 
        ISerializableAnnotator<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult, ShotgunProteomicsDB> {

        private static readonly IComparer<IMSScanProperty> comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);
        private readonly IMatchResultRefer<PeptideMsReference, MsScanMatchResult> ReferObject;

        public int Priority { get; }

        public LcmsFastaAnnotator(ShotgunProteomicsDB reference, MsRefSearchParameterBase msrefSearchParameter, ProteomicsParameter proteomicsParameter,
            string annotatorID, SourceType type, int priority) : base(reference, msrefSearchParameter, proteomicsParameter, annotatorID, type) {
            PeptideMsRef.Sort(comparer);
            DecoyPeptideMsRef.Sort(comparer);
            ReferObject = reference;
            Priority = priority;
        }

        public MsScanMatchResult Annotate(IPepAnnotationQuery query) {
            var msrefParam = query.MsRefSearchParameter ?? MsRefSearchParameter;
            var proteomicsParam = query.ProteomicsParameter ?? ProteomicsParameter;
            return FindCandidatesCore(query.Property, query.Scan, query.Isotopes, query.IonFeature, PeptideMsRef, msrefParam, proteomicsParam).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IPepAnnotationQuery query) {
            var parameter = query.MsRefSearchParameter ?? MsRefSearchParameter;
            var proteomicsParam = query.ProteomicsParameter ?? ProteomicsParameter;
            var pepResults = FindCandidatesCore(query.Property, query.Scan, query.Isotopes, query.IonFeature, PeptideMsRef, parameter, proteomicsParam);
            var decoyResults = FindCandidatesCore(query.Property, query.Scan, query.Isotopes, query.IonFeature, DecoyPeptideMsRef, parameter, proteomicsParam);

            //if (Math.Abs(query.Property.PrecursorMz - 447.54460) < 0.01 && Math.Abs(query.Property.ChromXs.RT.Value - 21.866) < 0.1) {
            //    Console.WriteLine();
            //}
            if (pepResults.IsEmptyOrNull() || decoyResults.IsEmptyOrNull()) return new List<MsScanMatchResult>();
            else {
                pepResults[0].IsDecoy = false;
                decoyResults[0].IsDecoy = true;
                return new List<MsScanMatchResult>() { pepResults[0], decoyResults[0] };
            }
        }

        private List<MsScanMatchResult> FindCandidatesCore(
       IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, IonFeatureCharacter character, 
       List<PeptideMsReference> pepMsRef, 
       MsRefSearchParameterBase msrefSearchParam, ProteomicsParameter proteomicsParam) {
            //if (Math.Abs(property.PrecursorMz - 447.54460) < 0.01 && Math.Abs(property.ChromXs.RT.Value - 21.866) < 0.1) {
            //    Console.WriteLine();
            //}
            (var lo, var hi) = SearchBoundIndex(property, character, pepMsRef, msrefSearchParam.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(hi - lo);
            for (var i = lo; i < hi; i++) {
                var candidate = pepMsRef[i];
                if (msrefSearchParam.IsUseTimeForAnnotationFiltering
                    && Math.Abs(property.ChromXs.RT.Value - candidate.ChromXs.RT.Value) > msrefSearchParam.RtTolerance) {
                    continue;
                }
                var result = CalculateScoreCore(property, scan, character, candidate, msrefSearchParam, proteomicsParam, this.SourceType, this.Key);
                result.LibraryIDWhenOrdered = i;
                ValidateCore(result, property, scan, character, candidate, msrefSearchParam, proteomicsParam);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IPepAnnotationQuery query, PeptideMsReference reference) {
            var parameter = query.MsRefSearchParameter ?? MsRefSearchParameter;
            var proteomicsParam = query.ProteomicsParameter ?? ProteomicsParameter;
            var result = CalculateScoreCore(query.Property, query.Scan, query.IonFeature, reference, parameter, proteomicsParam, this.SourceType, this.Key);
            ValidateCore(result, query.Property, query.Scan, query.IonFeature, reference, parameter, proteomicsParam);
            return result;
        }

        private MsScanMatchResult CalculateScoreCore(
            IMSIonProperty property, IMSScanProperty scan, IonFeatureCharacter character,
            PeptideMsReference reference, 
            MsRefSearchParameterBase msSearchParam, ProteomicsParameter proteomicsParam, SourceType type, string annotatorID) {

            var result = MsScanMatching.CompareMS2ScanProperties(scan, reference, msSearchParam, Common.Enum.TargetOmics.Proteomics, -1,
                null, null, proteomicsParam.AndromedaDelta, proteomicsParam.AndromedaMaxPeaks);
            var singlyChargedMz = MolecularFormulaUtility.ConvertSinglyChargedPrecursorMzAsProtonAdduct(property.PrecursorMz, character.Charge);
            var ms1Tol = CalculateMassTolerance(msSearchParam.Ms1Tolerance, property.PrecursorMz);
            var ms1Similarity = MsScanMatching.GetGaussianSimilarity(singlyChargedMz, reference.PrecursorMz, ms1Tol, out bool isMs1Match);

            result.IsPrecursorMzMatch = isMs1Match;
            result.TotalScore = (float)MsScanMatching.GetTotalScore(result, msSearchParam);

            result.Source = type;
            result.AnnotatorID = annotatorID;
            result.Priority = Priority;

            return result;
        }

        private static (int lo, int hi) SearchBoundIndex(IMSIonProperty property, IonFeatureCharacter character, IReadOnlyList<PeptideMsReference> pepDB, double ms1Tolerance) {

            var singlyChargedMz = MolecularFormulaUtility.ConvertSinglyChargedPrecursorMzAsProtonAdduct(property.PrecursorMz, character.Charge);

            ms1Tolerance = CalculateMassTolerance(ms1Tolerance, singlyChargedMz);
            var dummy = new MSScanProperty { PrecursorMz = singlyChargedMz - ms1Tolerance };
            var lo = SearchCollection.LowerBound(pepDB, dummy, comparer);
            dummy.PrecursorMz = singlyChargedMz + ms1Tolerance;
            var hi = SearchCollection.UpperBound(pepDB, dummy, lo, pepDB.Count, comparer);
            return (lo, hi);
        }

        public List<PeptideMsReference> Search(IPepAnnotationQuery query) {
            var parameter = query.MsRefSearchParameter ?? MsRefSearchParameter;
            (var lo, var hi) = SearchBoundIndex(query.Property, query.IonFeature, PeptideMsRef, parameter.Ms1Tolerance);
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
            ValidateCore(result, query.Property, query.Scan, query.IonFeature, reference, parameter, proteomicsParam);
        }

        private static void ValidateCore(
            MsScanMatchResult result,
            IMSIonProperty property, IMSScanProperty scan, IonFeatureCharacter character,
            PeptideMsReference reference,
            MsRefSearchParameterBase msrefSearchParam,
            ProteomicsParameter proteomicsParam) {

            ValidateBase(result, property, character, reference, msrefSearchParam, proteomicsParam);
        }

        private static void ValidateBase(MsScanMatchResult result, IMSIonProperty property, IonFeatureCharacter character, PeptideMsReference reference,
            MsRefSearchParameterBase parameter, ProteomicsParameter proteomicsParam) {
            result.IsSpectrumMatch = result.WeightedDotProduct >= parameter.WeightedDotProductCutOff
                && result.SimpleDotProduct >= parameter.SimpleDotProductCutOff
                && result.ReverseDotProduct >= parameter.ReverseDotProductCutOff
                && result.MatchedPeaksPercentage >= parameter.MatchedPeaksPercentageCutOff
                && result.MatchedPeaksCount >= parameter.MinimumSpectrumMatch
                && result.AndromedaScore >= parameter.AndromedaScoreCutOff;

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);

            var singlyChargedMz = MolecularFormulaUtility.ConvertSinglyChargedPrecursorMzAsProtonAdduct(property.PrecursorMz, character.Charge);
            result.IsPrecursorMzMatch = Math.Abs(singlyChargedMz - reference.PrecursorMz) <= ms1Tol;
            result.IsRtMatch = Math.Abs(property.ChromXs.RT.Value - reference.ChromXs.RT.Value) <= parameter.RtTolerance;
        }


        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.Argmax(result => result.TotalScore);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = MsRefSearchParameter;
            }
            return results.Where(result => SatisfySuggestedConditions(result, parameter)).ToList();
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
            if (parameter is null) {
                parameter = MsRefSearchParameter;
            }
            return results.Where(result => SatisfyRefMatchedConditions(result, parameter)).ToList();
        }

        public override PeptideMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return SatisfyRefMatchedConditions(result, parameter ?? MsRefSearchParameter);
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = MsRefSearchParameter;
            }
            return SatisfySuggestedConditions(result, parameter) && !SatisfyRefMatchedConditions(result, parameter);
        }

        private static bool SatisfyRefMatchedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch
                && result.IsSpectrumMatch
                && (!parameter.IsUseTimeForAnnotationFiltering || result.IsRtMatch);
        }

        private static bool SatisfySuggestedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch
                && (!parameter.IsUseTimeForAnnotationFiltering || result.IsRtMatch);
        }
    }
}

