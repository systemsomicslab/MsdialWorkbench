/*
 *  Copyright (C) 2010 Rajarshi Guha <rajarshi.guha@gmail.com>
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

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// <see cref="IMolecularDescriptor"/> that reports the fraction of sp3 carbons to sp2 carbons.
    /// </summary>
    /// <remarks>
    /// Note that it only considers carbon atoms and rather than use a simple ratio
    /// it reports the value of N<sub>sp3</sub>/ (N<sub>sp3</sub> + N<sub>sp2</sub>).
    /// The original form of the descriptor (i.e., simple ratio) has been used to
    /// characterize molecular complexity, especially in the are of natural products
    /// , which usually have a high value of the sp3 to sp2 ratio.
    /// </remarks>
    // @author Rajarshi Guha
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:hybratio
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#hybratio")]
    public class HybridizationRatioDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public HybridizationRatioDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.HybridizationRatio = value;
            }

            [DescriptorResultProperty("HybRatio")]
            public double HybridizationRatio { get; private set; }

            public double Value => HybridizationRatio;
        }

        /// <summary>
        /// Calculate sp3/sp2 hybridization ratio in the supplied <see cref="IAtomContainer"/>.
        /// </summary>
        /// <returns>The ratio of sp3 to sp2 carbons</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone();
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);

            int nsp2 = 0;
            int nsp3 = 0;
            foreach (var atom in container.Atoms)
            {
                if (!atom.AtomicNumber.Equals(AtomicNumbers.C))
                    continue;
                switch (atom.Hybridization)
                {
                    case Hybridization.SP2:
                        nsp2++;
                        break;
                    case Hybridization.SP3:
                        nsp3++;
                        break;
                }
            }
            double ratio = nsp3 / (double)(nsp2 + nsp3);
            return new Result(ratio);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
