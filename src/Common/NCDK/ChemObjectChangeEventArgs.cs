/* Copyright (C) 1997-2007  Christoph Steinbeck <steinbeck@users.sf.net>
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

namespace NCDK
{
    /// <summary>
    /// Event fired by cdk classes to their registered listeners
    /// in case something changes within them.
    /// </summary>
    // @cdk.module data
    public class ChemObjectChangeEventArgs
        : EventArgs
    {
        public object Source { get; private set; }

        /// <summary>
        /// Constructs a <see cref="ChemObjectChangeEventArgs"/> with a reference
        /// to the object where it originated.
        /// </summary>
        /// <param name="source">The reference to the object where this change event originated</param>
        public ChemObjectChangeEventArgs(object source)
            : base()
        {
            Source = source;
        }
    }
}
