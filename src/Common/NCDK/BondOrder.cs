/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using System.Collections.Generic;
using System.Reflection;
using static NCDK.BondOrder;

namespace NCDK
{
    /// <summary>
    /// A list of permissible bond orders.
    /// </summary>
    [Obfuscation(ApplyToMembers = true, Exclude = true)]
    public enum BondOrder
    {
        Unset = 0,
#pragma warning disable CA1720 // Identifier contains type name
        Single = 1,
        Double = 2,
#pragma warning restore CA1720 // Identifier contains type name
        Triple = 3,
        Quadruple = 4,
        Quintuple = 5,
        Sextuple = 6,
    }

    public static class BondOrderTools
    {
        internal static IReadOnlyList<BondOrder> Values { get; } = new[]
        {
            Unset,
            Single,
            Double,
            Triple,
            Quadruple,
            Quintuple,
            Sextuple,
        };

        /// <summary>
        /// A numeric value for the number of bonded electron pairs.
        /// </summary>
        public static int Numeric(this BondOrder value)
            => (int)value;

        public static bool IsUnset(this BondOrder value)
            => value == Unset;
    }
}
