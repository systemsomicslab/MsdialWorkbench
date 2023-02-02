/* Copyright (C) 2004-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
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

using NCDK.Config;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class returns the covalent radius of a given atom.
    /// </summary>
    // @author         Miguel Rojas
    // @cdk.created    2006-05-17
    // @cdk.module     qsaratomic
    // @cdk.dictref qsar-descriptors:covalentradius
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#covalentradius")]
    public partial class CovalentRadiusDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;

        public CovalentRadiusDescriptor(IAtomContainer container)
        {
            this.container = container;
        }

        public AtomTypeFactory AtomTypeFactory { get; set; } = CDK.JmolAtomTypeFactory;

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.CovalentRadius = value;
            }

            [DescriptorResultProperty("covalentRadius")]
            public double CovalentRadius { get; private set; }

            public double Value => CovalentRadius;
        }

        /// <summary>
        /// This method calculates the Covalent radius of an atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>The Covalent radius of the atom</returns>
        public Result Calculate(IAtom atom)
        {
            var symbol = atom.Symbol;
            var type = this.AtomTypeFactory.GetAtomType(symbol);
            var covalentRadius = type.CovalentRadius.Value;
            return new Result(covalentRadius);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
