using System.Threading;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    /// <summary>Cancellation belongs to an individual query, never to a shared annotator.</summary>
    public interface ICancellableAnnotationQuery
    {
        CancellationToken CancellationToken { get; set; }
    }

    public static class AnnotationQueryCancellation
    {
        public static CancellationToken GetCancellationToken<TResult>(this IAnnotationQuery<TResult> query)
            => query is ICancellableAnnotationQuery cancellable ? cancellable.CancellationToken : default;
    }
}
