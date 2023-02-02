/* Copyright (C) 2007  Egon Willighagen <ewilligh@users.sf.net>
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
    /// Class to represent an IPseudoAtom which embeds an IAtomContainer. Very much
    /// like the MDL molfile Group concept.
    /// </summary>
    // @cdk.module interfaces
    // @author egonw
    public interface IFragmentAtom
        : IPseudoAtom
    {
        /// <summary>
        /// Helper method to indicate that the method should be drawn fully, and not
        /// just the abbreviated form.
        /// </summary>
        /// <value><see langword="true"/> if the full structure should be drawn</value>
        bool IsExpanded { get; set; }

        /// <summary>
        /// The fully expanded form as an <see cref="IAtomContainer"/>  object
        /// </summary>
        IAtomContainer Fragment { get; set; }
    }
}
