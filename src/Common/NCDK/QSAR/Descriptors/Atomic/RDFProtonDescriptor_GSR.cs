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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class calculates GSR proton descriptors used in neural networks for H1 NMR
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
    public partial class RDFProtonDescriptorGSR : AbstractDescriptor, IAtomicDescriptor
    {
        private const string prefix = "gSr_";
        private const int desc_length = 7;

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
            ////////////////////////THE FOUTH DESCRIPTOR IS gS(r), WHICH TAKES INTO ACCOUNT Single BONDS IN RIGID SYSTEMS

            if (singles.Any())
            {
                const double limitSup = Math.PI / 2;
                const double smooth = -1.15;
                for (int c = 0; c < desc_length; c++)
                {
                    var ghs = limitSup * ((double)c / desc_length);
                    double sum = 0;
                    for (int sing = 0; sing < singles.Count; sing++)
                    {
                        var thisSingleBond = singles[sing];
                        var position = thisSingleBond;
                        var theSingleBond = mol.Bonds[position];
                        var middlePoint = theSingleBond.GetGeometric3DCenter();
                        var singleBondAtom0 = theSingleBond.Atoms[0];
                        var singleBondAtom1 = theSingleBond.Atoms[1];
                        var dist0 = CalculateDistanceBetweenTwoAtoms(singleBondAtom0, atom);
                        var dist1 = CalculateDistanceBetweenTwoAtoms(singleBondAtom1, atom);

                        var aA = middlePoint;
                        var aB = dist1 > dist0 ? singleBondAtom0.Point3D.Value : singleBondAtom1.Point3D.Value;
                        var bA = middlePoint;
                        var bB = atom.Point3D.Value;

                        var values = CalculateDistanceBetweenAtomAndBond(atom, theSingleBond);
                        var angle = CalculateAngleBetweenTwoLines(aA, aB, bA, bB);
                        var partial = (1 / Math.Pow(values[0], 2)) * Math.Exp(smooth * Math.Pow(ghs - angle, 2));
                        sum += partial;
                    }
                    rdfProtonCalculatedValues.Add(sum);
                    Debug.WriteLine($"RDF gSr prob.: {sum} at distance {ghs}");
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
