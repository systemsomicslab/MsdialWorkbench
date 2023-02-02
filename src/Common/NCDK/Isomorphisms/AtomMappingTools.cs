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

using NCDK.Isomorphisms.MCSS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Isomorphisms
{
    // @cdk.module    standard
    public static class AtomMappingTools
    {
        /// <summary>
        /// Returns a Dictionary with the AtomNumbers, the first number corresponds to the first (or the largest
        /// AtomContainer) atomContainer.
        /// </summary>
        /// <remarks>Only for similar and aligned molecules with coordinates!</remarks>
        /// <param name="firstAtomContainer">the (largest) first aligned AtomContainer which is the reference</param>
        /// <param name="secondAtomContainer">the second aligned AtomContainer</param>
        /// <param name="mappedAtoms"></param>
        /// <exception cref="CDKException">if there is an error in the UniversalIsomorphismTester</exception>
        public static void MapAtomsOfAlignedStructures(IAtomContainer firstAtomContainer, IAtomContainer secondAtomContainer, IDictionary<int, int> mappedAtoms)
        {
            // Dictionary atoms onto each other
            if (firstAtomContainer.Atoms.Count < 1 & secondAtomContainer.Atoms.Count < 1)
            {
                return;
            }
            try
            {
                var list = new UniversalIsomorphismTester().GetSubgraphAtomsMap(firstAtomContainer, secondAtomContainer);
                for (int i = 0; i < list.Count; i++)
                {
                    var map = list[i];
                    var atom1 = firstAtomContainer.Atoms[map.Id1];
                    var atom2 = secondAtomContainer.Atoms[map.Id2];
                    if (CheckAtomMapping(firstAtomContainer, secondAtomContainer, firstAtomContainer.Atoms.IndexOf(atom1),
                            secondAtomContainer.Atoms.IndexOf(atom2)))
                    {
                        mappedAtoms.Add(firstAtomContainer.Atoms.IndexOf(atom1), secondAtomContainer.Atoms.IndexOf(atom2));
                    }
                    else
                    {
                        Trace.TraceError("Error: Atoms are not similar !!");
                    }
                }
            }
            catch (CDKException e)
            {
                throw new CDKException($"Error in UniversalIsomorphismTester due to: {e.Message}", e);
            }
        }

        private static bool CheckAtomMapping(IAtomContainer firstAC, IAtomContainer secondAC, int posFirstAtom, int posSecondAtom)
        {
            var firstAtom = firstAC.Atoms[posFirstAtom];
            var secondAtom = secondAC.Atoms[posSecondAtom];
            // XXX: floating point comparison!
            if (firstAtom.Symbol.Equals(secondAtom.Symbol, StringComparison.Ordinal)
             && firstAC.GetConnectedAtoms(firstAtom).Count() == secondAC.GetConnectedAtoms(secondAtom).Count()
             && firstAtom.BondOrderSum == secondAtom.BondOrderSum
             && firstAtom.MaxBondOrder == secondAtom.MaxBondOrder)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
