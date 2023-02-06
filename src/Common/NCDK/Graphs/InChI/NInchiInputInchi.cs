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

using System.Collections.Generic;

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// Encapsulates input for InChI to structure conversion.
    /// </summary>
    // @author Sam Adams
    internal class NInchiInputInchi
    {
        /// <summary>
        /// InChI ASCIIZ string to be converted to a strucure
        /// </summary>
        public string Inchi { get; set; }

        /// <summary>
        /// InChI options: space-delimited
        /// </summary>
        public string Options 
        {
            get;
            protected internal set;
        }

        /// <summary>
        /// Constructor.
        /// <param name="inchi">InChI string</param>
        /// </summary>
        public NInchiInputInchi(string inchi)
        {
            this.Inchi = inchi;
            this.Options = "";
        }

        /// <summary>
        /// Constructor.
        /// <param name="inchi">InChI string</param>
        /// <param name="opts">Options</param>
        /// </summary>
        public NInchiInputInchi(string inchi, string opts)
        {
            this.Inchi = inchi;
            this.Options = NInchiWrapper.CheckOptions(opts);
        }

        /// <summary>
        /// Constructor.
        /// <param name="inchi">InChI string</param>
        /// <param name="opts">Options</param>
        /// </summary>
        public NInchiInputInchi(string inchi, IEnumerable<InChIOption> opts)
        {
            Inchi = inchi;
            Options = NInchiWrapper.CheckOptions(opts);
        }
    }
}
