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

using NCDK.Tools;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class return the VdW radius of a given atom.
    /// </summary>
    // @author         mfe4
    // @cdk.created    2004-11-13
    // @cdk.module     qsaratomic
    // @cdk.dictref qsar-descriptors:vdwradius
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#vdwradius")]
    public partial class VdWRadiusDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        public VdWRadiusDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.VdWRadius = value;
            }

            [DescriptorResultProperty("vdwRadius")]
            public double VdWRadius { get; private set; }

            public double Value => VdWRadius;
        }

        /// <summary>
        /// This method calculate the van der Waals radius of an atom.
        /// </summary>
        /// <returns>The Van der Waals radius of the atom</returns>
        public Result Calculate(IAtom atom)
        {
            var vdwradius = PeriodicTable.GetVdwRadius(atom.AtomicNumber);
            return new Result(vdwradius.Value);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
