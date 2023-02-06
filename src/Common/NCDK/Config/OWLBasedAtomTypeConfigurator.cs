/* Copyright (C) 2003-2008  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Config.AtomTypes;
using System;
using System.Collections.Generic;
using System.IO;

namespace NCDK.Config
{
    /// <summary>
    /// <see cref="IAtomType"/> resource that reads the atom type configuration from an OWL file.
    /// </summary>
    // @cdk.module  core
    public class OWLBasedAtomTypeConfigurator
        : IAtomTypeConfigurator
    {
        private Stream stream;

        /// <inheritdoc/>
        public Stream GetStream()
        {
            return stream;
        }

        /// <inheritdoc/>
        public void SetStream(Stream value)
        {
            stream = value;
        }

        public OWLBasedAtomTypeConfigurator() { }

        /// <summary>
        /// Reads the atom types from the OWL based atom type list.
        /// </summary>
        /// <returns>A <see cref="IEnumerable{IAtomType}"/> with read <see cref="IAtomType"/>'s.</returns>
        /// <exception cref="IOException">when a problem occurred with reading from the <see cref="GetStream()"/></exception>
        public IEnumerable<IAtomType> ReadAtomTypes()
        {
            if (GetStream() == null)
                throw new Exception("There was a problem getting an input stream");

            return new OWLAtomTypeReader(GetStream()).ReadAtomTypes();
        }
    }
}
