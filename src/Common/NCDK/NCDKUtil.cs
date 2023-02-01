// Copyright (C) 2016-2017  Kazuya Ujihara
// This file is under LGPL-2.1 

using System;
using System.Collections.Generic;

namespace NCDK
{
    public static class NCDKUtil
    {
        /// <summary>
        /// Adds a bond to this container.
        /// </summary>
        /// <param name="atom1">the first atom</param>
        /// <param name="atom2">the second atom</param>
        /// <param name="order">bond order</param>
        /// <param name="stereo">Stereochemical orientation</param>
        /// <returns>The bond added</returns>
        public static IBond AddBond(this IAtomContainer mol, IAtom atom1, IAtom atom2, BondOrder order, BondStereo stereo)
        {
            if (!(mol.Contains(atom1) && mol.Contains(atom2)))
                throw new InvalidOperationException();
            var bond = mol.Builder.NewBond(atom1, atom2, order, stereo);
            mol.Bonds.Add(bond);
            return bond;
        }

        /// <summary>
        /// Adds a bond to this container.
        /// </summary>
        /// <param name="atom1">the first atom</param>
        /// <param name="atom2">the second atom</param>
        /// <param name="order">bond order</param>
        /// <returns>The bond added</returns>
        public static IBond AddBond(this IAtomContainer mol, IAtom atom1, IAtom atom2, BondOrder order)
        {
            if (!(mol.Contains(atom1) && mol.Contains(atom2)))
                throw new InvalidOperationException();
            IBond bond = mol.Builder.NewBond(atom1, atom2, order);
            mol.Bonds.Add(bond);
            return bond;
        }

        /// <summary>
        /// Sets the array of atoms of this AtomContainer.
        /// </summary>
        /// <param name="atoms">The array of atoms to be assigned to this AtomContainer</param>
        /// <seealso cref="IAtomContainer.Atoms"/>
        public static void SetAtoms(this IAtomContainer mol, IEnumerable<IAtom> atoms)
        {
            mol.Atoms.Clear();
            foreach (var atom in atoms)
                mol.Atoms.Add(atom);
        }

        /// <summary>
        /// Sets the array of bonds of this AtomContainer.
        /// </summary>
        /// <param name="bonds">The array of bonds to be assigned to this AtomContainer</param>
        /// <seealso cref="IAtomContainer.Bonds"/>
        public static void SetBonds(this IAtomContainer mol, IEnumerable<IBond> bonds)
        {
            mol.Bonds.Clear();
            foreach (var bond in bonds)
                mol.Bonds.Add(bond);
        }

        /// <summary>
        /// Set the stereo elements - this will replace the existing instance with a new instance.
        /// </summary>
        /// <param name="elements"></param>
        public static void SetStereoElements(this IAtomContainer mol, IEnumerable<IStereoElement<IChemObject, IChemObject>> elements)
        {
            mol.StereoElements.Clear();
            foreach (var se in elements)
                mol.StereoElements.Add(se);
        }

        /// <summary>
        /// Adds a <see cref="ILonePair"/> to this <see cref="IAtom"/>.
        /// </summary>
        /// <param name="atom">The atom to which the <see cref="ILonePair"/> is added</param>
        public static void AddLonePairTo(this IAtomContainer mol, IAtom atom)
        {
            if (!mol.Contains(atom))
                throw new InvalidOperationException();
            var e = mol.Builder.NewLonePair(atom);
            mol.LonePairs.Add(e);
        }

        /// <summary>
        /// Adds a <see cref="ISingleElectron"/> to this <see cref="IAtom"/>.
        /// </summary>
        /// <param name="atom">The atom to which the <see cref="ISingleElectron"/> is added</param>
        public static void AddSingleElectronTo(this IAtomContainer mol, IAtom atom)
        {
            if (!mol.Contains(atom))
                throw new InvalidOperationException();
            var e = mol.Builder.NewSingleElectron(atom);
            mol.SingleElectrons.Add(e);
        }

        /// <summary>
        /// Add <paramref name="element"/> as <see cref="IAtom"/> to <paramref name="mol"/>.
        /// </summary>
        /// <param name="mol">The molecular to add atom.</param>
        /// <param name="element"><see cref="ChemicalElement"/> to add.</param>
        /// <returns><see cref="IAtom"/> added.</returns>
        public static IAtom Add(this IAtomContainer mol, ChemicalElement element)
        {
            var atom = mol.Builder.NewAtom(element);
            mol.Add(atom);
            return atom;
        }

        public static IAtom AddAtom(this IAtomContainer mol, string elementSymbol)
        {
            var atom = mol.Builder.NewAtom(elementSymbol);
            mol.Add(atom);
            return atom;
        }
    }
}
