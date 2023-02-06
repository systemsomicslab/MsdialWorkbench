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

using System;

namespace NCDK.Charges
{
    /// <summary>
    /// The calculation of the Gasteiger Marsili (PEOE) partial charges is based on
    /// <token>cdk-cite-GM80</token>. 
    /// </summary>
    /// <remarks>
    /// This class only implements the original method which only
    /// applies to É–-bond systems.
    /// </remarks>
    // @author      chhoppe
    // @author      rojas
    // @cdk.module  charges
    // @cdk.created 2004-11-03
    // @cdk.keyword partial atomic charges
    // @cdk.keyword charge distribution
    // @cdk.keyword electronegativities, partial equalization of orbital
    // @cdk.keyword PEOE
    public class GasteigerMarsiliPartialCharges : IChargeCalculator
    {
        // Flag is set if the formal charge of a chemobject is changed due to resonance.

        public GasteigerMarsiliPartialCharges() { }

        /// <summary>
        /// chi_cat value for hydrogen, because H poses a special problem due to lack of possible second ionisation.
        /// </summary>
        public double ChiCatHydrogen { get; set; } = 20.02;

        public double MaxGasteigerDamp { get; set; } = 0.5;

        public double MaxGasteigerIterations { get; set; } = 20;

        /// <summary>
        /// Main method which assigns Gasteiger Marisili partial sigma charges.
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <param name="setCharge">The Charge</param>
        /// <returns>AtomContainer with partial charges</returns>
        public IAtomContainer AssignGasteigerMarsiliSigmaPartialCharges(IAtomContainer ac, bool setCharge)
        {
            /* add the initial charge to 0. According results of Gasteiger */
            for (int i = 0; i < ac.Atoms.Count; i++)
                ac.Atoms[i].Charge = 0.0;
            var gasteigerFactors = AssignGasteigerSigmaMarsiliFactors(ac);//a,b,c,deoc,chi,q
            double alpha = 1.0;
            double q;
            double deoc;

            int atom1 = 0;
            int atom2 = 0;

            var q_old = new double[ac.Atoms.Count];
            for (int i = 0; i < q_old.Length; i++)
                q_old[0] = 20.0;
            for (int i = 0; i < MaxGasteigerIterations; i++)
            {
                alpha *= MaxGasteigerDamp;
                bool isDifferent = false;
                for (int j = 0; j < ac.Atoms.Count; j++)
                {
                    q = gasteigerFactors[StepSize * j + j + 5];
                    double difference = Math.Abs(q_old[j]) - Math.Abs(q);
                    if (Math.Abs(difference) > 0.001)
                        isDifferent = true;
                    q_old[j] = q;

                    gasteigerFactors[StepSize * j + j + 4] =
                        gasteigerFactors[StepSize * j + j + 2] * q * q
                      + gasteigerFactors[StepSize * j + j + 1] * q 
                      + gasteigerFactors[StepSize * j + j];
                }
                if (!isDifferent) /* automatically break the maximum iterations */
                    goto break_out;

                foreach (var bond in ac.Bonds)
                {
                    atom1 = ac.Atoms.IndexOf(bond.Begin);
                    atom2 = ac.Atoms.IndexOf(bond.End);

                    if (gasteigerFactors[StepSize * atom1 + atom1 + 4] >= gasteigerFactors[StepSize * atom2 + atom2 + 4])
                    {
                        if (ac.Atoms[atom2].AtomicNumber.Equals(AtomicNumbers.H))
                        {
                            deoc = ChiCatHydrogen;
                        }
                        else
                        {
                            deoc = gasteigerFactors[StepSize * atom2 + atom2 + 3];
                        }
                    }
                    else
                    {
                        if (ac.Atoms[atom1].AtomicNumber.Equals(AtomicNumbers.H))
                        {
                            deoc = ChiCatHydrogen;
                        }
                        else
                        {
                            deoc = gasteigerFactors[StepSize * atom1 + atom1 + 3];
                        }
                    }

                    q = (gasteigerFactors[StepSize * atom1 + atom1 + 4] - gasteigerFactors[StepSize * atom2 + atom2 + 4]) / deoc;
                    gasteigerFactors[StepSize * atom1 + atom1 + 5] -= (q * alpha);
                    gasteigerFactors[StepSize * atom2 + atom2 + 5] += (q * alpha);
                }
            }
            break_out:

            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                ac.Atoms[i].Charge = gasteigerFactors[StepSize * i + i + 5];
            }
            return ac;
        }

        public void CalculateCharges(IAtomContainer container)
        {
            try
            {
                this.AssignGasteigerMarsiliSigmaPartialCharges(container, true);
            }
            catch (Exception exception)
            {
                throw new CDKException($"Could not calculate Gasteiger-Marsili sigma charges: {exception.Message}", exception);
            }
        }

        public int StepSize { get; set; } = 5;

