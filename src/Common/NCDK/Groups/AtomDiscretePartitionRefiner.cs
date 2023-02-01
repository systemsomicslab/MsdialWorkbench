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
    /// An <see cref="IAtomContainerDiscretePartitionRefiner"/> for atoms.
    /// </summary>
    // @author maclean
    // @cdk.module group 
    class AtomDiscretePartitionRefiner : AtomContainerDiscretePartitionRefinerImpl
    {
        /// <summary>
        /// Ignore the elements when creating the initial partition.
        /// </summary>
        private readonly bool ignoreElements;

        /// <summary>
        /// Specialised option to allow generating automorphisms
        /// that ignore the bond order.
        /// </summary>
        private readonly bool ignoreBondOrders;

        /// <summary>
        /// Default constructor - does not ignore elements or bond orders
        /// or bond orders.
        /// </summary>
        public AtomDiscretePartitionRefiner() : this(false, false)
        {
        }

        /// <summary>
        /// Make a refiner with various advanced options.
        /// </summary>
        /// <param name="ignoreElements">ignore element symbols when making automorphisms</param>
        /// <param name="ignoreBondOrders">ignore bond order when making automorphisms</param>
        public AtomDiscretePartitionRefiner(bool ignoreElements, bool ignoreBondOrders)
        {
            this.ignoreElements = ignoreElements;
            this.ignoreBondOrders = ignoreBondOrders;
        }

        /// <inheritdoc/>
        protected override IRefinable CreateRefinable(IAtomContainer atomContainer)
        {
            return new AtomRefinable(atomContainer, ignoreElements, ignoreBondOrders);
        }
    }
}
