/* Copyright (C) 1997-2007  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Numerics;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Linq;

namespace NCDK.IO.CML
{
    /// <summary>
    /// Implementation of the PMPMol Convention for CML.
    /// </summary>
    /// PMP stands for PolyMorph Predictor and is a module of Cerius2 (tm).
    /// <remarks>
    /// </remarks>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public class PMPConvention : CMLCoreModule
    {
        public PMPConvention(IChemFile chemFile)
            : base(chemFile)
        {
        }

        public PMPConvention(ICMLModule conv)
            : base(conv)
        {
            Debug.WriteLine("New PMP Convention!");
        }

        public override void StartDocument()
        {
            base.StartDocument();
            CurrentChemModel = CurrentChemFile.Builder.NewChemModel();
        }

        public override void StartElement(CMLStack xpath, XElement element)
        {
            Debug.WriteLine("PMP element: name");
            base.StartElement(xpath, element);
        }

        public override void CharacterData(CMLStack xpath, XElement element)
        {
            string s = element.Value.Trim();
            Debug.WriteLine($"Start PMP chardata ({CurrentElement}) :{s}");
            Debug.WriteLine($" ElTitle: {ElementTitle}");
            if (xpath.ToString().EndsWith("string/", StringComparison.Ordinal) && Builtin.Equals("spacegroup", StringComparison.Ordinal))
            {
                string sg = "P1";
                // standardize space group names (see Crystal.java)
                if (string.Equals("P 21 21 21 (1)", s, StringComparison.Ordinal))
                {
                    sg = "P 2_1 2_1 2_1";
                }
                ((ICrystal)CurrentMolecule).SpaceGroup = sg;
            }
            else if (xpath.ToString().EndsWith("floatArray/", StringComparison.Ordinal))
            {
                switch (ElementTitle)
                {
                    case "a":
                    case "b":
                    case "c":
                        var tokens = s.Split(' ');
                        if (tokens.Length > 2)
                        {
                            if (string.Equals(ElementTitle, "a", StringComparison.Ordinal))
                            {
                                ((ICrystal)CurrentMolecule).A = new Vector3(
                                    double.Parse(tokens[0], NumberFormatInfo.InvariantInfo),
                                    double.Parse(tokens[1], NumberFormatInfo.InvariantInfo),
                                    double.Parse(tokens[2], NumberFormatInfo.InvariantInfo));
                            }
                            else if (string.Equals(ElementTitle, "b", StringComparison.Ordinal))
                            {
                                ((ICrystal)CurrentMolecule).B = new Vector3(
                                    double.Parse(tokens[0], NumberFormatInfo.InvariantInfo),
                                    double.Parse(tokens[1], NumberFormatInfo.InvariantInfo),
                                    double.Parse(tokens[2], NumberFormatInfo.InvariantInfo));
                            }
                            else if (string.Equals(ElementTitle, "c", StringComparison.Ordinal))
                            {
                                ((ICrystal)CurrentMolecule).C = new Vector3(
                                    double.Parse(tokens[0], NumberFormatInfo.InvariantInfo),
                                    double.Parse(tokens[1], NumberFormatInfo.InvariantInfo),
                                    double.Parse(tokens[2], NumberFormatInfo.InvariantInfo));
                            }
                        }
                        else
                        {
                            Debug.WriteLine("PMP Convention error: incorrect number of cell axis fractions!");
                        }
                        break;
                }
            }
            else
            {
                base.CharacterData(xpath, element);
            }
            Debug.WriteLine("End PMP chardata");
        }
    }
}
