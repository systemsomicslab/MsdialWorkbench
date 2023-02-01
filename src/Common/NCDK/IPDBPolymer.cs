/* Copyright (C) 2006-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
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
    /// A PDBPolymer is a subclass of a BioPolymer which is supposed to store
    /// additional informations about the BioPolymer which are connected to BioPolymers.
    /// </summary>
    // @cdk.module  interfaces
    // @author      Miguel Rojas <miguel.rojas @uni-koeln.de>
    // @cdk.created 2006-11-20
    // @cdk.keyword polymer
    // @cdk.keyword biopolymer
    // @cdk.keyword pdbpolymer
    public interface IPDBPolymer
        : IBioPolymer
    {
        /// <summary>
        /// Adds the atom oAtom without specifying a Monomer or a Strand. Therefore the
        /// atom to this AtomContainer, but not to a certain Strand or Monomer (intended
        /// e.g. for HETATMs).
        /// </summary>
        /// <param name="oAtom">The atom to add</param>
        void Add(IPDBAtom oAtom);

        /// <summary>
        /// Adds the atom to a specified Strand and a specified Monomer.
        /// </summary>
        /// <param name="oAtom">The atom to add</param>
        /// <param name="oMonomer">The monomer the atom belongs to</param>
        /// <param name="oStrand">The strand the atom belongs to</param>
        void AddAtom(IPDBAtom oAtom, IMonomer oMonomer, IStrand oStrand);

        /// <summary>
        /// Adds the <see cref="IPDBStructure"/> structure a this <see cref="IPDBPolymer"/>.
        /// </summary>
        /// <param name="structureToAdd">The <see cref="IPDBStructure"/> to add</param>
        void Add(IPDBStructure structureToAdd);

        /// <summary>
        /// Returns a Collection containing the PDBStructure in the PDBPolymer.
        /// </summary>
        /// <returns>Collection containing the PDBStructure in the PDBPolymer</returns>
        IEnumerable<IPDBStructure> GetStructures();
    }
}
