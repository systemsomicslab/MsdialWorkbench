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
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Aromaticity perception using AllCycles.
    /// </summary>
    // @author John May
    internal sealed class AllCycles
    {
        /// <summary>Number of pi electrons for Sp2 atoms.</summary>
        private readonly int[] ps;

        private readonly IList<PathEdge>[] pathGraph;

        public bool[] aromatic;

        private readonly Graph org;

        private const int MAX_VERTEX_DEGREE = 684;
        class Inner_ElectronDonation_Cycle : ElectronDonation.ICycle
        {
            public bool Contains(int u)
            {
                throw new NotSupportedException();
            }
        }

        public AllCycles(Graph g, ElectronDonation model, int lim)
        {
            this.org = g;
            this.ps = new int[g.Order];
            this.pathGraph = new IList<PathEdge>[g.Order];
            this.aromatic = new bool[g.Order];

            ElectronDonation.ICycle cycle = new Inner_ElectronDonation_Cycle();

            BitArray cyclic = new BiconnectedComponents(g).Cyclic;

            for (int u = 0; u < g.Order; u++)
                ps[u] = model.Contribution(u, g, cycle, cyclic);

            for (int u = 0; u < g.Order; u++)
                this.pathGraph[u] = new List<PathEdge>();

            // build the path graph
            foreach (var e in g.Edges)
            {
                int u = e.Either();
                int v = e.Other(u);
                if (cyclic[u] && cyclic[v] && ps[u] >= 0 && ps[v] >= 0)
                {
                    PathEdge f = new PathEdge(u, v, new BitArray(g.Order), 0);
                    Add(u, v, f);
                }
            }

            for (int u = 0; u < g.Order; u++)
            {
                if (this.pathGraph[u].Count > MAX_VERTEX_DEGREE)
                    throw new ArgumentException("too many cycles generated: " + pathGraph[u].Count);
                Reduce(u, lim);
            }
        }

        public Graph AromaticForm()
        {
            Graph cpy = new Graph(org.Order);
            cpy.AddFlags(org.GetFlags(-1)); //0xffffffff

            for (int i = 0; i < org.Order; i++)
            {
                if (aromatic[i])
                {
                    cpy.AddAtom(org.GetAtom(i).AsAromaticForm());
                    cpy.AddFlags(Graph.HAS_AROM);
                }
                else
                {
                    cpy.AddAtom(org.GetAtom(i));
                }
                cpy.AddTopology(org.TopologyOf(i));
            }

            foreach (var e in org.Edges)
            {
                int u = e.Either();
                int v = e.Other(u);
                if (aromatic[u] && aromatic[v])
                {
                    // check implHCount for subset
                    cpy.AddEdge(new Edge(u, v, Bond.Implicit));
                }
                else
                {
                    cpy.AddEdge(e);
                }
            }

            // check for required hydrogens
            for (int i = 0; i < org.Order; i++)
            {
                int hCount = org.ImplHCount(i);
                if (hCount != cpy.ImplHCount(i))
                {
                    cpy.SetAtom(i,
                                new AtomImpl.BracketAtom(-1,
                                                         cpy.GetAtom(i).Element,
                                                         hCount,
                                                         0,
                                                         0,
                                                         true));
                }
            }
            return cpy.Sort(new Graph.CanOrderFirst());
        }

        private void Add(PathEdge e)
        {
            int u = e.Either();
            int v = e.Other(u);
            Add(u, v, e);
        }

        private void Add(int u, int v, PathEdge e)
        {
            this.pathGraph[Math.Min(u, v)].Add(e);
        }

        private void Reduce(int x, int lim)
        {
            IList<PathEdge> es = pathGraph[x];
            int deg = es.Count;
            for (int i = 0; i < deg; i++)
            {
                PathEdge e1 = es[i];
                for (int j = i + 1; j < deg; j++)
                {
                    PathEdge e2 = es[j];
                    if (!e1.Intersects(e2))
                    {
                        PathEdge reduced = Reduce(e1, e2, x);
                        if (BitArrays.Cardinality(reduced.xs) >= lim)
                            continue;
                        if (reduced.Loop())
                        {
                            if (reduced.CheckPiElectrons(ps))
                            {
                                reduced.Flag(aromatic);
                            }
                        }
                        else
                        {
                            Add(reduced);
                        }
                    }
                }
            }
            pathGraph[x].Clear();
        }

        static BitArray Union(BitArray s, BitArray t, int x)
        {
            BitArray u = (BitArray)s.Clone();
            u.Or(t);
            u.Set(x, true);
            return u;
        }

        private PathEdge Reduce(PathEdge e, PathEdge f, int x)
        {
            return new PathEdge(e.Other(x),
                                f.Other(x),
                                Union(e.xs, f.xs, x),
                                ps[x] + e.ps + f.ps);
        }

        sealed class PathEdge
        {
            /* Reduced vertices. */
            internal BitArray xs;
            /// <summary>End points of the edge.</summary>
            int u, v;
            /// <summary>Number of pi electrons in the path.</summary>
            internal int ps;

            public PathEdge(int u, int v, BitArray xs, int ps)
            {
                this.xs = xs;
                this.ps = ps;
                this.u = u;
                this.v = v;
            }

            public int Either()
            {
                return u;
            }

            public int Other(int x)
            {
                return (x == u) ? v : u;
            }

            public bool Loop()
            {
                return u == v;
            }

            // 4n+2
            public bool CheckPiElectrons(int[] ps)
            {
                return (this.ps + ps[u] - 2) % 4 == 0;
            }

            public void Flag(bool[] mark)
            {
                mark[u] = true;
                for (int i = BitArrays.NextSetBit(xs, 0); i >= 0; i = BitArrays.NextSetBit(xs, i + 1))
                {
                    mark[i] = true;
                }
            }

            public bool Intersects(PathEdge e)
            {
                return BitArrays.Intersects(e.xs, xs);
            }
        }

        public static AllCycles DaylightModel(Graph g)
        {
            return new AllCycles(g, ElectronDonation.Daylight, g.Order);
        }

        public static AllCycles DaylightModel(Graph g, int lim)
        {
            return new AllCycles(g, ElectronDonation.Daylight, lim);
        }
    }
}
