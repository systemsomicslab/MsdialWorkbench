namespace NCDK.FaulonSignatures.Simple
{
    public class SimpleGraphBuilder : AbstractGraphBuilder
    {
        private SimpleGraph graph;

        public SimpleGraphBuilder()
                : base()
        { }

        public override void MakeEdge(
                int vertexIndex1, int vertexIndex2, string a, string b, string edgeLabel)
        {
            this.graph.MakeEdge(vertexIndex1, vertexIndex2);
        }

        public override void MakeGraph()
        {
            this.graph = new SimpleGraph();
        }

        public override void MakeVertex(string label)
        {
            // oddly, do nothing
        }

        public SimpleGraph FromTree(ColoredTree tree)
        {
            base.MakeFromColoredTree(tree);
            return this.graph;
        }
    }
}
