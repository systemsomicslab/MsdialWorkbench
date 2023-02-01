using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCDK.FaulonSignatures
{
    /// <summary>
    /// A directed acyclic graph that is the core data structure of a signature. It
    /// is the DAG that is canonized by sorting its layers of nodes.
    /// </summary>
    // @author maclean
    public class DAG : IEnumerable<List<DAG.Node>>
    {
        /// <summary>
        /// The direction up and down the DAG. UP is from leaves to root.
        /// </summary>
        public enum Direction { Up, Down };

        /// <summary>
        /// A node of the directed acyclic graph
        /// </summary>
        public class Node : IVisitableDAG
        {
            /// <summary>
            /// The index of the vertex in the graph. Note that for signatures that
            /// cover only part of the graph (with a height less than the diameter)
            /// this index may have to be mapped to the original index 
            /// </summary>
            public readonly int vertexIndex;

            /// <summary>
            /// The parent nodes in the DAG
            /// </summary>
            public readonly List<Node> parents;

            /// <summary>
            /// The child nodes in the DAG
            /// </summary>
            public readonly List<Node> children;

            /// <summary>
            /// What layer this node is in
            /// </summary>
            public readonly int layer;

            /// <summary>
            /// Labels for the edges between this node and the parent nodes
            /// </summary>
            public readonly Dictionary<int, int> edgeColors;

            /// <summary>
            /// The final computed invariant, used for sorting children when printing
            /// </summary>
            public int invariant;

            /// <summary>
            /// Make a Node that refers to a vertex, in a layer, and with a label.
            /// </summary>
            /// <param name="vertexIndex">the graph vertex index</param>
            /// <param name="layer">the layer of this Node</param>
            public Node(int vertexIndex, int layer)
            {
                this.vertexIndex = vertexIndex;
                this.layer = layer;
                this.parents = new List<Node>();
                this.children = new List<Node>();
                this.edgeColors = new Dictionary<int, int>();
            }

            public void AddParent(Node node)
            {
                this.parents.Add(node);
            }

            public void AddChild(Node node)
            {
                this.children.Add(node);
            }

            public void AddEdgeColor(int partnerIndex, int edgeColor)
            {
                this.edgeColors[partnerIndex] = edgeColor;
            }

            public void Accept(IDAGVisitor visitor)
            {
                visitor.Visit(this);
            }

            public override string ToString()
            {
                var parentString = new StringBuilder();
                parentString.Append('[');
                foreach (var parent in this.parents)
                {
                    parentString.Append(parent.vertexIndex).Append(',');
                }
                if (parentString.Length > 1)
                {
                    parentString[parentString.Length - 1] = ']';
                }
                else
                {
                    parentString.Append(']');
                }
                var childString = new StringBuilder();
                childString.Append('[');
                foreach (var child in this.children)
                {
                    childString.Append(child.vertexIndex).Append(',');
                }
                if (childString.Length > 1)
                {
                    childString[childString.Length - 1] = ']';
                }
                else
                {
                    childString.Append(']');
                }

                return vertexIndex + " "
                      + " (" + parentString + ", " + childString + ")";
            }
        }

        /// <summary>
        /// An arc of the directed acyclic graph.
        /// </summary>
        public class Arc
        {
            public readonly int a;

            public readonly int b;

            public Arc(int a, int b)
            {
                this.a = a;
                this.b = b;
            }

            public override bool Equals(object other)
            {
                if (other is Arc o)
                {
                    return (this.a == o.a && this.b == o.b)
                        || (this.a == o.b && this.b == o.a);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return a.GetHashCode() * 31 + b.GetHashCode();
            }
        }

        /// <summary>
        /// Comparator for nodes based on string labels.
        /// </summary>
        public class NodeStringLabelComparator : IComparer<Node>
        {
            /// <summary>
            /// The labels for vertices.
            /// </summary>
            public string[] vertexLabels;

            public NodeStringLabelComparator(string[] vertexLabels)
            {
                this.vertexLabels = vertexLabels;
            }

            public int Compare(Node o1, Node o2)
            {
                string o1s = this.vertexLabels[o1.vertexIndex];
                string o2s = this.vertexLabels[o2.vertexIndex];
                int c = string.Compare(o1s, o2s, StringComparison.Ordinal);
                if (c == 0)
                {
                    if (o1.invariant < o2.invariant)
                    {
                        return -1;
                    }
                    else if (o1.invariant > o2.invariant)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return c;
                }
            }
        }

        /// <summary>
        /// Comparator for nodes based on int labels.
        /// </summary>
        public class NodeIntegerLabelComparator : IComparer<Node>
        {
            /// <summary>
            /// The labels for vertices.
            /// </summary>
            public int[] vertexLabels;

            public NodeIntegerLabelComparator(int[] vertexLabels)
            {
                this.vertexLabels = vertexLabels;
            }

            public int Compare(Node o1, Node o2)
            {
                int o1n = this.vertexLabels[o1.vertexIndex];
                int o2n = this.vertexLabels[o2.vertexIndex];
                int c = (o1n == o2n) ? 0 : (o1n < o2n ? -1 : 1);
                if (c == 0)
                {
                    if (o1.invariant < o2.invariant)
                    {
                        return -1;
                    }
                    else if (o1.invariant > o2.invariant)
                    {
                        return 1;
                    }
                    else
                    {
                        return 0;
                    }
                }
                else
                {
                    return c;
                }
            }
        }

        /// <summary>
        /// Used to sort nodes, it is public so that the AbstractVertexSignature
        /// can use it 
        /// </summary>
        public IComparer<Node> nodeComparator;

        /// <summary>
        /// The layers of the DAG
        /// </summary>
        private List<List<Node>> layers;

        /// <summary>
        /// The counts of parents for vertices  
        /// </summary>
        private readonly int[] parentCounts;

        /// <summary>
        /// The counts of children for vertices  
        /// </summary>
        private readonly int[] childCounts;

        private Invariants invariants;

        /// <summary>
        /// Convenience reference to the nodes of the DAG
        /// </summary>
        private List<DAG.Node> nodes;

        /// <summary>
        /// A convenience record of the number of vertices
        /// </summary>
        private int vertexCount;

        /// <summary>
        /// Create a DAG from a graph, starting at the root vertex.
        /// </summary>
        /// <param name="rootVertexIndex">the vertex to start from</param>
        /// <param name="graphVertexCount">the number of vertices in the original graph</param>
        public DAG(int rootVertexIndex, int graphVertexCount)
        {
            this.layers = new List<List<Node>>();
            this.nodes = new List<Node>();
            var rootLayer = new List<Node>();
            Node rootNode = new Node(rootVertexIndex, 0);
            rootLayer.Add(rootNode);
            this.layers.Add(rootLayer);
            this.nodes.Add(rootNode);

            this.vertexCount = 1;
            this.parentCounts = new int[graphVertexCount];
            this.childCounts = new int[graphVertexCount];
        }

        public IEnumerator<List<Node>> GetEnumerator()
        {
            return layers.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public List<DAG.Node> GetRootLayer()
        {
            return this.layers[0];
        }

        public DAG.Node GetRoot()
        {
            return this.layers[0][0];
        }

        public Invariants CopyInvariants()
        {
            return (Invariants)this.invariants.Clone();
        }

        /// <summary>
        /// Initialize the invariants, assuming that the vertex count for the
        /// signature is the same as the length of the label array.
        /// </summary>
        public void InitializeWithStringLabels(string[] vertexLabels)
        {
            vertexCount = vertexLabels.Length;
            this.invariants = new Invariants(vertexCount, nodes.Count);

            var pairs = new List<InvariantIntStringPair>();
            for (int i = 0; i < vertexCount; i++)
            {
                string l = vertexLabels[i];
                int p = parentCounts[i];
                pairs.Add(new InvariantIntStringPair(l, p, i));
            }
            pairs.Sort();

            if (pairs.Count == 0) return;

            nodeComparator = new NodeStringLabelComparator(vertexLabels);
            int order = 1;
            InvariantIntStringPair first = pairs[0];
            invariants.SetVertexInvariant(first.GetOriginalIndex(), order);
            for (int i = 1; i < pairs.Count; i++)
            {
                InvariantIntStringPair a = pairs[i - 1];
                InvariantIntStringPair b = pairs[i];
                if (!a.Equals(b))
                {
                    order++;
                }
                invariants.SetVertexInvariant(b.GetOriginalIndex(), order);
            }
        }

        public void InitializeWithIntLabels(int[] vertexLabels)
        {
            vertexCount = vertexLabels.Length;
            this.invariants = new Invariants(vertexCount, nodes.Count);

            List<InvariantIntIntPair> pairs = new List<InvariantIntIntPair>();
            for (int i = 0; i < vertexCount; i++)
            {
                int l = vertexLabels[i];
                int p = parentCounts[i];
                pairs.Add(new InvariantIntIntPair(l, p, i));
            }
            pairs.Sort();

            if (pairs.Count == 0) return;

            nodeComparator = new NodeIntegerLabelComparator(vertexLabels);
            int order = 1;
            InvariantIntIntPair first = pairs[0];
            invariants.SetVertexInvariant(first.GetOriginalIndex(), order);
            for (int i = 1; i < pairs.Count; i++)
            {
                InvariantIntIntPair a = pairs[i - 1];
                InvariantIntIntPair b = pairs[i];
                if (!a.Equals(b))
                {
                    order++;
                }
                invariants.SetVertexInvariant(b.GetOriginalIndex(), order);
            }
        }

        public void SetColor(int vertexIndex, int color)
        {
            this.invariants.SetColor(vertexIndex, color);
        }

        public int Occurences(int vertexIndex)
        {
            int count = 0;
            foreach (var node in nodes)
            {
                if (node.vertexIndex == vertexIndex)
                {
                    count++;
                }
            }
            return count;
        }

        public void SetInvariants(Invariants invariants)
        {
            //        this.invariants = invariants;
            this.invariants.colors = (int[])invariants.colors.Clone();
            this.invariants.nodeInvariants = (int[])invariants.nodeInvariants.Clone();
            this.invariants.vertexInvariants = (int[])invariants.vertexInvariants.Clone();
        }

        /// <summary>
        /// Create and return a DAG.Node, while setting some internal references to
        /// the same data. Does not add the node to a layer.
        /// </summary>
        /// <param name="vertexIndex">the index of the vertex in the original graph</param>
        /// <param name="layer">the index of the layer</param>
        /// <returns>the new node </returns>
        public DAG.Node MakeNode(int vertexIndex, int layer)
        {
            DAG.Node node = new DAG.Node(vertexIndex, layer);
            this.nodes.Add(node);
            return node;
        }

        /// <summary>
        /// Create and return a DAG.Node, while setting some internal references to
        /// the same data. Note: also adds the node to a layer, creating it if 
        /// necessary.
        /// </summary>
        /// <param name="vertexIndex">the index of the vertex in the original graph</param>
        /// <param name="layer">the index of the layer</param>
        /// <returns>the new node</returns>
        public DAG.Node MakeNodeInLayer(int vertexIndex, int layer)
        {
            DAG.Node node = this.MakeNode(vertexIndex, layer);
            if (layers.Count <= layer)
            {
                this.layers.Add(new List<DAG.Node>());
            }
            this.layers[layer].Add(node);
            return node;
        }

        public void AddRelation(DAG.Node childNode, DAG.Node parentNode)
        {
            childNode.parents.Add(parentNode);
            parentCounts[childNode.vertexIndex]++;
            childCounts[parentNode.vertexIndex]++;
            parentNode.children.Add(childNode);
        }

        public int[] GetParentsInFinalString()
        {
            int[] counts = new int[vertexCount];
            GetParentsInFinalString(
                    counts, GetRoot(), null, new List<DAG.Arc>());
            return counts;
        }

        private void GetParentsInFinalString(int[] counts, DAG.Node node,
                DAG.Node parent, List<DAG.Arc> arcs)
        {
            if (parent != null)
            {
                counts[node.vertexIndex]++;
            }
            node.children.Sort(nodeComparator);
            foreach (var child in node.children)
            {
                DAG.Arc arc = new Arc(node.vertexIndex, child.vertexIndex);
                if (arcs.Contains(arc))
                {
                    continue;
                }
                else
                {
                    arcs.Add(arc);
                    GetParentsInFinalString(counts, child, node, arcs);
                }
            }

        }

        /// <summary>
        /// Count the occurrences of each vertex index in the final signature string.
        /// Since duplicate DAG edges are removed, this count will not be the same as
        /// the simple count of occurrences in the DAG before printing.
        /// </summary>
        /// <returns></returns>
        public int[] GetOccurrences()
        {
            int[] occurences = new int[vertexCount];
            GetOccurences(occurences, GetRoot(), null, new List<DAG.Arc>());
            return occurences;
        }

        private void GetOccurences(int[] occurences, DAG.Node node, DAG.Node parent, List<DAG.Arc> arcs)
        {
            occurences[node.vertexIndex]++;
            node.children.Sort(nodeComparator);
            foreach (var child in node.children)
            {
                DAG.Arc arc = new Arc(node.vertexIndex, child.vertexIndex);
                if (arcs.Contains(arc))
                {
                    continue;
                }
                else
                {
                    arcs.Add(arc);
                    GetOccurences(occurences, child, node, arcs);
                }
            }
        }

        public List<InvariantInt> GetInvariantPairs(int[] parents)
        {
            List<InvariantInt> pairs = new List<InvariantInt>();
            for (int i = 0; i < this.vertexCount; i++)
            {
                if (invariants.GetColor(i) == -1
                        && parents[i] >= 2)
                {
                    pairs.Add(
                            new InvariantInt(
                                    invariants.GetVertexInvariant(i), i));
                }
            }
            pairs.Sort();
            return pairs;
        }

        public int ColorFor(int vertexIndex)
        {
            return this.invariants.GetColor(vertexIndex);
        }

        public void Accept(IDAGVisitor visitor)
        {
            this.GetRoot().Accept(visitor);
        }

        public void AddLayer(List<Node> layer)
        {
            this.layers.Add(layer);
        }

        public List<int> CreateOrbit(int[] parents)
        {
            // get the orbits
            var orbits = new Dictionary<int, List<int>>();
            for (int j = 0; j < vertexCount; j++)
            {
                if (parents[j] >= 2)
                {
                    int invariant = invariants.GetVertexInvariant(j);
                    List<int> orbit;
                    if (orbits.ContainsKey(invariant))
                    {
                        orbit = orbits[invariant];
                    }
                    else
                    {
                        orbit = new List<int>();
                        orbits[invariant] = orbit;
                    }
                    orbit.Add(j);
                }
            }

            //        Console.Out.WriteLine("Orbits " + orbits);

            // find the largest orbit
            if (!orbits.Any())
            {
                return new List<int>();
            }
            else
            {
                List<int> maxOrbit = null;
                List<int> invariants = new List<int>(orbits.Keys);
                invariants.Sort();

                foreach (var invariant in invariants)
                {
                    List<int> orbit = orbits[invariant];
                    if (maxOrbit == null || orbit.Count > maxOrbit.Count)
                    {
                        maxOrbit = orbit;
                    }
                }
                return maxOrbit;
            }
        }

        public void ComputeVertexInvariants()
        {
            var layerInvariants = new Dictionary<int, int[]>();
            for (int i = 0; i < this.nodes.Count; i++)
            {
                DAG.Node node = this.nodes[i];
                int j = node.vertexIndex;
                int[] layerInvariantsJ;
                if (layerInvariants.ContainsKey(j))
                {
                    layerInvariantsJ = layerInvariants[j];
                }
                else
                {
                    layerInvariantsJ = new int[this.layers.Count];
                    layerInvariants[j] = layerInvariantsJ;
                }
                layerInvariantsJ[node.layer] = invariants.GetNodeInvariant(i);
            }

            var invariantLists = new List<InvariantArray>();
            foreach (var i in layerInvariants.Keys)
            {
                InvariantArray invArr = new InvariantArray(layerInvariants[i], i);
                invariantLists.Add(invArr);
            }
            invariantLists.Sort();

            int order = 1;
            int first = invariantLists[0].originalIndex;
            invariants.SetVertexInvariant(first, 1);
            for (int i = 1; i < invariantLists.Count; i++)
            {
                InvariantArray a = invariantLists[i - 1];
                InvariantArray b = invariantLists[i];
                if (!a.Equals(b))
                {
                    order++;
                }
                invariants.SetVertexInvariant(b.originalIndex, order);
            }
        }

        public void UpdateVertexInvariants()
        {
            int[] oldInvariants = new int[vertexCount];
            bool invariantSame = true;
            while (invariantSame)
            {
                oldInvariants = invariants.GetVertexInvariantCopy();

                UpdateNodeInvariants(Direction.Up); // From the leaves to the root

                // This is needed here otherwise there will be cases where a node 
                // invariant is reset when the tree is traversed down. 
                // This is not mentioned in Faulon's paper.
                ComputeVertexInvariants();

                UpdateNodeInvariants(Direction.Down); // From the root to the leaves
                ComputeVertexInvariants();

                invariantSame = CheckInvariantChange(oldInvariants, invariants.GetVertexInvariants());
            }

            // finally, copy the node invariants into the nodes, for easy sorting
            for (int i = 0; i < this.nodes.Count; i++)
            {
                this.nodes[i].invariant = invariants.GetNodeInvariant(i);
            }
        }

        public bool CheckInvariantChange(int[] a, int[] b)
        {
            for (int i = 0; i < vertexCount; i++)
            {
                if (a[i] != b[i])
                {
                    return true;
                }
            }
            return false;
        }

        public void UpdateNodeInvariants(DAG.Direction direction)
        {
            int start, end, increment;
            if (direction == Direction.Up)
            {
                start = this.layers.Count - 1;
                // The root node is not included but it doesn't matter since it
                // is always alone.
                end = -1;
                increment = -1;
            }
            else
            {
                start = 0;
                end = this.layers.Count;
                increment = 1;
            }

            for (int i = start; i != end; i += increment)
            {
                this.UpdateLayer(this.layers[i], direction);
            }
        }

        public void UpdateLayer(List<DAG.Node> layer, DAG.Direction direction)
        {
            List<InvariantList> nodeInvariantList = new List<InvariantList>();
            for (int i = 0; i < layer.Count; i++)
            {
                DAG.Node layerNode = layer[i];
                int x = layerNode.vertexIndex;
                InvariantList nodeInvariant =
                    new InvariantList(nodes.IndexOf(layerNode));
                nodeInvariant.Add(this.invariants.GetColor(x));
                nodeInvariant.Add(this.invariants.GetVertexInvariant(x));

                List<int> relativeInvariants = new List<int>();

                // If we go up we should check the children.
                List<DAG.Node> relatives = (direction == Direction.Up) ?
                        layerNode.children : layerNode.parents;
                foreach (var relative in relatives)
                {
                    int j = this.nodes.IndexOf(relative);
                    int inv = this.invariants.GetNodeInvariant(j);
                    int edgeColor;
                    if (direction == Direction.Up)
                    {
                        edgeColor = relative.edgeColors[layerNode.vertexIndex];
                    }
                    else
                    {
                        edgeColor = layerNode.edgeColors[relative.vertexIndex];
                    }

                    relativeInvariants.Add(inv);
                    relativeInvariants.Add(vertexCount + 1 + edgeColor);
                }
                relativeInvariants.Sort();
                nodeInvariant.AddAll(relativeInvariants);
                nodeInvariantList.Add(nodeInvariant);
            }

            nodeInvariantList.Sort();

            int order = 1;
            int first = nodeInvariantList[0].originalIndex;
            this.invariants.SetNodeInvariant(first, order);
            for (int i = 1; i < nodeInvariantList.Count; i++)
            {
                InvariantList a = nodeInvariantList[i - 1];
                InvariantList b = nodeInvariantList[i];
                if (!a.Equals(b))
                {
                    order++;
                }
                this.invariants.SetNodeInvariant(b.originalIndex, order);
            }
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            foreach (var layer in this)
            {
                buffer.Append(layer);
                buffer.Append("\n");
            }
            return buffer.ToString();
        }
    }
}
