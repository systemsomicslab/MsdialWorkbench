/* Copyright (C) 2009  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it under
 * the terms of the GNU Lesser General Public License as published by the Free
 * Software Foundation; either version 2.1 of the License, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public License for more
 * details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Prediction of logP based on the number of carbon and hetero atoms. The
    /// implemented equation was proposed in <token>cdk-cite-Mannhold2009</token>.
    /// </summary>
    // @cdk.module     qsarmolecular
    // @cdk.dictref    qsar-descriptors:mannholdLogP
    // @cdk.keyword LogP
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#mannholdLogP")]
    public class MannholdLogPDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public MannholdLogPDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.MLogP = value;
            }

            [DescriptorResultProperty("MLogP")]
            public double MLogP { get; private set; }

            public double Value => MLogP;
        }

        /// <summary>
        /// Calculates the Mannhold LogP for an atom container.
        /// </summary>
        /// <returns>A descriptor value wrapping a <see cref="System.Double"/>.</returns>
        public Result Calculate(IAtomContainer container)
        {
            int carbonCount = 0;
            int heteroCount = 0;
            foreach (var atom in container.Atoms)
            {
                switch (atom.AtomicNumber)
                {
                    case AtomicNumbers.H:
                        break;
                    case AtomicNumbers.C:
                        carbonCount++;
                        break;
                    default:
                        heteroCount++;
                        break;
                }
            }
            var mLogP = 1.46 + 0.11 * carbonCount - 0.11 * heteroCount;

            return new Result(mLogP);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
