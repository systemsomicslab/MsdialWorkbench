using CompMs.Common.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMatchResultEvaluator<TResult>
    {
        TResult SelectTopHit(IEnumerable<TResult> results, MsRefSearchParameterBase parameter);
        List<TResult> FilterByThreshold(IEnumerable<TResult> results, MsRefSearchParameterBase parameter);
        List<TResult> SelectReferenceMatchResults(IEnumerable<TResult> results, MsRefSearchParameterBase parameter);
        bool IsReferenceMatched(TResult result, MsRefSearchParameterBase parameter);
        bool IsAnnotationSuggested(TResult result, MsRefSearchParameterBase parameter);
    }
}
