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
        private readonly bool canUseSpectrumMatch;
        private readonly MsRefSearchParameterBase searchParameter;

        private MsScanMatchResultEvaluator(bool canUseSpectrumMatch, MsRefSearchParameterBase searchParameter) {
            this.canUseSpectrumMatch = canUseSpectrumMatch;
            this.searchParameter = searchParameter ?? throw new ArgumentNullException(nameof(searchParameter));
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }

            return results.Where(result => SatisfySuggestedConditions(result, searchParameter)).ToList();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            if (result is null) {
                throw new ArgumentNullException(nameof(result));
            }

            return SatisfySuggestedConditions(result, searchParameter) && !SatisfyRefMatchedConditions(result, searchParameter);
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            if (result is null) {
                throw new ArgumentNullException(nameof(result));
            }

            return SatisfyRefMatchedConditions(result, searchParameter);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }

            return results.Where(result => SatisfyRefMatchedConditions(result, searchParameter)).ToList();
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }

            return results.DefaultIfEmpty().Argmax(result => result?.TotalScore ?? double.MinValue);
        }

        private bool SatisfyRefMatchedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch
                && (!canUseSpectrumMatch || result.IsSpectrumMatch)
                && (!parameter.IsUseTimeForAnnotationFiltering || result.IsRtMatch)
                && (!parameter.IsUseCcsForAnnotationFiltering || result.IsCcsMatch);
        }

        private bool SatisfySuggestedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch
                && (!parameter.IsUseTimeForAnnotationFiltering || result.IsRtMatch)
                && (!parameter.IsUseCcsForAnnotationFiltering || result.IsCcsMatch);
        }

        public static MsScanMatchResultEvaluator CreateEvaluatorWithSpectrum(MsRefSearchParameterBase searchParameter) {
            if (searchParameter is null) {
                throw new ArgumentNullException(nameof(searchParameter));
            }

            return new MsScanMatchResultEvaluator(true, searchParameter);
        }

        public static MsScanMatchResultEvaluator CreateEvaluator(MsRefSearchParameterBase searchParameter) {
            if (searchParameter is null) {
                throw new ArgumentNullException(nameof(searchParameter));
            }

            return new MsScanMatchResultEvaluator(false, searchParameter);
        }
    }
}
