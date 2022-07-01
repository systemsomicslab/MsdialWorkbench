namespace CompMs.Common.DataStructure
{
    public interface IVisitor<out TResult, in TElement>
    {
        TResult Visit(TElement item);
    }

    public interface IDecomposer<TResult, in TElement, out TDecomposed>
    {
        TResult Decompose(IVisitor<TResult, TDecomposed> visitor, TElement element);
    }

    public interface IVisitableElement<out TElement>
    {
        TResult Accept<TResult, TDecomposed>(IVisitor<TResult, TDecomposed> visitor, IDecomposer<TResult, TElement, TDecomposed> decomposer);
    }

    public sealed class IdentityDecomposer<TResult, TElement> : IDecomposer<TResult, TElement, TElement>
    {
        TResult IDecomposer<TResult, TElement, TElement>.Decompose(IVisitor<TResult, TElement> visitor, TElement element) {
            return visitor.Visit(element);
        }
    }
}
