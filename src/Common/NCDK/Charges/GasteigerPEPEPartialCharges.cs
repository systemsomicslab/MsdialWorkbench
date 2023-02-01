/* Copyright (C) 2004-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
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

using NCDK.Common.Collections;
using NCDK.Aromaticities;
using NCDK.Config;
using NCDK.Reactions;
using NCDK.Reactions.Types;
using NCDK.Reactions.Types.Parameters;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Charges
{
    /// <summary>
    /// The calculation of the Gasteiger (PEPE) partial charges is based on
    /// <token>cdk-cite-Saller85</token>. This class doesn't implement the original method of the Marsili but the
    /// method based on H. Saller which is described from Petra manual version 2.6
    /// </summary>
    /// <remarks>
    /// They are calculated by generating all valence bond (resonance) structures
    /// for this system and then weighting them on the basis of pi-orbital electronegativities
    /// and formal considerations based on PEPE (Partial Equalization of pi-electronegativity).
    /// </remarks>
    /// <seealso cref="GasteigerMarsiliPartialCharges"/>
    // @author      Miguel Rojas
    // @cdk.module  charges
    // @cdk.created 2006-05-14
    // @cdk.keyword partial atomic charges
    // @cdk.keyword charge distribution
    // @cdk.keyword electronegativities, partial equalization of orbital
    // @cdk.keyword PEPE
    public class GasteigerPEPEPartialCharges : IChargeCalculator
    {
        /// <summary>The maxGasteigerIters attribute of the GasteigerPEPEPartialCharges object.</summary>
        public int MaxGasteigerIterations { get; set; } = 8;

        /// <summary>The maximum resonance structures to be searched.</summary>
        public int MaxResonanceStructures { get; set; } = 50;

        /// <summary>The StepSize attribute of the GasteigerMarsiliPartialCharges object.</summary>
        public int StepSize { get; set; } = 5;

        private readonly AtomTypeFactory factory = CDK.JmolAtomTypeFactory;

        /// <summary>Corresponds an empirical influence between the electrostatic potential and the neighbours.</summary>
        private const double fE = 1.1;
                                         
        /// <summary>Scale factor which makes same heavy for all structures</summary>
        private const double fS = 0.37;

        public GasteigerPEPEPartialCharges() { }
        
        /// <summary>
        /// Main method which assigns Gasteiger partial pi charges.
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <param name="setCharge">currently unused</param>
        /// <returns>AtomContainer with partial charges</returns>
        public IAtomContainer AssignGasteigerPiPartialCharges(IAtomContainer ac, bool setCharge)
        {
            // we save the aromaticity flags for the input molecule so that
            // we can add them back before we return
            var oldBondAromaticity = new bool[ac.Bonds.Count];
            var oldAtomAromaticity = new bool[ac.Atoms.Count];
            for (int i = 0; i < ac.Atoms.Count; i++)
                oldAtomAromaticity[i] = ac.Atoms[i].IsAromatic;
            for (int i = 0; i < ac.Bonds.Count; i++)
                oldBondAromaticity[i] = ac.Bonds[i].IsAromatic;

            /* 0: remove charge, and possible flag ac */
            for (int j = 0; j < ac.Atoms.Count; j++)
            {
                ac.Atoms[j].Charge = 0.0;
                ac.Atoms[j].IsPlaced = false;
            }
            for (int j = 0; j < ac.Bonds.Count; j++)
            {
                ac.Bonds[j].IsPlaced = false;
            }

            /* 1: detect resonance structure */
            var gR1 = new StructureResonanceGenerator(); // according G. should be integrated the breaking bonding
            var reactionList1 = gR1.Reactions.ToList();
            var paramList1 = new List<IParameterReaction>();
            var param = new SetReactionCenter { IsSetParameter = true };
            paramList1.Add(param);
            var reactionHCPB = new HeterolyticCleavagePBReaction { ParameterList = paramList1 };
            reactionList1.Add(new SharingAnionReaction());
            foreach (var reaction in reactionList1)
            {
                reaction.ParameterList = paramList1;
            }
            gR1.Reactions = reactionList1;

            // according G. should be integrated the breaking bonding
            var gR2 = new StructureResonanceGenerator { MaximalStructures = MaxResonanceStructures };
            var reactionList2 = gR2.Reactions.ToList();
            var paramList = new List<IParameterReaction>();
            var paramA = new SetReactionCenter { IsSetParameter = true };
            paramList.Add(paramA);
            reactionList2.Add(new HeterolyticCleavagePBReaction());
            reactionList2.Add(new SharingAnionReaction());
            foreach (var reaction in reactionList2)
            {
                reaction.ParameterList = paramList;
            }
            gR2.Reactions = reactionList2;

            /* find resonance containers, which eliminates the repetitions */
            StructureResonanceGenerator gRN = new StructureResonanceGenerator(); // according G. should be integrated the breaking bonding
            var acSet = gRN.GetContainers(RemovingFlagsAromaticity(ac));

            var iSet = ac.Builder.NewAtomContainerSet();
            iSet.Add(ac);

            if (acSet != null)
                foreach (var container in acSet)
                {
                    ac = SetFlags(container, ac, true);

                    // Aromatic don't break its double bond homolytically
                    if (Aromaticity.CDKLegacy.Apply(ac))
                        reactionList1.Remove(reactionHCPB);
                    else
                        reactionList1.Add(reactionHCPB);

                    var a = gR1.GetStructures(RemovingFlagsAromaticity(ac));
                    if (a.Count > 1)
                    {
                        for (int j = 1; j < a.Count; j++)
                        { // the first is already added
                            iSet.Add(a[j]);
                        }
                    }
                    ac = SetFlags(container, ac, false);

                    /* processing for bonds which are not in resonance */
                    for (int number = 0; number < ac.Bonds.Count; number++)
                    {
                        IAtomContainer aa = SetAntiFlags(container, ac, number, true);
                        if (aa != null)
                        {
                            var ab = gR2.GetStructures(aa);
                            if (ab.Count > 1)
                                for (int j = 1; j < ab.Count; j++)
                                { // the first is already added
                                    iSet.Add(ab[j]);
                                }
                            ac = SetAntiFlags(container, aa, number, false);
                        }
                    }
                }

            /* detect hyperconjugation interactions */
            var setHI = GetHyperconjugationInteractions(ac, iSet);

            if (setHI != null)
            {
                if (setHI.Count != 0) iSet.AddRange(setHI);
                Debug.WriteLine($"setHI: {iSet.Count}");
            }
            if (iSet.Count < 2)
            {
                for (int i = 0; i < ac.Atoms.Count; i++)
                    ac.Atoms[i].IsAromatic = oldAtomAromaticity[i];
                for (int i = 0; i < ac.Bonds.Count; i++)
                    ac.Bonds[i].IsAromatic = oldBondAromaticity[i];
                return ac;
            }

            /* 2: search whose atoms which don't keep their formal charge and set flags */
            var sumCharges = Arrays.CreateJagged<double>(iSet.Count, ac.Atoms.Count);
            for (int i = 1; i < iSet.Count; i++)
            {
                var iac = iSet[i];
                for (int j = 0; j < iac.Atoms.Count; j++)
                    sumCharges[i][j] = iac.Atoms[j].FormalCharge.Value;
            }

            for (int i = 1; i < iSet.Count; i++)
            {
                var iac = iSet[i];
                int count = 0;
                for (int j = 0; j < ac.Atoms.Count; j++)
                    if (count < 2)
                        if (sumCharges[i][j] != ac.Atoms[j].FormalCharge)
                        {
                            ac.Atoms[j].IsPlaced = true;
                            iac.Atoms[j].IsPlaced = true;
                            count++; /* TODO- error */
                        }
            }

            /* 3: set sigma charge (PEOE). Initial start point */
            var peoe = new GasteigerMarsiliPartialCharges(); ;
            peoe.MaxGasteigerIterations = 6;
            IAtomContainer acCloned;

            var gasteigerFactors = AssignPiFactors(iSet);//a,b,c,deoc,chi,q

            /* 4: calculate topological weight factors Wt=fQ*fB*fA */
            var Wt = new double[iSet.Count - 1];
            for (int i = 1; i < iSet.Count; i++)
            {
                Wt[i - 1] = GetTopologicalFactors(iSet[i], ac);
                Debug.WriteLine($", W:{Wt[i - 1]}");
                acCloned = (IAtomContainer)iSet[i].Clone();

                acCloned = peoe.AssignGasteigerMarsiliSigmaPartialCharges(acCloned, true);
                for (int j = 0; j < acCloned.Atoms.Count; j++)
                    if (iSet[i].Atoms[j].IsPlaced)
                    {
                        gasteigerFactors[i][StepSize * j + j + 5] = acCloned.Atoms[j].Charge.Value;
                    }
            }

            // calculate electronegativity for changed atoms and make the difference
            // between whose atoms which change their formal charge
            for (int iter = 0; iter < MaxGasteigerIterations; iter++)
            {
                for (int k = 1; k < iSet.Count; k++)
                {
                    IAtomContainer iac = iSet[k];
                    double[] electronegativity = new double[2];
                    int count = 0;
                    int atom1 = 0;
                    int atom2 = 0;
                    for (int j = 0; j < iac.Atoms.Count; j++)
                    {
                        if (count == 2) // The change of sign is product of only two atoms, is not true 
                            break;
                        if (iac.Atoms[j].IsPlaced)
                        {
                            Debug.WriteLine("Atom: " + j + ", S:" + iac.Atoms[j].Symbol + ", C:"
                                    + iac.Atoms[j].FormalCharge);
                            if (count == 0)
                                atom1 = j;
                            else
                                atom2 = j;

                            double q1 = gasteigerFactors[k][StepSize * j + j + 5];
                            electronegativity[count] = 
                                      gasteigerFactors[k][StepSize * j + j + 2] * q1 * q1
                                    + gasteigerFactors[k][StepSize * j + j + 1] * q1
                                    + gasteigerFactors[k][StepSize * j + j];
                            Debug.WriteLine("e:" + electronegativity[count] + ",q1: " + q1 + ", c:"
                                    + gasteigerFactors[k][StepSize * j + j + 2] + ", b:"
                                    + gasteigerFactors[k][StepSize * j + j + 1] + ", a:"
                                    + gasteigerFactors[k][StepSize * j + j]);
                            count++;
                        }
                    }
                    Debug.WriteLine("Atom1:" + atom1 + ",Atom2:" + atom2);
                    /* difference of electronegativity 1 lower */
                    var max1 = Math.Max(electronegativity[0], electronegativity[1]);
                    var min1 = Math.Min(electronegativity[0], electronegativity[1]);
                    double DX = 1.0;
                    if (electronegativity[0] < electronegativity[1])
                        DX = gasteigerFactors[k][StepSize * atom1 + atom1 + 3];
                    else
                        DX = gasteigerFactors[k][StepSize * atom2 + atom2 + 3];

                    var Dq = (max1 - min1) / DX;
                    Debug.WriteLine("Dq : " + Dq + " = (" + max1 + "-" + min1 + ")/" + DX);
                    var epN1 = GetElectrostaticPotentialN(iac, atom1, gasteigerFactors[k]);
                    var epN2 = GetElectrostaticPotentialN(iac, atom2, gasteigerFactors[k]);
                    var SumQN = Math.Abs(epN1 - epN2);
                    Debug.WriteLine("sum(" + SumQN + ") = (" + epN1 + ") - (" + epN2 + ")");

                    /* electronic weight */
                    var WE = Dq + fE * SumQN;
                    Debug.WriteLine("WE : " + WE + " = Dq(" + Dq + ")+FE(" + fE + ")*SumQN(" + SumQN);
                    var iTE = iter + 1;

                    /* total topological */
                    var W = WE * Wt[k - 1] * fS / (iTE);
                    Debug.WriteLine("W : " + W + " = WE(" + WE + ")*Wt(" + Wt[k - 1] + ")*FS(" + fS + ")/iter(" + iTE
                            + "), atoms: " + atom1 + ", " + atom2);

                    /* iac == new structure, ac == old structure */
                    /* atom1 */
                    if (iac.Atoms[atom1].FormalCharge == 0)
                    {
                        if (ac.Atoms[atom1].FormalCharge < 0)
                        {
                            gasteigerFactors[k][StepSize * atom1 + atom1 + 5] = -1 * W;
                        }
                        else
                        {
                            gasteigerFactors[k][StepSize * atom1 + atom1 + 5] = W;
                        }
                    }
                    else if (iac.Atoms[atom1].FormalCharge > 0)
                    {
                        gasteigerFactors[k][StepSize * atom1 + atom1 + 5] = W;
                    }
                    else
                    {
                        gasteigerFactors[k][StepSize * atom1 + atom1 + 5] = -1 * W;
                    }
                    /* atom2 */
                    if (iac.Atoms[atom2].FormalCharge == 0)
                    {
                        if (ac.Atoms[atom2].FormalCharge < 0)
                        {
                            gasteigerFactors[k][StepSize * atom2 + atom2 + 5] = -1 * W;
                        }
                        else
                        {
                            gasteigerFactors[k][StepSize * atom2 + atom2 + 5] = W;
                        }
                    }
                    else if (iac.Atoms[atom2].FormalCharge > 0)
                    {
                        gasteigerFactors[k][StepSize * atom2 + atom2 + 5] = W;
                    }
                    else
                    {
                        gasteigerFactors[k][StepSize * atom2 + atom2 + 5] = -1 * W;
                    }

                }
                for (int k = 1; k < iSet.Count; k++)
                {

                    for (int i = 0; i < ac.Atoms.Count; i++)
                        if (iSet[k].Atoms[i].IsPlaced)
                        {
                            var charge = ac.Atoms[i].Charge.Value;
                            double chargeT = 0.0;
                            chargeT = charge + gasteigerFactors[k][StepSize * i + i + 5];
                            Debug.WriteLine("i<|" + ac.Atoms[i].Symbol + ", " + chargeT + "=c:" + charge + "+g: "
                                    + gasteigerFactors[k][StepSize * i + i + 5]);
                            ac.Atoms[i].Charge = chargeT;
                        }
                }

            }// iterations
            Debug.WriteLine("final");

            // before getting back we should set back the aromatic flags
            for (int i = 0; i < ac.Atoms.Count; i++)
                ac.Atoms[i].IsAromatic = oldAtomAromaticity[i];
            for (int i = 0; i < ac.Bonds.Count; i++)
                ac.Bonds[i].IsAromatic = oldBondAromaticity[i];

            return ac;
        }

        public void CalculateCharges(IAtomContainer container)
        {
            try
            {
                this.AssignGasteigerPiPartialCharges(container, true);
            }
            catch (Exception exception)
            {
                throw new CDKException($"Could not calculate Gasteiger-Marsili PEPE charges: {exception.Message}", exception);
            }
        }

        /// <summary>
        /// remove the aromaticity flags.
        /// </summary>
        /// <param name="ac">The IAtomContainer to remove flags</param>
        /// <returns>The IAtomContainer with the flags removed</returns>
        private static IAtomContainer RemovingFlagsAromaticity(IAtomContainer ac)
        {
            foreach (var atom in ac.Atoms)
                atom.IsAromatic = false;
            foreach (var bond in ac.Bonds)
                bond.IsAromatic = false;
            return ac;
        }

        /// <summary>
        /// Set the Flags to atoms and bonds from an atomContainer.
        /// </summary>
        /// <param name="container">Container with the flags</param>
        /// <param name="ac">Container to put the flags</param>
        /// <param name="b"><see langword="true"/>, if the the flag is true</param>
        /// <returns>Container with added flags</returns>
        private static IAtomContainer SetFlags(IAtomContainer container, IAtomContainer ac, bool b)
        {
            foreach (var atom in container.Atoms)
            {
                int positionA = ac.Atoms.IndexOf(atom);
                ac.Atoms[positionA].IsReactiveCenter = b;
            }
            foreach (var bond in container.Bonds)
            {
                int positionB = ac.Bonds.IndexOf(bond);
                ac.Bonds[positionB].IsReactiveCenter = b;
            }
            return ac;
        }

        /// <summary>
        /// Set the Flags to atoms and bonds which are not contained in an atomContainer.
        /// </summary>
        /// <param name="container">Container with the flags</param>
        /// <param name="ac">Container to put the flags</param>
        /// <param name="number"><see langword="true"/>, if the the flag is true</param>
        /// <param name="b">Container with added flags</param>
        /// <returns></returns>
        private static IAtomContainer SetAntiFlags(IAtomContainer container, IAtomContainer ac, int number, bool b)
        {
            var bond = ac.Bonds[number];
            if (!container.Contains(bond))
            {
                bond.IsReactiveCenter = b;
                bond.Atoms[0].IsReactiveCenter = b;
                bond.Atoms[1].IsReactiveCenter = b;
            }
            else
                return null;
            return ac;
        }

        /// <summary>
        /// get the possible structures after hyperconjugation interactions for bonds which do not belong to any resonance structure.
        /// </summary>
        /// <param name="ac"></param>
        /// <param name="iSet"></param>
        /// <returns></returns>
        private IChemObjectSet<IAtomContainer> GetHyperconjugationInteractions(IAtomContainer ac, IChemObjectSet<IAtomContainer> iSet)
        {
            var set = ac.Builder.NewAtomContainerSet();
            IReactionProcess type = new HeterolyticCleavageSBReaction();
            CleanFlagReactiveCenter(ac);
            bool found = false; /* control obtained containers */
            var setOfReactants = ac.Builder.NewAtomContainerSet();
            /* search of reactive center. */
            for (int i = 0; i < ac.Bonds.Count; i++)
            {
                if (ac.Bonds[i].Order != BondOrder.Single)
                {
                    foreach (var ati in iSet)
                    {
                        if (!ati.Equals(ac))
                            for (int k = 0; k < ati.Bonds.Count; k++)
                            {
                                var a0 = ati.Bonds[k].Atoms[0];
                                var a1 = ati.Bonds[k].Atoms[1];
                                if (!a0.AtomicNumber.Equals(AtomicNumbers.H) || !a1.AtomicNumber.Equals(AtomicNumbers.H))
                                    if ((a0.Id.Equals(ac.Bonds[i].Atoms[0].Id, StringComparison.Ordinal) && a1.Id.Equals(ac.Bonds[i].Atoms[1].Id, StringComparison.Ordinal))
                                     || (a1.Id.Equals(ac.Bonds[i].Atoms[0].Id, StringComparison.Ordinal) && a0.Id.Equals(ac.Bonds[i].Atoms[1].Id, StringComparison.Ordinal)))
                                    {
                                        if (a0.FormalCharge != 0 || a1.FormalCharge != 0)
                                            goto continue_out;
                                    }
                            }
                    }
                    ac.Bonds[i].Atoms[0].IsReactiveCenter = true;
                    ac.Bonds[i].Atoms[1].IsReactiveCenter = true;
                    ac.Bonds[i].IsReactiveCenter = true;
                    found = true;
                }
            continue_out:
                ;
            }
            if (!found)
                return null;

            setOfReactants.Add(ac);

            var paramList = new List<IParameterReaction>();
            var param = new SetReactionCenter
            {
                IsSetParameter = true
            };
            paramList.Add(param);
            type.ParameterList = paramList;
            var setOfReactions = type.Initiate(setOfReactants, null);
            for (int i = 0; i < setOfReactions.Count; i++)
            {
                type = new HyperconjugationReaction();
                var setOfM2 = ac.Builder.NewAtomContainerSet();
                IAtomContainer mol = setOfReactions[i].Products[0];
                for (int k = 0; k < mol.Bonds.Count; k++)
                {
                    mol.Bonds[k].IsReactiveCenter = false;
                    mol.Bonds[k].Atoms[0].IsReactiveCenter = false;
                    mol.Bonds[k].Atoms[1].IsReactiveCenter = false;
                }
                setOfM2.Add(mol);
                List<IParameterReaction> paramList2 = new List<IParameterReaction>();
                IParameterReaction param2 = new SetReactionCenter
                {
                    IsSetParameter = false
                };
                paramList2.Add(param);
                type.ParameterList = paramList2;
                IReactionSet setOfReactions2 = type.Initiate(setOfM2, null);
                if (setOfReactions2.Count > 0)
                {

                    IAtomContainer react = setOfReactions2[0].Reactants[0];

                    set.Add(react);
                }
            }

            return set;
        }

        /// <summary>
        /// get the electrostatic potential of the neighbours of a atom.
        /// </summary>
        /// <param name="ac">The IAtomContainer to study</param>
        /// <param name="atom1">The position of the IAtom to study</param>
        /// <param name="ds"></param>
        /// <returns>The sum of electrostatic potential of the neighbours</returns>
        private double GetElectrostaticPotentialN(IAtomContainer ac, int atom1, double[] ds)
        {

            //        double CoulombForceConstant = 1/(4*Math.PI*8.81/*Math.Pow(10, -12)*/);
            double CoulombForceConstant = 0.048;
            double sum = 0.0;
            try
            {
                var atoms = ac.GetConnectedAtoms(ac.Atoms[atom1]);
                foreach (var atom in atoms)
                {
                    double covalentradius = 0;
                    string symbol = atom.Symbol;
                    IAtomType type = factory.GetAtomType(symbol);
                    covalentradius = type.CovalentRadius.Value;

                    double charge = ds[StepSize * atom1 + atom1 + 5];
                    Debug.WriteLine($"sum_({sum}) = CFC({CoulombForceConstant})*Charge({charge}/ret({covalentradius}");
                    sum += CoulombForceConstant * charge / (covalentradius * covalentradius);
                }
            }
            catch (CDKException e)
            {
                Debug.WriteLine(e);
            }

            return sum;
        }

        /// <summary>
        /// Get the topological weight factor for each atomContainer.
        /// </summary>
        /// <param name="atomContainer">The IAtomContainer to study.</param>
        /// <param name="ac">The IAtomContainer to study.</param>
        /// <returns>The value</returns>
        private static double GetTopologicalFactors(IAtomContainer atomContainer, IAtomContainer ac)
        {
            /* factor for separation of charge */
            int totalNCharge1 = AtomContainerManipulator.GetTotalNegativeFormalCharge(atomContainer);
            int totalPCharge1 = AtomContainerManipulator.GetTotalPositiveFormalCharge(atomContainer);

            double fQ = 1.0;
            if (totalNCharge1 != 0.0)
            {
                fQ = 0.5;
                for (int i = 0; i < atomContainer.Bonds.Count; i++)
                {
                    IBond bond = atomContainer.Bonds[i];
                    if (bond.Atoms[0].FormalCharge != 0.0 && bond.Atoms[1].FormalCharge != 0.0)
                    {
                        fQ = 0.25;
                        break;
                    }
                }
            }
            /* factor, if the number of covalents bonds is decreased */
            double fB = 1.0;

            int numBond1 = 0;
            int numBond2 = 0;
            for (int i = 0; i < atomContainer.Bonds.Count; i++)
            {
                if (atomContainer.Bonds[i].Order == BondOrder.Double) numBond1 += 1;
                if (ac.Bonds[i].Order == BondOrder.Double) numBond2 += 1;
            }

            if (numBond1 < /* > */numBond2) fB = 0.8;

            double fPlus = 1.0;
            if (totalNCharge1 == 0.0 && totalPCharge1 == 0.0) fPlus = 0.1;

            /* aromatic */
            double fA = 1.0;
            try
            {
                if (Aromaticity.CDKLegacy.Apply(ac)) if (!Aromaticity.CDKLegacy.Apply(atomContainer)) fA = 0.3;
            }
            catch (CDKException e)
            {
                Console.Out.WriteLine(e.StackTrace);
            }
            Debug.WriteLine("return " + fQ * fB * fPlus * fA + "= sp:" + fQ + ", dc:" + fB + ", fPlus:" + fPlus + ", fA:" + fA);

            return fQ * fB * fPlus * fA;
        }

        /// <summary>
        /// Method which stores and assigns the factors a,b,c and CHI+.
        /// </summary>
        /// <param name="setAc"></param>
        /// <returns>Array of doubles [a1,b1,c1,denom1,chi1,q1...an,bn,cn...] 1:Atom 1-n in AtomContainer</returns>
        private double[][] AssignPiFactors(IChemObjectSet<IAtomContainer> setAc)
        {
            //a,b,c,denom,chi,q
            double[][] gasteigerFactors = Arrays.CreateJagged<double>(setAc.Count, (setAc[0].Atoms.Count * (StepSize + 1)));
            double[] factors = new double[] { 0.0, 0.0, 0.0 };
            for (int k = 1; k < setAc.Count; k++)
            {
                var ac = setAc[k];
                for (int i = 0; i < ac.Atoms.Count; i++)
                {
                    factors[0] = 0.0;
                    factors[1] = 0.0;
                    factors[2] = 0.0;
                    switch (ac.Atoms[i].AtomicNumber)
                    {
                        case AtomicNumbers.H:
                            factors[0] = 0.0;
                            factors[1] = 0.0;
                            factors[2] = 0.0;
                            break;
                        case AtomicNumbers.C:
                            factors[0] = 5.60;
                            factors[1] = 8.93;
                            factors[2] = 2.94;
                            break;
                        case AtomicNumbers.O:
                            if (ac.GetMaximumBondOrder(ac.Atoms[i]) == BondOrder.Single)
                            {
                                factors[0] = 10.0;
                                factors[1] = 13.86;
                                factors[2] = 9.68;
                            }
                            else
                            {
                                factors[0] = 7.91;
                                factors[1] = 14.76;
                                factors[2] = 6.85;
                            }
                            break;
                        case AtomicNumbers.N:
                            if (ac.GetMaximumBondOrder(ac.Atoms[i]) != BondOrder.Single)
                            {
                                factors[0] = 7.95;/* 7.95 */
                                factors[1] = 9.73;/* 9.73 */
                                factors[2] = 2.67;/* 2.67 */
                            }
                            else
                            {
                                factors[0] = 4.54;/* 4.54 *//* 5.5 */
                                factors[1] = 11.86;/* 11.86 *//* 10.86 */
                                factors[2] = 7.32;/* 7.32 *//* 7.99 */
                            }
                            break;
                        case AtomicNumbers.S:
                            if (ac.GetMaximumBondOrder(ac.Atoms[i]) == BondOrder.Single)
                            {
                                factors[0] = 7.73;
                                factors[1] = 8.16;
                                factors[2] = 1.81;
                            }
                            else
                            {
                                factors[0] = 6.60;
                                factors[1] = 10.32;
                                factors[2] = 3.72;
                            }
                            break;
                        case AtomicNumbers.F:
                            factors[0] = 7.34;
                            factors[1] = 13.86;
                            factors[2] = 9.68;
                            break;
                        case AtomicNumbers.Cl:
                            factors[0] = 6.50;
                            factors[1] = 11.02;
                            factors[2] = 4.52;
                            break;
                        case AtomicNumbers.Br:
                            factors[0] = 5.20;
                            factors[1] = 9.68;
                            factors[2] = 4.48;
                            break;
                        case AtomicNumbers.I:
                            factors[0] = 4.95;
                            factors[1] = 8.81;
                            factors[2] = 3.86;
                            break;
                    }

                    gasteigerFactors[k][StepSize * i + i] = factors[0];
                    gasteigerFactors[k][StepSize * i + i + 1] = factors[1];
                    gasteigerFactors[k][StepSize * i + i + 2] = factors[2];
                    gasteigerFactors[k][StepSize * i + i + 5] = ac.Atoms[i].Charge.Value;

                    if (factors[0] == 0 && factors[1] == 0 && factors[2] == 0)
                    {
                        gasteigerFactors[k][StepSize * i + i + 3] = 1;
                    }
                    else
                    {
                        gasteigerFactors[k][StepSize * i + i + 3] = factors[0] + factors[1] + factors[2];
                    }
                }
            }

            return gasteigerFactors;
        }

        /// <summary>
        /// Method which stores and assigns the factors a,b,c and CHI+.
        /// </summary>
        /// <param name="setAc"></param>
        /// <returns>Array of doubles [a1,b1,c1,denom1,chi1,q1...an,bn,cn...] 1:Atom 1-n in AtomContainer</returns>
        public double[][] AssignrPiMarsilliFactors(IChemObjectSet<IAtomContainer> setAc)
        {
            //a,b,c,denom,chi,q
            double[][] gasteigerFactors = Arrays.CreateJagged<double>(setAc.Count, (setAc[0].Atoms.Count * (StepSize + 1)));
            double[] factors = new double[] { 0.0, 0.0, 0.0 };
            for (int k = 1; k < setAc.Count; k++)
            {
                IAtomContainer ac = setAc[k];

                for (int i = 0; i < ac.Atoms.Count; i++)
                {
                    factors[0] = 0.0;
                    factors[1] = 0.0;
                    factors[2] = 0.0;
                    switch (ac.Atoms[i].AtomicNumber)
                    {
                        case AtomicNumbers.H:
                            factors[0] = 0.0;
                            factors[1] = 0.0;
                            factors[2] = 0.0;
                            break;
                        case AtomicNumbers.C:
                            factors[0] = 5.98;/* 5.98-5.60 */
                            factors[1] = 7.93;/* 7.93-8.93 */
                            factors[2] = 1.94;
                            break;
                        case AtomicNumbers.O:
                            if (ac.GetMaximumBondOrder(ac.Atoms[i]) != BondOrder.Single)
                            {
                                factors[0] = 11.2;/* 11.2-10.0 */
                                factors[1] = 13.24;/* 13.24-13.86 */
                                factors[2] = 9.68;
                            }
                            else
                            {
                                factors[0] = 7.91;
                                factors[1] = 14.76;
                                factors[2] = 6.85;
                            }
                            break;
                        case AtomicNumbers.N:
                            if (ac.GetMaximumBondOrder(ac.Atoms[i]) != BondOrder.Single)
                            {

                                factors[0] = 8.95;/* 7.95 */
                                factors[1] = 9.73;/* 9.73 */
                                factors[2] = 2.67;/* 2.67 */
                            }
                            else
                            {
                                factors[0] = 4.54;
                                factors[1] = 11.86;
                                factors[2] = 7.32;
                            }
                            break;
                        case AtomicNumbers.P:
                            {// <--No correct
                                if (ac.GetMaximumBondOrder(ac.Atoms[i]) != BondOrder.Single)
                                {
                                    factors[0] = 10.73;// <--No correct
                                    factors[1] = 11.16;// <--No correct
                                    factors[2] = 6.81;// <--No correct
                                }
                                else
                                {
                                    factors[0] = 9.60;// <--No correct
                                    factors[1] = 13.32;// <--No correct
                                    factors[2] = 2.72;// <--No correct
                                }
                            }
                            break;
                        case AtomicNumbers.S:
                            if (ac.GetMaximumBondOrder(ac.Atoms[i]) != BondOrder.Single)
                            {

                                factors[0] = 7.73;
                                factors[1] = 8.16;
                                factors[2] = 1.81;
                            }
                            else
                            {
                                factors[0] = 6.60;
                                factors[1] = 10.32;
                                factors[2] = 3.72;
                            }
                            break;
                        case AtomicNumbers.F:
                            factors[0] = 7.14/* 7.34 */;
                            factors[1] = 13.86;
                            factors[2] = 5.68;
                            break;
                        case AtomicNumbers.Cl:
                            factors[0] = 6.51;/* 6.50 */
                            factors[1] = 11.02;
                            factors[2] = 4.52;
                            break;
                        case AtomicNumbers.Br:
                            factors[0] = 5.20;
                            factors[1] = 9.68;
                            factors[2] = 4.48;
                            break;
                        case AtomicNumbers.I:
                            factors[0] = 4.95;
                            factors[1] = 8.81;
                            factors[2] = 3.86;
                            break;
                    }

                    gasteigerFactors[k][StepSize * i + i] = factors[0];
                    gasteigerFactors[k][StepSize * i + i + 1] = factors[1];
                    gasteigerFactors[k][StepSize * i + i + 2] = factors[2];
                    gasteigerFactors[k][StepSize * i + i + 5] = ac.Atoms[i].Charge.Value;

                    if (factors[0] == 0 && factors[1] == 0 && factors[2] == 0)
                    {
                        gasteigerFactors[k][StepSize * i + i + 3] = 1;
                    }
                    else
                    {
                        gasteigerFactors[k][StepSize * i + i + 3] = factors[0] + factors[1] + factors[2];
                    }
                }
            }

            return gasteigerFactors;
        }

        /// <summary>
        /// clean the flags <see cref="IAtomType.IsReactiveCenter"/> from the molecule.
        /// </summary>
        /// <param name="ac"></param>
        private static void CleanFlagReactiveCenter(IAtomContainer ac)
        {
            for (int j = 0; j < ac.Atoms.Count; j++)
                ac.Atoms[j].IsReactiveCenter = false;
            for (int j = 0; j < ac.Bonds.Count; j++)
                ac.Bonds[j].IsReactiveCenter = false;
        }
    }
}
