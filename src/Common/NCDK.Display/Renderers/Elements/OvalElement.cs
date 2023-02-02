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
    /// An oval element (should) have both a width and a height.
    /// </summary>
    // @cdk.module renderbasic
    public class OvalElement : IRenderingElement
    {
        /// <summary>The center of the oval.</summary>
        public Point Coord { get; private set; }

        /// <summary>The radius of the oval.</summary>
        public double Radius { get; private set; }

        /// <summary>The stroke width.</summary>
        public double Stroke { get; private set; }

        /// <summary>If true, draw the oval as filled. </summary>
        public bool Fill { get; private set; }

        /// <summary>The color to draw the oval. </summary>
        public Color Color { get; private set; }

        /// <summary>
        /// Make an oval with a default radius of 10.
        /// </summary>
        /// <param name="coord">the coordinate of the center of the oval</param>
        /// <param name="color">the color of the oval</param>
        public OvalElement(Point coord, Color color)
            : this(coord, 10, color)
        { }

        /// <summary>
        /// Make an oval with the supplied radius.
        /// </summary>
        /// <param name="coord">the coordinate of the center of the oval</param>
        /// <param name="radius">the radius of the oval</param>
        /// <param name="color">the color of the oval</param>
        public OvalElement(Point coord, double radius, Color color)
            : this(coord, radius, true, color)
        { }

        /// <summary>
        /// Make an oval with a particular fill and color.
        /// </summary>
        /// <param name="coord">the coordinate of the center of the oval</param>
        /// <param name="radius">the radius of the oval</param>
        /// <param name="fill">if true, fill the oval when drawing</param>
        /// <param name="color">the color of the oval</param>
        public OvalElement(Point coord, double radius, double stroke, bool fill, Color color)
        {
            this.Coord = coord;
            this.Radius = radius;
            this.Stroke = stroke;
            this.Fill = fill;
            this.Color = color;
        }

        /// <summary>
        /// Make an oval with a particular fill and color.
        /// </summary>
        /// <param name="coord">the coordinate of the center of the oval</param>
        /// <param name="radius">the radius of the oval</param>
        /// <param name="fill">if true, fill the oval when drawing</param>
        /// <param name="color">the color of the oval</param>
        public OvalElement(Point coord, double radius, bool fill, Color color)
            : this(coord, radius, 1, fill, color)
        {
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
