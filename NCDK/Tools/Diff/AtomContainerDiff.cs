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
using System.Linq;

namespace NCDK.Tools.Diff
{
    /// <summary>
    /// Compares two <see cref="IAtomContainer"/> classes.
    /// </summary>
    // @author     egonw
    // @cdk.module diff
    public static class AtomContainerDiff
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
            if (!(first is IAtomContainer && second is IAtomContainer))
            {
                return null;
            }
            var firstAC = (IAtomContainer)first;
            var secondAC = (IAtomContainer)second;
            var totalDiff = new ChemObjectDifference("AtomContainerDiff");
            totalDiff.AddChild(IntegerDifference.Construct("atomCount", firstAC.Atoms.Count, secondAC.Atoms.Count));
            if (firstAC.Atoms.Count == secondAC.Atoms.Count)
            {
                for (int i = 0; i < firstAC.Atoms.Count; i++)
                {
                    totalDiff.AddChild(AtomDiff.Difference(firstAC.Atoms[i], secondAC.Atoms[i]));
                }
            }
            totalDiff.AddChild(IntegerDifference.Construct("electronContainerCount", firstAC.GetElectronContainers().Count(),
                    secondAC.GetElectronContainers().Count()));
            if (firstAC.GetElectronContainers().Count() == secondAC.GetElectronContainers().Count())
            {
                for (int i = 0; i < firstAC.GetElectronContainers().Count(); i++)
                {
                    if (firstAC.GetElectronContainers().ElementAt(i) is IBond
                            && secondAC.GetElectronContainers().ElementAt(i) is IBond)
                    {
                        totalDiff.AddChild(BondDiff.Difference(firstAC.GetElectronContainers().ElementAt(i),
                                secondAC.GetElectronContainers().ElementAt(i)));
                    }
                    else if (firstAC.GetElectronContainers().ElementAt(i) is ILonePair
                          && secondAC.GetElectronContainers().ElementAt(i) is ILonePair)
                    {
                        totalDiff.AddChild(LonePairDiff.Difference(firstAC.GetElectronContainers().ElementAt(i),
                                secondAC.GetElectronContainers().ElementAt(i)));
                    }
                    else if (firstAC.GetElectronContainers().ElementAt(i) is ISingleElectron
                          && secondAC.GetElectronContainers().ElementAt(i) is ISingleElectron)
                    {
                        totalDiff.AddChild(SingleElectronDiff.Difference(firstAC.GetElectronContainers().ElementAt(i),
                                secondAC.GetElectronContainers().ElementAt(i)));
                    }
                    else
                    {
                        totalDiff.AddChild(ElectronContainerDiff.Difference(firstAC.GetElectronContainers().ElementAt(i),
                                secondAC.GetElectronContainers().ElementAt(i)));
                    }
                }
            }
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
