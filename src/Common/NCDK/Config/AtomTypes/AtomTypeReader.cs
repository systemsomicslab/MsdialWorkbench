/* Copyright (C) 2002-2007  Egon Willighagen <egonw@users.sf.net>
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
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
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
using System.Globalization;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace NCDK.Config.AtomTypes
{
    /// <summary>
    /// XML Reader for the CDKBasedAtomTypeConfigurator.
    /// </summary>
    /// <seealso cref="CDKBasedAtomTypeConfigurator"/>
    // @cdk.module core
    public class AtomTypeReader : IDisposable
    {
        private readonly IChemObjectBuilder builder = Silent.ChemObjectBuilder.Instance;
        private TextReader input;

        /// <summary>
        /// Instantiates the XML based AtomTypeReader.
        /// </summary>
        /// <param name="input">The Reader to read the IAtomType's from.</param>
        public AtomTypeReader(Stream input)
        {
            this.input = new StreamReader(input);
        }

        /// <summary>
        /// Instantiates the XML based AtomTypeReader.
        /// </summary>
        /// <param name="input">The Reader to read the IAtomType's from.</param>
        public AtomTypeReader(TextReader input)
        {
            this.input = input;
        }

        /// <summary>
        /// Reads the atom types from the data file.
        /// </summary>
        /// <returns><see cref="IEnumerable{T}"/> with atom types. Is empty if some reading error occurred.</returns>
        public IEnumerable<IAtomType> ReadAtomTypes()
        {
            var settings = new XmlReaderSettings
            {
                ValidationType = ValidationType.None,
                ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None
            };
            XmlReader reader = XmlReader.Create(input, settings);

            var doc = XElement.Load(reader);
            var nAtomType = doc.Name.Namespace + "atomType";
            var nAtom = doc.Name.Namespace + "atom";
            var nLabel = doc.Name.Namespace + "label";
            var nScalar = doc.Name.Namespace + "scalar";
            foreach (var elementAtomType in doc.Elements(nAtomType))
            {
                var atomType = builder.NewAtomType("R");
                atomType.AtomTypeName = elementAtomType.Attribute("id")?.Value;
                foreach (var elm in elementAtomType.Descendants())
                {
                    // lazy compare
                    switch (elm.Name.LocalName)
                    {
                        case "atom":
                            atomType.Symbol = elm.Attribute("elementType")?.Value;
                            var sFormalCharge = elm.Attribute("formalCharge")?.Value;
                            if (sFormalCharge != null)
                                atomType.FormalCharge = int.Parse(sFormalCharge, NumberFormatInfo.InvariantInfo);
                            break;
                        case "label":
                            var aValue = elm.Attribute("value");
                            if (aValue != null)
                            {
                                if (atomType.AtomTypeName != null)
                                    atomType.Id = atomType.AtomTypeName;
                                atomType.AtomTypeName = aValue.Value;
                            }
                            break;
                        case "scalar":
                            var dictRef = elm.Attribute("dictRef")?.Value;
                            var value = string.IsNullOrWhiteSpace(elm.Value) ? null : elm.Value;
                            if (value != null)
                            {
                                switch (dictRef)
                                {
                                    case "cdk:bondOrderSum":
                                        atomType.BondOrderSum = double.Parse(value, NumberFormatInfo.InvariantInfo);
                                        break;
                                    case "cdk:maxBondOrder":
                                        atomType.MaxBondOrder = (BondOrder)(int)double.Parse(value, NumberFormatInfo.InvariantInfo);
                                        break;
                                    case "cdk:formalNeighbourCount":
                                        atomType.FormalNeighbourCount = (int)double.Parse(value, NumberFormatInfo.InvariantInfo);
                                        break;
                                    case "cdk:valency":
                                        atomType.Valency = (int)double.Parse(value, NumberFormatInfo.InvariantInfo);
                                        break;
                                    case "cdk:formalCharge":
                                        atomType.FormalCharge = (int)double.Parse(value, NumberFormatInfo.InvariantInfo);
                                        break;
                                    case "cdk:hybridization":
                                        atomType.Hybridization = HybridizationTools.ToHybridization(value);
                                        break;
                                    case "cdk:DA":
                                        switch (value)
                                        {
                                            case "A":
                                                atomType.IsHydrogenBondAcceptor |= true;
                                                break;
                                            case "D":
                                                atomType.IsHydrogenBondDonor |= true;
                                                break;
                                            default:
                                                Trace.TraceWarning($"Unrecognized H-bond donor/acceptor pattern in configuration file: {value}");
                                                break;
                                        }
                                        break;
                                    case "cdk:sphericalMatcher":
                                        atomType.SetProperty(CDKPropertyName.SphericalMatcher, value);
                                        break;
                                    case "cdk:ringSize":
                                        atomType.SetProperty(CDKPropertyName.PartOfRingOfSize, int.Parse(value, NumberFormatInfo.InvariantInfo));
                                        break;
                                    case "cdk:ringConstant":
                                        atomType.SetProperty(CDKPropertyName.ChemicalGroupConstant, int.Parse(value, NumberFormatInfo.InvariantInfo));
                                        break;
                                    case "cdk:aromaticAtom":
                                        atomType.IsAromatic |= true;
                                        break;
                                    case "emboss:vdwrad":
                                        break;
                                    case "cdk:piBondCount":
                                        atomType.SetProperty(CDKPropertyName.PiBondCount, int.Parse(value, NumberFormatInfo.InvariantInfo));
                                        break;
                                    case "cdk:lonePairCount":
                                        atomType.SetProperty(CDKPropertyName.LonePairCount, int.Parse(value, NumberFormatInfo.InvariantInfo));
                                        break;
                                    default:
                                        break;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                yield return atomType;
            }
            yield break;
       }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    input.Dispose();
                }

                input = null;

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
