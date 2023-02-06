namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IReferenceScorer<in TQuery, in TReference, out TResult>
    {
        TResult Score(TQuery query, TReference reference);
    }
}
