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
    // @author sea36
    internal enum INCHI_STATUS
    {
        INCHI_VALID_STANDARD = 0,
        INCHI_VALID_NON_STANDARD = 1,
        INCHI_VALID_BETA = 2,
        INCHI_INVALID_PREFIX = 3,
        INCHI_INVALID_VERSION = 4,
        INCHI_INVALID_LAYOUT = 5,
        INCHI_FAIL_I2I = 6,
    }
}
