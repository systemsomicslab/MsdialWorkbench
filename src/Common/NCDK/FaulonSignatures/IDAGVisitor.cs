namespace NCDK.FaulonSignatures
{
    public interface IDAGVisitor
    {
         void Visit(DAG.Node node);
    }
}
