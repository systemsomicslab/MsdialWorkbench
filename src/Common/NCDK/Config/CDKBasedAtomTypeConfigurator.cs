/* Copyright (C) 2003-2007  Egon Willighagen <egonw@users.sf.net>
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

using System.Collections.Generic;
using System.IO;
using NCDK.Config.AtomTypes;

namespace NCDK.Config
{
    /// <summary>
    /// <see cref="IAtomType"/> resource that reads the atom type configuration from an XML file.
    /// The format is required to be in the STMML format <token>cdk-cite-PMR2002</token>; examples
    /// can be found in the NCDK.Config.Data directory.
    /// </summary>
    // @cdk.module core
    public class CDKBasedAtomTypeConfigurator
        : IAtomTypeConfigurator
    {
        private const string configFile = "NCDK.Config.Data.structgen_atomtypes.xml";
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

        public CDKBasedAtomTypeConfigurator() { }

        /// <summary>
        /// Reads the atom types from the CDK based atom type list.
        /// </summary>
        /// <returns><see cref="IEnumerable{IAtomType}"/> with read IAtomType's.</returns>
        /// <exception cref="IOException">when a problem occurred with reading from the <see cref="GetStream()"/></exception>
        public IEnumerable<IAtomType> ReadAtomTypes()
        {
            if (GetStream() == null)
                SetStream(ResourceLoader.GetAsStream(configFile));

            return new AtomTypeReader(GetStream()).ReadAtomTypes();
        }
    }
}
