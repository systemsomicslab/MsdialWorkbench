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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
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
    /// The base class for all chemical objects in this cdk. It provides methods for
    /// adding listeners and for their notification of events, as well a a hash
    /// table for administration of physical or chemical properties
    /// </summary>
    // @author        egonw
    // @cdk.module    interfaces
    public partial interface IChemObject
        : ICDKObject, INotify
    {
        /// <summary>
        /// Deep comparator of <see cref="IChemObject"/>.  
        /// </summary>
        /// <param name="o">Object to compare with.</param>
        /// <returns><see langword="true"/> if all properties of this object equals to <paramref name="o"/>.</returns>
        bool Compare(object o);
        
        /// <summary>
        /// Identifier (ID) of this object.
        /// </summary>
        string Id { get; set; }

        /// <summary>Flag that is set if the <see cref="IChemObject"/> is placed (somewhere).</summary>
        bool IsPlaced { get; set; }

        /// <summary>Flag is set if <see cref="IChemObject"/> has been visited</summary>
        bool IsVisited { get; set; }
    }
}
