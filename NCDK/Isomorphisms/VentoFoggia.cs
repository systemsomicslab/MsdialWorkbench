/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Graphs;
using NCDK.Isomorphisms.Matchers;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// A structure pattern which utilises the Vento-Foggia (VF) algorithm <token>cdk-cite-Cordella04</token>.
    /// </summary>
    /// <example>
    /// Find and count the number molecules which contain the query substructure.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.VentoFoggia_Example.cs+1"]/*' />
    /// Finding the matching to molecules which contain the query substructure. It is
    /// more efficient to obtain the <see cref="Match(IAtomContainer)"/> and check it's size rather than
    /// test if it <see cref="MatchAll(IAtomContainer)"/>. These methods automatically verify
    /// stereochemistry.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.VentoFoggia_Example.cs+2"]/*' />
    /// </example>
    // @author John May
    // @cdk.module isomorphism
    public sealed class VentoFoggia : Pattern
    {
        /// <summary>The query structure.</summary>
        private readonly IAtomContainer query;

        /// <summary>The query structure adjacency list.</summary>
        private readonly int[][] g1;

        /// <summary>The bonds of the query structure.</summary>
        private readonly EdgeToBondMap bonds1;

        /// <summary>The atom matcher to determine atom feasibility.</summary>
        private readonly AtomMatcher atomMatcher;

        /// <summary>The bond matcher to determine atom feasibility.</summary>
        private readonly BondMatcher bondMatcher;

        /// <summary>Search for a subgraph.</summary>
        private readonly bool subgraph;

        /// <summary>
        /// Non-public constructor for-now the atom/bond semantics are fixed.
        /// </summary>
        /// <param name="query">the query structure</param>
        /// <param name="atomMatcher">how atoms should be matched</param>
        /// <param name="bondMatcher">how bonds should be matched</param>
        /// <param name="substructure">substructure search</param>
        private VentoFoggia(IAtomContainer query, AtomMatcher atomMatcher, BondMatcher bondMatcher, bool substructure)
        {
            this.query = query;
            this.atomMatcher = atomMatcher;
            this.bondMatcher = bondMatcher;
            this.bonds1 = EdgeToBondMap.WithSpaceFor(query);
            this.g1 = GraphUtil.ToAdjList(query, bonds1);
            this.subgraph = substructure;
            DetermineFilters(query);
        }

        /// <inheritdoc/>
        public override int[] Match(IAtomContainer target)
        {
            return MatchAll(target).First();
        }

        /// <inheritdoc/>
        public override Mappings MatchAll(IAtomContainer target)
        {
            EdgeToBondMap bonds2;
            int[][] g2;

            var cached = target.GetProperty<AdjListCache>(typeof(AdjListCache).FullName);
            if (cached == null || !cached.Validate(target))
            {
                cached = new AdjListCache(target);
                target.SetProperty(typeof(AdjListCache).FullName, cached);
            }

            bonds2 = cached.bmap;
            g2 = cached.g;
            var iterable = new VFIterable(query, target, g1, g2, bonds1, bonds2, atomMatcher, bondMatcher, subgraph);
            var mappings = new Mappings(query, target, iterable);
            return Filter(mappings, query, target);
        }

        /// <summary>
        /// Create a pattern which can be used to find molecules which contain the
        /// <paramref name="query"/> structure.
        /// </summary>
        /// <param name="query">the substructure to find</param>
        /// <returns>a pattern for finding the <paramref name="query"/></returns>
        public static new Pattern CreateSubstructureFinder(IAtomContainer query)
        {
            bool isQuery = query is IQueryAtomContainer;
            return CreateSubstructureFinder(query,
                isQuery ? AtomMatcher.CreateQueryMatcher() : AtomMatcher.CreateElementMatcher(),
                isQuery ? BondMatcher.CreateQueryMatcher() : BondMatcher.CreateOrderMatcher());
        }

        /// <summary>
        /// Create a pattern which can be used to find molecules which are the same
        /// as the <paramref name="query"/> structure.
        /// </summary>
        /// <param name="query">the substructure to find</param>
        /// <returns>a pattern for finding the <paramref name="query"/></returns>
        public static new Pattern CreateIdenticalFinder(IAtomContainer query)
        {
            bool isQuery = query is IQueryAtomContainer;
            return CreateIdenticalFinder(query,
                                 isQuery ? AtomMatcher.CreateQueryMatcher() : AtomMatcher.CreateElementMatcher(),
                                 isQuery ? BondMatcher.CreateQueryMatcher() : BondMatcher.CreateOrderMatcher());
        }

        /// <summary>
        /// Create a pattern which can be used to find molecules which contain the
        /// <paramref name="query"/> structure.
        /// </summary>
        /// <param name="query">the substructure to find</param>
        /// <param name="atomMatcher">how atoms are matched</param>
        /// <param name="bondMatcher">how bonds are matched</param>
        /// <returns>a pattern for finding the <paramref name="query"/></returns>
        public static Pattern CreateSubstructureFinder(IAtomContainer query, AtomMatcher atomMatcher, BondMatcher bondMatcher)
        {
            return new VentoFoggia(query, atomMatcher, bondMatcher, true);
        }

        /// <summary>
        /// Create a pattern which can be used to find molecules which are the same
        /// as the <paramref name="query"/> structure.
        /// </summary>
        /// <param name="query">the substructure to find</param>
        /// <param name="atomMatcher">how atoms are matched</param>
        /// <param name="bondMatcher">how bonds are matched</param>
        /// <returns>a pattern for finding the <paramref name="query"/></returns>
        public static Pattern CreateIdenticalFinder(IAtomContainer query, AtomMatcher atomMatcher, BondMatcher bondMatcher)
        {
            return new VentoFoggia(query, atomMatcher, bondMatcher, false);
        }

        private sealed class VFIterable : IEnumerable<int[]>
        {
            /// <summary>Query and target containers.</summary>
            private readonly IAtomContainer container1, container2;

            /// <summary>Query and target adjacency lists.</summary>
            private readonly int[][] g1, g2;

            /// <summary>Query and target bond lookup.</summary>
            private readonly EdgeToBondMap bonds1, bonds2;

            /// <summary>How are atoms are matched.</summary>
            private readonly AtomMatcher atomMatcher;

            /// <summary>How are bonds are match.</summary>
            private readonly BondMatcher bondMatcher;

            /// <summary>The query is a subgraph.</summary>
            private readonly bool subgraph;

            /// <summary>
            /// Create a match for the following parameters.
            /// </summary>
            /// <param name="container1">query structure</param>
            /// <param name="container2">target structure</param>
            /// <param name="g1">query adjacency list</param>
            /// <param name="g2">target adjacency list</param>
            /// <param name="bonds1">query bond map</param>
            /// <param name="bonds2">target bond map</param>
            /// <param name="atomMatcher">how atoms are matched</param>
            /// <param name="bondMatcher">how bonds are matched</param>
            /// <param name="subgraph">perform subgraph search</param>
            public VFIterable(IAtomContainer container1, IAtomContainer container2, int[][] g1, int[][] g2,
                    EdgeToBondMap bonds1, EdgeToBondMap bonds2, AtomMatcher atomMatcher, BondMatcher bondMatcher,
                    bool subgraph)
            {
                this.container1 = container1;
                this.container2 = container2;
                this.g1 = g1;
                this.g2 = g2;
                this.bonds1 = bonds1;
                this.bonds2 = bonds2;
                this.atomMatcher = atomMatcher;
                this.bondMatcher = bondMatcher;
                this.subgraph = subgraph;
            }

            /// <inheritdoc/>
            public IEnumerator<int[]> GetEnumerator()
            {
                StateStream ss;
                if (subgraph)
                {
                    ss = new StateStream(new VFSubState(container1, container2, g1, g2, bonds1, bonds2, atomMatcher,
                            bondMatcher));
                }
                else
                {
                    ss = new StateStream(
                        new VFState(container1, container2, g1, g2, bonds1, bonds2, atomMatcher, bondMatcher));
                }
                return ss.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private sealed class AdjListCache
        {
            // 100 ms max age
            private static readonly long MAX_AGE = new TimeSpan(0, 0, 0, 100).Ticks;

            internal readonly int[][] g;
            internal readonly EdgeToBondMap bmap;
            private readonly int numAtoms, numBonds;
            private readonly long tInit;

            internal AdjListCache(IAtomContainer mol)
            {
                this.bmap = EdgeToBondMap.WithSpaceFor(mol);
                this.g = GraphUtil.ToAdjList(mol, bmap);
                this.numAtoms = mol.Atoms.Count;
                this.numBonds = mol.Bonds.Count;
                this.tInit = DateTime.Now.Ticks;
            }

            internal bool Validate(IAtomContainer mol)
            {
                return mol.Atoms.Count == numAtoms 
                    && mol.Bonds.Count == numBonds
                    && (DateTime.Now.Ticks - tInit) < MAX_AGE;
            }
        }
    }
}
