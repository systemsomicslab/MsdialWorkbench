/*
 * Copyright (C) 2010  Mark Rijnbeek <mark_rynbeek@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may
 * distribute with programs based on this work.
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

namespace NCDK.Isomorphisms.Matchers
{
    /// <summary>
    /// Represents a single substitute structure in an <see cref="RGroupList"/>.
    /// <para>
    /// The order of attachment points is provided (first and second only, conform
    /// RGFile spec). This order is relevant when the structure connects to the root
    /// with more than one bond.</para>
    /// </summary>
    /// <seealso cref="RGroupList"/>
    /// <seealso cref="RGroupQuery"/>
    // @cdk.module  isomorphism
    // @cdk.keyword Rgroup
    // @cdk.keyword R group
    // @cdk.keyword R-group
    // @author Mark Rijnbeek
    public class RGroup
    {
        /// <summary>
        /// Standard label/title to be used for the root atom container.
        /// </summary>
        public const string RootLabelKey = "Root structure";

        /// <summary>
        /// Makes a label/title to be used for a substituent.
        /// </summary>
        /// <param name="rgroupNum">R-Group number (1..32)</param>
        /// <returns>label for substituent, like "R3"</returns>
        public static string MakeLabel(int rgroupNum)
        {
            return "(R" + rgroupNum + ")";
        }

        public IAtom FirstAttachmentPoint { get; set; }
        public IAtom SecondAttachmentPoint { get; set; }
        public IAtomContainer Group { get; set; }
    }
}
