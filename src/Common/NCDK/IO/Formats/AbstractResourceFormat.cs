/*
 * Copyright (c) 2013. John May <jwmay@users.sf.net>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using System;
using System.Collections.Generic;

namespace NCDK.IO.Formats
{
    /// <summary>
    /// An abstract class providing <see cref="GetHashCode()"/> and <see cref="Equals(object)"/> for
    /// <see cref="IResourceFormat"/>s. As <see cref="IResourceFormat"/>s are stateless this implementation uses the class for equality testing.
    /// </summary>
    // @author John May
    // @cdk.module ioformats
    public abstract class AbstractResourceFormat : IResourceFormat
    {
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return GetType().GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(Object that)
        {
            return that != null && this.GetType().Equals(that.GetType());
        }

        /// <inheritdoc/>
        public abstract string FormatName { get; }
        
        /// <inheritdoc/>
        public abstract string PreferredNameExtension { get; }
        
        /// <inheritdoc/>
        public abstract IReadOnlyList<string> NameExtensions { get; }
        
        /// <inheritdoc/>
        public abstract string MIMEType { get; }
        
        /// <inheritdoc/>
        public abstract bool IsXmlBased { get; }
    }
}
