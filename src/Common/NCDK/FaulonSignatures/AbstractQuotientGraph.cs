using System.Collections.Generic;
using System.Text;

namespace NCDK.FaulonSignatures
{
    /// <summary>
    /// A quotient graph Q is derived from a simple graph G (or molecule graph) by 
    /// determining the signature for each vertex in G, and making a vertex in Q
    /// for each signature. These vertices in Q are then connected to each other -
    /// or <b>themselves</b> - if vertices in G with those signatures are connected.
    /// 
    /// Therefore, the quotient graph is a summary of the original graph.
    /// </summary>
    // @author maclean
    public abstract class AbstractQuotientGraph
    {
        private class Vertex
        {
            public List<int> members;
            public string signature;

            public Vertex(List<int> members, string signature)
            {
                this.members = members;
                this.signature = signature;
            }

            public override string ToString()
            {
                return signature + " " + members;
            }
        }

        private class Edge
        {
            public int count;
            public int vertexIndexA;
            public int vertexIndexB;

            public Edge(int vertexIndexA, int vertexIndexB, int count)
            {
                this.vertexIndexA = vertexIndexA;
                this.vertexIndexB = vertexIndexB;
                this.count = count;
            }

            public bool IsLoop()
            {
                return vertexIndexA == vertexIndexB;
            }

            public override string ToString()
            {
                return vertexIndexA + "-" + vertexIndexB + "(" + count + ")";
            }
        }

        private List<Vertex> vertices;
        private List<Edge> edges;

        protected AbstractQuotientGraph()
        {
            vertices = new List<Vertex>();
            edges = new List<Edge>();
        }

        public int GetVertexCount()
        {
            return vertices.Count;
        }

        public int GetEdgeCount()
        {
            return edges.Count;
        }

        public int NumberOfLoopEdges()
        {
            int loopEdgeCount = 0;
            foreach (var e in edges)
            {
                if (e.IsLoop())
                {
                    loopEdgeCount++;
                }
            }
            return loopEdgeCount;
        }

        public abstract bool IsConnected(int i, int j);

        public List<string> GetVertexSignatureStrings()
        {
            var signatureStrings = new List<string>();
            foreach (var vertex in vertices)
            {
                signatureStrings.Add(vertex.signature);
            }
            return signatureStrings;
        }

        public void Construct(List<SymmetryClass> symmetryClasses)
        {
            // make the vertices from the symmetry classes
            for (int i = 0; i < symmetryClasses.Count; i++)
            {
                SymmetryClass symmetryClass = symmetryClasses[i];
                string signatureString = symmetryClass.GetSignatureString();
                List<int> members = new List<int>();
                foreach (var e in symmetryClass) { members.Add(e); }
                vertices.Add(new Vertex(members, signatureString));
            }

            // compare all vertices (classwise) for connectivity
            List<Edge> visitedEdges = new List<Edge>();
            for (int i = 0; i < symmetryClasses.Count; i++)
            {
                SymmetryClass symmetryClass = symmetryClasses[i];
                for (int j = i; j < symmetryClasses.Count; j++)
                {
                    SymmetryClass otherSymmetryClass = symmetryClasses[j];
                    int totalCount = 0;
                    foreach (var x in symmetryClass)
                    {
                        int countForX = 0;
                        foreach (var y in otherSymmetryClass)
                        {
                            if (x == y) continue;
                            if (IsConnected(x, y)
                                    && !InVisitedEdges(x, y, visitedEdges))
                            {
                                countForX++;
                                visitedEdges.Add(new Edge(x, y, 0));
                            }
                        }
                        totalCount += countForX;
                    }
                    if (totalCount > 0)
                    {
                        edges.Add(new Edge(i, j, totalCount));
                    }
                }
            }
        }

        private static bool InVisitedEdges(int x, int y, List<Edge> visitedEdges)
        {
            foreach (var edge in visitedEdges)
            {
                if ((edge.vertexIndexA == x && edge.vertexIndexB == y)
                        || (edge.vertexIndexA == y && edge.vertexIndexB == x))
                {
                    return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            var buffer = new StringBuilder();
            int i = 0;
            foreach (var vertex in vertices)
            {
                buffer.Append(i).Append(' ').Append(vertex).Append('\n');
                i++;
            }
            buffer.Append(edges);
            return buffer.ToString();
        }
    }
}
