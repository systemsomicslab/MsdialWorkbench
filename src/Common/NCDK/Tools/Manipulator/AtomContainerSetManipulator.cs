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

using NCDK.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// </summary>
    /// <seealso cref="ChemModelManipulator"/>
    // @cdk.module standard
    public static class AtomContainerSetManipulator
    {
        public static int GetAtomCount(IEnumerable<IAtomContainer> set)
        {
            int count = 0;
            foreach (var atomContainer in set)
            {
                count += (atomContainer).Atoms.Count;
            }
            return count;
        }

        public static int GetBondCount<T>(IEnumerable<T> set) where T : IAtomContainer
        {
            int count = 0;
            foreach (var atomContainer in set)
            {
                count += (atomContainer).Bonds.Count;
            }
            return count;
        }

        public static void RemoveAtomAndConnectedElectronContainers(IChemObjectSet<IAtomContainer> set, IAtom atom)
        {
            foreach (var atomContainer in set)
            {
                if (atomContainer.Contains(atom))
                {
                    atomContainer.RemoveAtom(atom);
                    var molecules = ConnectivityChecker.PartitionIntoMolecules(atomContainer);
                    if (molecules.Count > 1)
                    {
                        set.Remove(atomContainer);
                        for (int k = 0; k < molecules.Count; k++)
                        {
                            set.Add(molecules[k]);
                        }
                    }
                    return;
                }
            }
        }

        public static void RemoveElectronContainer(IChemObjectSet<IAtomContainer> set, IElectronContainer electrons)
        {
            foreach (var atomContainer in set)
            {
                if (atomContainer.Contains(electrons))
                {
                    atomContainer.Remove(electrons);
                    var molecules = ConnectivityChecker.PartitionIntoMolecules(atomContainer);
                    if (molecules.Count > 1)
                    {
                        set.Remove(atomContainer);
                        for (int k = 0; k < molecules.Count; k++)
                        {
                            set.Add(molecules[k]);
                        }
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Returns all the AtomContainer's of a MoleculeSet.
        /// </summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <returns>A list of individual IAtomContainer's</returns>
        public static IEnumerable<T> GetAllAtomContainers<T>(IEnumerable<T> set) where T : IAtomContainer
        {
            return set;
        }

        /// <summary>
        /// </summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <returns>The summed charges of all atoms in this set.</returns>
        public static double GetTotalCharge(IChemObjectSet<IAtomContainer> set)
        {
            double charge = 0;
            for (int i = 0; i < set.Count; i++)
            {
                int thisCharge = AtomContainerManipulator.GetTotalFormalCharge(set[i]);
                double stoich = set.GetMultiplier(i).Value;
                charge += stoich * thisCharge;
            }
            return charge;
        }

        /// <summary>
        /// </summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <returns>The summed formal charges of all atoms in this set.</returns>
        public static double GetTotalFormalCharge(IChemObjectSet<IAtomContainer> set)
        {
            int charge = 0;
            for (int i = 0; i < set.Count; i++)
            {
                int thisCharge = AtomContainerManipulator.GetTotalFormalCharge(set[i]);
                double stoich = set.GetMultiplier(i).Value;
                charge += (int)(stoich * thisCharge);
            }
            return charge;
        }

        /// <summary>
        /// </summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <returns>The summed implicit hydrogens of all atoms in this set.</returns>
        public static int GetTotalHydrogenCount(IEnumerable<IAtomContainer> set) 
        {
            int hCount = 0;
            foreach (var item in set)
            { 
                hCount += AtomContainerManipulator.GetTotalHydrogenCount(item);
            }
            return hCount;
        }

        public static IEnumerable<string> GetAllIDs<T>(IChemObjectSet<T> set) where T : IAtomContainer
        {
            if (set != null)
            {
                if (set.Id != null)
                    yield return set.Id; 
                foreach (var atomContainer in set)
                {
                    foreach (var id in AtomContainerManipulator.GetAllIDs(atomContainer))
                        yield return id;
                }
            }
            yield break;
        }

        public static void SetAtomProperties<T>(IChemObjectSet<T> set, string propKey, object propVal) where T : IAtomContainer
        {
            if (set != null)
            {
                for (int i = 0; i < set.Count; i++)
                {
                    AtomContainerManipulator.SetAtomProperties(set[i], propKey, propVal);
                }
            }
        }

        public static T GetRelevantAtomContainer<T>(IEnumerable<T> containerSet, IAtom atom) where T: IAtomContainer
        {
            foreach (var atomContainer in containerSet)
            {
                if (atomContainer.Contains(atom))
                {
                    return atomContainer;
                }
            }
            return default(T);
        }

        public static T GetRelevantAtomContainer<T>(IEnumerable<T> containerSet, IBond bond) where T : IAtomContainer
        {
            foreach (var atomContainer in containerSet)
            {
                if (atomContainer.Contains(bond))
                {
                    return atomContainer;
                }
            }
            return default(T);
        }

        /// <summary>
        /// Does not recursively return the contents of the AtomContainer.
        /// </summary>
        /// <param name="set">The collection of IAtomContainer objects</param>
        /// <returns>a list of individual ChemObject's</returns>
        public static IEnumerable<IChemObject> GetAllChemObjects(IChemObjectSet<IAtomContainer> set)
        {
            yield return set;
            foreach (var atomContainer in set)
            {
                yield return atomContainer;
            }
            yield break;
        }

        /// <summary>
        /// <para>Sorts the IAtomContainers in the given IAtomContainerSet by the following
        /// criteria with decreasing priority:</para>
        /// <list type="bullet">
        ///   <item>Compare atom count</item>
        ///   <item>Compare molecular weight (heavy atoms only)</item>
        ///   <item>Compare bond count</item>
        ///   <item>Compare sum of bond orders (heavy atoms only)</item>
        /// </list>
        /// <para>If no difference can be found with the above criteria, the IAtomContainers are
        /// considered equal.</para>
        /// </summary>
        /// <param name="atomContainerSet">The collection of IAtomContainer objects</param>
        public static void Sort<T>(IChemObjectSet<T> atomContainerSet) where T : IAtomContainer
        {
            var atomContainerList = atomContainerSet.ToList();
            atomContainerList.Sort(new AtomContainerComparator<T>());
            atomContainerSet.Clear();
            foreach (var anAtomContainerList in atomContainerList)
                atomContainerSet.Add(anAtomContainerList);
        }

        /// <summary>
        /// Tells if an AtomContainerSet contains at least one AtomContainer with the
        /// same ID as atomContainer. Note this checks Id for equality, not pointers.
        /// </summary>
        /// <param name="id">The IAtomContainer to look for</param>
        /// <param name="atomContainerSet">The collection of IAtomContainer objects</param>
        public static bool ContainsByID(IEnumerable<IAtomContainer> atomContainerSet, string id)
        {
            foreach (var ac in atomContainerSet)
            {
                if (id != null && string.Equals(ac.Id, id, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }
    }
}
