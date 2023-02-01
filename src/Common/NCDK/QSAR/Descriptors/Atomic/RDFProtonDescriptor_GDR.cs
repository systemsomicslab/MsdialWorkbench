/* Copyright (C) 2004-2007  Matteo Floris <mfe4@users.sf.net>
 * Copyright (C) 2006-2007  Federico
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
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

using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class calculates GDR proton descriptors used in neural networks for H1 NMR
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
    public partial class RDFProtonDescriptorGDR : AbstractDescriptor, IAtomicDescriptor
    {
        private const string prefix = "gDr_";
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
            ////////////////////////THE THIRD DESCRIPTOR IS gD(r) WITH DISTANCE AND RADIAN ANGLE BTW THE PROTON AND THE MIDDLE POINT OF Double BOND

            //if (doubles.Count > -0.0001)
            {
                const double limitSup = Math.PI / 2;
                const double smooth = -1.15;
                for (int c = 0; c < desc_length; c++)
                {
                    var ghd = limitSup * ((double)c / desc_length);
                    double sum = 0;
                    for (int dou = 0; dou < doubles.Count; dou++)
                    {
                        double partial = 0;
                        int position = doubles[dou];
                        var theDoubleBond = mol.Bonds[position];
                        var goodPosition = GetNearestBondtoAGivenAtom(mol, atom, theDoubleBond);
                        var goodBond = mol.Bonds[goodPosition];
                        var goodAtom0 = goodBond.Atoms[0];
                        var goodAtom1 = goodBond.Atoms[1];
                        var atomP = atom.Point3D.Value;
                        var goodAtom0P = goodAtom0.Point3D.Value;
                        var goodAtom1P = goodAtom1.Point3D.Value;

                        var middlePoint = theDoubleBond.GetGeometric3DCenter();
                        var values = CalculateDistanceBetweenAtomAndBond(atom, theDoubleBond);

                        Vector3 aA;
                        Vector3 aB;
                        double angle;

                        if (theDoubleBond.Contains(goodAtom0))
                        {
                            aA = new Vector3(goodAtom0P.X, goodAtom0P.Y, goodAtom0P.Z);
                            aB = new Vector3(goodAtom1P.X, goodAtom1P.Y, goodAtom1P.Z);
                        }
                        else
                        {
                            aA = new Vector3(goodAtom1P.X, goodAtom1P.Y, goodAtom1P.Z);
                            aB = new Vector3(goodAtom0P.X, goodAtom0P.Y, goodAtom0P.Z);
                        }
                        var bA = new Vector3(middlePoint.X, middlePoint.Y, middlePoint.Z);
                        var bB = new Vector3(atomP.X, atomP.Y, atomP.Z);
                        angle = CalculateAngleBetweenTwoLines(aA, aB, bA, bB);
                        partial = 1 / Math.Pow(values[0], 2) * Math.Exp(smooth * Math.Pow(ghd - angle, 2));
                        sum += partial;
                    }
                    rdfProtonCalculatedValues.Add(sum);
                    Debug.WriteLine($"GDR prob dist.: {sum} at distance {ghd}");
                }
                return true;
            }
            //else
            //{
            //    return false;
            //}
        }
    }
}
