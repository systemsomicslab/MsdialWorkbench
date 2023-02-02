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
    /// <para>Type-safe enumeration of InChI return codes.</para>
    ///
    /// <para>InChI library return values:
    /// <list type="bullet">
    /// <item>OK                      = 0</item>
    /// <item>UNKNOWN_ERROR          = 1</item>
    /// <item>EMPTY_INPUT              = 2</item>
    /// <item>NOT_INCHI_INPUT          = 3</item>
    /// <item>NOT_ENOUGH_MEMORY     = 4</item>
    /// <item>ERROR_IN_FLAG_CHAR      = 5</item>
    /// </list> 
    /// <para>See <tt>inchi_api.h</tt>.</para>
    /// </para>
    /// </summary>
    // @author Sam Adams
    internal enum INCHI_KEY
    {
        OK = 0,
        UNKNOWN_ERROR = 1,
        EMPTY_INPUT = 2,
        INVALID_INCHI_PREFIX = 3,
        NOT_ENOUGH_MEMORY = 4,
        INVALID_INCHI = 20,
        INVALID_STD_INCHI = 21,
    }
}