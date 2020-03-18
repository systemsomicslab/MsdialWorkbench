/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;

namespace NCDK.Tools.Manipulator
{
    /// <summary>
    /// Class with convenience methods that provide methods to manipulate 
    /// <see cref="IAtomContainer"/>'s <see cref="IBond"/>. 
    /// </summary>
    /// <example>
    /// <code>
    /// AtomContainerManipulator.ReplaceAtomByAtom(container, atom1, atom2);
    /// </code>
    /// will replace the Atom in the AtomContainer, but in all the ElectronContainer's
    /// it participates too.
    /// </example>
    // @cdk.module  core
    // @author  Egon Willighagen
    // @cdk.created 2003-08-07
    public static class BondManipulator
    {
        /// <summary>
        /// Constructs an array of Atom objects from Bond.
        /// </summary>
        public static IAtom[] GetAtomArray(IBond container)
        {
            var ret = new IAtom[container.Atoms.Count];
            for (int i = 0; i < ret.Length; ++i)
                ret[i] = container.Atoms[i];
            return ret; 
        }

        /// <summary>
        /// Returns true if the first bond has a lower bond order than the second bond.
        /// It returns false if the bond order is equal, and if the order of the first
        /// bond is larger than that of the second. Also returns false if either bond
        /// order is unset.
        /// </summary>
        /// <param name="first">The first bond order object</param>
        /// <param name="second">The second bond order object</param>
        /// <returns>true if the first bond order is lower than the second one, false otherwise</returns>
        /// <seealso cref="IsHigherOrder(BondOrder, BondOrder)"/>
        public static bool IsLowerOrder(BondOrder first, BondOrder second)
        {
            if (first == BondOrder.Unset || second == BondOrder.Unset)
                return false;
            return first.CompareTo(second) < 0;
        }

        /// <summary>
        /// Returns true if the first bond has a higher bond order than the second bond.
        /// It returns false if the bond order is equal, and if the order of the first
        /// bond is lower than that of the second. Also returns false if either bond
        /// order is unset.
        /// </summary>
        /// <param name="first">The first bond order object</param>
        /// <param name="second">The second bond order object</param>
        /// <returns>true if the first bond order is higher than the second one, false otherwise</returns>
        /// <seealso cref="IsLowerOrder(BondOrder, BondOrder)"/>
        public static bool IsHigherOrder(BondOrder first, BondOrder second)
        {
            if (first == BondOrder.Unset || second == BondOrder.Unset)
                return false;
            return first.CompareTo(second) > 0;
        }

        /// <summary>
        /// Returns the BondOrder one higher. Does not increase the bond order
        /// beyond the Quadruple bond order.
        /// </summary>
        /// <param name="oldOrder">the old order</param>
        /// <returns>The incremented bond order</returns>
        /// <seealso cref="IncreaseBondOrder(IBond)"/>
        /// <seealso cref="DecreaseBondOrder(BondOrder)"/>
        /// <seealso cref="DecreaseBondOrder(IBond)"/>
        public static BondOrder IncreaseBondOrder(BondOrder oldOrder)
        {
            if (oldOrder == BondOrder.Single) return BondOrder.Double;
            if (oldOrder == BondOrder.Double) return BondOrder.Triple;
            if (oldOrder == BondOrder.Triple) return BondOrder.Quadruple;
            if (oldOrder == BondOrder.Quadruple) return BondOrder.Quintuple;
            if (oldOrder == BondOrder.Quintuple) return BondOrder.Sextuple;
            return oldOrder;
        }

        /// <summary>
        /// Increment the bond order of this bond.
        /// </summary>
        /// <param name="bond">The bond whose order is to be incremented</param>
        /// <seealso cref="IncreaseBondOrder(BondOrder)"/>
        /// <seealso cref="DecreaseBondOrder(BondOrder)"/>
        /// <seealso cref="DecreaseBondOrder(IBond)"/>
        public static void IncreaseBondOrder(IBond bond)
        {
            bond.Order = IncreaseBondOrder(bond.Order);
        }

        /// <summary>
        /// Returns the BondOrder one lower. Does not decrease the bond order
        /// lower the Quadruple bond order.
        /// </summary>
        /// <param name="oldOrder">the old order</param>
        /// <returns>the decremented order</returns>
        /// <seealso cref="DecreaseBondOrder(IBond)"/>
        /// <seealso cref="IncreaseBondOrder(BondOrder)"/>
        /// <seealso cref="IncreaseBondOrder(BondOrder)"/>
        public static BondOrder DecreaseBondOrder(BondOrder oldOrder)
        {
            if (oldOrder == BondOrder.Sextuple) return BondOrder.Quintuple;
            if (oldOrder == BondOrder.Quintuple) return BondOrder.Quadruple;
            if (oldOrder == BondOrder.Quadruple) return BondOrder.Triple;
            if (oldOrder == BondOrder.Triple) return BondOrder.Double;
            if (oldOrder == BondOrder.Double) return BondOrder.Single;
            return oldOrder;
        }

