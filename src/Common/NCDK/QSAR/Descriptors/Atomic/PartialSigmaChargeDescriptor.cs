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

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// The calculation of sigma partial charges in sigma-bonded systems of an heavy atom
    /// was made by Marsilli-Gasteiger. It is implemented with the Partial Equalization
    /// of Orbital Electronegativity (PEOE).
    /// </summary>
    /// <seealso cref="GasteigerMarsiliPartialCharges"/>
    // @author      Miguel Rojas
    // @cdk.created 2006-04-15
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:partialSigmaCharge
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#partialSigmaCharge")]
    public partial class PartialSigmaChargeDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;
        IAtomContainer clonedContainer;

        /// <param name="maxIterations">Number of maximum iterations</param>
        public PartialSigmaChargeDescriptor(IAtomContainer container, int maxIterations = int.MaxValue)
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
                this.PiSigmaCharge = value;
            }

            [DescriptorResultProperty("partialSigmaCharge")]
            public double PiSigmaCharge { get; private set; }

            public double Value => PiSigmaCharge;
        }

        /// <summary>
        /// The method returns apha partial charges assigned to an heavy atom through Gasteiger Marsili
        /// It is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// For this method will be only possible if the heavy atom has single bond.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>Value of the alpha partial charge</returns>
        public Result Calculate(IAtom atom)
        {
            var index = container.Atoms.IndexOf(atom);
            return new Result(clonedContainer.Atoms[index].Charge.Value);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
