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

using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// A Strand is an AtomContainer which stores additional strand specific informations for a group of Atoms.
    /// </summary>
    // @cdk.module  interfaces
    // @cdk.created 2004-12-20
    // @author Martin Eklund<martin.eklund@farmbio.uu.se>
    public interface IStrand
        : IAtomContainer
    {
        string StrandName { get; set; }
        string StrandType { get; set; }

        /// <summary>
        /// Adds the atom <paramref name="oAtom"/> without specifying a <see cref="IMonomer"/> or a <see cref="IStrand"/>. 
        /// Therefore the atom gets added to a <see cref="IMonomer"/> of type "Unknown" in a Strand of type "Unknown".
        /// </summary>
        /// <param name="oAtom">The atom to add</param>
        void AddAtom(IAtom oAtom);

        /// <summary>
        /// Adds the atom <paramref name="oAtom"/> to a specified <paramref name="oMonomer"/>.
        /// </summary>
        /// <param name="oAtom">The atom to add</param>
        /// <param name="oMonomer">The monomer the atom belongs to</param>
        void AddAtom(IAtom oAtom, IMonomer oMonomer);

        /// <summary>
        /// Returns the monomers in this strand.
        /// </summary>
        /// <returns>Map containing the monomers in the strand.</returns>
        IReadOnlyDictionary<string, IMonomer> GetMonomerMap();

        /// <summary>
        /// Retrieve a <see cref="IMonomer"/> object by specifying its name.
        /// </summary>
        /// <param name="name">The name of the monomer to look for</param>
        /// <returns>The Monomer object which was asked for</returns>
        IMonomer GetMonomer(string name);

        /// <summary>
        /// Returns a <see cref="IEnumerable{T}"/> of the names of all <see cref="IMonomer"/>s in this polymer.
        /// </summary>
        /// <returns>a <see cref="IEnumerable{T}"/> of all the monomer names.</returns>
        IEnumerable<string> GetMonomerNames();

        /// <summary>
        /// Removes a particular monomer, specified by its name.
        /// </summary>
        /// <param name="name">The name of the monomer to remove</param>
        void RemoveMonomer(string name);
    }
}
