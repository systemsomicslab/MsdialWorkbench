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
    /// Class defining the color which with atoms are colored.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This scheme used the atomic partial charge to determine
    /// the Atom's color:
    /// uncharged atoms are colored white, positively charged
    /// atoms are blue, and negatively charge atoms are red.
    /// </para>
    /// </remarks>
    // @cdk.module  render
    // @cdk.keyword atom coloring, partial charges
    public class PartialAtomicChargeColors : IAtomColorer
    {
        /// <summary>
        /// Returns the a color reflecting the given atom's partial charge.
        /// </summary>
        /// <param name="atom">IAtom to get a color for</param>
        /// <returns>the color for the given atom.</returns>
        public Color GetAtomColor(IAtom atom)
        {
            return GetAtomColor(atom, WPF.Media.Colors.White);
        }

        /// <summary>
        /// Returns the a color reflecting the given atom's partial charge, or
        /// defaults to the given color if no color is defined.
        /// </summary>
        /// <param name="atom">IAtom to get a color for</param>
        /// <param name="defaultColor">Color returned if this scheme does not define a color for the passed IAtom</param>
        /// <returns>the color for the given atom.</returns>
        public Color GetAtomColor(IAtom atom, Color defaultColor)
        {
            var color = defaultColor;
            if (atom.Charge == null)
                return defaultColor;
            var charge = atom.Charge.Value;
            if (charge > 0.0)
            {
                if (charge < 1.0)
                {
                    int index = 255 - (int)(charge * 255.0);
                    color = Color.FromRgb((byte)index, (byte)index, 255);
                }
                else
                {
                    color = WPF.Media.Colors.Blue;
                }
            }
            else if (charge < 0.0)
            {
                if (charge > -1.0)
                {
                    int index = 255 + (int)(charge * 255.0);
                    color = Color.FromRgb(255, (byte)index, (byte)index);
                }
                else
                {
                    color = WPF.Media.Colors.Red;
                }
            }
            return color;
        }
    }
}
