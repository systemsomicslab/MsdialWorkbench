/* Copyright (C) 2003-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sf.net
 *
 *  This library is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public
 *  License as published by the Free Software Foundation; either
 *  version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 *  Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with this library; if not, write to the Free Software
 *  Foundation, 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Utils.Xml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace NCDK.Config.Isotopes
{
    /// <summary>
    /// Reads an isotope list in CML2 format. 
    /// </summary>
    /// <remarks>
    /// An example definition is:
    /// <![CDATA[
    /// <isotopeList id = "H">
    ///   <isotope id="H1" isotopeNumber="1" elementTyp="H">
    ///     <abundance dictRef="cdk:relativeAbundance">100.0</abundance>
    ///     <scalar dictRef="cdk:exactMass">1.00782504</scalar>
    ///     <scalar dictRef="cdk:atomicNumber">1</scalar>
    ///   </isotope>
    ///   <isotope id="H2" isotopeNumber="2" elementTyp="H">
    ///     <abundance dictRef="cdk:relativeAbundance">0.015</abundance>
    ///     <scalar dictRef="cdk:exactMass">2.01410179</scalar>
    ///     <scalar dictRef="cdk:atomicNumber">1</scalar>
    ///   </isotope>
    /// </isotopeList>   
    /// ]]>
    /// </remarks>
    // @cdk.module  extra
    public class IsotopeHandler
        : XContentHandler
    {
        private readonly IChemObjectBuilder builder = Silent.ChemObjectBuilder.Instance;

        private List<IIsotope> isotopes;
        private IIsotope workingIsotope;
        private string currentElement;
        private string dictRef;

        /// <summary>
        /// Constructs an IsotopeHandler used by the IsotopeReader.
        /// </summary>
        public IsotopeHandler()
        {
        }

        /// <summary>
        /// The isotopes read from the XML file.
        /// </summary>
        public IReadOnlyList<IIsotope> Isotopes => isotopes;

        // SAX Parser methods

        public override void StartDocument()
        {
            isotopes = new List<IIsotope>();
        }

        public override void EndElement(XElement element)
        {
            Debug.WriteLine($"end element: {element.ToString()}");
            switch (element.Name.LocalName)
            {
                case "isotope":
                    if (workingIsotope != null)
                        isotopes.Add(workingIsotope);
                    workingIsotope = null;
                    break;
                case "isotopeList":
                    currentElement = null;
                    break;
                case "scalar":
                    try
                    {
                        switch (dictRef)
                        {
                            case "bo:exactMass":
                                workingIsotope.ExactMass = double.Parse(element.Value, NumberFormatInfo.InvariantInfo);
                                break;
                            case "bo:atomicNumber":
                                workingIsotope.AtomicNumber = int.Parse(element.Value, NumberFormatInfo.InvariantInfo);
                                break;
                            case "bo:relativeAbundance":
                                workingIsotope.Abundance = double.Parse(element.Value, NumberFormatInfo.InvariantInfo);
                                break;
                            default:
                                break;
                        }
                    }
                    catch (FormatException exception)
                    {
                        Trace.TraceError($"The {dictRef} value is incorrect: {element.Value}");
                        Debug.WriteLine(exception);
                    }
                    break;
                default:
                    break;
            }
        }

        public override void StartElement(XElement element)
        {
            dictRef = "";
            Debug.WriteLine($"startElement: {element.ToString()}");
            switch (element.Name.LocalName)
            {
                case "isotope":
                    workingIsotope = CreateIsotopeOfElement(currentElement, element);
                    break;
                case "isotopeList":
                    currentElement = GetElementSymbol(element);
                    break;
                case "scalar":
                    var att = element.Attribute("dictRef");
                    if (att != null)
                        dictRef = att.Value;
                    break;
                default:
                    break;
            }
        }

        private IIsotope CreateIsotopeOfElement(string currentElement, XElement element)
        {
            var isotope = builder.NewIsotope(currentElement);

            XAttribute att = null;
            try
            {
                att = element.Attribute("id");
                if (att != null)
                    isotope.Id = att.Value;
                att = element.Attribute("number");
                if (att != null)
                    isotope.MassNumber = int.Parse(att.Value, NumberFormatInfo.InvariantInfo);
                att = element.Attribute("elementType");
                    if (att != null)
                        isotope.Symbol = att.Value;
            }
            catch (FormatException exception)
            {
                Trace.TraceError($"Value of isotope {att.Name} is not as expected.");
                Debug.WriteLine(exception);
            }

            // we set the natural abundance to 0, since the default is -1, but
            // some isotope entries have no entry for this field, so the values
            // stays at -1
            isotope.Abundance = 0;

            return isotope;
        }

        private static string GetElementSymbol(XElement element)
        {
            var att = element.Attribute("id");
            return att == null ? "" : att.Value;
        }
    }
}
