/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project.
 */

using System;
using System.Collections.Generic;
using System.Globalization;

namespace NCDK.Beam
{
    /// <summary>
    /// Internal atom implementations.
    /// </summary>
    // @author John May
    internal sealed class AtomImpl
    {
        public sealed class AliphaticSubset : IAtom
        {
            public static readonly AliphaticSubset Any = new AliphaticSubset(Element.Unknown);
            public static readonly AliphaticSubset Boron = new AliphaticSubset(Element.Boron);
            public static readonly AliphaticSubset Carbon = new AliphaticSubset(Element.Carbon);
            public static readonly AliphaticSubset Nitrogen = new AliphaticSubset(Element.Nitrogen);
            public static readonly AliphaticSubset Oxygen = new AliphaticSubset(Element.Oxygen);
            public static readonly AliphaticSubset Sulfur = new AliphaticSubset(Element.Sulfur);
            public static readonly AliphaticSubset Phosphorus = new AliphaticSubset(Element.Phosphorus);
            public static readonly AliphaticSubset Fluorine = new AliphaticSubset(Element.Fluorine);
            public static readonly AliphaticSubset Chlorine = new AliphaticSubset(Element.Chlorine);
            public static readonly AliphaticSubset Bromine = new AliphaticSubset(Element.Bromine);
            public static readonly AliphaticSubset Iodine = new AliphaticSubset(Element.Iodine);

            public static IReadOnlyList<AliphaticSubset> Values = new[]
            {
                Any,
                Boron,
                Carbon,
                Nitrogen,
                Oxygen,
                Sulfur,
                Phosphorus,
                Fluorine,
                Chlorine,
                Bromine,
                Iodine,
            };

            private static readonly Dictionary<Element, IAtom> atoms = new Dictionary<Element, IAtom>();

            static AliphaticSubset()
            {
                foreach (var a in Values)
                    atoms.Add(a.Element, a);
            }

            private AliphaticSubset(Element element)
            {
                this.Element = element;
                this.Token = new Generator.SubsetToken(element.Symbol);
            }

            public int Isotope => -1;

            public Element Element { get; }

            public string Label => Element.Symbol;

            public bool IsAromatic() => false;

            public int Charge => 0;

            public int NumOfHydrogens
            { get { throw new InvalidOperationException("use bond Order sum to determine implicit hydrogen count"); } }

            public int AtomClass => 0;

            public bool Subset => true;

            public IAtom AsAromaticForm()
            {
                return Element.IsAromatic() ? AromaticSubset.OfElement(Element) : this;
            }

            public IAtom AsAliphaticForm() 
            {
                return this;
            }

            public int GetNumberOfHydrogens(Graph g, int u)
            {
                return Element.NumOfImplicitHydrogens(Element, g.BondedValence(u));
            }

            public Generator.AtomToken Token { get; }

            public static IAtom OfElement(Element e)
            {
                if (!atoms.TryGetValue(e, out IAtom a))
                    throw new ArgumentException(e + "can not be an aliphatic subset atom");
                return a;
            }
        }

        public sealed class AromaticSubset : IAtom
        {
            public static readonly AromaticSubset Any = new AromaticSubset(Element.Unknown);
            public static readonly AromaticSubset Boron = new AromaticSubset(Element.Boron);
            public static readonly AromaticSubset Carbon = new AromaticSubset(Element.Carbon);
            public static readonly AromaticSubset Nitrogen = new AromaticSubset(Element.Nitrogen);
            public static readonly AromaticSubset Oxygen = new AromaticSubset(Element.Oxygen);
            public static readonly AromaticSubset Sulfur = new AromaticSubset(Element.Sulfur);
            public static readonly AromaticSubset Phosphorus = new AromaticSubset(Element.Phosphorus);

            public static readonly IEnumerable<AromaticSubset> Values = new[]
            {
                Boron, Carbon, Nitrogen, Oxygen, Sulfur, Phosphorus,
            };

            private Element element;
            private readonly Generator.AtomToken token;

            private static readonly Dictionary<Element, IAtom> atoms = new Dictionary<Element, IAtom>();

            static AromaticSubset()
            {
                foreach (var a in Values)
                    atoms.Add(a.Element, a);
            }

            private AromaticSubset(Element element)
            {
                this.element = element;
                this.token = new Generator.SubsetToken(element.Symbol.ToLowerInvariant());
            }

            public string Label => element.Symbol;

            public int Isotope => -1;

            public Element Element => element;

            public bool IsAromatic() => true;

            public int Charge => 0;

