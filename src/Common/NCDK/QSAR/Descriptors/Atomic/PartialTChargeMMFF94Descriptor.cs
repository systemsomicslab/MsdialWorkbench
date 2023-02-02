/* Copyright (C) 2006-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.ForceFields;
using System.Diagnostics;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// The calculation of total partial charges of an heavy atom is based on MMFF94 model.
    /// </summary>
    /// <seealso cref="Charges.MMFF94PartialCharges"/>
    // @author Miguel Rojas
    // @cdk.created 2006-04-11
    // @cdk.module qsaratomic
    // @cdk.dictref qsar-descriptors:partialTChargeMMFF94
    // @cdk.bug 1628461
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#partialTChargeMMFF94")]
    public partial class PartialTChargeMMFF94Descriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;
        IAtomContainer clonedContainer = null;

        public PartialTChargeMMFF94Descriptor(IAtomContainer container)
        {
            foreach (var atom in container.Atoms)
            {
                if (atom.ImplicitHydrogenCount == null || atom.ImplicitHydrogenCount != 0)
                    throw new CDKException("Hydrogens must be explicit for MMFF charge calculation");
            }

            clonedContainer = (IAtomContainer)container.Clone();
            var mmff = new Mmff();
            if (!mmff.AssignAtomTypes(clonedContainer))
                Trace.TraceWarning("One or more atoms could not be assigned an MMFF atom type");
            mmff.PartialCharges(clonedContainer);
            mmff.ClearProps(clonedContainer);

            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.TotalPartialCharge = value;
            }

            [DescriptorResultProperty("partialTCMMFF94")]
            public double TotalPartialCharge { get; private set; }

            public double Value => TotalPartialCharge;
        }

        /// <summary>
        /// The method returns partial charges assigned to an heavy atom through
        /// MMFF94 method. It is needed to call the addExplicitHydrogensToSatisfyValency
        /// method from the class tools.HydrogenAdder.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>partial charge of parameter atom</returns>
        public Result Calculate(IAtom atom)
        {
            var index = container.Atoms.IndexOf(atom);
            return new Result(clonedContainer.Atoms[index].Charge.Value);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
