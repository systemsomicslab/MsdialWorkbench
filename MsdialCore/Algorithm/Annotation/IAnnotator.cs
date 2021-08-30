using CompMs.Common.Parameter;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IAnnotator<in T, U, V>
        : IMatchResultRefer<U, V>
    {
        V Annotate(T query);
        List<V> FindCandidates(T query);
        V CalculateScore(T query, U reference);
        List<U> Search(T query);

        V SelectTopHit(IEnumerable<V> results, MsRefSearchParameterBase parameter = null);
        List<V> FilterByThreshold(IEnumerable<V> results, MsRefSearchParameterBase parameter = null);
        List<V> SelectReferenceMatchResults(IEnumerable<V> results, MsRefSearchParameterBase parameter = null);
        bool IsReferenceMatched(V result, MsRefSearchParameterBase parameter = null);
        bool IsAnnotationSuggested(V result, MsRefSearchParameterBase parameter = null);
    }

    public interface ISerializableAnnotator<in T, U, V>
        : IAnnotator<T, U, V>, IRestorableRefer<T, U, V>
    {

    }

    public interface ISerializableAnnotator<in T, U, V, in W>
        : IAnnotator<T, U, V>, IRestorableRefer<T, U, V, W>
    {

    }
}
