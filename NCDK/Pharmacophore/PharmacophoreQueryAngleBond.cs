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

using NCDK.Isomorphisms.Matchers;
using System;
using System.Text;

namespace NCDK.Pharmacophore
{
    /// <summary>
    /// Represents a pharmacophore query angle constraint.
    /// </summary>
    /// <seealso cref="PharmacophoreQueryAtom"/>
    /// <seealso cref="PharmacophoreMatcher"/>
    /// <seealso cref="QueryAtomContainer"/>
    // @author Rajarshi Guha
    // @cdk.module pcore
    // @cdk.keyword pharmacophore
    // @cdk.keyword 3D isomorphism
    public class PharmacophoreQueryAngleBond : Silent.Bond, IQueryBond
    {
        private double upper;
        private double lower;

        public PharmacophoreQueryAngleBond() { }

        /// <summary>
        /// Create a query angle constraint between three query groups.
        /// </summary>
        /// <remarks>
        /// Note that the angle is only considered upto 2 decimal places.
        /// </remarks>
        /// <param name="atom1">The first pharmacophore group</param>
        /// <param name="atom2">The second pharmacophore group</param>
        /// <param name="atom3">The third pharmacophore group</param>
        /// <param name="lower">The lower bound of the angle between the three groups</param>
        /// <param name="upper">The upper bound of the angle between the three groups</param>
        public PharmacophoreQueryAngleBond(PharmacophoreQueryAtom atom1, PharmacophoreQueryAtom atom2,
                PharmacophoreQueryAtom atom3, double lower, double upper)
            : base(new IAtom[] { atom1, atom2, atom3 })
        {
            this.upper = Round(upper, 2);
            this.lower = Round(lower, 2);
        }

        /// <summary>
        /// Create a query angle constraint between three query groups.
        /// <para>
        /// This constructor allows you to define a query angle constraint
        /// such that the angle between the three query groups is exact
        /// (i.e., not a range).
        /// </para>
        /// </summary>
        /// <remarks>
        /// Note that the angle is only considered upto 2 decimal places.
        /// </remarks>
        /// <param name="atom1">The first pharmacophore group</param>
        /// <param name="atom2">The second pharmacophore group</param>
        /// <param name="atom3">The third pharmacophore group</param>
        /// <param name="angle">The exact angle between the two groups</param>
        public PharmacophoreQueryAngleBond(PharmacophoreQueryAtom atom1, PharmacophoreQueryAtom atom2,
                PharmacophoreQueryAtom atom3, double angle)
            : base(new PharmacophoreQueryAtom[] { atom1, atom2, atom3 })
        {
            this.upper = Round(angle, 2);
            this.lower = Round(angle, 2);
        }

        /// <summary>
        /// Checks whether the query angle constraint matches a target distance.
        /// <para>
        /// This method checks whether a query constraint is satisfied by an observed
        /// angle (represented by a <see cref="PharmacophoreAngleBond"/> in the target molecule.
        /// </para>
        /// </summary>
        /// <remarks>
        /// Note that angles are compared upto 2 decimal places.
        /// </remarks>
        /// <param name="bond">The angle relationship in a target molecule</param>
        /// <returns>true if the target angle lies within the range of the query constraint</returns>
        public bool Matches(IBond bond)
        {
            bond = BondRef.Deref(bond);
            if (bond is PharmacophoreAngleBond pbond)
            {
                double bondLength = Round(pbond.BondLength, 2);
                return bondLength >= lower && bondLength <= upper;
            }
            else
                return false;
        }

        public double GetUpper()
        {
            return upper;
        }

        public double GetLower()
        {
            return lower;
        }

        private static double Round(double val, int places)
        {
            long factor = (long)Math.Pow(10, places);
            val = val * factor;
            long tmp = (long)Math.Round(val);
            return (double)tmp / factor;
        }

        /// <summary>
        /// string representation of an angle constraint.
        /// </summary>
        /// <returns>string representation of and angle constraint</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("AC::").Append(Atoms[0]).Append("::").Append(Atoms[1]).Append("::").Append(Atoms[2]);
            sb.Append("::[").Append(GetLower()).Append(" - ").Append(GetUpper()).Append("] ");
            return sb.ToString();
        }
    }
}
