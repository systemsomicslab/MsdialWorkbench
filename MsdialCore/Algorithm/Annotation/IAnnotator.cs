using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IAnnotator<in TQuery, out TReference, TResult> : IMatchResultRefer<TReference, TResult>, IMatchResultEvaluator<TResult>
    {
        List<TResult> FindCandidates(TQuery query);
    }

    public interface ISerializableAnnotator<in TQuery, TReference, TResult>
        : IAnnotator<TQuery, TReference, TResult>, IRestorableRefer<TQuery, TReference, TResult>
    {

    }

    public interface ISerializableAnnotator<in TQuery, TReference, TResult, in TDataBase>
        : IAnnotator<TQuery, TReference, TResult>, IRestorableRefer<TQuery, TReference, TResult, TDataBase>
    {

    }
}
