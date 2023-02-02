/*  Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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
    /// Atomic descriptor that reflects that Gasteiger-Marsili sigma electronegativity.
    /// The used approach is given by "X = a + bq + c(q*q)" where a, b, and c are
    /// the Gasteiger-Marsili parameters and q is the sigma charge. For the actual
    /// calculation it uses the <see cref="Electronegativity"/> class.
    /// </summary>
    /// <seealso cref="Electronegativity"/>
    // @author      mfe4
    // @cdk.created 2004-11-03
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:sigmaElectronegativity
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#sigmaElectronegativity")]
    public partial class SigmaElectronegativityDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        private readonly IAtomContainer container;
        private readonly IAtomContainer clonedContainer = null;
        private readonly Electronegativity electronegativity;

        /// <param name="maxIterations">Number of maximum iterations</param>
        public SigmaElectronegativityDescriptor(IAtomContainer container, int maxIterations = int.MaxValue)
        {
            this.clonedContainer = (IAtomContainer)container.Clone();
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(this.clonedContainer);
            electronegativity = new Electronegativity();
            if (maxIterations != int.MaxValue)
                electronegativity.MaxIterations = maxIterations;

            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.SigmaElectronegativity = value;
            }

            [DescriptorResultProperty("elecSigmA")]
            public double SigmaElectronegativity { get; private set; }

            public double Value => SigmaElectronegativity;
        }

        /// <summary>
        /// The method calculates the sigma electronegativity of a given atom
        /// It is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>return the sigma electronegativity</returns>
        public Result Calculate(IAtom atom)
        {
            var index = container.Atoms.IndexOf(atom);
            var localAtom = clonedContainer.Atoms[index];
            var value = electronegativity.CalculateSigmaElectronegativity(clonedContainer, localAtom);

            return new Result(value);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
