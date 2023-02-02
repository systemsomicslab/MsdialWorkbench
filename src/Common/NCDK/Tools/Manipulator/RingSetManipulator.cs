/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;
using System.Linq;

namespace NCDK.Tools.Manipulator
{
    // @cdk.module standard
    public static class RingSetManipulator
    {
        /// <summary>
        /// Return the total number of atoms over all the rings in the collection.
        /// </summary>
        /// <param name="set">The collection of rings</param>
        /// <returns>The total number of atoms</returns>
        public static int GetAtomCount(IRingSet set)
        {
            int count = 0;
            foreach (var atomContainer in set)
            {
                count += atomContainer.Atoms.Count;
            }
            return count;
        }

        /// <summary>
        /// Puts all rings of a ringSet in a single atomContainer
        /// </summary>
        /// <param name="ringSet">The ringSet to use</param>
        /// <returns>The produced atomContainer</returns>
        public static IAtomContainer GetAllInOneContainer(IRingSet ringSet)
        {
            var resultContainer = ringSet.Builder.NewAtomContainer();
            var containers = RingSetManipulator.GetAllAtomContainers(ringSet).GetEnumerator();
            while (containers.MoveNext())
            {
                resultContainer.Add(containers.Current);
            }
            return resultContainer;
        }

        /// <summary>
        /// Returns the largest (number of atoms) ring set in a molecule
        /// </summary>
        /// <param name="ringSystems">RingSystems of a molecule</param>
        /// <returns>The largestRingSet</returns>
        public static IRingSet GetLargestRingSet(IEnumerable<IRingSet> ringSystems)
        {
            IRingSet largestRingSet = null;
            int atomNumber = 0;
            IAtomContainer container = null;
            foreach (var ringSystem in ringSystems)
            {
                container = RingSetManipulator.GetAllInOneContainer(ringSystem);
                if (atomNumber < container.Atoms.Count)
                {
                    atomNumber = container.Atoms.Count;
                    largestRingSet = ringSystem;
                }
            }
            return largestRingSet;
        }

        /// <summary>
        /// Return the total number of bonds over all the rings in the collection.
        /// </summary>
        /// <param name="set">The collection of rings</param>
        /// <returns>The total number of bonds</returns>
        public static int GetBondCount(IRingSet set)
        {
            int count = 0;
            foreach (var atomContainer in set)
            {
                count += atomContainer.Bonds.Count;
            }
            return count;
        }

        /// <summary>
        /// Returns all the AtomContainer's in a RingSet.
        /// <param name="set">The collection of rings</param>
        /// <returns>A list of IAtomContainer objects corresponding to individual rings</returns>
        /// </summary>
        public static IEnumerable<IAtomContainer> GetAllAtomContainers(IEnumerable<IRing> set)
        {
            return set;
        }

        /// <summary>
        /// Sorts the rings in the set by size. The smallest ring comes
        /// first.
        /// <param name="ringSet">The collection of rings</param>
        /// </summary>
        public static void Sort(IList<IRing> ringSet)
        {
            var ringList = ringSet.ToList();
            ringList.Sort(new RingSizeComparator(SortMode.SmallFirst));
            ringSet.Clear();
            foreach (var aRingList in ringList)
                ringSet.Add(aRingList);
        }

        /// <summary>
        /// We define the heaviest ring as the one with the highest number of double bonds.
        /// Needed for example for the placement of in-ring double bonds.
        /// </summary>
        /// <param name="ringSet">The collection of rings</param>
        /// <param name="bond">A bond which must be contained by the heaviest ring</param>
        /// <returns>The ring with the highest number of double bonds connected to a given bond</returns>
        public static IRing GetHeaviestRing(IRingSet ringSet, IBond bond)
        {
            var rings = ringSet.GetRings(bond);
            IRing ring = null;
            int maxOrderSum = 0;
            foreach (var ring1 in rings)
            {
                if (maxOrderSum < ((IRing)ring1).GetBondOrderSum())
                {
                    ring = (IRing)ring1;
                    maxOrderSum = ring.GetBondOrderSum();
                }
            }
            return ring;
        }

