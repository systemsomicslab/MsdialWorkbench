using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcImMsApi.Algorithm.Annotation
{
    public sealed class AccumulatedPeakEvaluator : IMatchResultEvaluator<ChromatogramPeakFeature>, IMatchResultEvaluator<AlignmentSpotProperty>
    {
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

        public AccumulatedPeakEvaluator(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
        }

        List<AlignmentSpotProperty> IMatchResultEvaluator<AlignmentSpotProperty>.FilterByThreshold(IEnumerable<AlignmentSpotProperty> results) {
            return results.Where(result => _evaluator.FilterByThreshold(result.AlignmentDriftSpotFeatures.Select(spot => spot.MatchResults.Representative)).Any()).ToList();
        }

        List<ChromatogramPeakFeature> IMatchResultEvaluator<ChromatogramPeakFeature>.FilterByThreshold(IEnumerable<ChromatogramPeakFeature> results) {
            return results.Where(result => _evaluator.FilterByThreshold(result.DriftChromFeatures.Select(spot => spot.MatchResults.Representative)).Any()).ToList();
        }

        bool IMatchResultEvaluator<AlignmentSpotProperty>.IsAnnotationSuggested(AlignmentSpotProperty result) {
            return result.AlignmentDriftSpotFeatures.Any(spot => spot.MatchResults.IsAnnotationSuggested(_evaluator));
        }

        bool IMatchResultEvaluator<ChromatogramPeakFeature>.IsAnnotationSuggested(ChromatogramPeakFeature result) {
            return result.DriftChromFeatures.Any(spot => spot.MatchResults.IsAnnotationSuggested(_evaluator));
        }

        bool IMatchResultEvaluator<AlignmentSpotProperty>.IsReferenceMatched(AlignmentSpotProperty result) {
            return result.AlignmentDriftSpotFeatures.Any(spot => spot.MatchResults.IsReferenceMatched(_evaluator));
        }

        bool IMatchResultEvaluator<ChromatogramPeakFeature>.IsReferenceMatched(ChromatogramPeakFeature result) {
            return result.DriftChromFeatures.Any(spot => spot.MatchResults.IsReferenceMatched(_evaluator));
        }

        List<AlignmentSpotProperty> IMatchResultEvaluator<AlignmentSpotProperty>.SelectReferenceMatchResults(IEnumerable<AlignmentSpotProperty> results) {
            return results.Where(result => _evaluator.SelectReferenceMatchResults(result.AlignmentDriftSpotFeatures.Select(spot => spot.MatchResults.Representative)).Any()).ToList();
        }

        List<ChromatogramPeakFeature> IMatchResultEvaluator<ChromatogramPeakFeature>.SelectReferenceMatchResults(IEnumerable<ChromatogramPeakFeature> results) {
            return results.Where(result => _evaluator.SelectReferenceMatchResults(result.DriftChromFeatures.Select(spot => spot.MatchResults.Representative)).Any()).ToList();
        }

        AlignmentSpotProperty IMatchResultEvaluator<AlignmentSpotProperty>.SelectTopHit(IEnumerable<AlignmentSpotProperty> results) {
            var pairs = results.SelectMany(result => result.AlignmentDriftSpotFeatures.Select(spot => (spot.MatchResults.Representative, spot))).ToArray();
            var top = _evaluator.SelectTopHit(pairs.Select(pair => pair.Representative));
            return pairs.FirstOrDefault(pair => pair.Representative == top).spot;
        }

        ChromatogramPeakFeature IMatchResultEvaluator<ChromatogramPeakFeature>.SelectTopHit(IEnumerable<ChromatogramPeakFeature> results) {
            var pairs = results.SelectMany(result => result.DriftChromFeatures.Select(spot => (spot.MatchResults.Representative, spot))).ToArray();
            var top = _evaluator.SelectTopHit(pairs.Select(pair => pair.Representative));
            return pairs.FirstOrDefault(pair => pair.Representative == top).spot;
        }
    }
}