            public int NumOfHydrogens
            { get { throw new InvalidOperationException("use bond Order sum to determine implicit hydrogen count"); } }

            public int AtomClass => 0;

            public Generator.AtomToken Token => token;

            public bool Subset => true;

            public IAtom AsAromaticForm() => this;

            public IAtom AsAliphaticForm() => AliphaticSubset.OfElement(element);

            public int GetNumberOfHydrogens(Graph g, int u)
            {
                int v = g.BondedValence(u);

                // no double, triple or quadruple bonds - then for aromatic atoms
                // we increase the bond Order sum by '1'
                if (v == g.Degree(u))
                    return Element.NumOfAromaticImplicitHydrogens(element, v + 1);

                // note: we only check first valence
                return Element.NumOfAromaticImplicitHydrogens(element, v);
            }

            public static IAtom OfElement(Element e)
            {
                if (!atoms.TryGetValue(e, out IAtom a))
                    throw new ArgumentException(e + "can not be an aromatic subset atom");
                return a;
            }
        }

        public class BracketAtom : IAtom
        {
            private readonly Element element;
            private readonly int hCount, charge, atomClass, isotope;
            private readonly bool aromatic;
            private readonly string label;

            public BracketAtom(int isotope, Element element, int hCount, int charge, int atomClass, bool aromatic)
                : this(isotope, element, element.Symbol, hCount, charge, atomClass, aromatic)
            { }

            public BracketAtom(int isotope, Element element, string label, int hCount, int charge, int atomClass, bool aromatic)
            {
                this.element = element;
                this.label = label;
                this.hCount = hCount;
                this.charge = charge;
                this.atomClass = atomClass;
                this.isotope = isotope;
                this.aromatic = aromatic;
            }

            public BracketAtom(Element element, int hCount, int charge)
                : this(-1, element, hCount, charge, 0, false)
            { }

            public BracketAtom(string label)
               : this(-1, Element.Unknown, label, 0, 0, 0, false)
            { }

            public int Isotope => isotope;

            public Element Element => element;

            public bool IsAromatic() => aromatic;

            public int Charge => charge;

            public string Label => label;

            public int NumOfHydrogens => hCount;

            public int AtomClass => atomClass;

            public Generator.AtomToken Token => new Generator.BracketToken(this);

            public bool Subset => false;

            public int GetNumberOfHydrogens(Graph g, int u)
            {
                return NumOfHydrogens;
            }

            public IAtom AsAromaticForm()
            {
                return aromatic || !element.IsAromatic() ? this
                                                       : new BracketAtom(isotope,
                                                                         element,
                                                                         label,
                                                                         hCount,
                                                                         charge,
                                                                         atomClass,
                                                                         true);
            }

            public IAtom AsAliphaticForm()
            {
                return !aromatic ? this
                                 : new BracketAtom(isotope,
                                                   element,
                                                   label,
                                                   hCount,
                                                   charge,
                                                   atomClass,
                                                   false);
            }

            public override bool Equals(object o)
            {
                var that = o as BracketAtom;
                if (o == null)
                    return false;
                if (aromatic != that.aromatic) return false;
                if (atomClass != that.atomClass) return false;
                if (charge != that.charge) return false;
                if (hCount != that.hCount) return false;
                if (isotope != that.isotope) return false;
                if (element != that.element) return false;
                if (!string.Equals(label, that.label, StringComparison.Ordinal)) return false;

                return true;
            }

            public override int GetHashCode()
            {
                int result = element != null ? element.GetHashCode() : 0;
                result = 31 * result + hCount;
                result = 31 * result + charge;
                result = 31 * result + atomClass;
                result = 31 * result + isotope;
                result = 31 * result + (aromatic ? 1 : 0);
                return result;
            }

            public override string ToString()
            {
                return "[isotope" + element.Symbol + "H"
                    + hCount.ToString(NumberFormatInfo.InvariantInfo)
                    + (charge != 0 ? charge.ToString(NumberFormatInfo.InvariantInfo) : "") 
                    + ":" + atomClass + "]"
                    + (!string.Equals(label, element.Symbol, StringComparison.Ordinal) ? "(" + label + ")" : "");
            }
        }

        public static IAtom EXPLICIT_HYDROGEN = new BracketAtom(Element.Hydrogen, 0, 0);

        public static IAtom DEUTERIUM = AtomBuilder.Aliphatic(Element.Hydrogen)
                                   .Isotope(2)
                                   .Build();

        public static IAtom TRITIUM = AtomBuilder.Aliphatic(Element.Hydrogen)
                                 .Isotope(3)
                                 .Build();
    }
}
