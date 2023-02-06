/* Copyright (C) 2004-2008  Rajarshi Guha <rajarshi.guha@gmail.com>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;
using NCDK.Numerics;
using System;

namespace NCDK.Pharmacophore
{
    /// <summary>
    /// A representation of a pharmacophore group.
    /// <para>
    /// In general this class is used internally for pharmacophore matchin and does not be instantiated
    /// by the user. However after a successful match the user will get access to objects of this class
    /// which match parts of a query.
    /// </para>
    /// <para>
    /// The main features of a pharmacophore group are the SMARTS pattern defining what the group
    /// is meant to identify and the atoms of a molecule that correspond to the SMARTS pattern.
    /// </para>
    /// </summary>
    /// <seealso cref="PharmacophoreMatcher"/>
    /// <seealso cref="PharmacophoreBond"/>
    // @author Rajarshi Guha
    // @cdk.module pcore
    // @cdk.keyword pharmacophore
    // @cdk.keyword 3D isomorphism
    public class PharmacophoreAtom : Silent.Atom
    {
        private int[] matchingAtoms;

        /// <summary>
        /// Create a pharmacophore group.
        /// </summary>
        /// <param name="smarts">The SMARTS pattern for the group</param>
        /// <param name="symbol">The label for this group.</param>
        /// <param name="coordinates">The coordinates for the group. Note that since a pharmacophore group may match
        ///                    multiple atoms (say a c1ccccc1 group), the coordinates for the group are the effective coordinates
        ///                    of all the atoms for the group. In effect this means that for multi-atom groups, the coordinate
        ///                    is simply the mean of the coordinates of the individual atoms for the group.</param>
        public PharmacophoreAtom(string smarts, string symbol, Vector3 coordinates)
        {
            Smarts = smarts;
            this.Symbol = symbol;
            Point3D = coordinates;
        }

        /// <summary>
        /// Create a pharmacophore group.
        /// </summary>
        /// <param name="pharmacophoreAtom">A previously created pharmacophore group</param>
        public PharmacophoreAtom(PharmacophoreAtom pharmacophoreAtom)
        {
            Smarts = pharmacophoreAtom.Smarts;
            this.Symbol = pharmacophoreAtom.Symbol;
            Point3D = pharmacophoreAtom.Point3D;
            if (pharmacophoreAtom.GetMatchingAtoms() != null)
            {
                var indices = pharmacophoreAtom.GetMatchingAtoms();
                matchingAtoms = new int[indices.Length];
                Array.Copy(indices, 0, matchingAtoms, 0, indices.Length);
            }
        }

        public static PharmacophoreAtom Get(IAtom atom)
        {
            if (atom is PharmacophoreAtom)
                return (PharmacophoreAtom)atom;
            if (atom is AtomRef)
                return Get(((AtomRef)atom).Deref());
            return null;
        }

        /// <summary>
        /// The SMARTS for the group.
        /// </summary>
        public string Smarts { get; set; }

        /// <inheritdoc/>
        public override string Symbol { get; set; }

        /// <summary>
        /// Set the atoms of a target molecule that correspond to this group.
        /// <para>
        /// This method is generally only useful in the context of pharmacophore matching
        /// </para>
        /// </summary>
        /// <param name="atomIndices">The indicies of the atoms in a molecule that match the pattern for this group.</param>
        /// <seealso cref="GetMatchingAtoms"/>
        /// <seealso cref="PharmacophoreMatcher"/>
        public void SetMatchingAtoms(int[] atomIndices)
        {
            this.matchingAtoms = new int[atomIndices.Length];
            Array.Copy(atomIndices, 0, this.matchingAtoms, 0, atomIndices.Length);
            Array.Sort(matchingAtoms);
        }

        /// <summary>
        /// Get the atoms of a target molecule that correspond to this group.
        /// <para>
        /// This method is generally only useful in the context of pharmacophore matching
        /// </para>
        /// </summary>
        /// <returns>The indices of the atoms, in a molecule, that match the pattern for this group.</returns>
        /// <seealso cref="SetMatchingAtoms(int[])"/>
        /// <seealso cref="PharmacophoreMatcher"/>
        public int[] GetMatchingAtoms()
        {
            return matchingAtoms;
        }

        public override int GetHashCode()
        {
            int result = Smarts != null ? Smarts.GetHashCode() : 0;
            result = 31 * result + (matchingAtoms != null ? Arrays.GetHashCode(matchingAtoms) : 0);
            return result;
        }

        public override bool Equals(object o)
        {
            if (!(o is PharmacophoreAtom)) return false;

            PharmacophoreAtom that = (PharmacophoreAtom)o;
            return Smarts.Equals(that.Smarts, StringComparison.Ordinal) && Symbol.Equals(that.Symbol, StringComparison.Ordinal)
                    && Point3D.Equals(that.Point3D) && Arrays.AreEqual(this.matchingAtoms, that.matchingAtoms);
        }
    }
}
