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

using System.Reflection;
using static NCDK.Hybridization;

namespace NCDK
{
    /// <summary>
    /// Hybridization states.
    /// </summary>
    [Obfuscation(ApplyToMembers =true, Exclude =true)]
    public enum Hybridization
    {
        /// <summary>
        /// A undefined hybridization.
        /// </summary>
        Unset,
        /// <summary>
        /// Liner.
        /// </summary>
        S,
        /// <summary>
        /// A geometry of neighboring atoms when an s orbital is hybridized with one p orbital.
        /// </summary>
        SP1,
        /// <summary>
        /// A geometry of neighboring atoms when an s orbital is hybridized with two p orbitals.
        /// </summary>
        SP2,
        /// <summary>
        /// A geometry of neighboring atoms when an s orbital is hybridized with three p orbitals.
        /// </summary>
        SP3,
        /// <summary>
        /// Trigonal planar (lone pair in pz).
        /// </summary>
        Planar3,
        /// <summary>
        /// A geometry of neighboring atoms when an s orbital is hybridized with three p orbitals with one d orbital.
        /// </summary>
        SP3D1,
        /// <summary>
        /// A geometry of neighboring atoms when an s orbital is hybridized with three p orbitals with two d orbitals.
        /// </summary>
        SP3D2,
        /// <summary>
        /// A geometry of neighboring atoms when an s orbital is hybridized with three p orbitals with three d orbitals.
        /// </summary>
        SP3D3,
        /// <summary>
        /// A geometry of neighboring atoms when an s orbital is hybridized with three p orbitals with four d orbitals.
        /// </summary>
        SP3D4,
        /// <summary>
        /// A geometry of neighboring atoms when an s orbital is hybridized with three p orbitals with five d orbitals.
        /// </summary>
        SP3D5,
    }

    internal static class HybridizationTools
    {
        public static bool IsUnset(this Hybridization value)
        {
            return value == Unset;
        }

        internal static Hybridization ToHybridization(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return Unset;

            switch (value.ToUpperInvariant())
            {
                case "S":
                    return S;
                case "SP":
                    return SP1;
                case "SP1":
                    return SP1;
                case "SP2":
                    return SP2;
                case "SP3":
                case "TETRAHEDRAL":
                    return SP3;
                case "PLANAR":
                    return Planar3;
                case "SP3D1":
                    return SP3D1;
                case "SP3D2":
                case "OCTAHEDRAL":
                    return SP3D2;
                case "SP3D3":
                    return SP3D3;
                case "SP3D4":
                    return SP3D4;
                case "SP3D5":
                    return SP3D5;
                default:
                    break;
            }

            throw new System.ArgumentException("Unrecognized hybridization", nameof(value));
        }
    }
}
