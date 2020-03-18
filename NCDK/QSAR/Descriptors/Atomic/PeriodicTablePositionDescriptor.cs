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

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class returns the period in the periodic table of an atom belonging to an atom container
    /// </summary>
    // @author         mfe4
    // @cdk.created    2004-11-13
    // @cdk.module     qsaratomic
    // @cdk.dictref qsar-descriptors:period
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#period")]
    public partial class PeriodicTablePositionDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        public PeriodicTablePositionDescriptor()
        {
        }

        /// <param name="container">Ignore this</param>
        public PeriodicTablePositionDescriptor(IAtomContainer container)
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.PeriodicTablePosition = value;
            }

            [DescriptorResultProperty("periodicTablePosition")]
            public int PeriodicTablePosition { get; private set; }

            public int Value => PeriodicTablePosition;
        }

        /// <summary>
        /// This method calculates the period of an atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>The period</returns>
        public Result Calculate(IAtom atom)
        {
            return new Result(Tools.PeriodicTable.GetPeriod(atom.AtomicNumber));
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
