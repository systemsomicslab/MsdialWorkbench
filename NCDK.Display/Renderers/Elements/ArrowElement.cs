/* Copyright (C) 2009  Stefan Kuhn <shk3@users.sf.net>
 *               2011  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@list.sourceforge.net
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
    /// <see cref="IRenderingElement"/> for linear arrows.
    /// </summary>
    // @cdk.module renderbasic
    public class ArrowElement : IRenderingElement
    {
        /// <summary>coordinate of the point where the arrow starts.</summary>
        public readonly Point Start;
        /// <summary>coordinate of the point where the arrow ends.</summary>
        public readonly Point End;
        /// <summary>Width of the arrow line.</summary>
        public readonly double Width;
        /// <summary>Color of the arrow.</summary>
        public readonly Color Color;
        /// <summary>boolean that is <see langword="true"/> if the arrow points from start to end, <see langword="false"/> if from end to start.</summary>
        public readonly bool Direction;

        /// <summary>
        /// Constructor for an arrow element, based on starting point, end point, width,
        /// direction, and color.
        /// </summary>
        /// <param name="start">Coordinate of the point where the arrow starts.</param>
        /// <param name="end">Coordinate of the point where the arrow ends.</param>
        /// <param name="width">width of the arrow line.</param>
        /// <param name="direction">true is the arrow points from start to end, false if from end to start</param>
        /// <param name="color"><see cref="System.Windows.Media.Color"/> of the arrow</param>
        public ArrowElement(Point start, Point end, double width, bool direction, Color color)
        {
            this.Start = start;
            this.End = end;
            this.Width = width;
            this.Color = color;
            this.Direction = direction;
        }

        public virtual void Accept(IRenderingVisitor v, Transform transform)
        {
            v.Visit(this, transform);
        }

        /// <inheritdoc/>
        public virtual void Accept(IRenderingVisitor v)
        {
            v.Visit(this);
        }
    }
}
