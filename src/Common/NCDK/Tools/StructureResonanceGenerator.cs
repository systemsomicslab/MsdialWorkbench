/* Copyright (C) 2006-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
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

using NCDK.Aromaticities;
using NCDK.Isomorphisms;
using NCDK.Isomorphisms.Matchers;
using NCDK.Reactions;
using NCDK.Reactions.Types;
using NCDK.Reactions.Types.Parameters;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Tools
{
    /// <summary>
    /// This class try to generate resonance structure for a determinate molecule.
    /// </summary>
    /// <remarks>
    /// <para>Make sure that the molecule has the corresponding lone pair electrons
    /// for each atom. You can use the method: <see cref="LonePairElectronChecker"/>
    /// </para>
    /// <para>It is needed to call the addExplicitHydrogensToSatisfyValency  
    ///  from the class <see cref="CDKHydrogenAdder"/>.</para>
    /// <para>It is based on rearrangements of electrons and charge</para>
    /// <para>The method is based on call by reactions which occur in a resonance.</para>
    ///
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Tools.StructureResonanceGenerator_Example.cs+1"]/*' />
    ///
    /// <para>We have the possibility to localize the reactive center. Good method if you
    /// want to localize the reaction in a fixed point</para>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Tools.StructureResonanceGenerator_Example.cs+2"]/*' />
    /// <para>Moreover you must put the parameter as true</para>
    /// <para>If the reactive center is not localized then the reaction process will
    /// try to find automatically the possible reactive center.</para>
    /// </remarks>
    /// <seealso cref="IReactionProcess"/>
    // @author       Miguel Rojas
    // @cdk.created  2006-5-05
    // @cdk.module   reaction
    public class StructureResonanceGenerator
    {
       /// <summary>Generate resonance structure without looking at the symmetry</summary>
        private readonly bool lookingSymmetry;

        /// <summary>
        /// Construct an instance of StructureResonanceGenerator. Default restrictions
        /// are initiated.
        /// </summary>
        /// <seealso cref="SetDefaultReactions"/>
        public StructureResonanceGenerator()
            : this(false)
        {
        }

        /// <summary>
        /// Construct an instance of StructureResonanceGenerator. Default restrictions
        /// are initiated.
        /// </summary>
        /// <param name="lookingSymmetry">Specify if the resonance generation is based looking at the symmetry</param>
        /// <seealso cref="SetDefaultReactions"/>
        public StructureResonanceGenerator(bool lookingSymmetry)
        {
            Trace.TraceInformation("Initiate StructureResonanceGenerator");
            this.lookingSymmetry = lookingSymmetry;
            SetDefaultReactions();
        }

        /// <summary>
        /// The reactions that must be used in the generation of the resonance.
        /// </summary>
        /// <seealso cref="IReactionProcess"/>
        public IReadOnlyList<IReactionProcess> Reactions { get; set; }

        /// <summary>
        /// The number maximal of resonance structures to be found. The
        /// algorithm breaks the process when is came to this number.
        /// </summary>
        public int MaximalStructures { get; set; } = 50; /* TODO: REACT: some time takes too much time. At the moment fixed to 50 structures*/

        /// <summary>
        /// Set the default reactions that must be presents to generate the resonance.
        /// </summary>
        /// <seealso cref="Reactions"/>
        public void SetDefaultReactions()
        {
            CallDefaultReactions();
        }

        private void CallDefaultReactions()
        {
            var reactions = new List<IReactionProcess>();

            var paramList = new List<IParameterReaction>();
            var param = new SetReactionCenter { IsSetParameter = false };
            paramList.Add(param);

            IReactionProcess type;
            
            type = new SharingLonePairReaction();
            try
            {
                type.ParameterList = paramList;
            }
            catch (CDKException e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            reactions.Add(type);

            type = new PiBondingMovementReaction();
            var paramList2 = new List<IParameterReaction>();
            var param2 = new SetReactionCenter { IsSetParameter = false };
            paramList2.Add(param2);
            try
            {
                type.ParameterList = paramList2;
            }
            catch (CDKException e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            reactions.Add(type);

            type = new RearrangementAnionReaction();
            try
            {
                type.ParameterList = paramList;
            }
            catch (CDKException e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            reactions.Add(type);

            type = new RearrangementCationReaction();
            try
            {
                type.ParameterList = paramList;
            }
            catch (CDKException e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            reactions.Add(type);

            type = new RearrangementLonePairReaction();
            try
            {
                type.ParameterList = paramList;
            }
            catch (CDKException e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            reactions.Add(type);

            type = new RearrangementRadicalReaction();
            try
            {
                type.ParameterList = paramList;
            }
            catch (CDKException e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            reactions.Add(type);

            this.Reactions = reactions;
        }

        /// <summary>
        /// Get the resonance structures from an <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="molecule">The IAtomContainer to analyze</param>
        /// <returns>The different resonance structures</returns>
        public IChemObjectSet<IAtomContainer> GetStructures(IAtomContainer molecule)
        {
            int countStructure = 0;
            var setOfMol = molecule.Builder.NewAtomContainerSet();
            setOfMol.Add(molecule);

            for (int i = 0; i < setOfMol.Count; i++)
            {
                var mol = setOfMol[i];
                foreach (var aReactionsList in Reactions)
                {
                    var reaction = aReactionsList;
                    var setOfReactants = molecule.Builder.NewAtomContainerSet();
                    setOfReactants.Add(mol);
                    try
                    {
                        var setOfReactions = reaction.Initiate(setOfReactants, null);
                        if (setOfReactions.Count != 0)
                            for (int k = 0; k < setOfReactions.Count; k++)
                                for (int j = 0; j < setOfReactions[k].Products.Count; j++)
                                {
                                    var product = setOfReactions[k].Products[j];
                                    if (!ExistAC(setOfMol, product))
                                    {
                                        setOfMol.Add(product);
                                        countStructure++;
                                        if (countStructure > MaximalStructures) return setOfMol;
                                    }
                                }
                    }
                    catch (CDKException e)
                    {
                        Console.Error.WriteLine(e.StackTrace);
                    }
                }
            }
            return setOfMol;
        }

        /// <summary>
        /// Get the container which is found resonance from a <see cref="IAtomContainer"/>.
        /// It is based on looking if the order of the bond changes.
        /// </summary>
        /// <param name="molecule">The IAtomContainer to analyze</param>
        /// <returns>The different containers</returns>
        public IChemObjectSet<IAtomContainer> GetContainers(IAtomContainer molecule)
        {
            var setOfCont = molecule.Builder.NewAtomContainerSet();
            var setOfMol = GetStructures(molecule);

            if (setOfMol.Count == 0)
                return setOfCont;

            /* extraction of all bonds which has been produced a changes of order */
            var bondList = new List<IBond>();
            for (int i = 1; i < setOfMol.Count; i++)
            {
                var mol = setOfMol[i];
                for (int j = 0; j < mol.Bonds.Count; j++)
                {
                    var bond = molecule.Bonds[j];
                    if (!mol.Bonds[j].Order.Equals(bond.Order))
                    {
                        if (!bondList.Contains(bond))
                            bondList.Add(bond);
                    }
                }
            }

            if (bondList.Count == 0)
                return null;

            int[] flagBelonging = new int[bondList.Count];
            for (int i = 0; i < flagBelonging.Length; i++)
                flagBelonging[i] = 0;
            int[] position = new int[bondList.Count];
            int maxGroup = 1;

            /* Analysis if the bond are linked together */
            List<IBond> newBondList = new List<IBond>
            {
                bondList[0]
            };

            int pos = 0;
            for (int i = 0; i < newBondList.Count; i++)
            {
                if (i == 0)
                    flagBelonging[i] = maxGroup;
                else
                {
                    if (flagBelonging[position[i]] == 0)
                    {
                        maxGroup++;
                        flagBelonging[position[i]] = maxGroup;
                    }
                }

                var bondA = newBondList[i];
                for (int ato = 0; ato < 2; ato++)
                {
                    var atomA1 = bondA.Atoms[ato];
                    var bondA1s = molecule.GetConnectedBonds(atomA1);
                    foreach (var bondB in bondA1s)
                    {
                        if (!newBondList.Contains(bondB))
                            for (int k = 0; k < bondList.Count; k++)
                                if (bondList[k].Equals(bondB))
                                    if (flagBelonging[k] == 0)
                                    {
                                        flagBelonging[k] = maxGroup;
                                        pos++;
                                        newBondList.Add(bondB);
                                        position[pos] = k;
                                    }
                    }
                }
                //if it is final size and not all are added
                if (newBondList.Count - 1 == i)
                    for (int k = 0; k < bondList.Count; k++)
                        if (!newBondList.Contains(bondList[k]))
                        {
                            newBondList.Add(bondList[k]);
                            position[i + 1] = k;
                            break;
                        }
            }
            /* creating containers according groups */
            for (int i = 0; i < maxGroup; i++)
            {
                var container = molecule.Builder.NewAtomContainer();
                for (int j = 0; j < bondList.Count; j++)
                {
                    if (flagBelonging[j] != i + 1)
                        continue;
                    var bond = bondList[j];
                    var atomA1 = bond.Atoms[0];
                    var atomA2 = bond.Atoms[1];
                    if (!container.Contains(atomA1))
                        container.Atoms.Add(atomA1);
                    if (!container.Contains(atomA2))
                        container.Atoms.Add(atomA2);
                    container.Bonds.Add(bond);
                }
                setOfCont.Add(container);
            }
            return setOfCont;
        }

        /// <summary>
        /// Get the container which the atom is found on resonance from a <see cref="IAtomContainer"/>.
        /// It is based on looking if the order of the bond changes. Return null
        /// is any is found.
        /// </summary>
        /// <param name="molecule">The IAtomContainer to analyze</param>
        /// <param name="atom">The IAtom</param>
        /// <returns>The container with the atom</returns>
        public IAtomContainer GetContainer(IAtomContainer molecule, IAtom atom)
        {
            var setOfCont = GetContainers(molecule);
            if (setOfCont == null)
                return null;

            foreach (var container in setOfCont)
            {
                if (container.Contains(atom))
                    return container;
            }

            return null;
        }

        /// <summary>
        /// Get the container which the bond is found on resonance from a <see cref="IAtomContainer"/>.
        /// It is based on looking if the order of the bond changes. Return null
        /// is any is found.
        /// </summary>
        /// <param name="molecule">The IAtomContainer to analyze</param>
        /// <param name="bond">The IBond</param>
        /// <returns>The container with the bond</returns>
        public IAtomContainer GetContainer(IAtomContainer molecule, IBond bond)
        {
            var setOfCont = GetContainers(molecule);
            if (setOfCont == null)
                return null;

            foreach (var container in setOfCont)
            {
                if (container.Contains(bond))
                    return container;
            }

            return null;
        }

        /// <summary>
        /// Search if the setOfAtomContainer contains the atomContainer
        /// </summary>
        /// <param name="set">ISetOfAtomContainer object where to search</param>
        /// <param name="atomContainer">IAtomContainer to search</param>
        /// <returns>True, if the atomContainer is contained</returns>
        private bool ExistAC(IChemObjectSet<IAtomContainer> set, IAtomContainer atomContainer)
        {
            IAtomContainer acClone = null;
            acClone = (IAtomContainer)atomContainer.Clone();
            if (!lookingSymmetry)
            { /* remove all aromatic flags */
                foreach (var atom in acClone.Atoms)
                    atom.IsAromatic = false;
                foreach (var bond in acClone.Bonds)
                    bond.IsAromatic = false;
            }

            for (int i = 0; i < acClone.Atoms.Count; i++)
                acClone.Atoms[i].Id = "" + acClone.Atoms.IndexOf(acClone.Atoms[i]);

            if (lookingSymmetry)
            {
                try
                {
                    Aromaticity.CDKLegacy.Apply(acClone);
                }
                catch (CDKException e)
                {
                    Console.Error.WriteLine(e.StackTrace);
                }
            }
            else
            {
                if (!lookingSymmetry)
                { /* remove all aromatic flags */
                    foreach (var atom in acClone.Atoms)
                        atom.IsAromatic = false;
                    foreach (var bond in acClone.Bonds)
                        bond.IsAromatic = false;
                }
            }
            for (int i = 0; i < set.Count; i++)
            {
                var ss = set[i];
                for (int j = 0; j < ss.Atoms.Count; j++)
                    ss.Atoms[j].Id = "" + ss.Atoms.IndexOf(ss.Atoms[j]);

                try
                {
                    if (!lookingSymmetry)
                    {
                        var qAC = QueryAtomContainerCreator.CreateSymbolChargeIDQueryContainer(acClone);
                        if (new UniversalIsomorphismTester().IsIsomorph(ss, qAC))
                        {
                            var qAC2 = QueryAtomContainerCreator.CreateSymbolAndBondOrderQueryContainer(acClone);
                            if (new UniversalIsomorphismTester().IsIsomorph(ss, qAC2))
                                return true;
                        }
                    }
                    else
                    {
                        var qAC = QueryAtomContainerCreator.CreateSymbolAndChargeQueryContainer(acClone);
                        Aromaticity.CDKLegacy.Apply(ss);
                        if (new UniversalIsomorphismTester().IsIsomorph(ss, qAC))
                            return true;
                    }
                }
                catch (CDKException e1)
                {
                    Console.Error.WriteLine(e1);
                    Trace.TraceError(e1.Message);
                    Debug.WriteLine(e1);
                }
            }
            return false;
        }
    }
}
