using NCDK.Common.Collections;
using System.Collections.Generic;
using static NCDK.FaulonSignatures.DAG;

namespace NCDK.FaulonSignatures
{
    public class CanonicalLabellingVisitor : IDAGVisitor
    {
        private readonly int[] labelling;
        private int currentLabel;
        private readonly IComparer<Node> comparator = null;

        public CanonicalLabellingVisitor(
                int vertexCount, IComparer<Node> comparator)
        {
            labelling = new int[vertexCount];
            Arrays.Fill(labelling, -1);
            currentLabel = 0;
        }

        public void Visit(Node node)
        {
            // only label if this vertex has not yet been labeled
            if (this.labelling[node.vertexIndex] == -1)
            {
                this.labelling[node.vertexIndex] = this.currentLabel;
                this.currentLabel++;
            }
            if (comparator != null)
            {
                node.children.Sort(comparator);
            }
            foreach (var child in node.children)
            {
                child.Accept(this);
            }
        }

        public int[] GetLabelling()
        {
            return this.labelling;
        }
    }
}
