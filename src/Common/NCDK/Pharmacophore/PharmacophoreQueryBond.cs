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
using System.Globalization;
using System.Text;

namespace NCDK.Pharmacophore
{
    /// <summary>
    /// Represents a pharmacophore query distance constraint.
    /// </summary>
    /// <seealso cref="PharmacophoreQueryAtom"/>
    /// <seealso cref="PharmacophoreMatcher"/>
    /// <seealso cref="QueryAtomContainer"/>
    // @author Rajarshi Guha
    // @cdk.module pcore
    // @cdk.keyword pharmacophore
    // @cdk.keyword 3D isomorphism
    public class PharmacophoreQueryBond : Silent.Bond, IQueryBond
    {
        private double upper;
        private double lower;

        public PharmacophoreQueryBond() { }

        /// <summary>
        /// Create a query distance constraint between two query groups.
        /// </summary>
        /// <remarks>
        /// Note that the distance is only considered upto 2 decimal places.
        /// </remarks>
        /// <param name="atom1">The first pharmacophore group</param>
        /// <param name="atom2">The second pharmacophore group</param>
        /// <param name="lower">The lower bound of the distance between the two groups</param>
        /// <param name="upper">The upper bound of the distance between the two groups</param>
         /// <seealso cref="PharmacophoreQueryBond(PharmacophoreQueryAtom,PharmacophoreQueryAtom,double)"/>
        public PharmacophoreQueryBond(PharmacophoreQueryAtom atom1, PharmacophoreQueryAtom atom2, double lower, double upper)
            : base(atom1, atom2)
        {
            this.upper = Round(upper, 2);
            this.lower = Round(lower, 2);
        }

        /// <summary>
        /// Create a query distance constraint between two query groups.
        /// <para>
        /// This constructor allows you to define a query distance constraint
        /// such that the distance between the two query groups is exact
        /// (i.e., not a range).
        /// </para>
        /// </summary>
        /// <remarks>
        /// Note that the distance is only considered upto 2 decimal places.
        /// </remarks>
        /// <param name="atom1">The first pharmacophore group</param>
        /// <param name="atom2">The second pharmacophore group</param>
        /// <param name="distance">The exact distance between the two groups</param>
         /// <seealso cref="PharmacophoreQueryBond(PharmacophoreQueryAtom, PharmacophoreQueryAtom, double, double)"/>
        public PharmacophoreQueryBond(PharmacophoreQueryAtom atom1, PharmacophoreQueryAtom atom2, double distance)
            : base(atom1, atom2)
        {
            this.upper = Round(distance, 2);
            this.lower = Round(distance, 2);
        }

        /// <summary>
        /// Checks whether the query distance constraint matches a target distance.
        /// <para>
        /// This method checks whether a query constraint is satisfied by an observed
        /// distance (represented by a <see cref="PharmacophoreBond"/> in the target molecule.
        /// Note that distance are compared upto 2 decimal places.
        /// </para>
        /// </summary>
        /// <param name="bond">The distance relationship in a target molecule</param>
        /// <returns>true if the target distance lies within the range of the query constraint</returns>
        public bool Matches(IBond bond)
        {
            bond = BondRef.Deref(bond);
            if (bond is PharmacophoreBond pbond)
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
        /// string representation of a distance constraint.
        /// </summary>
        /// <returns>string representation of a distance constraint</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"DC::{Atoms[0]}::{Atoms[1]}::[{GetLower().ToString("F1", NumberFormatInfo.InvariantInfo)} - {GetUpper().ToString("F1", NumberFormatInfo.InvariantInfo)}] ");
            return sb.ToString();
        }
    }
}