        /// <summary>
        /// Returns the ring with the highest numbers of other rings attached to it. If an
        /// equally complex ring is found the last one is selected allow prioritization by
        /// size with <see cref="RingSetManipulator.Sort(IList{IRing})"/>.
        /// </summary>
        /// <param name="ringSet">The collection of rings</param>
        /// <returns>the ring with the highest numbers of other rings attached to it.</returns>
        public static IRing GetMostComplexRing(IChemObjectSet<IRing> ringSet)
        {
            var neighbors = new int[ringSet.Count];
            IRing ring1, ring2;
            IAtom atom1, atom2;
            int mostComplex = 0, mostComplexPosition = 0;
            /* for all rings in this RingSet */
            for (int i = 0; i < ringSet.Count; i++)
            {
                /* Take each ring */
                ring1 = (IRing)ringSet[i];
                // look at each Atom in this ring whether it is part of any other ring
                for (int j = 0; j < ring1.Atoms.Count; j++)
                {
                    atom1 = ring1.Atoms[j];
                    /* Look at each of the other rings in the ring set */
                    for (int k = i + 1; k < ringSet.Count; k++)
                    {
                        ring2 = (IRing)ringSet[k];
                        if (ring1 != ring2)
                        {
                            for (int l = 0; l < ring2.Atoms.Count; l++)
                            {
                                atom2 = ring2.Atoms[l];
                                if (atom1 == atom2)
                                {
                                    neighbors[i]++;
                                    neighbors[k]++;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] >= mostComplex)
                {
                    mostComplex = neighbors[i];
                    mostComplexPosition = i;
                }
            }
            return (IRing)ringSet[mostComplexPosition];
        }

        /// <summary>
        /// Checks if <paramref name="atom1"/> and <paramref name="atom2"/> share membership in the same ring or ring system.
        /// Membership in the same ring is checked if the RingSet contains the SSSR of a molecule; membership in
        /// the same ring or same ring system is checked if the RingSet contains all rings of a molecule.
        /// </summary>
        /// <remarks>
        /// <note type="important">
        /// This method only returns meaningful results if <paramref name="atom1"/> and 
        /// <paramref name="atom2"/> are members of the same molecule for which the ring set was calculated!
        /// </note>
        /// </remarks>
        /// <param name="ringSet">The collection of rings</param>
        /// <param name="atom1">The first atom</param>
        /// <param name="atom2">The second atom</param>
        /// <returns><see langword="true"/> if <paramref name="atom1"/> and <paramref name="atom2"/> share membership of at least one ring or ring system, false otherwise</returns>
        public static bool IsSameRing(IRingSet ringSet, IAtom atom1, IAtom atom2)
        {
            foreach (var atomContainer in ringSet)
            {
                var ring = (IRing)atomContainer;
                if (ring.Contains(atom1) && ring.Contains(atom2))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks - and returns 'true' - if a certain ring is already
        /// stored in the ring set. This is not a test for equality of Ring
        /// objects, but compares all Bond objects of the ring.
        /// </summary>
        /// <param name="newRing">The ring to be tested if it is already stored</param>
        /// <param name="ringSet">The collection of rings</param>
        /// <returns><see langword="true"/> if it is already stored</returns>
        public static bool RingAlreadyInSet(IRing newRing, IRingSet ringSet)
        {
            IRing ring;
            int equalCount;
            bool equals;
            for (int f = 0; f < ringSet.Count; f++)
            {
                equals = false;
                equalCount = 0;
                ring = (IRing)ringSet[f];

                if (ring.Bonds.Count == newRing.Bonds.Count)
                {
                    foreach (var newBond in newRing.Bonds)
                    {
                        foreach (var bond in ring.Bonds)
                        {
                            if (newBond == bond)
                            {
                                equals = true;
                                equalCount++;
                                break;
                            }
                        }
                        if (!equals)
                            break;
                    }
                }

                if (equalCount == ring.Bonds.Count)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Iterates over the rings in the ring set, and marks the ring
        /// aromatic if all atoms and all bonds are aromatic.
        /// </summary>
        /// <remarks>
        /// This method assumes that aromaticity perception has been done before hand.
        /// </remarks>
        /// <param name="ringset">The collection of rings</param>
        public static void MarkAromaticRings(IRingSet ringset)
        {
            foreach (var atomContainer in ringset)
            {
                RingManipulator.MarkAromaticRings((IRing)atomContainer);
            }
        }
    }
}
