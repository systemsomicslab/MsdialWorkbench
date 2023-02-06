namespace NCDK.FaulonSignatures.Simple
{
    public class SimpleVertexSignature : AbstractVertexSignature
    {
        private SimpleGraph graph;

        public SimpleVertexSignature(int rootVertexIndex, SimpleGraph graph)
                : this(rootVertexIndex, -1, graph)
        { }

        public SimpleVertexSignature(int rootVertexIndex, int height, SimpleGraph graph)
                : base()
        {
            this.graph = graph;
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
            return "";
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
            return 1;
        }
    }
}
