namespace CompMs.Common.DataStructure
{
    public interface IAcyclicVisitor {

    }

    public interface IVisitor<out TResult, in TElement> : IAcyclicVisitor
    {
        TResult Visit(TElement item);
    }

    public interface IDecomposer<TResult, in TElement, out TDecomposed>
    {
        TResult Decompose(IAcyclicVisitor visitor, TElement element);
    }

    public interface IVisitableElement<out TElement>
    {
        TResult Accept<TResult, TDecomposed>(IAcyclicVisitor visitor, IDecomposer<TResult, TElement, TDecomposed> decomposer);
    }

    public sealed class IdentityDecomposer<TResult, TElement> : IDecomposer<TResult, TElement, TElement>
    {
        TResult IDecomposer<TResult, TElement, TElement>.Decompose(IAcyclicVisitor visitor, TElement element) {
            if (visitor is IVisitor<TResult, TElement> vis) {
                return vis.Visit(element);
            }
            return default;
        }
    }
}
