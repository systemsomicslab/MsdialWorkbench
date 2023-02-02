/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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

using System.Collections.Generic;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// </summary>
    /// <seealso cref="ChemModelManipulator"/>
    // @cdk.module standard
    public static class MoleculeSetManipulator
    {
        public static int GetAtomCount(IChemObjectSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetAtomCount(set);
        }

        public static int GetBondCount(IChemObjectSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetBondCount(set);
        }

        public static void RemoveAtomAndConnectedElectronContainers(IChemObjectSet<IAtomContainer> set, IAtom atom)
        {
            AtomContainerSetManipulator.RemoveAtomAndConnectedElectronContainers(set, atom);
        }

        public static void RemoveElectronContainer(IChemObjectSet<IAtomContainer> set, IElectronContainer electrons)
        {
            AtomContainerSetManipulator.RemoveElectronContainer(set, electrons);
        }

        /// <summary>
        /// Returns all the AtomContainer's of a MoleculeSet.
        /// </summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <returns>a list containing individual IAtomContainer's</returns>
        public static IEnumerable<IAtomContainer> GetAllAtomContainers(IEnumerable<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetAllAtomContainers(set);
        }

        /// <summary>
        /// </summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <seealso cref="AtomContainerSetManipulator"/>
        /// <returns>The total charge on the collection of molecules</returns>
        public static double GetTotalCharge(IChemObjectSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetTotalCharge(set);
        }

        /// <summary>
        /// </summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <seealso cref="AtomContainerSetManipulator"/>
        /// <returns>The total formal charge on the collection of molecules</returns>
        public static double GetTotalFormalCharge(IChemObjectSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetTotalFormalCharge(set);
        }

        /// <summary>
        /// </summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <seealso cref="AtomContainerSetManipulator"/>
        /// <returns>the total implicit hydrogen count on the collection of molecules</returns>
        public static int GetTotalHydrogenCount(IChemObjectSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetTotalHydrogenCount(set);
        }

        public static IEnumerable<string> GetAllIDs(IChemObjectSet<IAtomContainer> set)
        {
            // the ID is set in AtomContainerSetManipulator.GetAllIDs()
            foreach (var id in AtomContainerSetManipulator.GetAllIDs(set))
                yield return id;
            yield break;
        }

        public static void SetAtomProperties(IChemObjectSet<IAtomContainer> set, string propKey, object propVal)
        {
            AtomContainerSetManipulator.SetAtomProperties(set, propKey, propVal);
        }

        public static IAtomContainer GetRelevantAtomContainer(IChemObjectSet<IAtomContainer> moleculeSet, IAtom atom)
        {
            return AtomContainerSetManipulator.GetRelevantAtomContainer(moleculeSet, atom);
        }

        public static IAtomContainer GetRelevantAtomContainer(IChemObjectSet<IAtomContainer> moleculeSet, IBond bond)
        {
            return AtomContainerSetManipulator.GetRelevantAtomContainer(moleculeSet, bond);
        }

        public static IEnumerable<IChemObject> GetAllChemObjects(IChemObjectSet<IAtomContainer> set)
        {
            return AtomContainerSetManipulator.GetAllChemObjects(set);
        }
    }
}
