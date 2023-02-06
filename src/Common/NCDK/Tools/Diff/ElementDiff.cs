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

namespace NCDK.Tools.Diff.Tree
{
    /// <summary>
    /// Compares two <see cref="IElement"/> classes.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    public static class ElementDiff
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
        ///
        /// <param name="first">the first of the two classes to compare</param>
        /// <param name="second">the second of the two classes to compare</param>
        /// <returns>an <see cref="IDifference"/> representation of the difference between the first and second <see cref="IChemObject"/>.</returns>
        /// </summary>
        public static IDifference Difference(IChemObject first, IChemObject second)
        {
            if (!(first is IElement && second is IElement))
            {
                return null;
            }
            var firstElem = (IElement)first;
            var secondElem = (IElement)second;
            var coDiff = new ChemObjectDifference("ElementDiff");
            coDiff.AddChild(StringDifference.Construct("S", firstElem.Symbol, secondElem.Symbol));
            coDiff.AddChild(StringDifference.Construct("ID", firstElem.Id, secondElem.Id));
            coDiff.AddChild(IntegerDifference.Construct("AN", firstElem.AtomicNumber, secondElem.AtomicNumber));
            coDiff.AddChild(ChemObjectDiff.Difference(first, second));
            if (coDiff.ChildCount() > 0)
            {
                return coDiff;
            }
            else
            {
                return null;
            }
        }
    }
}