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
    /// <para>Type-safe enumeration of InChIKey check return codes.</para>
    ///
    /// <para>InChI library return values:
    /// <list type="bullet">
    /// <item>VALID_STANDARD            (0)</item>
    /// <item>INVALID_LENGTH     (1)</item>
    /// <item>INVALID_LAYOUT     (2)</item>
    /// <item>INVALID_VERSION    (3)</item>
    /// </list>
    /// </para>
    /// <para>See <tt>inchi_api.h</tt>.</para>
    /// </summary>
    // @author Sam Adams
    internal enum INCHI_KEY_STATUS
    {
        VALID_STANDARD = 0,
        VALID_NON_STANDARD = -1,
        INVALID_LENGTH = 1,
        INVALID_LAYOUT = 2,
        INVALID_VERSION = 3,
    }
}