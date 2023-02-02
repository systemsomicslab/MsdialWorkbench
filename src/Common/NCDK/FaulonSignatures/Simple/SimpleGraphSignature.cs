namespace NCDK.FaulonSignatures.Simple
{
    /// <summary>
    /// A test implementation of signatures for <see cref="SimpleGraph"/>s.
    /// </summary>
    // @author maclean
    public class SimpleGraphSignature : AbstractGraphSignature
    {
        public SimpleGraph graph;

        public SimpleGraphSignature(SimpleGraph graph)
            : base()
        {
            this.graph = graph;
        }

        public override int GetVertexCount()
        {
            return this.graph.GetVertexCount();
        }

        public override string SignatureStringForVertex(int vertexIndex)
        {
            SimpleVertexSignature vertexSignature;
            int height = base.Height;
            if (height == -1)
            {
                vertexSignature =
                    new SimpleVertexSignature(vertexIndex, this.graph);
            }
            else
            {
                vertexSignature =
                    new SimpleVertexSignature(vertexIndex, height, this.graph);
            }
            return vertexSignature.ToCanonicalString();
        }

        public override string SignatureStringForVertex(int vertexIndex, int height)
        {
            SimpleVertexSignature vertexSignature =
                new SimpleVertexSignature(vertexIndex, height, this.graph);
            return vertexSignature.ToCanonicalString();
        }

        public override string ToCanonicalString()
        {
            return base.ToCanonicalString();
        }

        public override AbstractVertexSignature SignatureForVertex(int vertexIndex)
        {
            return new SimpleVertexSignature(vertexIndex, this.graph);
        }
    }
}
