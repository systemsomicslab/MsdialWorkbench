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

namespace NCDK
{
    /// <summary>
    /// A set of reactions, for example those taking part in a reaction.
    /// </summary>
    // @cdk.module  interfaces
    // @cdk.keyword reaction
    public interface IReactionSet
        : IChemObject, IList<IReaction>
    {
        /// <summary>
        /// Returns true if this IReactionSet is empty.
        /// </summary>
        /// <returns>a boolean indicating if this ring set no reactions</returns>
        bool IsEmpty();
    }
}
