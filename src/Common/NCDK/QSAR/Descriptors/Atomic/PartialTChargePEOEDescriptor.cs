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
using System.Linq;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// The calculation of total partial charges of an heavy atom is based on
    /// Partial Equalization of Electronegativity method (PEOE-PEPE) from Gasteiger. 
    /// <para>They are obtained by summation of the results of the calculations on
    /// sigma- and pi-charges. </para>
    /// </summary>
    /// <seealso cref="GasteigerMarsiliPartialCharges"/>
    /// <seealso cref="GasteigerPEPEPartialCharges"/>
    // @author      Miguel Rojas
    // @cdk.created 2006-04-11
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:PartialTChargePEOE
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#PartialTChargePEOE")]
    public partial class PartialTChargePEOEDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;
        IAtomContainer clonedContainer;

        /// <param name="maxIterations">Number of maximum iterations</param>
        /// <param name="checkLonePairElectron">Checking lone pair electrons. Default <see langword="true"/></param>
        /// <param name="maxResonanceStructures">Number of maximum resonance structures to be searched</param>
        public PartialTChargePEOEDescriptor(IAtomContainer container,
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
                this.TotalPartialCharge = value;
            }

            [DescriptorResultProperty("pepeT")]
            public double TotalPartialCharge { get; private set; }

            public double Value => TotalPartialCharge;
        }

        /// <summary>
        /// The method returns partial total charges assigned to an heavy atom through PEOE method.
        /// It is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>an array of doubles with partial charges of [heavy, proton_1 ... proton_n]</returns>
        public Result Calculate(IAtom atom)
        {
            var index = container.Atoms.IndexOf(atom);
            return new Result(clonedContainer.Atoms[index].Charge.Value);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
