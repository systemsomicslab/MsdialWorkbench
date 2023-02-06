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
    public static class PDBStructureType
    {
        public const string Unset = null;
        public const string Helix = "helix";
        public const string Sheet = "sheet";
        public const string Turn = "turn";
    }

    /// <summary>
    /// Represents the idea of an chemical structure.
    /// </summary>
    // @cdk.module  interfaces
    // @author      Miguel Rojas <miguel.rojas @uni-koeln.de>
    // @cdk.created 2006-11-20
    // @cdk.keyword pdbpolymer
    public interface IPDBStructure
       : ICDKObject
    {
        /// <summary>
        /// the ending Chain identifier of this structure
        /// </summary>
        char? EndChainID { get; set; }

        /// <summary>
        /// the ending Code for insertion of residues of this structure
        /// </summary>
        char? EndInsertionCode { get; set; }

        /// <summary>
        /// the ending sequence number of this structure
        /// </summary>
        int? EndSequenceNumber { get; set; }

        /// <summary>
        /// the start Chain identifier of this structure
        /// </summary>
        char? StartChainID { get; set; }

        /// <summary>
        /// the start Code for insertion of residues of this structure
        /// </summary>
        char? StartInsertionCode { get; set; }

        /// <summary>
        /// the start sequence number of this structure
        /// </summary>
        int? StartSequenceNumber { get; set; }

        /// <summary>
        /// the Structure Type of this structure
        /// </summary>
        string StructureType { get; set; }
    }
}
