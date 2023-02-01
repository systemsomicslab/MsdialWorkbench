/*  
 * Copyright (C) 2012  Klas Jönsson <klas.joensson@gmail.com>
 *               2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Tools.Manipulator;
using System;
using System.Diagnostics;

namespace NCDK.Tools
{
    /// <summary>
    /// This class tries to figure out the bond order of the bonds that has the flag
    /// <see cref="IBond.IsSingleOrDouble"/> raised (i.e. set to <see langword="true"/>).
    /// <para>
    /// The code is written with the assumption that the properties of the atoms in
    /// the molecule has configured with the help of <see cref="AtomContainerManipulator"/>.
    /// This class uses the <see cref="SaturationChecker"/> internally.</para>
    /// <para>
    /// If it can't find a solution where all atoms in the molecule are saturated,
    /// it gives a "best guess", i.e. the solution with most saturated atoms. If not
    /// all atoms are saturated then it will be noticed as a warning in the log.</para>
    /// </summary>
    // @author Klas Jönsson
    // @author Egon Willighagen
    // @cdk.created 2012-04-13
    // @cdk.keyword bond order
    // @cdk.module  valencycheck
    public class AtomTypeAwareSaturationChecker : IValencyChecker, IDeduceBondOrderTool
    {
        private static readonly ISaturationChecker staturationChecker = CDK.SaturationChecker;

        private BondOrder oldBondOrder;
        private int startBond;

        /// <summary>
        /// Constructs an <see cref="AtomTypeAwareSaturationChecker"/> checker.
        /// </summary>
        public AtomTypeAwareSaturationChecker()
        {
        }

        /// <summary>
        /// This method decides the bond order on bonds that has the
        /// <see cref="IBond.IsSingleOrDouble"/> raised.
        /// </summary>
        /// <param name="atomContainer">The molecule to investigate</param>
        /// <param name="atomsSaturated">Set to true if you want to make sure that all atoms are saturated.</param>
        public void DecideBondOrder(IAtomContainer atomContainer, bool atomsSaturated)
        {
            if (atomContainer.Bonds.Count == 0)
                // In this case the atom only has implicit bonds, and then it wan't be aromatic
                return;
            startBond = 0;
            int saturnatedAtoms = 0;
            int[] bestGuess = { startBond, saturnatedAtoms };
            if (atomsSaturated)
            {
                do
                {
                    if (startBond == atomContainer.Bonds.Count)
                    {
                        if (bestGuess[1] == 0)
                            throw new CDKException("Can't find any solution");
                        else
                        {
                            DecideBondOrder(atomContainer, bestGuess[0]);
                            double satAtoms = ((bestGuess[1] * 1.0) / atomContainer.Atoms.Count) * 10000;
                            satAtoms = Math.Round(satAtoms) / 100;
                            Trace.TraceWarning($"Can't find any solution where all atoms are saturated. A best guess gives {satAtoms}% Saturated atoms.");
                            return;
                        }
                    }

                    DecideBondOrder(atomContainer, startBond);

                    saturnatedAtoms = 0;
                    foreach (var atom in atomContainer.Atoms)
                    {
                        if (IsSaturated(atom, atomContainer)) saturnatedAtoms++;
                    }

                    if (bestGuess[1] < saturnatedAtoms)
                    {
                        bestGuess[0] = startBond;
                        bestGuess[1] = saturnatedAtoms;
                    }

                    startBond++;
                } while (!IsSaturated(atomContainer));
            }
            else
                DecideBondOrder(atomContainer, startBond);
        }

        /// <summary>
        /// This method decides the bond order on bonds that has the
        /// <see cref="IBond.IsSingleOrDouble"/> raised.
        /// </summary>
        /// <param name="atomContainer">The molecule to investigate.</param>
        public void DecideBondOrder(IAtomContainer atomContainer)
        {
            this.DecideBondOrder(atomContainer, true);
        }

        /// <summary>
        /// This method decides the bond order on bonds that has the 
        /// <see cref="IBond.IsSingleOrDouble"/> raised.
        /// </summary>
        /// <param name="atomContainer">The molecule to investigate</param>
        /// <param name="start">The bond to start with</param>
        private void DecideBondOrder(IAtomContainer atomContainer, int start)
        {
            for (int i = 0; i < atomContainer.Bonds.Count; i++)
                if (atomContainer.Bonds[i].IsSingleOrDouble)
                    atomContainer.Bonds[i].Order = BondOrder.Single;

            for (int i = start; i < atomContainer.Bonds.Count; i++)
            {
                CheckBond(atomContainer, i);
            }

            // If we don't start with first bond, then we have to check the bonds
            // before the bond we started with.
            if (start > 0)
            {
                for (int i = start - 1; i >= 0; i--)
                {
                    CheckBond(atomContainer, i);
                }
            }
        }

        /// <summary>
        /// This method tries to set the bond order on the current bond.
        /// </summary>
        /// <param name="atomContainer">The molecule</param>
        /// <param name="index">The index of the current bond</param>
        /// <exception cref="CDKException">when no suitable solution can be found</exception>
        private void CheckBond(IAtomContainer atomContainer, int index)
        {
            var bond = atomContainer.Bonds[index];

            if (bond != null && bond.IsSingleOrDouble)
            {
                try
                {
                    oldBondOrder = bond.Order;
                    bond.Order = BondOrder.Single;
                    SetMaxBondOrder(bond, atomContainer);
                }
                catch (CDKException e)
                {
                    bond.Order = oldBondOrder;
                    Debug.WriteLine(e);
                }
            }
        }

        /// <summary>
        /// This method decides the highest bond order that the bond can have and set
        /// it to that.
        /// </summary>
        /// <param name="bond">The bond to be investigated</param>
        /// <param name="atomContainer">The <see cref="IAtomContainer"/> that contains the bond</param>
        /// <exception cref="CDKException">when the bond cannot be further increased</exception>
        private void SetMaxBondOrder(IBond bond, IAtomContainer atomContainer)
        {
            if (BondOrderCanBeIncreased(bond, atomContainer))
            {
                if (bond.Order != BondOrder.Quadruple)
                    bond.Order = BondManipulator.IncreaseBondOrder(bond.Order);
                else
                    throw new CDKException("Can't increase a quadruple bond!");
            }
        }

        /// <summary>
        /// Check if the bond order can be increased. This method assumes that the
        /// bond is between only two atoms.
        /// </summary>
        /// <param name="bond">The bond to check</param>
        /// <param name="atomContainer">The <see cref="IAtomContainer"/> that the bond belongs to</param>
        /// <returns>True if it is possibly to increase the bond order</returns>
        public virtual bool BondOrderCanBeIncreased(IBond bond, IAtomContainer atomContainer)
        {
            bool atom0isUnsaturated = false;
            bool atom1isUnsaturated = false;
            double sum;
            if (bond.Begin.BondOrderSum == null)
            {
                sum = GetAtomBondOrderSum(bond.End, atomContainer);
            }
            else
                sum = bond.Begin.BondOrderSum.Value;
            if (BondsUsed(bond.Begin, atomContainer) < sum)
                atom0isUnsaturated = true;

            if (bond.End.BondOrderSum == null)
            {
                sum = GetAtomBondOrderSum(bond.End, atomContainer);
            }
            else
                sum = bond.End.BondOrderSum.Value;
            if (BondsUsed(bond.End, atomContainer) < sum)
                atom1isUnsaturated = true;

            if (atom0isUnsaturated == atom1isUnsaturated)
                return atom0isUnsaturated;
            else
            {
                // If one of the atoms is saturated and the other isn't, what do we
                // do then? Look at the bonds on each side and decide from that...
                var myIndex = atomContainer.Bonds.IndexOf(bond);
                // If it's the first bond, then just move on.
                if (myIndex == 0)
                    return false;
                // If the previous bond is the reason it's no problem, so just move on...

                // TODO instead check if the atom that are in both bonds are saturated...?
                if (atomContainer.Bonds[myIndex - 1].Order == BondOrder.Double)
                    return false;

                // The only reason for trouble should now be that the next bond make
                // one of the atoms saturated, so lets throw an exception and
                // reveres until we can place a double bond and set it as single and
                // continue
                if (IsConnected(atomContainer.Bonds[myIndex], atomContainer.Bonds[0]))
                    throw new CantDecideBondOrderException("Can't decide bond order of this bond");
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// This method is used if, by some reason, the bond order sum is not set
        /// for an atom.
        /// </summary>
        /// <param name="atom">The atom in question</param>
        /// <param name="mol">The molecule that the atom belongs to</param>
        /// <returns>The bond order sum</returns>
        private static double GetAtomBondOrderSum(IAtom atom, IAtomContainer mol)
        {
            double sum = 0;

            foreach (var bond in mol.Bonds)
                if (bond.Contains(atom))
                    sum += BondManipulator.DestroyBondOrder(bond.Order);

            return sum;
        }

        /// <summary>
        /// Look if any atoms in <paramref name="bond1"/> also are in <paramref name="bond2"/>
        /// and if so it conceder the bonds connected.
        /// </summary>
        /// <param name="bond1">The first bond</param>
        /// <param name="bond2">The other bond</param>
        /// <returns>True if any of the atoms in <paramref name="bond1"/> also are in <paramref name="bond2"/></returns>
        private static bool IsConnected(IBond bond1, IBond bond2)
        {
            foreach (var atom in bond1.Atoms)
                if (bond2.Contains(atom))
                    return true;
            return false;
        }

        /// <summary>
        /// This method calculates the number of bonds that an <see cref="IAtom"/>
        /// can have.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> to be investigated</param>
        /// <returns>The max number of bonds the <see cref="IAtom"/> can have</returns>
        /// <exception cref="CDKException">when the atom's valency is not set</exception>
        public virtual double GetMaxNoOfBonds(IAtom atom)
        {
            double noValenceElectrons = atom.Valency ?? -1;
            if (noValenceElectrons == -1)
            {
                throw new CDKException("Atom property not set: Valency");
            }
            // This will probably only work for group 13-18, and not for helium...
            return 8 - noValenceElectrons;
        }

        /// <summary>
        /// A small help method that count how many bonds an atom has, regarding
        /// bonds due to its charge and to implicit hydrogens.
        /// </summary>
        /// <param name="atom">The atom to check</param>
        /// <param name="atomContainer">The atomContainer containing the atom</param>
        /// <returns>The number of bonds that the atom has</returns>
        private static double BondsUsed(IAtom atom, IAtomContainer atomContainer)
        {
            int bondsToAtom = 0;
            foreach (var bond in atomContainer.Bonds)
                if (bond.Contains(atom))
                    bondsToAtom += (int)BondManipulator.DestroyBondOrder(bond.Order);
            int implicitHydrogens;
            if (atom.ImplicitHydrogenCount == null || atom.ImplicitHydrogenCount == null)
            {
                // Will probably only work with group 13-18, and not for helium...
                if (atom.Valency == null || atom.Valency == null)
                    throw new CDKException($"Atom {atom.AtomTypeName} has not got the valency set.");
                if (atom.FormalNeighbourCount == null || atom.FormalNeighbourCount == null)
                    throw new CDKException($"Atom {atom.AtomTypeName} has not got the formal neighbour count set.");
                implicitHydrogens = (8 - atom.Valency.Value) - atom.FormalNeighbourCount.Value;
                Trace.TraceWarning($"Number of implicit hydrogens not set for atom {atom.AtomTypeName}. Estimated it to: {implicitHydrogens}");
            }
            else
                implicitHydrogens = atom.ImplicitHydrogenCount.Value;

            double charge;
            if (atom.Charge == null)
                if (atom.FormalCharge == null)
                {
                    charge = 0;
                    Trace.TraceWarning($"Neither charge nor formal charge is set for atom {atom.AtomTypeName}. Estimate it to: 0");
                }
                else
                    charge = atom.FormalCharge.Value;
            else
                charge = atom.Charge.Value;
            return bondsToAtom - charge + implicitHydrogens;
        }

        /// <inheritdoc/>
        public void Saturate(IAtomContainer container)
        {
            staturationChecker.Saturate(container);
        }

        /// <inheritdoc/>
        public bool IsSaturated(IAtomContainer container)
        {
            return staturationChecker.IsSaturated(container);
        }

        /// <inheritdoc/>
        public bool IsSaturated(IAtom atom, IAtomContainer container)
        {
            return staturationChecker.IsSaturated(atom, container);
        }

        /// <summary>
        /// This is a private exception thrown when it detects an error and needs to
        /// start to back-trace.
        /// </summary>
        // @author Klas Jönsson
        private class CantDecideBondOrderException : CDKException
        {
            public CantDecideBondOrderException()
            {
            }

            /// <summary>
            /// Creates a new <see cref="CantDecideBondOrderException"/> with a given message.
            /// </summary>
            /// <param name="message">Explanation about why the decision could not be made.</param>
            public CantDecideBondOrderException(string message) : base(message)
            {
            }

            public CantDecideBondOrderException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }
    }
}
