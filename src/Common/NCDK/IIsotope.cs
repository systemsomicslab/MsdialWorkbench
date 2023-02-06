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

namespace NCDK
{
    /// <summary>
    /// Used to store and retrieve data of a particular isotope.
    /// </summary>
    // @cdk.module interfaces
    // @author      egonw
    // @cdk.created 2005-08-24
    // @cdk.keyword isotope
    // @cdk.keyword mass number
    // @cdk.keyword number, mass
    public interface IIsotope
        : IElement
    {
        /// <summary>
        /// The natural abundance attribute of the <see cref="IIsotope"/> object.
        /// </summary>
        double? Abundance { get; set; }

        /// <summary>
        /// The exact mass attribute of the <see cref="IIsotope"/> object.
        /// </summary>
        double? ExactMass { get; set; }

        /// <summary>
        /// the atomic mass of this element.
        /// </summary>
        /// <value><see langword="null"/> when unconfigured.</value>
        int? MassNumber { get; set; }
    }
}
