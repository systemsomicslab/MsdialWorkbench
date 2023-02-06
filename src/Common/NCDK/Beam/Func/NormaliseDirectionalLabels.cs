using System.Collections;
using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Normalise directional labels such that the first label is always a '/'. Given 
    /// a molecule with directional bonds "F\C=C\F" the labels are normalised 
    /// to be "F/C=C/F".
    /// </summary>
    // @author John May
    internal sealed class NormaliseDirectionalLabels
            : AbstractFunction<Graph, Graph>
    {
        public override Graph Apply(Graph g)
        {
            Traversal traversal = new Traversal(g);
            Graph h = new Graph(g.Order);

            // copy atom/topology information this is unchanged
            for (int u = 0; u < g.Order; u++)
            {
                h.AddAtom(g.GetAtom(u));
                h.AddTopology(g.TopologyOf(u));
            }
            
            // change edges (only changed added to replacement)
            for (int u = 0; u < g.Order; u++)
            {
                foreach (var e in g.GetEdges(u))
                {
                    if (e.Other(u) > u)
                    {
                        if (traversal.acc.ContainsKey(e))
                        {
                            h.AddEdge(traversal.acc[e]);
                        }
                        else
                        {
                            h.AddEdge(e);
                        }
                    }
                }
            }

            return h.Sort(new Graph.CanOrderFirst());
        }

        private sealed class Traversal
        {
            private readonly Graph g;
            private readonly bool[] visited;
            private readonly int[] ordering;
            private int i;
            public Dictionary<Edge, Edge> acc = new Dictionary<Edge, Edge>();

            private List<Edge> doubleBonds = new List<Edge>();
            private HashSet<int> adj = new HashSet<int>();

            public Traversal(Graph g)
            {
                this.g = g;
                this.visited = new bool[g.Order];
                this.ordering = new int[g.Order];

                var dbAtoms = new BitArray(g.Order);
                for (int u = 0; u < g.Order; u++)
                {
                    if (!visited[u])
                        dbAtoms.Or(Visit(u, u));
                }

                foreach (var e in doubleBonds)
                {
                    Flip(g, e, dbAtoms);
                }
            }

            private BitArray Visit(int p, int u)
            {
                visited[u] = true;
                ordering[u] = i++;
                var dbAtoms = new BitArray(g.Order);
                foreach (var e in g.GetEdges(u))
                {
                    int v = e.Other(u);

                    if (!visited[v])
                    {
                        if (e.Bond == Bond.Double && HasAdjDirectionalLabels(g, e))
                        {
                            dbAtoms.Set(u, true);
                            dbAtoms.Set(v, true);

                            // only the first bond we encounter in an isolated system
                            // is marked - if we need to flip the other we propagate
                            // this down the chain
                            bool newSystem = !adj.Contains(u) && !adj.Contains(v);

                            // to stop adding other we mark all vertices adjacent to the
                            // double bond
                            foreach (var f in g.GetEdges(u))
                                adj.Add(f.Other(u));
                            foreach (var f in g.GetEdges(v))
                                adj.Add(f.Other(v));

                            if (newSystem)
                                doubleBonds.Add(e);
                        }
                        dbAtoms.Or(Visit(u, v));
                    }
                }

                return dbAtoms;
            }

            private static bool HasAdjDirectionalLabels(Graph g, Edge e)
            {
                int u = e.Either();
                int v = e.Other(u);
                return HasAdjDirectionalLabels(g, u) && HasAdjDirectionalLabels(g, v);
            }

            private static bool HasAdjDirectionalLabels(Graph g, int u)
            {
                foreach (var f in g.GetEdges(u))
                    if (f.Bond.IsDirectional)
                        return true;
                return false;
            }

            private void Flip(Graph g, Edge e, BitArray dbAtoms)
            {
                var u = e.Either();
                var v = e.Other(u);

                if (ordering[u] < ordering[v])
                {
                    var first = FirstDirectionalLabel(g, u);
                    if (first != null)
                    {
                        Flip(first, u, dbAtoms);
                    }
                    else
                    {
                        first = FirstDirectionalLabel(g, v);
                        Flip(first, v, dbAtoms);
                    }
                }
                else
                {
                    var first = FirstDirectionalLabel(g, v);
                    if (first != null)
                    {
                        Flip(first, v, dbAtoms);
                    }
                    else
                    {
                        first = FirstDirectionalLabel(g, u);
                        Flip(first, u, dbAtoms);
                    }
                }
            }

            private void Flip(Edge first, int u, BitArray dbAtoms)
            {
                if (ordering[first.Other(u)] < ordering[u])
                {
                    if (first.GetBond(u) == Bond.Up)
                        InvertExistingDirectionalLabels(g,
                                                        new BitArray(g.Order),
                                                        acc,
                                                        dbAtoms,
                                                        u);
                }
                else
                {
                    if (first.GetBond(u) == Bond.Down)
                        InvertExistingDirectionalLabels(g,
                                                        new BitArray(g.Order),
                                                        acc,
                                                        dbAtoms,
                                                        u);
                }
            }

            Edge FirstDirectionalLabel(Graph g, int u)
            {
                Edge first = null;
                foreach (var f in g.GetEdges(u))
                {
                    if (f.Bond == Bond.Up || f.Bond == Bond.Down)
                    {
                        if (first == null || ordering[f.Other(u)] < ordering[first.Other(u)])
                            first = f;
                    }
                }
                return first;
            }

            private void InvertExistingDirectionalLabels(Graph g,
                                                         BitArray visited,
                                                         Dictionary<Edge, Edge> replacement,
                                                         BitArray dbAtoms,
                                                         int u)
            {
                visited.Set(u, true);
                foreach (var e in g.GetEdges(u))
                {
                    var v = e.Other(u);
                    if (!visited[v])
                    {
                        if (replacement.TryGetValue(e, out Edge f))
                        {
                            replacement[e] = f.Inverse();
                        }
                        else
                        {
                            replacement[e] = e.Inverse();
                        }
                        if (dbAtoms[v])
                            InvertExistingDirectionalLabels(g, visited, replacement, dbAtoms, v);
                    }
                }
            }
        }
    }
}
