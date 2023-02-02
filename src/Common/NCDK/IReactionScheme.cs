/* Copyright (C) 2006-2007  Miguel Rojas <miguelrojasch@yahoo.es>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// Classes that implement this interface of a scheme.
    /// This is designed to contain a set of reactions which are linked in
    /// some way but without hard coded semantics.
    /// </summary>
    // @author      miguelrojasch <miguelrojasch@yahoo.es>
    public interface IReactionScheme : IReactionSet, ICloneable
    {
        /// <summary>
        /// Add a scheme of reactions.
        /// </summary>
        /// <param name="reactScheme">The <see cref="IReactionScheme"/> to include</param>
        void Add(IReactionScheme reactScheme);

        /// <summary>
        /// ll schemes in this scheme.
        /// </summary>
        ICollection<IReactionScheme> Schemes { get; }

        /// <summary>
        /// The reactions in this scheme.
        /// </summary>
        IEnumerable<IReaction> Reactions { get; }

        /// <summary>
        /// Removes an <see cref="IReactionScheme"/> from this.
        /// </summary>
        /// <param name="scheme">The scheme to be removed from this </param>
        void Remove(IReactionScheme scheme);
    }
}