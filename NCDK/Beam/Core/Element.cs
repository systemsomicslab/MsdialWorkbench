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

using NCDK.Common.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NCDK.Beam
{
    /// <summary>
    /// Enumeration of valid OpenSMILES elements.
    /// </summary>
    /// <remarks>
    /// <para>Organic subsets</para>
    /// <para>
    /// Several of the elements belong to the organic
    /// subset. Atoms of an organic element type can be written just as their symbol
    /// (see. <see href="http://www.opensmiles.org/opensmiles.html#orgsbst">Organic Subset, OpenSMILES Specification</see>).
    /// </para>
    /// <list type="bullet">
    /// <item><see cref="Unknown"/> (*)</item> 
    /// <item><see cref="Boron"/></item>
    /// <item><see cref="Carbon"/></item>
    /// <item><see cref="Nitrogen"/></item>
    /// <item><see cref="Oxygen"/></item>
    /// <item><see cref="Fluorine"/></item> 
    /// <item><see cref="Phosphorus"/></item>
    /// <item><see cref="Sulfur"/></item> 
    /// <item><see cref="Chlorine"/></item>
    /// <item><see cref="Bromine"/></item> 
    /// <item><see cref="Iodine"/></item> 
    /// </list>
    /// </remarks>
    /// <example>
    /// <para>Usage</para>
    /// 
    /// Elements can be created by either using the value directly or by looking up
    /// it's symbol. If the element may be aromatic the lower-case symbol can also be
    /// used. For example the variable 'e' in the three statements below all have the
    /// same value, <see cref="Element.Carbon"/>.
    /// 
    /// <code>
    /// Element e = Element.Carbon;
    /// Element e = ChemicalElement.OfSymbol("C");
    /// Element e = ChemicalElement.OfSymbol("c");
    /// </code>
    /// 
    /// When the symbol is invalid the result wil be null.
    /// <code>
    /// Element e = ChemicalElement.OfSymbol("R1"); // e = null
    /// </code>
    /// 
    /// The <see cref="Element.Unknown"/> element can be used to represent generic/alias
    /// atoms.
    /// <code>
    /// Element e = Element.Unknown;
    /// Element e = ChemicalElement.OfSymbol("*");
    /// </code>
    /// 
    /// To access the symbol of an already created element. Use <see cref="Element.Symbol"/>.
    /// 
    /// <code>
    /// IAtom a = ...;
    /// Element e = a.Element;
    /// 
    /// string symbol = e.Symbol;
    /// </code>
    /// </example>
    /// <seealso href="http://www.opensmiles.org/opensmiles.html#inatoms">Atoms, OpenSMILES Specification</seealso>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    class Element
    {
        /// <summary>Unspecified/Unknown element (*)</summary>
        public static readonly Element Unknown = new Element(0, "*", 0);

        public static readonly Element Hydrogen = new Element(1, "H");
        public static readonly Element Helium = new Element(2, "He");

        public static readonly Element Lithium = new Element(3, "Li");
        public static readonly Element Beryllium = new Element(4, "Be");
        public static readonly Element Boron = new Element(5, "B", 3);
        public static readonly Element Carbon = new Element(6, "C", 4);
        public static readonly Element Nitrogen = new Element(7, "N", 3, 5);
        public static readonly Element Oxygen = new Element(8, "O", 2);
        public static readonly Element Fluorine = new Element(9, "F", 1);
        public static readonly Element Neon = new Element(10, "Ne");

        public static readonly Element Sodium = new Element(11, "Na");
        public static readonly Element Magnesium = new Element(12, "Mg");
        public static readonly Element Aluminium = new Element(13, "Al");
        public static readonly Element Silicon = new Element(14, "Si");
        public static readonly Element Phosphorus = new Element(15, "P", 3, 5);
        public static readonly Element Sulfur = new Element(16, "S", 2, 4, 6);
        public static readonly Element Chlorine = new Element(17, "Cl", 1);
        public static readonly Element Argon = new Element(18, "Ar");

        public static readonly Element Potassium = new Element(19, "K");
        public static readonly Element Calcium = new Element(20, "Ca");
        public static readonly Element Scandium = new Element(21, "Sc");
        public static readonly Element Titanium = new Element(22, "Ti");
        public static readonly Element Vanadium = new Element(23, "V");
        public static readonly Element Chromium = new Element(24, "Cr");
        public static readonly Element Manganese = new Element(25, "Mn");
        public static readonly Element Iron = new Element(26, "Fe");
        public static readonly Element Cobalt = new Element(27, "Co");
        public static readonly Element Nickel = new Element(28, "Ni");
        public static readonly Element Copper = new Element(29, "Cu");
        public static readonly Element Zinc = new Element(30, "Zn");
        public static readonly Element Gallium = new Element(31, "Ga");
        public static readonly Element Germanium = new Element(32, "Ge");
        public static readonly Element Arsenic = new Element(33, "As");
        public static readonly Element Selenium = new Element(34, "Se");
        public static readonly Element Bromine = new Element(35, "Br", 1);
        public static readonly Element Krypton = new Element(36, "Kr");

        public static readonly Element Rubidium = new Element(37, "Rb");
        public static readonly Element Strontium = new Element(38, "Sr");
        public static readonly Element Yttrium = new Element(39, "Y");
        public static readonly Element Zirconium = new Element(40, "Zr");
        public static readonly Element Niobium = new Element(41, "Nb");
        public static readonly Element Molybdenum = new Element(42, "Mo");
        public static readonly Element Technetium = new Element(43, "Tc");
        public static readonly Element Ruthenium = new Element(44, "Ru");
        public static readonly Element Rhodium = new Element(45, "Rh");
        public static readonly Element Palladium = new Element(46, "Pd");
        public static readonly Element Silver = new Element(47, "Ag");
        public static readonly Element Cadmium = new Element(48, "Cd");
        public static readonly Element Indium = new Element(49, "In");
        public static readonly Element Tin = new Element(50, "Sn");
        public static readonly Element Antimony = new Element(51, "Sb");
        public static readonly Element Tellurium = new Element(52, "Te");
        public static readonly Element Iodine = new Element(53, "I", 1);
        public static readonly Element Xenon = new Element(54, "Xe");

        public static readonly Element Cesium = new Element(55, "Cs");
        public static readonly Element Barium = new Element(56, "Ba");
        // f-block = new Element (see below)
        public static readonly Element Lutetium = new Element(71, "Lu");
        public static readonly Element Hafnium = new Element(72, "Hf");
        public static readonly Element Tantalum = new Element(73, "Ta");
        public static readonly Element Tungsten = new Element(74, "W");
        public static readonly Element Rhenium = new Element(75, "Re");
        public static readonly Element Osmium = new Element(76, "Os");
        public static readonly Element Iridium = new Element(77, "Ir");
        public static readonly Element Platinum = new Element(78, "Pt");
        public static readonly Element Gold = new Element(79, "Au");
        public static readonly Element Mercury = new Element(80, "Hg");
        public static readonly Element Thallium = new Element(81, "Tl");
        public static readonly Element Lead = new Element(82, "Pb");
        public static readonly Element Bismuth = new Element(83, "Bi");
        public static readonly Element Polonium = new Element(84, "Po");
        public static readonly Element Astatine = new Element(85, "At");
        public static readonly Element Radon = new Element(86, "Rn");

        public static readonly Element Francium = new Element(87, "Fr");
        public static readonly Element Radium = new Element(88, "Ra");
        // f-block = new Element (see below)
        public static readonly Element Lawrencium = new Element(103, "Lr");
        public static readonly Element Rutherfordium = new Element(104, "Rf");
        public static readonly Element Dubnium = new Element(105, "Db");
        public static readonly Element Seaborgium = new Element(106, "Sg");
        public static readonly Element Bohrium = new Element(107, "Bh");
        public static readonly Element Hassium = new Element(108, "Hs");
        public static readonly Element Meitnerium = new Element(109, "Mt");
        public static readonly Element Darmstadtium = new Element(110, "Ds");
        public static readonly Element Roentgenium = new Element(111, "Rg");
        public static readonly Element Copernicium = new Element(112, "Cn");
        public static readonly Element Nihonium = new Element(113, "Nh");
        public static readonly Element Flerovium = new Element(114, "Fl");
        public static readonly Element Moscovium = new Element(115, "Mc");
        public static readonly Element Livermorium = new Element(116, "Lv");
        public static readonly Element Tennessine = new Element(117, "Ts");
        public static readonly Element Oganesson = new Element(118, "Og");

        public static readonly Element Lanthanum = new Element(57, "La");
        public static readonly Element Cerium = new Element(58, "Ce");
        public static readonly Element Praseodymium = new Element(59, "Pr");
        public static readonly Element Neodymium = new Element(60, "Nd");
        public static readonly Element Promethium = new Element(61, "Pm");
        public static readonly Element Samarium = new Element(62, "Sm");
        public static readonly Element Europium = new Element(63, "Eu");
        public static readonly Element Gadolinium = new Element(64, "Gd");
        public static readonly Element Terbium = new Element(65, "Tb");
        public static readonly Element Dysprosium = new Element(66, "Dy");
        public static readonly Element Holmium = new Element(67, "Ho");
        public static readonly Element Erbium = new Element(68, "Er");
        public static readonly Element Thulium = new Element(69, "Tm");
        public static readonly Element Ytterbium = new Element(70, "Yb");

        public static readonly Element Actinium = new Element(89, "Ac");
        public static readonly Element Thorium = new Element(90, "Th");
        public static readonly Element Protactinium = new Element(91, "Pa");
        public static readonly Element Uranium = new Element(92, "U");
        public static readonly Element Neptunium = new Element(93, "Np");
        public static readonly Element Plutonium = new Element(94, "Pu");
        public static readonly Element Americium = new Element(95, "Am");
        public static readonly Element Curium = new Element(96, "Cm");
        public static readonly Element Berkelium = new Element(97, "Bk");
        public static readonly Element Californium = new Element(98, "Cf");
        public static readonly Element Einsteinium = new Element(99, "Es");
        public static readonly Element Fermium = new Element(100, "Fm");
        public static readonly Element Mendelevium = new Element(101, "Md");
        public static readonly Element Nobelium = new Element(102, "No");

        /// <summary>Atomic number of the elemnt.</summary>
        private readonly int atomicNumber;

        /// <summary>The symbol of the element.</summary>
        private readonly string symbol;

        /// <summary>
        /// Default valence information - only present if the atom is part of the
        /// organic subset.
        /// </summary>
        private readonly int[] valence;

        private readonly int[] electrons;

        /// <summary>Look up of elements by symbol</summary>
        private static readonly Dictionary<string, Element> elementMap = new Dictionary<string, Element>();

        private static readonly Element[] elements = new Element[119];

        /// <summary>Lookup elements by atomic number.</summary>
        public static Element[] Values { get; } = new Element[]
        {
        #region Values
            Unknown,
            Hydrogen,
            Helium,
            Lithium,
            Beryllium,
            Boron,
            Carbon,
            Nitrogen,
            Oxygen,
            Fluorine,
            Neon,
            Sodium,
            Magnesium,
            Aluminium,
            Silicon,
            Phosphorus,
            Sulfur,
            Chlorine,
            Argon,
            Potassium,
            Calcium,
            Scandium,
            Titanium,
            Vanadium,
            Chromium,
            Manganese,
            Iron,
            Cobalt,
            Nickel,
            Copper,
            Zinc,
            Gallium,
            Germanium,
            Arsenic,
            Selenium,
            Bromine,
            Krypton,
            Rubidium,
            Strontium,
            Yttrium,
            Zirconium,
            Niobium,
            Molybdenum,
            Technetium,
            Ruthenium,
            Rhodium,
            Palladium,
            Silver,
            Cadmium,
            Indium,
            Tin,
            Antimony,
            Tellurium,
            Iodine,
            Xenon,
            Cesium,
            Barium,
            Lanthanum,
            Cerium,
            Praseodymium,
            Neodymium,
            Promethium,
            Samarium,
            Europium,
            Gadolinium,
            Terbium,
            Dysprosium,
            Holmium,
            Erbium,
            Thulium,
            Ytterbium,
            Lutetium,
            Hafnium,
            Tantalum,
            Tungsten,
            Rhenium,
            Osmium,
            Iridium,
            Platinum,
            Gold,
            Mercury,
            Thallium,
            Lead,
            Bismuth,
            Polonium,
            Astatine,
            Radon,
            Francium,
            Radium,
            Actinium,
            Thorium,
            Protactinium,
            Uranium,
            Neptunium,
            Plutonium,
            Americium,
            Curium,
            Berkelium,
            Californium,
            Einsteinium,
            Fermium,
            Mendelevium,
            Nobelium,
            Lawrencium,
            Rutherfordium,
            Dubnium,
            Seaborgium,
            Bohrium,
            Hassium,
            Meitnerium,
            Darmstadtium,
            Roentgenium,
            Copernicium,
            Nihonium,
            Flerovium,
            Moscovium,
            Livermorium,
            Tennessine,
            Oganesson,
        #endregion
        };

        /// <summary>Provide verification of valence/charge values.</summary>
        private ElementCheck defaults = ElementCheck.NoCheck;

        static Element()
        {
            foreach (var element in Values)
            {
                elementMap[element.Symbol.ToLowerInvariant()] = element;
                elementMap[element.Symbol] = element;
                elements[element.AtomicNumber] = element;
            }

            // load normal ranges from 'element-defaults.txt' and set for the
            // elements
            foreach (var e in LoadDefaults())
            {
                elementMap[e.Key].defaults = e.Value;
            }
        }

        private Element(int atomicNumber, string symbol)
           : this(atomicNumber, symbol, null)
        {
        }

        private Element(int atomicNumber,
                        string symbol,
                        params int[] valence)
        {
            this.atomicNumber = atomicNumber;
            this.symbol = symbol;
            this.valence = valence;
            if (valence != null)
            {
                this.electrons = new int[valence.Length];
                for (int i = 0; i < valence.Length; i++)
                {
                    electrons[i] = valence[i] * 2;
                }
            }
            else
            {
                this.electrons = null;
            }
        }

        /// <summary>
        /// The symbol of the element.
        /// </summary>
        public string Symbol => symbol;

        /// <summary>
        /// The atomic number of the element. If the element is Unknown '0' is returned.
        /// </summary>
        public int AtomicNumber => atomicNumber;

        /// <summary>
        /// Can the element be aromatic. This definition is very loose and includes
        /// elements which are not part of the Daylight, OpenSMILES specification. To
        /// test if ane element is aromatic by the specification use 
        /// <see cref="IsAromatic(AromaticSpecification)"/>.
        /// </summary>
        /// <returns>whether the element may be aromatic</returns>
        public bool IsAromatic()
            => IsAromatic(AromaticSpecification.General);

        /// <summary>
        /// Can the element be aromatic in accordance with a given specification.
        /// </summary>
        /// <param name="spec">such <see cref="Element.AromaticSpecification.Daylight"/>,
        /// <see cref="Element.AromaticSpecification.OpenSmiles"/></param>
        /// <returns>the element is accepted as being aromatic by that scheme</returns>
        public bool IsAromatic(AromaticSpecification spec)
        {
            return spec.Contains(this);
        }

        /// <summary>
        /// Is the element a member of the organic subset and can be written without
        /// brackets. If the element is both organic and aromatic is a member of the
        /// aromatic subset and can still be written without brackets.
        /// </summary>
        /// <returns>the element can be written without brackets</returns>
        public bool IsOrganic()
        {
            return valence != null;
        }

        /// <summary>
        /// Determine the number of implied hydrogens an organic (or aromatic) subset
        /// atom has based on it's bond Order sum. The valances for the organic
        /// elements (B, C, N, O, P, S, F, Cl, Br and I) are defined in the
        /// OpenSMILES specification.
        /// </summary>
        /// <param name="v"></param>
        /// <returns>the number of implied hydrogens</returns>
        /// <exception cref="InvalidOperationException">
        /// the element was not a member of the
        /// organic subset and did not have default valence information
        /// </exception>
        [Obsolete]
        public int NumOfImplicitHydrogens(int v)
        {
            return NumOfImplicitHydrogens(this, v);
        }

        [Obsolete]
        public int NumOfAromaticImplicitHydrogens(int v)
        {
            return NumOfAromaticImplicitHydrogens(this, v);
        }

        /// <summary>
        /// Determine the number of available electrons which could be bonding to
        /// implicit hydrogens. This include electrons donated from the hydrogen.
        /// </summary>
        /// <remarks>
        /// The central carbon of "C-C=C" 6 bonded electrons - using SMILES
        /// default valence there must be 2 electrons involved in bonding an implicit
        /// hydrogen (i.e. there is a single bond to a hydrogen).
        /// </remarks>
        /// <param name="bondElectronSum">the sum of the bonded electrons</param>
        /// <returns>number of electrons which could be involved with bonds to hydrogen</returns>
        public int NumOfAvailableElectrons(int bondElectronSum)
        {
            foreach (var e in electrons)
                if (bondElectronSum <= e)
                    return e - bondElectronSum;
            return 0;
        }

        /// <summary>
        /// Determine the number of available electrons which could be bonding to
        /// implicit hydrogens for an aromatic atom with delocalized bonds. This
        /// include electrons donated from the hydrogen.
        /// </summary>
        /// <remarks>
        /// Instead of checking higher valence states only the lowest is checked. For
        /// example nitrogen has valence 3 and 5 but in a delocalized system only the
        /// lowest (3) is used. The electrons which would allow bonding of implicit
        /// hydrogens in the higher valence states are donated to the aromatic system
        /// and thus cannot be <i>reached</i>. Using a generalisation that an
        /// aromatic bond as 3 electrons we reached the correct value for multi
        /// valence aromatic elements.
        ///
        /// <para>
        /// <list type="table">
        /// <item>
        /// <term>c1c[nH]cn1</term>
        /// <term>
        /// the aromatic subset nitrogen is bonded to two aromatic
        /// nitrogen bond Order sum of 3 (6 electrons) there are
        /// no implicit hydrogens
        /// </term>
        /// </item>
        /// <item>
        /// <term>c1cc2ccccn2c1</term>
        /// <term>
        /// the nitrogen has three aromatic bond 4.5 bond Order
        /// (9 electrons) - as we only check the lowest valence
        /// (3 - 4.5) &lt; 0 so there are 0 implicit hydrogens
        /// </term>
        /// </item>
        /// <item>
        /// <term>c1ccpcc1</term>
        /// <term>
        /// the phosphorus has 2 aromatic bond (bond Order sum 3)
        /// and the lowest valence is '3' - there are no implicit
        /// hydrogens
        /// </term>
        /// </item>
        /// <item>
        /// <term>oc1ccscc1</term>
        /// <term>
        /// the sulphur has two aromatic bonds (bond Order sum 3)
        /// the lowest valence is '2' - 3 &gt; 2 so there are no
        /// implicit hydrogens
        /// </term>
        /// </item>
        /// <item>
        /// <term>oc1ccscc1</term>
        /// <term>
        /// the oxygen has a single aromatic bond, the default
        /// valence of oxygen in the specification is '2' there
        /// are no hydrogens (2 - 1.5 = 0.5).
        /// </term>
        /// </item>
        /// </list>                  
        /// </para>
        /// </remarks>
        /// <param name="bondElectronSum">the sum of the bonded electrons</param>
        /// <returns>number of electrons which could be involved with bonds to hydrogen</returns>
        public int NumOfAvailableDelocalisedElectrons(int bondElectronSum)
        {
            if (bondElectronSum <= electrons[0])
                return electrons[0] - bondElectronSum;
            return 0;
        }

        /// <summary>
        /// Verify whether the given valence and charge are 'normal' for the
        /// element.
        /// </summary>
        /// <param name="v">valence (bond Order Order sum)</param>
        /// <param name="q">charge</param>
        /// <returns>whether the valence and charge are valid</returns>
        public bool Verify(int v, int q)
        {
            // table driven verification (see. element-defaults.txt)
            return defaults.Verify(v, q);
        }

        /// <summary>
        /// Given an element symbol, provide the element for that symbol. If no
        /// symbol was found then null is returned.
        /// </summary>
        /// <param name="symbol">the element symbol</param>
        /// <returns>element for the symbol, or null if none found</returns>
        public static Element OfSymbol(string symbol)
        {
            if (symbol == null)
                return null;
            if (!elementMap.TryGetValue(symbol, out Element ret))
                ret = null;
            return ret;
        }

        /// <summary>
        /// Access an element by atomic number.
        /// </summary>
        /// <param name="elem">atomic number</param>
        /// <returns>the element for the atomic number</returns>
        public static Element OfNumber(int elem)
        {
            return elements[elem];
        }

        /// <summary>
        /// Read an element and progress the character buffer. If the element was not
        /// read then a 'null' element is returned.
        /// </summary>
        /// <param name="buffer">a character buffer</param>
        /// <returns>the element, or null</returns>
        internal static Element Read(CharBuffer buffer)
        {
            if (!buffer.HasRemaining())
                return null;
            char c = buffer.Get();
            string cs;
            if (buffer.HasRemaining() && buffer.NextChar >= 'a' && buffer
                    .NextChar <= 'z')
                cs = new string(new char[] { c, buffer.Get() });
            else
                cs = char.ToString(c);
            if (!elementMap.TryGetValue(cs, out Element ret))
                ret = null;
            return ret;
        }

        static Dictionary<string, ElementCheck> LoadDefaults()
        {
            var checks = new Dictionary<string, ElementCheck>();
            try
            {
                using (var br = new StreamReader(ResourceLoader.GetAsStream(typeof(Element), "element-defaults.txt")))
                {
                    string line = null;
                    while ((line = br.ReadLine()) != null)
                    {
                        if (line.Length == 0 || line[0] == '-') // empty line or comment
                            continue;
                        KeyValuePair<string, ElementCheck> entry = Load(line);
                        checks.Add(entry.Key, entry.Value);
                    }
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("error whilst loading element-defaults.txt: " + e);
            }
            return checks;
        }

        static KeyValuePair<string, ElementCheck> Load(string line)
        {
            var data = Strings.Tokenize(line);
            var symbol = data[0];
            var electrons = int.Parse(data[3], NumberFormatInfo.InvariantInfo);
            var valenceCheck = ValenceCheck.Parse(data[1], electrons);
            var chargeCheck = ChargeCheck.Parse(data[2]);
            return new KeyValuePair<string, ElementCheck>(symbol, new ElementCheck(valenceCheck, chargeCheck));
        }

        private sealed class ElementCheck
        {
            private readonly ValenceCheck valenceCheck;
            private readonly ChargeCheck chargeCheck;

            public ElementCheck(ValenceCheck valenceCheck, ChargeCheck chargeCheck)
            {
                this.valenceCheck = valenceCheck;
                this.chargeCheck = chargeCheck;
            }

            public bool Verify(int v, int q)
            {
                return chargeCheck.Verify(q) && valenceCheck.Verify(v, q);
            }

            public static readonly ElementCheck NoCheck = new ElementCheck(NoValenceCheck.Instance, ChargeCheck.None);

            public override string ToString()
            {
                return chargeCheck + ", " + valenceCheck;
            }
        }

        abstract class ValenceCheck
        {
            public abstract bool Verify(int v, int q);

            public static ValenceCheck Parse(string line, int nElectrons)
            {
                string[] vs = line.Split(',');
                if (vs.Length == 1)
                {
                    if (string.Equals(vs[0], "n/a", StringComparison.Ordinal))
                    {
                        return NoValenceCheck.Instance;
                    }
                    else if (vs[0][0] == '(')
                    {
                        return new FixedValence(int.Parse(vs[0].Substring(1, vs[0].Length - 2), NumberFormatInfo.InvariantInfo));
                    }
                    else if (vs[0][0] == '[')
                    {
                        return new NeutralValence(int.Parse(vs[0].Substring(1, vs[0].Length - 2), NumberFormatInfo.InvariantInfo));
                    }
                    else
                    {
                        return new ChargeAdjustedValence(int.Parse(vs[0], NumberFormatInfo.InvariantInfo), nElectrons);
                    }
                }
                ValenceCheck[] valences = new ValenceCheck[vs.Length];
                for (int i = 0; i < vs.Length; i++)
                {
                    valences[i] = Parse(vs[i], nElectrons);
                }

                return new MultiValenceCheck(valences);
            }
        }

        sealed class ChargeAdjustedValence : ValenceCheck
        {
            private readonly int valence, nElectrons;

            public ChargeAdjustedValence(int valence, int nElectrons)
            {
                this.valence = valence;
                this.nElectrons = nElectrons;
            }

            public override bool Verify(int v, int q)
            {
                if (nElectrons == 2 && valence + q > nElectrons - q)  // Group 2 exception
                    return v == nElectrons - q;
                return valence + q == v;
            }

            public override string ToString()
            {
                return "Charge(" + valence + ")";
            }
        }

        /// <summary>A valence check which is only valid at netural charge</summary>
        sealed class NeutralValence : ValenceCheck
        {
            private readonly int valence;

            public NeutralValence(int valence)
            {
                this.valence = valence;
            }

            public override bool Verify(int v, int q)
            {
                return q == 0 && v == valence;
            }

            public override string ToString()
            {
                return "Neutral(" + valence + ")";
            }
        }

        sealed class FixedValence : ValenceCheck
        {
            private readonly int valence;

            public FixedValence(int valence)
            {
                this.valence = valence;
            }

            public override bool Verify(int v, int q)
            {
                return valence == v;
            }

            public override string ToString()
            {
                return "Fixed(" + valence + ")";
            }
        }

        private sealed class MultiValenceCheck : ValenceCheck
        {

            private readonly ValenceCheck[] valences;

            public MultiValenceCheck(ValenceCheck[] valences)
            {
                this.valences = valences;
            }

            public override bool Verify(int v, int q)
            {
                foreach (var vc in valences)
                {
                    if (vc.Verify(v, q))
                    {
                        return true;
                    }
                }
                return false;
            }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("[");
                foreach (var valence in valences)
                    sb.Append(valence.ToString()).Append(", ");
                sb.Append("]");
                return sb.ToString();
            }
        }

        private sealed class NoValenceCheck : ValenceCheck
        {
            public override bool Verify(int v, int q)
            {
                return true;
            }

            public static ValenceCheck Instance { get; } = new NoValenceCheck();
        }

        private sealed class ChargeCheck
        {
            private readonly int lo;
            private readonly int hi;

            private ChargeCheck(int lo, int hi)
            {
                this.lo = lo;
                this.hi = hi;
            }

            public bool Verify(int q)
            {
                return lo <= q && q <= hi;
            }

            public static ChargeCheck Parse(string range)
            {
                if (string.Equals(range, "n/a", StringComparison.Ordinal))
                    return None;
                string[] data = range.Split(',');
                int lo = int.Parse(data[0], NumberFormatInfo.InvariantInfo);
                int hi = int.Parse(data[1], NumberFormatInfo.InvariantInfo);
                return new ChargeCheck(lo, hi);
            }

            public static readonly ChargeCheck None = new ChargeCheck(int.MinValue, int.MaxValue);

            public override string ToString()
            {
                return lo + " < q < " + hi;
            }
        }

        /// <summary>
        /// Stores which elements the Daylight and OpenSMILES specification consider
        /// to be aromatic. The General scheme is what might be encountered 'in the
        /// wild'.
        /// </summary>
        public class AromaticSpecification
        {
            public static readonly AromaticSpecification Daylight = new AromaticSpecification(
                Unknown,
                Carbon,
                Nitrogen,
                Oxygen,
                Sulfur,
                Phosphorus,
                Arsenic,
                Selenium);

            public static readonly AromaticSpecification OpenSmiles = new AromaticSpecification(
                Unknown,
                Boron,
                Carbon,
                Nitrogen,
                Oxygen,
                Sulfur,
                Phosphorus,
                Arsenic,
                Selenium);

            public static readonly AromaticSpecification General = new AromaticSpecification(
                Unknown,
                Boron,
                Carbon,
                Nitrogen,
                Oxygen,
                Sulfur,
                Phosphorus,
                Arsenic,
                Selenium,
                Silicon,
                Germanium,
                Tin,
                Antimony,
                Tellurium,
                Bismuth);

            private ICollection<Element> elements = new List<Element>();

            AromaticSpecification(params Element[] es)
            {
                foreach (var e in es)
                    elements.Add(e);
            }

            public bool Contains(Element e)
            {
                return elements.Contains(e);
            }
        }

        /// <summary>
        /// Determine the implicit hydrogen count of an organic subset atom
        /// given its bonded valence. The number of implied hydrogens an 
        /// organic (or aromatic) subset atom has is based on it's bonded
        /// valence. The valances for the organic elements (B, C, N, O, P,
        /// S, F, Cl, Br and I) are defined in the OpenSMILES specification.
        /// </summary>
        /// <param name="elem">Element</param>
        /// <param name="v">bonded valence</param>
        /// <returns>hydrogen count >= 0</returns>
        internal static int NumOfImplicitHydrogens(Element elem, int v)
        {
            var aa = elem;
            if (aa == Boron)
            {
                if (v < 3) return 3 - v;
            }
            else if (aa == Carbon)
            {
                if (v < 4) return 4 - v;
            }
            else if (aa == Nitrogen || aa == Phosphorus)
            {
                if (v <= 3) return 3 - v;
                if (v < 5) return 5 - v;
            }
            else if (aa == Oxygen)
            {
                if (v < 2) return 2 - v;
            }
            else if (aa == Sulfur)
            {
                if (v <= 2) return 2 - v;
                if (v <= 4) return 4 - v;
                if (v < 6) return 6 - v;
            }
            else if (aa == Chlorine ||
                     aa == Bromine ||
                     aa == Iodine ||
                     aa == Fluorine)
            {
                if (v < 1) return 1;
            }
            return 0;
        }

        /// <summary>
        /// Determine the implicit hydrogen count of an organic subset atom
        /// given its bonded valence. The number of implied hydrogens an 
        /// organic (or aromatic) subset atom has is based on it's bonded
        /// valence. The valances for the organic elements (B, C, N, O, P,
        /// S, F, Cl, Br and I) are defined in the OpenSMILES specification.
        /// For aromatic atoms we only check the first level.
        /// </summary>
        /// <param name="elem">Element</param>
        /// <param name="v">bonded valence</param>
        /// <returns>hydrogen count >= 0</returns>
        internal static int NumOfAromaticImplicitHydrogens(Element elem, int v)
        {
            var aa = elem;
            if (aa == Boron) // arom?
            {
                if (v < 3) return 3 - v;
            }
            else if (aa == Carbon)
            {
                if (v < 4) return 4 - v;
            }
            else if (aa == Nitrogen || aa == Phosphorus)
            {
                if (v < 3) return 3 - v;
            }
            else if (aa == Oxygen)
            {
                if (v < 2) return 2 - v;
            }
            else if (aa == Sulfur)
            {
                if (v < 2) return 2 - v;
            }
            return 0;
        }
    }
}
