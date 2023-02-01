/* Copyright (C) 2003-2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sf.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Utils.Xml;
using System.Collections.Generic;
using System.Xml.Linq;
using static NCDK.Config.AtomTypes.OWLConstants;

namespace NCDK.Config.AtomTypes
{
    /// <summary>
    /// Handler for the <see cref="OWLAtomTypeMappingReader"/>.
    /// </summary>
    // @cdk.module  atomtype
    public class OWLAtomTypeMappingHandler : XContentHandler
    {
        private Dictionary<string, string> atomTypeMappings;

        private string fromType;
        private string toType;

        /// <summary>
        /// Constructs a new OWLAtomTypeMappingHandler.
        /// </summary>
        public OWLAtomTypeMappingHandler() { }

        /// <summary>
        /// Returns a <see cref="IReadOnlyDictionary{T, T}"/> with atom type mappings.
        /// </summary>
        /// <returns>a <see cref="IReadOnlyDictionary{T, T}"/> with the atom type name of the source schema as key, and the atom type name of the target schema as values.</returns>
        public IReadOnlyDictionary<string, string> GetAtomTypeMappings()
        {
            return atomTypeMappings;
        }

        // SAX Parser methods

        public override void StartDocument()
        {
            atomTypeMappings = new Dictionary<string, string>();
        }

        public override void EndElement(XElement element)
        {
            if (element.Name.Equals(XName_OWL_Thing) && toType != null && fromType != null)
            {
                atomTypeMappings.Add(fromType, toType);
            } // ignore other namespaces
        }

        public override void StartElement(XElement element)
        {
            var uri = element.Name.Namespace;
            if (element.Name.Equals(XName_OWL_Thing))
            {
                toType = null;
                fromType = element.Attribute(XName_rdf_about).Value;
                fromType = fromType.Substring(fromType.IndexOf('#') + 1);
            }
            else if (element.Name.Equals(XName_AtomTypeMapping_mapsToType) ||
                      element.Name.Equals(XName_AtomTypeMapping_equivalentAsType))
            {
                toType = element.Attribute(XName_rdf_resource).Value;
                toType = toType.Substring(toType.IndexOf('#') + 1);
            } // ignore other namespaces
        }
    }
}
