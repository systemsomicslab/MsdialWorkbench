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
    /// A <see cref="IBioPolymer"/> is a subclass of a <see cref="IPolymer"/> which is supposed to store
    /// additional informations about the <see cref="IPolymer"/> which are connected to <see cref="IBioPolymer"/>s.
    /// </summary>
    // @cdk.module  interfaces
    // @author      Edgar Luttmann <edgar@uni-paderborn.de>
    // @cdk.created 2001-08-06
    // @cdk.keyword polymer
    // @cdk.keyword biopolymer
    public interface IBioPolymer
        : IPolymer
    {
        /// <summary>
        /// Adds the atom oAtom to a specified Strand, whereas the Monomer is unspecified. Hence
        /// the atom will be added to a Monomer of type Unknown in the specified Strand.
        /// </summary>
        /// <param name="oAtom">The atom to add</param>
        /// <param name="oStrand">The strand the atom belongs to</param>
        void AddAtom(IAtom oAtom, IStrand oStrand);

        /// <summary>
        /// Adds the atom to a specified Strand and a specified Monomer.
        /// </summary>
        /// <param name="oAtom">The atom to add</param>
        /// <param name="oMonomer">The monomer the atom belongs to</param>
        /// <param name="oStrand">The strand the atom belongs to</param>
        void AddAtom(IAtom oAtom, IMonomer oMonomer, IStrand oStrand);

        /// <summary>
        /// Retrieve a <see cref="IMonomer"/> object by specifying its name.
        /// You have to specify the strand to enable
        /// monomers with the same name in different strands. There is at least one such case: every
        /// strand contains a monomer called "".
        /// </summary>
        /// <param name="monName">The name of the monomer to look for</param>
        /// <param name="strandName">The name of the strand to look for</param>
        /// <returns>The Monomer object which was asked for</returns>
        IMonomer GetMonomer(string monName, string strandName);

        /// <summary>
        /// Map containing the strands in the Polymer.
        /// </summary>
        /// <returns><see cref="Dictionary{TKey, TValue}"/> containing the strands in the Polymer</returns>
        IReadOnlyDictionary<string, IStrand> GetStrandMap();

        /// <summary>
        /// Retrieve a Monomer object by specifying its name.
        /// </summary>
        /// <param name="cName">The name of the monomer to look for</param>
        /// <returns>The Monomer object which was asked for</returns>
        IStrand GetStrand(string cName);

        /// <summary>
        /// Returns a collection of the names of all <see cref="IStrand"/>s in this <see cref="IBioPolymer"/>.
        /// </summary>
        /// <returns><see cref="IEnumerable{T}"/> of all the strand names.</returns>
        IEnumerable<string> GetStrandNames();

        /// <summary>
        /// Removes a particular strand, specified by its name.
        /// </summary>
        /// <param name="name">The name of the strand to remove</param>
        void RemoveStrand(string name);
    }
}
