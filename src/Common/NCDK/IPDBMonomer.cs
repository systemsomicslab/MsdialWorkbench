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
    /// Represents the idea of an protein monomer as found in PDB files.
    /// </summary>
    // @cdk.module  interfaces
    // @author      Miguel Rojas <miguel.rojas @uni-koeln.de>
    // @cdk.created 2006-11-20
    // @cdk.keyword pdbpolymer
    public interface IPDBMonomer
        : IMonomer
    {
        /// <summary>
        /// the I code of this monomer
        /// </summary>
        string ICode { get; set; }

        /// <summary>
        /// the Chain ID of this monomer
        /// </summary>
        string ChainID { get; set; }

        /// <summary>
        /// the sequence identifier of this monomer
        /// </summary>
        string ResSeq { get; set; }
    }
}
