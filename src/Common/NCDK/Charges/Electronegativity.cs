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

namespace NCDK.Charges
{
    /// <summary>
    /// Calculation of the electronegativity of orbitals of a molecule
    /// by the method Gasteiger based on electronegativity is given by X = a + bq + c(q*q).
    /// </summary>
    // @author         Miguel Rojas Cherto
    // @cdk.created    2008-104-31
    // @cdk.module  charges
    // @cdk.keyword electronegativity
    public class Electronegativity
    {
        private GasteigerMarsiliPartialCharges peoe = null;

        private IAtomContainer molSigma;
        private IAtomContainer acOldS;
        private double[] marsiliFactors;

        /// <summary>
        /// Constructor for the PiElectronegativity object.
        /// </summary>
        public Electronegativity()
            : this(6, 50)
        { }

        /// <summary>
        /// Constructor for the Electronegativity object.
        /// </summary>
        /// <param name="maxIterations">The maximal number of Iteration</param>
        /// <param name="maxResonStruc">The maximal number of Resonance Structures</param>
        public Electronegativity(int maxIterations, int maxResonStruc)
        {
            peoe = new GasteigerMarsiliPartialCharges();
            MaxIterations = maxIterations;
            MaxResonanceStructures = maxResonStruc;
        }

        /// <summary>
        /// calculate the electronegativity of orbitals sigma.
        /// </summary>
        /// <param name="ac">IAtomContainer</param>
        /// <param name="atom">atom for which effective atom electronegativity should be calculated</param>
        /// <returns>piElectronegativity</returns>
        public double CalculateSigmaElectronegativity(IAtomContainer ac, IAtom atom)
        {
            return CalculateSigmaElectronegativity(ac, atom, MaxIterations, MaxResonanceStructures);
        }

        /// <summary>
        /// calculate the electronegativity of orbitals sigma.
        /// </summary>
        /// <param name="ac">IAtomContainer</param>
        /// <param name="atom">atom for which effective atom electronegativity should be calculated</param>
        /// <param name="maxIterations">The maximal number of Iterations</param>
        /// <param name="maxResonStruc">The maximal number of Resonance Structures</param>
        /// <returns>piElectronegativity</returns>
        public double CalculateSigmaElectronegativity(IAtomContainer ac, IAtom atom, int maxIterations, int maxResonStruc)
        {
            MaxIterations = maxIterations;
            MaxResonanceStructures = maxResonStruc;

            double electronegativity = 0;

            try
            {
                if (!ac.Equals(acOldS))
                {
                    molSigma = ac.Builder.NewAtomContainer(ac);
                    peoe.MaxGasteigerIterations = MaxIterations;
                    peoe.AssignGasteigerMarsiliSigmaPartialCharges(molSigma, true);
                    marsiliFactors = peoe.AssignGasteigerSigmaMarsiliFactors(molSigma);

                    acOldS = ac;
                }
                int stepSize = peoe.StepSize;
                int atomPosition = ac.Atoms.IndexOf(atom);
                int start = (stepSize * (atomPosition) + atomPosition);

                electronegativity = 
                    marsiliFactors[start]
                  + (molSigma.Atoms[atomPosition].Charge.Value * marsiliFactors[start + 1]) 
                  + (marsiliFactors[start + 2] 
                    * (molSigma.Atoms[atomPosition].Charge.Value * molSigma.Atoms[atomPosition].Charge.Value));
                return electronegativity;
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }

            return electronegativity;
        }

        /// <summary>
        /// the maximum number of Iterations.
        /// </summary>
        public int MaxIterations { get; set; } = 6;

        /// <summary>
        /// the maximum number of resonance structures.
        /// </summary>
        public int MaxResonanceStructures { get; set; } = 50;
    }
}
