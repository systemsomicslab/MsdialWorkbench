using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class MsScanMatchResultEvaluator : IMatchResultEvaluator<MsScanMatchResult>
    {
        public MsScanMatchResultEvaluator(MsRefSearchParameterBase searchParameter) {

        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }

            return results.Where(result => result.IsAnnotationSuggested || result.IsReferenceMatched).ToList();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            if (result is null) {
                throw new ArgumentNullException(nameof(result));
            }
            return result.IsAnnotationSuggested;
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            if (result is null) {
                throw new ArgumentNullException(nameof(result));
            }

            return result.IsReferenceMatched;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }

            return results.Where(result => result.IsReferenceMatched).ToList();
        }

        /// <summary>
        /// The best of several candidates from ONE annotator.
        /// </summary>
        /// <remarks>
        /// The evidence rank sits below the two verdicts and above the score. Below them because
        /// they are the stronger statement -- the annotator's full acceptance criteria were met --
        /// and because moving it above would change what a text-database precursor-only match
        /// outranks, which is a separate decision from this one. Above the score because a
        /// qualitative judgement that a spectrum decided a name is not something a large enough
        /// similarity should be able to buy past.
        ///
        /// Candidates that record no evidence source all rank alike, so a project written before
        /// the record existed is ordered exactly as it was; Argmax keeps the first of a tie.
        /// </remarks>
        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }

            return results.DefaultIfEmpty().Argmax(result => (result?.IsReferenceMatched ?? false, result?.IsAnnotationSuggested ?? false, AnnotationEvidence.RankOf(result), result?.TotalScore ?? double.MinValue));
        }
    }
}
