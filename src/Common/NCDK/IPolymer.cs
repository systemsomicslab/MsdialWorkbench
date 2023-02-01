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
    /// Subclass of Molecule to store Polymer specific attributes that a Polymer has.
    /// </summary>
    // @cdk.module  interfaces
    // @author      Edgar Luttmann <edgar@uni-paderborn.de>
    // @author      Martin Eklund <martin.eklund@farmbio.uu.se>
    // @cdk.created 2001-08-06
    // @cdk.keyword polymer
    public interface IPolymer : IAtomContainer
    {
        /// <summary>
        /// Adds the atom oAtom to a specified Monomer.
        /// </summary>
        /// <param name="oAtom">The atom to add</param>
        /// <param name="oMonomer">The monomer the atom belongs to</param>
        void AddAtom(IAtom oAtom, IMonomer oMonomer);

        /// <summary>
        /// Returns the monomers present in the <see cref="IPolymer"/>.
        /// </summary>
        /// <returns><see cref="IEnumerable{T}"/> of <see cref="KeyValuePair{TKey, TValue}"/> of monomers present in the <see cref="IPolymer"/>.</returns>
        IEnumerable<KeyValuePair<string, IMonomer>> GetMonomerMap();

        /// <summary>
        /// Retrieve a Monomer object by specifying its name.
        /// </summary>
        /// <param name="name">The name of the monomer to look for</param>
        /// <returns>The <see cref="IMonomer"/> object which was asked for</returns>
        IMonomer GetMonomer(string name);

        /// <summary>
        /// Returns enumeration of the names of all <see cref="IMonomer"/>s in this polymer.
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetMonomerNames();

        /// <summary>
        /// Removes a particular monomer, specified by its name.
        /// </summary>
        /// <param name="name">The name of the monomer to be removed</param>
        void RemoveMonomer(string name);
    }
}