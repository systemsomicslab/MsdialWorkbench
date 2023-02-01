namespace NCDK.FaulonSignatures.Simple
{
    public class SimpleQuotientGraph : AbstractQuotientGraph
    {
        private SimpleGraph graph;

        public SimpleQuotientGraph(SimpleGraph graph)
            : base()
        {
            this.graph = graph;

            SimpleGraphSignature graphSignature = new SimpleGraphSignature(graph);
            base.Construct(graphSignature.GetSymmetryClasses());
        }

        public SimpleQuotientGraph(SimpleGraph graph, int height)
            : base()
        {
            this.graph = graph;

            SimpleGraphSignature graphSignature = new SimpleGraphSignature(graph);
            base.Construct(graphSignature.GetSymmetryClasses(height));
        }

        public override bool IsConnected(int i, int j)
        {
            return graph.IsConnected(i, j);
        }
    }
}
