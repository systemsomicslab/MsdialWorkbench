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
using System;

namespace NCDK.Pharmacophore
{
    /// <summary>
    /// Represents an angle relationship between three pharmacophore groups.
    /// </summary>
    /// <seealso cref="PharmacophoreAtom"/>
    // @author Rajarshi Guha
    // @cdk.module pcore
    // @cdk.keyword pharmacophore
    // @cdk.keyword 3D isomorphism
    public class PharmacophoreAngleBond : Silent.Bond
    {
        /// <summary>
        /// Create a pharmacophore distance constraint.
        /// </summary>
        /// <param name="patom1">The first pharmacophore group</param>
        /// <param name="patom2">The second pharmacophore group</param>
        /// <param name="patom3">The third pharmacophore group</param>
        public PharmacophoreAngleBond(PharmacophoreAtom patom1, PharmacophoreAtom patom2, PharmacophoreAtom patom3)
            : base(new PharmacophoreAtom[] { patom1, patom2, patom3 })
        { }

        /// <summary>
        /// The angle between the three pharmacophore groups that make up the constraint.
        /// </summary>
        /// <returns>The angle in degrees between the two groups</returns>
        public double BondLength
        {
            get
            {
                double epsilon = 1e-3;
                PharmacophoreAtom atom1 = (PharmacophoreAtom)Atoms[0];
                PharmacophoreAtom atom2 = (PharmacophoreAtom)Atoms[1];
                PharmacophoreAtom atom3 = (PharmacophoreAtom)Atoms[2];

                double a2 = Vector3.DistanceSquared(atom3.Point3D.Value, atom1.Point3D.Value);
                double b2 = Vector3.DistanceSquared(atom3.Point3D.Value, atom2.Point3D.Value);
                double c2 = Vector3.DistanceSquared(atom2.Point3D.Value, atom1.Point3D.Value);

                double cosangle = (b2 + c2 - a2) / (2 * Math.Sqrt(b2) * Math.Sqrt(c2));
                if (-1.0 - epsilon < cosangle && -1.0 + epsilon > cosangle)
                    return 180.0;
                if (1.0 - epsilon < cosangle && 1.0 + epsilon > cosangle)
                    return 0.0;

                return Math.Acos(cosangle) * 180.0 / Math.PI;
            }
        }
    }
}
