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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace NCDK.Dict
{
    /// <summary>
    /// Dictionary with entries build from an OWL React.
    /// </summary>
    // @author       Miguel Rojas <miguelrojasch@users.sf.net>
    // @cdk.created  2008-01-01
    // @cdk.keyword  dictionary
    // @cdk.module   dict
    public class OWLReact : EntryDictionary
    {
        private static readonly XNamespace rdfNS = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
        private static readonly XNamespace rdfsNS = "http://www.w3.org/2000/01/rdf-schema#";

        public static new EntryDictionary Unmarshal(TextReader reader)
        {
            EntryDictionary dict = new OWLReact();
            try
            {
                XDocument doc = XDocument.Load(reader);
                XElement root = doc.Root;
                Debug.WriteLine($"Found root element: {root.Name}");

                // Extract ownNS from root element
                //            final string ownNS = root.GetBaseURI();
                string ownNS = root.Attribute("xmlns").Value;
                dict.NS = ownNS;

                Debug.WriteLine($"Found ontology namespace: {ownNS}");

                // process the defined facts
                var entries = root.Elements();
                foreach (var entry in entries)
                {
                    if (entry.Name.NamespaceName.Equals(ownNS, StringComparison.Ordinal))
                    {
                        EntryReact dbEntry = Unmarshal(entry, ownNS);
                        dict.AddEntry(dbEntry);
                        Debug.WriteLine($"Added entry: {dbEntry}");
                    }
                    else
                    {
                        Debug.WriteLine($"Found a non-fact: {entry.Name.ToString()}");
                    }
                }
            }
            catch (XmlException ex)
            {
                Trace.TraceError($"Dictionary is not well-formed: {ex.Message}");
                Debug.WriteLine($"Error at line {ex.LineNumber}, column {ex.LinePosition}");
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

        public static EntryReact Unmarshal(XElement entry, XNamespace ownNS)
        {
            // create a new entry by ID
            XAttribute id = entry.Attribute(rdfNS + "ID");
            Debug.WriteLine($"ID: {id.Value}");
            EntryReact dbEntry = new EntryReact(id.Value);

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
                Debug.WriteLine($"definition name: {definition.Value}");
            }
            XElement description = entry.Element(ownNS + "description");
            if (description != null)
            {
                dbEntry.Description = description.Value;
                Debug.WriteLine($"description name: {description.Value}");
            }
            var representations = entry.Elements(ownNS + "representation");
            foreach (var representation in representations)
            {
                //                string idRepr = representations[i].GetAttributeValue("id");
                string contentRepr = representation.Attribute("content")?.Value;
                dbEntry.AddRepresentation(contentRepr);
            }

            var params_ = entry.Elements(ownNS + "parameters");
            foreach (var p in params_)
            {
                string typeParam = p.Attribute("dataType")?.Value;
                typeParam = typeParam.Substring(typeParam.IndexOf(':') + 1);
                string nameParam = p.Attribute("resource")?.Value;
                string value = p.Value;
                dbEntry.SetParameters(nameParam, typeParam, value);
            }

            var paramsList = entry.Elements(ownNS + "parameterList");
            foreach (var p in paramsList)
            {
                var params2 = p.Elements(ownNS + "parameter2");
                foreach (var p2 in params2)
                {
                    string paramClass = p2.FirstAttribute.Value;
                    paramClass = paramClass.Substring(paramClass.IndexOf('#') + 1);
                    Debug.WriteLine($"parameter class: {paramClass}");

                    string needsToSet = "";
                    string value = "";
                    string dataType = "";
                    var paramSubt1 = p2.Elements(ownNS + "IsSetParameter");

                    needsToSet = paramSubt1.FirstOrDefault()?.Value;
                    if (needsToSet == null) needsToSet = "";

                    var paramSubt2 = p2.Elements(ownNS + "value");
                    var eparamSubt2 = paramSubt2.FirstOrDefault();
                    value = eparamSubt2 == null ? "" : eparamSubt2.Value;
                    dataType = eparamSubt2.Attribute("dataType")?.Value;
                    dataType = dataType.Substring(dataType.IndexOf(':') + 1);
                    var pp = new List<string>
                    {
                        paramClass,
                        needsToSet,
                        dataType,
                        value
                    };
                    dbEntry.AddParameter(pp);
                }
            }

            var mechanismDependence = entry.Elements(ownNS + "mechanismDependence");
            string mechanism = "";
            foreach (var md in mechanismDependence)
            {
                mechanism = md.FirstAttribute.Value;
                mechanism = mechanism.Substring(mechanism.IndexOf('#') + 1);
                Debug.WriteLine($"mechanism name: {mechanism}");
            }

            dbEntry.Mechanism = mechanism;

            var exampleReact = entry.Elements(ownNS + "example-Reactions");
            foreach (var er in exampleReact)
            {
                var reaction = er.Elements(ownNS + "reaction");
                foreach (var r in reaction)
                {
                    dbEntry.AddExampleReaction(r.ToString(SaveOptions.DisableFormatting));
                }
            }
            return dbEntry;
        }
    }
}