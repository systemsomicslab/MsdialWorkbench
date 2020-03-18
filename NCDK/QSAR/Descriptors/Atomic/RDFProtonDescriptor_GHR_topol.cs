/* Copyright (C) 2004-2007  Matteo Floris <mfe4@users.sf.net>
 * Copyright (C) 2006-2007  Federico
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Graphs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class calculates GHR topological proton descriptors used in neural networks for H1 NMR
    /// shift <token>cdk-cite-AiresDeSousa2002</token>. It only applies to (explicit) hydrogen atoms,
    /// requires aromaticity to be perceived (possibly done via a parameter), and
    /// needs 3D coordinates for all atoms.
    /// </summary>
    // @author      Federico
    // @cdk.created 2006-12-11
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:rdfProtonCalculatedValues
    // @cdk.bug     1632419
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#rdfProtonCalculatedValues")]
    public partial class RDFProtonDescriptorGHRTopology : AbstractDescriptor, IAtomicDescriptor
    {
        private const string prefix = "gHrTop_";
        private const int desc_length = 15;

        private static bool MakeDescriptorLastStage(
            List<double> rdfProtonCalculatedValues,
            IAtom atom,
            IAtom clonedAtom,
            IAtomContainer mol,
            IAtom neighbour0,
            List<int> singles,
            List<int> doubles,
            List<int> atoms,
            List<int> bondsInCycloex)
        {
            ///////////////////////THE SECOND CALCULATED DESCRIPTOR IS g(H)r TOPOLOGICAL WITH SUM OF BOND LENGTHS

            if (atoms.Any())
            {
                const double limitInf = 1.4;
                const double limitSup = 4;
                const double smooth = -20;
                var startVertex = clonedAtom;
                var shortestPaths = new ShortestPaths(mol, startVertex);
                for (int c = 0; c < desc_length; c++)
                {
                    var ghrt = limitInf + (limitSup - limitInf) * ((double)c / desc_length);
                    double sum = 0;
                    for (int at = 0; at < atoms.Count; at++)
                    {
                        double distance = 0;
                        var thisAtom = atoms[at];
                        var position = thisAtom;
                        var endVertex = mol.Atoms[position];
                        var atom2 = mol.Atoms[position];
                        var path = shortestPaths.GetPathTo(endVertex);
                        for (int i = 1; i < path.Length; i++)
                        {
                            distance += CalculateDistanceBetweenTwoAtoms(mol.Atoms[path[i - 1]], mol.Atoms[path[i]]);
                        }
                        var partial = atom2.Charge.Value * Math.Exp(smooth * Math.Pow(ghrt - distance, 2));
                        sum += partial;
                    }
                    rdfProtonCalculatedValues.Add(sum);
                    Debug.WriteLine($"RDF gr-topol distance prob.: {sum} at distance {ghrt}");
                }
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
