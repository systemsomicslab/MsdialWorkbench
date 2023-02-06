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

using NCDK.Numerics;

namespace NCDK
{
    /// <summary>
    /// Class representing a molecular crystal.
    /// The crystal is described with molecules in fractional
    /// coordinates and three cell axes: a,b and c.
    /// The crystal is designed to store only the asymetric atoms.
    /// Though this is not enforced, it is assumed by all methods.
    /// </summary>
    // @cdk.module interfaces
    // @cdk.keyword crystal
    public interface ICrystal
        : IAtomContainer
    {
        /// <summary>the A unit cell axes in carthesian coordinates in a eucledian space.</summary>
        Vector3 A { get; set; }

        /// <summary>the B unit cell axes in carthesian coordinates in a eucledian space.</summary>
        Vector3 B { get; set; }

        /// <summary>the C unit cell axes in carthesian coordinates in a eucledian space.</summary>
        Vector3 C { get; set; }

        /// <summary>the space group of this crystal.</summary>
        string SpaceGroup { get; set; }

        /// <summary>the number of asymmetric parts in the unit cell.</summary>
        int? Z { get; set; }
    }
}