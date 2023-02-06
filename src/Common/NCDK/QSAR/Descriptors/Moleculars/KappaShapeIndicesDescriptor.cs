/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
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

using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Kier and Hall kappa molecular shape indices compare the molecular graph with minimal and maximal molecular graphs;
    /// a description is given at: http://www.chemcomp.com/Journal_of_CCG/Features/descr.htm#KH :
    /// "they are intended to capture different aspects of molecular shape.  Note that hydrogens are ignored.
    /// In the following description, n denotes the number of atoms in the hydrogen suppressed graph,
    /// m is the number of bonds in the hydrogen suppressed graph. Also, let p2 denote the number of paths of length 2
    /// and let p3 denote the number of paths of length 3".
    /// </summary>
    /// <remarks>
    /// Returns three values in the order
    /// <list type="bullet"> 
    /// <item>Kier1 - First kappa shape index</item>
    /// <item>Kier2 - Second kappa shape index</item>
    /// <item>Kier3 - Third kappa (É») shape index</item>
    /// </list>
    /// <para>This descriptor does not have any parameters.</para>
    /// </remarks>
    // @author mfe4
    // @cdk.created 2004-11-03
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:kierValues
    // @cdk.keyword Kappe shape index
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#kierValues")]
    public class KappaShapeIndicesDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public KappaShapeIndicesDescriptor()
        {
        }

        [DescriptorResult(prefix: "Kier", baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(IReadOnlyList<double> values)
                : base(values)
            {
            }

            [DescriptorResultProperty]
            public double Kier1 => Values[0];

            [DescriptorResultProperty]
            public double Kier2 => Values[1];

            [DescriptorResultProperty]
            public double Kier3 => Values[2];
        }

        /// <summary>
        /// Calculates the kier shape indices for an atom container
        /// </summary>
        /// <returns>kier1, kier2 and kier3 are returned as arrayList of doubles</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();
            container = AtomContainerManipulator.RemoveHydrogens(container);

            var singlePaths = new List<double>();
            var doublePaths = new List<string>();
            var triplePaths = new List<string>();

            foreach (var atom1 in container.Atoms)
            {
                var firstAtomNeighboors = container.GetConnectedAtoms(atom1);
                foreach (var firstAtomNeighboor in firstAtomNeighboors)
                {
                    var bond1 = container.Bonds.IndexOf(container.GetBond(atom1, firstAtomNeighboor));
                    if (!singlePaths.Contains(bond1))
                    {
                        singlePaths.Add(bond1);
                        singlePaths.Sort();
                    }

                    var secondAtomNeighboors = container.GetConnectedAtoms(firstAtomNeighboor);
                    foreach (var secondAtomNeighboor in secondAtomNeighboors)
                    {
                        var bond2 = container.Bonds.IndexOf(container.GetBond(firstAtomNeighboor, secondAtomNeighboor));
                        if (!singlePaths.Contains(bond2))
                        {
                            singlePaths.Add(bond2);
                        }
                        var sorterFirst = new double[] { bond1, bond2 };
                        Array.Sort(sorterFirst);

                        var tmpbond2 = sorterFirst[0] + "+" + sorterFirst[1];

                        if (!doublePaths.Contains(tmpbond2) && (bond1 != bond2))
                        {
                            doublePaths.Add(tmpbond2);
                        }

                        var thirdAtomNeighboors = container.GetConnectedAtoms(secondAtomNeighboor);
                        foreach (var thirdAtomNeighboor in thirdAtomNeighboors)
                        {
                            var bond3 = container.Bonds.IndexOf(container.GetBond(secondAtomNeighboor, thirdAtomNeighboor));
                            if (!singlePaths.Contains(bond3))
                            {
                                singlePaths.Add(bond3);
                            }
                            var sorterSecond = new double[] { bond1, bond2, bond3 };
                            Array.Sort(sorterSecond);

                            var tmpbond3 = sorterSecond[0] + "+" + sorterSecond[1] + "+" + sorterSecond[2];
                            if (!triplePaths.Contains(tmpbond3))
                            {
                                if ((bond1 != bond2) && (bond1 != bond3) && (bond2 != bond3))
                                {
                                    triplePaths.Add(tmpbond3);
                                }
                            }
                        }
                    }
                }
            }

            var kier = new double[] { 0, 0, 0, };
            do
            {
                if (container.Atoms.Count == 1)
                    break;
                kier[0] =
                      (double)(container.Atoms.Count * (container.Atoms.Count - 1) * (container.Atoms.Count - 1)) / (singlePaths.Count * singlePaths.Count);
                if (container.Atoms.Count == 2)
                    break;
                kier[1] = doublePaths.Count == 0
                    ? double.NaN
                    : (double)((container.Atoms.Count - 1) * (container.Atoms.Count - 2) * (container.Atoms.Count - 2)) / (doublePaths.Count * doublePaths.Count);
                if (container.Atoms.Count == 3)
                    break;
                kier[2] = triplePaths.Count == 0
                    ? double.NaN
                    : (
                        container.Atoms.Count % 2 != 0
                        ? (double)((container.Atoms.Count - 1) * (container.Atoms.Count - 3) * (container.Atoms.Count - 3)) / (triplePaths.Count * triplePaths.Count)
                        : (double)((container.Atoms.Count - 3) * (container.Atoms.Count - 2) * (container.Atoms.Count - 2)) / (triplePaths.Count * triplePaths.Count)
                      );
            } while (false);

            return new Result(kier);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
