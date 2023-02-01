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

namespace NCDK.Beam
{
    /// <summary>
    /// Convert a chemical graph with implicit edge labels to one with explicit
    /// single or aromatic edge labels.
    /// </summary>
    // @author John May
    internal sealed class ImplicitToExplicit : AbstractFunction<Graph, Graph>
    {
        /// <summary>
        /// Transform all implicit to explicit bonds. The original graph is
        /// unmodified
        /// </summary>
        /// <param name="g">a chemical graph</param>
        /// <returns>new chemical graph but with all explicit bonds</returns>
        public override Graph Apply(Graph g)
        {

            Graph h = new Graph(g.Order);

            // copy atom/topology information
            for (int u = 0; u < g.Order; u++)
            {
                h.AddAtom(g.GetAtom(u));
                h.AddTopology(g.TopologyOf(u));
            }

            // apply edges
            for (int u = 0; u < g.Order; u++)
            {
                foreach (var e in g.GetEdges(u))
                {
                    if (e.Other(u) > u)
                        h.AddEdge(ToExplicitEdge(g, e));
                }
            }

            return h;
        }

        /// <summary>
        /// Given a chemical graph and an edge in that graph, return the explicit
        /// form of that edge. Neither the graph or the edge is modified, if the edge
        /// is already explicit then '<paramref name="e"/>' is returned.
        /// </summary>
        /// <param name="g">chemical graph</param>
        /// <param name="e">an edge of g</param>
        /// <returns>the edge with specified explicit bond type</returns>
        public static Edge ToExplicitEdge(Graph g, Edge e)
        {
            int u = e.Either(), v = e.Other(u);
            if (e.Bond == Bond.Implicit)
            {
                return new Edge(u, v,
                                Type(g.GetAtom(u),
                                     g.GetAtom(v)));
            }
            return e;
        }

        /// <summary>
        /// Given two atoms which are implicitly connected determine the explicit
        /// bond type. The type is 'aromatic' if both atoms are aromatic, if either
        /// or both atoms are non-aromatic then the bond type is 'single'.
        /// </summary>
        /// <param name="u">an atom</param>
        /// <param name="v">another atom </param>(connected to u)
        /// <returns>the bond type</returns>
        public static Bond Type(IAtom u, IAtom v)
        {
            return u.IsAromatic() && v.IsAromatic() ? Bond.Aromatic : Bond.Single;
        }
    }
}
