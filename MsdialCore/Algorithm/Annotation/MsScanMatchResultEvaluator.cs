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

        private MsScanMatchResultEvaluator(bool canUseSpectrumMatch) {
            this.canUseSpectrumMatch = canUseSpectrumMatch;
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            return results.Where(result => SatisfySuggestedConditions(result, parameter)).ToList();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            if (result is null) {
                throw new ArgumentNullException(nameof(result));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            return SatisfySuggestedConditions(result, parameter) && !SatisfyRefMatchedConditions(result, parameter);
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            if (result is null) {
                throw new ArgumentNullException(nameof(result));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            return SatisfyRefMatchedConditions(result, parameter);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            return results.Where(result => SatisfyRefMatchedConditions(result, parameter)).ToList();
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter) {
            if (results is null) {
                throw new ArgumentNullException(nameof(results));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
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

        public static MsScanMatchResultEvaluator CreateEvaluatorWithSpectrum() {
            return new MsScanMatchResultEvaluator(true);
        }

        public static MsScanMatchResultEvaluator CreateEvaluator() {
            return new MsScanMatchResultEvaluator(false);
        }
    }
}
