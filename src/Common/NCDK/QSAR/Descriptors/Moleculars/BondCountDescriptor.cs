/* Copyright (C) 2004-2007  Matteo Floris <mfe4@users.sf.net>
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

using System;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Descriptor based on the number of bonds of a certain bond order.
    /// </summary>
    /// <remarks>
    /// Note that the descriptor does not consider bonds to H's.
    /// </remarks>
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:bondCount
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#bondCount")]
    public class BondCountDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public BondCountDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorSingleResult<int>
        {
            public Result(int value, BondOrder order)
                : base(BondOrderToKey(order), value)
            {
            }

            private static string BondOrderToKey(BondOrder order)
            {
                switch (order)
                {
                    case BondOrder.Unset:
                        return "nB";
                    case BondOrder.Single:
                        return "nBs";
                    case BondOrder.Double:
                        return "nBd";
                    case BondOrder.Triple:
                        return "nBt";
                    case BondOrder.Quadruple:
                        return "nBq";
                    default:
                        throw new ArgumentException(nameof(order), "The only allowed bond types are single, double, truple, and quadruple bonds.");
                }
            }
        }

        /// <summary>
        /// Calculate the number of bonds of a given type in an atomContainer
        /// </summary>
        /// <param name="order">The bond order. Default is <see cref="BondOrder.Unset"/>, which means count all bonds.</param>
        /// <returns>The number of bonds of a certain type.</returns>
        public Result Calculate(IAtomContainer container, BondOrder order = BondOrder.Unset)
        {
            if (order.IsUnset())
            {
                var count = container.Bonds
                    .Select(bond => bond.Atoms
                        .Count(atom => atom.AtomicNumber.Equals(AtomicNumbers.H)))
                    .Sum();
                return new Result(count, order);
            }
            else
            {
                var count = container.Bonds.Count(bond => bond.Order.Equals(order));
                return new Result(count, order);
            }
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
