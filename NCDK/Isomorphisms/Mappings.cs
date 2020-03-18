/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
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
using System.Collections.ObjectModel;
using System.Linq;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// A fluent interface for handling (sub)-graph mappings from a query to a target
    /// structure. The utility allows one to modify the mappings and provides
    /// convenience utilities. <see cref="Mappings"/> are obtained from a (sub)-graph
    /// matching using <see cref="Pattern"/>.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+1"]/*' />
    /// The primary function is to provide an iterable of matches - each match is
    /// a permutation (mapping) of the query graph indices (atom indices).
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+enum_mappings"]/*' />
    /// The matches can be filtered to provide only those that have valid
    /// stereochemistry.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+stereochemistry"]/*' />
    /// Unique matches can be obtained for both atoms and bonds.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+unique_matches"]/*' />
    /// As matches may be lazily generated - iterating over the match twice (as
    /// above) will actually perform two graph matchings. If the mappings are needed
    /// for subsequent use the <see cref="ToArray"/> provides the permutations as a
    /// fixed size array.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+toarray"]/*' />
    /// Graphs with a high number of automorphisms can produce many valid matchings.
    /// Operations can be combined such as to limit the number of matches we
    /// retrieve.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+limit_matches"]/*' />
    /// There is no restrictions on which operation can be applied and how many times
    /// but the order of operations may change the result.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+all"]/*' />
    /// </example>
    /// <seealso cref="Pattern"/>
    // @author John May
    // @cdk.module isomorphism
    // @cdk.keyword substructure search
    // @cdk.keyword structure search
    // @cdk.keyword mappings
    // @cdk.keyword matching
    [Obsolete("Results now automatically consider stereo if it's present, to match without stereochemistry remove the stereo features.")]
    public sealed class Mappings : IEnumerable<int[]>
    {
        /// <summary>Enumerable permutations of the query vertices.</summary>
        private readonly IEnumerable<int[]> iterable;

        /// <summary>Query and target structures.</summary>
        private IAtomContainer query, target;

        /// <summary>
        /// Create a fluent mappings instance for the provided query / target and an
        /// enumerable of permutations on the query vertices (specified as indices).
        /// </summary>
        /// <param name="query">the structure to be found</param>
        /// <param name="target">the structure being searched</param>
        /// <param name="iterable">enumerable of permutation</param>
        /// <seealso cref="Pattern"/>
        internal Mappings(IAtomContainer query, IAtomContainer target, IEnumerable<int[]> iterable)
        {
            this.query = query;
            this.target = target;
            this.iterable = iterable;
        }

        /// <summary>
        /// Filter the mappings and keep only those which match the provided
        /// predicate (Guava).
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+Filter"]/*' />
        /// </example>
        /// <param name="predicate">a predicate</param>
        /// <returns>fluent-api reference</returns>
        public Mappings Filter(Predicate<int[]> predicate)
        {
            return new Mappings(query, target, iterable.Where(n => predicate.Invoke(n)));
        }

        /// <summary>
        /// Enumerate the mappings to another type. Each mapping is transformed using the
        /// provided function.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+Filter"]/*' />
        /// </example>
        /// <typeparam name="T"></typeparam>
        /// <param name="f">function to transform a mapping</param>
        /// <returns>The transformed types</returns>
        public IEnumerable<T> GetMapping<T>(Func<int[], T> f)
        {
            return iterable.Select(n => f(n));
        }

        /// <summary>
        /// Limit the number of mappings - only this number of mappings will be
        /// generate.
        /// </summary>
        /// <param name="limit">the number of mappings</param>
        /// <returns>fluent-api instance</returns>
        public Mappings Limit(int limit)
        {
            return new Mappings(query, target, iterable.Take(limit));
        }

        /// <summary>
        /// Filter the mappings for those which preserve stereochemistry specified in
        /// the query.
        /// </summary>
        /// <returns>fluent-api instance</returns>
        public Mappings GetStereochemistry()
        {
            // query structures currently have special requirements (i.e. SMARTS)
            if (query is IQueryAtomContainer)
                return this;
            var match = new StereoMatch(query, target);
            return Filter(n => match.Apply(n));
        }

        /// <summary>
        /// Filter the mappings for those which cover a unique set of atoms in the
        /// target. The unique atom mappings are a subset of the unique bond
        /// matches.
        /// </summary>
        /// <returns>fluent-api instance</returns>
        /// <seealso cref="GetUniqueBonds"/>
        public Mappings GetUniqueAtoms()
        {
            // we need the unique predicate to be reset for each new iterator -
            // otherwise multiple iterations are always filtered (seen before)
            var m = new UniqueAtomMatches();
            return new Mappings(query, target, iterable.Where(n => m.Apply(n)));
        }

        /// <summary>
        /// Filter the mappings for those which cover a unique set of bonds in the
        /// target.
        /// </summary>
        /// <returns>fluent-api instance</returns>
        /// <seealso cref="GetUniqueAtoms"/>
        public Mappings GetUniqueBonds()
        {
            // we need the unique predicate to be reset for each new iterator -
            // otherwise multiple iterations are always filtered (seen before)
            int[][] g = GraphUtil.ToAdjList(query);
            var m = new UniqueBondMatches(g);
            return new Mappings(query, target, iterable.Where(m.Apply));
        }

        /// <summary>
        /// Mappings are lazily generated and best used in a loop. However if all
        /// mappings are required this method can provide a fixed size array of
        /// mappings.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+ToArray1"]/*' />
        /// The method can be used in combination with other modifiers.
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+ToArray2"]/*' />
        /// </example>
        /// <returns>array of mappings</returns>
        public int[][] ToArray()
        {
            return iterable.ToArray();
        }

        /// <summary>
        /// Convert the permutations to a atom-atom map.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+ToAtomMap"]/*' />
        /// </example>
        /// <returns>iterable of atom-atom mappings</returns>
        public IEnumerable<IReadOnlyDictionary<IAtom, IAtom>> ToAtomMaps()
        {
            var mapper = new AtomMaper(query, target);
            return GetMapping(n => mapper.Apply(n));
        }

        /// <summary>
        /// Convert the permutations to a bond-bond map.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+ToBondMap"]/*' />
        /// </example>
        /// <returns>iterable of bond-bond mappings</returns>
        public IEnumerable<IReadOnlyDictionary<IBond, IBond>> ToBondMaps()
        {
            var mapper = new BondMaper(query, target);
            return GetMapping(n => mapper.Apply(n));
        }

        /// <summary>
        /// Convert the permutations to an atom-atom bond-bond map.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+ToAtomBondMap"]/*' />
        /// </example>
        /// <returns>iterable of atom-atom and bond-bond mappings</returns>
        public IEnumerable<IReadOnlyDictionary<IChemObject, IChemObject>> ToAtomBondMaps()
        {
            var map = new AtomBondMaper(query, target);
            return GetMapping(n => map.Apply(n));
        }

        /// <summary>
        /// Obtain the chem objects (atoms and bonds) that have 'hit' in the target molecule.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+ToChemObjects"]/*' />
        /// </example>
        /// <returns>lazy iterable of chem objects</returns>
        public IEnumerable<IChemObject> ToChemObjects()
        {
            var mapper = new AtomBondMaper(query, target);
            foreach (var a in GetMapping(n => mapper.Apply(n)).Select(map => map.Values))
                foreach (var b in a)
                    yield return b;
            yield break;
        }

        /// <summary>
        /// Obtain the mapped substructures (atoms/bonds) of the target compound. The atoms
        /// and bonds are the same as in the target molecule but there may be less of them.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+ToSubstructures"]/*' />
        /// </example>
        /// <returns>lazy iterable of molecules</returns>
        public IEnumerable<IAtomContainer> ToSubstructures()
        {
            var mapper = new AtomBondMaper(query, target);
            return GetMapping(n => mapper.Apply(n)).Select(map =>
            {
                var submol = target.Builder.NewAtomContainer();
                foreach (var atom in query.Atoms)
                    submol.Atoms.Add((IAtom)map[atom]);
                foreach (var bond in query.Bonds)
                    submol.Bonds.Add((IBond)map[bond]);
                return submol;
            });
        }

        /// <summary>
        /// Efficiently determine if there are at least 'n' matches
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Mappings_Example.cs+AtLeast"]/*' />
        /// </example>
        /// <param name="n">number of matches</param>
        /// <returns>there are at least 'n' matches</returns>
        public bool AtLeast(int n)
        {
            return Limit(n).Count() == n;
        }

        /// <summary>
        /// Obtain the first match - if there is no first match an empty array is
        /// returned.
        /// </summary>
        /// <returns>first match</returns>
        public int[] First()
        {
            var f = iterable.FirstOrDefault();
            return f ?? Array.Empty<int>();
        }

        /// <summary>
        /// Convenience method to count the number of unique atom mappings. 
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// Mappings are lazily generated and checking the count and then iterating
        /// over the mappings currently performs two searches. If the mappings are
        /// also needed, it is more efficient to check the mappings and count
        /// manually.
        /// </note>
        /// <para>
        /// The method is simply invokes <c>Mappings.GetUniqueAtoms().Count()</c>.
        /// </para>
        /// </remarks>
        /// <returns>number of matches</returns>
        public int CountUnique()
        {
            return GetUniqueAtoms().Count();
        }

        /// <inheritdoc/>
        public IEnumerator<int[]> GetEnumerator()
        {
            return iterable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>Utility to transform a permutation into the atom-atom map.</summary>
        private sealed class AtomMaper
        {
            /// <summary>Query/target containers from the graph matching.</summary>
            private readonly IAtomContainer query, target;

            /// <summary>
            /// Use the provided query and target to obtain the atom instances.
            /// </summary>
            /// <param name="query">the structure to be found</param>
            /// <param name="target">the structure being searched</param>
            public AtomMaper(IAtomContainer query, IAtomContainer target)
            {
                this.query = query;
                this.target = target;
            }

            /// <inheritdoc/>
            public IReadOnlyDictionary<IAtom, IAtom> Apply(int[] mapping)
            {
                var map = new Dictionary<IAtom, IAtom>();
                for (int i = 0; i < mapping.Length; i++)
                    map.Add(query.Atoms[i], target.Atoms[mapping[i]]);
                return map;
            }
        }

        /// <summary>Utility to transform a permutation into the bond-bond map.</summary>
        private sealed class BondMaper
        {
            /// <summary>The query graph - indicates a presence of edges.</summary>
            private readonly int[][] g1;

            /// <summary>Bond look ups for the query and target.</summary>
            private readonly EdgeToBondMap bonds1, bonds2;

            /// <summary>
            /// Use the provided query and target to obtain the bond instances.
            /// </summary>
            /// <param name="query">the structure to be found</param>
            /// <param name="target">the structure being searched</param>
            public BondMaper(IAtomContainer query, IAtomContainer target)
            {
                this.bonds1 = EdgeToBondMap.WithSpaceFor(query);
                this.bonds2 = EdgeToBondMap.WithSpaceFor(target);
                this.g1 = GraphUtil.ToAdjList(query, bonds1);
                GraphUtil.ToAdjList(target, bonds2);
            }

            /// <inheritdoc/>
            public IReadOnlyDictionary<IBond, IBond> Apply(int[] mapping)
            {
                var map = new Dictionary<IBond, IBond>();
                for (int u = 0; u < g1.Length; u++)
                {
                    foreach (var v in g1[u])
                    {
                        if (v > u)
                        {
                            map.Add(bonds1[u, v], bonds2[mapping[u], mapping[v]]);
                        }
                    }
                }
                return map;
            }
        }

        /// <summary>Utility to transform a permutation into an atom-atom and bond-bond map.</summary>
        private sealed class AtomBondMaper
        {
            /// <summary>The query graph - indicates a presence of edges.</summary>
            private readonly int[][] g1;

            /// <summary>Bond look ups for the query and target.</summary>
            private readonly EdgeToBondMap bonds1, bonds2;

            private IAtomContainer query;
            private IAtomContainer target;

            /// <summary>
            /// Use the provided query and target to obtain the bond instances.
            /// </summary>
            /// <param name="query">the structure to be found</param>
            /// <param name="target">the structure being searched</param>
            public AtomBondMaper(IAtomContainer query, IAtomContainer target)
            {
                this.query = query;
                this.target = target;
                this.bonds1 = EdgeToBondMap.WithSpaceFor(query);
                this.bonds2 = EdgeToBondMap.WithSpaceFor(target);
                this.g1 = GraphUtil.ToAdjList(query, bonds1);
                GraphUtil.ToAdjList(target, bonds2);
            }

            /// <inheritdoc/>
            public IReadOnlyDictionary<IChemObject, IChemObject> Apply(int[] mapping)
            {
                var map = new Dictionary<IChemObject, IChemObject>();
                for (int u = 0; u < g1.Length; u++)
                {
                    map.Add(query.Atoms[u], target.Atoms[mapping[u]]);
                    foreach (var v in g1[u])
                    {
                        if (v > u)
                        {
                            map.Add(bonds1[u, v], bonds2[mapping[u], mapping[v]]);
                        }
                    }
                }
                return new ReadOnlyDictionary<IChemObject, IChemObject>(map);
            }
        }
    }
}
