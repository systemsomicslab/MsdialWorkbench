/*
 * Copyright 2006-2011 Sam Adams <sea36 at users.sourceforge.net>
 *
 * This file is part of JNI-InChI.
 *
 * JNI-InChI is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * JNI-InChI is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with JNI-InChI.  If not, see <http://www.gnu.org/licenses/>.
 */

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// Enumeration of InChI bond type definitions.
    /// Corresponds to <tt>inchi_BondType</tt> in <tt>inchi_api.h</tt>.
    /// </summary>
    // @author Sam Adams
    internal enum INCHI_BOND_TYPE
    {
        None = 0,

        /// <summary>
        /// Single bond.
        /// </summary>
        Single = 1,

        /// <summary>
        /// Double bond.
        /// </summary>
        Double = 2,

        /// <summary>
        /// Triple bond.
        /// </summary>
        Triple = 3,

        /// <summary>
        /// Alternating (single-double) bond. Avoid where possible.
        /// </summary>
        Altern = 4
    }
}
