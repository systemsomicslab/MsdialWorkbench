/* Copyright (C) 2004-2007  Egon Willighagen <egonw@users.sf.net>
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

using System;
using System.Collections.Generic;

namespace NCDK.QSAR
{
    /// <summary>
    /// Classes that implement this interface are QSAR descriptor calculators.
    /// The architecture provides a few subinterfaces such as the
    /// <see cref="IMolecularDescriptor"/>, 
    /// <see cref="IAtomicDescriptor"/> and
    /// <see cref="IBondDescriptor"/>.
    /// </summary>
    /// <remarks>
    /// To interactively query which parameters are available, one can
    /// use the methods <see cref="System.Collections.Generic.IReadOnlyDictionary{TKey, TValue}.Keys"/> to see how many
    /// and which parameters are available.
    /// </remarks>
    // @cdk.module qsar
    public interface IDescriptor
    {
        /// <summary>Type of calculated results</summary>
        Type ResultType { get; }
    }
}
