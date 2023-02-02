/* Copyright (C) 2011-2015  Egon Willighagen <egonw@users.sf.net>
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
 * 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Substances
{
    /// <summary>
    /// Descriptor that returns the number of oxygens in the chemical
    /// formula. Originally aimed at metal oxide nanoparticles <token>cdk-cite-Liu2011</token>.
    /// </summary>
    // @author      egonw
    [DescriptorSpecification(DescriptorTargets.Substance, "http://egonw.github.com/resource/NM_001002")]
    public class OxygenAtomCountDescriptor : AbstractDescriptor, ISubstanceDescriptor
    {
        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.NumberOfOxygenAtom = value;
            }

            [DescriptorResultProperty("NoMe")]
            public int NumberOfOxygenAtom { get; private set; }

            public int Value => NumberOfOxygenAtom;
        }

        /// <inheritdoc/>
        public Result Calculate(IEnumerable<IAtomContainer> substance)
        {
            int count = 0;
            if (substance != null)
                foreach (var container in substance)
                    foreach (var atom in container.Atoms)
                        if (atom.AtomicNumber == AtomicNumbers.Oxygen)
                            count++;

            return new Result(count);
        }

        /// <inheritdoc/>
        IDescriptorResult ISubstanceDescriptor.Calculate(IEnumerable<IAtomContainer> substance) => Calculate(substance);
    }
}
