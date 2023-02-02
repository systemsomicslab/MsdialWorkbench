using NCDK.Common.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NCDK.FaulonSignatures
{
    /// <summary>
    /// The base class for signatures that are created from a vertex of a graph. A
    /// concrete derived class will implement the methods (getConnected, 
    /// GetVertexCount() etc.) that communicate between the graph and the signature.
    /// </summary>
    // @author maclean
    public abstract class AbstractVertexSignature
    {
        private const char StartBranchSymbolChar = '(';
        private const char EndBranchSymbolChar = ')';
        private const char StartNodeSymbolChar = '[';
        private const char EndNodeSymbolChar = ']';

        private DAG dag;

        /// <summary>
        /// The number of vertices from the graph that were visited to make the
        /// signature. This is either the number of vertices in the graph - if the
        /// height is equal to the graph diameter - or the number of vertices seen up
        /// to that height
        /// </summary>
        private int vertexCount;

        /// <summary>
        /// Mapping between the vertex indices in the original graph and the vertex   
        /// indices stored in the Nodes. This is necessary for signatures with a
        /// height less than the graph diameter. It is also the order in which the
        /// vertices were visited to make the DAG.
        /// </summary>
        private Dictionary<int, int> vertexMapping;

        public enum InvariantType { String, Integer };

        private readonly InvariantType invariantType;

        /// <summary>
        /// Create an abstract vertex signature.
        /// </summary>
        protected AbstractVertexSignature()
           : this(InvariantType.String)
        { }

        /// <summary>
        /// Create an abstract vertex signature that uses the given invariant type
        /// for the initial invariants. 
        /// </summary>
        /// <param name="invariantType"></param>
        protected AbstractVertexSignature(InvariantType invariantType)
        {
            this.vertexCount = 0;
            this.invariantType = invariantType;
        }

        /// <summary>
        /// The height of the signature.
        /// </summary>
        /// <remarks>
        /// If the signature is considered as a tree, the height is the maximum 
        /// distance from the root to the leaves. A height of -1 is taken to mean
        /// the same as the maximum possible height, which is the graph diameter
        /// </remarks>
        public int Height { get; private set; }

        /// <summary>
        /// Look up the original graph vertex that <paramref name="vertexIndex"/> maps to.  
        /// </summary>
        /// <param name="vertexIndex">the internal vertex index that </param>
        /// <returns>the vertex index in the original graph</returns>
        public int GetOriginalVertexIndex(int vertexIndex)
        {
            foreach (var originalVertexIndex in vertexMapping.Keys)
            {
                int internalVertexIndex = vertexMapping[originalVertexIndex];
                if (internalVertexIndex == vertexIndex)
                {
                    return originalVertexIndex;
                }
            }
            return -1;
        }

        /// <summary>
        /// This is a kind of constructor that builds the internal representation of
        /// the signature given the index of the vertex to use as a root.
        /// </summary>
        /// <param name="rootVertexIndex">the index in the graph of the root for this signature</param>
        /// <param name="graphVertexCount">the number of vertices in the graph</param>
        public void CreateMaximumHeight(int rootVertexIndex, int graphVertexCount)
        {
            Create(rootVertexIndex, graphVertexCount, -1);
        }

        /// <summary>
        /// This is a kind of constructor that builds the internal representation of
        /// the signature given the index of the vertex to use as a root. It also
        /// takes a maximum height, which limits how many vertices will be visited.
        /// </summary>
        /// <param name="rootVertexIndex">the index in the graph of the root for this signature</param>
        /// <param name="graphVertexCount">the number of vertices in the graph</param>
        /// <param name="height">the maximum height of the signature</param>
        public void Create(int rootVertexIndex, int graphVertexCount, int height)
        {
            this.Height = height;
            vertexMapping = new Dictionary<int, int>
            {
                [rootVertexIndex] = 0
            };
            dag = new DAG(0, graphVertexCount);
            vertexCount = 1;
            Builder(1, dag.GetRootLayer(), new List<DAG.Arc>(), height);
            if (invariantType == InvariantType.String)
            {
                CreateWithStringLabels();
            }
            else if (invariantType == InvariantType.Integer)
            {
                CreateWithIntLabels();
            }
            else
            {
                // XXX TODO : unknown invariant type
                Console.Error.WriteLine("unknown invariant type " + invariantType);
            }
        }

        private void CreateWithIntLabels()
        {
            int[] vertexLabels = new int[vertexCount];
            foreach (var externalIndex in vertexMapping.Keys)
            {
                int internalIndex = vertexMapping[externalIndex];
                vertexLabels[internalIndex] = GetIntLabel(externalIndex);
            }
            dag.InitializeWithIntLabels(vertexLabels);
        }

        private void CreateWithStringLabels()
        {
            string[] vertexLabels = new string[vertexCount];
            foreach (var externalIndex in vertexMapping.Keys)
            {
                int internalIndex = vertexMapping[externalIndex];
                vertexLabels[internalIndex] = GetVertexSymbol(externalIndex);
            }
            dag.InitializeWithStringLabels(vertexLabels);
        }

        private void Builder(int layer,
                List<DAG.Node> previousLayer, List<DAG.Arc> usedArcs, int height)
        {
            if (height == 0) return;
            List<DAG.Node> nextLayer = new List<DAG.Node>();
            List<DAG.Arc> layerArcs = new List<DAG.Arc>();
            foreach (var node in previousLayer)
            {
                int mappedIndex = GetOriginalVertexIndex(node.vertexIndex);
                int[] connected = GetConnected(mappedIndex);
                Array.Sort(connected);
                foreach (var connectedVertex in connected)
                {
                    AddNode(
                      layer, node, connectedVertex, layerArcs, usedArcs, nextLayer);
                }
            }
            usedArcs.AddRange(layerArcs);
            if (!nextLayer.Any())
            {
                return;
            }
            else
            {
                dag.AddLayer(nextLayer);
                Builder(layer + 1, nextLayer, usedArcs, height - 1);
            }
        }

        private void AddNode(int layer, DAG.Node parentNode, int vertexIndex,
                List<DAG.Arc> layerArcs, List<DAG.Arc> usedArcs,
                List<DAG.Node> nextLayer)
        {

            // look up the mapping or create a new mapping for the vertex index
            int mappedVertexIndex;
            if (vertexMapping.ContainsKey(vertexIndex))
            {
                mappedVertexIndex = vertexMapping[vertexIndex];
            }
            else
            {
                vertexMapping[vertexIndex] = vertexCount;
                mappedVertexIndex = vertexCount;
                vertexCount++;
            }

            // find an existing node if there is one
            var arc = new DAG.Arc(parentNode.vertexIndex, mappedVertexIndex);
            if (usedArcs.Contains(arc)) return;
            DAG.Node existingNode = null;
            foreach (var otherNode in nextLayer)
            {
                if (otherNode.vertexIndex == mappedVertexIndex)
                {
                    existingNode = otherNode;
                    break;
                }
            }

            // if there isn't, make a new node and add it to the layer
            if (existingNode == null)
            {
                existingNode = dag.MakeNode(mappedVertexIndex, layer);
                nextLayer.Add(existingNode);
            }

            // add the edge label to the node's edge label list
            int originalParentIndex =
                GetOriginalVertexIndex(parentNode.vertexIndex);
            string edgeLabel = GetEdgeLabel(originalParentIndex, vertexIndex);
            int edgeColor = ConvertEdgeLabelToColor(edgeLabel);
            existingNode.AddEdgeColor(parentNode.vertexIndex, edgeColor);
            parentNode.AddEdgeColor(mappedVertexIndex, edgeColor);

            dag.AddRelation(existingNode, parentNode);
            layerArcs.Add(arc);
        }

        /// <summary>
        /// Convert this signature into a canonical signature string.
        /// </summary>
        /// <returns>the canonical string form</returns>
        public string ToCanonicalString()
        {
            var stringBuffer = new StringBuilder();
            TMP_COLORING_COUNT = 0;
            this.Canonize(0, stringBuffer);
            return stringBuffer.ToString();
        }

        public int TMP_COLORING_COUNT;

        /// <summary>
        /// Find the minimal signature string by trying all colors.
        /// </summary>
        /// <param name="color">the current color to use</param>
        /// <param name="canonicalVertexSignature">the buffer to fill</param>
        public void Canonize(int color, StringBuilder canonicalVertexSignature)
        {
            // assume that the atom invariants have been initialized
            if (this.GetVertexCount() == 0) return;

            this.dag.UpdateVertexInvariants();
            int[] parents = dag.GetParentsInFinalString();
            List<int> orbit = this.dag.CreateOrbit(parents);
            if (orbit.Count < 2)
            {
                // Color all uncolored atoms having two parents 
                // or more according to their invariant.
                List<InvariantInt> pairs = dag.GetInvariantPairs(parents);
                foreach (var pair in pairs)
                {
                    this.dag.SetColor(pair.index, color);
                    color++;
                }

                TMP_COLORING_COUNT++;

                // Creating the root signature string.
                string signature = this.ToString();
                int cmp = string.Compare(signature, canonicalVertexSignature.ToString(), StringComparison.Ordinal);
                int l = canonicalVertexSignature.Length;
                if (cmp > 0)
                {
                    var temp = signature + canonicalVertexSignature.ToString().Substring(l);
                    canonicalVertexSignature.Clear();
                    canonicalVertexSignature.Append(temp);
                }
                else
                {
                }
                return;
            }
            else
            {
                foreach (var o in orbit)
                {
                    this.dag.SetColor(o, color);
                    Invariants invariantsCopy = this.dag.CopyInvariants();
                    this.Canonize(color + 1, canonicalVertexSignature);
                    this.dag.SetInvariants(invariantsCopy);
                    this.dag.SetColor(o, -1);
                }
            }
        }

        /// <summary>
        /// Get a canonical labelling for this signature. Note that a signature that
        /// does not cover the graph (has a height &lt; graph diameter) will not have
        /// labels for every vertex. Unlabelled vertices will have a value of -1. To
        /// handle all cases, the total number of vertices must be passed to the 
        /// method.
        /// </summary>
        /// <param name="totalVertexCount">the number of vertices in the graph</param>
        /// <returns>the permutation necessary to transform the graph into a canonical form</returns>
        public int[] GetCanonicalLabelling(int totalVertexCount)
        {
            // TODO : get the totalVertexCount from the graph?
            Canonize(0, new StringBuilder());
            var labeller =
                new CanonicalLabellingVisitor(GetVertexCount(), dag.nodeComparator);
            this.dag.Accept(labeller);
            int[] internalLabels = labeller.GetLabelling();
            int[] externalLabels = new int[totalVertexCount];
            Arrays.Fill(externalLabels, -1);
            for (int i = 0; i < GetVertexCount(); i++)
            {
                int externalIndex = GetOriginalVertexIndex(i);
                externalLabels[externalIndex] = internalLabels[i];
            }
            return externalLabels;
        }

        public void Accept(IDAGVisitor visitor)
        {
            dag.Accept(visitor);
        }

        /// <summary>
        /// Get the number of vertices.
        /// </summary>
        /// <returns>the number of vertices seen when making the signature, which may
        ///         be less than the number in the full graph, depending on the height</returns>
        public int GetVertexCount()
        {
            return this.vertexCount;
        }

        /// <summary>
        /// Convert the edge label (if any) to an integer color, for example the bond
        /// order in a chemistry implementation.
        /// </summary>
        /// <param name="label">the label for an edge</param>
        /// <returns>an int color</returns>
        public abstract int ConvertEdgeLabelToColor(string label);

        /// <summary>
        /// Get the integer label for a vertex - in chemistry implementations this
        /// will be the element mass.
        /// </summary>
        /// <param name="vertexIndex">the index of the vertex in the input graph</param>
        /// <returns>an integer label</returns>
        public abstract int GetIntLabel(int vertexIndex);

        /// <summary>
        /// Get the symbol to use in the output signature string for this vertex of 
        /// the input graph.
        /// </summary>
        /// <param name="vertexIndex">the index of the vertex in the input graph</param>
        /// <returns>a string symbol</returns>
        public abstract string GetVertexSymbol(int vertexIndex);

        /// <summary>
        /// Get a list of the indices of the vertices connected to the vertex with 
        /// the supplied index.
        /// </summary>
        /// <param name="vertexIndex">the index of the vertex to use</param>
        /// <returns>the indices of connected vertices in the input graph</returns>
        public abstract int[] GetConnected(int vertexIndex);

        /// <summary>
        /// Get the symbol (if any) for the edge between the vertices with these two indices.
        /// </summary>
        /// <param name="vertexIndex">the index of one of the vertices in the edge</param>
        /// <param name="otherVertexIndex">the index of the other vertex in the edge </param>
        /// <returns>a string symbol for this edge</returns>
        public abstract string GetEdgeLabel(int vertexIndex, int otherVertexIndex);

        /// <summary>
        /// Recursively print the signature into the buffer.
        /// </summary>
        /// <param name="buffer">the string buffer to print into</param>
        /// <param name="node">the current node of the signature</param>
        /// <param name="parent">the parent node, or null</param>
        /// <param name="arcs">the list of already visited arcs</param>
        private void Print(StringBuilder buffer, DAG.Node node, DAG.Node parent, List<DAG.Arc> arcs)
        {
            int vertexIndex = GetOriginalVertexIndex(node.vertexIndex);

            // print out any symbol for the edge in the input graph
            if (parent != null)
            {
                int parentVertexIndex = GetOriginalVertexIndex(parent.vertexIndex);
                buffer.Append(GetEdgeLabel(vertexIndex, parentVertexIndex));
            }

            // print out the text that represents the node itself
            buffer.Append(AbstractVertexSignature.StartNodeSymbolChar);
            buffer.Append(GetVertexSymbol(vertexIndex));
            int color = dag.ColorFor(node.vertexIndex);
            if (color != -1)
            {
                buffer.Append(',').Append(color);
            }
            buffer.Append(AbstractVertexSignature.EndNodeSymbolChar);

            // Need to sort the children here, so that they are printed in an order 
            // according to their invariants.
            node.children.Sort(dag.nodeComparator);

            // now print the sorted children, surrounded by branch symbols
            bool addedBranchSymbol = false;
            foreach (var child in node.children)
            {
                DAG.Arc arc = new DAG.Arc(node.vertexIndex, child.vertexIndex);
                if (arcs.Contains(arc))
                {
                    continue;
                }
                else
                {
                    if (!addedBranchSymbol)
                    {
                        buffer.Append(AbstractVertexSignature.StartBranchSymbolChar);
                        addedBranchSymbol = true;
                    }
                    arcs.Add(arc);
                    Print(buffer, child, node, arcs);
                }
            }
            if (addedBranchSymbol)
            {
                buffer.Append(AbstractVertexSignature.EndBranchSymbolChar);
            }
        }

        /// <summary>
        /// Convert this vertex signature into a signature string.
        /// </summary>
        public override string ToString()
        {
            var buffer = new StringBuilder();
            Print(buffer, this.dag.GetRoot(), null, new List<DAG.Arc>());
            return buffer.ToString();
        }

        public static ColoredTree Parse(string s)
        {
            ColoredTree tree = null;
            ColoredTree.Node parent = null;
            ColoredTree.Node current = null;
            int currentHeight = 1;
            int color = -1;
            int j = 0;
            int k = 0;
            int l = 0;
            string edgeSymbol = null;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == AbstractVertexSignature.StartBranchSymbolChar)
                {
                    parent = current;
                    currentHeight++;
                    tree.UpdateHeight(currentHeight);
                    l = i;
                }
                else if (c == AbstractVertexSignature.EndBranchSymbolChar)
                {
                    parent = parent.parent;
                    currentHeight--;
                    l = i;
                }
                else if (c == StartNodeSymbolChar)
                {
                    if (l < i)
                    {
                        edgeSymbol = s.Substring(l + 1, i - (l + 1));
                        l = i;
                    }
                    j = i + 1;
                }
                else if (c == EndNodeSymbolChar)
                {
                    string ss;
                    if (k < j)
                    {    // no color
                        ss = s.Substring(j, i - j);
                        color = -1;
                    }
                    else
                    {        // color
                        ss = s.Substring(j, k - 1 - j);
                        color = int.Parse(s.Substring(k, i - k), NumberFormatInfo.InvariantInfo);
                    }
                    if (tree == null)
                    {
                        tree = new ColoredTree(ss);
                        parent = tree.GetRoot();
                        current = tree.GetRoot();
                    }
                    else
                    {
                        if (edgeSymbol == null)
                        {
                            current = tree.MakeNode(
                                    ss, parent, currentHeight, color);
                        }
                        else
                        {
                            current = tree.MakeNode(
                                    ss, parent, currentHeight, color, edgeSymbol);
                        }
                    }
                    edgeSymbol = null;
                    l = i;
                }
                else if (c == ',')
                {
                    k = i + 1;
                }
            }
            return tree;
        }
    }
}
