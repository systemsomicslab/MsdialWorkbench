/* Copyright (C) 2009  Gilleain Torrance <gilleain@users.sf.net>
 *
 * Contact: cdk-devel@list.sourceforge.net
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

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using static NCDK.Renderers.Elements.TextGroupElement.Position;

namespace NCDK.Renderers.Elements
{
    /// <summary>
    /// A group of text elements, particularly the element symbol (eg: "C")
    /// surrounded by other annotations such as mass number, charge, etc. These
    /// annotation elements are the 'children' of the parent text element.
    /// </summary>
    // @cdk.module renderbasic
    public class TextGroupElement : TextElement
    {
        /// <summary>
        /// Compass-point positions for text element annotation children.
        /// </summary>
        public enum Position
        {
            NW, SW, SE, NE, S, N, W, E,
        }

        public static class PositionTools
        {
            public static Position[] Values { get; } = new[] { NW, SW, SE, NE, S, N, W, E,};
        }

        /// <summary>
        /// A string of text that should be shown around the parent.
        /// </summary>
        // @author maclean
        public class Child
        {
            /// <summary>
            /// The text of this child.
            /// </summary>
            public readonly string text;

            /// <summary>
            /// A subscript (if any) for the child.
            /// </summary>
            public readonly string subscript;

            /// <summary>
            /// The position of the child relative to the parent.
            /// </summary>
            public readonly Position position;

            /// <summary>
            /// Make a child element with the specified text and position.
            /// </summary>
            /// <param name="text">the child's text</param>
            /// <param name="position">the position of the child relative to the parent</param>
            public Child(string text, Position position)
            {
                this.text = text;
                this.position = position;
                this.subscript = null;
            }

            /// <summary>
            /// Make a child element with the specified text, subscript, and position.
            /// </summary>
            /// <param name="text">the child's text</param>
            /// <param name="subscript">a subscript for the child</param>
            /// <param name="position">the position of the child relative to the parent</param>
            public Child(string text, string subscript, Position position)
            {
                this.text = text;
                this.position = position;
                this.subscript = subscript;
            }
        }

        /// <summary>
        /// The child text elements.
        /// </summary>
        public readonly IList<Child> children;

        /// <summary>
        /// Make a text group at (x, y) with the text and color given.
        /// </summary>
        /// <param name="coord">the coordinate of the center of the text</param>
        /// <param name="text">the text to render</param>
        /// <param name="color">the color of the text</param>
        public TextGroupElement(Point coord, string text, Color color)
            : base(coord, text, color)
        {
            this.children = new List<Child>();
        }

        /// <summary>
        /// Add a child text element.
        /// </summary>
        /// <param name="text">the child text to add</param>
        /// <param name="position">the position of the child relative to this parent</param>
        public void AddChild(string text, Position position)
        {
            this.children.Add(new Child(text, position));
        }

        /// <summary>
        /// Add a child text element with a subscript.
        /// </summary>
        /// <param name="text">the child text to add</param>
        /// <param name="subscript">a subscript for the child</param>
        /// <param name="position">the position of the child relative to the parent</param>
        public void AddChild(string text, string subscript, Position position)
        {
            this.children.Add(new Child(text, subscript, position));
        }

        /// <inheritdoc/>
        public override void Accept(IRenderingVisitor v)
        {
            v.Visit(this);
        }
    }
}
