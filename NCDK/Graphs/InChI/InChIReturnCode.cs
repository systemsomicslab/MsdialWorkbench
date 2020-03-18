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
    /// Type-safe enumeration of InChI return codes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// InChI library return values:
    /// <list type="bullet">
    /// <item><see cref="Skip"/>     (-2)    Not used in InChI library</item>
    /// <item><see cref="EOF"/>      (-1)    No structural data has been provided</item>
    /// <item><see cref="Ok"/>       (0)     Success, no errors or warnings</item>
    /// <item><see cref="Warning"/>  (1)     Success, Warning(s) issued</item>
    /// <item><see cref="Error"/>    (2)     Error: no InChI has been created</item>
    /// <item><see cref="Fatal"/>    (3)     Severe error: no InChI has been created (typically,
    ///                  memory allocation failure)</item>
    /// <item><see cref="Unknown"/>  (4)     Unknown program error</item>
    /// <item><see cref="Busy"/>     (5)     Previous call to InChI has not returned yet</item>
    /// </list>
    /// </para>
    /// <para>See <tt>inchi_api.h</tt>.</para>
    /// </remarks>
    // @author Sam Adams
    public enum InChIReturnCode
    {
        /// <summary>
        /// Not used in InChI library.
        /// </summary>
        Skip = -2,

        /// <summary>
        /// No structural data has been provided.
        /// </summary>
        EOF = -1,

        /// <summary>
        /// Success; no errors or warnings.
        /// </summary>
        Ok = 0,

        /// <summary>
        /// Success; Warning(s) issued.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Error: no InChI has been created.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Severe error: no InChI has been created (typically, memory allocation failure).
        /// </summary>
        Fatal = 3,

        /// <summary>
        /// Unknown program error.
        /// </summary>
        Unknown = 4,

        /// <summary>
        /// Previous call to InChI has not returned yet.
        /// </summary>
        Busy = 5,
    }
}
