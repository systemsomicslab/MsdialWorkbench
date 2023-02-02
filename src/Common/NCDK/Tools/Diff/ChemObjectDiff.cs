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
    /// Compares two <see cref="IChemObject"/> classes.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    public static class ChemObjectDiff
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
            if (!(first is IChemObject && second is IChemObject))
            {
                return null;
            }
            var firstElem = (IChemObject)first;
            var secondElem = (IChemObject)second;
            var coDiff = new ChemObjectDifference("ChemObjectDiff");
            // Compare flags 
            bool[] firstFlags = new[] { firstElem.IsVisited };
            bool[] secondFlags = new[] { secondElem.IsVisited };
            coDiff.AddChild(BooleanArrayDifference.Construct("flag", firstFlags, secondFlags));
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
