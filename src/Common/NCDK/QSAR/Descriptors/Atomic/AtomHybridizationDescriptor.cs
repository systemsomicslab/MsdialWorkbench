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
    /// This class returns the hybridization of an atom.
    /// </summary>
    // @author         mfe4
    // @cdk.created    2004-11-13
    // @cdk.module     qsaratomic
    // @cdk.dictref    qsar-descriptors:atomHybridization
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#atomHybridization")]
    public partial class AtomHybridizationDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        private IAtomContainer container;

        public AtomHybridizationDescriptor(IAtomContainer container)
        {
            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(Hybridization value)
            {
                this.AtomHybridization = value;
            }

            [DescriptorResultProperty("aHyb")]
            public Hybridization AtomHybridization { get; private set; }          

            public Hybridization Value => AtomHybridization;
        }

        /// <summary>
        /// This method calculates the hybridization of an atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>The hybridization</returns>
        public Result Calculate(IAtom atom)
        {
            var matched = CDK.AtomTypeMatcher.FindMatchingAtomType(container, atom);
            if (matched == null)
            {
                var atnum = container.Atoms.IndexOf(atom);
                throw new CDKException($"The matched atom type was null (atom number {atnum}) {atom.Symbol}");
            }
            var atomHybridization = matched.Hybridization;
            return new Result(atomHybridization);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
