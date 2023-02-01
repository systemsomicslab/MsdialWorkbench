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
    /// Encapsulates structure input for InChI generation.
    /// </summary>
    // @author Sam Adams
    internal class NInchiInput : NInchiStructure
    {
        /// <summary>
        /// Options string,
        /// </summary>
        public string Options {
            get;
            protected internal set;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public NInchiInput()
        {
            Options = "";
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="opts">Options string.</param>
        public NInchiInput(string opts)
        {
            Options = opts == null ? "" : NInchiWrapper.CheckOptions(opts);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="opts">List of options.</param>
        public NInchiInput(IList<InChIOption> opts)
        {
            Options = NInchiWrapper.CheckOptions(opts);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public NInchiInput(NInchiStructure struct_)
            : this()
        {
            SetStructure(struct_);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public NInchiInput(NInchiStructure struct_, string opts)
            : this(opts)
        {
            SetStructure(struct_);
        }
    }
}
