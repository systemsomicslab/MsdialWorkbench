/* Copyright (C) 2007  Federico
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

using NCDK.Aromaticities;
using NCDK.Charges;
using NCDK.Graphs;
using NCDK.Graphs.Matrix;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// This class calculates ATS autocorrelation descriptor, where the weight equal to the charges.
    /// </summary>
    // @author Federico
    // @cdk.created 2007-03-01
    // @cdk.module qsarmolecular
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#autoCorrelationPolarizability")]
    public class AutocorrelationDescriptorPolarizability : AbstractDescriptor, IMolecularDescriptor
    {
        private const int DefaultSize = 5;

        public AutocorrelationDescriptorPolarizability()
        {
        }

        [DescriptorResult(prefix: "ATSp", baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(IReadOnlyList<double> values)
                : base(values)
            {
            }
        }

        private static double[] Listpolarizability(IAtomContainer container, int[][] dmat)
        {
            int natom = container.Atoms.Count;
            double[] polars = new double[natom];

            for (int i = 0; i < natom; i++)
            {
                IAtom atom = container.Atoms[i];
                try
                {
                    polars[i] = Polarizability.CalculateGHEffectiveAtomPolarizability(container, atom, false, dmat);
                }
                catch (Exception ex1)
                {
                    throw new CDKException("Problems with assign Polarizability due to " + ex1.ToString(), ex1);
                }
            }

            return polars;
        }

        /// <summary>
        /// This method calculate the ATS Autocorrelation descriptor.
        /// </summary>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();

            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
            var hAdder = CDK.HydrogenAdder;
            hAdder.AddImplicitHydrogens(container);
            AtomContainerManipulator.ConvertImplicitToExplicitHydrogens(container);
            Aromaticity.CDKLegacy.Apply(container);

            // get the distance matrix for pol calcs as well as for later on
            var distancematrix = PathTools.ComputeFloydAPSP(AdjacencyMatrix.GetMatrix(container));

            var w = Listpolarizability(container, distancematrix);
            var natom = container.Atoms.Count;
            var polarizabilitySum = new double[5];

            for (int k = 0; k < 5; k++)
            {
                for (int i = 0; i < natom; i++)
                {
                    if (container.Atoms[i].AtomicNumber.Equals(AtomicNumbers.H))
                        continue;
                    for (int j = 0; j < natom; j++)
                    {
                        if (container.Atoms[j].AtomicNumber.Equals(AtomicNumbers.H))
                            continue;
                        if (distancematrix[i][j] == k)
                            polarizabilitySum[k] += w[i] * w[j];
                        else
                            polarizabilitySum[k] += 0.0;
                    }
                }
                if (k > 0)
                    polarizabilitySum[k] = polarizabilitySum[k] / 2;
            }
            return new Result(polarizabilitySum);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
