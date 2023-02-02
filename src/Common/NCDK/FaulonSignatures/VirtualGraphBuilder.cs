using System;
using System.Collections.Generic;
using System.Text;

namespace NCDK.FaulonSignatures
{
    /// <summary>
    /// Only intended for use in creating 'virtual' graphs for checking canonicity.
    /// </summary>
    // @author maclean
    public class VirtualGraphBuilder : AbstractGraphBuilder
    {
        private class VirtualEdge : IComparable<VirtualEdge>
        {
            public readonly int lowerVertexIndex;
            public readonly int upperVertexIndex;
            public readonly string lowerVertexSymbol;
            public readonly string upperVertexSymbol;
            public readonly string edgeLabel;

            public VirtualEdge(int vertexIndex1, int vertexIndex2,
                    string vertexSymbol1, string vertexSymbol2, string edgeLabel)
            {
                if (vertexIndex1 < vertexIndex2)
                {
                    this.lowerVertexIndex = vertexIndex1;
                    this.upperVertexIndex = vertexIndex2;
                    this.lowerVertexSymbol = vertexSymbol1;
                    this.upperVertexSymbol = vertexSymbol2;
                }
                else
                {
                    this.lowerVertexIndex = vertexIndex2;
                    this.upperVertexIndex = vertexIndex1;
                    this.lowerVertexSymbol = vertexSymbol2;
                    this.upperVertexSymbol = vertexSymbol1;
                }
                this.edgeLabel = edgeLabel;
            }

            public int CompareTo(VirtualEdge o)
            {
                if (this.lowerVertexIndex < o.lowerVertexIndex)
                {
                    return -1;
                }
                else if (this.lowerVertexIndex == o.lowerVertexIndex)
                {
                    if (this.upperVertexIndex < o.upperVertexIndex)
                    {
                        return -1;
                    }
                    else if (this.upperVertexIndex == o.upperVertexIndex)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else
                {
                    return 1;
                }
            }

            public override string ToString()
            {
                return this.lowerVertexIndex + this.lowerVertexSymbol +
                        ":" + this.upperVertexIndex + this.upperVertexSymbol
                        + "(" + edgeLabel + ")";
            }
        }

        private readonly List<VirtualEdge> edges;

        public VirtualGraphBuilder()
            : base()
        {
            this.edges = new List<VirtualEdge>();
        }

        public string ToEdgeString()
        {
            var edgeString = new StringBuilder();
            this.edges.Sort();
            foreach (var edge in this.edges)
            {
                edgeString.Append(edge.ToString()).Append(",");
            }
            return edgeString.ToString();
        }

        public override void MakeEdge(int vertexIndex1, int vertexIndex2,
                string vertexSymbol1, string vertexSymbol2, string edgeLabel)
        {
            this.edges.Add(
                    new VirtualEdge(
                         vertexIndex1, vertexIndex2,
                         vertexSymbol1, vertexSymbol2, edgeLabel));
        }

        public override void MakeGraph()
        {
            this.edges.Clear();
        }

        public override void MakeVertex(string label)
        {
            // do nothing
        }
    }
}
