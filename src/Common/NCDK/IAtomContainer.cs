/* Copyright (C) 2006-2007,2010  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General License for more details.
 *
 * You should have received a copy of the GNU Lesser General License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// Base class for all chemical objects that maintain a list of Atoms and
    /// ElectronContainers.
    /// </summary>
    /// <example>
    /// Looping over all <see cref="IBond"/>s in the <see cref="IAtomContainer"/> is typically done like:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.IAtomContainer_Example.cs"]/*' />
    /// </example>
    // @author     steinbeck
    // @cdk.module interfaces
    // @cdk.created    2000-10-02
    public interface IAtomContainer
        : IChemObject, IChemObjectListener
    {
        /// <summary>
        /// Sets the array of atoms of this AtomContainer.
        /// </summary>
        /// <param name="atoms">The array of atoms to be assigned to this AtomContainer</param>
        /// <seealso cref="Atoms"/>
        void SetAtoms(IEnumerable<IAtom> atoms);

        /// <summary>
        /// Sets the array of bonds of this AtomContainer.
        /// </summary>
        /// <param name="bonds">The array of bonds to be assigned to this AtomContainer</param>
        /// <seealso cref="Bonds"/>
        void SetBonds(IEnumerable<IBond> bonds);

        /// <summary>
        /// The atoms in this container.
        /// </summary>
        /// <seealso cref="SetAtoms(IEnumerable{IAtom})"/>
        IList<IAtom> Atoms { get; }

        /// <summary>
        /// The bonds in this container.
        /// </summary>
        /// <seealso cref="SetBonds(IEnumerable{IBond})"/>
        IList<IBond> Bonds { get; }

        /// <summary>
        /// The lone pairs in this container.
        /// </summary>
        IList<ILonePair> LonePairs { get; }

        /// <summary>
        /// The single electrons in this container.
        /// </summary>
        IList<ISingleElectron> SingleElectrons { get; }

        /// <summary>
        /// A stereo element to this container.
        /// </summary>
        ICollection<IStereoElement<IChemObject, IChemObject>> StereoElements { get; }

        /// <summary>
        /// One or several of the bonds have <see cref="IBond.IsSingleOrDouble"/> raised (which may indicate aromaticity).
        /// </summary>
        bool IsSingleOrDouble { get; set; }

        /// <summary>
        /// This object is part of an aromatic system. 
        /// </summary>
        bool IsAromatic { get; set; }

        /// <summary>
        /// Returns <see cref="IElectronContainer"/>s in the container.
        /// </summary>
        /// <returns><see cref="IElectronContainer"/>s in the container.</returns>
        /// <seealso cref="Add(IElectronContainer)"/> 
        IEnumerable<IElectronContainer> GetElectronContainers();

        /// <summary>
        /// Returns the bond that connects the two given atoms.
        /// </summary>
        /// <param name="atom1">The first atom</param>
        /// <param name="atom2">The second atom</param>
        /// <returns>The <see cref="IBond"/> that connects between <paramref name="atom1"/> and <paramref name="atom2"/></returns>
        IBond GetBond(IAtom atom1, IAtom atom2);

        /// <summary>
        /// Returns the atoms connected to the specified atom by
        /// a bond.
        /// </summary>
        /// <param name="atom">the atom</param>
        /// <returns>connected atoms</returns>
        IEnumerable<IAtom> GetConnectedAtoms(IAtom atom);

        /// <summary>
        /// Returns the bonds connected to the specified atom.
        /// </summary>
        /// <param name="atom">the atom</param>
        /// <returns>connected bonds</returns>
        IEnumerable<IBond> GetConnectedBonds(IAtom atom);

        /// <summary>
        /// Returns the lone pairs connected to the specified atom.
        /// </summary>
        /// <param name="atom">the atom</param>
        /// <returns>connected lone pairs</returns>
        IEnumerable<ILonePair> GetConnectedLonePairs(IAtom atom);

        /// <summary>
        /// Returns the electron containers (bonds, radicals, and lone pairs )
        /// connected to the specified atom.
        /// </summary>
        /// <param name="atom">the atom</param>
        /// <returns>connected lone pairs</returns>
        IEnumerable<ISingleElectron> GetConnectedSingleElectrons(IAtom atom);
        
        /// <summary>
        /// Returns the electron containers (bonds, radicals, and lone pairs )
        /// connected to the specified atom.
        /// </summary>
        /// <param name="atom">the atom</param>
        /// <returns>connected lone pairs</returns>
        IEnumerable<IElectronContainer> GetConnectedElectronContainers(IAtom atom);

        /// <summary>
        /// Returns the sum of the bond orders for a given Atom.
        /// </summary>
        /// <param name="atom">The atom</param>
        /// <returns>The number of bond orders for this <paramref name="atom"/></returns>
        double GetBondOrderSum(IAtom atom);

        /// <summary>
        /// Returns the maximum bond order that this atom currently has in the context
        /// of this AtomContainer.  If the atom has no bonds
        /// but does have implicit hydrogens the minimum bond order is
        /// <see cref="BondOrder.Single"/>, otherwise the bond is unset
        /// <see cref="BondOrder.Unset"/>.
        /// </summary>
        /// <param name="atom">The atom</param>
        /// <returns>The maximum bond order that this atom currently has</returns>
        BondOrder GetMaximumBondOrder(IAtom atom);

        /// <summary>
        /// Returns the minimum bond order that this atom currently has
        /// in the context of this AtomContainer. If the atom has no bonds
        /// but does have implicit hydrogens the minimum bond order is
        /// <see cref="BondOrder.Single"/>, otherwise the bond is unset
        /// <see cref="BondOrder.Unset"/>.
        /// </summary>
        /// <param name="atom">The atom</param>
        /// <returns>The minimum bond order that this atom currently has</returns>
        BondOrder GetMinimumBondOrder(IAtom atom);

        /// <summary>
        /// Adds all atoms and electron containers of a given <paramref name="atomContainer"/> to this container.
        /// </summary>
        /// <param name="atomContainer">The <see cref="IAtomContainer"/> to be added</param>
        void Add(IAtomContainer atomContainer);

        /// <summary>
        /// Add <paramref name="atom"/> to this container.
        /// </summary>
        /// <param name="atom">The atom to be added.</param>
        void Add(IAtom atom);

        /// <summary>
        /// Adds a <see cref="IElectronContainer"/> to this <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="electronContainer">The <see cref="IElectronContainer"/> to added to this container</param>
        void Add(IElectronContainer electronContainer);

        /// <summary>
        /// Removes all atoms and electronContainers of a given atomcontainer from this container.
        /// </summary>
        /// <param name="atomContainer">The atomcontainer to be removed</param>
        void Remove(IAtomContainer atomContainer);

        /// <summary>
        /// Removes <paramref name="electronContainer"/> from this container.
        /// </summary>
        /// <param name="electronContainer">The electron container to be removed</param>
        void Remove(IElectronContainer electronContainer);

        /// <summary>
        /// Safely remove an atom from the container.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Removes a single atom from the container updating all internal
        /// state to be consistent. All bonds connected to the atom will be
        /// deleted as well as all stereo elements. If multiple atoms/bonds are
        /// being deleted they should be gathered into a single transaction
        /// and removed with <see cref="Remove(IAtomContainer)"/>.
        /// </para>
        /// <para>
        /// If you are removing hydrogens one of the
        /// utility methods (e.g. <see cref="Tools.Manipulator.AtomContainerManipulator.RemoveHydrogens(IAtomContainer)"/>)
        /// is preferable.
        /// </para>
        /// </remarks>
        /// <param name="atom">the atom to be removed</param>
        void RemoveAtom(IAtom atom);

        /// <summary>
        /// Safely remove an atom from the container.
        /// </summary>
        /// <remarks>
        /// Removes a single atom from the container updating all internal
        /// state to be consistent. All bonds connected to the atom will be
        /// deleted as well as all stereo elements. If multiple atoms/bonds are
        /// being deleted they should be gathered into a single transaction
        /// and removed with <see cref="Remove(IAtomContainer)"/>.
        /// <para>
        /// If you are removing hydrogens one of the
        /// utility methods (e.g. AtomContainerManipulator.removeHydrogens(IAtomContainer))
        /// is preferable.
        /// </para>
        /// </remarks>
        /// <param name="pos">the position of the atom to be removed</param>
        void RemoveAtom(int pos);

        /// <summary>
        /// Safely remove an atom from the container.
        /// </summary>
        /// <seealso cref="RemoveAtom(IAtom)"/>
        [Obsolete("Method has be renamed " + nameof(RemoveAtom) + ".")]
        void RemoveAtomAndConnectedElectronContainers(IAtom atom);

        /// <summary>
        /// Removes all atoms, bonds and stereo elements from this container.
        /// </summary>
        void RemoveAllElements();

        /// <summary>
        /// Removes electronContainers from this container.
        /// </summary>
        void RemoveAllElectronContainers();

        /// <summary>
        /// Removes the bond that connects the two given atoms.
        /// </summary>
        /// <param name="atom1">The first atom</param>
        /// <param name="atom2">The second atom</param>
        /// <returns>The bond that connects the two atoms</returns>
        IBond RemoveBond(IAtom atom1, IAtom atom2);

        /// <summary>
        /// Removes the bond.
        /// </summary>
        /// <param name="bond">The bond to remove.</param>
        /// <returns></returns>
        void RemoveBond(IBond bond);

        /// <summary>
        /// <see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given atom object.
        /// </summary>
        /// <param name="atom">the atom this <see cref="IAtomContainer"/> is searched for</param>
        /// <returns><see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given atom object</returns>
        bool Contains(IAtom atom);

        /// <summary>
        /// <see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given bond object.
        /// </summary>
        /// <param name="bond">the bond this AtomContainer is searched for</param>
        /// <returns><see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given bond object</returns>
        bool Contains(IBond bond);

        /// <summary>
        /// <see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given <see cref="ILonePair"/> object.
        /// </summary>
        /// <param name="lonePair">the <see cref="ILonePair"/> this <see cref="IAtomContainer"/> is searched for</param>
        /// <returns><see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given <see cref="ILonePair"/> object</returns>
        bool Contains(ILonePair lonePair);

        /// <summary>
        /// <see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given <see cref="ISingleElectron"/> object.
        /// </summary>
        /// <param name="singleElectron">the <see cref="ISingleElectron"/> this <see cref="IAtomContainer"/> is searched for</param>
        /// <returns><see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given <see cref="ISingleElectron"/> object</returns>
        bool Contains(ISingleElectron singleElectron);

        /// <summary>
        /// <see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given <see cref="IElectronContainer"/> object.
        /// </summary>
        /// <param name="electronContainer">the <see cref="IElectronContainer"/> this <see cref="IAtomContainer"/> is searched for</param>
        /// <returns><see langword="true"/>, if the <see cref="IAtomContainer"/> contains the given <see cref="IElectronContainer"/> object</returns>
        bool Contains(IElectronContainer electronContainer);

        /// <summary>
        /// Indicates whether this container is empty. The container is considered empty if
        /// there are no atoms. Bonds are not checked as a graph with no vertexes can not
        /// have edges.
        /// </summary>
        /// <returns>whether the container is empty</returns>
        bool IsEmpty();
        
        /// <summary>
        /// The title of the record.
        /// </summary>
        string Title { get; set; }
    }
}
