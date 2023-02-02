/* Copyright (C) 2012  Gilleain Torrance <gilleain.torrance@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

namespace NCDK.Groups
{
    /// <summary>
    /// An <see cref="IAtomContainerDiscretePartitionRefiner"/> for bonds.
    /// </summary>
    /// <remarks>
    /// If two bonds are equivalent under an automorphism in the group, then
    /// roughly speaking they are in symmetric positions in the molecule. For
    /// example, the C-C bonds attaching two methyl groups to a benzene ring
    /// are 'equivalent' in this sense.
    /// </remarks>
    // @author maclean
    // @cdk.module group 
    internal class BondDiscretePartitionRefiner : AtomContainerDiscretePartitionRefinerImpl
    {
        /// <summary>
        /// Specialised option to allow generating automorphisms that ignore the bond order.
        /// </summary>
        private readonly bool ignoreBondOrders;

        /// <summary>
        /// Make a bond partition refiner that takes bond-orders into account.
        /// </summary>
        public BondDiscretePartitionRefiner() : this(false)
        {
        }

        /// <summary>
        /// Make a bond partition refiner and specify whether bonds-orders should be
        /// considered when calculating the automorphisms.
        /// </summary>
        /// <param name="ignoreBondOrders">if true, ignore the bond orders</param>
        public BondDiscretePartitionRefiner(bool ignoreBondOrders)
        {
            this.ignoreBondOrders = ignoreBondOrders;
        }

        /// <inheritdoc/>
        protected override IRefinable CreateRefinable(IAtomContainer atomContainer)
        {
            return new BondRefinable(atomContainer, ignoreBondOrders);
        }
    }
}
