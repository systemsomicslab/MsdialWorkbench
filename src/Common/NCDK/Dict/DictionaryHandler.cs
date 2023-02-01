/* Copyright (C) 2002-2007  The Chemistry Development Kit (CDK) project
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
using System;
using System.Xml.Linq;

namespace NCDK.Dict
{
    /// <summary>
    /// Class for unmarshalling a dictionary schema file.
    /// </summary>
    // @cdk.module     dict
    public class DictionaryHandler : XContentHandler
    {
        private bool inEntry = false;
        private bool inMetadataList = false;
        Entry entry;

        public DictionaryHandler() { }

        public virtual void DoctypeDecl(string name, string publicId, string systemId) { }
        
        public override void StartDocument()
        {
            Dictionary = new EntryDictionary();
        }

        public override void EndDocument()
        {
        }

        public override void EndElement(XElement element)
        {
            if (string.Equals("entry", element.Name.LocalName, StringComparison.Ordinal) && !("bibtex:entry" == element.Name.ToString()) && inEntry)
            {
                Dictionary.AddEntry(entry);
                inEntry = false;
            }
            else if (string.Equals("metadataList", element.Name.LocalName, StringComparison.Ordinal) && inMetadataList)
            {
                inMetadataList = false;
            }
        }

        public override void StartElement(XElement element)
        {
            if ("entry" == element.Name.LocalName && "bibtex:entry" != element.Name.ToString() && !inEntry)
            {
                inEntry = true;
                entry = new Entry();
                foreach (var att in element.Attributes())
                {
                    switch (att.Name.LocalName)
                    {
                        case "id":
                            entry.Id = att.Value;
                            break;
                        case "term":
                            entry.Label = att.Value;
                            break;
                        default:
                            break;
                    }
                }
            }
            else if ("metadataList" == element.Name.LocalName && !inMetadataList)
            {
                inMetadataList = true;
            }

            // if we're in a metadataList then look at individual
            // metadata nodes and check for any whose content refers
            // to QSAR metadata and save that. Currently it doesn't
            // differentiate between descriptorType or descriptorClass.
            // Do we need to differentiate?
            //
            // RG: I think so and so I save a combination of the dictRef attribute
            // and the content attribute
            else if ("metadata" == element.Name.LocalName && inMetadataList)
            {
                foreach (var att in element.Attributes())
                {
                    string dictRefValue = "";
                    switch (att.Name.LocalName)
                    {
                        case "dictRef":
                            dictRefValue = att.Value;
                            break;
                        case "content":
                            string content = att.Value;
                            if (content.StartsWith("qsar-descriptors-metadata:", StringComparison.Ordinal))
                            {
                                entry.AddDescriptorMetadata(dictRefValue + "/" + content);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public EntryDictionary Dictionary { get; private set; }
    }
}
