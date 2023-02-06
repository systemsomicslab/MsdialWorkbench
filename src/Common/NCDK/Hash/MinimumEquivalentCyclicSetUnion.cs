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
using System;
using System.Collections.Generic;

namespace NCDK.Hash
{
    /// <summary>
    /// The union of all the smallest set of equivalent values are members of a ring.
    /// This class is intended to drive the systematic perturbation of the <see cref="PerturbedAtomHashGenerator"/>. 
    /// The method is more comprehensive then a single <see cref="MinimumEquivalentCyclicSet"/> and not as
    /// computationally demanding as <see cref="AllEquivalentCyclicSet"/>. In reality one
    /// should choose either use the fast (but good) heuristic <see cref="MinimumEquivalentCyclicSet"/> 
    /// or the exact <see cref="AllEquivalentCyclicSet"/>. This method is provided for demonstration only.
    /// </summary>
    /// <remarks>
    /// As with the <see cref="MinimumEquivalentCyclicSet"/> perturbation, this method does
    /// not guarantee that all molecules will be distinguished. At the time of
    /// writing (Feb 2013) there are only 8 structure in PubChem-Compound which need
    /// the more comprehensive perturbation method (<see cref="AllEquivalentCyclicSet"/>),
    /// these are listed below.
    /// <list type="bullet">
    /// <item>CID <see href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=144432">144432</see>
    /// and CID <see href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=15584856">15584856</see></item>
    /// <item>CID <see href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=138898">138898</see>
    /// and CID <see href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=241107">241107</see></item>
    /// <item>CID <see href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=9990759">9990759</see>
    /// and CID <see href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=10899923">10899923</see></item>
    /// <item>CID <see href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=5460768">5460768</see>
    /// and CID <see href="http://pubchem.ncbi.nlm.nih.gov/summary/summary.cgi?cid=20673269">20673269</see></item>
    /// </list> 
    /// </remarks>
    /// <example>
    /// The easiest way to use this class is with the <see cref="HashGeneratorMaker"/>.
    /// <code>
    /// MoleculeHashGenerator generator =
    ///   new HashGeneratorMaker().Depth(6)
    ///                           .Elemental()
    ///                           .PerturbWith(new MinimumEquivalentCyclicSetUnion())
    ///                           .Molecular();
    /// </code>
    /// </example>
    /// <seealso cref="PerturbedAtomHashGenerator"/>
    /// <seealso cref="MinimumEquivalentCyclicSet"/>
    /// <seealso cref="AllEquivalentCyclicSet"/>
    // @author John May
    // @cdk.module hash
    [Obsolete("provided for to demonstrate a relatively robust but ultimately incomplete approach")]
    internal sealed class MinimumEquivalentCyclicSetUnion : EquivalentSetFinder
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
                ISet<int> vertices = e.Value;
                if (vertices.Count < minSize && vertices.Count > 1)
                {
                    min = vertices;
                    minSize = vertices.Count;
                }
                else if (vertices.Count == minSize)
                {
                    foreach (var vertice in vertices)
                        min.Add(vertice);
                }
            }

            return min;
        }
    }
}
