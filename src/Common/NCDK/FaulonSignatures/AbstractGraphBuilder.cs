using System.Collections.Generic;

namespace NCDK.FaulonSignatures
{
    /// <summary>
    /// Class to reconstruct a graph from a signature string (or a colored tree).
    /// </summary>
    // @author maclean
    public abstract class AbstractGraphBuilder
    {
        private Dictionary<int, int> colorToVertexIndexMap;

        private int vertexCount;

        protected AbstractGraphBuilder()
        {
            this.colorToVertexIndexMap = new Dictionary<int, int>();
            this.vertexCount = 0;
        }

        public void MakeFromColoredTree(ColoredTree tree)
        {
            this.MakeGraph();
            ColoredTree.Node root = tree.GetRoot();
            this.MakeVertex(root.label);
            this.vertexCount = 1;
            foreach (var child in root.children)
            {
                this.MakeFromColoredTreeNode(root, child, 0);
            }

            // Important! resets so that the builder can be used again
            this.vertexCount = 0;
            colorToVertexIndexMap.Clear();
        }

        private void MakeFromColoredTreeNode(ColoredTree.Node parent, ColoredTree.Node node, int parentIndex)
        {
            int vertexIndex;
            if (node.IsColored())
            {
                if (this.colorToVertexIndexMap.ContainsKey(node.color))
                {
                    vertexIndex = this.colorToVertexIndexMap[node.color];
                }
                else
                {
                    this.MakeVertex(node.label);
                    this.vertexCount++;
                    vertexIndex = this.vertexCount - 1;
                    this.colorToVertexIndexMap[node.color] = vertexIndex;
                }
            }
            else
            {
                this.MakeVertex(node.label);
                this.vertexCount++;
                vertexIndex = this.vertexCount - 1;
            }

            this.MakeEdge(parentIndex, vertexIndex, parent.label, node.label, node.edgeLabel);
            foreach (var child in node.children)
            {
                this.MakeFromColoredTreeNode(node, child, vertexIndex);
            }
        }

        /// <summary>
        /// Make the initial, empty, graph to be filled. It is up to the
        /// implementing class to store the graph instance.
        /// </summary>
        public abstract void MakeGraph();

        /// <summary>
        /// Make a vertex in the graph with label <paramref name="label"/>.
        /// </summary>
        /// <param name="label">the string label to use</param>
        public abstract void MakeVertex(string label);

        /// <summary>
        /// Make an edge between the two vertices indexed by 
        /// <paramref name="vertexIndex1"/> and <paramref name="vertexIndex2"/>.
        /// </summary>
        /// <param name="vertexIndex1">the index of the first vertex in the graph</param>
        /// <param name="vertexIndex2">the index of the second vertex in the graph</param>
        /// <param name="vertexSymbol1"></param>
        /// <param name="vertexSymbol2"></param>
        /// <param name="edgeLabel"></param>
        public abstract void MakeEdge(int vertexIndex1, int vertexIndex2, string vertexSymbol1, string vertexSymbol2, string edgeLabel);
    }
}
