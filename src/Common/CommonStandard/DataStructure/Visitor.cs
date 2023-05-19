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

    public interface IDecomposer<out TResult, in TElement> : IAcyclicDecomposer<TResult>
    {
        TResult Decompose<T>(IAcyclicVisitor visitor, T element) where T : TElement;
    }

    public interface IVisitableElement {
        TResult Accept<TResult>(IAcyclicVisitor visitor, IAcyclicDecomposer<TResult> decomposer);
    }

    public sealed class IdentityDecomposer<TResult, TElement> : IDecomposer<TResult, TElement>
    {
        private static IdentityDecomposer<TResult, TElement> _instance;

        public static IdentityDecomposer<TResult, TElement> Instance => _instance ?? (_instance = new IdentityDecomposer<TResult, TElement>());

        TResult IDecomposer<TResult, TElement>.Decompose<T>(IAcyclicVisitor visitor, T element) {
            if (visitor is IVisitor<TResult, T> vis) {
                return vis.Visit(element);
            }
            return default;
        }
    }
}
