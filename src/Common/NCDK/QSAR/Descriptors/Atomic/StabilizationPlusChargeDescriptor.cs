/* Copyright (C) 2008  Miguel Rojas <miguelrojasch@yahoo.es>
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

using NCDK.Charges;
using NCDK.Tools.Manipulator;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// The stabilization of the positive charge
    /// (e.g.) obtained in the polar breaking of a bond is calculated from the sigma- and
    /// lone pair-electronegativity values of the atoms that are in conjugation to the atoms
    /// obtaining the charges. The method is based following <token>cdk-cite-Saller85</token>.
    /// The value is calculated looking for resonance structures which can stabilize the charge.
    /// </summary>
    /// <seealso cref="StabilizationCharges"/>
    // @author         Miguel Rojas Cherto
    // @cdk.created    2008-104-31
    // @cdk.module     qsaratomic
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#stabilizationPlusCharge")]
    public partial class StabilizationPlusChargeDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;
        IAtomContainer clonedContainer = null;

        public StabilizationPlusChargeDescriptor(IAtomContainer container)
        {
            this.clonedContainer = (IAtomContainer)container.Clone();
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(this.clonedContainer);

            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.StabilizationPlusCharge = value;
            }

            [DescriptorResultProperty("stabilPlusC")]
            public double StabilizationPlusCharge { get; private set; }

            public double Value => StabilizationPlusCharge;
        }

        /// <summary>
        /// The method calculates the stabilization of charge of a given atom
        /// It is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// </summary>
        /// <param name="atom">The atom requested</param>
        /// <returns>return the stabilization value</returns>
        public Result Calculate(IAtom atom)
        {
            var index = container.Atoms.IndexOf(atom);
            var localAtom = clonedContainer.Atoms[index];
            var value = StabilizationCharges.CalculatePositive(clonedContainer, localAtom);

            return new Result(value);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
