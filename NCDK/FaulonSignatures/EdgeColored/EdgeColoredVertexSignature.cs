using System.Collections.Generic;

namespace NCDK.FaulonSignatures.EdgeColored
{
    public class EdgeColoredVertexSignature : AbstractVertexSignature
    {
        private readonly EdgeColoredGraph graph;
        private IReadOnlyDictionary<string, int> colorMap;

        public EdgeColoredVertexSignature(int rootVertexIndex, EdgeColoredGraph graph, IReadOnlyDictionary<string, int> colorMap)
            : this(rootVertexIndex, -1, graph, colorMap)
        { }

        public EdgeColoredVertexSignature(int rootVertexIndex, int height, EdgeColoredGraph graph, IReadOnlyDictionary<string, int> colorMap)
            : base()
        {
            this.graph = graph;
            this.colorMap = colorMap;
            if (height == -1)
            {
                base.CreateMaximumHeight(rootVertexIndex, graph.GetVertexCount());
            }
            else
            {
                base.Create(rootVertexIndex, graph.GetVertexCount(), height);
            }
        }

        public override int[] GetConnected(int vertexIndex)
        {
            return this.graph.GetConnected(vertexIndex);
        }

        public override string GetEdgeLabel(int vertexIndex, int otherVertexIndex)
        {
            var edge = this.graph.GetEdge(vertexIndex, otherVertexIndex);
            if (edge != null)
            {
                return edge.edgeLabel;
            }
            else
            {
                // ??
                return "";
            }
        }

        public override string GetVertexSymbol(int vertexIndex)
        {
            return ".";
        }

        public override int GetIntLabel(int vertexIndex)
        {
            return -1;
        }

        public override int ConvertEdgeLabelToColor(string label)
        {
            if (colorMap.ContainsKey(label))
            {
                return colorMap[label];
            }
            return 1;   // or throw error?
        }
    }
}
