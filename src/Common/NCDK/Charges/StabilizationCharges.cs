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

using NCDK.Graphs;
using NCDK.Reactions.Types;
using NCDK.Tools;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Charges
{
    /// <summary>
    /// The stabilization of the positive and the negative charge
    /// obtained (e.g in the polar breaking of a bond) is calculated from the sigma- and
    /// lone pair-electronegativity values of the atoms that are in conjugation to the atoms
    /// obtaining the charges. Based on H. Saller Dissertation @cdk.cite{SallerH1895}.
    /// </summary>
    // @author       Miguel Rojas Cherto
    // @cdk.created  2008-104-31
    // @cdk.module   charges
    // @cdk.keyword  stabilization charge
    public static class StabilizationCharges
    {
        /// <summary>
        /// calculate the stabilization of orbitals when they contain deficiency of charge.
        /// </summary>
        /// <param name="atomContainer">the molecule to be considered</param>
        /// <param name="atom">IAtom for which effective atom StabilizationCharges factor should be calculated</param>
        /// <returns>stabilizationValue</returns>
        public static double CalculatePositive(IAtomContainer atomContainer, IAtom atom)
        {
            /* restrictions */
            //        if(atomContainer.GetConnectedSingleElectronsCount(atom) > 0 || atom.FormalCharge != 1){
            if (atom.FormalCharge != 1)
            {
                return 0.0;
            }

            // only must be generated all structures which stabilize the atom in question.
            StructureResonanceGenerator gRI = new StructureResonanceGenerator();
            var reactionList = gRI.Reactions.ToList();
            reactionList.Add(new HyperconjugationReaction());
            gRI.Reactions = reactionList;
            var resonanceS = gRI.GetStructures(atomContainer);
            var containerS = gRI.GetContainers(atomContainer);
            if (resonanceS.Count < 2) // meaning it was not find any resonance structure
                return 0.0;

            int positionStart = atomContainer.Atoms.IndexOf(atom);

            var result1 = new List<double>();
            var distance1 = new List<int>();

            resonanceS.RemoveAt(0);// the first is the initial structure
            foreach (var resonance in resonanceS)
            {
                if (resonance.Atoms.Count < 2) // resonance with only one atom donnot have resonance
                    continue;

                ShortestPaths shortestPaths = new ShortestPaths(resonance, resonance.Atoms[positionStart]);

                /* search positive charge */

                PiElectronegativity electronegativity = new PiElectronegativity();

                foreach (var atomP in resonance.Atoms)
                {
                    IAtom atomR = atomContainer.Atoms[resonance.Atoms.IndexOf(atomP)];
                    if (containerS[0].Contains(atomR))
                    {

                        electronegativity.MaxIterations = 6;
                        double result = electronegativity.CalculatePiElectronegativity(resonance, atomP);
                        result1.Add(result);

                        int dis = shortestPaths.GetDistanceTo(atomP);
                        distance1.Add(dis);
                    }

                }
            }
            /* logarithm */
            double value = 0.0;
            double sum = 0.0;

            var itDist = distance1.GetEnumerator();
            foreach (var ee in result1)
            {
                double suM = ee;
                if (suM < 0) suM = -1 * suM;
                itDist.MoveNext();
                sum += suM * Math.Pow(0.67, itDist.Current);
            }
            value = sum;

            return value;
        }
    }
}
