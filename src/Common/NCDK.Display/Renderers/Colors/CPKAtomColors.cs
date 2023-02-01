/* Copyright (C) 1997-2007  Chris Pudney
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

using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace NCDK.Renderers.Colors
{
    /// <summary>
    /// Colors atoms using CPK color scheme <token>cdk-cite-BER2001</token>. 
    /// </summary>
    // @cdk.module  render
    // @cdk.keyword atom coloring, CPK
    [Obsolete(nameof(JmolColors) + " provides more comprehensive CPK color pallet")]
    [Serializable]
    public class CPKAtomColors : IAtomColorer
    {
        ////////////
        // CONSTANTS
        ////////////

        // CPK colours.
        private static readonly Color LightGrey = Color.FromRgb(0xC8, 0xC8, 0xC8);
        private static readonly Color SkyBlue = Color.FromRgb(0x8F, 0x8F, 0xFF);
        private static readonly Color Red = Color.FromRgb(0xF0, 0x00, 0x00);
        private static readonly Color Yellow = Color.FromRgb(0xFF, 0xC8, 0x32);
        private static readonly Color White = Color.FromRgb(0xFF, 0xFF, 0xFF);
        private static readonly Color Pink = Color.FromRgb(0xFF, 0xC0, 0xCB);
        private static readonly Color GoldenRod = Color.FromRgb(0xDA, 0xA5, 0x20);
        private static readonly Color Blue = Color.FromRgb(0x00, 0x00, 0xFF);
        private static readonly Color Orange = Color.FromRgb(0xFF, 0xA5, 0x00);
        private static readonly Color DarkGrey = Color.FromRgb(0x80, 0x80, 0x90);
        private static readonly Color Brown = Color.FromRgb(0xA5, 0x2A, 0x2A);
        private static readonly Color Purple = Color.FromRgb(0xA0, 0x20, 0xF0);
        private static readonly Color DeepPink = Color.FromRgb(0xFF, 0x14, 0x93);
        private static readonly Color Green = Color.FromRgb(0x00, 0xFF, 0x00);
        private static readonly Color FireBrick = Color.FromRgb(0xB2, 0x22, 0x22);
        private static readonly Color ForestGreen = Color.FromRgb(0x22, 0x8B, 0x22);

        private static IReadOnlyDictionary<string, Color> AtomColorsSymbol { get; } = new Dictionary<string, Color>
        {
            // Colors keyed on (uppercase) atomic symbol.
            ["H"] = White,
            ["HE"] = Pink,
            ["LI"] = FireBrick,
            ["B"] = Green,
            ["C"] = LightGrey,
            ["N"] = SkyBlue,
            ["O"] = Red,
            ["F"] = GoldenRod,
            ["NA"] = Blue,
            ["MG"] = ForestGreen,
            ["AL"] = DarkGrey,
            ["SI"] = GoldenRod,
            ["P"] = Orange,
            ["S"] = Yellow,
            ["CL"] = Green,
            ["CA"] = DarkGrey,
            ["TI"] = DarkGrey,
            ["CR"] = DarkGrey,
            ["MN"] = DarkGrey,
            ["FE"] = Orange,
            ["NI"] = Brown,
            ["CU"] = Brown,
            ["ZN"] = Brown,
            ["BR"] = Brown,
            ["AG"] = DarkGrey,
            ["I"] = Purple,
            ["BA"] = Orange,
            ["AU"] = GoldenRod
        };

        // The atom color look-up table.
        private static IReadOnlyDictionary<int, Color> AtomColorsMassNum { get; } = new Dictionary<int, Color>
        {
            // Colors keyed on atomic number.
            [1] = AtomColorsSymbol["H"],
            [2] = AtomColorsSymbol["HE"],
            [3] = AtomColorsSymbol["LI"],
            [5] = AtomColorsSymbol["B"],
            [6] = AtomColorsSymbol["C"],
            [7] = AtomColorsSymbol["N"],
            [8] = AtomColorsSymbol["O"],
            [9] = AtomColorsSymbol["F"],
            [11] = AtomColorsSymbol["NA"],
            [12] = AtomColorsSymbol["MG"],
            [13] = AtomColorsSymbol["AL"],
            [14] = AtomColorsSymbol["SI"],
            [15] = AtomColorsSymbol["P"],
            [16] = AtomColorsSymbol["S"],
            [17] = AtomColorsSymbol["CL"],
            [20] = AtomColorsSymbol["CA"],
            [22] = AtomColorsSymbol["TI"],
            [24] = AtomColorsSymbol["CR"],
            [25] = AtomColorsSymbol["MN"],
            [26] = AtomColorsSymbol["FE"],
            [28] = AtomColorsSymbol["NI"],
            [29] = AtomColorsSymbol["CU"],
            [30] = AtomColorsSymbol["ZN"],
            [35] = AtomColorsSymbol["BR"],
            [47] = AtomColorsSymbol["AG"],
            [53] = AtomColorsSymbol["I"],
            [56] = AtomColorsSymbol["BA"],
            [79] = AtomColorsSymbol["AU"]
        };

        //////////
        // METHODS
        //////////

        /// <summary>
        /// Returns the font color for atom given atom.
        /// </summary>
        /// <param name="atom">the atom.</param>
        /// <returns>A color for the atom.</returns>
        public Color GetAtomColor(IAtom atom)
        {
            return GetAtomColor(atom, DeepPink);
        }

        /// <summary>
        /// Returns the font color for atom given atom.
        /// </summary>
        /// <param name="atom">the atom.</param>
        /// <param name="defaultColor">atom default color.</param>
        /// <returns>A color for the atom.  The default colour is used if none is found for the atom.</returns>
        public Color GetAtomColor(IAtom atom, Color defaultColor)
        {
            var color = defaultColor;
            var symbol = atom.Symbol.ToUpperInvariant();
            if (AtomColorsMassNum.ContainsKey(atom.AtomicNumber))
            {
                color = AtomColorsMassNum[atom.AtomicNumber]; // lookup by atomic number.
            }
            else if (AtomColorsSymbol.ContainsKey(symbol))
            {
                color = AtomColorsSymbol[symbol]; // lookup by atomic symbol.
            }

            return Color.FromRgb(color.R, color.G, color.B); // return atom copy.
        }
    }
}