        /// <summary>
        /// Method which stores and assigns the factors a,b,c and CHI+.
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <returns>Array of doubles [a1,b1,c1,denom1,chi1,q1...an,bn,cn...] 1:Atom 1-n in AtomContainer</returns>
        public double[] AssignGasteigerSigmaMarsiliFactors(IAtomContainer ac)
        {
            //a,b,c,denom,chi,q
            var gasteigerFactors = new double[(ac.Atoms.Count * (StepSize + 1))];
            var factors = new double[] { 0.0, 0.0, 0.0 };
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                factors[0] = 0.0;
                factors[1] = 0.0;
                factors[2] = 0.0;
                var atom = ac.Atoms[i];
                var symbol = atom.Symbol;
                var maxBondOrder = ac.GetMaximumBondOrder(atom);
                var charge = atom.FormalCharge;
                switch (atom.AtomicNumber)
                {
                    case AtomicNumbers.H:
                        factors[0] = 7.17;
                        factors[1] = 6.24;
                        factors[2] = -0.56;
                        break;
                    case AtomicNumbers.C:
                        if (maxBondOrder == BondOrder.Double 
                         || (maxBondOrder == BondOrder.Single && (charge == -1 || charge == +1)))
                        {
                            factors[0] = 8.79;/* 8.79 *//* 8.81 */
                            factors[1] = 9.32;/* 9.32 *//* 9.34 */
                            factors[2] = 1.51;/* 1.51 *//* 1.52 */
                        }
                        else if (maxBondOrder == BondOrder.Single && charge == 0)
                        {
                            factors[0] = 7.98;
                            factors[1] = 9.18;
                            factors[2] = 1.88;
                        }
                        else if (maxBondOrder == BondOrder.Triple
                              || maxBondOrder == BondOrder.Quadruple)
                        {
                            factors[0] = 10.39;/* 10.39 */
                            factors[1] = 9.45;/* 9.45 */
                            factors[2] = 0.73;
                        }
                        break;
                    case AtomicNumbers.N:
                        if ((maxBondOrder == BondOrder.Single)
                         && (charge != -1))
                        {
                            factors[0] = 11.54;
                            factors[1] = 10.82;
                            factors[2] = 1.36;
                        }
                        else if ((maxBondOrder == BondOrder.Double)
                              || (maxBondOrder == BondOrder.Single))
                        {
                            factors[0] = 12.87;
                            factors[1] = 11.15;
                            factors[2] = 0.85;
                        }
                        else if (maxBondOrder == BondOrder.Triple
                              || maxBondOrder == BondOrder.Quadruple)
                        {
                            factors[0] = 17.68;/* 15.68 */
                            factors[1] = 12.70;/* 11.70 */
                            factors[2] = -0.27;/*-0.27*/
                        }
                        break;
                    case AtomicNumbers.O:
                        if ((maxBondOrder == BondOrder.Single)
                         && (charge != -1))
                        {
                            factors[0] = 14.18;
                            factors[1] = 12.92;
                            factors[2] = 1.39;
                        }
                        else if ((maxBondOrder == BondOrder.Double)
                              || (maxBondOrder == BondOrder.Single))
                        {
                            factors[0] = 17.07; /* paramaters aren'T correct parametrized. */
                            factors[1] = 13.79;
                            factors[2] = 0.47;/* 0.47 */
                        }
                        break;
                    case AtomicNumbers.Si: // <--not correct
                        factors[0] = 8.10;// <--not correct
                        factors[1] = 7.92;// <--not correct
                        factors[2] = 1.78;// <--not correct
                        break;
                    case AtomicNumbers.P:
                        factors[0] = 8.90;
                        factors[1] = 8.32;
                        factors[2] = 1.58;
                        break;
                    case AtomicNumbers.S:
                        factors[0] = 10.14;/* 10.14 */
                        factors[1] = 9.13;/* 9.13 */
                        factors[2] = 1.38;/* 1.38 */
                        break;
                    case AtomicNumbers.F:
                        factors[0] = 14.66;
                        factors[1] = 13.85;
                        factors[2] = 2.31;
                        break;
                    case AtomicNumbers.Cl:
                        factors[0] = 12.31;/* 11.0 *//* 12.31 */
                        factors[1] = 10.84;/* 9.69 *//* 10.84 */
                        factors[2] = 1.512;/* 1.35 *//* 1.512 */
                        break;
                    case AtomicNumbers.Br:
                        factors[0] = 11.44;/* 10.08 *//* 11.2 */
                        factors[1] = 9.63;/* 8.47 *//* 9.4 */
                        factors[2] = 1.31;/* 1.16 *//* 1.29 */
                        break;
                    case AtomicNumbers.I:
                        factors[0] = 9.88;/* 9.90 */
                        factors[1] = 7.95;/* 7.96 */
                        factors[2] = 0.945;/* 0.96 */
                        break;
                    default:
                        throw new CDKException($"Partial charge not-supported for element: '{symbol}'.");
                }

                gasteigerFactors[StepSize * i + i] = factors[0];
                gasteigerFactors[StepSize * i + i + 1] = factors[1];
                gasteigerFactors[StepSize * i + i + 2] = factors[2];
                gasteigerFactors[StepSize * i + i + 5] = atom.Charge.Value;
                if (factors[0] == 0 && factors[1] == 0 && factors[2] == 0)
                {
                    gasteigerFactors[StepSize * i + i + 3] = 1;
                }
                else
                {
                    gasteigerFactors[StepSize * i + i + 3] = factors[0] + factors[1] + factors[2];
                }
            }
            return gasteigerFactors;
        }
    }
}
