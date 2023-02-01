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
    /// Compares two <see cref="IAtom"/> classes.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    public static class AtomDiff
    {
        /// <summary>
        /// Compare two <see cref="IChemObject"/> classes and return the difference as a <see cref="string"/>.
        /// </summary>
        /// <param name="first">the first of the two classes to compare</param>
        /// <param name="second">the second of the two classes to compare</param>
        /// <returns>a <see cref="string"/> representation of the difference between the first and second <see cref="IChemObject"/>.</returns>
        public static string Diff(IChemObject first, IChemObject second)
        {
            var diff = Difference(first, second);
            if (diff == null)
            {
                return "";
            }
            else
            {
                return diff.ToString();
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
            if (!(first is IAtom && second is IAtom))
            {
                return null;
            }
            var firstElem = (IAtom)first;
            var secondElem = (IAtom)second;
            var totalDiff = new ChemObjectDifference("AtomDiff");
            totalDiff.AddChild(IntegerDifference.Construct("H", firstElem.ImplicitHydrogenCount,
                    secondElem.ImplicitHydrogenCount));
            totalDiff
                    .AddChild(IntegerDifference.Construct("SP", (int)firstElem.StereoParity, (int)secondElem.StereoParity));
            totalDiff.AddChild(Point2DDifference.Construct("2D", firstElem.Point2D, secondElem.Point2D));
            totalDiff.AddChild(Point3DDifference.Construct("3D", firstElem.Point3D, secondElem.Point3D));
            totalDiff.AddChild(Point3DDifference.Construct("F3D", firstElem.FractionalPoint3D,
                    secondElem.FractionalPoint3D));
            totalDiff.AddChild(DoubleDifference.Construct("C", firstElem.Charge, secondElem.Charge));
            totalDiff.AddChild(AtomTypeDiff.Difference(first, second));
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
