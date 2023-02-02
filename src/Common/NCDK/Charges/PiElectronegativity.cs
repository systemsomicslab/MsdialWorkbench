/*  Copyright (C) 2008  Miguel Rojas <miguelrojasch@yahoo.es>
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
using System.Diagnostics;
using System.Linq;

namespace NCDK.Charges
{
    /// <summary>
    /// Calculation of the electronegativity of orbitals of a molecule
    /// by the method Gasteiger based on electronegativity is given by X = a + bq + c(q*q).
    /// </summary>
    // @author       Miguel Rojas Cherto
    // @cdk.created  2008-104-31
    // @cdk.module   charges
    // @cdk.keyword  electronegativity
    public class PiElectronegativity
    {
        private GasteigerMarsiliPartialCharges peoe = null;
        private GasteigerPEPEPartialCharges pepe = null;

        /// <summary>The maximum number of Iterations.</summary>
        public int MaxIterations { get; set; } = 6;

        /// <summary>Number of maximum resonance structures</summary>
        public int MaxResonanceStructures { get; set; } = 50;

        private IAtomContainer molPi;
        private IAtomContainer acOldP;
        private double[][] gasteigerFactors;

        /// <summary>
        /// Constructor for the PiElectronegativity object.
        /// </summary>
        public PiElectronegativity()
            : this(6, 50)
        { }

        /// <summary>
        /// Constructor for the Electronegativity object.
        /// </summary>
        /// <param name="maxIterations">The maximal number of Iteration</param>
        /// <param name="maxResonStruc">The maximal number of Resonance Structures</param>
        public PiElectronegativity(int maxIterations, int maxResonStruc)
        {
            peoe = new GasteigerMarsiliPartialCharges();
            pepe = new GasteigerPEPEPartialCharges();
            MaxIterations = maxIterations;
            MaxResonanceStructures = maxResonStruc;
        }

        /// <summary>
        /// calculate the electronegativity of orbitals pi.
        /// </summary>
        /// <param name="ac">IAtomContainer</param>
        /// <param name="atom">atom for which effective atom electronegativity should be calculated</param>
        /// <returns>piElectronegativity</returns>
        public double CalculatePiElectronegativity(IAtomContainer ac, IAtom atom)
        {
            return CalculatePiElectronegativity(ac, atom, MaxIterations, MaxResonanceStructures);
        }

        /// <summary>
        /// Calculate the electronegativity of orbitals pi.
        /// </summary>
        /// <param name="ac"></param>
        /// <param name="atom">atom for which effective atom electronegativity should be calculated</param>
        /// <param name="maxIterations">The maximal number of Iteration</param>
        /// <param name="maxResonStruc">The maximal number of Resonance Structures</param>
        /// <returns>Electronegativity of orbitals pi.</returns>
        public double CalculatePiElectronegativity(IAtomContainer ac, IAtom atom, int maxIterations, int maxResonStruc)
        {
            MaxIterations = maxIterations;
            MaxResonanceStructures = maxResonStruc;

            double electronegativity = 0;

            try
            {
                if (!ac.Equals(acOldP))
                {
                    molPi = ac.Builder.NewAtomContainer(ac);

                    peoe = new GasteigerMarsiliPartialCharges();
                    peoe.AssignGasteigerMarsiliSigmaPartialCharges(molPi, true);
                    var iSet = ac.Builder.NewAtomContainerSet();
                    iSet.Add(molPi);
                    iSet.Add(molPi);

                    gasteigerFactors = pepe.AssignrPiMarsilliFactors(iSet);

                    acOldP = ac;
                }
                IAtom atomi = molPi.Atoms[ac.Atoms.IndexOf(atom)];
                int atomPosition = molPi.Atoms.IndexOf(atomi);
                int stepSize = pepe.StepSize;
                int start = (stepSize * (atomPosition) + atomPosition);
                double q = atomi.Charge.Value;
                if (molPi.GetConnectedLonePairs(molPi.Atoms[atomPosition]).Any()
                        || molPi.GetMaximumBondOrder(atomi) != BondOrder.Single || atomi.FormalCharge != 0)
                {
                    return ((gasteigerFactors[1][start]) + (q * gasteigerFactors[1][start + 1]) + (gasteigerFactors[1][start + 2] * (q * q)));
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.StackTrace);
            }

            return electronegativity;
        }
    }
}
