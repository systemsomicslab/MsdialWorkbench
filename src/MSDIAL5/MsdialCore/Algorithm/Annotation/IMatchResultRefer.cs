using CompMs.MsdialCore.Parser;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMatchResultRefer<out TReference, in TResult> {
        string Key { get; }

        TReference Refer(TResult result);
    }

    public interface IRestorableRefer<in TQuery, TReference, TResult, in TDatabase> : IMatchResultRefer<TReference, TResult>
    {
        IReferRestorationKey<TQuery, TReference, TResult, TDatabase> Save();
    }
}