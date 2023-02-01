using NCDK.FaulonSignatures;
using System.Collections.Generic;

namespace NCDK.FaulonSignatures.EdgeColored
{
    public class EdgeColoredQuotientGraph : AbstractQuotientGraph
    {
        private EdgeColoredGraph graph;

        public EdgeColoredQuotientGraph(EdgeColoredGraph graph, IReadOnlyDictionary<string, int> colorMap)
            : base()
        {
            this.graph = graph;

            EdgeColoredGraphSignature graphSignature = new EdgeColoredGraphSignature(graph, colorMap);
            base.Construct(graphSignature.GetSymmetryClasses());
        }

        public EdgeColoredQuotientGraph(EdgeColoredGraph graph, IReadOnlyDictionary<string, int> colorMap, int height)
                : base()
        {
            this.graph = graph;

            EdgeColoredGraphSignature graphSignature = new EdgeColoredGraphSignature(graph, colorMap);
            base.Construct(graphSignature.GetSymmetryClasses(height));
        }

        public override bool IsConnected(int i, int j)
        {
            return graph.IsConnected(i, j);
        }
    }
}
