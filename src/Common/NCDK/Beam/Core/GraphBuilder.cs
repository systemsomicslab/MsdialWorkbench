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
using System.Linq;

namespace NCDK.Beam
{
    /// <summary>
    /// Provides the ability to incrementally build up a chemical graph from atoms
    /// and their connections.
    /// </summary>
    /// <example><code>
    /// Graph g = GraphBuilder.Create(3)
    ///     .Add(Carbon, 3)
    ///     .Add(AtomBuilder.Aliphatic(Carbon)
    ///     .NumOfHydrogens(2)
    ///     .Build())
    ///     .Add(Oxygen, 1)
    ///     .Add(0, 1)
    ///     .Add(1, 2)
    ///     .Add(2, 3)
    ///     .Build();
    /// </code></example>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    sealed class GraphBuilder
    {
        /// <summary>Current we just use the non-public methods of the actual graph object.</summary>
        private readonly Graph g;

        private readonly IList<GeometricBuilder> builders = new List<GeometricBuilder>(2);

        private int[] valence;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="nAtoms">expected number of atoms</param>
        private GraphBuilder(int nAtoms)
        {
            this.g = new Graph(nAtoms);
            this.valence = new int[nAtoms];
        }

        public static GraphBuilder Create(int n)
        {
            return new GraphBuilder(n);
        }

        /// <summary>
        /// Add an aliphatic element with the specified number of carbons.
        /// </summary>
        /// <param name="e">element</param>
        /// <param name="hCount">number of hydrogens</param>
        /// <returns>graph builder for adding more atoms/connections</returns>
        public GraphBuilder Add(Element e, int hCount)
        {
            return Add(AtomBuilder.Aliphatic(e)
                                  .NumOfHydrogens(hCount)
                                  .Build());
        }

        /// <summary>
        /// Add an atom to the graph.
        /// </summary>
        /// <param name="a">the atom to add</param>
        /// <returns>graph builder for adding more atoms/connections</returns>
        public GraphBuilder Add(IAtom a)
        {
            if (g.Order >= valence.Length)
                valence = Arrays.CopyOf(valence, valence.Length * 2);
            g.AddAtom(a);
            return this;
        }

        /// <summary>
        /// Add an edge to the graph.
        /// </summary>
        /// <param name="e">the edge to add</param>
        /// <returns>graph builder for adding more atoms/connections</returns>
        public GraphBuilder Add(Edge e)
        {
            Bond b = e.Bond;
            int u = e.Either();
            int v = e.Other(u);
            if (b == Bond.Single && (!g.GetAtom(u).IsAromatic() || !g.GetAtom(v).IsAromatic()))
                e.SetBond(Bond.Implicit);
            else if (b == Bond.Aromatic && g.GetAtom(u).IsAromatic() && g.GetAtom(v).IsAromatic())
                e.SetBond(Bond.Implicit);
            g.AddEdge(e);
            valence[u] += b.Order;
            valence[v] += b.Order;
            return this;
        }

        /// <summary>
        /// Connect the vertices <paramref name="u"/> and <paramref name="v"/> with an <see cref="Bond.Implicit"/> bond label.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">another vertex</param>
        /// <returns>graph builder for adding more atoms/connections</returns>
        public GraphBuilder Add(int u, int v)
        {
            Add(u, v, Bond.Implicit);
            return this;
        }

        /// <summary>
        /// Connect the vertices <paramref name="u"/> and <paramref name="v"/> with the specified bond label.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">another vertex</param>
        /// <param name="b">bond</param>
        /// <returns>graph builder for adding more atoms/connections</returns>
        public GraphBuilder Add(int u, int v, Bond b)
        {
            Add(b.CreateEdge(u, v));
            return this;
        }

        /// <summary>
        /// Connect the vertices <paramref name="u"/> and <paramref name="v"/> with a single bond.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">another vertex</param>
        /// <returns>graph builder for adding more atoms/connections</returns>
        public GraphBuilder ConnectWithSingleBond(int u, int v)
        {
            if (g.GetAtom(u).IsAromatic() && g.GetAtom(v).IsAromatic())
                return Add(u, v, Bond.Single);
            return Add(u, v, Bond.Implicit);
        }

        /// <summary>
        /// Connect the vertices <paramref name="u"/> and <paramref name="v"/> with an aromatic bond.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">another vertex</param>
        /// <returns>graph builder for adding more atoms/connections</returns>
        public GraphBuilder IsAromaticBond(int u, int v)
        {
            if (g.GetAtom(u).IsAromatic() && g.GetAtom(v).IsAromatic())
                return Add(u, v, Bond.Implicit);
            return Add(u, v, Bond.Aromatic);
        }

        /// <summary>
        /// Connect the vertices <paramref name="u"/> and <paramref name="v"/> with a double bond.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">another vertex</param>
        /// <returns>graph builder for adding more atoms/connections</returns>
        public GraphBuilder ConnectWithDoubleBond(int u, int v)
        {
            return Add(u, v, Bond.Double);
        }

        /// <summary>
        /// Start building a tetrahedral configuration.
        /// </summary>
        /// <param name="u">the central atom</param>
        /// <returns>a <see cref="TetrahedralBuilder"/> to create the stereo-configuration from </returns>
        public TetrahedralBuilder CreateTetrahedral(int u)
        {
            return new TetrahedralBuilder(this, u);
        }

        /// <summary>Start building the geometric configuration of the double bond '<paramref name="u"/>' / '<paramref name="v"/>'.</summary>
        public GeometricBuilder Geometric(int u, int v)
        {
            var builder = new GeometricBuilder(this, u, v) { Extended = false };
            return builder;
        }

        /// <summary>
        /// Start building a extended tetrahedral configuration.
        /// </summary>
        /// <param name="u">the central atom</param>
        /// <returns>a <see cref="ExtendedTetrahedralBuilder"/> to create the stereo-configuration from</returns>
        ///         
        public ExtendedTetrahedralBuilder CreateExtendedTetrahedral(int u)
        {
            return new ExtendedTetrahedralBuilder(this, u);
        }

        /// <summary>
        /// Start building the extended geometric configuration of a set of cumulated
        /// double bonds between <paramref name="u"/> and <paramref name="v"/>.
        /// </summary>
        /// <param name="u"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public GeometricBuilder CreateExtendedGeometric(int u, int v)
        {
            var builder = new GeometricBuilder(this, u, v) { Extended = true };
            return builder;
        }

        /// <summary>
        /// (internal) Add a topology to the chemical graph. The topologies should be
        /// created using one of the configuration builders (e.g. <see cref="TetrahedralBuilder"/>).
        /// </summary>
        /// <param name="u"></param>
        /// <param name="t">the topology to add</param>
        private void AddTopology(int u, Topology t)
        {
            g.AddTopology(t);
            if (t != Topology.Unknown)
            {
                g.AddFlags(Graph.HAS_ATM_STRO);
                if (t.Configuration.Type == Configuration.ConfigurationType.ExtendedTetrahedral)
                    g.AddFlags(Graph.HAS_EXT_STRO);
            }
        }

        private void AssignLeftOverFlags()
        {
            for (int v = 0; v < g.Order; v++)
            {
                if (g.GetAtom(v).IsAromatic())
                    g.AddFlags(Graph.HAS_AROM);
            }
        }

        private Edge FindDoubleBond(Graph g, int i)
        {
            Edge res = null;
            foreach (var e in g.GetEdges(i))
            {
                if (e.Bond != Bond.Double)
                    continue;
                if (res != null)
                    return null;
                res = e;
            }
            return res;
        }

        private Edge FindBondToLabel(Graph g, int i)
        {
            Edge res = null;
            foreach (var e in g.GetEdges(i))
            {
                if (e.Bond.Order != 1)
                    continue;
                if (res == null)
                    res = e;
                else if (e.Bond.IsDirectional && !res.Bond.IsDirectional)
                    res = e;
            }
            return res;
        }
        private void SetDirection(Edge e, int u, Bond b)
        {
            if (e.Either() == u)
                e.SetBond(b);
            else
                e.SetBond(b.Inverse());
        }

        private void AssignDirectionalLabels()
        {
            if (!builders.Any())
                return;

            // handle extended geometric configurations first

            var buildersToRemove = new List<GeometricBuilder>();
            foreach (var builder in builders)
            {
                if (!builder.Extended)
                    continue;
                buildersToRemove.Add(builder);
                Edge e = FindDoubleBond(g, builder.u);
                Edge f = FindDoubleBond(g, builder.v);
                if (e == null || f == null)
                    continue;
                Edge eRef = g.CreateEdge(builder.u, builder.X);
                Edge fRef = g.CreateEdge(builder.v, builder.Y);
                Edge eLab = FindBondToLabel(g, builder.u);
                Edge fLab = FindBondToLabel(g, builder.v);
                // adjust for reference
                Configuration.ConfigurationDoubleBond config = builder.c;
                if ((eLab == eRef) != (fRef == fLab))
                {
                    if (config == Configuration.ConfigurationDoubleBond.Together)
                        config = Configuration.ConfigurationDoubleBond.Opposite;
                    else if (config == Configuration.ConfigurationDoubleBond.Opposite)
                        config = Configuration.ConfigurationDoubleBond.Together;
                }
                if (eLab.Bond.IsDirectional)
                {
                    if (fLab.Bond.IsDirectional)
                    {
                        // can't do anything, may be incorrect
                    }
                    else
                    {
                        if (config == Configuration.ConfigurationDoubleBond.Together)
                            SetDirection(fLab, builder.v, eLab.GetBond(builder.u));
                        else if (config == Configuration.ConfigurationDoubleBond.Opposite)
                            SetDirection(fLab, builder.v, eLab.GetBond(builder.u));
                    }
                }
                else
                {
                    if (fLab.Bond.IsDirectional)
                    {
                        if (config == Configuration.ConfigurationDoubleBond.Together)
                            SetDirection(eLab, builder.v, fLab.GetBond(builder.u));
                        else if (config == Configuration.ConfigurationDoubleBond.Opposite)
                            SetDirection(eLab, builder.v, fLab.GetBond(builder.u));
                    }
                    else
                    {
                        SetDirection(eLab, builder.u, Bond.Down);
                        if (config == Configuration.ConfigurationDoubleBond.Together)
                            SetDirection(fLab, builder.v, Bond.Down);
                        else if (config == Configuration.ConfigurationDoubleBond.Opposite)
                            SetDirection(fLab, builder.v, Bond.Up);
                    }
                }
            }
            foreach (var builder in buildersToRemove)
                builders.Remove(builder);

            if (!builders.Any())
                return;

            // store the vertices which are adjacent to pi bonds with a config
            BitArray pibonded = new BitArray(g.Order);
            BitArray unspecified = new BitArray(g.Order);
            var unspecEdges = new HashSet<Edge>();

            // clear existing directional labels, if build is called multiple times
            // this can cause problems
            if (g.GetFlags(Graph.HAS_BND_STRO) != 0)
            {
                foreach (Edge edge in g.Edges)
                {
                    if (edge.Bond.IsDirectional)
                    {
                        edge.SetBond(Bond.Implicit);
                    }
                }
            }

            foreach (Edge e in g.Edges)
            {
                int u = e.Either();
                int v = e.Other(u);
                if (e.Bond.Order == 2 && g.Degree(u) >= 2 && g.Degree(v) >= 2)
                {
                    unspecified.Set(u, true);
                    unspecified.Set(v, true);
                    pibonded.Set(u, true);
                    pibonded.Set(v, true);
                    unspecEdges.Add(e);
                }
            }

            foreach (var builder in builders)
            {
                g.AddFlags(Graph.HAS_BND_STRO);

                // unspecified only used for getting not setting configuration
                if (builder.c == Configuration.ConfigurationDoubleBond.Unspecified)
                    continue;
                CheckGeometricBuilder(builder); // check required vertices are adjacent

                int u = builder.u, v = builder.v, x = builder.X, y = builder.Y;

                if (x == y) continue;

                unspecEdges.Remove(g.CreateEdge(u, v));
                unspecified.Set(u, false);
                unspecified.Set(v, false);

                Fix(g, u, v, pibonded);
                Fix(g, v, u, pibonded);

                Bond first = FirstDirectionalLabel(u, x, pibonded);
                Bond second = builder.c == Configuration.ConfigurationDoubleBond.Together ? first : first.Inverse();

                // check if the second label would cause a conflict
                if (CheckDirectionalAssignment(second, v, y, pibonded))
                {
                    // okay to assign the labels as they are
                    g.Replace(g.CreateEdge(u, x), new Edge(u, x, first));
                    g.Replace(g.CreateEdge(v, y), new Edge(v, y, second));
                }
                // there will be a conflict - check if we invert the first one...
                else if (CheckDirectionalAssignment(first.Inverse(), u, x, pibonded))
                {
                    g.Replace(g.CreateEdge(u, x), new Edge(u, x, (first = first.Inverse())));
                    g.Replace(g.CreateEdge(v, y), new Edge(v, y, (second = second.Inverse())));
                }
                else
                {
                    BitArray visited = new BitArray(g.Order);
                    visited.Set(v, true);
                    InvertExistingDirectionalLabels(pibonded, visited, v, u);
                    if (!CheckDirectionalAssignment(first, u, x, pibonded) ||
                            !CheckDirectionalAssignment(second, v, y, pibonded))
                        throw new ArgumentException("cannot assign geometric configuration");
                    g.Replace(g.CreateEdge(u, x), new Edge(u, x, first));
                    g.Replace(g.CreateEdge(v, y), new Edge(v, y, second));
                }

                // propagate bond directions to other adjacent bonds
                foreach (var e in g.GetEdges(u))
                    if (e.Bond != Bond.Double && !e.Bond.IsDirectional)
                    {
                        e.SetBond(e.Either() == u ? first.Inverse() : first);
                    }
                foreach (var e in g.GetEdges(v))
                    if (e.Bond != Bond.Double && !e.Bond.IsDirectional)
                    {
                        e.SetBond(e.Either() == v ? second.Inverse() : second);
                    }
            }

            // unspecified pibonds should "not" have a configuration, if they
            // do we try to eliminate it
            foreach (Edge unspecEdge in unspecEdges)
            {
                int u = unspecEdge.Either();
                int v = unspecEdge.Other(u);
                // no problem if one side isn't defined
                if (!HasDirectional(g, u) || !HasDirectional(g, v))
                    continue;
                foreach (Edge e in g.GetEdges(u))
                    if (IsRedundantDirectionalEdge(g, e, unspecified))
                        e.SetBond(Bond.Implicit);
                if (!HasDirectional(g, u))
                    continue;
                foreach (Edge e in g.GetEdges(v))
                    if (IsRedundantDirectionalEdge(g, e, unspecified))
                        e.SetBond(Bond.Implicit);
                // if (hasDirectional(g, v))
                // could generate warning!
            }
        }

        private static bool HasDirectional(Graph g, int v)
        {
            foreach (Edge e in g.GetEdges(v))
            {
                if (e.Bond.IsDirectional)
                    return true;
            }
            return false;
        }

        private static bool IsRedundantDirectionalEdge(Graph g, Edge edge, BitArray unspecified)
        {
            if (!edge.Bond.IsDirectional)
                return false;
            int u = edge.Either();
            int v = edge.Other(u);
            if (!unspecified[u])
            {
                foreach (Edge f in g.GetEdges(u))
                    if (f.Bond.IsDirectional && edge != f)
                        return true;
            }
            else if (!unspecified[v])
            {
                foreach (Edge f in g.GetEdges(v))
                    if (f.Bond.IsDirectional && edge != f)
                        return true;
            }
            return false;
        }

        private void Fix(Graph g, int u, int p, BitArray adjToDb)
        {
            Bond other = null;

            // original code is foreach (var e in g.GetEdges(u))
            // g.edges can be modified in InvertExistingDirectionalLabels.
            var gEdges = g.GetEdges(u);
            for (int i = 0; i < gEdges.Count; i++)
            {
                var e = gEdges[i];

                Bond bond = e.GetBond(u);
                if (bond.IsDirectional)
                {
                    if (other != null && other == bond)
                    {
                        BitArray visited = new BitArray(g.Order);
                        visited.Set(p, true);
                        visited.Set(e.Other(u), true);
                        InvertExistingDirectionalLabels(adjToDb, visited, u, p);
                    }
                    other = bond;
                }
            }
        }

        private void InvertExistingDirectionalLabels(BitArray adjToDb,
                                                     BitArray visited,
                                                     int u,
                                                     int p)
        {
            visited.Set(u, true);

            // original code is foreach (var e in g.GetEdges(u))
            // edges is modified in g.Replace.
            var gEdges = g.GetEdges(u);
            for (var i = 0; i < gEdges.Count; i++)
            {
                var e = gEdges[i];

                int v = e.Other(u);
                if (!visited[v] && p != v)
                {
                    g.Replace(e, e.Inverse());
                    if (adjToDb[v])
                        InvertExistingDirectionalLabels(adjToDb, visited, v, u);
                }
            }
        }

        private Bond FirstDirectionalLabel(int u, int x, BitArray adjToDb)
        {
            Edge e = g.CreateEdge(u, x);
            Bond b = e.GetBond(u);

            // the edge is next to another double bond configuration, we
            // need to consider its assignment
            if (adjToDb[x] && g.Degree(x) > 2)
            {
                foreach (var f in g.GetEdges(x))
                {
                    if (f.Other(x) != u && f.Bond != Bond.Double && f.Bond.IsDirectional)
                        return f.GetBond(x);
                }
            }
            // consider other labels on this double-bond
            if (g.Degree(u) > 2)
            {
                foreach (var f in g.GetEdges(u))
                {
                    if (f.Other(u) != x && f.Bond != Bond.Double && f.Bond.IsDirectional)
                        return f.GetBond(u).Inverse();
                }
            }
            return b.IsDirectional ? b : Bond.Down;
        }

        private bool CheckDirectionalAssignment(Bond b, int u, int v, BitArray adjToDb)
        {
            foreach (var e in g.GetEdges(u))
            {
                int x = e.Other(u);
                Bond existing = e.GetBond(u);
                if (existing.IsDirectional)
                {
                    // if there is already a directional label on a different edge
                    // and they are equal this produces a conflict
                    if (x != v)
                    {
                        if (existing == b)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (existing != b)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        // safety checks
        private void CheckGeometricBuilder(GeometricBuilder builder)
        {
            if (!g.Adjacent(builder.u, builder.X)
                    || !g.Adjacent(builder.u, builder.v)
                    || !g.Adjacent(builder.v, builder.Y))
                throw new ArgumentException("cannot assign directional labels, vertices were not adjacent" +
                                                           "where not adjacent - expected topology of" +
                                                           " 'x-u=v-y' where x=" + builder.X
                                                           + " u=" + builder.u
                                                           + " v=" + builder.v
                                                           + " y=" + builder.Y);
            Edge db = g.CreateEdge(builder.u, builder.v);
            if (db.Bond != Bond.Double)
                throw new ArgumentException("cannot assign double bond configuration to non-double bond");
        }

        private void Suppress()
        {
            for (int v = 0; v < g.Order; v++)
            {
                if (g.TopologyOf(v).Type == Configuration.ConfigurationType.None)
                {
                    IAtom atom = g.GetAtom(v);
                    if (Suppressible(atom, valence[v]))
                    {
                        g.SetAtom(v, ToSubset(atom));
                    }
                }
            }
        }

        private static IAtom ToSubset(IAtom a)
        {
            if (a.IsAromatic())
                return AtomImpl.AromaticSubset.OfElement(a.Element);
            else
                return AtomImpl.AliphaticSubset.OfElement(a.Element);
        }

        private static bool Suppressible(IAtom a, int v)
        {
            if (!a.Subset
                    && a.Element.IsOrganic()
                    && a.Isotope < 0
                    && a.Charge == 0
                    && a.AtomClass == 0)
            {
                int h = a.NumOfHydrogens;
                if (a.IsAromatic())
                    return h == a.Element.NumOfAromaticImplicitHydrogens(1 + v);
                else
                    return h == a.Element.NumOfImplicitHydrogens(v);
            }
            return false;
        }

        /// <summary>
        /// Finalise and build the chemical graph.
        /// </summary>
        /// <returns>chemical graph instance</returns>
        public Graph Build()
        {
            Suppress();
            AssignDirectionalLabels();
            return g;
        }

        // @author John May
        public sealed class TetrahedralBuilder
        {

            /// <summary>
            /// Reference to the graph builder we came from - allows us to add the
            /// topology once the configuration as been built.
            /// </summary>
            readonly GraphBuilder gb;

            /// <summary>Central vertex.</summary>
            readonly int u;

            /// <summary>The vertex we are looking from.</summary>
            int v;

            /// <summary>The other neighbors</summary>
            int[] vs;

            /// <summary>The configuration of the other neighbors</summary>
            Configuration config;

            /// <summary>
            /// (internal) - constructor for starting to configure a tetrahedral centre.
            /// </summary>
            /// <param name="gb">the graph builder (where we came from)</param>
            /// <param name="u"> the vertex to</param>
            internal TetrahedralBuilder(GraphBuilder gb,
                                       int u)
            {
                this.gb = gb;
                this.u = u;
            }

            /// <summary>
            /// Indicate from which vertex the tetrahedral is being 'looked-at'.
            /// </summary>
            /// <param name="v">the vertex from which we are looking from</param>.
            /// <returns>tetrahedral builder for further configuration</returns>
            public TetrahedralBuilder LookingFrom(int v)
            {
                this.v = v;
                return this;
            }

            /// <summary>
            /// Indicate the other neighbors of tetrahedral (excluding the vertex we
            /// are looking from). There should be exactly 3 neighbors.
            /// </summary>
            /// <param name="vs">the neighbors</param>
            /// <returns>tetrahedral builder for further configuration</returns>
            /// <exception cref="ArgumentException">when there was not exactly 3 neighbors</exception>
            public TetrahedralBuilder Neighbors(int[] vs)
            {
                if (vs.Length != 3)
                    throw new ArgumentException("3 vertex required for tetrahedral centre");
                this.vs = vs;
                return this;
            }

            /// <summary>
            /// Indicate the other neighbors of tetrahedral (excluding the vertex we
            /// are looking from).
            /// </summary>
            /// <param name="u">a neighbor</param>
            /// <param name="v">another neighbor</param>
            /// <param name="w">another neighbor</param>
            /// <returns>tetrahedral builder for further configuration</returns>
            public TetrahedralBuilder Neighbors(int u, int v, int w)
            {
                return Neighbors(new int[] { u, v, w });
            }

            /// <summary>
            /// Convenience method to specify the parity as odd (-1) for
            /// anti-clockwise or even (+1) for clockwise. The parity is translated
            /// in to 'TH1' and 'TH2' stereo specification.
            /// </summary>
            /// <param name="p">parity value</param>
            /// <returns>tetrahedral builder for further configuration</returns>
            public TetrahedralBuilder Parity(int p)
            {
                if (p < 0)
                    return Winding(Configuration.TH1);
                if (p > 0)
                    return Winding(Configuration.TH2);
                throw new ArgumentException("parity must be < 0 or > 0");
            }

            /// <summary>
            /// Specify the winding of the <see cref="Neighbors(int, int, int)"/>.
            /// </summary>
            /// <param name="c">configuration <see cref="Configuration.TH1"/>, <see cref="Configuration.TH2"/>, 
            /// <see cref="Configuration.AntiClockwise"/> or <see cref="Configuration.Clockwise"/></param>
            /// <returns>tetrahedral builder for further configuration</returns>
            public TetrahedralBuilder Winding(Configuration c)
            {
                this.config = c;
                return this;
            }

            /// <summary>
            /// Finish configuring the tetrahedral centre and add it to the graph.
            /// </summary>
            /// <returns>the graph-builder to add more atoms/bonds or stereo elements</returns>
            /// <exception cref="InvalidOperationException">configuration was missing</exception>
            public GraphBuilder Build()
            {
                if (config == null)
                    throw new InvalidOperationException("no configuration defined");
                if (vs == null)
                    throw new InvalidOperationException("no neighbors defined");
                Topology t = Topology.CreateTetrahedral(u,
                                                  new int[]{
                                                          v,
                                                          vs[0], vs[1], vs[2]
                                                  },
                                                  config);
                gb.AddTopology(u, t);
                return gb;
            }
        }

        // @author John May
        public sealed class ExtendedTetrahedralBuilder
        {
            /// <summary>
            /// Reference to the graph builder we came from - allows us to add the
            /// topology once the configuration as been built.
            /// </summary>
            readonly GraphBuilder gb;

            /// <summary>Central vertex.</summary>
            readonly int u;

            /// <summary>The vertex we are looking from.</summary>
            int v;

            /// <summary>The other neighbors</summary>
            int[] vs;

            /// <summary>The configuration of the other neighbors</summary>
            Configuration config;

            /// <summary>
            /// (internal) - constructor for starting to configure a tetrahedral centre.
            /// </summary>
            /// <param name="gb">the graph builder </param>(where we came from)
            /// <param name="u"> the vertex to</param>
            internal ExtendedTetrahedralBuilder(GraphBuilder gb,
                                       int u)
            {
                this.gb = gb;
                this.u = u;
            }

            /// <summary>
            /// Indicate from which vertex the tetrahedral is being 'looked-at'.
            /// </summary>
            /// <param name="v">the vertex from which we are looking from</param>.
            /// <returns>tetrahedral builder for further configuration</returns>
            public ExtendedTetrahedralBuilder LookingFrom(int v)
            {
                this.v = v;
                return this;
            }

            /// <summary>
            /// Indicate the other neighbors of tetrahedral (excluding the vertex we
            /// are looking from). There should be exactly 3 neighbors.
            /// </summary>
            /// <param name="vs">the neighbors</param>
            /// <returns>tetrahedral builder for further configuration</returns>
            /// <exception cref="ArgumentException">when there was not exactly 3 neighbors</exception>
            public ExtendedTetrahedralBuilder Neighbors(int[] vs)
            {
                if (vs.Length != 3)
                    throw new ArgumentException("3 vertex required for tetrahedral centre");
                this.vs = vs;
                return this;
            }

            /// <summary>
            /// Indicate the other neighbors of tetrahedral (excluding the vertex we
            /// are looking from).
            /// </summary>
            /// <param name="u">a neighbor</param>
            /// <param name="v">another neighbor</param>
            /// <param name="w">another neighbor</param>
            /// <returns>tetrahedral builder for further configuration</returns>
            public ExtendedTetrahedralBuilder Neighbors(int u, int v, int w)
            {
                return Neighbors(new int[] { u, v, w });
            }

            /// <summary>
            /// Convenience method to specify the parity as odd (-1) for
            /// anti-clockwise or even (+1) for clockwise. The parity is translated
            /// in to 'TH1' and 'TH2' stereo specification.
            /// </summary>
            /// <param name="p">parity value</param>
            /// <returns>tetrahedral builder for further configuration</returns>
            public ExtendedTetrahedralBuilder Parity(int p)
            {
                if (p < 0)
                    return Winding(Configuration.AL1);
                if (p > 0)
                    return Winding(Configuration.AL2);
                throw new ArgumentException("parity must be < 0 or > 0");
            }

            /// <summary>
            /// Specify the winding of the <see cref="Neighbors(int, int, int)"/>.
            /// </summary>
            /// <param name="c">configuration <see cref="Configuration.TH1"/>, <see cref="Configuration.TH2"/>, 
            /// <see cref="Configuration.AntiClockwise"/> or <see cref="Configuration.Clockwise"/></param>
            /// <returns>tetrahedral builder for further configuration</returns>
            public ExtendedTetrahedralBuilder Winding(Configuration c)
            {
                this.config = c;
                return this;
            }

            /// <summary>
            /// Finish configuring the tetrahedral centre and add it to the graph.
            /// </summary>
            /// <returns>the graph-builder to add more atoms/bonds or stereo elements</returns>
            /// <exception cref="InvalidOperationException">configuration was missing</exception>
            public GraphBuilder Build()
            {
                if (config == null)
                    throw new InvalidOperationException("no configuration defined");
                if (vs == null)
                    throw new InvalidOperationException("no neighbors defined");
                if (gb.g.Degree(u) != 2)
                    throw new InvalidOperationException("extended tetrahedral atom needs exactly 2 neighbors");
                Topology t = Topology.CreateExtendedTetrahedral(u,
                                                          new int[]{
                                                                  v,
                                                                  vs[0], vs[1], vs[2]
                                                          },
                                                          config);
                gb.AddTopology(u, t);
                return gb;
            }
        }

        /// <summary>Fluent assembly of a double-bond configuration.</summary>
        public sealed class GeometricBuilder
        {
            /// <summary>
            /// Reference to the graph builder we came from - allows us to add the
            /// double bond once the configuration as been built.
            /// </summary>
            readonly GraphBuilder gb;
            internal readonly int u, v;

            internal int X { get; set; }
            internal int Y { get; set; }
            internal bool Extended { get; set; }
            internal Configuration.ConfigurationDoubleBond c;

            public GeometricBuilder(GraphBuilder gb, int u, int v)
            {
                this.gb = gb;
                this.u = u;
                this.v = v;
            }

            public GraphBuilder Together(int x, int y)
            {
                return Configure(x, y, Configuration.ConfigurationDoubleBond.Together);
            }

            public GraphBuilder Opposite(int x, int y)
            {
                return Configure(x, y, Configuration.ConfigurationDoubleBond.Opposite);
            }

            public GraphBuilder Configure(int x, int y, Configuration.ConfigurationDoubleBond c)
            {
                this.X = x;
                this.Y = y;
                this.c = c;
                gb.builders.Add(this);
                return gb;
            }

            public override string ToString()
            {
                return X + "/" + u + "=" + v + (c == Configuration.ConfigurationDoubleBond.Together ? "\\" : "/") + Y;
            }
        }
    }
}