        /// <summary>
        /// Decrease the order of a bond.
        /// </summary>
        /// <param name="bond">The bond in question</param>
        /// <seealso cref="DecreaseBondOrder(BondOrder)"/>
        /// <seealso cref="IncreaseBondOrder(BondOrder)"/>
        /// <seealso cref="IncreaseBondOrder(BondOrder)"/>
        public static void DecreaseBondOrder(IBond bond)
        {
            bond.Order = DecreaseBondOrder(bond.Order);
        }

        /// <summary>
        /// Convenience method to convert a double into an BondOrder.
        /// Returns <see cref="BondOrder.Unset"/> if the bond order is not 1.0, 2.0, 3.0 and 4.0.
        /// </summary>
        /// <param name="bondOrder">The numerical bond order</param>
        /// <returns>An instance of <see cref="BondOrder"/></returns>
        /// <seealso cref="DestroyBondOrder(BondOrder)"/>
        public static BondOrder CreateBondOrder(double bondOrder)
        {
            foreach (var order in BondOrderTools.Values)
            {
                if (order.Numeric() == bondOrder)
                    return order;
            }
            return BondOrder.Unset;
        }

        /// <summary>
        /// Convert a <see cref="BondOrder"/> to a numeric value.
        ///
        /// Single, double, triple and quadruple bonds are converted to 1.0, 2.0, 3.0
        /// and 4.0 respectively.
        /// </summary>
        /// <param name="bondOrder">The bond order object</param>
        /// <returns>The numeric value</returns>
        /// <seealso cref="CreateBondOrder(double)"/>
        [Obsolete("use BondOrder.Numeric instead")]
        public static double DestroyBondOrder(BondOrder bondOrder)
        {
            return bondOrder.Numeric();
        }

        /// <summary>
        /// Returns the maximum bond order for a List of bonds, given an iterator to the list.
        /// <param name="bonds">An iterator for the list of bonds</param>
        /// <returns>The maximum bond order found</returns>
        /// </summary>
        /// <seealso cref="GetMaximumBondOrder(IEnumerable{IBond})"/>
        public static BondOrder GetMaximumBondOrder(IEnumerable<IBond> bonds)
        {
            var maxOrder = BondOrder.Single;
            foreach (var bond in bonds)
            {
                if (IsHigherOrder(bond.Order, maxOrder))
                    maxOrder = bond.Order;
            }
            return maxOrder;
        }

        /// <summary>
        /// Returns the maximum bond order for the two bonds.
        /// </summary>
        /// <param name="firstBond">first bond to compare</param>
        /// <param name="secondBond">second bond to compare</param>
        /// <returns>The maximum bond order found</returns>
        public static BondOrder GetMaximumBondOrder(IBond firstBond, IBond secondBond)
        {
            if (firstBond == null)
                throw new ArgumentNullException(nameof(firstBond));
            if (secondBond == null)
                throw new ArgumentNullException(nameof(secondBond));
            return GetMaximumBondOrder(firstBond.Order, secondBond.Order);
        }

        /// <summary>
        /// Returns the maximum bond order for the two bond orders.
        /// </summary>
        /// <param name="firstOrder">first bond order to compare</param>
        /// <param name="secondOrder">second bond order to compare</param>
        /// <returns>The maximum bond order found</returns>
        public static BondOrder GetMaximumBondOrder(BondOrder firstOrder, BondOrder secondOrder)
        {
            if (firstOrder == BondOrder.Unset)
            {
                if (secondOrder == BondOrder.Unset)
                    throw new ArgumentException("Both bond orders are unset");
                return secondOrder;
            }
            if (secondOrder == BondOrder.Unset)
            {
                if (firstOrder == BondOrder.Unset)
                    throw new ArgumentException("Both bond orders are unset");
                return firstOrder;
            }

            if (IsHigherOrder(firstOrder, secondOrder))
                return firstOrder;
            else
                return secondOrder;
        }

        /// <summary>
        /// Returns the minimum bond order for a List of bonds, given an iterator
        /// to the list.
        /// </summary>
        /// <param name="bonds">An iterator for the list of bonds</param>
        /// <returns>The minimum bond order found</returns>
        /// <seealso cref="GetMinimumBondOrder(IEnumerable{IBond})"/>
        public static BondOrder GetMinimumBondOrder(IEnumerable<IBond> bonds)
        {
            var minOrder = BondOrder.Sextuple;
            foreach (var bond in bonds)
                if (IsLowerOrder(bond.Order, minOrder)) minOrder = bond.Order;
            return minOrder;
        }

        /// <summary>
        /// Get the single bond equivalent (SBE) of a list of bonds, given an iterator to the list.
        /// </summary>
        /// <param name="bonds">An iterator to the list of bonds</param>
        /// <returns>The SBE sum</returns>
        public static int GetSingleBondEquivalentSum(IEnumerable<IBond> bonds)
        {
            int sum = 0;
            foreach (var bond in bonds)
            {
                var order = bond.Order;
                if (!order.IsUnset())
                {
                    sum += order.Numeric();
                }
            }
            return sum;
        }
    }
}
