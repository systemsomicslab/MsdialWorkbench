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

using NCDK.Common.Collections;
using NCDK.Stereo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// </summary>
    /// <seealso cref="ChemModelManipulator"/>
    // @cdk.module standard
    public static class ReactionManipulator
    {
        public static int GetAtomCount(IReaction reaction)
        {
            int count = 0;
            var reactants = reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                count += reactants[i].Atoms.Count;
            }
            var agents = reaction.Agents;
            for (int i = 0; i < agents.Count; i++)
            {
                count += agents[i].Atoms.Count;
            }
            var products = reaction.Products;
            for (int i = 0; i < products.Count; i++)
            {
                count += products[i].Atoms.Count;
            }
            return count;
        }

        public static int GetBondCount(IReaction reaction)
        {
            int count = 0;
            var reactants = reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                count += reactants[i].Bonds.Count;
            }
            var agents = reaction.Agents;
            for (int i = 0; i < agents.Count; i++)
            {
                count += agents[i].Bonds.Count;
            }
            var products = reaction.Products;
            for (int i = 0; i < products.Count; i++)
            {
                count += products[i].Bonds.Count;
            }
            return count;
        }

        public static void RemoveAtomAndConnectedElectronContainers(IReaction reaction, IAtom atom)
        {
            var reactants = reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                IAtomContainer mol = reactants[i];
                if (mol.Contains(atom))
                {
                    mol.RemoveAtom(atom);
                }
            }
            var agents = reaction.Reactants;
            for (int i = 0; i < agents.Count; i++)
            {
                var mol = agents[i];
                if (mol.Contains(atom))
                {
                    mol.RemoveAtom(atom);
                }
            }
            var products = reaction.Products;
            for (int i = 0; i < products.Count; i++)
            {
                var mol = products[i];
                if (mol.Contains(atom))
                {
                    mol.RemoveAtom(atom);
                }
            }
        }

        public static void RemoveElectronContainer(IReaction reaction, IElectronContainer electrons)
        {
            var reactants = reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                IAtomContainer mol = reactants[i];
                if (mol.Contains(electrons))
                {
                    mol.Remove(electrons);
                }
            }
            var agents = reaction.Reactants;
            for (int i = 0; i < agents.Count; i++)
            {
                var mol = agents[i];
                if (mol.Contains(electrons))
                {
                    mol.Remove(electrons);
                }
            }
            var products = reaction.Products;
            for (int i = 0; i < products.Count; i++)
            {
                var mol = products[i];
                if (mol.Contains(electrons))
                {
                    mol.Remove(electrons);
                }
            }
        }

        /// <summary>
        /// Get all molecule of a <see cref="IReaction"/>: reactants + products.
        /// </summary>
        /// <param name="reaction">The IReaction</param>
        /// <returns>All molecules</returns>
        public static IEnumerable<IAtomContainer> GetAllMolecules(IReaction reaction)
        {
            return GetAllReactants(reaction).
                Union(GetAllAgents(reaction)).
                Union(GetAllProducts(reaction));
        }

        /// <summary>
        /// Get all products of a <see cref="IReaction"/>
        /// </summary>
        /// <param name="reaction">The reaction</param>
        /// <returns>Molecules</returns>
        public static IEnumerable<IAtomContainer> GetAllProducts(IReaction reaction)
        {
            return reaction.Products;
        }

        /// <summary>
        /// get all reactants of a IReaction
        /// </summary>
        /// <param name="reaction">The IReaction</param>
        /// <returns>Molecules</returns>
        public static IEnumerable<IAtomContainer> GetAllReactants(IReaction reaction)
        {
            return reaction.Reactants;
        }

        public static IEnumerable<IAtomContainer> GetAllAgents(IReaction reaction)
        {
            return reaction.Agents;
        }

        /// <summary>
        /// Returns a new Reaction object which is the reverse of the given
        /// Reaction.
        /// </summary>
        /// <param name="reaction">the reaction being considered</param>
        /// <returns>the reverse reaction</returns>
        public static IReaction Reverse(IReaction reaction)
        {
            var reversedReaction = reaction.Builder.NewReaction();
            if (reaction.Direction == ReactionDirection.Bidirectional)
            {
                reversedReaction.Direction = ReactionDirection.Bidirectional;
            }
            else if (reaction.Direction == ReactionDirection.Forward)
            {
                reversedReaction.Direction = ReactionDirection.Backward;
            }
            else if (reaction.Direction == ReactionDirection.Backward)
            {
                reversedReaction.Direction = ReactionDirection.Forward;
            }
            var reactants = reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                double coefficient = reaction.Reactants.GetMultiplier(reactants[i]).Value;
                reversedReaction.Products.Add(reactants[i], coefficient);
            }
            var products = reaction.Products;
            for (int i = 0; i < products.Count; i++)
            {
                double coefficient = reaction.Products.GetMultiplier(products[i]).Value;
                reversedReaction.Reactants.Add(products[i], coefficient);
            }
            return reversedReaction;
        }

        /// <summary>
        /// Returns all the AtomContainer's of a Reaction.
        /// </summary>
        /// <param name="reaction">The reaction being considered</param>
        /// <returns>a list of the IAtomContainer objects comprising the reaction</returns>
        public static IEnumerable<IAtomContainer> GetAllAtomContainers(IReaction reaction)
        {
            return MoleculeSetManipulator.GetAllAtomContainers(GetAllMolecules(reaction));
        }

        public static IEnumerable<string> GetAllIDs(IReaction reaction)
        {
            if (reaction.Id != null)
                yield return reaction.Id;
            var reactants = reaction.Reactants;
            for (int i = 0; i < reactants.Count; i++)
            {
                var mol = reactants[i];
                foreach (var id in AtomContainerManipulator.GetAllIDs(mol))
                    yield return id;
            }
            var products = reaction.Products;
            for (int i = 0; i < products.Count; i++)
            {
                var mol = products[i];
                foreach (var id in AtomContainerManipulator.GetAllIDs(mol))
                    yield return id;
            }
            yield break;
        }

        public static IAtomContainer GetRelevantAtomContainer(IReaction reaction, IAtom atom)
        {
            var result = MoleculeSetManipulator.GetRelevantAtomContainer(reaction.Reactants, atom);
            if (result != null)
            {
                return result;
            }
            return MoleculeSetManipulator.GetRelevantAtomContainer(reaction.Products, atom);
        }

        public static IAtomContainer GetRelevantAtomContainer(IReaction reaction, IBond bond)
        {
            var result = MoleculeSetManipulator.GetRelevantAtomContainer(reaction.Reactants, bond);
            if (result != null)
            {
                return result;
            }
            return MoleculeSetManipulator.GetRelevantAtomContainer(reaction.Products, bond);
        }

        public static void SetAtomProperties(IReaction reaction, string propKey, object propVal)
        {
            var reactants = reaction.Reactants;
            for (int j = 0; j < reactants.Count; j++)
            {
                AtomContainerManipulator.SetAtomProperties(reactants[j], propKey, propVal);
            }
            var products = reaction.Products;
            for (int j = 0; j < products.Count; j++)
            {
                AtomContainerManipulator.SetAtomProperties(products[j], propKey, propVal);
            }
        }

        public static IEnumerable<IChemObject> GetAllChemObjects(IReaction reaction)
        {
            yield return reaction;
            foreach (var mol in reaction.Reactants)
                yield return mol;
            foreach (var mol in reaction.Products)
                yield return mol;
            yield break;
        }

        /// <summary>
        /// Get the IAtom which is mapped
        /// </summary>
        /// <param name="reaction">The IReaction which contains the mapping</param>
        /// <param name="chemObject">The IChemObject which will be searched its mapped IChemObject</param>
        /// <returns>The mapped IChemObject</returns>
        public static IChemObject GetMappedChemObject(IReaction reaction, IChemObject chemObject)
        {
            foreach (var mapping in reaction.Mappings)
            {
                if (mapping[0].Equals(chemObject))
                {
                    return mapping[1];
                }
                else if (mapping[1].Equals(chemObject))
                    return mapping[0];
            }
            return null;
        }

        /// <summary>
        /// Assigns a reaction role and group id to all atoms in a molecule.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <param name="role">role to assign</param>
        /// <param name="groupId">group id</param>
        private static void AssignRoleAndGroup(IAtomContainer mol, ReactionRole role, int groupId)
        {
            foreach (IAtom atom in mol.Atoms)
            {
                atom.SetProperty(CDKPropertyName.ReactionRole, role);
                atom.SetProperty(CDKPropertyName.ReactionGroup, groupId);
            }
        }

        /// <summary>
        /// Converts a reaction to an 'inlined' reaction stored as a molecule. All
        /// reactants, agents, products are added to the molecule as disconnected
        /// components with atoms flagged as to their role <see cref="ReactionRole"/> and
        /// component group.
        /// </summary>
        /// <remarks>
        /// The inlined reaction, stored in a molecule can be converted back to an explicit
        /// reaction with <see cref="ToReaction(IAtomContainer)"/>. Data stored on the individual components (e.g.
        /// titles is lost in the conversion).
        /// </remarks>
        /// <param name="rxn">reaction to convert</param>
        /// <returns>inlined reaction stored in a molecule</returns>
        /// <seealso cref="ToReaction(IAtomContainer)"/>
        public static IAtomContainer ToMolecule(IReaction rxn)
        {
            if (rxn == null)
                throw new ArgumentNullException(nameof(rxn), "Null reaction provided");
            var bldr = rxn.Builder;
            var mol = bldr.NewAtomContainer();
            mol.SetProperties(rxn.GetProperties());
            mol.Id = rxn.Id;
            int grpId = 0;
            foreach (IAtomContainer comp in rxn.Reactants)
            {
                AssignRoleAndGroup(comp, ReactionRole.Reactant, ++grpId);
                mol.Add(comp);
            }
            foreach (IAtomContainer comp in rxn.Agents)
            {
                AssignRoleAndGroup(comp, ReactionRole.Agent, ++grpId);
                mol.Add(comp);
            }
            foreach (IAtomContainer comp in rxn.Products)
            {
                AssignRoleAndGroup(comp, ReactionRole.Product, ++grpId);
                mol.Add(comp);
            }
            return mol;
        }

        /// <summary>
        /// Converts an 'inlined' reaction stored in a molecule back to a reaction.
        /// </summary>
        /// <param name="mol">molecule to convert</param>
        /// <returns>reaction</returns>
        /// <seealso cref="ToMolecule(IReaction)"/>
        public static IReaction ToReaction(IAtomContainer mol)
        {
            if (mol == null)
                throw new ArgumentException("Null molecule provided");
            var bldr = mol.Builder;
            var rxn = bldr.NewReaction();
            rxn.SetProperties(mol.GetProperties());
            rxn.Id = mol.Id;

            var components = new Dictionary<int, IAtomContainer>();

            // split atoms
            foreach (var atom in mol.Atoms)
            {
                var role = atom.GetProperty<ReactionRole?>(CDKPropertyName.ReactionRole);
                var grpIdx = atom.GetProperty<int?>(CDKPropertyName.ReactionGroup);

                if (role == null || role.Value == ReactionRole.None)
                    throw new ArgumentException("Atom " + mol.Atoms.IndexOf(atom) + " had undefined role");
                if (grpIdx == null)
                    throw new ArgumentException("Atom " + mol.Atoms.IndexOf(atom) + " had no reaction group id");

                // new component, and add to appropriate role
                if (!components.TryGetValue(grpIdx.Value, out IAtomContainer comp))
                {
                    components[grpIdx.Value] = comp = bldr.NewAtomContainer();
                    switch (role)
                    {
                        case ReactionRole.Reactant:
                            rxn.Reactants.Add(comp);
                            break;
                        case ReactionRole.Product:
                            rxn.Products.Add(comp);
                            break;
                        case ReactionRole.Agent:
                            rxn.Agents.Add(comp);
                            break;
                    }
                }

                comp.Atoms.Add(atom);
            }

            // split bonds
            foreach (var bond in mol.Bonds)
            {
                var beg = bond.Begin;
                var end = bond.End;
                var begIdx = beg.GetProperty<int?>(CDKPropertyName.ReactionGroup);
                var endIdx = end.GetProperty<int?>(CDKPropertyName.ReactionGroup);
                if (begIdx == null || endIdx == null)
                    throw new ArgumentException("Bond " + mol.Bonds.IndexOf(bond) + " had atoms with no reaction group id");
                if (!begIdx.Equals(endIdx))
                    throw new ArgumentException("Bond " + mol.Bonds.IndexOf(bond) + " had atoms with different reaction group id");
                components[begIdx.Value].Bonds.Add(bond);
            }

            // split stereochemistry
            foreach (var se in mol.StereoElements)
            {
                IAtom focus = null;
                switch (se)
                {
                    case ITetrahedralChirality tc:
                        focus = tc.ChiralAtom;
                        break;
                    case IDoubleBondStereochemistry ds:
                        focus = ds.StereoBond.Begin;
                        break;
                    case ExtendedTetrahedral et:
                        focus = et.Focus;
                        break;
                }
                if (focus == null)
                    throw new ArgumentException("Stereochemistry had no focus");
                var grpIdx = focus.GetProperty<int>(CDKPropertyName.ReactionGroup);
                components[grpIdx].StereoElements.Add(se);
            }

            foreach (var se in mol.SingleElectrons)
            {
                var grpIdx = se.Atom.GetProperty<int>(CDKPropertyName.ReactionGroup);
                components[grpIdx].SingleElectrons.Add(se);
            }

            foreach (var lp in mol.LonePairs)
            {
                var grpIdx = lp.Atom.GetProperty<int>(CDKPropertyName.ReactionGroup);
                components[grpIdx].LonePairs.Add(lp);
            }

            return rxn;
        }

        /// <summary>
        /// Bi-direction int-tuple for looking up bonds by index.
        /// </summary>
        private class IntTuple
        {
            private readonly int beg, end;

            public IntTuple(int beg, int end)
            {
                this.beg = beg;
                this.end = end;
            }

            public override bool Equals(object o)
            {
                if (this == o)
                    return true;
                if (o == null || GetType() != o.GetType())
                    return false;
                var that = (IntTuple)o;
                return (this.beg == that.beg && this.end == that.end) 
                    || (this.beg == that.end && this.end == that.beg);
            }

            public override int GetHashCode()
            {
                return beg ^ end;
            }
        }

        /// <summary>
        /// Collect the set of bonds that mapped in both a reactant and a product. The method uses
        /// the <see cref="CDKPropertyName.AtomAtomMapping"/> property of atoms.
        /// </summary>
        /// <param name="reaction">reaction</param>
        /// <returns>mapped bonds</returns>
        public static ISet<IBond> FindMappedBonds(IReaction reaction)
        {
            var mapped = new HashSet<IBond>();

            // first we collect the occurrance of mapped bonds from reacants then products
            var mappedReactantBonds = new HashSet<IntTuple>();
            var mappedProductBonds = new HashSet<IntTuple>();
            foreach (var reactant in reaction.Reactants)
            {
                foreach (var bond in reactant.Bonds)
                {
                    var begidx = bond.Begin.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
                    var endidx = bond.End.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
                    if (begidx != null && endidx != null)
                        mappedReactantBonds.Add(new IntTuple(begidx.Value, endidx.Value));
                }
            }
            // fail fast
            if (!mappedReactantBonds.Any())
                return Sets.Empty<IBond>();

            foreach (var product in reaction.Products)
            {
                foreach (var bond in product.Bonds)
                {
                    var begidx = bond.Begin.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
                    var endidx = bond.End.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
                    if (begidx != null && endidx != null)
                        mappedProductBonds.Add(new IntTuple(begidx.Value, endidx.Value));
                }
            }
            // fail fast
            if (!mappedProductBonds.Any())
                return Sets.Empty<IBond>();

            // repeat above but now store any that are different or unmapped as being mapped
            foreach (var reactant in reaction.Reactants)
            {
                foreach (var bond in reactant.Bonds)
                {
                    var begidx = bond.Begin.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
                    var endidx = bond.End.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
                    if (begidx != null && endidx != null && mappedProductBonds.Contains(new IntTuple(begidx.Value, endidx.Value)))
                        mapped.Add(bond);
                }
            }
            foreach (var product in reaction.Products)
            {
                foreach (var bond in product.Bonds)
                {
                    var begidx = bond.Begin.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
                    var endidx = bond.End.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
                    if (begidx != null && endidx != null && mappedReactantBonds.Contains(new IntTuple(begidx.Value, endidx.Value)))
                        mapped.Add(bond);
                }
            }
            return mapped;
        }

        public static void PerceiveRadicals(IReaction reaction)
        {
            foreach (var mol in ReactionManipulator.GetAllMolecules(reaction))
            {
                AtomContainerManipulator.PerceiveRadicals(mol);
            }
        }

        public static void PerceiveDativeBonds(IReaction reaction)
        {
            foreach (var mol in ReactionManipulator.GetAllMolecules(reaction))
            {
                AtomContainerManipulator.PerceiveDativeBonds(mol);
            }
        }
    }
}
