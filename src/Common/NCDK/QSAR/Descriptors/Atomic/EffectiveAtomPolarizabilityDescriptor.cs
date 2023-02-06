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
    /// Effective polarizability of a heavy atom
    /// </summary>
    /// <seealso cref="Polarizability"/>
    // @author      Miguel Rojas
    // @cdk.created 2006-05-03
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:effectivePolarizability
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#effectivePolarizability")]
    public partial class EffectiveAtomPolarizabilityDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;

        public EffectiveAtomPolarizabilityDescriptor(IAtomContainer container)
        {
            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.Polarizability = value;
            }

            [DescriptorResultProperty("effAtomPol")]
            public double Polarizability { get; private set; }

            public double Value => Polarizability;
        }

        /// <summary>
        /// The method calculates the Effective Atom Polarizability of a given atom
        /// It is needed to call the addExplicitHydrogensToSatisfyValency method from the class tools.HydrogenAdder.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>return the effective polarizability</returns>
        public Result Calculate(IAtom atom)
        {
            // FIXME: for now I'll cache a few modified atomic properties, and restore them at the end of this method
            var originalAtomtypeName = atom.AtomTypeName;
            var originalNeighborCount = atom.FormalNeighbourCount;
            var originalHCount = atom.ImplicitHydrogenCount;
            var originalValency = atom.Valency;
            var originalHybridization = atom.Hybridization;
            var originalFlag = atom.IsVisited;
            var originalBondOrderSum = atom.BondOrderSum;
            var originalMaxBondOrder = atom.MaxBondOrder;
            var polarizability = Polarizability.CalculateGHEffectiveAtomPolarizability(container, atom, 100, true);
            // restore original props
            atom.AtomTypeName = originalAtomtypeName;
            atom.FormalNeighbourCount = originalNeighborCount;
            atom.Valency = originalValency;
            atom.ImplicitHydrogenCount = originalHCount;
            atom.IsVisited = originalFlag;
            atom.Hybridization = originalHybridization;
            atom.MaxBondOrder = originalMaxBondOrder;
            atom.BondOrderSum = originalBondOrderSum;
            return new Result(polarizability);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
