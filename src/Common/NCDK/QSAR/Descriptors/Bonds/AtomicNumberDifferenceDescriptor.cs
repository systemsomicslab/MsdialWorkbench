/* Copyright (C) 2007-2008  Egon Willighagen <egonw@users.sf.net>
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
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

using NCDK.Tools.Manipulator;
using System;

namespace NCDK.QSAR.Descriptors.Bonds
{
    /// <summary>
    /// Describes the imbalance in atomic number of the <see cref="IBond"/> .
    /// </summary>
    // @author      Egon Willighagen
    // @cdk.created 2007-12-29
    // @cdk.module  qsarbond
    // @cdk.dictref qsar-descriptors:bondAtomicNumberImbalance
    [DescriptorSpecification(DescriptorTargets.Bond, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#bondAtomicNumberImbalance")]
    public class AtomicNumberDifferenceDescriptor : AbstractDescriptor, IBondDescriptor
    {
        public AtomicNumberDifferenceDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.Value = value;
            }

            [DescriptorResultProperty("MNDiff")]
            public int Value { get; private set; }
        }

        public Result Calculate(IBond bond)
        {
            if (bond.Atoms.Count != 2)
                throw new CDKException("Only 2-center bonds are considered");

            var atoms = BondManipulator.GetAtomArray(bond);

            var factory = CDK.IsotopeFactory;

            return new Result(Math.Abs(bond.Begin.AtomicNumber - bond.End.AtomicNumber));
        }

        IDescriptorResult IBondDescriptor.Calculate(IBond bond) => Calculate(bond);
    }
}
