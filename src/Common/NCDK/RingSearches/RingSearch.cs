/*
 * Copyright (C) 2012 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by the
 * Free Software Foundation; either version 2.1 of the License, or (at your
 * option) any later version. All we ask is that proper credit is given for our
 * work, which includes - but is not limited to - adding the above copyright
 * notice to the beginning of your source code files, and to any copyright
 * notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License
 * for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation, Inc.,
 * 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Graphs;
using System;
using System.Collections.Generic;

namespace NCDK.RingSearches
{
    /// <summary>
    /// Efficiently search for atoms that are members of a ring. A depth first search
    /// (DFS) determines which vertices belong to cycles (rings). As cycles are
    /// discovered they are separated into two sets of cycle systems, fused and
    /// isolated. A ring is in a fused cycle systems if it shares at least one edge
    /// (bond) with another cycle. The isolated cycles consist of cycles which at
    /// most share only one vertex (atom) with another cyclic system. A molecule may
    /// contain more then one isolated and/or fused cycle system (see. Examples).
    /// Additional computations such as C<sub>R</sub> (relevant cycles), Minimum
    /// Cycle Basis (MCB) (aka. Smallest Set of Smallest Rings (SSSR)) or the Set of
    /// All Rings can be completely bypassed for members of the isolated rings. Since
    /// every isolated cycle (ring) does not share any edges (bonds) with any other
    /// elementary cycle it cannot be made by composing any other cycles (rings).
    /// Therefore, all isolated cycles (rings) are relevant and are members of all
    /// minimum cycle bases (SSSRs). 
    /// <note type="important">The cycle sets returned are not ordered in the path of the cycle.</note> 
    /// </summary>
    /// <remarks>
    /// <para>Further Explanation</para> 
    /// <para>The diagram below illustrates the isolated
    /// and fused sets of cyclic atoms. The colored circles indicate the atoms and
    /// bonds that are returned for each molecules.
    /// </para>
    /// <para>
    /// <img alt="isolated and fused cycle systems" src="http://cdk.github.io/cdk/img/isolated-and-fused-cycles-01_zpse0311377.PNG"/>
    /// </para>
    /// <list type="bullet">
    /// <item>Two separate isolated cycles</item> <item>Two
    /// separate fused cycle systems. The bridged systems are fused but separate from
    /// each other</item> <item>Fused rings - a single fused cycle system</item> <item>Spiro
    /// rings - three separate isolated systems, no bonds are shared</item>
    /// <item>Cyclophane - a single fused system, the perimeter rings share bonds with
    /// the smaller rings </item> <item>One isolated system and one fused system</item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.RingSearches.RinSearch_Example.cs"]/*' />
    /// </example>
    /// <seealso href="http://en.wikipedia.org/wiki/Cycle_(graph_theory)">Cycle (Graph Theory) - Wikipedia</seealso>
    /// <seealso href="http://efficientbits.blogspot.co.uk/2012/12/scaling-up-faster-ring-detection-in-cdk.html">Scaling Up: Faster Ring Detecting in CDK - Efficient Bits, Blog</seealso>
    /// <seealso cref="SpanningTree"/>
    /// <seealso cref="AllRingsFinder"/>
    /// <seealso cref="ICyclicVertexSearch"/>
    // @author John May
    // @cdk.module core
    public sealed class RingSearch
    {
        /// <summary>depending on molecule size, delegate the search to one of two sub-classes</summary>
        private readonly ICyclicVertexSearch searcher;

        /// <summary>input atom container</summary>
        private readonly IAtomContainer container;

        /// <summary>
        /// Create a new RingSearch for the specified container.
        /// </summary>
        /// <param name="container">non-null input structure</param>
        /// <exception cref="ArgumentNullException">if the container was null</exception>
        /// <exception cref="ArgumentException">if the container contains a bond which references an atom which could not be found</exception>
        public RingSearch(IAtomContainer container)
            : this(container, GraphUtil.ToAdjList(container))
        { }

        /// <summary>
        /// Create a new RingSearch for the specified container and graph. The
        /// adjacency list allows much faster graph traversal but is not free to
        /// create. If the adjacency list representation of the input container has
        /// already been created you can bypass the creation with this constructor.
        /// </summary>
        /// <param name="container">non-null input structure</param>
        /// <param name="graph">non-null adjacency list representation of the container</param>
        /// <exception cref="ArgumentNullException">if the container or graph was null</exception>
        public RingSearch(IAtomContainer container, IReadOnlyList<IReadOnlyList<int>> graph)
            : this(container, MakeSearcher(graph))
        { }

        /// <summary>
        /// Create a new RingSearch for the specified container using the provided
        /// search.
        /// </summary>
        /// <param name="container">non-null input structure</param>
        /// <param name="searcher">non-null adjacency list representation of the container</param>
        /// <exception cref="ArgumentNullException">if the container or graph was null</exception>
        public RingSearch(IAtomContainer container, ICyclicVertexSearch searcher)
        {
            this.searcher = searcher ?? throw new ArgumentNullException(nameof(searcher), "searcher was null");
            this.container = container ?? throw new ArgumentNullException(nameof(container), "container must not be null");
        }

        /// <summary>
        /// Utility method making a new <see cref="ICyclicVertexSearch"/> during
        /// construction.
        /// </summary>
        /// <param name="graph">non-null graph</param>
        /// <returns>a new cyclic vertex search for the given graph</returns>
        /// <exception cref="ArgumentNullException">if the graph was null</exception>
        private static ICyclicVertexSearch MakeSearcher(IReadOnlyList<IReadOnlyList<int>> graph)
        {
            if (graph == null)
                throw new ArgumentNullException(nameof(graph), "graph[][] must not be null");

            // if the molecule has 64 or less atoms we can use single 64 bit long
            // values to represent our sets of vertices
            if (graph.Count <= 64)
            {
                return new RegularCyclicVertexSearch(graph);
            }
            else
            {
                return new JumboCyclicVertexSearch(graph);
            }
        }

        /// <summary>
        /// Access the number of rings found (aka. circuit rank, SSSR size).
        /// </summary>
        /// <returns>number of rings</returns>
        /// <seealso href="https://en.wikipedia.org/wiki/Circuit_rank">Circuit Rank</seealso>
        public int NumRings => searcher.NumCycles;

        /// <summary>
        /// Determine whether the edge between the vertices <paramref name="u"/> and <paramref name="v"/> is
        /// cyclic.
        /// </summary>
        /// <param name="u">an end point of the edge</param>
        /// <param name="v">another end point of the edge</param>
        /// <returns>whether the edge formed by the given end points is in a cycle</returns>
        public bool Cyclic(int u, int v)
        {
            return searcher.Cyclic(u, v);
        }

        /// <summary>
        /// Determine whether the provided atom belongs to a ring (is cyclic).
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.RingSearches.RingSearch_Example.cs+Cyclic"]/*' />
        /// </example>
        /// <param name="atom">an atom</param>
        /// <returns>whether the atom is in a ring</returns>
        /// <exception cref="NoSuchAtomException">the atom was not found</exception>
        public bool Cyclic(IAtom atom)
        {
            int i = container.Atoms.IndexOf(atom);
            if (i < 0)
                throw new NoSuchAtomException("no such atom");
            return Cyclic(i);
        }

        /// <summary>
        /// Determine whether the bond is cyclic. Note this currently requires a
        /// linear search to look-up the indices of each atoms.
        /// </summary>
        /// <param name="bond">a bond of the container</param>
        /// <returns>whether the vertex at the given index is in a cycle</returns>
        public bool Cyclic(IBond bond)
        {
            // XXX: linear search - but okay for now
            int u = container.Atoms.IndexOf(bond.Begin);
            int v = container.Atoms.IndexOf(bond.End);
            if (u < 0 || v < 0)
                throw new NoSuchAtomException("atoms of the bond are not found in the container");
            return searcher.Cyclic(u, v);
        }

        /// <summary>
        /// Determine whether the vertex at index <paramref name="i"/> is a cyclic vertex.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.RingSearches.RingSearch_Example.cs+Cyclic_int"]/*' />
        /// </example>
        /// <param name="i">atom index</param>
        /// <returns>whether the vertex at the given index is in a cycle</returns>
        public bool Cyclic(int i)
        {
            return searcher.Cyclic(i);
        }

        /// <summary>
        /// Construct a set of vertices which belong to any cycle (ring).
        /// </summary>
        /// <returns>cyclic vertices</returns>
        public int[] Cyclic()
        {
            return searcher.Cyclic();
        }

        /// <summary>
        /// Construct the sets of vertices which belong to isolated rings.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.RingSearches.RingSearch_Example.cs+Isolated"]/*' />
        /// </example>
        /// <returns>array of isolated fragments, defined by the vertices in the fragment</returns>
        public int[][] Isolated()
        {
            return searcher.Isolated();
        }

        /// <summary>
        /// Construct the sets of vertices which belong to fused ring systems.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.RingSearches.RingSearch_Example.cs+Fused"]/*' />
        /// </example>
        /// <returns>array of fused fragments, defined by the vertices in the fragment</returns>
        public int[][] Fused()
        {
            return searcher.Fused();
        }

        /// <summary>
        /// Extract the cyclic atom and bond fragments of the container. Bonds which
        /// join two different isolated/fused cycles (e.g. biphenyl) are not be
        /// included.
        /// </summary>
        /// <returns>a new container with only the cyclic atoms and bonds</returns>
        /// <seealso cref="SpanningTree.GetCyclicFragmentsContainer"/>>
        public IAtomContainer RingFragments()
        {
            var vertices = Cyclic();
            int n = vertices.Length;
            var atoms = new IAtom[n];
            List<IBond> bonds = new List<IBond>();

            for (int i = 0; i < vertices.Length; i++)
            {
                atoms[i] = container.Atoms[vertices[i]];
            }

            var abonds = container.Bonds;

            foreach (var bond in abonds)
            {
                IAtom either = bond.Begin;
                IAtom other = bond.End;

                int u = container.Atoms.IndexOf(either);
                int v = container.Atoms.IndexOf(other);

                // add the bond if the vertex colors match
                if (searcher.Cyclic(u, v)) bonds.Add(bond);
            }

            IChemObjectBuilder builder = container.Builder;
            IAtomContainer fragment = builder.NewAtomContainer(atoms, bonds);

            return fragment;
        }

        /// <summary>
        /// Determines whether the two vertex colors match. This method provides the
        /// conditional as to whether to include a bond in the construction of the
        /// <see cref="RingFragments"/>.
        /// </summary>
        /// <param name="eitherColor">either vertex color</param>
        /// <param name="otherColor">other vertex color</param>
        /// <returns>whether the two vertex colours match</returns>
        internal static bool Match(int eitherColor, int otherColor)
        {
            return (eitherColor != -1 && otherColor != -1)
                    && (eitherColor == otherColor || (eitherColor == 0 || otherColor == 0));
        }

        /// <summary>
        /// Construct a list of <see cref="IAtomContainer"/>s each of which only contains a
        /// single isolated ring. A ring is consider isolated if it does not share
        /// any bonds with another ring. By this definition each ring of a spiro ring
        /// system is considered isolated. The atoms are <b>not</b> arranged
        /// sequential.
        /// </summary>
        /// <returns>list of isolated ring fragments</returns>
        /// <seealso cref="Isolated"/>
        public IEnumerable<IAtomContainer> IsolatedRingFragments()
        {
            return ToFragments(Isolated());
        }

        /// <summary>
        /// Construct a list of <see cref="IAtomContainer"/>s which only contain fused
        /// rings. A ring is consider fused if it shares any bonds with another ring.
        /// By this definition bridged ring systems are also included. The atoms are
        /// <b>not</b> arranged sequential.
        /// </summary>
        /// <returns>list of fused ring fragments</returns>
        /// <seealso cref="Fused"/>
        public IEnumerable<IAtomContainer> FusedRingFragments()
        {
            return ToFragments(Fused());
        }

        /// <summary>
        /// Utility method for creating the fragments for the fused/isolated sets
        /// </summary>
        /// <param name="verticesList">2D array of vertices (rows=n fragments)</param>
        /// <returns>the vertices converted to an atom container</returns>
        /// <seealso cref="ToFragment(IReadOnlyList{System.Int32})"/>
        /// <seealso cref="FusedRingFragments"/>
        /// <seealso cref="IsolatedRingFragments"/>
        private IEnumerable<IAtomContainer> ToFragments(IReadOnlyList<IReadOnlyList<int>> verticesList)
        {
            foreach (var vertices in verticesList)
            {
                yield return ToFragment(vertices);
            }
            yield break;
        }

        /// <summary>
        /// Utility method for creating a fragment from an array of vertices
        /// </summary>
        /// <param name="vertices">array of vertices. Length=cycle weight, values 0 ... nAtoms</param>
        /// <returns>atom container only containing the specified atoms (and bonds)</returns>
        private IAtomContainer ToFragment(IReadOnlyList<int> vertices)
        {
            int n = vertices.Count;

            ICollection<IAtom> atoms = new HashSet<IAtom>();
            IList<IBond> bonds = new List<IBond>();

            // fill the atom set
            foreach (var v in vertices)
            {
                atoms.Add(container.Atoms[v]);
            }

            // include bonds that have both atoms in the atoms set
            foreach (var bond in container.Bonds)
            {
                IAtom either = bond.Begin;
                IAtom other = bond.End;
                if (atoms.Contains(either) && atoms.Contains(other))
                {
                    bonds.Add(bond);
                }
            }

            IAtomContainer fragment = container.Builder.NewAtomContainer(atoms, bonds);

            return fragment;
        }
    }
}
