/*
 * Copyright (C) 2016-2018  The Chemistry Development Kit (CDK) project
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
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
 */

using NCDK.Common.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Smiles.Isomorphisms
{
    /// <summary>
    /// A filter for substructure matches implementing the logic for Atom-Atom Mapping matching. The following
    /// table from the Daylight theory manual summarises the expected functionality:
    /// </summary>
    /// <pre>
    /// C>>C                 CC>>CC    4 hits                        No maps, normal match.
    /// C>>C                 [CH3:7][CH3:8]>>[CH3:7][CH3:8] 4 hits   No maps in query, maps in target are ignored.
    /// [C:1]>>C             [CH3:7][CH3:8]>>[CH3:7][CH3:8] 4 hits   Unpaired map in query ignored.
    /// [C:1]>>[C:1]         CC>>CC  0 hits                          No maps in target, hence no matches.
    /// [C:?1]>>[C:?1]       CC>>CC  4 hits                          Query says mapped as shown or not present.
    /// [C:1]>>[C:1]         [CH3:7][CH3:8]>>[CH3:7][CH3:8] 2 hits   Matches for target 7,7 and 8,8 atom pairs.
    /// [C:1]>>[C:2]         [CH3:7][CH3:8]>>[CH3:7][CH3:8] 4 hits   When a query class is not found on both sides of the
    ///                                                              query, it is ignored; this query does NOT say that the
    ///                                                              atoms are in different classes.
    /// [C:1][C:1]>>[C:1]    [CH3:7][CH3:7]>>[CH3:7][CH3:7] 4 hits   Atom maps match with "or" logic. All atoms  get bound to
    ///                                                              class 7.
    /// [C:1][C:1]>>[C:1]    [CH3:7][CH3:8]>>[CH3:7][CH3:8] 4 hits   The reactant atoms are bound to classes 7 and 8. Note that
    ///                                                              having the first query atom bound to class 7 does not
    ///                                                              preclude binding the second atom. Next, the product
    ///                                                              atom can bind to classes 7 or 8.
    /// [C:1][C:1]>>[C:1]    [CH3:7][CH3:7]>>[CH3:7][CH3:8] 2 hits   The reactants are bound to class 7. The product atom can
    ///                                                              bind to class 7 only.
    /// </pre>
    /// <seealso href="http://www.daylight.com/dayhtml/doc/theory/theory.smarts.html">Daylight Theory Manual</seealso>
    internal sealed class AtomMapFilter
    {
        private readonly List<MappedPairs> mapped = new List<MappedPairs>();
        private readonly IAtomContainer target;

        internal AtomMapFilter(IAtomContainer query, IAtomContainer target)
        {
            IMultiDictionary<int, int> reactInvMap = null;
            IMultiDictionary<int, int> prodInvMap = null;

            this.target = target;

            // transform query maps in to matchable data-structure
            int numAtoms = query.Atoms.Count;
            for (int idx = 0; idx < numAtoms; idx++)
            {
                var atom = query.Atoms[idx];
                int mapidx = Mapidx(atom);
                if (mapidx == 0)
                    continue;
                switch (Role(atom))
                {
                    case ReactionRole.Reactant:
                        if (reactInvMap == null) reactInvMap = new MultiDictionary<int, int>();
                        reactInvMap.Add(mapidx, idx);
                        break;
                    case ReactionRole.Product:
                        if (prodInvMap == null) prodInvMap = new MultiDictionary<int, int>();
                        prodInvMap.Add(mapidx, idx);
                        break;
                }
            }

            if (reactInvMap != null && prodInvMap != null)
            {
                foreach (var e in reactInvMap)
                {
                    int[] reacMaps = e.Value.ToArray();
                    int[] prodMaps = prodInvMap[e.Key].ToArray();
                    if (prodMaps.Length == 0)
                        continue; // unpaired
                    mapped.Add(new MappedPairs(reacMaps, prodMaps));
                }
            }
        }

        /// <summary>
        /// Safely access the mapidx of an atom, returns 0 if null.
        /// </summary>
        /// <param name="atom">atom</param>
        /// <returns>mapidx, 0 if undefined</returns>
        private static int Mapidx(IAtom atom)
        {
            int mapidx = atom.GetProperty<int>(CDKPropertyName.AtomAtomMapping, 0);
            return mapidx;
        }

        /// <summary>
        /// Safely access the reaction role of an atom, returns <see cref="ReactionRole.None"/> if null.
        /// </summary>
        /// <param name="atom">atom</param>
        /// <returns>mapidx, None if undefined</returns>
        private static ReactionRole Role(IAtom atom)
        {
            ReactionRole role = atom.GetProperty<ReactionRole>(CDKPropertyName.ReactionRole, ReactionRole.None);
            return role;
        }

        /// <summary>
        /// Filters a structure match (described as an index permutation query -> target) for
        /// those where the atom-atom maps are acceptable.
        /// </summary>
        /// <param name="perm">permuation</param>
        /// <returns>whether the match should be accepted</returns>
        public bool Apply(int[] perm)
        {
            foreach (var mpair in mapped)
            {
                // possibly 'or' of query maps, need to use a set
                if (mpair.rIdxs.Length > 1)
                {
                    // bind target reactant maps
                    var bound = new HashSet<int>();
                    foreach (int rIdx in mpair.rIdxs)
                    {
                        int refidx = Mapidx(target.Atoms[perm[rIdx]]);
                        if (refidx == 0) return false; // unmapped in target
                        bound.Add(refidx);
                    }

                    // check product maps
                    foreach (var pIdx in mpair.pIdxs)
                    {
                        if (!bound.Contains(Mapidx(target.Atoms[perm[pIdx]])))
                            return false;
                    }
                }
                // no 'or' of query atom map (more common case)
                else
                {
                    var refidx = Mapidx(target.Atoms[perm[mpair.rIdxs[0]]]);
                    if (refidx == 0)
                        return false; // unmapped in target
                                      // pairwise mismatch
                    if (refidx != Mapidx(target.Atoms[perm[mpair.pIdxs[0]]]))
                        return false;
                    for (int i = 1; i < mpair.pIdxs.Length; i++)
                    {
                        if (refidx != Mapidx(target.Atoms[perm[mpair.pIdxs[i]]]))
                            return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Helper class list all reactant atom indices (rIdxs) and product
        /// atom indices (pIdxs) that are in the same Atom-Atom-Mapping class.
        /// </summary>
        private sealed class MappedPairs
        {
            internal readonly int[] rIdxs, pIdxs;

            internal MappedPairs(int[] rIdxs, int[] pIdxs)
            {
                this.rIdxs = rIdxs;
                this.pIdxs = pIdxs;
            }

            public override string ToString()
            {
                return "{" + Arrays.ToJavaString(rIdxs) + "=>" + Arrays.ToJavaString(pIdxs) + "}";
            }
        }
    }
}
