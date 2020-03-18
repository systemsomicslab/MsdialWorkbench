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
using System.Linq;

namespace NCDK.QSAR.Descriptors.Bonds
{
    /// <summary>
    /// The calculation of bond total Partial charge is calculated
    /// determining the difference the Partial Total Charge on atoms
    /// A and B of a bond. Based in Gasteiger Charge.
    /// </summary>
    /// <seealso cref="Atomic.PartialPiChargeDescriptor"/>
    /// <seealso cref="Atomic.PartialSigmaChargeDescriptor"/>
    // @author      Miguel Rojas
    // @cdk.created 2006-05-18
    // @cdk.module  qsarbond
    // @cdk.dictref qsar-descriptors:bondPartialTCharge
    [DescriptorSpecification(DescriptorTargets.Bond, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#bondPartialTCharge")]
    public partial class BondPartialTChargeDescriptor : AbstractDescriptor, IBondDescriptor
    {
        private readonly IAtomContainer container;
        private readonly IAtomContainer clonedContainer;

        /// <param name="maxIterations">Number of maximum iterations</param>
        /// <param name="checkLonePairElectron">Checking lone pair electrons. Default <see langword="true"/></param>
        /// <param name="maxResonanceStructures">Number of maximum resonance structures to be searched</param>
        public BondPartialTChargeDescriptor(IAtomContainer container,
            int maxIterations = int.MaxValue,
            bool checkLonePairElectron = true,
            int maxResonanceStructures = int.MaxValue)
        {
            clonedContainer = (IAtomContainer)container.Clone();
            AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(clonedContainer);

            var pepe = new GasteigerPEPEPartialCharges();
            if (checkLonePairElectron)
                CDK.LonePairElectronChecker.Saturate(clonedContainer);
            if (maxIterations != int.MaxValue)
                pepe.MaxGasteigerIterations = maxIterations;
            if (maxResonanceStructures != int.MaxValue)
                pepe.MaxResonanceStructures = maxResonanceStructures;

            var peoe = new GasteigerMarsiliPartialCharges();
            peoe.AssignGasteigerMarsiliSigmaPartialCharges(clonedContainer, true);
            var peoeAtom = clonedContainer.Atoms.Select(n => n.Charge.Value).ToList();

            foreach (var aatom in clonedContainer.Atoms)
                aatom.Charge = 0;
            pepe.AssignGasteigerPiPartialCharges(clonedContainer, true);

            for (int i = 0; i < clonedContainer.Atoms.Count; i++)
            {
                var aatom = clonedContainer.Atoms[i];
                aatom.Charge = aatom.Charge.Value + peoeAtom[i];
            }

            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.Value = value;
            }

            [DescriptorResultProperty("pCB")]
            public double Value { get; private set; }
        }

        /// <summary>
        /// The method calculates the bond total Partial charge of a given bond
        /// It is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
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
