namespace NCDK.FaulonSignatures
{
    public interface IVisitableColoredTree
    {
        void Accept(IColoredTreeVisitor visitor);
    }
}
