/* Copyright (C) 2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation, version 2.1.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Config.AtomTypes;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace NCDK.AtomTypes.Mappers
{
    /// <summary>
    /// An <see cref="AtomTypeMapper"/> allows the mapping of atom types between atom type
    /// schemes. For example, it allows to convert atom types from the CDK scheme to the
    /// Sybyl scheme; using this approach it is possible to use the CDK atom type perception
    /// algorithm and write the resulting atom types using the Sybyl atom type scheme.
    /// </summary>
    // @cdk.module atomtype
    public class AtomTypeMapper
    {
        private static ConcurrentDictionary<string, AtomTypeMapper> mappers = new ConcurrentDictionary<string, AtomTypeMapper>();

        private readonly IReadOnlyDictionary<string, string> mappings;

        private AtomTypeMapper(string mappingFile)
        {
            this.Name = mappingFile;
            using (var reader = new OWLAtomTypeMappingReader(new StreamReader(ResourceLoader.GetAsStream(mappingFile))))
            {
                mappings = reader.ReadAtomTypeMappings();
            }
        }

        private AtomTypeMapper(string mappingFile, Stream stream)
        {
            this.Name = mappingFile;
            using (var reader = new OWLAtomTypeMappingReader(new StreamReader(stream)))
            {
                mappings = reader.ReadAtomTypeMappings();
            }
        }

        /// <summary>
        /// Instantiates an atom type to atom type mapping, based on the given mapping file.
        /// For example, the mapping file NCDK.Config.Data.cdk-sybyl-mappings.owl
        /// which defines how CDK atom types are mapped to Sybyl atom types.
        /// </summary>
        /// <param name="mappingFile">File name of the OWL file defining the atom type to atom type mappings.</param>
        /// <returns>An instance of AtomTypeMapper for the given mapping file.</returns>
        public static AtomTypeMapper GetInstance(string mappingFile)
        {
            return mappers.GetOrAdd(mappingFile, n => new AtomTypeMapper(n));
        }

        /// <summary>
        /// Instantiates an atom type to atom type mapping, based on the given <see cref="Stream"/>.
        /// </summary>
        /// <param name="mappingFile">Name of the <see cref="Stream"/> defining the atom type to atom type mappings.</param>
        /// <param name="stream">the <see cref="Stream"/> from which the mappings as read</param>
        /// <returns>An instance of AtomTypeMapper for the given mapping file.</returns>
        public static AtomTypeMapper GetInstance(string mappingFile, Stream stream)
        {
            return mappers.GetOrAdd(mappingFile, n => new AtomTypeMapper(n, stream));
        }

        /// <summary>
        /// Maps an atom type from one scheme to another, as specified in the input used when creating
        /// this <see cref="AtomTypeMapper"/> instance.
        /// </summary>
        /// <param name="type">atom type to map to the target schema</param>
        /// <returns>atom type name in the target schema</returns>
        public string MapAtomType(string type)
        {
            return mappings[type];
        }

        /// <summary>
        /// Returns the name of this mapping. In case of file inputs, it returns the filename,
        /// but when the input was an <see cref="Stream"/> then the name is less well defined.
        /// </summary>
        /// <returns>the name of the mapping represented by this <see cref="AtomTypeMapper"/>.</returns>
        public string Name { get; }
    }
}
