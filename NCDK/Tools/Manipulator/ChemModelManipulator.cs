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
 *  */

using System;
using System.Collections.Generic;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// Class with convenience methods that provide methods from
    /// methods from ChemObjects within the ChemModel. For example:
    /// <code>
    /// ChemModelManipulator.RemoveAtom(chemModel, atom);
    /// </code>
    /// will find the Atom in the model by traversing the ChemModel's
    /// MoleculeSet, Crystal and ReactionSet fields and remove
    /// it with the <see cref="IAtomContainer.RemoveAtom(IAtom)"/> method.
    /// </summary>
    /// <seealso cref="IAtomContainer.RemoveAtom(IAtom)"/>
    // @cdk.module standard
    public static class ChemModelManipulator
    {
        /// <summary>
        /// Get the total number of atoms inside an IChemModel.
        /// </summary>
        /// <param name="chemModel">The IChemModel object.</param>
        /// <returns>The number of Atom object inside.</returns>
        public static int GetAtomCount(IChemModel chemModel)
        {
            int count = 0;
            var crystal = chemModel.Crystal;
            if (crystal != null)
            {
                count += crystal.Atoms.Count;
            }
            var moleculeSet = chemModel.MoleculeSet;
            if (moleculeSet != null)
            {
                count += MoleculeSetManipulator.GetAtomCount(moleculeSet);
            }
            var reactionSet = chemModel.ReactionSet;
            if (reactionSet != null)
            {
                count += ReactionSetManipulator.GetAtomCount(reactionSet);
            }
            return count;
        }

        /// <summary>
        /// Get the total number of bonds inside an IChemModel.
        /// </summary>
        /// <param name="chemModel">The IChemModel object.</param>
        /// <returns>The number of Bond object inside.</returns>
        public static int GetBondCount(IChemModel chemModel)
        {
            int count = 0;
            var crystal = chemModel.Crystal;
            if (crystal != null)
            {
                count += crystal.Bonds.Count;
            }
            var moleculeSet = chemModel.MoleculeSet;
            if (moleculeSet != null)
            {
                count += MoleculeSetManipulator.GetBondCount(moleculeSet);
            }
            var reactionSet = chemModel.ReactionSet;
            if (reactionSet != null)
            {
                count += ReactionSetManipulator.GetBondCount(reactionSet);
            }
            return count;
        }

        /// <summary>
        /// Remove an Atom and the connected ElectronContainers from all AtomContainers
        /// inside an IChemModel.
        /// </summary>
        /// <param name="chemModel">The IChemModel object.</param>
        /// <param name="atom">The Atom object to remove.</param>
        public static void RemoveAtomAndConnectedElectronContainers(IChemModel chemModel, IAtom atom)
        {
            var crystal = chemModel.Crystal;
            if (crystal != null)
            {
                if (crystal.Contains(atom))
                {
                    crystal.RemoveAtom(atom);
                }
                return;
            }
            var moleculeSet = chemModel.MoleculeSet;
            if (moleculeSet != null)
            {
                MoleculeSetManipulator.RemoveAtomAndConnectedElectronContainers(moleculeSet, atom);
            }
            var reactionSet = chemModel.ReactionSet;
            if (reactionSet != null)
            {
                ReactionSetManipulator.RemoveAtomAndConnectedElectronContainers(reactionSet, atom);
            }
        }

        /// <summary>
        /// Remove an ElectronContainer from all AtomContainers
        /// inside an IChemModel.
        /// </summary>
        /// <param name="chemModel">The IChemModel object.</param>
        /// <param name="electrons">The ElectronContainer to remove.</param>
        public static void RemoveElectronContainer(IChemModel chemModel, IElectronContainer electrons)
        {
            var crystal = chemModel.Crystal;
            if (crystal != null)
            {
                if (crystal.Contains(electrons))
                {
                    crystal.Remove(electrons);
                }
                return;
            }
            var moleculeSet = chemModel.MoleculeSet;
            if (moleculeSet != null)
            {
                MoleculeSetManipulator.RemoveElectronContainer(moleculeSet, electrons);
            }
            var reactionSet = chemModel.ReactionSet;
            if (reactionSet != null)
            {
                ReactionSetManipulator.RemoveElectronContainer(reactionSet, electrons);
            }
        }

        /// <summary>
        /// Adds a new Molecule to the MoleculeSet inside a given ChemModel.
        /// Creates a MoleculeSet if none is contained.
        /// </summary>
        /// <param name="chemModel">The ChemModel object.</param>
        /// <returns>The created Molecule object.</returns>
        public static IAtomContainer CreateNewMolecule(IChemModel chemModel)
        {
            // Add a new molecule either the set of molecules
            IAtomContainer molecule = chemModel.Builder.NewAtomContainer();
            if (chemModel.MoleculeSet != null)
            {
                IChemObjectSet<IAtomContainer> moleculeSet = chemModel.MoleculeSet;
                for (int i = 0; i < moleculeSet.Count; i++)
                {
                    if (moleculeSet[i].Atoms.Count == 0)
                    {
                        moleculeSet.RemoveAt(i);
                        i--;
                    }
                }
                moleculeSet.Add(molecule);
            }
            else
            {
                var moleculeSet = chemModel.Builder.NewAtomContainerSet();
                moleculeSet.Add(molecule);
                chemModel.MoleculeSet = moleculeSet;
            }
            return molecule;
        }

        /// <summary>
        /// Create a new ChemModel containing an IAtomContainer. It will create an
        /// <see cref="IAtomContainer"/> from the passed IAtomContainer when needed, which may cause
        /// information loss.
        /// </summary>
        /// <param name="atomContainer">The AtomContainer to have inside the ChemModel.</param>
        /// <returns>The new IChemModel object.</returns>
        public static IChemModel NewChemModel(IAtomContainer atomContainer)
        {
            IChemModel model = atomContainer.Builder.NewChemModel();
            IChemObjectSet<IAtomContainer> moleculeSet = model.Builder.NewAtomContainerSet();
            moleculeSet.Add(atomContainer);
            model.MoleculeSet = moleculeSet;
            return model;
        }

        /// <summary>
        /// This badly named methods tries to determine which AtomContainer in the
        /// ChemModel is best suited to contain added Atom's and Bond's.
        /// </summary>
        public static IAtomContainer GetRelevantAtomContainer(IChemModel chemModel, IAtom atom)
        {
            IAtomContainer result = null;
            if (chemModel.MoleculeSet != null)
            {
                var moleculeSet = chemModel.MoleculeSet;
                result = MoleculeSetManipulator.GetRelevantAtomContainer(moleculeSet, atom);
                if (result != null)
                {
                    return result;
                }
            }
            if (chemModel.ReactionSet != null)
            {
                var reactionSet = chemModel.ReactionSet;
                return ReactionSetManipulator.GetRelevantAtomContainer(reactionSet, atom);
            }
            if (chemModel.Crystal != null && chemModel.Crystal.Contains(atom))
            {
                return chemModel.Crystal;
            }
            if (chemModel.RingSet != null)
            {
                return AtomContainerSetManipulator.GetRelevantAtomContainer(chemModel.RingSet, atom);
            }
            throw new ArgumentException("The provided atom is not part of this IChemModel.");
        }

        /// <summary>
        /// Retrieves the first IAtomContainer containing a given IBond from an
        /// IChemModel.
        /// </summary>
        /// <param name="chemModel">The IChemModel object.</param>
        /// <param name="bond">The IBond object to search.</param>
        /// <returns>The IAtomContainer object found, null if none is found.</returns>
        public static IAtomContainer GetRelevantAtomContainer(IChemModel chemModel, IBond bond)
        {
            IAtomContainer result = null;
            if (chemModel.MoleculeSet != null)
            {
                var moleculeSet = chemModel.MoleculeSet;
                result = MoleculeSetManipulator.GetRelevantAtomContainer(moleculeSet, bond);
                if (result != null)
                {
                    return result;
                }
            }
            if (chemModel.ReactionSet != null)
            {
                var reactionSet = chemModel.ReactionSet;
                return ReactionSetManipulator.GetRelevantAtomContainer(reactionSet, bond);
            }
            // This should never happen.
            return null;
        }

        /// <summary>
        /// Retrieves the first IReaction containing a given IAtom from an
        /// IChemModel.
        /// </summary>
        /// <param name="chemModel">The IChemModel object.</param>
        /// <param name="atom">The IAtom object to search.</param>
        /// <returns>The IAtomContainer object found, null if none is found.</returns>
        public static IReaction GetRelevantReaction(IChemModel chemModel, IAtom atom)
        {
            IReaction reaction = null;
            if (chemModel.ReactionSet != null)
            {
                IReactionSet reactionSet = chemModel.ReactionSet;
                reaction = ReactionSetManipulator.GetRelevantReaction(reactionSet, atom);
            }
            return reaction;
        }

        /// <summary>
        /// Returns all the AtomContainer's of a ChemModel.
        /// </summary>
        public static IEnumerable<IAtomContainer> GetAllAtomContainers(IChemModel chemModel)
        {
            var moleculeSet = chemModel.Builder.NewAtomContainerSet();
            if (chemModel.MoleculeSet != null)
            {
                moleculeSet.AddRange(chemModel.MoleculeSet);
            }
            if (chemModel.ReactionSet != null)
            {
                moleculeSet.AddRange(ReactionSetManipulator.GetAllMolecules(chemModel.ReactionSet));
            }
            return MoleculeSetManipulator.GetAllAtomContainers(moleculeSet);
        }

        /// <summary>
        /// Sets the AtomProperties of all Atoms inside an IChemModel.
        /// </summary>
        /// <param name="chemModel">The IChemModel object.</param>
        /// <param name="propKey">The key of the property.</param>
        /// <param name="propVal">The value of the property.</param>
        public static void SetAtomProperties(IChemModel chemModel, string propKey, object propVal)
        {
            if (chemModel.MoleculeSet != null)
            {
                MoleculeSetManipulator.SetAtomProperties(chemModel.MoleculeSet, propKey, propVal);
            }
            if (chemModel.ReactionSet != null)
            {
                ReactionSetManipulator.SetAtomProperties(chemModel.ReactionSet, propKey, propVal);
            }
            if (chemModel.Crystal != null)
            {
                AtomContainerManipulator.SetAtomProperties(chemModel.Crystal, propKey, propVal);
            }
        }

        /// <summary>
        /// Retrieve a List of all ChemObject objects within an IChemModel.
        /// </summary>
        /// <param name="chemModel">The IChemModel object.</param>
        /// <returns>A List of all ChemObjects inside.</returns>
        public static List<IChemObject> GetAllChemObjects(IChemModel chemModel)
        {
            var list = new List<IChemObject>();
            // list.Add(chemModel); // only add ChemObjects contained within
            ICrystal crystal = chemModel.Crystal;
            if (crystal != null)
            {
                list.Add(crystal);
            }
            var moleculeSet = chemModel.MoleculeSet;
            if (moleculeSet != null)
            {
                list.Add(moleculeSet);
                var current = MoleculeSetManipulator.GetAllChemObjects(moleculeSet);
                foreach (var chemObject in current)
                {
                    if (!list.Contains(chemObject)) list.Add(chemObject);
                }
            }
            var reactionSet = chemModel.ReactionSet;
            if (reactionSet != null)
            {
                list.Add(reactionSet);
                var current = ReactionSetManipulator.GetAllChemObjects(reactionSet);
                foreach (var chemObject in current)
                {
                    if (!list.Contains(chemObject)) list.Add(chemObject);
                }
            }
            return list;
        }

        public static IEnumerable<string> GetAllIDs(IChemModel chemModel)
        {
            if (chemModel.Id != null)
                yield return chemModel.Id;
            var crystal = chemModel.Crystal;
            if (crystal != null)
                foreach (var e in AtomContainerManipulator.GetAllIDs(crystal))
                    yield return e;
            var moleculeSet = chemModel.MoleculeSet;
            if (moleculeSet != null)
                foreach (var e in MoleculeSetManipulator.GetAllIDs(moleculeSet))
                    yield return e;
            var reactionSet = chemModel.ReactionSet;
            if (reactionSet != null)
                foreach (var e in ReactionSetManipulator.GetAllIDs(reactionSet))
                    yield return e;
            yield break;
        }
    }
}
