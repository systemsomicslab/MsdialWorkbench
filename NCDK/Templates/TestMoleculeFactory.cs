/*
 * Copyright (c) 2013. John May <jwmay@users.sf.net>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Config;
using NCDK.Numerics;
using System;
using System.Diagnostics;

namespace NCDK.Templates
{
    /// <summary>
    /// This class contains methods for generating simple organic molecules and is
    /// copy of <see cref="FaulonSignatures.Chemistry.MoleculeFactory"/> for use in tests.
    /// </summary>
    // @cdk.module test-data
    internal static class TestMoleculeFactory
    {
        private static IChemObjectBuilder builder = CDK.Builder;

        private static IAtomContainer NewAtomContainer()
        {
            return builder.NewAtomContainer();
        }

        private static void MolAddBond(IAtomContainer mol, int a, int b, BondOrder order)
        {
            mol.AddBond(mol.Atoms[a], mol.Atoms[b], order);
        }

        public static IAtomContainer MakeEthanol()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("O"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("C"));
            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            return mol;
        }

        public static IAtomContainer MakeAlphaPinene()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9
            mol.Atoms.Add(builder.NewAtom("C")); // 10

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6
            MolAddBond(mol, 0, 6, BondOrder.Single); // 7
            MolAddBond(mol, 3, 7, BondOrder.Single); // 8
            MolAddBond(mol, 5, 7, BondOrder.Single); // 9
            MolAddBond(mol, 7, 8, BondOrder.Single); // 10
            MolAddBond(mol, 7, 9, BondOrder.Single); // 11
            ConfigureAtoms(mol);
            return mol;
        }

        /// <summary>
        /// Generate an Alkane (chain of carbons with no hydrogens) of a given length.
        /// </summary>
        /// <param name="chainLength">The number of carbon atoms to have in the chain.</param>
        /// <returns>A molecule containing a bonded chain of carbons.</returns>
        /// This method was written by Stephen Tomkinson.
        // @cdk.created 2003-08-15
        public static IAtomContainer MakeAlkane(int chainLength)
        {
            IAtomContainer currentChain = NewAtomContainer();

            //Add the initial atom
            currentChain.Atoms.Add(builder.NewAtom("C"));

            //Add further atoms and bonds as needed, a pair at a time.
            for (int atomCount = 1; atomCount < chainLength; atomCount++)
            {
                currentChain.Atoms.Add(builder.NewAtom("C"));
                MolAddBond(currentChain, atomCount, atomCount - 1, BondOrder.Single);
            }

            return currentChain;
        }

        public static IAtomContainer MakeEthylCyclohexane()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6
            MolAddBond(mol, 0, 6, BondOrder.Single); // 7
            MolAddBond(mol, 6, 7, BondOrder.Single); // 8
            return mol;
        }

        /// <summary>
        /// Returns cyclohexene without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C6H10/c1-2-4-6-5-3-1/h1-2H,3-6H2
        public static IAtomContainer MakeCyclohexene()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Double); // 6
            return mol;
        }

        /// <summary>
        /// Returns cyclohexane without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C6H12/c1-2-4-6-5-3-1/h1-6H2
        public static IAtomContainer MakeCyclohexane()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6
            return mol;
        }

        /// <summary>
        /// Returns cyclopentane without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C5H10/c1-2-4-5-3-1/h1-5H2
        public static IAtomContainer MakeCyclopentane()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Single); // 5
            return mol;
        }

        /// <summary>
        /// Returns cyclobutane without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C4H8/c1-2-4-3-1/h1-4H2
        public static IAtomContainer MakeCyclobutane()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 0, BondOrder.Single); // 4
            return mol;
        }

        /// <summary>
        /// Returns cyclobutadiene without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C4H4/c1-2-4-3-1/h1-4H
        public static IAtomContainer MakeCyclobutadiene()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Double); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 0, BondOrder.Double); // 4
            return mol;
        }

        public static IAtomContainer MakePropylCycloPropane()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 0, BondOrder.Single); // 3
            MolAddBond(mol, 2, 3, BondOrder.Single); // 4
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 4

            return mol;
        }

        /// <summary>
        /// Returns biphenyl without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C12H10/c1-3-7-11(8-4-1)12-9-5-2-6-10-12/h1-10H
        public static IAtomContainer MakeBiphenyl()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9
            mol.Atoms.Add(builder.NewAtom("C")); // 10
            mol.Atoms.Add(builder.NewAtom("C")); // 11

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6

            MolAddBond(mol, 0, 6, BondOrder.Single); // 7
            MolAddBond(mol, 6, 7, BondOrder.Single); // 8
            MolAddBond(mol, 7, 8, BondOrder.Double); // 5
            MolAddBond(mol, 8, 9, BondOrder.Single); // 6
            MolAddBond(mol, 9, 10, BondOrder.Double); // 7
            MolAddBond(mol, 10, 11, BondOrder.Single); // 8
            MolAddBond(mol, 11, 6, BondOrder.Double); // 5
            return mol;
        }

        public static IAtomContainer MakePhenylEthylBenzene()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9
            mol.Atoms.Add(builder.NewAtom("C")); // 10
            mol.Atoms.Add(builder.NewAtom("C")); // 11
            mol.Atoms.Add(builder.NewAtom("C")); // 12
            mol.Atoms.Add(builder.NewAtom("C")); // 13

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6

            MolAddBond(mol, 0, 6, BondOrder.Single); // 7
            MolAddBond(mol, 6, 7, BondOrder.Single); // 8
            MolAddBond(mol, 7, 8, BondOrder.Single); // 5
            MolAddBond(mol, 8, 9, BondOrder.Single); // 6
            MolAddBond(mol, 9, 10, BondOrder.Double); // 7
            MolAddBond(mol, 10, 11, BondOrder.Single); // 8
            MolAddBond(mol, 11, 12, BondOrder.Double); // 5
            MolAddBond(mol, 12, 13, BondOrder.Single);
            MolAddBond(mol, 13, 8, BondOrder.Double); // 5
            return mol;
        }

        public static IAtomContainer MakePhenylAmine()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("N")); // 6

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6

            MolAddBond(mol, 0, 6, BondOrder.Single); // 7
            return mol;
        }

        /* build a molecule from 4 condensed triangles */
        public static IAtomContainer Make4x3CondensedRings()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 0, BondOrder.Single); // 3
            MolAddBond(mol, 2, 3, BondOrder.Single); // 4
            MolAddBond(mol, 1, 3, BondOrder.Single); // 5
            MolAddBond(mol, 3, 4, BondOrder.Single); // 6
            MolAddBond(mol, 4, 2, BondOrder.Single); // 7
            MolAddBond(mol, 4, 5, BondOrder.Single); // 8
            MolAddBond(mol, 5, 3, BondOrder.Single); // 9

            return mol;
        }

        public static IAtomContainer MakeSpiroRings()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 6, BondOrder.Single); // 6
            MolAddBond(mol, 6, 0, BondOrder.Single); // 7
            MolAddBond(mol, 6, 7, BondOrder.Single); // 8
            MolAddBond(mol, 7, 8, BondOrder.Single); // 9
            MolAddBond(mol, 8, 9, BondOrder.Single); // 10
            MolAddBond(mol, 9, 6, BondOrder.Single); // 11
            return mol;
        }

        public static IAtomContainer MakeBicycloRings()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6
            MolAddBond(mol, 6, 0, BondOrder.Single); // 7
            MolAddBond(mol, 6, 7, BondOrder.Single); // 8
            MolAddBond(mol, 7, 3, BondOrder.Single); // 9
            return mol;
        }

        public static IAtomContainer MakeFusedRings()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6
            MolAddBond(mol, 5, 6, BondOrder.Single); // 7
            MolAddBond(mol, 6, 7, BondOrder.Single); // 8
            MolAddBond(mol, 7, 4, BondOrder.Single); // 9
            MolAddBond(mol, 8, 0, BondOrder.Single); // 10
            MolAddBond(mol, 9, 1, BondOrder.Single); // 11
            MolAddBond(mol, 9, 8, BondOrder.Single); // 11
            return mol;
        }

        public static IAtomContainer MakeMethylDecaline()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9
            mol.Atoms.Add(builder.NewAtom("C")); // 10

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6
            MolAddBond(mol, 5, 6, BondOrder.Single); // 7
            MolAddBond(mol, 6, 7, BondOrder.Single); // 8RingSet
            MolAddBond(mol, 7, 8, BondOrder.Single); // 9
            MolAddBond(mol, 8, 9, BondOrder.Single); // 10
            MolAddBond(mol, 9, 0, BondOrder.Single); // 11
            MolAddBond(mol, 5, 10, BondOrder.Single); // 12
            return mol;

        }

        public static IAtomContainer MakeEthylPropylPhenantren()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9
            mol.Atoms.Add(builder.NewAtom("C")); // 10
            mol.Atoms.Add(builder.NewAtom("C")); // 11
            mol.Atoms.Add(builder.NewAtom("C")); // 12
            mol.Atoms.Add(builder.NewAtom("C")); // 13
            mol.Atoms.Add(builder.NewAtom("C")); // 14
            mol.Atoms.Add(builder.NewAtom("C")); // 15
            mol.Atoms.Add(builder.NewAtom("C")); // 16
            mol.Atoms.Add(builder.NewAtom("C")); // 17
            mol.Atoms.Add(builder.NewAtom("C")); // 18

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Double); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Double); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 6, BondOrder.Double); // 6
            MolAddBond(mol, 6, 7, BondOrder.Single); // 8
            MolAddBond(mol, 7, 8, BondOrder.Double); // 9
            MolAddBond(mol, 8, 9, BondOrder.Single); // 10
            MolAddBond(mol, 9, 0, BondOrder.Double); // 11
            MolAddBond(mol, 9, 4, BondOrder.Single); // 12
            MolAddBond(mol, 8, 10, BondOrder.Single); // 12
            MolAddBond(mol, 10, 11, BondOrder.Double); // 12
            MolAddBond(mol, 11, 12, BondOrder.Single); // 12
            MolAddBond(mol, 12, 13, BondOrder.Double); // 12
            MolAddBond(mol, 13, 7, BondOrder.Single); // 12
            MolAddBond(mol, 3, 14, BondOrder.Single); // 12
            MolAddBond(mol, 14, 15, BondOrder.Single); // 12
            MolAddBond(mol, 12, 16, BondOrder.Single); // 12
            MolAddBond(mol, 16, 17, BondOrder.Single); // 12
            MolAddBond(mol, 17, 18, BondOrder.Single); // 12
            ConfigureAtoms(mol);
            return mol;
        }

        public static IAtomContainer MakeSteran()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9
            mol.Atoms.Add(builder.NewAtom("C")); // 10
            mol.Atoms.Add(builder.NewAtom("C")); // 11
            mol.Atoms.Add(builder.NewAtom("C")); // 12
            mol.Atoms.Add(builder.NewAtom("C")); // 13
            mol.Atoms.Add(builder.NewAtom("C")); // 14
            mol.Atoms.Add(builder.NewAtom("C")); // 15
            mol.Atoms.Add(builder.NewAtom("C")); // 16

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 6, BondOrder.Single); // 6
            MolAddBond(mol, 6, 7, BondOrder.Single); // 8
            MolAddBond(mol, 7, 8, BondOrder.Single); // 9
            MolAddBond(mol, 8, 9, BondOrder.Single); // 10
            MolAddBond(mol, 9, 0, BondOrder.Single); // 11
            MolAddBond(mol, 9, 4, BondOrder.Single); // 12
            MolAddBond(mol, 8, 10, BondOrder.Single); // 13
            MolAddBond(mol, 10, 11, BondOrder.Single); // 14
            MolAddBond(mol, 11, 12, BondOrder.Single); // 15
            MolAddBond(mol, 12, 13, BondOrder.Single); // 16
            MolAddBond(mol, 13, 7, BondOrder.Single); // 17
            MolAddBond(mol, 13, 14, BondOrder.Single); // 18
            MolAddBond(mol, 14, 15, BondOrder.Single); // 19
            MolAddBond(mol, 15, 16, BondOrder.Single); // 20
            MolAddBond(mol, 16, 12, BondOrder.Single); // 21

            ConfigureAtoms(mol);
            return mol;
        }

        /// <summary>
        /// Returns azulene without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C10H8/c1-2-5-9-7-4-8-10(9)6-3-1/h1-8H
        public static IAtomContainer MakeAzulene()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 6, BondOrder.Single); // 6
            MolAddBond(mol, 6, 7, BondOrder.Double); // 8
            MolAddBond(mol, 7, 8, BondOrder.Single); // 9
            MolAddBond(mol, 8, 9, BondOrder.Double); // 10
            MolAddBond(mol, 9, 5, BondOrder.Single); // 11
            MolAddBond(mol, 9, 0, BondOrder.Single); // 12

            return mol;
        }

        /// <summary>
        /// Returns indole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C8H7N/c1-2-4-8-7(3-1)5-6-9-8/h1-6,9H
        public static IAtomContainer MakeIndole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("N")); // 8

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 6, BondOrder.Single); // 6
            MolAddBond(mol, 6, 7, BondOrder.Double); // 8
            MolAddBond(mol, 7, 8, BondOrder.Single); // 9
            MolAddBond(mol, 0, 5, BondOrder.Single); // 11
            MolAddBond(mol, 8, 0, BondOrder.Single); // 12

            return mol;
        }

        /// <summary>
        /// Returns pyrrole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C4H5N/c1-2-4-5-3-1/h1-5H
        public static IAtomContainer MakePyrrole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns pyrrole anion without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C4H4N/c1-2-4-5-3-1/h1-4H/q-1
        public static IAtomContainer MakePyrroleAnion()
        {
            var mol = NewAtomContainer();
            var nitrogenAnion = builder.NewAtom("N");
            nitrogenAnion.FormalCharge = -1;
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(nitrogenAnion); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns imidazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C3H4N2/c1-2-5-3-4-1/h1-3H,(H,4,5)/f/h4H
        public static IAtomContainer MakeImidazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("N")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns pyrazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C3H4N2/c1-2-4-5-3-1/h1-3H,(H,4,5)/f/h4H
        public static IAtomContainer MakePyrazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("N")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns 1,2,4-triazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C3H4N2/c1-2-4-5-3-1/h1-3H,(H,4,5)/f/h4H
        public static IAtomContainer Make124Triazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("N")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("N")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns 1,2,3-triazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C2H3N3/c1-2-4-5-3-1/h1-2H,(H,3,4,5)/f/h5H
        public static IAtomContainer Make123Triazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("N")); // 2
            mol.Atoms.Add(builder.NewAtom("N")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns tetrazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/CH2N4/c1-2-4-5-3-1/h1H,(H,2,3,4,5)/f/h4H
        public static IAtomContainer MakeTetrazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("N")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("N")); // 2
            mol.Atoms.Add(builder.NewAtom("N")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns oxazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C3H3NO/c1-2-5-3-4-1/h1-3H
        public static IAtomContainer MakeOxazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("O")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("N")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns isoxazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C3H3NO/c1-2-4-5-3-1/h1-3H
        public static IAtomContainer MakeIsoxazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("O")); // 1
            mol.Atoms.Add(builder.NewAtom("N")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns isothiazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C3H3NS/c1-2-4-5-3-1/h1-3H
        public static IAtomContainer MakeIsothiazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("S")); // 1
            mol.Atoms.Add(builder.NewAtom("N")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns thiadiazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C2H2N2S/c1-3-4-2-5-1/h1-2H
        public static IAtomContainer MakeThiadiazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("S")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("N")); // 3
            mol.Atoms.Add(builder.NewAtom("N")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns oxadiazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C2H2N2O/c1-3-4-2-5-1/h1-2H
        public static IAtomContainer MakeOxadiazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("O")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("N")); // 3
            mol.Atoms.Add(builder.NewAtom("N")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        /// <summary>
        /// Returns pyridine without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C3H3NO/c1-2-4-5-3-1/h1-3H
        public static IAtomContainer MakePyridine()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6

            return mol;
        }

        /// <summary>
        /// Returns pyridine oxide without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C5H5NO/c7-6-4-2-1-3-5-6/h1-5H
        public static IAtomContainer MakePyridineOxide()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms[1].FormalCharge = 1;
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("O")); // 6
            mol.Atoms[6].FormalCharge = -1;

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6
            MolAddBond(mol, 1, 6, BondOrder.Single); // 7

            return mol;
        }

        /// <summary>
        /// Returns pyrimidine without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C4H4N2/c1-2-5-4-6-3-1/h1-4H
        public static IAtomContainer MakePyrimidine()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("N")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6

            return mol;
        }

        /// <summary>
        /// Returns pyridazine without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C4H4N2/c1-2-4-6-5-3-1/h1-4H
        public static IAtomContainer MakePyridazine()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("N")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6

            return mol;
        }

        /// <summary>
        /// Returns triazine without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C4H4N2/c1-2-4-6-5-3-1/h1-4H
        public static IAtomContainer MakeTriazine()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("N")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("N")); // 5

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Double); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6

            return mol;
        }

        /// <summary>
        /// Returns thiazole without explicit hydrogens.
        /// </summary>
        // @cdk.inchi InChI=1/C3H3NS/c1-2-5-3-4-1/h1-3H
        public static IAtomContainer MakeThiazole()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("N")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("S")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Double); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 0, BondOrder.Double); // 5

            return mol;
        }

        public static IAtomContainer MakeSingleRing()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            //        mol.Add(builder.NewAtom("C")); // 6
            //        mol.Add(builder.NewAtom("C")); // 7
            //        mol.Add(builder.NewAtom("C")); // 8
            //        mol.Add(builder.NewAtom("C")); // 9

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6
            //        MolAddBond(mol, 5, 6, BondOrder.Single); // 7
            //        MolAddBond(mol, 6, 7, BondOrder.Single); // 8
            //        MolAddBond(mol, 7, 4, BondOrder.Single); // 9
            //        MolAddBond(mol, 8, 0, BondOrder.Single); // 10
            //        MolAddBond(mol, 9, 1, BondOrder.Single); // 11

            return mol;
        }

        public static IAtomContainer MakeDiamantane()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9
            mol.Atoms.Add(builder.NewAtom("C")); // 10
            mol.Atoms.Add(builder.NewAtom("C")); // 11
            mol.Atoms.Add(builder.NewAtom("C")); // 12
            mol.Atoms.Add(builder.NewAtom("C")); // 13

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Single); // 6
            MolAddBond(mol, 5, 6, BondOrder.Single); // 7
            MolAddBond(mol, 6, 9, BondOrder.Single); // 8
            MolAddBond(mol, 1, 7, BondOrder.Single); // 9
            MolAddBond(mol, 7, 9, BondOrder.Single); // 10
            MolAddBond(mol, 3, 8, BondOrder.Single); // 11
            MolAddBond(mol, 8, 9, BondOrder.Single); // 12
            MolAddBond(mol, 0, 10, BondOrder.Single); // 13
            MolAddBond(mol, 10, 13, BondOrder.Single); // 14
            MolAddBond(mol, 2, 11, BondOrder.Single); // 15
            MolAddBond(mol, 11, 13, BondOrder.Single); // 16
            MolAddBond(mol, 4, 12, BondOrder.Single); // 17
            MolAddBond(mol, 12, 13, BondOrder.Single); // 18

            return mol;
        }

        public static IAtomContainer MakeBranchedAliphatic()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("C")); // 7
            mol.Atoms.Add(builder.NewAtom("C")); // 8
            mol.Atoms.Add(builder.NewAtom("C")); // 9
            mol.Atoms.Add(builder.NewAtom("C")); // 10
            mol.Atoms.Add(builder.NewAtom("C")); // 11
            mol.Atoms.Add(builder.NewAtom("C")); // 12
            mol.Atoms.Add(builder.NewAtom("C")); // 13
            mol.Atoms.Add(builder.NewAtom("C")); // 14
            mol.Atoms.Add(builder.NewAtom("C")); // 15
            mol.Atoms.Add(builder.NewAtom("C")); // 16
            mol.Atoms.Add(builder.NewAtom("C")); // 17
            mol.Atoms.Add(builder.NewAtom("C")); // 18

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 2, 6, BondOrder.Single); // 6
            MolAddBond(mol, 6, 7, BondOrder.Single); // 7
            MolAddBond(mol, 7, 8, BondOrder.Single); // 8
            MolAddBond(mol, 6, 9, BondOrder.Single); // 9
            MolAddBond(mol, 6, 10, BondOrder.Single); // 10
            MolAddBond(mol, 10, 11, BondOrder.Single); // 11
            MolAddBond(mol, 8, 12, BondOrder.Triple); // 12
            MolAddBond(mol, 12, 13, BondOrder.Single); // 13
            MolAddBond(mol, 11, 14, BondOrder.Single); // 14
            MolAddBond(mol, 9, 15, BondOrder.Single);
            MolAddBond(mol, 15, 16, BondOrder.Double);
            MolAddBond(mol, 16, 17, BondOrder.Double);
            MolAddBond(mol, 17, 18, BondOrder.Single);

            return mol;
        }

        public static IAtomContainer MakeBenzene()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("C")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5

            MolAddBond(mol, 0, 1, BondOrder.Single); // 1
            MolAddBond(mol, 1, 2, BondOrder.Double); // 2
            MolAddBond(mol, 2, 3, BondOrder.Single); // 3
            MolAddBond(mol, 3, 4, BondOrder.Double); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 0, BondOrder.Double); // 6
            return mol;
        }

        public static IAtomContainer MakeQuinone()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("O")); // 0
            mol.Atoms.Add(builder.NewAtom("C")); // 1
            mol.Atoms.Add(builder.NewAtom("C")); // 2
            mol.Atoms.Add(builder.NewAtom("C")); // 3
            mol.Atoms.Add(builder.NewAtom("C")); // 4
            mol.Atoms.Add(builder.NewAtom("C")); // 5
            mol.Atoms.Add(builder.NewAtom("C")); // 6
            mol.Atoms.Add(builder.NewAtom("O")); // 7

            MolAddBond(mol, 0, 1, BondOrder.Double); // 1
            MolAddBond(mol, 1, 2, BondOrder.Single); // 2
            MolAddBond(mol, 2, 3, BondOrder.Double); // 3
            MolAddBond(mol, 3, 4, BondOrder.Single); // 4
            MolAddBond(mol, 4, 5, BondOrder.Single); // 5
            MolAddBond(mol, 5, 6, BondOrder.Double); // 6
            MolAddBond(mol, 6, 1, BondOrder.Single); // 7
            MolAddBond(mol, 4, 7, BondOrder.Double); // 8
            return mol;
        }

        public static IAtomContainer MakePiperidine()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("N"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("H"));

            MolAddBond(mol, 0, 1, BondOrder.Single);
            MolAddBond(mol, 1, 2, BondOrder.Single);
            MolAddBond(mol, 2, 3, BondOrder.Single);
            MolAddBond(mol, 3, 4, BondOrder.Single);
            MolAddBond(mol, 4, 5, BondOrder.Single);
            MolAddBond(mol, 5, 0, BondOrder.Single);

            MolAddBond(mol, 0, 6, BondOrder.Single);

            return mol;

        }

        public static IAtomContainer MakeTetrahydropyran()
        {
            IAtomContainer mol = NewAtomContainer();
            mol.Atoms.Add(builder.NewAtom("O"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("C"));
            mol.Atoms.Add(builder.NewAtom("C"));

            MolAddBond(mol, 0, 1, BondOrder.Single);
            MolAddBond(mol, 1, 2, BondOrder.Single);
            MolAddBond(mol, 2, 3, BondOrder.Single);
            MolAddBond(mol, 3, 4, BondOrder.Single);
            MolAddBond(mol, 4, 5, BondOrder.Single);
            MolAddBond(mol, 5, 0, BondOrder.Single);

            return mol;

        }

        // @cdk.inchi InChI=1/C5H5N5/c6-4-3-5(9-1-7-3)10-2-8-4/h1-2H,(H3,6,7,8,9,10)/f/h7H,6H2
        public static IAtomContainer MakeAdenine()
        {
            IAtomContainer mol = NewAtomContainer(); // Adenine
            IAtom a1 = mol.Builder.NewAtom("C");
            a1.Point2D = new Vector2(21.0223, -17.2946);
            mol.Atoms.Add(a1);
            IAtom a2 = mol.Builder.NewAtom("C");
            a2.Point2D = new Vector2(21.0223, -18.8093);
            mol.Atoms.Add(a2);
            IAtom a3 = mol.Builder.NewAtom("C");
            a3.Point2D = new Vector2(22.1861, -16.6103);
            mol.Atoms.Add(a3);
            IAtom a4 = mol.Builder.NewAtom("N");
            a4.Point2D = new Vector2(19.8294, -16.8677);
            mol.Atoms.Add(a4);
            IAtom a5 = mol.Builder.NewAtom("N");
            a5.Point2D = new Vector2(22.2212, -19.5285);
            mol.Atoms.Add(a5);
            IAtom a6 = mol.Builder.NewAtom("N");
            a6.Point2D = new Vector2(19.8177, -19.2187);
            mol.Atoms.Add(a6);
            IAtom a7 = mol.Builder.NewAtom("N");
            a7.Point2D = new Vector2(23.4669, -17.3531);
            mol.Atoms.Add(a7);
            IAtom a8 = mol.Builder.NewAtom("N");
            a8.Point2D = new Vector2(22.1861, -15.2769);
            mol.Atoms.Add(a8);
            IAtom a9 = mol.Builder.NewAtom("C");
            a9.Point2D = new Vector2(18.9871, -18.0139);
            mol.Atoms.Add(a9);
            IAtom a10 = mol.Builder.NewAtom("C");
            a10.Point2D = new Vector2(23.4609, -18.8267);
            mol.Atoms.Add(a10);
            IBond b1 = mol.Builder.NewBond(a1, a2, BondOrder.Double);
            mol.Bonds.Add(b1);
            IBond b2 = mol.Builder.NewBond(a1, a3, BondOrder.Single);
            mol.Bonds.Add(b2);
            IBond b3 = mol.Builder.NewBond(a1, a4, BondOrder.Single);
            mol.Bonds.Add(b3);
            IBond b4 = mol.Builder.NewBond(a2, a5, BondOrder.Single);
            mol.Bonds.Add(b4);
            IBond b5 = mol.Builder.NewBond(a2, a6, BondOrder.Single);
            mol.Bonds.Add(b5);
            IBond b6 = mol.Builder.NewBond(a3, a7, BondOrder.Double);
            mol.Bonds.Add(b6);
            IBond b7 = mol.Builder.NewBond(a3, a8, BondOrder.Single);
            mol.Bonds.Add(b7);
            IBond b8 = mol.Builder.NewBond(a4, a9, BondOrder.Double);
            mol.Bonds.Add(b8);
            IBond b9 = mol.Builder.NewBond(a5, a10, BondOrder.Double);
            mol.Bonds.Add(b9);
            IBond b10 = mol.Builder.NewBond(a6, a9, BondOrder.Single);
            mol.Bonds.Add(b10);
            IBond b11 = mol.Builder.NewBond(a7, a10, BondOrder.Single);
            mol.Bonds.Add(b11);

            return mol;
        }

        /// <summary>
        /// InChI=1/C10H8/c1-2-6-10-8-4-3-7-9(10)5-1/h1-8H
        /// </summary>
        public static IAtomContainer MakeNaphthalene()
        {
            IAtomContainer mol = builder.NewAtomContainer();
            IAtom a1 = builder.NewAtom("C");
            a1.FormalCharge = 0;
            mol.Atoms.Add(a1);
            IAtom a2 = builder.NewAtom("C");
            a2.FormalCharge = 0;
            mol.Atoms.Add(a2);
            IAtom a3 = builder.NewAtom("C");
            a3.FormalCharge = 0;
            mol.Atoms.Add(a3);
            IAtom a4 = builder.NewAtom("C");
            a4.FormalCharge = 0;
            mol.Atoms.Add(a4);
            IAtom a5 = builder.NewAtom("C");
            a5.FormalCharge = 0;
            mol.Atoms.Add(a5);
            IAtom a6 = builder.NewAtom("C");
            a6.FormalCharge = 0;
            mol.Atoms.Add(a6);
            IAtom a7 = builder.NewAtom("C");
            a7.FormalCharge = 0;
            mol.Atoms.Add(a7);
            IAtom a8 = builder.NewAtom("C");
            a8.FormalCharge = 0;
            mol.Atoms.Add(a8);
            IAtom a9 = builder.NewAtom("C");
            a9.FormalCharge = 0;
            mol.Atoms.Add(a9);
            IAtom a10 = builder.NewAtom("C");
            a10.FormalCharge = 0;
            mol.Atoms.Add(a10);
            IBond b1 = builder.NewBond(a1, a2, BondOrder.Double);
            mol.Bonds.Add(b1);
            IBond b2 = builder.NewBond(a2, a3, BondOrder.Single);
            mol.Bonds.Add(b2);
            IBond b3 = builder.NewBond(a3, a4, BondOrder.Double);
            mol.Bonds.Add(b3);
            IBond b4 = builder.NewBond(a4, a5, BondOrder.Single);
            mol.Bonds.Add(b4);
            IBond b5 = builder.NewBond(a5, a6, BondOrder.Double);
            mol.Bonds.Add(b5);
            IBond b6 = builder.NewBond(a6, a7, BondOrder.Single);
            mol.Bonds.Add(b6);
            IBond b7 = builder.NewBond(a7, a8, BondOrder.Double);
            mol.Bonds.Add(b7);
            IBond b8 = builder.NewBond(a3, a8, BondOrder.Single);
            mol.Bonds.Add(b8);
            IBond b9 = builder.NewBond(a8, a9, BondOrder.Single);
            mol.Bonds.Add(b9);
            IBond b10 = builder.NewBond(a9, a10, BondOrder.Double);
            mol.Bonds.Add(b10);
            IBond b11 = builder.NewBond(a1, a10, BondOrder.Single);
            mol.Bonds.Add(b11);
            return mol;
        }

        // @cdk.inchi InChI=1/C14H10/c1-2-6-12-10-14-8-4-3-7-13(14)9-11(12)5-1/h1-10H
        public static IAtomContainer MakeAnthracene()
        {
            IAtomContainer mol = builder.NewAtomContainer();
            IAtom a1 = builder.NewAtom("C");
            a1.FormalCharge = 0;
            mol.Atoms.Add(a1);
            IAtom a2 = builder.NewAtom("C");
            a2.FormalCharge = 0;
            mol.Atoms.Add(a2);
            IAtom a3 = builder.NewAtom("C");
            a3.FormalCharge = 0;
            mol.Atoms.Add(a3);
            IAtom a4 = builder.NewAtom("C");
            a4.FormalCharge = 0;
            mol.Atoms.Add(a4);
            IAtom a5 = builder.NewAtom("C");
            a5.FormalCharge = 0;
            mol.Atoms.Add(a5);
            IAtom a6 = builder.NewAtom("C");
            a6.FormalCharge = 0;
            mol.Atoms.Add(a6);
            IAtom a7 = builder.NewAtom("C");
            a7.FormalCharge = 0;
            mol.Atoms.Add(a7);
            IAtom a8 = builder.NewAtom("C");
            a8.FormalCharge = 0;
            mol.Atoms.Add(a8);
            IAtom a9 = builder.NewAtom("C");
            a9.FormalCharge = 0;
            mol.Atoms.Add(a9);
            IAtom a10 = builder.NewAtom("C");
            a10.FormalCharge = 0;
            mol.Atoms.Add(a10);
            IAtom a11 = builder.NewAtom("C");
            a11.FormalCharge = 0;
            mol.Atoms.Add(a11);
            IAtom a12 = builder.NewAtom("C");
            a12.FormalCharge = 0;
            mol.Atoms.Add(a12);
            IAtom a13 = builder.NewAtom("C");
            a13.FormalCharge = 0;
            mol.Atoms.Add(a13);
            IAtom a14 = builder.NewAtom("C");
            a14.FormalCharge = 0;
            mol.Atoms.Add(a14);
            IBond b1 = builder.NewBond(a1, a2, BondOrder.Double);
            mol.Bonds.Add(b1);
            IBond b2 = builder.NewBond(a2, a3, BondOrder.Single);
            mol.Bonds.Add(b2);
            IBond b3 = builder.NewBond(a3, a4, BondOrder.Double);
            mol.Bonds.Add(b3);
            IBond b4 = builder.NewBond(a4, a5, BondOrder.Single);
            mol.Bonds.Add(b4);
            IBond b5 = builder.NewBond(a5, a6, BondOrder.Double);
            mol.Bonds.Add(b5);
            IBond b6 = builder.NewBond(a6, a7, BondOrder.Single);
            mol.Bonds.Add(b6);
            IBond b7 = builder.NewBond(a7, a8, BondOrder.Double);
            mol.Bonds.Add(b7);
            IBond b8 = builder.NewBond(a8, a9, BondOrder.Single);
            mol.Bonds.Add(b8);
            IBond b9 = builder.NewBond(a9, a10, BondOrder.Double);
            mol.Bonds.Add(b9);
            IBond b10 = builder.NewBond(a5, a10, BondOrder.Single);
            mol.Bonds.Add(b10);
            IBond b11 = builder.NewBond(a10, a11, BondOrder.Single);
            mol.Bonds.Add(b11);
            IBond b12 = builder.NewBond(a11, a12, BondOrder.Double);
            mol.Bonds.Add(b12);
            IBond b13 = builder.NewBond(a3, a12, BondOrder.Single);
            mol.Bonds.Add(b13);
            IBond b14 = builder.NewBond(a12, a13, BondOrder.Single);
            mol.Bonds.Add(b14);
            IBond b15 = builder.NewBond(a13, a14, BondOrder.Double);
            mol.Bonds.Add(b15);
            IBond b16 = builder.NewBond(a1, a14, BondOrder.Single);
            mol.Bonds.Add(b16);
            return mol;
        }

        /// <summary>
        /// octacyclo[17.2.2.2¹,⁴.2⁴,⁷.2⁷,¹⁰.2¹⁰,¹³.2¹³,¹⁶.2¹⁶,¹⁹]pentatriacontane
        /// </summary>
        // @cdk.inchi InChI=1/C35H56/c1-2-30-6-3-29(1)4-7-31(8-5-29)13-15-33(16-14-31)21-23-35(24-22-33)27-25-34(26-28-35)19-17-32(11-9-30,12-10-30)18-20-34/h1-28H2
        public static IAtomContainer MakeCyclophaneLike()
        {
            IAtomContainer mol = builder.NewAtomContainer();
            IAtom a1 = builder.NewAtom("C");
            a1.FormalCharge = 0;
            mol.Atoms.Add(a1);
            IAtom a2 = builder.NewAtom("C");
            a2.FormalCharge = 0;
            mol.Atoms.Add(a2);
            IAtom a3 = builder.NewAtom("C");
            a3.FormalCharge = 0;
            mol.Atoms.Add(a3);
            IAtom a4 = builder.NewAtom("C");
            a4.FormalCharge = 0;
            mol.Atoms.Add(a4);
            IAtom a5 = builder.NewAtom("C");
            a5.FormalCharge = 0;
            mol.Atoms.Add(a5);
            IAtom a6 = builder.NewAtom("C");
            a6.FormalCharge = 0;
            mol.Atoms.Add(a6);
            IAtom a7 = builder.NewAtom("C");
            a7.FormalCharge = 0;
            mol.Atoms.Add(a7);
            IAtom a8 = builder.NewAtom("C");
            a8.FormalCharge = 0;
            mol.Atoms.Add(a8);
            IAtom a9 = builder.NewAtom("C");
            a9.FormalCharge = 0;
            mol.Atoms.Add(a9);
            IAtom a10 = builder.NewAtom("C");
            a10.FormalCharge = 0;
            mol.Atoms.Add(a10);
            IAtom a11 = builder.NewAtom("C");
            a11.FormalCharge = 0;
            mol.Atoms.Add(a11);
            IAtom a12 = builder.NewAtom("C");
            a12.FormalCharge = 0;
            mol.Atoms.Add(a12);
            IAtom a13 = builder.NewAtom("C");
            a13.FormalCharge = 0;
            mol.Atoms.Add(a13);
            IAtom a14 = builder.NewAtom("C");
            a14.FormalCharge = 0;
            mol.Atoms.Add(a14);
            IAtom a15 = builder.NewAtom("C");
            a15.FormalCharge = 0;
            mol.Atoms.Add(a15);
            IAtom a16 = builder.NewAtom("C");
            a16.FormalCharge = 0;
            mol.Atoms.Add(a16);
            IAtom a17 = builder.NewAtom("C");
            a17.FormalCharge = 0;
            mol.Atoms.Add(a17);
            IAtom a18 = builder.NewAtom("C");
            a18.FormalCharge = 0;
            mol.Atoms.Add(a18);
            IAtom a19 = builder.NewAtom("C");
            a19.FormalCharge = 0;
            mol.Atoms.Add(a19);
            IAtom a20 = builder.NewAtom("C");
            a20.FormalCharge = 0;
            mol.Atoms.Add(a20);
            IAtom a21 = builder.NewAtom("C");
            a21.FormalCharge = 0;
            mol.Atoms.Add(a21);
            IAtom a22 = builder.NewAtom("C");
            a22.FormalCharge = 0;
            mol.Atoms.Add(a22);
            IAtom a23 = builder.NewAtom("C");
            a23.FormalCharge = 0;
            mol.Atoms.Add(a23);
            IAtom a24 = builder.NewAtom("C");
            a24.FormalCharge = 0;
            mol.Atoms.Add(a24);
            IAtom a25 = builder.NewAtom("C");
            a25.FormalCharge = 0;
            mol.Atoms.Add(a25);
            IAtom a26 = builder.NewAtom("C");
            a26.FormalCharge = 0;
            mol.Atoms.Add(a26);
            IAtom a27 = builder.NewAtom("C");
            a27.FormalCharge = 0;
            mol.Atoms.Add(a27);
            IAtom a28 = builder.NewAtom("C");
            a28.FormalCharge = 0;
            mol.Atoms.Add(a28);
            IAtom a29 = builder.NewAtom("C");
            a29.FormalCharge = 0;
            mol.Atoms.Add(a29);
            IAtom a30 = builder.NewAtom("C");
            a30.FormalCharge = 0;
            mol.Atoms.Add(a30);
            IAtom a31 = builder.NewAtom("C");
            a31.FormalCharge = 0;
            mol.Atoms.Add(a31);
            IAtom a32 = builder.NewAtom("C");
            a32.FormalCharge = 0;
            mol.Atoms.Add(a32);
            IAtom a33 = builder.NewAtom("C");
            a33.FormalCharge = 0;
            mol.Atoms.Add(a33);
            IAtom a34 = builder.NewAtom("C");
            a34.FormalCharge = 0;
            mol.Atoms.Add(a34);
            IAtom a35 = builder.NewAtom("C");
            a35.FormalCharge = 0;
            mol.Atoms.Add(a35);
            IBond b1 = builder.NewBond(a1, a2, BondOrder.Single);
            mol.Bonds.Add(b1);
            IBond b2 = builder.NewBond(a2, a3, BondOrder.Single);
            mol.Bonds.Add(b2);
            IBond b3 = builder.NewBond(a3, a4, BondOrder.Single);
            mol.Bonds.Add(b3);
            IBond b4 = builder.NewBond(a4, a5, BondOrder.Single);
            mol.Bonds.Add(b4);
            IBond b5 = builder.NewBond(a5, a6, BondOrder.Single);
            mol.Bonds.Add(b5);
            IBond b6 = builder.NewBond(a1, a6, BondOrder.Single);
            mol.Bonds.Add(b6);
            IBond b7 = builder.NewBond(a6, a7, BondOrder.Single);
            mol.Bonds.Add(b7);
            IBond b8 = builder.NewBond(a7, a8, BondOrder.Single);
            mol.Bonds.Add(b8);
            IBond b9 = builder.NewBond(a8, a9, BondOrder.Single);
            mol.Bonds.Add(b9);
            IBond b10 = builder.NewBond(a9, a10, BondOrder.Single);
            mol.Bonds.Add(b10);
            IBond b11 = builder.NewBond(a10, a11, BondOrder.Single);
            mol.Bonds.Add(b11);
            IBond b12 = builder.NewBond(a6, a11, BondOrder.Single);
            mol.Bonds.Add(b12);
            IBond b13 = builder.NewBond(a9, a12, BondOrder.Single);
            mol.Bonds.Add(b13);
            IBond b14 = builder.NewBond(a12, a13, BondOrder.Single);
            mol.Bonds.Add(b14);
            IBond b15 = builder.NewBond(a13, a14, BondOrder.Single);
            mol.Bonds.Add(b15);
            IBond b16 = builder.NewBond(a14, a15, BondOrder.Single);
            mol.Bonds.Add(b16);
            IBond b17 = builder.NewBond(a15, a16, BondOrder.Single);
            mol.Bonds.Add(b17);
            IBond b18 = builder.NewBond(a9, a16, BondOrder.Single);
            mol.Bonds.Add(b18);
            IBond b19 = builder.NewBond(a14, a17, BondOrder.Single);
            mol.Bonds.Add(b19);
            IBond b20 = builder.NewBond(a17, a18, BondOrder.Single);
            mol.Bonds.Add(b20);
            IBond b21 = builder.NewBond(a18, a19, BondOrder.Single);
            mol.Bonds.Add(b21);
            IBond b22 = builder.NewBond(a19, a20, BondOrder.Single);
            mol.Bonds.Add(b22);
            IBond b23 = builder.NewBond(a20, a21, BondOrder.Single);
            mol.Bonds.Add(b23);
            IBond b24 = builder.NewBond(a14, a21, BondOrder.Single);
            mol.Bonds.Add(b24);
            IBond b25 = builder.NewBond(a19, a22, BondOrder.Single);
            mol.Bonds.Add(b25);
            IBond b26 = builder.NewBond(a22, a23, BondOrder.Single);
            mol.Bonds.Add(b26);
            IBond b27 = builder.NewBond(a23, a24, BondOrder.Single);
            mol.Bonds.Add(b27);
            IBond b28 = builder.NewBond(a24, a25, BondOrder.Single);
            mol.Bonds.Add(b28);
            IBond b29 = builder.NewBond(a25, a26, BondOrder.Single);
            mol.Bonds.Add(b29);
            IBond b30 = builder.NewBond(a26, a27, BondOrder.Single);
            mol.Bonds.Add(b30);
            IBond b31 = builder.NewBond(a27, a28, BondOrder.Single);
            mol.Bonds.Add(b31);
            IBond b32 = builder.NewBond(a28, a29, BondOrder.Single);
            mol.Bonds.Add(b32);
            IBond b33 = builder.NewBond(a3, a29, BondOrder.Single);
            mol.Bonds.Add(b33);
            IBond b34 = builder.NewBond(a27, a30, BondOrder.Single);
            mol.Bonds.Add(b34);
            IBond b35 = builder.NewBond(a30, a31, BondOrder.Single);
            mol.Bonds.Add(b35);
            IBond b36 = builder.NewBond(a3, a31, BondOrder.Single);
            mol.Bonds.Add(b36);
            IBond b37 = builder.NewBond(a27, a32, BondOrder.Single);
            mol.Bonds.Add(b37);
            IBond b38 = builder.NewBond(a32, a33, BondOrder.Single);
            mol.Bonds.Add(b38);
            IBond b39 = builder.NewBond(a24, a33, BondOrder.Single);
            mol.Bonds.Add(b39);
            IBond b40 = builder.NewBond(a24, a34, BondOrder.Single);
            mol.Bonds.Add(b40);
            IBond b41 = builder.NewBond(a34, a35, BondOrder.Single);
            mol.Bonds.Add(b41);
            IBond b42 = builder.NewBond(a19, a35, BondOrder.Single);
            mol.Bonds.Add(b42);
            return mol;
        }

        /// <summary>
        /// octacyclo[24.2.2.2²,⁵.2⁶,⁹.2¹⁰,¹³.2¹⁴,¹⁷.2¹⁸,²¹.2²²,²⁵]dotetracontane
        /// </summary>
        // @cdk.inchi InChI=1/C42H70/c1-2-30-4-3-29(1)31-5-7-33(8-6-31)35-13-15-37(16-14-35)39-21-23-41(24-22-39)42-27-25-40(26-28-42)38-19-17-36(18-20-38)34-11-9-32(30)10-12-34/h29-42H,1-28H2
        public static IAtomContainer MakeGappedCyclophaneLike()
        {
            IAtomContainer mol = builder.NewAtomContainer();
            IAtom a1 = builder.NewAtom("C");
            a1.FormalCharge = 0;
            mol.Atoms.Add(a1);
            IAtom a2 = builder.NewAtom("C");
            a2.FormalCharge = 0;
            mol.Atoms.Add(a2);
            IAtom a3 = builder.NewAtom("C");
            a3.FormalCharge = 0;
            mol.Atoms.Add(a3);
            IAtom a4 = builder.NewAtom("C");
            a4.FormalCharge = 0;
            mol.Atoms.Add(a4);
            IAtom a5 = builder.NewAtom("C");
            a5.FormalCharge = 0;
            mol.Atoms.Add(a5);
            IAtom a6 = builder.NewAtom("C");
            a6.FormalCharge = 0;
            mol.Atoms.Add(a6);
            IAtom a7 = builder.NewAtom("C");
            a7.FormalCharge = 0;
            mol.Atoms.Add(a7);
            IAtom a8 = builder.NewAtom("C");
            a8.FormalCharge = 0;
            mol.Atoms.Add(a8);
            IAtom a9 = builder.NewAtom("C");
            a9.FormalCharge = 0;
            mol.Atoms.Add(a9);
            IAtom a10 = builder.NewAtom("C");
            a10.FormalCharge = 0;
            mol.Atoms.Add(a10);
            IAtom a11 = builder.NewAtom("C");
            a11.FormalCharge = 0;
            mol.Atoms.Add(a11);
            IAtom a12 = builder.NewAtom("C");
            a12.FormalCharge = 0;
            mol.Atoms.Add(a12);
            IAtom a13 = builder.NewAtom("C");
            a13.FormalCharge = 0;
            mol.Atoms.Add(a13);
            IAtom a14 = builder.NewAtom("C");
            a14.FormalCharge = 0;
            mol.Atoms.Add(a14);
            IAtom a15 = builder.NewAtom("C");
            a15.FormalCharge = 0;
            mol.Atoms.Add(a15);
            IAtom a16 = builder.NewAtom("C");
            a16.FormalCharge = 0;
            mol.Atoms.Add(a16);
            IAtom a17 = builder.NewAtom("C");
            a17.FormalCharge = 0;
            mol.Atoms.Add(a17);
            IAtom a18 = builder.NewAtom("C");
            a18.FormalCharge = 0;
            mol.Atoms.Add(a18);
            IAtom a19 = builder.NewAtom("C");
            a19.FormalCharge = 0;
            mol.Atoms.Add(a19);
            IAtom a20 = builder.NewAtom("C");
            a20.FormalCharge = 0;
            mol.Atoms.Add(a20);
            IAtom a21 = builder.NewAtom("C");
            a21.FormalCharge = 0;
            mol.Atoms.Add(a21);
            IAtom a22 = builder.NewAtom("C");
            a22.FormalCharge = 0;
            mol.Atoms.Add(a22);
            IAtom a23 = builder.NewAtom("C");
            a23.FormalCharge = 0;
            mol.Atoms.Add(a23);
            IAtom a24 = builder.NewAtom("C");
            a24.FormalCharge = 0;
            mol.Atoms.Add(a24);
            IAtom a25 = builder.NewAtom("C");
            a25.FormalCharge = 0;
            mol.Atoms.Add(a25);
            IAtom a26 = builder.NewAtom("C");
            a26.FormalCharge = 0;
            mol.Atoms.Add(a26);
            IAtom a27 = builder.NewAtom("C");
            a27.FormalCharge = 0;
            mol.Atoms.Add(a27);
            IAtom a28 = builder.NewAtom("C");
            a28.FormalCharge = 0;
            mol.Atoms.Add(a28);
            IAtom a29 = builder.NewAtom("C");
            a29.FormalCharge = 0;
            mol.Atoms.Add(a29);
            IAtom a30 = builder.NewAtom("C");
            a30.FormalCharge = 0;
            mol.Atoms.Add(a30);
            IAtom a31 = builder.NewAtom("C");
            a31.FormalCharge = 0;
            mol.Atoms.Add(a31);
            IAtom a32 = builder.NewAtom("C");
            a32.FormalCharge = 0;
            mol.Atoms.Add(a32);
            IAtom a33 = builder.NewAtom("C");
            a33.FormalCharge = 0;
            mol.Atoms.Add(a33);
            IAtom a34 = builder.NewAtom("C");
            a34.FormalCharge = 0;
            mol.Atoms.Add(a34);
            IAtom a35 = builder.NewAtom("C");
            a35.FormalCharge = 0;
            mol.Atoms.Add(a35);
            IAtom a36 = builder.NewAtom("C");
            a36.FormalCharge = 0;
            mol.Atoms.Add(a36);
            IAtom a37 = builder.NewAtom("C");
            a37.FormalCharge = 0;
            mol.Atoms.Add(a37);
            IAtom a38 = builder.NewAtom("C");
            a38.FormalCharge = 0;
            mol.Atoms.Add(a38);
            IAtom a39 = builder.NewAtom("C");
            a39.FormalCharge = 0;
            mol.Atoms.Add(a39);
            IAtom a40 = builder.NewAtom("C");
            a40.FormalCharge = 0;
            mol.Atoms.Add(a40);
            IAtom a41 = builder.NewAtom("C");
            a41.FormalCharge = 0;
            mol.Atoms.Add(a41);
            IAtom a42 = builder.NewAtom("C");
            a42.FormalCharge = 0;
            mol.Atoms.Add(a42);
            IBond b1 = builder.NewBond(a1, a2, BondOrder.Single);
            mol.Bonds.Add(b1);
            IBond b2 = builder.NewBond(a2, a3, BondOrder.Single);
            mol.Bonds.Add(b2);
            IBond b3 = builder.NewBond(a3, a4, BondOrder.Single);
            mol.Bonds.Add(b3);
            IBond b4 = builder.NewBond(a4, a5, BondOrder.Single);
            mol.Bonds.Add(b4);
            IBond b5 = builder.NewBond(a5, a6, BondOrder.Single);
            mol.Bonds.Add(b5);
            IBond b6 = builder.NewBond(a1, a6, BondOrder.Single);
            mol.Bonds.Add(b6);
            IBond b7 = builder.NewBond(a6, a7, BondOrder.Single);
            mol.Bonds.Add(b7);
            IBond b8 = builder.NewBond(a7, a8, BondOrder.Single);
            mol.Bonds.Add(b8);
            IBond b9 = builder.NewBond(a8, a9, BondOrder.Single);
            mol.Bonds.Add(b9);
            IBond b10 = builder.NewBond(a9, a10, BondOrder.Single);
            mol.Bonds.Add(b10);
            IBond b11 = builder.NewBond(a10, a11, BondOrder.Single);
            mol.Bonds.Add(b11);
            IBond b12 = builder.NewBond(a11, a12, BondOrder.Single);
            mol.Bonds.Add(b12);
            IBond b13 = builder.NewBond(a7, a12, BondOrder.Single);
            mol.Bonds.Add(b13);
            IBond b14 = builder.NewBond(a10, a13, BondOrder.Single);
            mol.Bonds.Add(b14);
            IBond b15 = builder.NewBond(a13, a14, BondOrder.Single);
            mol.Bonds.Add(b15);
            IBond b16 = builder.NewBond(a14, a15, BondOrder.Single);
            mol.Bonds.Add(b16);
            IBond b17 = builder.NewBond(a15, a16, BondOrder.Single);
            mol.Bonds.Add(b17);
            IBond b18 = builder.NewBond(a16, a17, BondOrder.Single);
            mol.Bonds.Add(b18);
            IBond b19 = builder.NewBond(a17, a18, BondOrder.Single);
            mol.Bonds.Add(b19);
            IBond b20 = builder.NewBond(a13, a18, BondOrder.Single);
            mol.Bonds.Add(b20);
            IBond b21 = builder.NewBond(a16, a19, BondOrder.Single);
            mol.Bonds.Add(b21);
            IBond b22 = builder.NewBond(a19, a20, BondOrder.Single);
            mol.Bonds.Add(b22);
            IBond b23 = builder.NewBond(a20, a21, BondOrder.Single);
            mol.Bonds.Add(b23);
            IBond b24 = builder.NewBond(a21, a22, BondOrder.Single);
            mol.Bonds.Add(b24);
            IBond b25 = builder.NewBond(a22, a23, BondOrder.Single);
            mol.Bonds.Add(b25);
            IBond b26 = builder.NewBond(a23, a24, BondOrder.Single);
            mol.Bonds.Add(b26);
            IBond b27 = builder.NewBond(a19, a24, BondOrder.Single);
            mol.Bonds.Add(b27);
            IBond b28 = builder.NewBond(a22, a25, BondOrder.Single);
            mol.Bonds.Add(b28);
            IBond b29 = builder.NewBond(a25, a26, BondOrder.Single);
            mol.Bonds.Add(b29);
            IBond b30 = builder.NewBond(a26, a27, BondOrder.Single);
            mol.Bonds.Add(b30);
            IBond b31 = builder.NewBond(a27, a28, BondOrder.Single);
            mol.Bonds.Add(b31);
            IBond b32 = builder.NewBond(a28, a29, BondOrder.Single);
            mol.Bonds.Add(b32);
            IBond b33 = builder.NewBond(a29, a30, BondOrder.Single);
            mol.Bonds.Add(b33);
            IBond b34 = builder.NewBond(a25, a30, BondOrder.Single);
            mol.Bonds.Add(b34);
            IBond b35 = builder.NewBond(a28, a31, BondOrder.Single);
            mol.Bonds.Add(b35);
            IBond b36 = builder.NewBond(a31, a32, BondOrder.Single);
            mol.Bonds.Add(b36);
            IBond b37 = builder.NewBond(a32, a33, BondOrder.Single);
            mol.Bonds.Add(b37);
            IBond b38 = builder.NewBond(a33, a34, BondOrder.Single);
            mol.Bonds.Add(b38);
            IBond b39 = builder.NewBond(a34, a35, BondOrder.Single);
            mol.Bonds.Add(b39);
            IBond b40 = builder.NewBond(a35, a36, BondOrder.Single);
            mol.Bonds.Add(b40);
            IBond b41 = builder.NewBond(a31, a36, BondOrder.Single);
            mol.Bonds.Add(b41);
            IBond b42 = builder.NewBond(a34, a37, BondOrder.Single);
            mol.Bonds.Add(b42);
            IBond b43 = builder.NewBond(a37, a38, BondOrder.Single);
            mol.Bonds.Add(b43);
            IBond b44 = builder.NewBond(a38, a39, BondOrder.Single);
            mol.Bonds.Add(b44);
            IBond b45 = builder.NewBond(a39, a40, BondOrder.Single);
            mol.Bonds.Add(b45);
            IBond b46 = builder.NewBond(a3, a40, BondOrder.Single);
            mol.Bonds.Add(b46);
            IBond b47 = builder.NewBond(a40, a41, BondOrder.Single);
            mol.Bonds.Add(b47);
            IBond b48 = builder.NewBond(a41, a42, BondOrder.Single);
            mol.Bonds.Add(b48);
            IBond b49 = builder.NewBond(a37, a42, BondOrder.Single);
            mol.Bonds.Add(b49);
            return mol;
        }

        public static IAtomContainer MakeWater()
        {
            var mol = CDK.Builder.NewAtomContainer();
            var c1 = CDK.Builder.NewAtom("O");
            c1.Point3D = new Vector3(0.0, 0.0, 0.0);
            var h1 = CDK.Builder.NewAtom("H");
            h1.Point3D = new Vector3(1.0, 0.0, 0.0);
            var h2 = CDK.Builder.NewAtom("H");
            h2.Point3D = new Vector3(-1.0, 0.0, 0.0);
            mol.Atoms.Add(c1);
            mol.Atoms.Add(h1);
            mol.Atoms.Add(h2);
            mol.AddBond(mol.Atoms[0], mol.Atoms[1], BondOrder.Single);
            mol.AddBond(mol.Atoms[0], mol.Atoms[2], BondOrder.Single);
            return mol;
        }

        private static void ConfigureAtoms(IAtomContainer mol)
        {
            try
            {
                foreach (IAtom atom in mol.Atoms)
                    atom.ImplicitHydrogenCount = null;
                BODRIsotopeFactory.Instance.ConfigureAtoms(mol);
            }
            catch (Exception exc)
            {
                Trace.TraceError($"Could not configure molecule! {exc.Message}");
            }
        }
    }
}
