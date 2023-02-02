/* Copyright (C) 2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using NCDK.Tools.Diff.Tree;

namespace NCDK.Tools.Diff
{
    /// <summary>
    /// Compares two <see cref="IAtomType"/> classes.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    public static class AtomTypeDiff
    {
        /// <summary>
        /// Compare two <see cref="IChemObject"/> classes and return the difference as a <see cref="string"/>.
        /// </summary>
        /// <param name="first">the first of the two classes to compare</param>
        /// <param name="second">the second of the two classes to compare</param>
        /// <returns>a <see cref="string"/> representation of the difference between the first and second <see cref="IChemObject"/>.</returns>
        public static string Diff(IChemObject first, IChemObject second)
        {
            var difference = Difference(first, second);
            if (difference == null)
            {
                return "";
            }
            else
            {
                return difference.ToString();
            }
        }

        /// <summary>
        /// Compare two <see cref="IChemObject"/> classes and return the difference as an <see cref="IDifference"/>.
        /// </summary>
        /// <param name="first">the first of the two classes to compare</param>
        /// <param name="second">the second of the two classes to compare</param>
        /// <returns>an <see cref="IDifference"/> representation of the difference between the first and second <see cref="IChemObject"/>.</returns>
        public static IDifference Difference(IChemObject first, IChemObject second)
        {
            if (!(first is IAtomType && second is IAtomType))
            {
                return null;
            }
            var firstElem = (IAtomType)first;
            var secondElem = (IAtomType)second;
            var totalDiff = new ChemObjectDifference("AtomTypeDiff");
            totalDiff.AddChild(StringDifference.Construct("N", firstElem.AtomTypeName, secondElem.AtomTypeName));
            totalDiff.AddChild(BondOrderDifference.Construct("MBO", firstElem.MaxBondOrder,
                    secondElem.MaxBondOrder));
            totalDiff
                    .AddChild(DoubleDifference.Construct("BOS", firstElem.BondOrderSum, secondElem.BondOrderSum));
            totalDiff
                    .AddChild(IntegerDifference.Construct("FC", firstElem.FormalCharge, secondElem.FormalCharge));
            totalDiff.AddChild(AtomTypeHybridizationDifference.Construct("H", firstElem.Hybridization,
                    secondElem.Hybridization));
            totalDiff.AddChild(IntegerDifference.Construct("NC", firstElem.FormalNeighbourCount,
                    secondElem.FormalNeighbourCount));
            totalDiff.AddChild(DoubleDifference.Construct("CR", firstElem.CovalentRadius,
                    secondElem.CovalentRadius));
            totalDiff.AddChild(IntegerDifference.Construct("V", firstElem.Valency, secondElem.Valency));
            totalDiff.AddChild(IsotopeDiff.Difference(first, second));
            if (totalDiff.ChildCount() > 0)
            {
                return totalDiff;
            }
            else
            {
                return null;
            }
        }
    }
}
