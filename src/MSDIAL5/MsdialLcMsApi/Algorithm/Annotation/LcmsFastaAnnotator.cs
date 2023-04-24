using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Function;
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
        //private readonly List<PeptideMsReference> OriginalOrderedDecoyPeptideMsRef;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public LcmsFastaAnnotator(ShotgunProteomicsDB reference, MsRefSearchParameterBase msrefSearchParameter, ProteomicsParameter proteomicsParameter,
            string annotatorID, SourceType type, int priority) : base(reference, msrefSearchParameter, proteomicsParameter, annotatorID, priority, type) {
            Id = annotatorID;
            PeptideMsRef.Sort(comparer);
            DecoyPeptideMsRef.Sort(comparer);
            ReferObject = reference;

            //OriginalOrderedDecoyPeptideMsRef = reference.DecoyPeptideMsRef;
            evaluator = new MsScanMatchResultEvaluator(msrefSearchParameter);
        }

        public string Id { get; }

        public MsScanMatchResult Annotate(IPepAnnotationQuery query) {
            var msrefParam = query.MsRefSearchParameter ?? MsRefSearchParameter;
            var proteomicsParam = query.ProteomicsParameter ?? ProteomicsParameter;
            return FindCandidatesCore(query.Property, query.Scan, query.Isotopes, query.IonFeature, PeptideMsRef, msrefParam, proteomicsParam).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IPepAnnotationQuery query) {

            if (query.Scan.Spectrum.IsEmptyOrNull()) return new List<MsScanMatchResult>();
            var parameter = query.MsRefSearchParameter ?? MsRefSearchParameter;
            var proteomicsParam = query.ProteomicsParameter ?? ProteomicsParameter;
            var pepResults = FindCandidatesCore(query.Property, query.Scan, query.Isotopes, query.IonFeature, PeptideMsRef, parameter, proteomicsParam);
            if (pepResults.IsEmptyOrNull()) return new List<MsScanMatchResult>();
            var repForwardResult = pepResults[0];
            var repPeptide = Refer(repForwardResult);

            // try method 1
            //var decoyPep = DecoyCreator.Convert2DecoyPeptide(repPeptide.Peptide);
            //var repReverseRef = new PeptideMsReference(decoyPep, null, -1, repPeptide.AdductType,
            //    repPeptide.ScanID, repPeptide.MinMs2, repPeptide.MaxMs2, repPeptide.CollisionType);

            //var decoyResult = FindCandidatesCore(query.Property, query.Scan, query.Isotopes, query.IonFeature, repReverseRef, parameter, proteomicsParam);
            //if (decoyResult is null) {
            //    var repReverseResult = new MsScanMatchResult();
            //    repReverseResult.IsDecoy = true;
            //    return new List<MsScanMatchResult>() { repForwardResult, repReverseResult };
            //}
            //else {
            //    decoyResult.LibraryIDWhenOrdered = repForwardResult.LibraryIDWhenOrdered;

            //    repForwardResult.IsDecoy = false;
            //    decoyResult.IsDecoy = true;
            //    return new List<MsScanMatchResult>() { repForwardResult, decoyResult };
            //}

            var decoyResults = FindCandidatesCore(query.Property, query.Scan, query.Isotopes, query.IonFeature, DecoyPeptideMsRef, parameter, proteomicsParam);
            if (decoyResults.IsEmptyOrNull()) {
                repForwardResult.IsDecoy = false;
                var repReverseResult = new MsScanMatchResult() { IsDecoy = true };

                return new List<MsScanMatchResult>() { repForwardResult, repReverseResult };
            }
            else {
                var repReverseResult = decoyResults[0];
                repForwardResult.IsDecoy = false;
                repReverseResult.IsDecoy = true;
                return new List<MsScanMatchResult>() { repForwardResult, repReverseResult };
            }
        }

        private MsScanMatchResult FindCandidatesCore(
       IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, IonFeatureCharacter character,
       PeptideMsReference pepMsRef,
       MsRefSearchParameterBase msrefSearchParam, ProteomicsParameter proteomicsParam) {
            var candidate = pepMsRef;
            if (msrefSearchParam.IsUseTimeForAnnotationFiltering
                && Math.Abs(property.ChromXs.RT.Value - candidate.ChromXs.RT.Value) > msrefSearchParam.RtTolerance) {
                return null;
            }
            var result = CalculateScoreCore(property, scan, character, candidate, msrefSearchParam, proteomicsParam, this.SourceType, this.Key);
            result.LibraryIDWhenOrdered = -1;
            ValidateCore(result, property, scan, character, candidate, msrefSearchParam, proteomicsParam);
            return result;
        }


        private List<MsScanMatchResult> FindCandidatesCore(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, IonFeatureCharacter character, 
            List<PeptideMsReference> pepMsRef, 
            MsRefSearchParameterBase msrefSearchParam, ProteomicsParameter proteomicsParam) {
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
            return results.OrderByDescending(result => result.AndromedaScore).ToList();
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

            var result = MsScanMatching.CompareMS2ScanProperties(scan, character.Charge, reference, msSearchParam, Common.Enum.TargetOmics.Proteomics, -1,
                null, null, proteomicsParam.AndromedaDelta, proteomicsParam.AndromedaMaxPeaks);
            var singlyChargedMz = MolecularFormulaUtility.ConvertSinglyChargedPrecursorMzAsProtonAdduct(property.PrecursorMz, character.Charge);
            var ms1Tol = CalculateMassTolerance(msSearchParam.Ms1Tolerance, property.PrecursorMz);
            _ = MsScanMatching.GetGaussianSimilarity(singlyChargedMz, reference.PrecursorMz, ms1Tol, out bool isMs1Match);

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
            result.IsReferenceMatched = result.IsPrecursorMzMatch && (!query.Parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch) && result.IsSpectrumMatch;
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch && (!query.Parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch) && !result.IsReferenceMatched;
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


        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            return evaluator.SelectTopHit(results);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            return evaluator.FilterByThreshold(results);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            return evaluator.SelectReferenceMatchResults(results);
        }

        public override PeptideMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            return evaluator.IsReferenceMatched(result);
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            return evaluator.IsAnnotationSuggested(result);
        }
    }
}

