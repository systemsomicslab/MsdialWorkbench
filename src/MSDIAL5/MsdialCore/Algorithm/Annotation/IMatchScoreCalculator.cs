namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IMatchScoreCalculator<in TQuery, in TReference, out TResult>
    {
        TResult Calculate(TQuery query, TReference reference);
    }
}
