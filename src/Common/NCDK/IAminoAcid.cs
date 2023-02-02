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
    /// A AminoAcid is Monomer which stores additional amino acid specific informations, like the N-terminus atom.
    /// </summary>
    // @cdk.module  interfaces
    // @author      Egon Willighagen <e.willighagen@science.ru.nl>
    // @cdk.created 2005-12-05
    // @cdk.keyword amino acid
    public interface IAminoAcid
        : IMonomer
    {
        /// <summary>
        /// The N-terminus atom.
        /// </summary>
        IAtom NTerminus { get; }

        /// <summary>
        /// The C-terminus atom.
        /// </summary>
        IAtom CTerminus { get; }

        /// <summary>
        /// Add an Atom and makes it the N-terminus atom.
        /// </summary>
        /// <param name="atom">The Atom that is the N-terminus</param>
        void AddNTerminus(IAtom atom);

        /// <summary>
        /// Add an Atom and makes it the C-terminus atom.
        /// </summary>
        /// <param name="atom">The Atom that is the C-terminus</param>
        void AddCTerminus(IAtom atom);
    }
}
