/* Copyright (C) 2008  Arvid Berg <goglepox@users.sf.net>
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
    /// A text element with added information.
    /// </summary>
    // @cdk.module renderbasic
    public class AtomSymbolElement : TextElement
    {
        /// <summary>The formal charge.</summary>
        public readonly int FormalCharge;

        /// <summary>The hydrogen count.</summary>
        public readonly int HydrogenCount;

        /// <summary>The hydrogen alignment.</summary>
        public readonly int Alignment;

        public AtomSymbolElement(Point point, string symbol, int? formalCharge, int? hydrogenCount, int alignment, Color color)
            : base(point, symbol, color)
        {
            this.FormalCharge = formalCharge ?? -1;
            this.HydrogenCount = hydrogenCount ?? -1;
            this.Alignment = alignment;
        }

        /// <inheritdoc/>
        public override void Accept(IRenderingVisitor v)
        {
            v.Visit(this);
        }
    }
}
