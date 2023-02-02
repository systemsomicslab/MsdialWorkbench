/* Copyright (C) 2009  Mark Rijnbeek <mark_rynbeek@users.sf.net>
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
    /// Atom coloring following RasMol/Chime Color scheme
    /// <see href="http://www.umass.edu/microbio/rasmol/rascolor.htm">http://www.umass.edu/microbio/rasmol/rascolor.htm</see>.
    /// </summary>
    // @cdk.module render
    public class RasmolColors : IAtomColorer
    {
        private readonly static Color DEFAULT = Color.FromRgb(255, 20, 147);

        private static IDictionary<string, Color> colorMap;

        /// <summary>
        /// Color map with RasMol/Chime Color RGB Values. Excepted H and C (too
        /// light).
        /// </summary>
        static RasmolColors()
        {
            colorMap = new Dictionary<string, Color>
            {
                ["C"] = Color.FromRgb(144, 144, 144),
                ["H"] = Color.FromRgb(144, 144, 144),
                ["O"] = Color.FromRgb(240, 0, 0),
                ["N"] = Color.FromRgb(143, 143, 255),
                ["S"] = Color.FromRgb(255, 200, 50),
                ["Cl"] = Color.FromRgb(0, 255, 0),
                ["B"] = Color.FromRgb(0, 255, 0),
                ["P"] = Color.FromRgb(255, 165, 0),
                ["Fe"] = Color.FromRgb(255, 165, 0),
                ["Ba"] = Color.FromRgb(255, 165, 0),
                ["Na"] = Color.FromRgb(0, 0, 255),
                ["Mg"] = Color.FromRgb(34, 139, 34),
                ["Zn"] = Color.FromRgb(165, 42, 42),
                ["Cu"] = Color.FromRgb(165, 42, 42),
                ["Ni"] = Color.FromRgb(165, 42, 42),
                ["Br"] = Color.FromRgb(165, 42, 42),
                ["Ca"] = Color.FromRgb(128, 128, 144),
                ["Mn"] = Color.FromRgb(128, 128, 144),
                ["Al"] = Color.FromRgb(128, 128, 144),
                ["Ti"] = Color.FromRgb(128, 128, 144),
                ["Cr"] = Color.FromRgb(128, 128, 144),
                ["Ag"] = Color.FromRgb(128, 128, 144),
                ["F"] = Color.FromRgb(218, 165, 32),
                ["Si"] = Color.FromRgb(218, 165, 32),
                ["Au"] = Color.FromRgb(218, 165, 32),
                ["I"] = Color.FromRgb(160, 32, 240),
                ["Li"] = Color.FromRgb(178, 34, 34),
                ["He"] = Color.FromRgb(255, 192, 203)
            };
        }

        /// <summary>
        /// Returns the Rasmol color for the given atom's element.
        /// </summary>
        /// <param name="atom">IAtom to get a color for</param>
        /// <returns>the atom's color according to this coloring scheme.</returns>
        public Color GetAtomColor(IAtom atom)
        {
            return GetAtomColor(atom, DEFAULT);
        }

        /// <summary>
        /// Returns the Rasmol color for the given atom's element, or
        /// defaults to the given color if no color is defined.
        /// </summary>
        /// <param name="atom">IAtom to get a color for</param>
        /// <param name="defaultColor">Color returned if this scheme does not define a color for the passed IAtom</param>
        /// <returns>the atom's color according to this coloring scheme.</returns>
        public Color GetAtomColor(IAtom atom, Color defaultColor)
        {
            var color = defaultColor;
            var symbol = atom.Symbol;
            if (colorMap.ContainsKey(symbol))
            {
                color = colorMap[symbol];
            }
            return color;
        }
    }
}
