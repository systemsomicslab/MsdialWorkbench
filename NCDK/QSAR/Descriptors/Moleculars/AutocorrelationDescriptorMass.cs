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

using NCDK.Graphs.Matrix;
using NCDK.Tools.Manipulator;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// This class calculates ATS autocorrelation descriptor, where the weight equal
    /// to the scaled atomic mass <token>cdk-cite-Moreau1980</token>.
    /// </summary>
    // @author      Federico
    // @cdk.created 2007-02-08
    // @cdk.module  qsarmolecular
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#autoCorrelationMass")]
    public class AutocorrelationDescriptorMass : AbstractDescriptor, IMolecularDescriptor
    {
        private const int DefaultSize = 5;
        private const double CarbonMass = 12.010735896788;

        public AutocorrelationDescriptorMass()
        {
        }

        [DescriptorResult(prefix: "ATSm", baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(IReadOnlyList<double> values)
                : base(values)
            {
            }
        }

        private static double ScaledAtomicMasses(ChemicalElement element)
        {
            var isofac = CDK.IsotopeFactory;
            var realmasses = isofac.GetNaturalMass(element);
            return (double)realmasses / (double)CarbonMass;
        }

        private static double[] ListConvertion(IAtomContainer container)
        {
            var natom = container.Atoms.Count;
            var scalated = new double[natom];

            for (int i = 0; i < natom; i++)
                scalated[i] = ScaledAtomicMasses(container.Atoms[i].Element);
            return scalated;
        }

        /// <summary>
        /// This method calculate the ATS Autocorrelation descriptor.
        /// </summary>
        public Result Calculate(IAtomContainer container, int count = DefaultSize)
        {
            container = AtomContainerManipulator.RemoveHydrogens(container);

            var w = ListConvertion(container);
            var natom = container.Atoms.Count;
            var distancematrix = TopologicalMatrix.GetMatrix(container);
            var masSum = new double[count];
            for (int k = 0; k < count; k++)
            { 
                for (int i = 0; i < natom; i++)
                    for (int j = 0; j < natom; j++)
                        if (distancematrix[i][j] == k)
                            masSum[k] += w[i] * w[j];
                        else
                            masSum[k] += 0;
                if (k > 0)
                    masSum[k] = masSum[k] / 2;
            }

            return new Result(masSum);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
