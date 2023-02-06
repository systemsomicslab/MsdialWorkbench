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

using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Convert direction (up/down) bonds to trigonal topology (double bond atom
    /// centric stereo specification).
    /// </summary>
    /// <remarks>
    /// <code>
    ///    F/C=C/F -> F/[C@H]=[C@H]F
    ///    F/C=C\F -> F/[C@H]=[C@@H]F
    ///    F\C=C/F -> F/[C@@H]=[C@H]F
    ///    F\C=C\F -> F/[C@@H]=[C@@H]F
    /// </code>
    /// </remarks>
    // @author John May
    internal sealed class ToTrigonalTopology : AbstractFunction<Graph, Graph>
    {
        public override Graph Apply(Graph g)
        {
            Graph h = new Graph(g.Order);

            // original topology information this is unchanged
            for (int u = 0; u < g.Order; u++)
            {
                h.AddTopology(g.TopologyOf(u));
            }

            var replacements = new Dictionary<Edge, Edge>();

            // change edges (only changed added to replacement)
            for (int u = 0; u < g.Order; u++)
            {
                foreach (var e in g.GetEdges(u))
                {
                    if (e.Other(u) > u && e.Bond == Bond.Up || e
                            .Bond == Bond.Down)
                    {
                        replacements[e] = new Edge(u, e.Other(u), Bond.Implicit);
                    }
                }
            }

            List<Edge> es = DoubleBondLabelledEdges(g);

            foreach (var e in es)
            {
                int u = e.Either();
                int v = e.Other(u);

                // add to topologies
                h.AddTopology(ToTrigonal(g, e, u));
                h.AddTopology(ToTrigonal(g, e, v));
            }

            for (int u = 0; u < g.Order; u++)
            {
                IAtom a = g.GetAtom(u);
                if (a.Subset && h.TopologyOf(u) != Topology.Unknown)
                {
                    h.AddAtom(AsBracketAtom(u, g));
                }
                else
                {
                    h.AddAtom(a);
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

        private static IAtom AsBracketAtom(int u, Graph g)
        {
            IAtom a = g.GetAtom(u);
            int sum = a.IsAromatic() ? 1 : 0;
            foreach (var e in g.GetEdges(u))
            {
                sum += e.Bond.Order;
            }
            return new AtomImpl.BracketAtom(-1,
                                            a.Element,
                                            a.IsAromatic() ? a.Element.NumOfAromaticImplicitHydrogens(sum)
                                                         : a.Element.NumOfImplicitHydrogens(sum),
                                            0,
                                            0,
                                            a.IsAromatic());
        }

        private static Topology ToTrigonal(Graph g, Edge e, int u)
        {
            var es = g.GetEdges(u);
            int offset = es.IndexOf(e);

            // vertex information for topology
            int[] vs = new int[] {
                e.Other(u), // double bond
                u,          // for implicit H
                u,          // for implicit H
            };

            if (es.Count == 2)
            {
                Edge e1 = es[(offset + 1) % 2];
                Bond b = e1.GetBond(u);
                if (IsUp(b))
                {
                    vs[1] = e1.Other(u);
                }
                else if (IsDown(b))
                {
                    vs[2] = e1.Other(u);
                }
            }
            else if (es.Count == 3)
            {
                Edge e1 = es[(offset + 1) % 3];
                Edge e2 = es[(offset + 2) % 3];
                Bond b1 = e1.GetBond(u);
                Bond b2 = e2.GetBond(u);
                if (b1 == Bond.Single || b1 == Bond.Implicit)
                {
                    if (IsUp(b2))
                    {
                        vs[1] = e2.Other(u);
                        vs[2] = e1.Other(u);
                    }
                    else if (IsDown(b2))
                    {
                        vs[1] = e1.Other(u);
                        vs[2] = e2.Other(u);
                    }
                }
                else
                {
                    if (IsUp(b1))
                    {
                        vs[1] = e1.Other(u);
                        vs[2] = e2.Other(u);
                    }
                    else if (IsDown(b1))
                    {
                        vs[1] = e2.Other(u);
                        vs[2] = e1.Other(u);
                    }
                }
            }

            if (vs[1] == vs[2])
                return Topology.Unknown;

            Configuration c = es[offset].Other(u) < u ? Configuration.DB1
                                                          : Configuration.DB2;


            return Topology.CreateTrigonal(u, vs, c);
        }

        static bool IsUp(Bond b)
        {
            return b == Bond.Up;
        }

        static bool IsDown(Bond b)
        {
            return b == Bond.Down;
        }

        private static List<Edge> DoubleBondLabelledEdges(Graph g)
        {
            List<Edge> es = new List<Edge>();
            for (int u = 0; u < g.Order; u++)
            {
                foreach (var e in g.GetEdges(u))
                {
                    if (e.Other(u) > u && e.Bond == Bond.Double)
                    {
                        es.Add(e);
                    }
                }
            }
            return es;
        }
    }
}
