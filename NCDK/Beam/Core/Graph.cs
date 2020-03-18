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
using static NCDK.Beam.Element;

namespace NCDK.Beam
{
    /// <summary>
    /// Defines a labelled graph with atoms as vertex labels and bonds as edge
    /// labels. Topological information around atoms can also be stored.
    /// </summary>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    sealed class Graph
    {
        /// <summary>
        /// Indicate the graph has one or aromatic atoms.
        /// </summary>
        public const int HAS_AROM = 0x1;

        public const int HAS_ATM_STRO = 0x2;

        // extended stereo (across multiple atoms) e.g. @AL1/@Al2
        public const int HAS_EXT_STRO = 0x4;

        public const int HAS_BND_STRO = 0x8;

        public const int HAS_STRO = HAS_ATM_STRO | HAS_EXT_STRO | HAS_BND_STRO;

        /// <summary> The vertex labels, atoms.</summary>
        private IAtom[] atoms;

        private int[] degrees;

        private int[] valences;

        /// <summary> Incidence list storage of edges with attached bond labels. .</summary>
        private Edge[][] edges;

        /// <summary> Topologies indexed by the atom which they describe.</summary>
        private Topology[] topologies;

        /// <summary> Vertex and edge counts.</summary>
        private int order, size;

        /// <summary> Molecule flags.</summary>
        private int flags = 0;

        /// <summary> Molecule title.</summary>
        public string Title { get; set; }

        /// <summary>
        /// Create a new chemical graph with expected size.
        /// </summary>
        /// <param name="expSize">expected size</param>
        internal Graph(int expSize)
        {
            this.order = 0;
            this.size = 0;
            this.edges = new Edge[expSize][];
            for (int i = 0; i < expSize; i++)
                edges[i] = new Edge[4];
            this.atoms = new IAtom[expSize];
            this.degrees = new int[expSize];
            this.valences = new int[expSize];
            this.topologies = new Topology[expSize];
        }

        /// <summary>
        /// Copy constructor.
        /// </summary>
        /// <param name="org">original graph</param>
        Graph(Graph org)
        {
            this.order = org.order;
            this.size = org.size;
            this.flags = org.flags;
            this.atoms = Arrays.CopyOf(org.atoms, order);
            this.valences = Arrays.CopyOf(org.valences, order);
            this.degrees = new int[order];
            this.edges = new Edge[order][];
            this.topologies = Arrays.CopyOf(org.topologies, org.topologies.Length);

            for (int u = 0; u < order; u++)
            {
                int deg = org.degrees[u];
                this.edges[u] = new Edge[deg];
                for (int j = 0; j < deg; ++j)
                {
                    Edge e = org.edges[u][j];
                    int v = e.Other(u);
                    // important - we have made use edges are allocated
                    if (u > v)
                    {
                        Edge f = new Edge(e);
                        edges[u][degrees[u]++] = f;
                        edges[v][degrees[v]++] = f;
                    }
                }
            }
        }

        /// <summary>
        /// (internal) - set the atom label at position 'i'.
        /// </summary>
        /// <param name="i">index</param>
        /// <param name="a">atom</param>
        internal void SetAtom(int i, IAtom a)
        {
            atoms[i] = a;
        }

        /// <summary> Resize the graph if we are at maximum capacity.</summary>
        private void EnsureCapacity()
        {
            if (Order >= atoms.Length)
            {
                atoms = Arrays.CopyOf(atoms, order * 2);
                valences = Arrays.CopyOf(valences, order * 2);
                degrees = Arrays.CopyOf(degrees, order * 2);
                edges = Arrays.CopyOf(edges, order * 2);
                topologies = Arrays.CopyOf(topologies, order * 2);
                for (int i = order; i < edges.Length; i++)
                    edges[i] = new Edge[4];
            }
        }

        /// <summary>
        /// Add an atom to the graph and return the index to which the atom was added.
        /// </summary>
        /// <param name="a">add an atom</param>
        /// <returns>index of the atom in the graph (vertex)</returns>
        internal int AddAtom(IAtom a)
        {
            EnsureCapacity();
            atoms[order++] = a;
            return order - 1;
        }

        /// <summary>
        /// Access the atom at the specified index.
        /// </summary>
        /// <param name="i">index of the atom to access</param>
        /// <returns>the atom at that index</returns>
        /// <exception cref="ArgumentException">no atom exists</exception>"
        public IAtom GetAtom(int i)
        {
            return atoms[i];
        }

        /// <summary>
        /// Add an labelled edge to the graph.
        /// </summary>
        /// <param name="e">new edge</param>
        public void AddEdge(Edge e)
        {
            int u = e.Either(), v = e.Other(u);
            EnsureEdgeCapacity(u);
            EnsureEdgeCapacity(v);
            edges[u][degrees[u]++] = e;
            edges[v][degrees[v]++] = e;
            int ord = e.Bond.Order;
            valences[u] += ord;
            valences[v] += ord;
            size++;
        }

        private void EnsureEdgeCapacity(int i)
        {
            if (degrees[i] == edges[i].Length)
                edges[i] = Arrays.CopyOf(edges[i], degrees[i] + 2);
        }

        /// <summary>
        /// Access the degree of vertex 'u'.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <returns>the degree of the specified vertex</returns>
        /// <exception cref="ArgumentOutOfRangeException">attempting to access the degree of an
        /// atom which does not exist</exception> 
        public int Degree(int u)
        {
            return degrees[u];
        }

        /// <summary>
        /// Access the bonded valence of vertex 'u'. This valence exclude any implicit hydrogen counts.
        /// </summary>
        /// <param name="u">a vertex index</param>
        /// <returns>the bonded valence of the specified vertex</returns>
        internal int BondedValence(int u)
        {
            return valences[u];
        }

        internal void UpdateBondedValence(int i, int x)
        {
            valences[i] += x;
        }

        /// <summary>
        /// Access the edges of which vertex '<paramref name="u"/>' is an endpoint.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <returns>edges incident to '<paramref name="u"/>'</returns>
        public IList<Edge> GetEdges(int u)
        {
            return new List<Edge>(Arrays.CopyOf(edges[u], degrees[u]));
        }

        /// <summary>
        /// Access the vertices adjacent to '<paramref name="u"/>' in <b>sorted</b> Order. This
        /// convenience method is provided to assist in configuring atom-based stereo
        /// using the <see cref="ConfigurationOf(int)"/> method. For general purpose
        /// access to the neighbors of a vertex the <see cref="GetEdges(int)"/> is
        /// preferred.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <returns>fixed-size array of vertices</returns>
        /// <seealso cref="ConfigurationOf(int)"/>
        public int[] Neighbors(int u)
        {
            IList<Edge> es = GetEdges(u);
            int[] vs = new int[es.Count];
            int deg = es.Count;
            for (int i = 0; i < deg; i++)
                vs[i] = es[i].Other(u);
            Array.Sort(vs);
            return vs;
        }

        /// <summary>
        /// Determine if the vertices '<paramref name="u"/>' and '<paramref name="v"/>' are adjacent and there is an edge
        /// which connects them.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">another vertex</param>
        /// <returns>whether they are adjacent</returns>
        public bool Adjacent(int u, int v)
        {
            int d = degrees[u];
            for (int j = 0; j < d; ++j)
            {
                Edge e = edges[u][j];
                if (e.Other(u) == v)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// The number of implied (or labelled) hydrogens for the vertex '<paramref name="u"/>'. Note
        /// the count does not include any bonded vertices which may also be
        /// hydrogen.
        /// </summary>
        /// <param name="u">the vertex to access the implicit h count for</param>.
        /// <returns>the number of implicit hydrogens</returns>
        public int ImplHCount(int u)
        {
            return GetAtom(u).GetNumberOfHydrogens(this, u);
        }

        /// <summary>
        /// Access the edge connecting two adjacent vertices.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">another vertex </param>(adjacent to u)
        /// <returns>the edge connected u and v</returns>
        /// <exception cref="ArgumentException">u and v are not adjacent</exception>
        public Edge CreateEdge(int u, int v)
        {
            int d = degrees[u];
            for (int j = 0; j < d; ++j)
            {
                Edge e = edges[u][j];
                if (e.Other(u) == v)
                    return e;
            }
            throw new ArgumentException(u + ", " + v + " are not adjacent");
        }

        public Edge EdgeAt(int u, int j)
        {
            return edges[u][j];
        }

        /// <summary>
        /// Replace an edge in the graph.
        /// </summary>
        /// <param name="org">the original edge</param>
        /// <param name="rep">the replacement</param>
        internal void Replace(Edge org, Edge rep)
        {
            int u = org.Either();
            int v = org.Other(u);

            for (int i = 0; i < degrees[u]; i++)
            {
                if (edges[u][i] == org)
                {
                    edges[u][i] = rep;
                }
            }

            for (int i = 0; i < degrees[v]; i++)
            {
                if (edges[v][i] == org)
                {
                    edges[v][i] = rep;
                }
            }

            int ord = rep.Bond.Order - org.Bond.Order;
            valences[u] += ord;
            valences[v] += ord;
        }

        /// <summary>
        /// Add a topology description to the graph. The topology describes the
        /// configuration around a given atom.
        /// </summary>
        /// <param name="t">topology</param> 
        /* fixed returns tags are removed. */
        internal void AddTopology(Topology t)
        {
            if (t != null && t != Topology.Unknown)
                topologies[t.Atom] = t;
        }

        void ClearTopology(int v)
        {
            topologies[v] = null;
        }

        /// <summary>
        /// Access the topology of the vertex 'u'. If no topology is defined then
        /// <see cref="Topology.Unknown"/> is returned.
        /// </summary>
        /// <param name="u">a vertex to access the topology of</param>
        /// <returns>the topology of vertex 'u'</returns>
        public Topology TopologyOf(int u)
        {
            if (topologies[u] == null)
                return Topology.Unknown;
            return topologies[u];
        }

        /// <summary>
        /// Provides the stereo-configuration of the atom label at vertex 'u'. The
        /// configuration describes the relative-stereo as though the atoms were
        /// arranged by atom number. </summary>
        /// <remarks>
        /// <b>Further Explanation for Tetrahedral Centres</b>
        /// As an example the
        /// molecule O[C@]12CCCC[C@@]1(O)CCCC2 has two tetrahedral centres.
        /// <list type="number">
        /// <item>
        /// The first one is on vertex '1' and looking from vertex '0' the
        /// other neighbors [6, 11, 2] proceed anti-clockwise ('@') - note ring
        /// bonds. It is easy to see that if we use the natural Order of the molecule
        /// and Order the neighbor [2, 6, 11] the winding is still anti-clockwise and
        /// '<see cref="Configuration.TH1"/>' is returned.
        /// </item>
        /// <item>
        /// The second centre is on vertex '6' and looking
        /// from vertex '5' the Ordering proceeds as [1, 7, 8] with clockwise
        /// winding. When we arrange the atoms by their natural Order we will now be
        /// looking from vertex '1' as it is the lowest. The other neighbors then
        /// proceed in the Order [5, 7, 8]. Drawing out the configuration it's clear
        /// that we look from vertex '1' instead of '5' the winding is now
        /// anti-clockwise and the configuration is also '<see cref="Configuration.TH1"/>'.
        /// </item>
        /// </list>
        /// </remarks>
        /// <param name="u">a vertex in the graph</param>
        /// <returns>The configuration around</returns>
        public Configuration ConfigurationOf(int u)
        {
            Topology t = TopologyOf(u);

            if (t == Topology.Unknown)
                return t.Configuration;

            // identity permutation
            int[] p = new int[Order];
            for (int i = 0; i < Order; i++)
                p[i] = i;

            return t.OrderBy(p).Configuration;
        }

        /// <summary>
        /// The Order is the number vertices in the graph, |V|.
        /// </summary>
        public int Order => order;

        /// <summary>
        /// The size is the number edges in the graph, |E|.
        /// </summary>
        public int Size => size;

        /// <summary>
        /// Convenience method to create a graph from a provided SMILES string.
        /// </summary>
        /// <param name="smi">string containing SMILES line notation</param>.
        /// <returns>graph instance from the SMILES</returns>
        /// <exception cref="InvalidSmilesException">if there was a syntax error while parsing the SMILES.</exception>
        public static Graph FromSmiles(string smi)
        {
            if (smi == null)
                throw new ArgumentNullException(nameof(smi), "no SMILES provided");
            var parser = new Parser(CharBuffer.FromString(smi), false);
            foreach (var warn in parser.Warnings())
            {
                foreach (var line in warn.Split('\n'))
                    Console.Error.WriteLine($"SMILES Warning: {line}");
            }
            return parser.Molecule();
        }

        public static Graph Parse(string smi, bool strict, ISet<string> warnings)
        {
            if (smi == null)
                throw new ArgumentNullException("no SMILES provided");
            var parser = new Parser(CharBuffer.FromString(smi), strict);
            foreach (var warning in parser.Warnings())
                warnings.Add(warning);
            return parser.Molecule();
        }

        /// <summary>
        /// Convenience method to write a SMILES string for the current configuration
        /// of the molecule.
        /// </summary>
        /// <returns>the SMILES string for the molecule</returns>.
        /// <exception cref="System.IO.IOException">a SMILES string could not be generated</exception>
        public string ToSmiles()
        {
            return Generator.Generate(this);
        }

        /// <summary>
        /// Generate a SMILES for the Graph. The <paramref name="visitedAt"/> is filled with
        /// the output rank of each vertex in the graph. This allows one to know
        /// the atom index when the SMILES in read in.
        /// </summary>
        /// <param name="visitedAt">vector to be filled with the output Order</param>
        /// <returns>the SMILES string</returns>
        /// <exception cref="InvalidSmilesException">a SMILES string could not be generated</exception>"
        public string ToSmiles(int[] visitedAt)
        {
            return Generator.Generate(this, visitedAt);
        }

        /// <summary>
        /// Delocalise a kekulé graph representation to one with <i>aromatic</i>
        /// bonds. The original graph remains unchanged.
        /// TODO: more explanation
        /// </summary>
        /// <returns>aromatic representation</returns>
        public Graph IsAromatic()
        {
            // note Daylight use SSSR - should update and use that by default but
            // provide the AllCycles method
            try
            {
                return AllCycles.DaylightModel(this).AromaticForm();
            }
            catch (ArgumentException)
            {
                // too many cycles - use a simpler model which only allows rings of
                // size 6 (catches fullerenes)
                return AllCycles.DaylightModel(this, 6).AromaticForm();
            }
        }

        /// <summary>
        /// Resonate bond assignments in conjugate rings such that two SMILES with
        /// the same Ordering have the same kekulé assignment.
        /// </summary>
        /// <returns>(self) - the graph is mutated</returns>
        public Graph Resonate()
        {
            return Localise.Resonate(this);
        }

        /// <summary>
        /// Localise delocalized (aromatic) bonds in this molecule producing the Kekulé form. 
        /// </summary>
        /// <remarks>
        /// The original graph is not modified.
        /// <code>
        /// Graph furan        = Graph.FromSmiles("o1cccc1");
        /// </code>
        /// If the graph could not be converted to a kekulé representation then a
        /// checked exception is thrown. Graphs cannot be converted if their
        /// structures are erroneous and there is no valid way to assign the
        /// delocalised electrons. 
        /// <para>
        /// Some reasons are shown below.
        /// <list type="bullet">
        /// <item>
        /// <term>n1cncc1</term>
        /// <description>pyrole (incorrect) could be either C1C=NC=N1 or N1C=CN=C1</description>
        /// </item>
        /// <item>
        /// <term>[Hg+2][c-]1ccccc1</term>
        /// <description>Mercury(2+) ion benzenide (incorrect)</description>
        /// </item>
        /// <item>
        /// <term>[Hg+2].[c-]1ccccc1</term>
        /// <description>Mercury(2+) ion benzenide (correct)</description>
        /// </item>
        /// </list>
        /// </para>
        /// </remarks>
        /// <returns>kekulé representation</returns>
        /// <exception cref="InvalidSmilesException">molecule exploded on contact with reality</exception>"
        public Graph Kekule()
        {
            return Localise.LocaliseInPlace(this);
        }


        /// <summary>
        /// Verify that electrons can be assigned to any delocalised (aromatic)
        /// bonds. This method is faster than doing a full kekulisation and allows
        /// versification of aromatic structures without localising the bond Orders.
        /// However the method of determining the Kekulé structure is very similar
        /// and often is preferable to provide a molecule with defined bond Orders.
        /// </summary>
        /// <returns>electrons can be assigned</returns>
        /// <seealso cref="Kekule"/>
        public bool Assignable()
        {
            return ElectronAssignment.Verify(this);
        }

        /// <summary>
        /// Permute the vertices of a graph using a given permutation.
        /// </summary>
        /// <example>
        /// <code>
        /// g = CNCO
        /// h = g.Permuate(new int[]{1, 0, 3, 2});
        /// h = NCOC
        /// </code>
        /// </example>
        /// <param name="p">a permutation mapping indicate the new index of each atom</param>
        /// <returns>a new chemical graph with the vertices permuted by the given Ordering</returns>
        public Graph Permute(int[] p)
        {
            if (p.Length != order)
                throw new ArgumentException("permuation size should equal |V| (Order)");

            Graph cpy = new Graph(order)
            {
                flags = flags,
                order = order,
                size = size
            };

            for (int u = 0; u < order; u++)
            {
                int d = degrees[u];
                // v is the image of u in the permutation
                int v = p[u];
                if (d > 4) cpy.edges[v] = new Edge[d];
                cpy.atoms[v] = atoms[u];
                cpy.valences[v] = valences[u];
                cpy.AddTopology(TopologyOf(u).Transform(p));
                while (--d >= 0)
                {
                    Edge e = EdgeAt(u, d);

                    // important this is the second time we have seen the edge
                    // so the capacity must have been allocated. otherwise we
                    // would get an index out of bounds
                    if (u > e.Other(u))
                    {
                        // w is the image of vertex adjacen to u
                        int w = p[e.Other(u)];
                        Edge f = new Edge(v, w, e.GetBond(u));
                        cpy.edges[v][cpy.degrees[v]++] = f;
                        cpy.edges[w][cpy.degrees[w]++] = f;
                        cpy.size++;
                    }
                }
            }

            // ensure edges are in sorted order
            return cpy.Sort(new CanOrderFirst());
        }

        /// <summary>
        /// Access the atoms of the chemical graph.
        /// </summary>
        /// <example>
        /// <code>
        /// foreach (var a in g.GetAtoms()) 
        /// {
        ///
        /// }
        /// </code></example>
        /// <returns>iterable of atoms</returns>
        public IEnumerable<IAtom> GetAtoms() => GetAtoms_();

        internal IEnumerable<IAtom> GetAtoms_() => atoms.Take(Order);

        /// <summary>
        /// Access the edges of the chemical graph.
        /// </summary>
        /// <returns>iterable of edges</returns>
        public IEnumerable<Edge> Edges
        {
            get
            {
                for (int u = 0; u < order; u++)
                {
                    int d = degrees[u];
                    for (int i = 0; i < d; ++i)
                    {
                        Edge e = edges[u][i];
                        if (e.Other(u) < u)
                            yield return e;
                    }
                }
                yield break;
            }
        }

        /// <summary>
        /// Apply a function to the chemical graph.
        /// </summary>
        /// <param name="f">  a function which transforms a graph into something</param>.
        /// <returns>the output of the function</returns>
        T Apply<T>(IFunction<Graph, T> f)
        {
            return f.Apply(this);
        }

        internal void Clear()
        {
            Arrays.Fill(topologies, Topology.Unknown);
            for (int i = 0; i < order; i++)
            {
                atoms[i] = null;
                degrees[i] = 0;
            }
            order = 0;
            size = 0;
        }

        public int GetFlags(int mask)
        {
            return this.flags & mask;
        }

        public int GetFlags()
        {
            return this.flags;
        }

        internal void AddFlags(int mask)
        {
            this.flags = flags | mask;
        }

        internal void SetFlags(int flags)
        {
            this.flags = flags;
        }

        /// <summary>
        /// Sort the edges of the graph to visit in a specific Order. The graph is
        /// modified.
        /// 
        /// <param name="comparator">Ordering on edges</param>
        /// <returns>the graph</returns>
        /// </summary>
        public Graph Sort(IEdgeComparator comparator)
        {
            for (int u = 0; u < order; u++)
            {
                Edge[] es = edges[u];

                // insertion sort as most atoms have small degree <= 4
                int deg = degrees[u];
                for (int i = 1; i < deg; i++)
                {
                    int j = i - 1;
                    Edge e = es[i];
                    while (j >= 0 && comparator.Less(this, u, e, es[j]))
                    {
                        es[j + 1] = es[j--];
                    }
                    es[j + 1] = e;
                }
            }
            return this;
        }

        /// <summary>
        /// Defines a method for arranging the neighbors of an atom.
        /// </summary>
        public interface IEdgeComparator
        {
            /// <summary>
            /// Should the edge, e, be visited before f.
            /// 
            /// <param name="g">graph</param>
            /// <param name="u">the atom we are sorting from</param>
            /// <param name="e">an edge adjacent to u</param>
            /// <param name="f">an edge adjacent to u</param>
            /// <returns>edge e is less than edge f</returns>
            /// </summary>
            bool Less(Graph g, int u, Edge e, Edge f);
        }

        /// <summary>
        /// Sort the neighbors of each atom such that hydrogens are visited first and
        /// deuterium before tritium. 
        /// </summary>
        public sealed class VisitHydrogenFirst : IEdgeComparator
        {
            public bool Less(Graph g, int u, Edge e, Edge f)
            {
                int v = e.Other(u);
                int w = f.Other(u);

                Element vElem = g.GetAtom(v).Element;
                Element wElem = g.GetAtom(w).Element;

                if (vElem == Hydrogen && wElem != Hydrogen)
                    return true;
                if (vElem != Hydrogen && wElem == Hydrogen)
                    return false;

                // sort hydrogens by isotope
                return vElem == Hydrogen && g.GetAtom(v).Isotope < g.GetAtom(w).Isotope;
            }
        }

        /// <summary>
        /// Visit high Order bonds before low Order bonds.
        /// </summary>
        public sealed class VisitHighOrderFirst : IEdgeComparator
        {
            public bool Less(Graph g, int u, Edge e, Edge f)
            {
                return e.Bond.Order > f.Bond.Order;
            }
        }

        /// <summary>
        /// Arrange neighbors in canonical Order.
        /// </summary>
        internal sealed class CanOrderFirst : IEdgeComparator
        {
            public bool Less(Graph g, int u, Edge e, Edge f)
            {
                return e.Other(u) < f.Other(u);
            }
        }
    }
}
