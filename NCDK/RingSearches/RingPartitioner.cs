/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;
using System.Linq;

namespace NCDK.RingSearches
{
    /// <summary>
    /// Partitions a RingSet into RingSets of connected rings. Rings which share an
    /// atom, a bond or three or more atoms with at least on other ring in the
    /// RingSet are considered connected.
    /// </summary>
    // @cdk.module standard
    public static class RingPartitioner
    {
        /// <summary>
        ///  Debugging on/off
        /// </summary>
        public const bool debug = false;

        // minimum details

        /// <summary>
        /// Partitions a RingSet into RingSets of connected rings. Rings which share
        /// an Atom, a Bond or three or more atoms with at least on other ring in
        /// the RingSet are considered connected. Thus molecules such as azulene and
        /// indole will return a List with 1 element.
        /// </summary>
        /// <remarks>
        /// Note that an isolated ring is considered to be <i>self-connect</i>. As a result
        /// a molecule such as biphenyl will result in a 2-element List being returned (each
        /// element corresponding to a phenyl ring).
        /// </remarks>
        /// <param name="ringSet">The RingSet to be partitioned</param>
        /// <returns>A <see cref="List{T}"/> of connected RingSets</returns>
        public static IReadOnlyList<IRingSet> PartitionRings(IEnumerable<IRing> ringSet)
        {
            var ringSets = new List<IRingSet>();
            if (!ringSet.Any())
                return ringSets;
            var ring = ringSet.First();
            if (ring == null)
                return ringSets;
            var rs = ring.Builder.NewRingSet();
            foreach (var r in ringSet)
                rs.Add(r);
            do
            {
                ring = rs[0];
                var newRs = ring.Builder.NewRingSet();
                newRs.Add(ring);
                ringSets.Add(WalkRingSystem(rs, ring, newRs));
            } while (rs.Count > 0);

            return ringSets;
        }

        /// <summary>
        /// Converts a RingSet to an AtomContainer.
        /// </summary>
        /// <param name="ringSet">The RingSet to be converted.</param>
        /// <returns>The AtomContainer containing the bonds and atoms of the ringSet.</returns>
        public static IAtomContainer ConvertToAtomContainer(IRingSet ringSet)
        {
            var ring = ringSet[0];
            if (ring == null)
                return null;
            var ac = ring.Builder.NewAtomContainer();
            for (int i = 0; i < ringSet.Count; i++)
            {
                ring = ringSet[i];
                for (int r = 0; r < ring.Bonds.Count; r++)
                {
                    var bond = ring.Bonds[r];
                    if (!ac.Contains(bond))
                    {
                        for (int j = 0; j < bond.Atoms.Count; j++)
                        {
                            ac.Atoms.Add(bond.Atoms[j]);
                        }
                        ac.Bonds.Add(bond);
                    }
                }
            }
            return ac;
        }

        /// <summary>
        /// Perform a walk in the given RingSet, starting at a given Ring and
        /// recursively searching for other Rings connected to this ring. By doing
        /// this it finds all rings in the RingSet connected to the start ring,
        /// putting them in newRs, and removing them from rs.
        /// </summary>
        /// <param name="rs">The RingSet to be searched</param>
        /// <param name="ring">The ring to start with</param>
        /// <param name="newRs">The RingSet containing all Rings connected to ring</param>
        /// <returns>newRs The RingSet containing all Rings connected to ring</returns>
        private static IRingSet WalkRingSystem(IRingSet rs, IRing ring, IRingSet newRs)
        {
            IRing tempRing;
            var tempRings = rs.GetConnectedRings(ring);
            rs.Remove(ring);
            foreach (var container in tempRings)
            {
                tempRing = (IRing)container;
                if (!newRs.Contains(tempRing))
                {
                    newRs.Add(tempRing);
                    newRs.Add(WalkRingSystem(rs, tempRing, newRs));
                }
            }
            return newRs;
        }
    }
}
