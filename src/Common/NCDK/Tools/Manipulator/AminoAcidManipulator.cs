/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// Class with convenience methods that provide methods to manipulate
    /// AminoAcid's.
    /// </summary>
    // @cdk.module  standard
    // @author      Egon Willighagen
    // @cdk.created 2005-08-19
    public static class AminoAcidManipulator
    {
        /// <summary>
        /// Removes the singly bonded oxygen from the acid group of the AminoAcid.
        /// </summary>
        /// <param name="acid">AminoAcid from which to remove the oxygen</param>
        /// <exception cref="CDKException">when the C-terminus is not defined for the given AminoAcid</exception>
        public static void RemoveAcidicOxygen(IAminoAcid acid)
        {
            if (acid.CTerminus == null)
                throw new CDKException("Cannot remove oxygen: C-terminus is not defined!");

            var atomsToRemove = new List<IAtom>();
            // ok, look for the oxygen which is singly bonded
            foreach (var bond in acid.GetConnectedBonds(acid.CTerminus))
            {
                if (bond.Order == BondOrder.Single)
                {
                    for (int j = 0; j < bond.Atoms.Count; j++)
                    {
                        if (bond.Atoms[j].AtomicNumber.Equals(AtomicNumbers.O))
                        {
                            // yes, we found a singly bonded oxygen!
                            atomsToRemove.Add(bond.Atoms[j]);
                        }
                    }
                }
            }
            foreach (var atom in atomsToRemove)
                acid.RemoveAtom(atom);
        }

        /// <summary>
        /// Adds the singly bonded oxygen from the acid group of the AminoAcid.
        /// </summary>
        /// <param name="acid">AminoAcid to which to add the oxygen</param>
        /// <exception cref="CDKException">when the C-terminus is not defined for the given AminoAcid</exception>
        public static void AddAcidicOxygen(IAminoAcid acid)
        {
            if (acid.CTerminus == null)
                throw new CDKException("Cannot add oxygen: C-terminus is not defined!");

            var acidicOxygen = acid.Builder.NewAtom("O");
            acid.Atoms.Add(acidicOxygen);
            acid.Bonds.Add(acid.Builder.NewBond(acid.CTerminus, acidicOxygen, BondOrder.Single));
        }
    }
}
