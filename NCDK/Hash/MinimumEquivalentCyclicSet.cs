/*
 * Copyright (c) 2013 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.RingSearches;
using System.Collections.Generic;

namespace NCDK.Hash
{
    /// <summary>
    /// Finds the smallest set of equivalent values are members of a ring. If there
    /// are multiple smallest sets then the set with the lowest invariant value is
    /// returned. This class is intended to drive the systematic perturbation of the
    /// <see cref="PerturbedAtomHashGenerator"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method will not distinguish all possible molecules but represents a good
    /// enough approximation to quickly narrow down an identity search. At the time
    /// of writing (Feb, 2013) there are only 128 molecules (64 false positives) in
    /// PubChem-Compound (46E6 molecules) which are not separated. In many data sets
    /// this method will suffice however the exact <see cref="AllEquivalentCyclicSet"/> is
    /// provided.</para>
    /// <para>
    /// This method is currently the default used by the <see cref="HashGeneratorMaker"/> but can also be explicitly specified. 
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// MoleculeHashGenerator generator =
    ///   new HashGeneratorMaker().Depth(6)
    ///                           .Elemental()
    ///                           .Perturbed() // use this class by default
    ///                           .Molecular();
    ///
    /// // explicitly specify the method
    /// MoleculeHashGenerator generator =
    ///   new HashGeneratorMaker().Depth(6)
    ///                           .Elemental()
    ///                           .PerturbWith(new MinimumEquivalentCyclicSet())
    ///                           .Molecular();
    /// </code>
    /// </example>
    /// <seealso cref="PerturbedAtomHashGenerator"/>
    /// <seealso cref="MinimumEquivalentCyclicSetUnion"/>
    /// <seealso cref="AllEquivalentCyclicSet"/>
    /// <seealso cref="HashGeneratorMaker"/>
    // @author John May
    // @cdk.module hash
    internal sealed class MinimumEquivalentCyclicSet : EquivalentSetFinder
    {
        public override ISet<int> Find(long[] invariants, IAtomContainer container, int[][] graph)
        {
            int n = invariants.Length;

            // find cyclic vertices using DFS
            RingSearch ringSearch = new RingSearch(container, graph);

            // ordered map of the set of vertices for each value
            var equivalent = new SortedDictionary<long, ISet<int>>();

            // divide the invariants into equivalent indexed and ordered sets
            for (int i = 0; i < invariants.Length; i++)
            {
                long invariant = invariants[i];
                if (!equivalent.TryGetValue(invariant, out ISet<int> set))
                {
                    if (ringSearch.Cyclic(i))
                    {
                        set = new HashSet<int>
                        {
                            i
                        };
                        equivalent[invariant] = set;
                    }
                }
                else
                {
                    set.Add(i);
                }
            }

            // find the smallest set of equivalent cyclic vertices
            int minSize = int.MaxValue;
            ISet<int> min = new SortedSet<int>();
            foreach (var e in equivalent)
            {
                var vertices = e.Value;
                if (vertices.Count < minSize && vertices.Count > 1)
                {
                    min = vertices;
                    minSize = vertices.Count;
                }
            }

            return min;
        }
    }
}
