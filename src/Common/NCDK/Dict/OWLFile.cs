/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace NCDK.Dict
{
    /// <summary>
    /// Dictionary with entries build from an OWL file.
    /// </summary>
    // @author       Egon Willighagen <egonw@users.sf.net>
    // @cdk.created  2005-11-18
    // @cdk.keyword  dictionary
    // @cdk.module   dict
    public class OWLFile : EntryDictionary
    {
        private static readonly XNamespace rdfNS = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        private static readonly XNamespace rdfsNS = "http://www.w3.org/2000/01/rdf-schema#";

        public OWLFile()
            : base()
        { }

        public new static EntryDictionary Unmarshal(TextReader reader)
        {
            EntryDictionary dict = new OWLFile();
            try
            {
                var text = reader.ReadToEnd();

                var doc = XDocument.Parse(text);
                var root = doc.Root;
                Debug.WriteLine($"Found root element: {root.Name}");

                // Extract ownNS from root element
                //            final string ownNS = root.GetBaseURI();
                string ownNS = root.Attribute("xmlns").Value;
                dict.NS = ownNS;

                Debug.WriteLine($"Found ontology namespace: {ownNS}");

                // process the defined facts
                var entries = root.Elements();
                //Trace.TraceInformation("Found #elements in OWL dict:", entries.Count());
                foreach (var entry in entries)
                {
                    if (entry.Name.NamespaceName.Equals(ownNS, StringComparison.Ordinal))
                    {
                        Entry dbEntry = Unmarshal(entry, ownNS);
                        dict.AddEntry(dbEntry);
                        Debug.WriteLine($"Added entry: {dbEntry}");
                    }
                    else
                    {
                        Debug.WriteLine($"Found a non-fact: {entry.Name}");
                    }
                }
            }
            catch (XmlException ex)
            {
                Trace.TraceError($"Dictionary is not well-formed: {ex.Message}");
                Debug.WriteLine("Error at line " + ex.LineNumber, ", column " + ex.LinePosition);
                dict = null;
            }
            catch (IOException ex)
            {
                Trace.TraceError($"Due to an IOException, the parser could not check:{ex.Message}");
                Debug.WriteLine(ex);
                dict = null;
            }
            return dict;
        }

        public static Entry Unmarshal(XElement entry, XNamespace ownNS)
        {
            // create a new entry by ID
            XAttribute id = entry.Attribute(rdfNS + "ID");
            Debug.WriteLine($"ID: {id.Value}");
            Entry dbEntry = new Entry(id.Value);

            // set additional, optional data
            XElement label = entry.Element(rdfsNS + "label");
            Debug.WriteLine($"label: {label}");
            if (label != null) dbEntry.Label = label.Value;

            dbEntry.ClassName = entry.Name.LocalName;
            Debug.WriteLine($"class name: {dbEntry.ClassName}");

            XElement definition = entry.Element(ownNS + "definition");
            if (definition != null)
            {
                dbEntry.Definition = definition.Value;
            }
            XElement description = entry.Element(ownNS + "description");
            if (description != null)
            {
                dbEntry.Description = description.Value;
            }

            if (entry.Name.LocalName == "Descriptor") dbEntry.RawContent = entry;

            return dbEntry;
        }
    }
}
