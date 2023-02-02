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
using System;

namespace NCDK.QSAR.Descriptors.Bonds
{
    /// <summary>
    ///  The calculation of bond-sigma Partial charge is calculated
    ///  determining the difference the Partial Sigma Charge on atoms
    ///  A and B of a bond. Based in Gasteiger Charge.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///  This descriptor uses these parameters:
    /// <list type="table">
    /// <listheader><term>Name</term><term>Default</term><term>Description</term></listheader>
    /// <item><term>bondPosition</term><term>0</term><term>The position of the target bond</term></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <seealso cref="Atomic.PartialSigmaChargeDescriptor"/>
    // @author      Miguel Rojas
    // @cdk.created 2006-05-08
    // @cdk.module  qsarbond
    // @cdk.dictref qsar-descriptors:bondPartialSigmaCharge
    [DescriptorSpecification(DescriptorTargets.Bond, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#bondPartialSigmaCharge")]
    public partial class BondPartialSigmaChargeDescriptor : AbstractDescriptor, IBondDescriptor
    {
        private readonly IAtomContainer container;
        private readonly IAtomContainer clonedContainer;

        /// <param name="maxIterations">Number of maximum iterations</param>
        public BondPartialSigmaChargeDescriptor(IAtomContainer container, int maxIterations = int.MaxValue)
        {
            clonedContainer = (IAtomContainer)container.Clone();

            var peoe = new GasteigerMarsiliPartialCharges();
            if (maxIterations != int.MaxValue)
                peoe.MaxGasteigerIterations = maxIterations;
            peoe.AssignGasteigerMarsiliSigmaPartialCharges(clonedContainer, true);

            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.Value = value;
            }

            [DescriptorResultProperty("peoeB")]
            public double Value { get; private set; }
        }

        /// <summary>
        /// The method calculates the bond-sigma Partial charge of a given bond
        /// it is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// </summary>
        /// <returns>return the sigma electronegativity</returns>
        public Result Calculate(IBond bond)
        {
            if (bond.Atoms.Count != 2)
                throw new CDKException("Only 2-center bonds are considered");

            bond = clonedContainer.Bonds[container.Bonds.IndexOf(bond)];

            return new Result(Math.Abs(bond.Begin.Charge.Value - bond.End.Charge.Value));
        }

        IDescriptorResult IBondDescriptor.Calculate(IBond bond) => Calculate(bond);
    }
}
