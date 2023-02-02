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

using NCDK.Charges;
using NCDK.Graphs.Matrix;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// This class calculates ATS autocorrelation descriptor, where the weight equal
    /// to the charges.
    /// </summary>
    // @author      Federico
    // @cdk.created 2007-02-27
    // @cdk.module  qsarmolecular
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#autoCorrelationCharge")]
    public class AutocorrelationDescriptorCharge : AbstractDescriptor, IMolecularDescriptor
    {
        private const int DefaultSize = 5;

        public AutocorrelationDescriptorCharge()
        {
        }

        [DescriptorResult(prefix: "ATSc", baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(IReadOnlyList<double> values) : base(values) { }
            public Result(Exception e) : base(e) { }
        }

        private static double[] Listcharges(IAtomContainer container)
        {
            var natom = container.Atoms.Count;
            var charges = new double[natom];
            try
            {
                var mol = container.Builder.NewAtomContainer(((IAtomContainer)container.Clone()));
                var peoe = new GasteigerMarsiliPartialCharges();
                peoe.AssignGasteigerMarsiliSigmaPartialCharges(mol, true);
                for (int i = 0; i < natom; i++)
                {
                    var atom = mol.Atoms[i];
                    charges[i] = atom.Charge.Value;
                }
            }
            catch (Exception ex1)
            {
                throw new CDKException($"Problems with assigning Gasteiger-Marsili partial charges due to {ex1.Message}", ex1);
            }

            return charges;
        }

        public Result Calculate(IAtomContainer container, int count = DefaultSize)
        {
            container = AtomContainerManipulator.RemoveHydrogens(container);

            try
            {
                var w = Listcharges(container);
                var natom = container.Atoms.Count;
                var distancematrix = TopologicalMatrix.GetMatrix(container);

                var chargeSum = new double[count];

                for (int k = 0; k < count; k++)
                {
                    for (int i = 0; i < natom; i++)
                        for (int j = 0; j < natom; j++)
                            if (distancematrix[i][j] == k)
                                chargeSum[k] += w[i] * w[j];
                            else
                                chargeSum[k] += 0;
                    if (k > 0)
                        chargeSum[k] = chargeSum[k] / 2;
                }

                return new Result(chargeSum);
            }
            catch (CDKException e)
            {
                return new Result(e);
            }
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
