/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Config;
using NCDK.Numerics;
using System;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Charges
{
    /// <summary>
    /// The calculation of the inductive partial atomic charges and equalization of
    /// effective electronegativities is based on <token>cdk-cite-CHE03</token>.
    /// </summary>
    // @author      mfe4
    // @cdk.module  charges
    // @cdk.created 2004-11-03
    // @cdk.keyword partial atomic charges
    // @cdk.keyword charge distribution
    // @cdk.keyword electronegativity
    public class InductivePartialCharges : IChargeCalculator
    {
        private static double[] pauling;
        private IsotopeFactory ifac = null;
        private readonly AtomTypeFactory factory = CDK.JmolAtomTypeFactory;

        /// <summary>
        ///  Constructor for the InductivePartialCharges object.
        /// </summary>
        public InductivePartialCharges()
        {
            if (pauling == null)
            {
                // pauling ElEn :
                // second position is H, last is Ac
                pauling = new double[]{0, 2.1, 0, 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 0, 0.9, 1.2, 1.5, 1.8, 2.1, 2.5, 3.0,
                    0, 0.8, 1.0, 1.3, 1.5, 1.6, 1.6, 1.5, 1.8, 1.8, 1.8, 1.9, 1.6, 1.6, 1.8, 2.0, 2.4, 2.8, 0, 0.8,
                    1.0, 1.3, 1.4, 1.6, 1.8, 1.9, 2.2, 2.2, 2.2, 1.9, 1.7, 1.7, 1.8, 1.9, 2.1, 2.5, 0.7, 0.9, 1.1, 1.3,
                    1.5, 1.7, 1.9, 2.2, 2.2, 2.2, 2.4, 1.9, 1.8, 1.8, 1.9, 2.0, 2.2, 0, 0.7, 0.9, 1.1};
            }
        }

        /// <summary>
        /// Main method, set charge as atom properties.
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <returns>AtomContainer</returns>
        public IAtomContainer AssignInductivePartialCharges(IAtomContainer ac)
        {
            int stepsLimit = 9;
            var atoms = ac.Atoms.ToArray();
            var pChInch = new double[atoms.Length * (stepsLimit + 1)];
            var ElEn = new double[atoms.Length * (stepsLimit + 1)];
            var pCh = new double[atoms.Length * (stepsLimit + 1)];
            var startEE = GetPaulingElectronegativities(ac, true);
            for (int e = 0; e < atoms.Length; e++)
            {
                ElEn[e] = startEE[e];
            }
            for (int s = 1; s < 10; s++)
            {
                for (int a = 0; a < atoms.Length; a++)
                {
                    pChInch[a + (s * atoms.Length)] = GetAtomicChargeIncrement(ac, a, ElEn, s);
                    pCh[a + (s * atoms.Length)] = pChInch[a + (s * atoms.Length)] + pCh[a + ((s - 1) * atoms.Length)];
                    ElEn[a + (s * atoms.Length)] = ElEn[a + ((s - 1) * atoms.Length)]
                            + (pChInch[a + (s * atoms.Length)] / GetAtomicSoftnessCore(ac, a));
                    if (s == 9)
                    {
                        atoms[a].SetProperty("InductivePartialCharge", pCh[a + (s * atoms.Length)]);
                        atoms[a].SetProperty("EffectiveAtomicElectronegativity", ElEn[a + (s * atoms.Length)]);
                    }
               }
            }
            return ac;
        }

        public void CalculateCharges(IAtomContainer container)
        {
            try
            {
                this.AssignInductivePartialCharges(container);
            }
            catch (Exception exception)
            {
                throw new CDKException($"Could not calculate inductive partial charges: {exception.Message}", exception);
            }
        }

        /// <summary>
        ///  Gets the paulingElectronegativities attribute of the
        ///  InductivePartialCharges object.
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <param name="modified">if true, some values are modified by following the reference</param>
        /// <returns>The pauling electronegativities</returns>
        public double[] GetPaulingElectronegativities(IAtomContainer ac, bool modified)
        {
            var paulingElectronegativities = new double[ac.Atoms.Count];
            string symbol = null;
            int atomicNumber = 0;
            try
            {
                ifac = CDK.IsotopeFactory;
                for (int i = 0; i < ac.Atoms.Count; i++)
                {
                    var atom = ac.Atoms[i];
                    symbol = ac.Atoms[i].Symbol;
                    var element = ifac.GetElement(symbol);
                    atomicNumber = element.AtomicNumber;
                    if (modified)
                    {
                        switch (atom.AtomicNumber)
                        {
                            case AtomicNumbers.Cl:
                                paulingElectronegativities[i] = 3.28;
                                break;
                            case AtomicNumbers.Br:
                                paulingElectronegativities[i] = 3.13;
                                break;
                            case AtomicNumbers.I:
                                paulingElectronegativities[i] = 2.93;
                                break;
                            case AtomicNumbers.H:
                                paulingElectronegativities[i] = 2.10;
                                break;
                            case AtomicNumbers.C:
                                if (ac.GetMaximumBondOrder(atom) == BondOrder.Single)
                                {
                                    // Csp3
                                    paulingElectronegativities[i] = 2.20;
                                }
                                else if (ac.GetMaximumBondOrder(atom) == BondOrder.Double)
                                {
                                    paulingElectronegativities[i] = 2.31;
                                }
                                else
                                {
                                    paulingElectronegativities[i] = 3.15;
                                }
                                break;
                            case AtomicNumbers.O:
                                if (ac.GetMaximumBondOrder(atom) == BondOrder.Single)
                                {
                                    // Osp3
                                    paulingElectronegativities[i] = 3.20;
                                }
                                else if (ac.GetMaximumBondOrder(atom) != BondOrder.Single)
                                {
                                    paulingElectronegativities[i] = 4.34;
                                }
                                break;
                            case AtomicNumbers.Si:
                                paulingElectronegativities[i] = 1.99;
                                break;
                            case AtomicNumbers.S:
                                paulingElectronegativities[i] = 2.74;
                                break;
                            case AtomicNumbers.N:
                                paulingElectronegativities[i] = 2.59;
                                break;
                            default:
                                paulingElectronegativities[i] = pauling[atomicNumber];
                                break;
                        }
                    }
                    else
                    {
                        paulingElectronegativities[i] = pauling[atomicNumber];
                    }
                }
                return paulingElectronegativities;
            }
            catch (Exception ex1)
            {
                Debug.WriteLine(ex1);
                throw new CDKException($"Problems with IsotopeFactory due to {ex1.Message}", ex1);
            }
        }

        /// <summary>
        ///  Gets the atomicSoftnessCore attribute of the InductivePartialCharges object.
        /// </summary>
        /// <remarks>
        /// this method returns the result of the core of the equation of atomic softness
        /// that can be used for qsar descriptors and during the iterative calculation
        /// of effective electronegativity
        /// </remarks>
        /// <param name="ac">AtomContainer</param>
        /// <param name="atomPosition">position of target atom</param>
        /// <returns>The atomicSoftnessCore value</returns>
        /// <exception cref="CDKException"></exception>
        public double GetAtomicSoftnessCore(IAtomContainer ac, int atomPosition)
        {
            IAtom target = null;
            double core = 0;
            double radiusTarget = 0;
            target = ac.Atoms[atomPosition];
            double partial = 0;
            double radius = 0;
            IAtomType type = null;
            try
            {
                type = factory.GetAtomType(ac.Atoms[atomPosition].Symbol);
                var n = ac.Atoms[atomPosition].AtomicNumber;
                if (GetCovalentRadius(n, ac.GetMaximumBondOrder(target)) > 0)
                {
                    radiusTarget = GetCovalentRadius(n, ac.GetMaximumBondOrder(target));
                }
                else
                {
                    radiusTarget = type.CovalentRadius.Value;
                }

            }
            catch (Exception ex1)
            {
                Debug.WriteLine(ex1);
                throw new CDKException("Problems with AtomTypeFactory due to " + ex1.Message, ex1);
            }

            foreach (var atom in ac.Atoms)
            {
                if (!target.Equals(atom))
                {
                    partial = 0;
                    try
                    {
                        type = factory.GetAtomType(atom.Symbol);
                    }
                    catch (Exception ex1)
                    {
                        Debug.WriteLine(ex1);
                        throw new CDKException($"Problems with AtomTypeFactory due to {ex1.Message}", ex1);
                    }
                    if (GetCovalentRadius(atom.AtomicNumber, ac.GetMaximumBondOrder(atom)) > 0)
                    {
                        radius = GetCovalentRadius(atom.AtomicNumber, ac.GetMaximumBondOrder(atom));
                    }
                    else
                    {
                        radius = type.CovalentRadius.Value;
                    }
                    partial += radius * radius;
                    partial += (radiusTarget * radiusTarget);
                    partial = partial / (CalculateSquaredDistanceBetweenTwoAtoms(target, atom));
                    core += partial;
                }
            }
            core = 2 * core;
            core = 0.172 * core;
            return core;
        }

        // this method returns the partial charge increment for a given atom
        /// <summary>
        ///  Gets the atomicChargeIncrement attribute of the InductivePartialCharges object.
        /// </summary>
        /// <param name="ac">AtomContainer</param>
        /// <param name="atomPosition">position of target atom</param>
        /// <param name="ElEn">electronegativity of target atom</param>
        /// <param name="step">step in iteration</param>
        /// <returns>The atomic charge increment for the target atom</returns>
        /// <exception cref="CDKException"></exception>
        private double GetAtomicChargeIncrement(IAtomContainer ac, int atomPosition, double[] ElEn, int step)
        {
            IAtom[] allAtoms = null;
            IAtom target = null;
            double incrementedCharge = 0;
            double radiusTarget = 0;
            target = ac.Atoms[atomPosition];
            allAtoms = ac.Atoms.ToArray();
            double tmp = 0;
            double radius = 0;
            IAtomType type = null;
            try
            {
                type = factory.GetAtomType(target.Symbol);
                if (GetCovalentRadius(target.AtomicNumber, ac.GetMaximumBondOrder(target)) > 0)
                {
                    radiusTarget = GetCovalentRadius(target.AtomicNumber, ac.GetMaximumBondOrder(target));
                }
                else
                {
                    radiusTarget = type.CovalentRadius.Value;
                }
            }
            catch (Exception ex1)
            {
                Debug.WriteLine(ex1);
                throw new CDKException($"Problems with AtomTypeFactory due to {ex1.Message}", ex1);
            }

            for (int a = 0; a < allAtoms.Length; a++)
            {
                if (!target.Equals(allAtoms[a]))
                {
                    tmp = 0;
                    var atom = allAtoms[a];
                    try
                    {
                        type = factory.GetAtomType(atom.Symbol);
                    }
                    catch (Exception ex1)
                    {
                        Debug.WriteLine(ex1);
                        throw new CDKException("Problems with AtomTypeFactory due to " + ex1.Message, ex1);
                    }
                    if (GetCovalentRadius(atom.AtomicNumber, ac.GetMaximumBondOrder(allAtoms[a])) > 0)
                    {
                        radius = GetCovalentRadius(atom.AtomicNumber, ac.GetMaximumBondOrder(allAtoms[a]));
                    }
                    else
                    {
                        radius = type.CovalentRadius.Value;
                    }
                    tmp = (ElEn[a + ((step - 1) * allAtoms.Length)] - ElEn[atomPosition + ((step - 1) * allAtoms.Length)]);
                    tmp = tmp * ((radius * radius) + (radiusTarget * radiusTarget));
                    tmp = tmp / (CalculateSquaredDistanceBetweenTwoAtoms(target, allAtoms[a]));
                    incrementedCharge += tmp;
                }
            }
            incrementedCharge = 0.172 * incrementedCharge;
            return incrementedCharge;
        }

        /// <summary>
        /// Gets the covalentRadius attribute of the InductivePartialCharges object.
        /// </summary>
        /// <param name="atomicNumber">atomic number of the atom</param>
        /// <param name="maxBondOrder">its max bond order</param>
        /// <returns>The covalentRadius value given by the reference</returns>
        private static double GetCovalentRadius(int atomicNumber, BondOrder maxBondOrder)
        {
            double radiusTarget = 0;
            switch (atomicNumber)
            {
                case AtomicNumbers.F:
                    radiusTarget = 0.64;
                    break;
                case AtomicNumbers.Cl:
                    radiusTarget = 0.99;
                    break;
                case AtomicNumbers.Br:
                    radiusTarget = 1.14;
                    break;
                case AtomicNumbers.I:
                    radiusTarget = 1.33;
                    break;
                case AtomicNumbers.H:
                    radiusTarget = 0.30;
                    break;
                case AtomicNumbers.C:
                    if (maxBondOrder == BondOrder.Single)
                    {
                        // Csp3
                        radiusTarget = 0.77;
                    }
                    else if (maxBondOrder == BondOrder.Double)
                    {
                        radiusTarget = 0.67;
                    }
                    else
                    {
                        radiusTarget = 0.60;
                    }
                    break;
                case AtomicNumbers.O:
                    if (maxBondOrder == BondOrder.Single)
                    {
                        // Csp3
                        radiusTarget = 0.66;
                    }
                    else if (maxBondOrder != BondOrder.Single)
                    {
                        radiusTarget = 0.60;
                    }
                    break;
                case AtomicNumbers.Si:
                    radiusTarget = 1.11;
                    break;
                case AtomicNumbers.S:
                    radiusTarget = 1.04;
                    break;
                case AtomicNumbers.N:
                    radiusTarget = 0.70;
                    break;
                default:
                    radiusTarget = 0;
                    break;
            }
            return radiusTarget;
        }

        /// <summary>
        /// Evaluate the square of the Euclidean distance between two atoms.
        /// </summary>
        /// <param name="atom1">first atom</param>
        /// <param name="atom2">second atom</param>
        /// <returns>squared distance between the 2 atoms</returns>
        private static double CalculateSquaredDistanceBetweenTwoAtoms(IAtom atom1, IAtom atom2)
        {
            double distance = 0;
            double tmp = 0;
            var firstPoint = atom1.Point3D.Value;
            var secondPoint = atom2.Point3D.Value;
            tmp = Vector3.Distance(firstPoint, secondPoint);
            distance = tmp * tmp;
            return distance;
        }
    }
}
