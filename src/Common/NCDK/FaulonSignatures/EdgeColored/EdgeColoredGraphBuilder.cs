using NCDK.FaulonSignatures;

namespace NCDK.FaulonSignatures.EdgeColored
{
    public class EdgeColoredGraphBuilder : AbstractGraphBuilder
    {
        private EdgeColoredGraph graph;

        public EdgeColoredGraphBuilder()
            : base()
        { }

        public override void MakeEdge(
                int vertexIndex1, int vertexIndex2, string a, string b, string edgeLabel)
        {
            this.graph.MakeEdge(vertexIndex1, vertexIndex2, edgeLabel);
        }

        public override void MakeGraph()
        {
            this.graph = new EdgeColoredGraph();
        }

        public override void MakeVertex(string label)
        {
            // oddly, do nothing
        }

        public EdgeColoredGraph FromTree(ColoredTree tree)
        {
            base.MakeFromColoredTree(tree);
            return this.graph;
        }
    }
}
