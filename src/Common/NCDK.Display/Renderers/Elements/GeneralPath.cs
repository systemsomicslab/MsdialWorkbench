/*
 * Copyright (C) 2009  Arvid Berg <goglepox@users.sourceforge.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All I ask is that proper credit is given for my work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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

namespace NCDK.Renderers.Elements
{
    /// <summary>
    /// A path of rendering elements from the elements.path package.
    /// </summary>
    // @author Arvid
    // @cdk.module renderbasic
    public class GeneralPath : IRenderingElement
    {
        /// <summary>The color of the path.</summary>
        public readonly Color Color;

        /// <summary>The width of the stroke.</summary>
        public readonly double StrokeWith;

        /// <summary>Fill the shape instead of drawing outline.</summary>
        public readonly bool Fill;

        /// <summary>The elements in the path.</summary>
        public readonly PathGeometry Elements;

        /// <summary>Winding rule for determining path interior.</summary>
        public readonly FillRule Winding;

        /// <summary>
        /// Make a path from a list of path elements.
        /// </summary>
        /// <param name="elements">the elements that make up the path</param>
        /// <param name="color">the color of the path</param>
        public GeneralPath(PathGeometry elements, Color color)
           : this(elements, color, 1, true)
        { }

        /// <summary>
        /// Make a path from a list of path elements.
        /// </summary>
        /// <param name="elements">the elements that make up the path</param>
        /// <param name="color">the color of the path</param>
        private GeneralPath(PathGeometry elements, Color color, double stroke, bool fill)
        {
            this.Elements = elements;
            this.Color = color;
            this.Fill = fill;
            this.StrokeWith = stroke;
        }

        /// <summary>
        /// Recolor the path with the specified color.
        /// </summary>
        /// <param name="newColor">new path color</param>
        /// <returns>the recolored path</returns>
        public GeneralPath Recolor(Color newColor)
        {
            return new GeneralPath(Elements, newColor, StrokeWith, Fill);
        }

        /// <summary>
        /// Outline the general path with the specified stroke size.
        /// </summary>
        /// <param name="newStroke">new stroke size</param>
        /// <returns>the outlined path</returns>
        public GeneralPath Outline(double newStroke)
        {
            return new GeneralPath(Elements, Color, newStroke, false);
        }

        public virtual void Accept(IRenderingVisitor v, Transform transform)
        {
            v.Visit(this, transform);
        }

        public virtual void Accept(IRenderingVisitor v)
        {
            v.Visit(this);
        }

        /// <summary>
        /// Create a filled path of the specified Java 2D Shape and color.
        /// </summary>
        /// <param name="shape">shape</param>
        /// <param name="color">the color to fill the shape with</param>
        /// <returns>a new general path</returns>
        public static GeneralPath ShapeOf(Geometry shape, Color color)
        {
            PathGeometry pathIt = shape.GetOutlinedPathGeometry();
            pathIt.FillRule = FillRule.EvenOdd;
            return new GeneralPath(pathIt, color, 0, true);
        }

        /// <summary>
        /// Create an outline path of the specified shape and color.
        /// </summary>
        /// <param name="shape">shape</param>
        /// <param name="color">the color to draw the outline with</param>
        /// <returns>a new general path</returns>
        public static GeneralPath OutlineOf(Geometry shape, double stroke, Color color)
        {
            if (shape is PathGeometry)
            {
                return new GeneralPath((PathGeometry)shape, color, stroke, false);
            }
            else
            {
                var pathIt = shape.GetWidenedPathGeometry(new Pen(new SolidColorBrush(color), stroke));
                return new GeneralPath(pathIt, color, 0, true);
            }
        }
    }
}
