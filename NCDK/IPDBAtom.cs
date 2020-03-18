/* Copyright (C) 2006-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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
    /// A PDBAtom is a subclass of a Atom which is supposed to store additional informations about the Atom.
    /// </summary>
    // @cdk.module  interfaces
    // @author      Miguel Rojas <miguel.rojas @uni-koeln.de>
    // @cdk.created 2006-11-20
    // @cdk.keyword pdbpolymer
    public interface IPDBAtom
        : IAtom
    {
        string Record { get; set; }

        /// <summary>
        /// the Temperature factor of this atom
        /// </summary>
        double? TempFactor { get; set; }

        /// <summary>
        /// the Residue name of this atom
        /// </summary>
        string ResName { get; set; }

        /// <summary>
        /// the Code for insertion of residues of this atom
        /// </summary>
        string ICode { get; set; }

        /// <summary>
        /// The Atom name of this atom.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// the Chain identifier of this atom
        /// </summary>
        string ChainID { get; set; }

        /// <summary>
        /// the Alternate location indicator of this atom
        /// </summary>
        string AltLoc { get; set; }

        /// <summary>
        /// the Segment identifier, left-justified of this atom
        /// </summary>
        string SegID { get; set; }

        /// <summary>
        /// the Atom serial number of this atom
        /// </summary>
        int? Serial { get; set; }

        /// <summary>
        /// the Residue sequence number of this atom
        /// </summary>
        string ResSeq { get; set; }

        /// <summary>
        /// true if this atom is a PDB OXT atom.
        /// </summary>
        bool Oxt { get; set; }

        /// <summary>
        /// true if the atom is a heteroatom, otherwise false
        /// </summary>
        bool? HetAtom { get; set; }

        /// <summary>
        /// the Occupancy of this atom
        /// </summary>
        double? Occupancy { get; set; }
    }
}
