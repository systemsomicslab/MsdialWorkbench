using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation;

public interface IMatchResultFinder<in TQuery, TResult>
{
    string Id { get; }
    List<TResult> FindCandidates(TQuery query);
    int Priority { get; }
}
