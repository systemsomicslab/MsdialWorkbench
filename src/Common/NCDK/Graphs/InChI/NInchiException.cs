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

using System;

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// Exception thrown by JniInchi.
    /// </summary>
    // @author Sam Adams
    public class NInchiException : Exception
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public NInchiException()
            : base()
        { }

        /// <summary>
        /// Constructs a new exception with the specified detail message.
        ///
        /// <param name="message">the detail message.</param>
        /// </summary>
        public NInchiException(string message)
            : base(message)
        { }

        /// <summary>
        /// Constructs a new exception with the specified cause.
        ///
        /// <param name="ex">the cause.</param>
        /// </summary>
        public NInchiException(Exception ex)
            : base(ex.Message, ex)
        { }

        public NInchiException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
