using System.Collections.Generic;

namespace NCDK.FaulonSignatures.EdgeColored
{
    /// <summary>
    /// A test implementation of signatures for <see cref="EdgeColoredGraph"/>s.
    /// </summary>
    // @author maclean 
    public class EdgeColoredGraphSignature : AbstractGraphSignature
    {
        public EdgeColoredGraph graph;
        private readonly IReadOnlyDictionary<string, int> colorMap;

        public EdgeColoredGraphSignature(EdgeColoredGraph graph, IReadOnlyDictionary<string, int> colorMap)
            : base()
        {
            this.graph = graph;
            this.colorMap = colorMap;
        }

        public override int GetVertexCount()
        {
            return this.graph.GetVertexCount();
        }

        public override string SignatureStringForVertex(int vertexIndex)
        {
            EdgeColoredVertexSignature vertexSignature;
            int height = base.Height;
            if (height == -1)
            {
                vertexSignature =
                    new EdgeColoredVertexSignature(vertexIndex, this.graph, this.colorMap);
            }
            else
            {
                vertexSignature =
                    new EdgeColoredVertexSignature(vertexIndex, height, this.graph, this.colorMap);
            }
            return vertexSignature.ToCanonicalString();
        }

        public override string SignatureStringForVertex(int vertexIndex, int height)
        {
            EdgeColoredVertexSignature vertexSignature =
                new EdgeColoredVertexSignature(vertexIndex, height, this.graph, this.colorMap);
            return vertexSignature.ToCanonicalString();
        }

        public override string ToCanonicalString()
        {
            return base.ToCanonicalString();
        }

        public override AbstractVertexSignature SignatureForVertex(int vertexIndex)
        {
            return new EdgeColoredVertexSignature(vertexIndex, this.graph, this.colorMap);
        }
    }
}
