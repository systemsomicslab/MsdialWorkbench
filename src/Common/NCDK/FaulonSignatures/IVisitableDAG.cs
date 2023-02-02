namespace NCDK.FaulonSignatures
{
    public interface IVisitableDAG
    {
        void Accept(IDAGVisitor visitor);
    }
}
