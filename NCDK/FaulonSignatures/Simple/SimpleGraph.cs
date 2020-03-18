using System;
using System.Collections.Generic;
using System.Globalization;

namespace NCDK.FaulonSignatures.Simple
{
    /// <summary>
    /// A very simple graph class - the equivalent of a client library class.
    /// </summary>
    // @author maclean
    public class SimpleGraph
    {
        public class Edge : IComparable<Edge>
        {
            public int a;
            public int b;

            public Edge(int a, int b)
            {
                if (a < b)
                {
                    this.a = a;
                    this.b = b;
                }
                else
                {
                    this.a = b;
                    this.b = a;
                }
            }

            public int CompareTo(Edge other)
            {
                if (this.a < other.a || (this.a == other.a && this.b < other.b))
                {
                    return -1;
                }
                else
                {
                    if (this.a == other.a && this.b == other.b)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }

            public override string ToString()
            {
                return this.a + "-" + this.b;
            }

        }

        public List<Edge> edges;
        public int maxVertexIndex;
        public string name;

        public SimpleGraph()
        {
            this.edges = new List<Edge>();
        }

        public SimpleGraph(string graphString)
                : this()
        {
            foreach (var edgeString in graphString.Split(','))
            {
                string[] vertexStrings = edgeString.Split(':');
                int a = int.Parse(vertexStrings[0], NumberFormatInfo.InvariantInfo);
                int b = int.Parse(vertexStrings[1], NumberFormatInfo.InvariantInfo);
                MakeEdge(a, b);
            }
        }

        public SimpleGraph(SimpleGraph graph, int[] permutation)
                : this()
        {
            foreach (var e in graph.edges)
            {
                MakeEdge(permutation[e.a], permutation[e.b]);
            }
        }

        public virtual void MakeEdge(int a, int b)
        {
            if (a > maxVertexIndex) maxVertexIndex = a;
            if (b > maxVertexIndex) maxVertexIndex = b;
            this.edges.Add(new Edge(a, b));
        }

        public void MakeEdges(int a, params int[] bs)
        {
            foreach (var b in bs)
            {
                MakeEdge(a, b);
            }
        }

        public int GetVertexCount()
        {
            return this.maxVertexIndex + 1;
        }

        public virtual bool IsConnected(int i, int j)
        {
            foreach (var e in edges)
            {
                if ((e.a == i && e.b == j) || (e.b == i && e.a == j))
                {
                    return true;
                }
            }
            return false;
        }

        public virtual int[] GetConnected(int vertexIndex)
        {
            List<int> connected = new List<int>();
            foreach (var edge in this.edges)
            {
                if (edge.a == vertexIndex)
                {
                    connected.Add(edge.b);
                }
                else if (edge.b == vertexIndex)
                {
                    connected.Add(edge.a);
                }
                else
                {
                    continue;
                }
            }
            int[] connectedArray = new int[connected.Count];
            int i = 0;
            foreach (var connectedVertexIndex in connected)
            {
                connectedArray[i] = connectedVertexIndex;
                i++;
            }
            return connectedArray;
        }

        public int Degree(int vertexIndex)
        {
            int degreeCount = 0;
            foreach (var e in edges)
            {
                if (e.a == vertexIndex || e.b == vertexIndex)
                {
                    degreeCount++;
                }
            }
            return degreeCount;
        }

        public override string ToString()
        {
            edges.Sort();
            return edges.ToString();
        }
    }
}
