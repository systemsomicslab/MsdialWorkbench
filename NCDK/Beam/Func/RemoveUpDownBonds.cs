/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project.
 */

using NCDK.Common.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Beam
{
    /// <summary>
    /// Given a molecule with explict double bond configurations remove redundant
    /// Up/Down bond. For example the removing redundant up/down labels from of
    /// "N/C(/C)=C\C" produces "N/C(C)=C\C".
    /// </summary>
    // @author John May
    internal sealed class RemoveUpDownBonds : AbstractFunction<Graph, Graph>
    {
        public override Graph Apply(Graph g)
        {
            Graph h = new Graph(g.Order);

            // copy atom/topology information this is unchanged
            for (int u = 0; u < g.Order; u++)
            {
                h.AddAtom(g.GetAtom(u));
                h.AddTopology(g.TopologyOf(u));
            }

            var Ordering = new DepthFirstOrder(g).visited;

            var replacements = new Dictionary<Edge, Edge>();
            var dbCentres = new SortedSet<int>();

            // change edges (only changed added to replacement)
            for (int u = 0; u < g.Order; u++)
            {
                foreach (var e in g.GetEdges(u))
                {
                    if (e.Other(u) > u && e.Bond == Bond.Double)
                    {
                        RemoveRedundant(g, e, Ordering, replacements);
                        dbCentres.Add(u);
                        dbCentres.Add(e.Other(u));
                    }
                }
            }

            // ensure we haven't accidentally removed one between two
            foreach (var e in new HashSet<Edge>(replacements.Keys))
            {
                if (dbCentres.Contains(e.Either())
                        && dbCentres.Contains(e.Other(e.Either())))
                {
                    replacements.Remove(e);
                }
            }

            // append the edges, replacing any which need to be changed
            for (int u = 0; u < g.Order; u++)
            {
                foreach (var e in g.GetEdges(u))
                {
                    if (e.Other(u) > u)
                    {
                        Edge ee = e;
                        if (replacements.TryGetValue(e, out Edge replacement))
                            ee = replacement;
                        h.AddEdge(ee);
                    }
                }
            }

            return h;
        }

        /// <summary>
        /// Given a double bond edge traverse the neighbors of both endpoints and
        /// accumulate any explicit replacements in the 'acc' accumulator.
        /// </summary>
        /// <param name="g">  the chemical graph</param>
        /// <param name="e">  a edge in the graph </param>('double bond type')
        /// <param name="ordering"></param>
        /// <param name="acc">accumulator for new edges</param>
        /// <exception cref="InvalidSmilesException">thrown if the edge could not be converted</exception>
        private static void RemoveRedundant(Graph g,
                                     Edge e,
                                     int[] ordering,
                                     Dictionary<Edge, Edge> acc)
        {
            int u = e.Either(), v = e.Other(u);

            ReplaceImplWithExpl(g, e, u, ordering, acc);
            ReplaceImplWithExpl(g, e, v, ordering, acc);
        }

        private class S : IComparer<Edge>
        {
            readonly Edge e;
            readonly int u;
            readonly int[] ordering;

            public S(Edge e, int u, int[] ordering)
            {
                this.e = e;
                this.u = u;
                this.ordering = ordering;
            }

            public int Compare(Edge e, Edge f)
            {
                int v = ordering[e.Other(u)];
                int w = ordering[f.Other(u)];
                if (v > w)
                    return +1;
                if (v < w)
                    return -1;
                return 0;
            }
        }

        /// <summary>
        /// Given a double bond edge traverse the neighbors of one of the endpoints
        /// and accumulate any explicit replacements in the 'acc' accumulator.
        /// </summary>
        /// <param name="g">  the chemical graph</param>
        /// <param name="e">  a edge in the graph </param>('double bond type')
        /// <param name="u">  a endpoint of the edge 'e'</param>
        /// <param name="ordering"></param>
        /// <param name="acc">accumulator for new edges</param>
        /// <exception cref="InvalidSmilesException">thrown if the edge could not be converted</exception>
        private static void ReplaceImplWithExpl(Graph g,
                                      Edge e,
                                      int u,
                                      int[] ordering,
                                      Dictionary<Edge, Edge> acc)
        {
            ICollection<Edge> edges = new SortedSet<Edge>(new S(e, u, ordering));

            foreach (var f in g.GetEdges(u))
            {
                var aa = f.Bond;
                if (aa == Bond.Double)
                {
                    if (!f.Equals(e))
                        return;
                }
                else if (aa == Bond.Up || aa == Bond.Down)
                {
                    edges.Add(f);
                }
            }

            if (edges.Count == 2)
            {
                Edge explicit_ = edges.First();
                int v = explicit_.Either();
                int w = explicit_.Other(v);
                acc[explicit_] = new Edge(v, w, Bond.Implicit);
            }
            else if (edges.Count > 2)
            {
                throw new InvalidSmilesException("Too many up/down bonds on double bonded atom");
            }
        }

        private sealed class DepthFirstOrder
        {
            private readonly Graph g;
            public readonly int[] visited;
            private int i;

            public DepthFirstOrder(Graph g)
            {
                this.g = g;
                this.visited = new int[g.Order];
                Arrays.Fill(visited, -1);
                for (int u = 0; u < g.Order; u++)
                {
                    if (visited[u] < 0)
                    {
                        Visit(u);
                    }
                }
            }

            private void Visit(int u)
            {
                visited[u] = i++;
                foreach (var e in g.GetEdges(u))
                {
                    int v = e.Other(u);
                    if (visited[v] < 0)
                    {
                        Visit(v);
                    }
                }
            }
        }
    }
}
