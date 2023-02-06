/* Copyright (C) 2006-2007  The Chemistry Development Kit (CDK) project
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

namespace NCDK.Features
{
    /// <summary>
    /// Utility that helps determine which data features are present.
    /// </summary>
    /// <seealso cref="Tools.DataFeatures"/>
    // @author egonw
    public static class MoleculeFeaturesTool
    {
        public static bool HasPartialCharges(IAtomContainer molecule)
        {
            foreach (var atom in molecule.Atoms)
                if (atom.Charge != 0.0000)
                    return true;
            return false;
        }

        public static bool HasFormalCharges(IAtomContainer molecule)
        {
            foreach (var atom in molecule.Atoms)
                if (atom.FormalCharge != 0)
                    return true;
            return false;
        }

        public static bool HasElementSymbols(IAtomContainer molecule)
        {
            foreach (var atom in molecule.Atoms)
            {
                if (atom.Symbol != null && atom.Symbol.Length > 0)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Checks whether all bonds have exactly two atoms.
        /// </summary>
        public static bool HasGraphRepresentation(IAtomContainer molecule)
        {
            foreach (var bond in molecule.Bonds)
                if (bond.Atoms.Count != 2)
                    return false;
            return true;
        }
    }
}
