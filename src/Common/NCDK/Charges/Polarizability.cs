/* Copyright (C) 2005-2007  Christian Hoppe <chhoppe@users.sf.net>
 *
 *  Contact: cdk-devel@list.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Graphs;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Charges
{
    /// <summary>
    /// Calculation of the polarizability of a molecule by the method of Kang and
    /// Jhon and Gasteiger based on <token>cdk-cite-KJ81</token> and <token>cdk-cite-GH82</token>
    /// Limitations in parameterization of atoms:
    /// H, Csp3, Csp2, Csp2arom, Csp3, Nsp3, Nsp2, Nsp3,
    /// P, Osp3 and Osp2. Aromaticity must be calculated beforehand.
    /// </summary>
    // @author         chhoppe
    // @cdk.created    2004-11-03
    // @cdk.keyword polarizability
    // @cdk.module     charges
    public static class Polarizability
    {
        private static void AddExplicitHydrogens(IAtomContainer container)
        {
            try
            {
                var matcher = CDK.AtomTypeMatcher;
                foreach (var atom in container.Atoms)
                {
                    var type = matcher.FindMatchingAtomType(container, atom);
                    AtomTypeManipulator.Configure(atom, type);
                }
                var hAdder = CDK.HydrogenAdder;
                hAdder.AddImplicitHydrogens(container);
                AtomContainerManipulator.ConvertImplicitToExplicitHydrogens(container);
            }
            catch (Exception)
            {
                Debug.WriteLine("Error in hydrogen addition");
            }
        }

        /// <summary>
        /// Gets the polarizabilitiy factor for atom.
        /// </summary>
        /// <param name="atomContainer">atom container</param>
        /// <param name="atom">atom for which the factor should become known</param>
        /// <returns>The polarizabilitiy factor for atom</returns>
        public static double GetPolarizabilitiyFactorForAtom(IAtomContainer atomContainer, IAtom atom)
        {
            var acH = atomContainer.Builder.NewAtomContainer(atomContainer);
            AddExplicitHydrogens(acH);
            return GetKJPolarizabilityFactor(acH, atom);
        }

        /// <summary>
        /// Calculates the mean molecular polarizability as described in paper of Kang and Jhorn.
        /// </summary>
        /// <param name="atomContainer">AtomContainer</param>
        /// <returns>polarizabilitiy</returns>
        public static double CalculateKJMeanMolecularPolarizability(IAtomContainer atomContainer)
        {
            double polarizabilitiy = 0;
            IAtomContainer acH = atomContainer.Builder.NewAtomContainer(atomContainer);
            AddExplicitHydrogens(acH);
            for (int i = 0; i < acH.Atoms.Count; i++)
            {
                polarizabilitiy += GetKJPolarizabilityFactor(acH, acH.Atoms[i]);
            }
            return polarizabilitiy;
        }

        /// <summary>
        /// Calculate effective atom polarizability.
        /// </summary>
        /// <param name="atomContainer">IAtomContainer</param>
        /// <param name="atom">atom for which effective atom polarizability should be calculated</param>
        /// <param name="influenceSphereCutOff">cut off for spheres which should taken into account for calculation</param>
        /// <param name="addExplicitH">if set to true, then explicit H's will be added, otherwise it assumes that they have
        ///  been added to the molecule before being called</param>
        /// <returns>polarizabilitiy</returns>
        public static double CalculateGHEffectiveAtomPolarizability(IAtomContainer atomContainer, IAtom atom, int influenceSphereCutOff, bool addExplicitH)
        {
            double polarizabilitiy = 0;

            IAtomContainer acH;
            if (addExplicitH)
            {
                acH = atomContainer.Builder.NewAtomContainer(atomContainer);
                AddExplicitHydrogens(acH);
            }
            else
            {
                acH = atomContainer;
            }

            var startAtom = new List<IAtom>(1);
            startAtom.Insert(0, atom);
            double bond;

            polarizabilitiy += GetKJPolarizabilityFactor(acH, atom);
            for (int i = 0; i < acH.Atoms.Count; i++)
            {
                if (!acH.Atoms[i].Equals(atom))
                {
                    bond = PathTools.BreadthFirstTargetSearch(acH, startAtom, acH.Atoms[i], 0, influenceSphereCutOff);
                    if (bond == 1)
                    {
                        polarizabilitiy += GetKJPolarizabilityFactor(acH, acH.Atoms[i]);
                    }
                    else
                    {
                        polarizabilitiy += (Math.Pow(0.5, bond - 1) * GetKJPolarizabilityFactor(acH, acH.Atoms[i]));
                    }//if bond==0
                }//if !=atom
            }//for
            return polarizabilitiy;
        }

        /// <summary>
        /// Calculate effective atom polarizability.
        /// </summary>
        /// <param name="atomContainer">IAtomContainer</param>
        /// <param name="atom">atom for which effective atom polarizability should be calculated</param>
        /// <param name="addExplicitH">if set to true, then explicit H's will be added, otherwise it assumes that they have
        ///                              been added to the molecule before being called</param>
        /// <param name="distanceMatrix">an n x n matrix of topological distances between all the atoms in the molecule.
        ///                              if this argument is non-null, then BFS will not be used and instead path lengths will be looked up. This
        ///                              form of the method is useful, if it is being called for multiple atoms in the same molecule</param>
        /// <returns>polarizabilitiy</returns>
        public static double CalculateGHEffectiveAtomPolarizability(IAtomContainer atomContainer, IAtom atom, bool addExplicitH, int[][] distanceMatrix)
        {
            double polarizabilitiy = 0;

            IAtomContainer acH;
            if (addExplicitH)
            {
                acH = atomContainer.Builder.NewAtomContainer(atomContainer);
                AddExplicitHydrogens(acH);
            }
            else
            {
                acH = atomContainer;
            }
            double bond;
            polarizabilitiy += GetKJPolarizabilityFactor(acH, atom);
            for (int i = 0; i < acH.Atoms.Count; i++)
            {
                if (acH.Atoms[i] != atom)
                {
                    int atomIndex = atomContainer.Atoms.IndexOf(atom);
                    bond = distanceMatrix[atomIndex][i];
                    if (bond == 1)
                    {
                        polarizabilitiy += GetKJPolarizabilityFactor(acH, acH.Atoms[i]);
                    }
                    else
                    {
                        polarizabilitiy += (Math.Pow(0.5, bond - 1) * GetKJPolarizabilityFactor(acH, acH.Atoms[i]));
                    }//if bond==0
                }//if !=atom
            }//for
            return polarizabilitiy;
        }

        /// <summary>
        /// Calculate bond polarizability.
        /// </summary>
        /// <param name="atomContainer">AtomContainer</param>
        /// <param name="bond">Bond bond for which the polarizabilitiy should be calculated</param>
        /// <returns>polarizabilitiy</returns>
        public static double CalculateBondPolarizability(IAtomContainer atomContainer, IBond bond)
        {
            double polarizabilitiy = 0;
            var acH = atomContainer.Builder.NewAtomContainer(atomContainer);
            AddExplicitHydrogens(acH);
            if (bond.Atoms.Count == 2)
            {
                polarizabilitiy += GetKJPolarizabilityFactor(acH, bond.Begin);
                polarizabilitiy += GetKJPolarizabilityFactor(acH, bond.End);
            }
            return (polarizabilitiy / 2);
        }

        /// <summary>
        /// Method which assigns the polarizabilitiyFactors.
        /// </summary>
        /// <param name="atomContainer">AtomContainer</param>
        /// <param name="atom">Atom</param>
        /// <returns>double polarizabilitiyFactor</returns>
        private static double GetKJPolarizabilityFactor(IAtomContainer atomContainer, IAtom atom)
        {
            double polarizabilitiyFactor = 0;
            switch (atom.AtomicNumber)
            {
                case AtomicNumbers.H:
                    polarizabilitiyFactor = 0.387;
                    break;
                case AtomicNumbers.C:
                    if (atom.IsAromatic)
                    {
                        polarizabilitiyFactor = 1.230;
                    }
                    else if (atomContainer.GetMaximumBondOrder(atom) == BondOrder.Single)
                    {
                        polarizabilitiyFactor = 1.064;/* 1.064 */
                    }
                    else if (atomContainer.GetMaximumBondOrder(atom) == BondOrder.Double)
                    {
                        if (GetNumberOfHydrogen(atomContainer, atom) == 0)
                        {
                            polarizabilitiyFactor = 1.382;
                        }
                        else
                        {
                            polarizabilitiyFactor = 1.37;
                        }
                    }
                    else if (atomContainer.GetMaximumBondOrder(atom) == BondOrder.Triple
                          || atomContainer.GetMaximumBondOrder(atom) == BondOrder.Quadruple)
                    {
                        polarizabilitiyFactor = 1.279;
                    }
                    break;
                case AtomicNumbers.N:
                    if (atom.Charge != null && atom.Charge < 0)
                    {
                        polarizabilitiyFactor = 1.090;
                    }
                    else if (atomContainer.GetMaximumBondOrder(atom) == BondOrder.Single)
                    {
                        polarizabilitiyFactor = 1.094;
                    }
                    else if (atomContainer.GetMaximumBondOrder(atom) == BondOrder.Double)
                    {
                        polarizabilitiyFactor = 1.030;
                    }
                    else
                    {
                        polarizabilitiyFactor = 0.852;
                    }
                    break;
                case AtomicNumbers.O:
                    if (atom.Charge != null && atom.Charge == -1)
                    {
                        polarizabilitiyFactor = 1.791;
                    }
                    else if (atom.Charge != null && atom.Charge == 1)
                    {
                        polarizabilitiyFactor = 0.422;
                    }
                    else if (atomContainer.GetMaximumBondOrder(atom) == BondOrder.Single)
                    {
                        polarizabilitiyFactor = 0.664;
                    }
                    else if (atomContainer.GetMaximumBondOrder(atom) == BondOrder.Double)
                    {
                        polarizabilitiyFactor = 0.460;
                    }
                    break;
                case AtomicNumbers.P:
                    if (atomContainer.GetConnectedBonds(atom).Count() == 4
                     && atomContainer.GetMaximumBondOrder(atom) == BondOrder.Double)
                    {
                        polarizabilitiyFactor = 0;
                    }
                    break;
                case AtomicNumbers.S:
                    if (atom.IsAromatic)
                    {
                        polarizabilitiyFactor = 3.38;
                    }
                    else if (atomContainer.GetMaximumBondOrder(atom) == BondOrder.Single)
                    {
                        polarizabilitiyFactor = 3.20;/* 3.19 */
                    }
                    else if (atomContainer.GetMaximumBondOrder(atom) == BondOrder.Double)
                    {
                        if (GetNumberOfHydrogen(atomContainer, atom) == 0)
                        {
                            polarizabilitiyFactor = 3.51;
                        }
                        else
                        {
                            polarizabilitiyFactor = 3.50;
                        }
                    }
                    else
                    {
                        polarizabilitiyFactor = 3.42;
                    }
                    break;
                case AtomicNumbers.F:
                    polarizabilitiyFactor = 0.296;
                    break;
                case AtomicNumbers.Cl:
                    polarizabilitiyFactor = 2.343;
                    break;
                case AtomicNumbers.Br:
                    polarizabilitiyFactor = 3.5;
                    break;
                case AtomicNumbers.I:
                    polarizabilitiyFactor = 5.79;
                    break;
            }
            return polarizabilitiyFactor;
        }

        /// <summary>
        /// Gets the numberOfHydrogen attribute of the Polarizability object.
        /// </summary>
        /// <param name="atomContainer">Description of the Parameter</param>
        /// <param name="atom">Description of the Parameter</param>
        /// <returns>The numberOfHydrogen value</returns>
        private static int GetNumberOfHydrogen(IAtomContainer atomContainer, IAtom atom)
        {
            var bonds = atomContainer.GetConnectedBonds(atom);
            IAtom connectedAtom;
            int hCounter = 0;
            foreach (var bond in bonds)
            {
                connectedAtom = bond.GetOther(atom);
                if (connectedAtom.AtomicNumber == AtomicNumbers.H)
                {
                    hCounter += 1;
                }
            }
            return hCounter;
        }
    }
}
