/*  Copyright (C) 2008  Arvid Berg <goglepox@users.sf.net>
 *
 *  Contact: cdk-devel@list.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Windows;
using System.Windows.Media;

namespace NCDK.Renderers.Elements
{
    /// <summary>
    /// Rendering element that shows the <see cref="IAtom"/> mass number information.
    /// </summary>
    // @cdk.module renderextra
    public class AtomMassSymbolElement : AtomSymbolElement
    {
        /// <summary>The <see cref="IAtom"/>s mass number.</summary>
        public readonly int AtomMassNumber;

        /// <summary>
        /// Constructs a new <see cref="TextElement"/> displaying the atom's mass number information.
        /// </summary>
        /// <param name="coord">screen coordinate of where the text is displayed</param>
        /// <param name="symbol">the element symbol of the atom</param>
        /// <param name="formalCharge">the formal charge of the atom</param>
        /// <param name="hydrogenCount">the number of implicit hydrogens of the atom</param>
        /// <param name="alignment">indicator of how the text should be aligned</param>
        /// <param name="atomMass">the mass number of the atom</param>
        /// <param name="color">the color</param>
        public AtomMassSymbolElement(Point coord, string symbol, int? formalCharge, int? hydrogenCount, int alignment, int? atomMass, Color color)
            : base(coord, symbol, formalCharge, hydrogenCount, alignment, color)
        {
            this.AtomMassNumber = atomMass ?? -1;
        }
    }
}
