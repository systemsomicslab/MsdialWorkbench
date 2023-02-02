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
using System;

namespace NCDK.QSAR.Descriptors.Bonds
{
    /// <summary>
    /// The calculation of bond-Polarizability is calculated determining the
    /// difference the Sigma electronegativity on atoms A and B of a bond.
    /// </summary>
    /// <seealso cref="Electronegativity"/>
    // @author      Miguel Rojas
    // @cdk.created 2006-05-08
    // @cdk.module  qsarbond
    // @cdk.dictref qsar-descriptors:bondSigmaElectronegativity
    [DescriptorSpecification(DescriptorTargets.Bond, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#bondSigmaElectronegativity")]
    public partial class BondSigmaElectronegativityDescriptor : AbstractDescriptor, IBondDescriptor
    {
        private readonly IAtomContainer container;
        private readonly IAtomContainer clonedContainer = null;
        private readonly Electronegativity electronegativity;

        /// <param name="maxIterations">Number of maximum iterations</param>
        public BondSigmaElectronegativityDescriptor(IAtomContainer container, int maxIterations = 6)
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
                this.Value = value;
            }

            [DescriptorResultProperty("elecSigB")]
            public double Value { get; private set; }
        }

        /// <summary>
        /// The method calculates the sigma electronegativity of a given bond
        ///  t is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// </summary>
        /// <returns>return the sigma electronegativity</returns>
        public Result Calculate(IBond bond)
        {
            var electroAtom1 = electronegativity.CalculateSigmaElectronegativity(clonedContainer, clonedContainer.Bonds[container.Bonds.IndexOf(bond)].Begin);
            var electroAtom2 = electronegativity.CalculateSigmaElectronegativity(clonedContainer, clonedContainer.Bonds[container.Bonds.IndexOf(bond)].End);

            return new Result(Math.Abs(electroAtom1 - electroAtom2));
        }

        IDescriptorResult IBondDescriptor.Calculate(IBond bond) => Calculate(bond);
    }
}
