namespace CompMs.Common.DataStructure
{
    public interface IAcyclicVisitor {

    }

    public interface IAcyclicDecomposer<out TResult> {

    }

    public interface IVisitor<out TResult, in TElement> : IAcyclicVisitor
    {
        TResult Visit(TElement item);
    }

    public interface IDecomposer<TResult, in TElement, out TDecomposed>
    {
        TResult Decompose(IAcyclicVisitor visitor, TElement element);
    }

    public interface IDecomposer<out TResult, in TElement> : IAcyclicDecomposer<TResult>
    {
        TResult Decompose(IAcyclicVisitor visitor, TElement element);
    }

    public interface IVisitableElement {
        TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer);
    }

    public sealed class IdentityDecomposer<TResult, TElement> : IDecomposer<TResult, TElement>
    {
        private static IdentityDecomposer<TResult, TElement> _instance;

        public static IdentityDecomposer<TResult, TElement> Instance => _instance ?? (_instance = new IdentityDecomposer<TResult, TElement>());

        TResult IDecomposer<TResult, TElement>.Decompose(IAcyclicVisitor visitor, TElement element) {
            if (visitor is IVisitor<TResult, TElement> vis) {
                return vis.Visit(element);
            }
            return default;
        }
    }
}
