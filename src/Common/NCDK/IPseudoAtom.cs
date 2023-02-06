/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
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
    /// Represents the idea of a non-chemical atom-like entity, like Me, R, X, Phe, His, etc.
    /// <para>This should be replaced by the mechanism explained in RFC #8.</para>
    /// </summary>
    /// <seealso cref="IAtom"/>
    // @cdk.module interfaces
    public interface IPseudoAtom
        : IAtom
    {
        /// <summary>
        /// The label of this <see cref="IPseudoAtom"/>.
        /// </summary>
        string Label { get; set; }

        /// <summary>
        /// The attachment point number.
        /// </summary>
        /// <value>The default, 0, indicates this atom is not an attachment point.</value>
        int AttachPointNum { get; set; }
    }
}
