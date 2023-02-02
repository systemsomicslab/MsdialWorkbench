
/* 
 * Copyright (C) 2018  Kazuya Ujihara <ujihara.kazuya@gmail.com>
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

namespace NCDK
{
    public sealed class ChemicalElement
    {
        private readonly int atomicNumber;
        private readonly string symbol;
        private readonly string name;

        public int AtomicNumber 
        { 
            get => atomicNumber; 
            set => throw new InvalidOperationException(); 
        }
        
        public string Symbol
        {
            get => symbol;
            set => throw new InvalidOperationException(); 
        }

        public string Name => name;

        public ChemicalElement()
            : this(0, "*")
        {
        }
        
        public ChemicalElement(string symbol)
            : this(0, symbol)
        {
        }

        public ChemicalElement(int atomicNumber, string symbol, string name = "")
        {
            this.atomicNumber = atomicNumber;
            this.symbol = symbol;
            this.name = name;
        }

        public override string ToString()
        {
            return atomicNumber.ToString() + ":" + (symbol ?? "null");
        }

        public static ChemicalElement Of(int number)
        {
            if (!(number >= 0 && number <= 118))
                throw new ArgumentException(nameof(number));
            return elements[number];                
        }

        public static ChemicalElement OfSymbol(string symbol)
        {
            if (symbol == null)
                return ChemicalElement.R;
            if (!symbolToElementMap.TryGetValue(symbol, out ChemicalElement element))
                return ChemicalElement.R;
            return element;
        }

#if false
        public static ChemicalElement Of(string id)
        {
            if (id == null)
                return ChemicalElement.R;
            id = id.ToUpperInvariant();
            if (!textToElementMap.TryGetValue(id, out ChemicalElement element))
                return ChemicalElement.R;
            return element;               
        }
#endif

        public static ChemicalElement R { get; } = new ChemicalElement(0, "R", "Unknown");
        public static ChemicalElement H { get; } = new ChemicalElement(1, "H", "Hydrogen");
        public static ChemicalElement He { get; } = new ChemicalElement(2, "He", "Helium");
        public static ChemicalElement Li { get; } = new ChemicalElement(3, "Li", "Lithium");
        public static ChemicalElement Be { get; } = new ChemicalElement(4, "Be", "Beryllium");
        public static ChemicalElement B { get; } = new ChemicalElement(5, "B", "Boron");
        public static ChemicalElement C { get; } = new ChemicalElement(6, "C", "Carbon");
        public static ChemicalElement N { get; } = new ChemicalElement(7, "N", "Nitrogen");
        public static ChemicalElement O { get; } = new ChemicalElement(8, "O", "Oxygen");
        public static ChemicalElement F { get; } = new ChemicalElement(9, "F", "Fluorine");
        public static ChemicalElement Ne { get; } = new ChemicalElement(10, "Ne", "Neon");
        public static ChemicalElement Na { get; } = new ChemicalElement(11, "Na", "Sodium");
        public static ChemicalElement Mg { get; } = new ChemicalElement(12, "Mg", "Magnesium");
        public static ChemicalElement Al { get; } = new ChemicalElement(13, "Al", "Aluminium");
        public static ChemicalElement Si { get; } = new ChemicalElement(14, "Si", "Silicon");
        public static ChemicalElement P { get; } = new ChemicalElement(15, "P", "Phosphorus");
        public static ChemicalElement S { get; } = new ChemicalElement(16, "S", "Sulfur");
        public static ChemicalElement Cl { get; } = new ChemicalElement(17, "Cl", "Chlorine");
        public static ChemicalElement Ar { get; } = new ChemicalElement(18, "Ar", "Argon");
        public static ChemicalElement K { get; } = new ChemicalElement(19, "K", "Potassium");
        public static ChemicalElement Ca { get; } = new ChemicalElement(20, "Ca", "Calcium");
        public static ChemicalElement Sc { get; } = new ChemicalElement(21, "Sc", "Scandium");
        public static ChemicalElement Ti { get; } = new ChemicalElement(22, "Ti", "Titanium");
        public static ChemicalElement V { get; } = new ChemicalElement(23, "V", "Vanadium");
        public static ChemicalElement Cr { get; } = new ChemicalElement(24, "Cr", "Chromium");
        public static ChemicalElement Mn { get; } = new ChemicalElement(25, "Mn", "Manganese");
        public static ChemicalElement Fe { get; } = new ChemicalElement(26, "Fe", "Iron");
        public static ChemicalElement Co { get; } = new ChemicalElement(27, "Co", "Cobalt");
        public static ChemicalElement Ni { get; } = new ChemicalElement(28, "Ni", "Nickel");
        public static ChemicalElement Cu { get; } = new ChemicalElement(29, "Cu", "Copper");
        public static ChemicalElement Zn { get; } = new ChemicalElement(30, "Zn", "Zinc");
        public static ChemicalElement Ga { get; } = new ChemicalElement(31, "Ga", "Gallium");
        public static ChemicalElement Ge { get; } = new ChemicalElement(32, "Ge", "Germanium");
        public static ChemicalElement As { get; } = new ChemicalElement(33, "As", "Arsenic");
        public static ChemicalElement Se { get; } = new ChemicalElement(34, "Se", "Selenium");
        public static ChemicalElement Br { get; } = new ChemicalElement(35, "Br", "Bromine");
        public static ChemicalElement Kr { get; } = new ChemicalElement(36, "Kr", "Krypton");
        public static ChemicalElement Rb { get; } = new ChemicalElement(37, "Rb", "Rubidium");
        public static ChemicalElement Sr { get; } = new ChemicalElement(38, "Sr", "Strontium");
        public static ChemicalElement Y { get; } = new ChemicalElement(39, "Y", "Yttrium");
        public static ChemicalElement Zr { get; } = new ChemicalElement(40, "Zr", "Zirconium");
        public static ChemicalElement Nb { get; } = new ChemicalElement(41, "Nb", "Niobium");
        public static ChemicalElement Mo { get; } = new ChemicalElement(42, "Mo", "Molybdenum");
        public static ChemicalElement Tc { get; } = new ChemicalElement(43, "Tc", "Technetium");
        public static ChemicalElement Ru { get; } = new ChemicalElement(44, "Ru", "Ruthenium");
        public static ChemicalElement Rh { get; } = new ChemicalElement(45, "Rh", "Rhodium");
        public static ChemicalElement Pd { get; } = new ChemicalElement(46, "Pd", "Palladium");
        public static ChemicalElement Ag { get; } = new ChemicalElement(47, "Ag", "Silver");
        public static ChemicalElement Cd { get; } = new ChemicalElement(48, "Cd", "Cadmium");
        public static ChemicalElement In { get; } = new ChemicalElement(49, "In", "Indium");
        public static ChemicalElement Sn { get; } = new ChemicalElement(50, "Sn", "Tin");
        public static ChemicalElement Sb { get; } = new ChemicalElement(51, "Sb", "Antimony");
        public static ChemicalElement Te { get; } = new ChemicalElement(52, "Te", "Tellurium");
        public static ChemicalElement I { get; } = new ChemicalElement(53, "I", "Iodine");
        public static ChemicalElement Xe { get; } = new ChemicalElement(54, "Xe", "Xenon");
        public static ChemicalElement Cs { get; } = new ChemicalElement(55, "Cs", "Caesium");
        public static ChemicalElement Ba { get; } = new ChemicalElement(56, "Ba", "Barium");
        public static ChemicalElement La { get; } = new ChemicalElement(57, "La", "Lanthanum");
        public static ChemicalElement Ce { get; } = new ChemicalElement(58, "Ce", "Cerium");
        public static ChemicalElement Pr { get; } = new ChemicalElement(59, "Pr", "Praseodymium");
        public static ChemicalElement Nd { get; } = new ChemicalElement(60, "Nd", "Neodymium");
        public static ChemicalElement Pm { get; } = new ChemicalElement(61, "Pm", "Promethium");
        public static ChemicalElement Sm { get; } = new ChemicalElement(62, "Sm", "Samarium");
        public static ChemicalElement Eu { get; } = new ChemicalElement(63, "Eu", "Europium");
        public static ChemicalElement Gd { get; } = new ChemicalElement(64, "Gd", "Gadolinium");
        public static ChemicalElement Tb { get; } = new ChemicalElement(65, "Tb", "Terbium");
        public static ChemicalElement Dy { get; } = new ChemicalElement(66, "Dy", "Dysprosium");
        public static ChemicalElement Ho { get; } = new ChemicalElement(67, "Ho", "Holmium");
        public static ChemicalElement Er { get; } = new ChemicalElement(68, "Er", "Erbium");
        public static ChemicalElement Tm { get; } = new ChemicalElement(69, "Tm", "Thulium");
        public static ChemicalElement Yb { get; } = new ChemicalElement(70, "Yb", "Ytterbium");
        public static ChemicalElement Lu { get; } = new ChemicalElement(71, "Lu", "Lutetium");
        public static ChemicalElement Hf { get; } = new ChemicalElement(72, "Hf", "Hafnium");
        public static ChemicalElement Ta { get; } = new ChemicalElement(73, "Ta", "Tantalum");
        public static ChemicalElement W { get; } = new ChemicalElement(74, "W", "Tungsten");
        public static ChemicalElement Re { get; } = new ChemicalElement(75, "Re", "Rhenium");
        public static ChemicalElement Os { get; } = new ChemicalElement(76, "Os", "Osmium");
        public static ChemicalElement Ir { get; } = new ChemicalElement(77, "Ir", "Iridium");
        public static ChemicalElement Pt { get; } = new ChemicalElement(78, "Pt", "Platinum");
        public static ChemicalElement Au { get; } = new ChemicalElement(79, "Au", "Gold");
        public static ChemicalElement Hg { get; } = new ChemicalElement(80, "Hg", "Mercury");
        public static ChemicalElement Tl { get; } = new ChemicalElement(81, "Tl", "Thallium");
        public static ChemicalElement Pb { get; } = new ChemicalElement(82, "Pb", "Lead");
        public static ChemicalElement Bi { get; } = new ChemicalElement(83, "Bi", "Bismuth");
        public static ChemicalElement Po { get; } = new ChemicalElement(84, "Po", "Polonium");
        public static ChemicalElement At { get; } = new ChemicalElement(85, "At", "Astatine");
        public static ChemicalElement Rn { get; } = new ChemicalElement(86, "Rn", "Radon");
        public static ChemicalElement Fr { get; } = new ChemicalElement(87, "Fr", "Francium");
        public static ChemicalElement Ra { get; } = new ChemicalElement(88, "Ra", "Radium");
        public static ChemicalElement Ac { get; } = new ChemicalElement(89, "Ac", "Actinium");
        public static ChemicalElement Th { get; } = new ChemicalElement(90, "Th", "Thorium");
        public static ChemicalElement Pa { get; } = new ChemicalElement(91, "Pa", "Protactinium");
        public static ChemicalElement U { get; } = new ChemicalElement(92, "U", "Uranium");
        public static ChemicalElement Np { get; } = new ChemicalElement(93, "Np", "Neptunium");
        public static ChemicalElement Pu { get; } = new ChemicalElement(94, "Pu", "Plutonium");
        public static ChemicalElement Am { get; } = new ChemicalElement(95, "Am", "Americium");
        public static ChemicalElement Cm { get; } = new ChemicalElement(96, "Cm", "Curium");
        public static ChemicalElement Bk { get; } = new ChemicalElement(97, "Bk", "Berkelium");
        public static ChemicalElement Cf { get; } = new ChemicalElement(98, "Cf", "Californium");
        public static ChemicalElement Es { get; } = new ChemicalElement(99, "Es", "Einsteinium");
        public static ChemicalElement Fm { get; } = new ChemicalElement(100, "Fm", "Fermium");
        public static ChemicalElement Md { get; } = new ChemicalElement(101, "Md", "Mendelevium");
        public static ChemicalElement No { get; } = new ChemicalElement(102, "No", "Nobelium");
        public static ChemicalElement Lr { get; } = new ChemicalElement(103, "Lr", "Lawrencium");
        public static ChemicalElement Rf { get; } = new ChemicalElement(104, "Rf", "Rutherfordium");
        public static ChemicalElement Db { get; } = new ChemicalElement(105, "Db", "Dubnium");
        public static ChemicalElement Sg { get; } = new ChemicalElement(106, "Sg", "Seaborgium");
        public static ChemicalElement Bh { get; } = new ChemicalElement(107, "Bh", "Bohrium");
        public static ChemicalElement Hs { get; } = new ChemicalElement(108, "Hs", "Hassium");
        public static ChemicalElement Mt { get; } = new ChemicalElement(109, "Mt", "Meitnerium");
        public static ChemicalElement Ds { get; } = new ChemicalElement(110, "Ds", "Darmstadtium");
        public static ChemicalElement Rg { get; } = new ChemicalElement(111, "Rg", "Roentgenium");
        public static ChemicalElement Cn { get; } = new ChemicalElement(112, "Cn", "Copernicium");
        public static ChemicalElement Nh { get; } = new ChemicalElement(113, "Nh", "Nihonium");
        public static ChemicalElement Fl { get; } = new ChemicalElement(114, "Fl", "Flerovium");
        public static ChemicalElement Mc { get; } = new ChemicalElement(115, "Mc", "Moscovium");
        public static ChemicalElement Lv { get; } = new ChemicalElement(116, "Lv", "Livermorium");
        public static ChemicalElement Ts { get; } = new ChemicalElement(117, "Ts", "Tennessine");
        public static ChemicalElement Og { get; } = new ChemicalElement(118, "Og", "Oganesson");
        
        internal static readonly Dictionary<string, ChemicalElement> symbolToElementMap = new Dictionary<string, ChemicalElement>()
            {
                ["H"] = H,
                ["He"] = He,
                ["Li"] = Li,
                ["Be"] = Be,
                ["B"] = B,
                ["C"] = C,
                ["N"] = N,
                ["O"] = O,
                ["F"] = F,
                ["Ne"] = Ne,
                ["Na"] = Na,
                ["Mg"] = Mg,
                ["Al"] = Al,
                ["Si"] = Si,
                ["P"] = P,
                ["S"] = S,
                ["Cl"] = Cl,
                ["Ar"] = Ar,
                ["K"] = K,
                ["Ca"] = Ca,
                ["Sc"] = Sc,
                ["Ti"] = Ti,
                ["V"] = V,
                ["Cr"] = Cr,
                ["Mn"] = Mn,
                ["Fe"] = Fe,
                ["Co"] = Co,
                ["Ni"] = Ni,
                ["Cu"] = Cu,
                ["Zn"] = Zn,
                ["Ga"] = Ga,
                ["Ge"] = Ge,
                ["As"] = As,
                ["Se"] = Se,
                ["Br"] = Br,
                ["Kr"] = Kr,
                ["Rb"] = Rb,
                ["Sr"] = Sr,
                ["Y"] = Y,
                ["Zr"] = Zr,
                ["Nb"] = Nb,
                ["Mo"] = Mo,
                ["Tc"] = Tc,
                ["Ru"] = Ru,
                ["Rh"] = Rh,
                ["Pd"] = Pd,
                ["Ag"] = Ag,
                ["Cd"] = Cd,
                ["In"] = In,
                ["Sn"] = Sn,
                ["Sb"] = Sb,
                ["Te"] = Te,
                ["I"] = I,
                ["Xe"] = Xe,
                ["Cs"] = Cs,
                ["Ba"] = Ba,
                ["La"] = La,
                ["Ce"] = Ce,
                ["Pr"] = Pr,
                ["Nd"] = Nd,
                ["Pm"] = Pm,
                ["Sm"] = Sm,
                ["Eu"] = Eu,
                ["Gd"] = Gd,
                ["Tb"] = Tb,
                ["Dy"] = Dy,
                ["Ho"] = Ho,
                ["Er"] = Er,
                ["Tm"] = Tm,
                ["Yb"] = Yb,
                ["Lu"] = Lu,
                ["Hf"] = Hf,
                ["Ta"] = Ta,
                ["W"] = W,
                ["Re"] = Re,
                ["Os"] = Os,
                ["Ir"] = Ir,
                ["Pt"] = Pt,
                ["Au"] = Au,
                ["Hg"] = Hg,
                ["Tl"] = Tl,
                ["Pb"] = Pb,
                ["Bi"] = Bi,
                ["Po"] = Po,
                ["At"] = At,
                ["Rn"] = Rn,
                ["Fr"] = Fr,
                ["Ra"] = Ra,
                ["Ac"] = Ac,
                ["Th"] = Th,
                ["Pa"] = Pa,
                ["U"] = U,
                ["Np"] = Np,
                ["Pu"] = Pu,
                ["Am"] = Am,
                ["Cm"] = Cm,
                ["Bk"] = Bk,
                ["Cf"] = Cf,
                ["Es"] = Es,
                ["Fm"] = Fm,
                ["Md"] = Md,
                ["No"] = No,
                ["Lr"] = Lr,
                ["Rf"] = Rf,
                ["Db"] = Db,
                ["Sg"] = Sg,
                ["Bh"] = Bh,
                ["Hs"] = Hs,
                ["Mt"] = Mt,
                ["Ds"] = Ds,
                ["Rg"] = Rg,
                ["Cn"] = Cn,
                ["Uut"] = Nh,
                ["Nh"] = Nh,
                ["Fl"] = Fl,
                ["Uup"] = Mc,
                ["Mc"] = Mc,
                ["Lv"] = Lv,
                ["Uus"] = Ts,
                ["Ts"] = Ts,
                ["Uuo"] = Og,
                ["Og"] = Og,
                ["R"] = R,
            };

        private static readonly Dictionary<string, ChemicalElement> textToElementMap = new Dictionary<string, ChemicalElement>()
            {
                ["HYDROGEN"] = H,
                ["H"] = H,
                ["HELIUM"] = He,
                ["HE"] = He,
                ["LITHIUM"] = Li,
                ["LI"] = Li,
                ["BERYLLIUM"] = Be,
                ["BE"] = Be,
                ["BORON"] = B,
                ["B"] = B,
                ["CARBON"] = C,
                ["C"] = C,
                ["NITROGEN"] = N,
                ["N"] = N,
                ["OXYGEN"] = O,
                ["O"] = O,
                ["FLUORINE"] = F,
                ["F"] = F,
                ["NEON"] = Ne,
                ["NE"] = Ne,
                ["SODIUM"] = Na,
                ["NA"] = Na,
                ["MAGNESIUM"] = Mg,
                ["MG"] = Mg,
                ["ALUMINIUM"] = Al,
                ["AL"] = Al,
                ["SILICON"] = Si,
                ["SI"] = Si,
                ["PHOSPHORUS"] = P,
                ["P"] = P,
                ["SULFUR"] = S,
                ["S"] = S,
                ["CHLORINE"] = Cl,
                ["CL"] = Cl,
                ["ARGON"] = Ar,
                ["AR"] = Ar,
                ["POTASSIUM"] = K,
                ["K"] = K,
                ["CALCIUM"] = Ca,
                ["CA"] = Ca,
                ["SCANDIUM"] = Sc,
                ["SC"] = Sc,
                ["TITANIUM"] = Ti,
                ["TI"] = Ti,
                ["VANADIUM"] = V,
                ["V"] = V,
                ["CHROMIUM"] = Cr,
                ["CR"] = Cr,
                ["MANGANESE"] = Mn,
                ["MN"] = Mn,
                ["IRON"] = Fe,
                ["FE"] = Fe,
                ["COBALT"] = Co,
                ["CO"] = Co,
                ["NICKEL"] = Ni,
                ["NI"] = Ni,
                ["COPPER"] = Cu,
                ["CU"] = Cu,
                ["ZINC"] = Zn,
                ["ZN"] = Zn,
                ["GALLIUM"] = Ga,
                ["GA"] = Ga,
                ["GERMANIUM"] = Ge,
                ["GE"] = Ge,
                ["ARSENIC"] = As,
                ["AS"] = As,
                ["SELENIUM"] = Se,
                ["SE"] = Se,
                ["BROMINE"] = Br,
                ["BR"] = Br,
                ["KRYPTON"] = Kr,
                ["KR"] = Kr,
                ["RUBIDIUM"] = Rb,
                ["RB"] = Rb,
                ["STRONTIUM"] = Sr,
                ["SR"] = Sr,
                ["YTTRIUM"] = Y,
                ["Y"] = Y,
                ["ZIRCONIUM"] = Zr,
                ["ZR"] = Zr,
                ["NIOBIUM"] = Nb,
                ["NB"] = Nb,
                ["MOLYBDENUM"] = Mo,
                ["MO"] = Mo,
                ["TECHNETIUM"] = Tc,
                ["TC"] = Tc,
                ["RUTHENIUM"] = Ru,
                ["RU"] = Ru,
                ["RHODIUM"] = Rh,
                ["RH"] = Rh,
                ["PALLADIUM"] = Pd,
                ["PD"] = Pd,
                ["SILVER"] = Ag,
                ["AG"] = Ag,
                ["CADMIUM"] = Cd,
                ["CD"] = Cd,
                ["INDIUM"] = In,
                ["IN"] = In,
                ["TIN"] = Sn,
                ["SN"] = Sn,
                ["ANTIMONY"] = Sb,
                ["SB"] = Sb,
                ["TELLURIUM"] = Te,
                ["TE"] = Te,
                ["IODINE"] = I,
                ["I"] = I,
                ["XENON"] = Xe,
                ["XE"] = Xe,
                ["CAESIUM"] = Cs,
                ["CS"] = Cs,
                ["BARIUM"] = Ba,
                ["BA"] = Ba,
                ["LANTHANUM"] = La,
                ["LA"] = La,
                ["CERIUM"] = Ce,
                ["CE"] = Ce,
                ["PRASEODYMIUM"] = Pr,
                ["PR"] = Pr,
                ["NEODYMIUM"] = Nd,
                ["ND"] = Nd,
                ["PROMETHIUM"] = Pm,
                ["PM"] = Pm,
                ["SAMARIUM"] = Sm,
                ["SM"] = Sm,
                ["EUROPIUM"] = Eu,
                ["EU"] = Eu,
                ["GADOLINIUM"] = Gd,
                ["GD"] = Gd,
                ["TERBIUM"] = Tb,
                ["TB"] = Tb,
                ["DYSPROSIUM"] = Dy,
                ["DY"] = Dy,
                ["HOLMIUM"] = Ho,
                ["HO"] = Ho,
                ["ERBIUM"] = Er,
                ["ER"] = Er,
                ["THULIUM"] = Tm,
                ["TM"] = Tm,
                ["YTTERBIUM"] = Yb,
                ["YB"] = Yb,
                ["LUTETIUM"] = Lu,
                ["LU"] = Lu,
                ["HAFNIUM"] = Hf,
                ["HF"] = Hf,
                ["TANTALUM"] = Ta,
                ["TA"] = Ta,
                ["TUNGSTEN"] = W,
                ["W"] = W,
                ["RHENIUM"] = Re,
                ["RE"] = Re,
                ["OSMIUM"] = Os,
                ["OS"] = Os,
                ["IRIDIUM"] = Ir,
                ["IR"] = Ir,
                ["PLATINUM"] = Pt,
                ["PT"] = Pt,
                ["GOLD"] = Au,
                ["AU"] = Au,
                ["MERCURY"] = Hg,
                ["HG"] = Hg,
                ["THALLIUM"] = Tl,
                ["TL"] = Tl,
                ["LEAD"] = Pb,
                ["PB"] = Pb,
                ["BISMUTH"] = Bi,
                ["BI"] = Bi,
                ["POLONIUM"] = Po,
                ["PO"] = Po,
                ["ASTATINE"] = At,
                ["AT"] = At,
                ["RADON"] = Rn,
                ["RN"] = Rn,
                ["FRANCIUM"] = Fr,
                ["FR"] = Fr,
                ["RADIUM"] = Ra,
                ["RA"] = Ra,
                ["ACTINIUM"] = Ac,
                ["AC"] = Ac,
                ["THORIUM"] = Th,
                ["TH"] = Th,
                ["PROTACTINIUM"] = Pa,
                ["PA"] = Pa,
                ["URANIUM"] = U,
                ["U"] = U,
                ["NEPTUNIUM"] = Np,
                ["NP"] = Np,
                ["PLUTONIUM"] = Pu,
                ["PU"] = Pu,
                ["AMERICIUM"] = Am,
                ["AM"] = Am,
                ["CURIUM"] = Cm,
                ["CM"] = Cm,
                ["BERKELIUM"] = Bk,
                ["BK"] = Bk,
                ["CALIFORNIUM"] = Cf,
                ["CF"] = Cf,
                ["EINSTEINIUM"] = Es,
                ["ES"] = Es,
                ["FERMIUM"] = Fm,
                ["FM"] = Fm,
                ["MENDELEVIUM"] = Md,
                ["MD"] = Md,
                ["NOBELIUM"] = No,
                ["NO"] = No,
                ["LAWRENCIUM"] = Lr,
                ["LR"] = Lr,
                ["RUTHERFORDIUM"] = Rf,
                ["RF"] = Rf,
                ["DUBNIUM"] = Db,
                ["DB"] = Db,
                ["SEABORGIUM"] = Sg,
                ["SG"] = Sg,
                ["BOHRIUM"] = Bh,
                ["BH"] = Bh,
                ["HASSIUM"] = Hs,
                ["HS"] = Hs,
                ["MEITNERIUM"] = Mt,
                ["MT"] = Mt,
                ["DARMSTADTIUM"] = Ds,
                ["DS"] = Ds,
                ["ROENTGENIUM"] = Rg,
                ["RG"] = Rg,
                ["COPERNICIUM"] = Cn,
                ["CN"] = Cn,
                ["UNUNTRIUM"] = Nh,
                ["UUT"] = Nh,
                ["NIHONIUM"] = Nh,
                ["NH"] = Nh,
                ["FLEROVIUM"] = Fl,
                ["FL"] = Fl,
                ["UNUNPENTIUM"] = Mc,
                ["UUP"] = Mc,
                ["MOSCOVIUM"] = Mc,
                ["MC"] = Mc,
                ["LIVERMORIUM"] = Lv,
                ["LV"] = Lv,
                ["UNUNSEPTIUM"] = Ts,
                ["UUS"] = Ts,
                ["TENNESSINE"] = Ts,
                ["TS"] = Ts,
                ["UNUNOCTIUM"] = Og,
                ["UUO"] = Og,
                ["OGANESSON"] = Og,
                ["OG"] = Og,
                ["UNKNOWN"] = R,
                ["R"] = R,
            };

        private static readonly ChemicalElement[] elements = new ChemicalElement[]  { R, H, He, Li, Be, B, C, N, O, F, Ne, Na, Mg, Al, Si, P, S, Cl, Ar, K, Ca, Sc, Ti, V, Cr, Mn, Fe, Co, Ni, Cu, Zn, Ga, Ge, As, Se, Br, Kr, Rb, Sr, Y, Zr, Nb, Mo, Tc, Ru, Rh, Pd, Ag, Cd, In, Sn, Sb, Te, I, Xe, Cs, Ba, La, Ce, Pr, Nd, Pm, Sm, Eu, Gd, Tb, Dy, Ho, Er, Tm, Yb, Lu, Hf, Ta, W, Re, Os, Ir, Pt, Au, Hg, Tl, Pb, Bi, Po, At, Rn, Fr, Ra, Ac, Th, Pa, U, Np, Pu, Am, Cm, Bk, Cf, Es, Fm, Md, No, Lr, Rf, Db, Sg, Bh, Hs, Mt, Ds, Rg, Cn, Nh, Fl, Mc, Lv, Ts, Og,  };

        public static IReadOnlyList<ChemicalElement> Values => elements;

        private static readonly string[] symbols = new string[]  { "R", "H", "He", "Li", "Be", "B", "C", "N", "O", "F", "Ne", "Na", "Mg", "Al", "Si", "P", "S", "Cl", "Ar", "K", "Ca", "Sc", "Ti", "V", "Cr", "Mn", "Fe", "Co", "Ni", "Cu", "Zn", "Ga", "Ge", "As", "Se", "Br", "Kr", "Rb", "Sr", "Y", "Zr", "Nb", "Mo", "Tc", "Ru", "Rh", "Pd", "Ag", "Cd", "In", "Sn", "Sb", "Te", "I", "Xe", "Cs", "Ba", "La", "Ce", "Pr", "Nd", "Pm", "Sm", "Eu", "Gd", "Tb", "Dy", "Ho", "Er", "Tm", "Yb", "Lu", "Hf", "Ta", "W", "Re", "Os", "Ir", "Pt", "Au", "Hg", "Tl", "Pb", "Bi", "Po", "At", "Rn", "Fr", "Ra", "Ac", "Th", "Pa", "U", "Np", "Pu", "Am", "Cm", "Bk", "Cf", "Es", "Fm", "Md", "No", "Lr", "Rf", "Db", "Sg", "Bh", "Hs", "Mt", "Ds", "Rg", "Cn", "Nh", "Fl", "Mc", "Lv", "Ts", "Og",  };

        public static IReadOnlyList<string> Symbols => symbols;
    }

    public static class AtomicNumbers
    {
        public const int R = 0;
        public const int Unknown = 0;
        public const int H = 1;
        public const int Hydrogen = 1;
        public const int He = 2;
        public const int Helium = 2;
        public const int Li = 3;
        public const int Lithium = 3;
        public const int Be = 4;
        public const int Beryllium = 4;
        public const int B = 5;
        public const int Boron = 5;
        public const int C = 6;
        public const int Carbon = 6;
        public const int N = 7;
        public const int Nitrogen = 7;
        public const int O = 8;
        public const int Oxygen = 8;
        public const int F = 9;
        public const int Fluorine = 9;
        public const int Ne = 10;
        public const int Neon = 10;
        public const int Na = 11;
        public const int Sodium = 11;
        public const int Mg = 12;
        public const int Magnesium = 12;
        public const int Al = 13;
        public const int Aluminium = 13;
        public const int Si = 14;
        public const int Silicon = 14;
        public const int P = 15;
        public const int Phosphorus = 15;
        public const int S = 16;
        public const int Sulfur = 16;
        public const int Cl = 17;
        public const int Chlorine = 17;
        public const int Ar = 18;
        public const int Argon = 18;
        public const int K = 19;
        public const int Potassium = 19;
        public const int Ca = 20;
        public const int Calcium = 20;
        public const int Sc = 21;
        public const int Scandium = 21;
        public const int Ti = 22;
        public const int Titanium = 22;
        public const int V = 23;
        public const int Vanadium = 23;
        public const int Cr = 24;
        public const int Chromium = 24;
        public const int Mn = 25;
        public const int Manganese = 25;
        public const int Fe = 26;
        public const int Iron = 26;
        public const int Co = 27;
        public const int Cobalt = 27;
        public const int Ni = 28;
        public const int Nickel = 28;
        public const int Cu = 29;
        public const int Copper = 29;
        public const int Zn = 30;
        public const int Zinc = 30;
        public const int Ga = 31;
        public const int Gallium = 31;
        public const int Ge = 32;
        public const int Germanium = 32;
        public const int As = 33;
        public const int Arsenic = 33;
        public const int Se = 34;
        public const int Selenium = 34;
        public const int Br = 35;
        public const int Bromine = 35;
        public const int Kr = 36;
        public const int Krypton = 36;
        public const int Rb = 37;
        public const int Rubidium = 37;
        public const int Sr = 38;
        public const int Strontium = 38;
        public const int Y = 39;
        public const int Yttrium = 39;
        public const int Zr = 40;
        public const int Zirconium = 40;
        public const int Nb = 41;
        public const int Niobium = 41;
        public const int Mo = 42;
        public const int Molybdenum = 42;
        public const int Tc = 43;
        public const int Technetium = 43;
        public const int Ru = 44;
        public const int Ruthenium = 44;
        public const int Rh = 45;
        public const int Rhodium = 45;
        public const int Pd = 46;
        public const int Palladium = 46;
        public const int Ag = 47;
        public const int Silver = 47;
        public const int Cd = 48;
        public const int Cadmium = 48;
        public const int In = 49;
        public const int Indium = 49;
        public const int Sn = 50;
        public const int Tin = 50;
        public const int Sb = 51;
        public const int Antimony = 51;
        public const int Te = 52;
        public const int Tellurium = 52;
        public const int I = 53;
        public const int Iodine = 53;
        public const int Xe = 54;
        public const int Xenon = 54;
        public const int Cs = 55;
        public const int Caesium = 55;
        public const int Ba = 56;
        public const int Barium = 56;
        public const int La = 57;
        public const int Lanthanum = 57;
        public const int Ce = 58;
        public const int Cerium = 58;
        public const int Pr = 59;
        public const int Praseodymium = 59;
        public const int Nd = 60;
        public const int Neodymium = 60;
        public const int Pm = 61;
        public const int Promethium = 61;
        public const int Sm = 62;
        public const int Samarium = 62;
        public const int Eu = 63;
        public const int Europium = 63;
        public const int Gd = 64;
        public const int Gadolinium = 64;
        public const int Tb = 65;
        public const int Terbium = 65;
        public const int Dy = 66;
        public const int Dysprosium = 66;
        public const int Ho = 67;
        public const int Holmium = 67;
        public const int Er = 68;
        public const int Erbium = 68;
        public const int Tm = 69;
        public const int Thulium = 69;
        public const int Yb = 70;
        public const int Ytterbium = 70;
        public const int Lu = 71;
        public const int Lutetium = 71;
        public const int Hf = 72;
        public const int Hafnium = 72;
        public const int Ta = 73;
        public const int Tantalum = 73;
        public const int W = 74;
        public const int Tungsten = 74;
        public const int Re = 75;
        public const int Rhenium = 75;
        public const int Os = 76;
        public const int Osmium = 76;
        public const int Ir = 77;
        public const int Iridium = 77;
        public const int Pt = 78;
        public const int Platinum = 78;
        public const int Au = 79;
        public const int Gold = 79;
        public const int Hg = 80;
        public const int Mercury = 80;
        public const int Tl = 81;
        public const int Thallium = 81;
        public const int Pb = 82;
        public const int Lead = 82;
        public const int Bi = 83;
        public const int Bismuth = 83;
        public const int Po = 84;
        public const int Polonium = 84;
        public const int At = 85;
        public const int Astatine = 85;
        public const int Rn = 86;
        public const int Radon = 86;
        public const int Fr = 87;
        public const int Francium = 87;
        public const int Ra = 88;
        public const int Radium = 88;
        public const int Ac = 89;
        public const int Actinium = 89;
        public const int Th = 90;
        public const int Thorium = 90;
        public const int Pa = 91;
        public const int Protactinium = 91;
        public const int U = 92;
        public const int Uranium = 92;
        public const int Np = 93;
        public const int Neptunium = 93;
        public const int Pu = 94;
        public const int Plutonium = 94;
        public const int Am = 95;
        public const int Americium = 95;
        public const int Cm = 96;
        public const int Curium = 96;
        public const int Bk = 97;
        public const int Berkelium = 97;
        public const int Cf = 98;
        public const int Californium = 98;
        public const int Es = 99;
        public const int Einsteinium = 99;
        public const int Fm = 100;
        public const int Fermium = 100;
        public const int Md = 101;
        public const int Mendelevium = 101;
        public const int No = 102;
        public const int Nobelium = 102;
        public const int Lr = 103;
        public const int Lawrencium = 103;
        public const int Rf = 104;
        public const int Rutherfordium = 104;
        public const int Db = 105;
        public const int Dubnium = 105;
        public const int Sg = 106;
        public const int Seaborgium = 106;
        public const int Bh = 107;
        public const int Bohrium = 107;
        public const int Hs = 108;
        public const int Hassium = 108;
        public const int Mt = 109;
        public const int Meitnerium = 109;
        public const int Ds = 110;
        public const int Darmstadtium = 110;
        public const int Rg = 111;
        public const int Roentgenium = 111;
        public const int Cn = 112;
        public const int Copernicium = 112;
        public const int Nh = 113;
        public const int Nihonium = 113;
        public const int Fl = 114;
        public const int Flerovium = 114;
        public const int Mc = 115;
        public const int Moscovium = 115;
        public const int Lv = 116;
        public const int Livermorium = 116;
        public const int Ts = 117;
        public const int Tennessine = 117;
        public const int Og = 118;
        public const int Oganesson = 118;
    }
}

