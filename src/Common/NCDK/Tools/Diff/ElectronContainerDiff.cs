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
    /// Compares two <see cref="IChemObject"/> classes.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    public static class ElectronContainerDiff
    {
        /// <summary>
        /// Compare two <see cref="IChemObject"/> classes and return the difference as a <see cref="string"/>.
        ///
        /// <param name="first">the first of the two classes to compare</param>
        /// <param name="second">the second of the two classes to compare</param>
        /// <returns>a <see cref="string"/> representation of the difference between the first and second <see cref="IChemObject"/>.</returns>
        /// </summary>
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
            if (!(first is IElectronContainer && second is IElectronContainer))
            {
                return null;
            }
            var firstEC = (IElectronContainer)first;
            var secondEC = (IElectronContainer)second;
            var totalDiff = new ChemObjectDifference("ElectronContainerDiff");
            totalDiff.AddChild(IntegerDifference.Construct("eCount", firstEC.ElectronCount,
                    secondEC.ElectronCount));
            totalDiff.AddChild(ChemObjectDiff.Difference(first, second));
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
