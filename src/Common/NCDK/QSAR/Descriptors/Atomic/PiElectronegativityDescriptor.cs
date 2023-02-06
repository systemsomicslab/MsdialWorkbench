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

using NCDK.Charges;
using NCDK.Tools.Manipulator;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// Pi electronegativity is given by X = a + bq + c(q*q)
    /// </summary>
    /// <seealso cref="Electronegativity"/>
    // @author      Miguel Rojas
    // @cdk.created 2006-05-17
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:piElectronegativity
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#piElectronegativity")]
    public partial class PiElectronegativityDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;
        IAtomContainer clonedContainer = null;
        PiElectronegativity electronegativity;

        /// <param name="maxIterations">Number of maximum iterations</param>
        /// <param name="checkLonePairElectron">Checking lone pair electrons. Default <see langword="true"/></param>
        /// <param name="maxResonanceStructures">Number of maximum resonance structures to be searched</param>
        public PiElectronegativityDescriptor(IAtomContainer container,
            int maxIterations = int.MaxValue,
            bool checkLonePairElectron = true,
            int maxResonanceStructures = int.MaxValue
            )
        {
            clonedContainer = (IAtomContainer)container.Clone();
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(clonedContainer);
            if (checkLonePairElectron)
                CDK.LonePairElectronChecker.Saturate(clonedContainer);
            electronegativity = new PiElectronegativity();
            if (maxIterations != int.MaxValue)
                electronegativity.MaxIterations = maxIterations;
            if (maxResonanceStructures != int.MaxValue)
                electronegativity.MaxResonanceStructures = maxResonanceStructures;

            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.PiElectronegativity = value;
            }

            [DescriptorResultProperty("elecPiA")]
            public double PiElectronegativity { get; private set; }

            public double Value => PiElectronegativity;
        }

        /// <summary>
        /// The method calculates the pi electronegativity of a given atom
        /// It is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>return the pi electronegativity</returns>
        public Result Calculate(IAtom atom)
        {
            var index = container.Atoms.IndexOf(atom);
            var pe = electronegativity.CalculatePiElectronegativity(clonedContainer, clonedContainer.Atoms[index]);
            return new Result(pe);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
