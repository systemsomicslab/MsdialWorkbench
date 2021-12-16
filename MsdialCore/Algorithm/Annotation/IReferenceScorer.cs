namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public interface IReferenceScorer<in Query, in Reference, out Result>
    {
        Result Score(Query query, Reference reference);
    }
}
