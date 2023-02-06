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

using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class calculates G3R proton descriptors used in neural networks for H1
    /// NMR shift <token>cdk-cite-AiresDeSousa2002</token>. It only applies to (explicit) hydrogen atoms,
    /// requires aromaticity to be perceived (possibly done via a parameter), and
    /// needs 3D coordinates for all atoms.
    /// </summary>
    // @author      Federico
    // @cdk.created 2006-12-11
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:rdfProtonCalculatedValues
    // @cdk.bug     1632419
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#rdfProtonCalculatedValues")]
    public partial class RDFProtonDescriptorG3R : AbstractDescriptor, IAtomicDescriptor
    {
        private const string prefix = "g3r_";
        private const int desc_length = 13;
 
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
            if (bondsInCycloex.Any())
            {
                const double limitSup = Math.PI;
                const double smooth = -2.86;
                for (int c = 0; c < desc_length; c++)
                {
                    var g3r = limitSup * ((double)c / desc_length);
                    double sum = 0;
                    foreach (var aBondsInCycloex in bondsInCycloex)
                    {
                        int yaCounter = 0;
                        int position = aBondsInCycloex;
                        var theInCycloexBond = mol.Bonds[position];
                        var cycloexBondAtom0 = theInCycloexBond.Atoms[0];
                        var cycloexBondAtom1 = theInCycloexBond.Atoms[1];

                        var connAtoms = mol.GetConnectedAtoms(cycloexBondAtom0);
                        foreach (var connAtom in connAtoms)
                        {
                            if (connAtom.Equals(neighbour0))
                                yaCounter += 1;
                        }

                        Vector3 aA;
                        Vector3 aB;
                        if (yaCounter > 0)
                        {
                            aA = new Vector3(cycloexBondAtom1.Point3D.Value.X, 
                                             cycloexBondAtom1.Point3D.Value.Y,
                                             cycloexBondAtom1.Point3D.Value.Z);
                            aB = new Vector3(cycloexBondAtom0.Point3D.Value.X, 
                                             cycloexBondAtom0.Point3D.Value.Y,
                                             cycloexBondAtom0.Point3D.Value.Z);
                        }
                        else
                        {
                            aA = new Vector3(cycloexBondAtom0.Point3D.Value.X, 
                                             cycloexBondAtom0.Point3D.Value.Y,
                                             cycloexBondAtom0.Point3D.Value.Z);
                            aB = new Vector3(cycloexBondAtom1.Point3D.Value.X, 
                                             cycloexBondAtom1.Point3D.Value.Y,
                                             cycloexBondAtom1.Point3D.Value.Z);
                        }
                        var bA = new Vector3(neighbour0.Point3D.Value.X,
                                         neighbour0.Point3D.Value.Y, 
                                         neighbour0.Point3D.Value.Z);
                        var bB = new Vector3(atom.Point3D.Value.X, 
                                         atom.Point3D.Value.Y,
                                         atom.Point3D.Value.Z);
                        var angle = CalculateAngleBetweenTwoLines(aA, aB, bA, bB);
                        var partial = Math.Exp(smooth * Math.Pow(g3r - angle, 2));
                        sum += partial;
                    }
                    rdfProtonCalculatedValues.Add(sum);
                    Debug.WriteLine($"RDF g3r prob.: {sum} at distance {g3r}");
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
