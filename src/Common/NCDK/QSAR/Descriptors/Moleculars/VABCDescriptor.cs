/* Copyright (C) 2011  Egon Willighagen <egon.willighagen@gmail.com>
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

using NCDK.Geometries.Volume;
using System;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Volume descriptor using the method implemented in the <see cref="VABCVolume"/> class.
    /// </summary>
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:vabc
    // @cdk.keyword volume
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#vabc")]
    public class VABCDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public VABCDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.VABC = value;
            }

            public Result(Exception e) : base(e) { }

            [DescriptorResultProperty("VABC")]
            public double VABC { get; private set; }

            public double Value => VABC;
        }

        /// <summary>
        /// Calculates the descriptor value using the <see cref="VABCVolume"/> class.
        /// </summary>
        /// <returns>A double containing the volume</returns>
        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone(); // don't mod original

            try
            {
                return new Result(VABCVolume.Calculate(container));
            }
            catch (CDKException e)
            {
                return new Result(e);
            }
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
