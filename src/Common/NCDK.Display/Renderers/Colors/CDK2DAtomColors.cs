/* Copyright (C) 1997-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using System.Windows.Media;
using WPF = System.Windows;

namespace NCDK.Renderers.Colors
{
    /// <summary>
    /// Gives a short table of atom colors for 2D display. The coloring is loosely
    /// based on Jmol CPK.
    /// </summary>
    /// <example>
    /// The internal color map can be modified by invoking the set method. For convenience the set method
    /// returns the colorer instance for chaining.
    /// <code>
    /// IAtomColorer colorer = new CDK2DAtomColors().Set("H", Color.LightGray).Set("O", WPF::Media.Colors.Red.lighter());
    /// </code>
    /// </example>
    ///<seealso href="http://en.wikipedia.org/wiki/CPK_coloring">CPK coloring</seealso>
    ///<seealso href="http://jmol.sourceforge.net/jscolors/">Jmol, Colors</seealso>
    // @cdk.module render
    public class CDK2DAtomColors : IAtomColorer
    {
        private static readonly Color hexD9FFFF = Color.FromRgb(0xD9, 0xFF, 0xFF);
        private static readonly Color hexCC80FF = Color.FromRgb(0xCC, 0x80, 0xFF);
        private static readonly Color hexC2FF00 = Color.FromRgb(0xC2, 0xFF, 0x00);
        private static readonly Color hexFFB5B5 = Color.FromRgb(0xFF, 0xB5, 0xB5);
        private static readonly Color hex3050F8 = Color.FromRgb(0x30, 0x50, 0xF8);
        private static readonly Color hexFF0D0D = Color.FromRgb(0xFF, 0x0D, 0x0D);
        private static readonly Color hex90E050 = Color.FromRgb(0x90, 0xE0, 0x50);
        private static readonly Color hexB3E3F5 = Color.FromRgb(0xB3, 0xE3, 0xF5);
        private static readonly Color hexAB5CF2 = Color.FromRgb(0xAB, 0x5C, 0xF2);
        private static readonly Color hex8AFF00 = Color.FromRgb(0x8A, 0xFF, 0x00);
        private static readonly Color hexBFA6A6 = Color.FromRgb(0xBF, 0xA6, 0xA6);
        private static readonly Color hexF0C8A0 = Color.FromRgb(0xF0, 0xC8, 0xA0);
        private static readonly Color hexFF8000 = Color.FromRgb(0xFF, 0x80, 0x00);
        private static readonly Color hexFFFF30 = Color.FromRgb(0xC6, 0xC6, 0x2C);
        private static readonly Color hex1FF01F = Color.FromRgb(0x1F, 0xF0, 0x1F);
        private static readonly Color hex80D1E3 = Color.FromRgb(0x80, 0xD1, 0xE3);
        private static readonly Color hex8F40D4 = Color.FromRgb(0x8F, 0x40, 0xD4);
        private static readonly Color hex3DFF00 = Color.FromRgb(0x3D, 0xFF, 0x00);
        private static readonly Color hexE6E6E6 = Color.FromRgb(0xE6, 0xE6, 0xE6);
        private static readonly Color hexBFC2C7 = Color.FromRgb(0xBF, 0xC2, 0xC7);
        private static readonly Color hexA6A6AB = Color.FromRgb(0xA6, 0xA6, 0xAB);
        private static readonly Color hex8A99C7 = Color.FromRgb(0x8A, 0x99, 0xC7);
        private static readonly Color hex9C7AC7 = Color.FromRgb(0x9C, 0x7A, 0xC7);
        private static readonly Color hexE06633 = Color.FromRgb(0xE0, 0x66, 0x33);
        private static readonly Color hexF090A0 = Color.FromRgb(0xF0, 0x90, 0xA0);
        private static readonly Color hex50D050 = Color.FromRgb(0x50, 0xD0, 0x50);
        private static readonly Color hexC88033 = Color.FromRgb(0xC8, 0x80, 0x33);
        private static readonly Color hex7D80B0 = Color.FromRgb(0x7D, 0x80, 0xB0);
        private static readonly Color hexC28F8F = Color.FromRgb(0xC2, 0x8F, 0x8F);
        private static readonly Color hex668F8F = Color.FromRgb(0x66, 0x8F, 0x8F);
        private static readonly Color hexBD80E3 = Color.FromRgb(0xBD, 0x80, 0xE3);
        private static readonly Color hexFFA100 = Color.FromRgb(0xFF, 0xA1, 0x00);
        private static readonly Color hexA62929 = Color.FromRgb(0xA6, 0x29, 0x29);
        private static readonly Color hex5CB8D1 = Color.FromRgb(0x5C, 0xB8, 0xD1);
        private static readonly Color hex702EB0 = Color.FromRgb(0x70, 0x2E, 0xB0);
        private static readonly Color hex00FF00 = Color.FromRgb(0x00, 0xFF, 0x00);
        private static readonly Color hex94FFFF = Color.FromRgb(0x94, 0xFF, 0xFF);
        private static readonly Color hex94E0E0 = Color.FromRgb(0x94, 0xE0, 0xE0);
        private static readonly Color hex73C2C9 = Color.FromRgb(0x73, 0xC2, 0xC9);
        private static readonly Color hex54B5B5 = Color.FromRgb(0x54, 0xB5, 0xB5);
        private static readonly Color hex3B9E9E = Color.FromRgb(0x3B, 0x9E, 0x9E);
        private static readonly Color hex248F8F = Color.FromRgb(0x24, 0x8F, 0x8F);
        private static readonly Color hex0A7D8C = Color.FromRgb(0x0A, 0x7D, 0x8C);
        private static readonly Color hex006985 = Color.FromRgb(0x00, 0x69, 0x85);
        private static readonly Color hexC0C0C0 = Color.FromRgb(0xC0, 0xC0, 0xC0);
        private static readonly Color hexFFD98F = Color.FromRgb(0xFF, 0xD9, 0x8F);
        private static readonly Color hexA67573 = Color.FromRgb(0xA6, 0x75, 0x73);
        private static readonly Color hex668080 = Color.FromRgb(0x66, 0x80, 0x80);
        private static readonly Color hex9E63B5 = Color.FromRgb(0x9E, 0x63, 0xB5);
        private static readonly Color hexD47A00 = Color.FromRgb(0xD4, 0x7A, 0x00);
        private static readonly Color hex940094 = Color.FromRgb(0x94, 0x00, 0x94);
        private static readonly Color hex429EB0 = Color.FromRgb(0x42, 0x9E, 0xB0);
        private static readonly Color hex57178F = Color.FromRgb(0x57, 0x17, 0x8F);
        private static readonly Color hex00C900 = Color.FromRgb(0x00, 0xC9, 0x00);
        private static readonly Color hex70D4FF = Color.FromRgb(0x70, 0xD4, 0xFF);
        private static readonly Color hexFFFFC7 = Color.FromRgb(0xFF, 0xFF, 0xC7);
        private static readonly Color hexD9FFC7 = Color.FromRgb(0xD9, 0xFF, 0xC7);
        private static readonly Color hexC7FFC7 = Color.FromRgb(0xC7, 0xFF, 0xC7);
        private static readonly Color hexA3FFC7 = Color.FromRgb(0xA3, 0xFF, 0xC7);
        private static readonly Color hex8FFFC7 = Color.FromRgb(0x8F, 0xFF, 0xC7);
        private static readonly Color hex61FFC7 = Color.FromRgb(0x61, 0xFF, 0xC7);
        private static readonly Color hex45FFC7 = Color.FromRgb(0x45, 0xFF, 0xC7);
        private static readonly Color hex30FFC7 = Color.FromRgb(0x30, 0xFF, 0xC7);
        private static readonly Color hex1FFFC7 = Color.FromRgb(0x1F, 0xFF, 0xC7);
        private static readonly Color hex00FF9C = Color.FromRgb(0x00, 0xFF, 0x9C);
        private static readonly Color hex00E675 = Color.FromRgb(0x00, 0xE6, 0x75);
        private static readonly Color hex00D452 = Color.FromRgb(0x00, 0xD4, 0x52);
        private static readonly Color hex00BF38 = Color.FromRgb(0x00, 0xBF, 0x38);
        private static readonly Color hex00AB24 = Color.FromRgb(0x00, 0xAB, 0x24);
        private static readonly Color hex4DC2FF = Color.FromRgb(0x4D, 0xC2, 0xFF);
        private static readonly Color hex4DA6FF = Color.FromRgb(0x4D, 0xA6, 0xFF);
        private static readonly Color hex2194D6 = Color.FromRgb(0x21, 0x94, 0xD6);
        private static readonly Color hex267DAB = Color.FromRgb(0x26, 0x7D, 0xAB);
        private static readonly Color hex266696 = Color.FromRgb(0x26, 0x66, 0x96);
        private static readonly Color hex175487 = Color.FromRgb(0x17, 0x54, 0x87);
        private static readonly Color hexD0D0E0 = Color.FromRgb(0xD0, 0xD0, 0xE0);
        private static readonly Color hexFFD123 = Color.FromRgb(0xFF, 0xD1, 0x23);
        private static readonly Color hexB8B8D0 = Color.FromRgb(0xB8, 0xB8, 0xD0);
        private static readonly Color hexA6544D = Color.FromRgb(0xA6, 0x54, 0x4D);
        private static readonly Color hex575961 = Color.FromRgb(0x57, 0x59, 0x61);
        private static readonly Color hex9E4FB5 = Color.FromRgb(0x9E, 0x4F, 0xB5);
        private static readonly Color hexAB5C00 = Color.FromRgb(0xAB, 0x5C, 0x00);
        private static readonly Color hex754F45 = Color.FromRgb(0x75, 0x4F, 0x45);
        private static readonly Color hex428296 = Color.FromRgb(0x42, 0x82, 0x96);
        private static readonly Color hex420066 = Color.FromRgb(0x42, 0x00, 0x66);
        private static readonly Color hex007D00 = Color.FromRgb(0x00, 0x7D, 0x00);
        private static readonly Color hex70ABFA = Color.FromRgb(0x70, 0xAB, 0xFA);
        private static readonly Color hex00BAFF = Color.FromRgb(0x00, 0xBA, 0xFF);
        private static readonly Color hex00A1FF = Color.FromRgb(0x00, 0xA1, 0xFF);
        private static readonly Color hex008FFF = Color.FromRgb(0x00, 0x8F, 0xFF);
        private static readonly Color hex0080FF = Color.FromRgb(0x00, 0x80, 0xFF);
        private static readonly Color hex006BFF = Color.FromRgb(0x00, 0x6B, 0xFF);
        private static readonly Color hex545CF2 = Color.FromRgb(0x54, 0x5C, 0xF2);
        private static readonly Color hex785CE3 = Color.FromRgb(0x78, 0x5C, 0xE3);
        private static readonly Color hex8A4FE3 = Color.FromRgb(0x8A, 0x4F, 0xE3);
        private static readonly Color hexA136D4 = Color.FromRgb(0xA1, 0x36, 0xD4);
        private static readonly Color hexB31FD4 = Color.FromRgb(0xB3, 0x1F, 0xD4);
        private static readonly Color hexB31FBA = Color.FromRgb(0xB3, 0x1F, 0xBA);
        private static readonly Color hexB30DA6 = Color.FromRgb(0xB3, 0x0D, 0xA6);
        private static readonly Color hexBD0D87 = Color.FromRgb(0xBD, 0x0D, 0x87);
        private static readonly Color hexC70066 = Color.FromRgb(0xC7, 0x00, 0x66);
        private static readonly Color hexCC0059 = Color.FromRgb(0xCC, 0x00, 0x59);
        private static readonly Color hexD1004F = Color.FromRgb(0xD1, 0x00, 0x4F);
        private static readonly Color hexD90045 = Color.FromRgb(0xD9, 0x00, 0x45);
        private static readonly Color hexE00038 = Color.FromRgb(0xE0, 0x00, 0x38);
        private static readonly Color hexE6002E = Color.FromRgb(0xE6, 0x00, 0x2E);
        private static readonly Color hexEB0026 = Color.FromRgb(0xEB, 0x00, 0x26);

        public Color GetAtomColor(IAtom atom)
        {
            return GetAtomColor(atom, hexB31FBA);
        }

        public Color GetAtomColor(IAtom atom, Color defaultColor)
        {
            var elem = ChemicalElement.OfSymbol(atom.Symbol).AtomicNumber;
            if (elem == AtomicNumbers.Unknown)
                elem = atom.AtomicNumber;
            switch (elem)
            {
                case AtomicNumbers.Helium:
                    return hexD9FFFF;
                case AtomicNumbers.Lithium:
                    return hexCC80FF;
                case AtomicNumbers.Beryllium:
                    return hexC2FF00;
                case AtomicNumbers.Boron:
                    return hexFFB5B5;
                case AtomicNumbers.Hydrogen:
                case AtomicNumbers.Carbon:
                case AtomicNumbers.Unknown:
                    return WPF.Media.Colors.Black;
                case AtomicNumbers.Nitrogen:
                    return hex3050F8;
                case AtomicNumbers.Oxygen:
                    return hexFF0D0D;
                case AtomicNumbers.Fluorine:
                    return hex90E050;
                case AtomicNumbers.Neon:
                    return hexB3E3F5;
                case AtomicNumbers.Sodium:
                    return hexAB5CF2;
                case AtomicNumbers.Magnesium:
                    return hex8AFF00;
                case AtomicNumbers.Aluminium:
                    return hexBFA6A6;
                case AtomicNumbers.Silicon:
                    return hexF0C8A0;
                case AtomicNumbers.Phosphorus:
                    return hexFF8000;
                case AtomicNumbers.Sulfur:
                    return hexFFFF30;
                case AtomicNumbers.Chlorine:
                    return hex1FF01F;
                case AtomicNumbers.Argon:
                    return hex80D1E3;
                case AtomicNumbers.Potassium:
                    return hex8F40D4;
                case AtomicNumbers.Calcium:
                    return hex3DFF00;
                case AtomicNumbers.Scandium:
                    return hexE6E6E6;
                case AtomicNumbers.Titanium:
                    return hexBFC2C7;
                case AtomicNumbers.Vanadium:
                    return hexA6A6AB;
                case AtomicNumbers.Chromium:
                    return hex8A99C7;
                case AtomicNumbers.Manganese:
                    return hex9C7AC7;
                case AtomicNumbers.Iron:
                    return hexE06633;
                case AtomicNumbers.Cobalt:
                    return hexF090A0;
                case AtomicNumbers.Nickel:
                    return hex50D050;
                case AtomicNumbers.Copper:
                    return hexC88033;
                case AtomicNumbers.Zinc:
                    return hex7D80B0;
                case AtomicNumbers.Gallium:
                    return hexC28F8F;
                case AtomicNumbers.Germanium:
                    return hex668F8F;
                case AtomicNumbers.Arsenic:
                    return hexBD80E3;
                case AtomicNumbers.Selenium:
                    return hexFFA100;
                case AtomicNumbers.Bromine:
                    return hexA62929;
                case AtomicNumbers.Krypton:
                    return hex5CB8D1;
                case AtomicNumbers.Rubidium:
                    return hex702EB0;
                case AtomicNumbers.Strontium:
                    return hex00FF00;
                case AtomicNumbers.Yttrium:
                    return hex94FFFF;
                case AtomicNumbers.Zirconium:
                    return hex94E0E0;
                case AtomicNumbers.Niobium:
                    return hex73C2C9;
                case AtomicNumbers.Molybdenum:
                    return hex54B5B5;
                case AtomicNumbers.Technetium:
                    return hex3B9E9E;
                case AtomicNumbers.Ruthenium:
                    return hex248F8F;
                case AtomicNumbers.Rhodium:
                    return hex0A7D8C;
                case AtomicNumbers.Palladium:
                    return hex006985;
                case AtomicNumbers.Silver:
                    return hexC0C0C0;
                case AtomicNumbers.Cadmium:
                    return hexFFD98F;
                case AtomicNumbers.Indium:
                    return hexA67573;
                case AtomicNumbers.Tin:
                    return hex668080;
                case AtomicNumbers.Antimony:
                    return hex9E63B5;
                case AtomicNumbers.Tellurium:
                    return hexD47A00;
                case AtomicNumbers.Iodine:
                    return hex940094;
                case AtomicNumbers.Xenon:
                    return hex429EB0;
                case AtomicNumbers.Caesium:
                    return hex57178F;
                case AtomicNumbers.Barium:
                    return hex00C900;
                case AtomicNumbers.Lanthanum:
                    return hex70D4FF;
                case AtomicNumbers.Cerium:
                    return hexFFFFC7;
                case AtomicNumbers.Praseodymium:
                    return hexD9FFC7;
                case AtomicNumbers.Neodymium:
                    return hexC7FFC7;
                case AtomicNumbers.Promethium:
                    return hexA3FFC7;
                case AtomicNumbers.Samarium:
                    return hex8FFFC7;
                case AtomicNumbers.Europium:
                    return hex61FFC7;
                case AtomicNumbers.Gadolinium:
                    return hex45FFC7;
                case AtomicNumbers.Terbium:
                    return hex30FFC7;
                case AtomicNumbers.Dysprosium:
                    return hex1FFFC7;
                case AtomicNumbers.Holmium:
                    return hex00FF9C;
                case AtomicNumbers.Erbium:
                    return hex00E675;
                case AtomicNumbers.Thulium:
                    return hex00D452;
                case AtomicNumbers.Ytterbium:
                    return hex00BF38;
                case AtomicNumbers.Lutetium:
                    return hex00AB24;
                case AtomicNumbers.Hafnium:
                    return hex4DC2FF;
                case AtomicNumbers.Tantalum:
                    return hex4DA6FF;
                case AtomicNumbers.Tungsten:
                    return hex2194D6;
                case AtomicNumbers.Rhenium:
                    return hex267DAB;
                case AtomicNumbers.Osmium:
                    return hex266696;
                case AtomicNumbers.Iridium:
                    return hex175487;
                case AtomicNumbers.Platinum:
                    return hexD0D0E0;
                case AtomicNumbers.Gold:
                    return hexFFD123;
                case AtomicNumbers.Mercury:
                    return hexB8B8D0;
                case AtomicNumbers.Thallium:
                    return hexA6544D;
                case AtomicNumbers.Lead:
                    return hex575961;
                case AtomicNumbers.Bismuth:
                    return hex9E4FB5;
                case AtomicNumbers.Polonium:
                    return hexAB5C00;
                case AtomicNumbers.Astatine:
                    return hex754F45;
                case AtomicNumbers.Radon:
                    return hex428296;
                case AtomicNumbers.Francium:
                    return hex420066;
                case AtomicNumbers.Radium:
                    return hex007D00;
                case AtomicNumbers.Actinium:
                    return hex70ABFA;
                case AtomicNumbers.Thorium:
                    return hex00BAFF;
                case AtomicNumbers.Protactinium:
                    return hex00A1FF;
                case AtomicNumbers.Uranium:
                    return hex008FFF;
                case AtomicNumbers.Neptunium:
                    return hex0080FF;
                case AtomicNumbers.Plutonium:
                    return hex006BFF;
                case AtomicNumbers.Americium:
                    return hex545CF2;
                case AtomicNumbers.Curium:
                    return hex785CE3;
                case AtomicNumbers.Berkelium:
                    return hex8A4FE3;
                case AtomicNumbers.Californium:
                    return hexA136D4;
                case AtomicNumbers.Einsteinium:
                    return hexB31FD4;
                case AtomicNumbers.Fermium:
                    return hexB31FBA;
                case AtomicNumbers.Mendelevium:
                    return hexB30DA6;
                case AtomicNumbers.Nobelium:
                    return hexBD0D87;
                case AtomicNumbers.Lawrencium:
                    return hexC70066;
                case AtomicNumbers.Rutherfordium:
                    return hexCC0059;
                case AtomicNumbers.Dubnium:
                    return hexD1004F;
                case AtomicNumbers.Seaborgium:
                    return hexD90045;
                case AtomicNumbers.Bohrium:
                    return hexE00038;
                case AtomicNumbers.Hassium:
                    return hexE6002E;
                case AtomicNumbers.Meitnerium:
                    return hexEB0026;
                default:
                    return defaultColor;
            }
        }
    }
}
