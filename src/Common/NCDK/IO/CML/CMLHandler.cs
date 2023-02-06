/* Copyright (C) 1997-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Utils.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using static NCDK.LibIO.CML.CMLElement;

namespace NCDK.IO.CML
{
    /// <summary>
    /// SAX2 implementation for CML XML fragment reading. CML Core is supported as well is the CRML module.
    /// <para>Data is stored into the Chemical Document Object which is passed when
    /// instantiating this class. This makes it possible that programs that do not
    /// use CDK for internal data storage, use this CML library.
    /// </para>
    /// </summary>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public class CMLHandler : XContentHandler
    {
        private ICMLModule conv;

        private Dictionary<string, ICMLModule> userConventions;

        private CMLStack xpath;
        private CMLStack conventionStack;
        private CMLModuleStack moduleStack;
        
        /// <summary>
        /// Constructor for the CMLHandler.
        /// </summary>
        /// <param name="chemFile">The document in which data is stored</param>
        public CMLHandler(IChemFile chemFile)
        {
            conv = new CMLCoreModule(chemFile);
            userConventions = new Dictionary<string, ICMLModule>();
            xpath = new CMLStack();
            conventionStack = new CMLStack();
            moduleStack = new CMLModuleStack();
        }

        public void RegisterConvention(string convention, ICMLModule conv)
        {
            userConventions[convention] = conv;
        }

        /// <summary>
        /// Implementation of the characters() procedure overwriting the DefaultHandler interface.
        /// </summary>
        /// <param name="element">element to handle</param>
        public override void CharacterData(XElement element)
        {
            Debug.WriteLine(element.Value);
            conv.CharacterData(xpath, element);
        }

        public static void DoctypeDecl(string name, string publicId, string systemId) { }

        /// <summary>
        /// Calling this procedure signals the end of the XML document.
        /// </summary>
        public override void EndDocument()
        {
            conv.EndDocument();
        }

        public override void EndElement(XElement element)
        {
            Debug.WriteLine($"</{element.Value}>");
            conv.EndElement(xpath, element);
            xpath.Pop();
            conventionStack.Pop();
            moduleStack.Pop();
            conv = moduleStack.Current;
        }

        public override void StartDocument()
        {
            conv.StartDocument();
            conventionStack.Push("CML");
            moduleStack.Push(conv);
        }

        public override void StartElement(XElement element)
        {
            var uri = element.Name.NamespaceName;
            var local = element.Name.LocalName;

            xpath.Push(element.Name.LocalName);
            Debug.WriteLine($"<{element.Value}> -> {xpath}");
            // Detect CML modules, like CRML and CCML
            if (local.StartsWith("reaction", StringComparison.Ordinal))
            {
                // e.g. reactionList, reaction -> CRML module
                Trace.TraceInformation("Detected CRML module");
                if (!string.Equals(conventionStack.Peek(), "CMLR", StringComparison.Ordinal))
                {
                    conv = new CMLReactionModule(conv);
                }
                conventionStack.Push("CMLR");
            }
            else if (string.IsNullOrEmpty(uri) || uri.StartsWith("http://www.xml-cml.org/", StringComparison.Ordinal))
            {
                // assume CML Core

                // Detect conventions
                string convName = "";
                if (element.Attribute(Attribute_convention) != null)
                {
                    convName = element.Attribute(Attribute_convention).Value;
                }
                if (convName.Length == 0)
                {
                    // no convention set/reset: take convention of parent
                    conventionStack.Push(conventionStack.Peek());
                }
                else
                if (convName.Length > 0)
                {
                    if (convName.Equals(conventionStack.Peek(), StringComparison.Ordinal))
                    {
                        Debug.WriteLine("Same convention as parent");
                    }
                    else
                    {
                        Trace.TraceInformation("New Convention: ", convName);
                        switch (convName)
                        {
                            case "CML":
                                // Don't reset the convention handler to CMLCore,
                                // becuase all handlers should extend this handler, and
                                // use it for any content other then specifically put
                                // into the specific convention
                                break;
                            case "PDB":
                                conv = new PDBConvention(conv);
                                break;
                            case "PMP":
                                conv = new PMPConvention(conv);
                                break;
                            case "MDLMol":
                                Debug.WriteLine("MDLMolConvention instantiated...");
                                conv = new MDLMolConvention(conv);
                                break;
                            case "JMOL-ANIMATION":
                                conv = new JMOLANIMATIONConvention(conv);
                                break;
                            default:
                                if (userConventions.ContainsKey(convName))
                                {
                                    //unknown convention. userConvention?
                                    ICMLModule newconv = (ICMLModule)userConventions[convName];
                                    newconv.Inherit(conv);
                                    conv = newconv;
                                }
                                else
                                {
                                    Trace.TraceWarning($"Detected unknown convention: {convName}");
                                }
                                break;
                        }
                    }
                    conventionStack.Push(convName);
                }
                else
                {
                    // no convention set/reset: take convention of parent
                    conventionStack.Push(conventionStack.Peek());
                }
            }
            else
            {
                conv = new OtherNamespace();
                conventionStack.Push("Other");
            }
            moduleStack.Push(conv);
            Debug.WriteLine($"ConventionStack: {conventionStack}");
            conv.StartElement(xpath, element);
        }
    }
}
