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

using NCDK.Numerics;

namespace NCDK.Pharmacophore
{
    /// <summary>
    /// Represents a distance relationship between two pharmacophore groups.
    /// </summary>
    /// <seealso cref="PharmacophoreAtom"/>
    // @author Rajarshi Guha
    // @cdk.module pcore
    // @cdk.keyword pharmacophore
    // @cdk.keyword 3D isomorphism
    public class PharmacophoreBond : Silent.Bond
    {
        /// <summary>
        /// Create a pharmacophore distance constraint.
        /// </summary>
        /// <param name="patom1">The first pharmacophore group</param>
        /// <param name="patom2">The second pharmacophore group</param>
        public PharmacophoreBond(PharmacophoreAtom patom1, PharmacophoreAtom patom2)
            : base(patom1, patom2)
        {
        }

        public static PharmacophoreBond Get(IBond bond)
        {
            if (bond is PharmacophoreBond)
                return (PharmacophoreBond)bond;
            if (bond is BondRef)
                return Get(((BondRef)bond).Deref());
            return null;
        }

        /// <summary>
        /// The distance between the two pharmacophore groups that make up the constraint.
        /// </summary>
        /// <returns>The distance between the two groups</returns>
        public double BondLength
        {
            get
            {
                PharmacophoreAtom atom1 = PharmacophoreAtom.Get(Atoms[0]);
                PharmacophoreAtom atom2 = PharmacophoreAtom.Get(Atoms[1]);
                return Vector3.Distance(atom1.Point3D.Value, atom2.Point3D.Value);
            }
        }
    }
}

