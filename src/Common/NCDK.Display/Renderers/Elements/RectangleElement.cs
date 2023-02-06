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

using System.Windows.Media;
using WPF = System.Windows;

namespace NCDK.Renderers.Elements
{
    /// <summary>
    /// A rectangle, with width and height.
    /// </summary>
    // @cdk.module renderbasic
    public class RectangleElement : IRenderingElement
    {
        /// <summary>The base point of the rectangle. </summary>
        public readonly WPF.Point Coord;

        /// <summary>The width of the rectangle. </summary>
        public readonly double Width;

        /// <summary>The height of the rectangle. </summary>
        public readonly double Height;

        /// <summary>If true, the rectangle is drawn as filled. </summary>
        public readonly bool Filled;

        /// <summary>The color of the rectangle. </summary>
        public readonly WPF.Media.Color Color;

        /// <summary>
        /// Make a rectangle from two opposite corners (x1, y1) and (x2, y2).
        /// </summary>
        /// <param name="coord1">the coordinate of the first point</param>
        /// <param name="coord2">the coordinate of the second point</param>
        /// <param name="color">the color of the rectangle</param>
        public RectangleElement(WPF.Point coord1, WPF.Point coord2, WPF.Media.Color color)
            : this(coord1, coord2.X - coord1.X, coord2.Y - coord1.Y, false, color)
        { }

        public RectangleElement(WPF.Rect rect, WPF.Media.Color color)
            : this(rect.TopLeft, rect.BottomRight, color)
        { }

        /// <summary>
        /// Make a rectangle centered on (x, y).
        /// </summary>
        /// <param name="coord">coordinate of the center of the rectangle</param>
        /// <param name="width">width of the rectangle</param>
        /// <param name="height">height of the rectangle</param>
        /// <param name="filled">if true, the rectangle is drawn as filled</param>
        /// <param name="color">the color of the rectangle</param>
        public RectangleElement(WPF.Point coord, double width, double height, bool filled, WPF.Media.Color color)
        {
            this.Coord = coord;
            this.Width = width;
            this.Height = height;
            this.Filled = filled;
            this.Color = color;
        }

        public RectangleElement(double height)
        {
            Height = height;
        }

        public virtual void Accept(IRenderingVisitor v, Transform transform)
        {
            v.Visit(this, transform);
        }

        /// <inheritdoc/>
        public virtual void Accept(IRenderingVisitor visitor)
        {
            visitor.Visit(this);
        }
    }
}
