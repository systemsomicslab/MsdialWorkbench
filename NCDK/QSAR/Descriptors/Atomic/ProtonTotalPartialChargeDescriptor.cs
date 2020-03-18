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

using NCDK.Charges;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// The calculation of partial charges of an heavy atom and its protons is based on Gasteiger Marsili (PEOE).
    /// </summary>
    /// <remarks>
    /// The result of this descriptor is a vector of up to 5 values, corresponding
    /// to a maximum of four protons for any given atom. If an atom has fewer than four protons, the remaining values
    /// are set to double.NaN. Also note that the values for the neighbors are not returned in a particular order
    /// (though the order is fixed for multiple runs for the same atom).
    /// </remarks>
    // @author mfe4
    // @cdk.created 2004-11-03
    // @cdk.module qsaratomic
    // @cdk.dictref qsar-descriptors:protonPartialCharge
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#protonPartialCharge")]
    public partial class ProtonTotalPartialChargeDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;
        IAtomContainer clonedContainer;

        public ProtonTotalPartialChargeDescriptor(IAtomContainer container)
        {
            clonedContainer = (IAtomContainer)container.Clone();
            var peoe = new GasteigerMarsiliPartialCharges { MaxGasteigerIterations = 6 };
            peoe.AssignGasteigerMarsiliSigmaPartialCharges(this.clonedContainer, true);

            this.container = container;
        }

        [DescriptorResult(prefix: "protonTotalPartialCharge", baseIndex: 1)]
        public class Result : AbstractDescriptorArrayResult<double>
        {
            public Result(IReadOnlyList<double> values)
                : base(values)
            {
            }
        }

        /// <summary>
        /// The method returns partial charges assigned to an heavy atom and its protons through Gasteiger Marsili
        /// It is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>an array of doubles with partial charges of [heavy, proton_1 ... proton_n]</returns>
        public Result Calculate(IAtom atom)
        {
            var localAtom = clonedContainer.Atoms[container.Atoms.IndexOf(atom)];
            var neighboors = clonedContainer.GetConnectedAtoms(localAtom).ToReadOnlyList();

            var protonPartialCharge = new List<double>
            {
                localAtom.Charge.Value
            };
            int hydrogenNeighbors = 0;
            foreach (var neighboor in neighboors)
            {
                if (neighboor.AtomicNumber.Equals(AtomicNumbers.H))
                {
                    hydrogenNeighbors++;
                    protonPartialCharge.Add(neighboor.Charge.Value);
                }
            }

            return new Result(protonPartialCharge);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
