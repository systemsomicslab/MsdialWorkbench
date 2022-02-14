using CompMs.Common.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMatchResultEvaluator<TResult>
    {
        TResult SelectTopHit(IEnumerable<TResult> results);
        List<TResult> FilterByThreshold(IEnumerable<TResult> results);
        List<TResult> SelectReferenceMatchResults(IEnumerable<TResult> results);
        bool IsReferenceMatched(TResult result);
        bool IsAnnotationSuggested(TResult result);
    }
}
