/* Copyright (C) 1997-2007  Egon Willighagen <egonw@users.sf.net>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCDK.IO.CML
{
    /// <summary>
    /// Low weight alternative to Sun's Stack class.
    /// </summary>
    // @cdk.module io
    // @cdk.keyword stack    
#pragma warning disable CA1010 // Collections should implement generic interface
    public sealed class CMLStack : Stack<string>
#pragma warning restore CA1010 // Collections should implement generic interface
    {
        /// <returns>A string representation of the stack.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('/');
            foreach (var e in this.Reverse())
            {
                sb.Append(e);
                sb.Append('/');
            }
            return sb.ToString();
        }

        /// <summary>
        /// Convenience method to check the last added elements.
        /// </summary>
        /// <param name="lastElement"></param>
        /// <returns></returns>
        internal bool EndsWith(string lastElement)
        {
            return string.Equals(Peek(), lastElement, StringComparison.Ordinal);
        }

        /// <summary>
        /// Convenience method to check the last two added elements.
        /// </summary>
        /// <param name="oneButLast"></param>
        /// <param name="lastElement"></param>
        /// <returns></returns>
        internal bool EndsWith(string oneButLast, string lastElement)
        {
            return EndsWith(lastElement) && string.Equals(this.ElementAt(1), oneButLast, StringComparison.Ordinal);
        }

        /// <summary>
        /// Convenience method to check the last three added elements.
        /// </summary>
        internal bool EndsWith(string twoButLast, string oneButLast, string lastElement)
        {
            return EndsWith(oneButLast, lastElement) && string.Equals(this.ElementAt(2), twoButLast, StringComparison.Ordinal);
        }
    }
}