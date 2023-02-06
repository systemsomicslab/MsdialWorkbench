/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using WPF = System.Windows;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// Immutable outline of text. The outline is maintained as a Java 2D shape
    /// instance and can be transformed. As an immutable instance, transforming the
    /// outline creates a new instance.
    /// </summary>
    // @author John May
    sealed class TextOutline
    {
        /// <summary>
        /// The original text.
        /// </summary>
        private readonly string text;

        /// <summary>
        /// The outline of the text (untransformed).
        /// </summary>
        private readonly Geometry outline;

        /// <summary>
        /// Transform applied to outline.
        /// </summary>
        private readonly Transform transform;

        private readonly Typeface typeface;
        private readonly double emSize;

        /// <summary>
        /// Create an outline of text in provided font.
        /// </summary>
        /// <param name="text">the text to create an outline of</param>
        /// <param name="typeface">the style and shape of font that defines the outline</param>
        /// <param name="emSize">the size of font that defines the outline</param>
        public TextOutline(string text, Typeface typeface, double emSize)
            : this(text, typeface, emSize, WPF.Media.Transform.Identity)
        { }

        /// <summary>
        /// Create an outline of text and the glyphs for that text.
        /// </summary>
        /// <param name="text">the text to create an outline of</param>
        /// <param name="typeface">the style and shape of font that defines the outline</param>
        /// <param name="emSize">the size of font that defines the outline</param>
        /// <param name="transform">the transform</param>
        private TextOutline(string text, Typeface typeface, double emSize, Transform transform)
            : this(text, typeface, emSize, 
                   new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, typeface, emSize, Brushes.Transparent).BuildGeometry(new Point()),
                   transform)
        { }

        /// <summary>
        /// Internal constructor, requires all attributes.
        /// </summary>
        /// <param name="text">the text</param>
        /// <param name="typeface">the style and shape of font that defines the outline</param>
        /// <param name="emSize">the size of font that defines the outline</param>
        /// <param name="outline">the outline of the glyphs</param>
        /// <param name="transform">the transform</param>
        private TextOutline(string text, Typeface typeface, double emSize, Geometry outline, Transform transform)
        {
            this.text = text;
            this.typeface = typeface;
            this.emSize = emSize;
            this.outline = outline;
            this.transform = transform;
        }

        /// <summary>
        /// The text which the outline displays.
        /// <returns>the text</returns>
        /// </summary>
        public string Text => text;

        /// <summary>
        /// Access the transformed outline of the text.
        /// </summary>
        /// <returns>transformed outline</returns>
        public Geometry GetOutline()
        {
            var c = outline.Clone();
            c.Transform = new MatrixTransform(c.Transform.Value * transform.Value);
            return c;
        }

        /// <summary>
        /// Access the transformed bounds of the outline text.
        /// </summary>
        /// <returns>transformed bounds</returns>
        public Rect GetBounds() => TransformedBounds(outline);

        /// <summary>
        /// Access the transformed logical bounds of the outline text.
        /// </summary>
        /// <returns>logical bounds</returns>
        public Rect LogicalBounds
            => TransformedBounds(new RectangleGeometry(outline.Bounds));

        /// <summary>
        /// Access the bounds of a shape that have been transformed.
        /// </summary>
        /// <param name="shape">any shape</param>
        /// <returns>the bounds of the shape transformed</returns>
        private Rect TransformedBounds(Geometry shape)
        {
            var rectangle2D = shape.Bounds;

            var minPoint = new Point(rectangle2D.Left, rectangle2D.Top);
            var maxPoint = new Point(rectangle2D.Right, rectangle2D.Bottom);

            minPoint = transform.Transform(minPoint);
            maxPoint = transform.Transform(maxPoint);

            // may be flipped by transformation
            var minX = Math.Min(minPoint.X, maxPoint.X);
            var maxX = Math.Max(minPoint.X, maxPoint.X);
            var minY = Math.Min(minPoint.Y, maxPoint.Y);
            var maxY = Math.Max(minPoint.Y, maxPoint.Y);

            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// The transformed center of the whole outline.
        /// </summary>
        public Point GetCenter()
        {
            var bounds = GetBounds();
            return new Point((bounds.Left + bounds.Right) / 2, (bounds.Top + bounds.Bottom) / 2);
        }

        /// <summary>
        /// Access the transformed center of the first glyph outline.
        /// </summary>
        /// <returns>center of first glyph outline</returns>
        public Point GetFirstGlyphCenter()
        {
            return GetGlyphCenter(0);
        }

        /// <summary>
        /// Access the transformed center of the last glyph outline.
        /// </summary>
        /// <returns>center of last glyph outline</returns>
        public Point GetLastGlyphCenter()
        {
            return GetGlyphCenter(text.Length - 1);
        }

        /// <summary>
        /// Determines the transformed centre of a specified glyph.
        /// </summary>
        /// <param name="index">glyph index</param>
        /// <returns>center point</returns>
        private Point GetGlyphCenter(int index)
        {
            if (text.Length == 1)
            {
                return GetCenter();
            }
            else if (index == 0)
            {
                var o1 = new TextOutline(text.Substring(0, 1), typeface, emSize, transform);
                var center = o1.GetCenter();
                return transform.Transform(center);
            }
            else
            {
                var o1 = new TextOutline(text.Substring(0, index), typeface, emSize, transform);
                var o2 = new TextOutline(text.Substring(0, index + 1), typeface, emSize, transform);
                var b1 = o1.GetBounds();
                var b2 = o2.GetBounds();
                return new Point((b1.Right + b2.Right) / 2, (b2.Top + b2.Bottom) / 2);
            }
        }

        /// <summary>
        /// Add a transformation to the outline.
        /// </summary>
        /// <param name="nextTransform">new transformation</param>
        /// <returns>new text outline</returns>
        public TextOutline Transform(Transform nextTransform)
        {
            return new TextOutline(text, 
                                   typeface,
                                   emSize,
                                   outline, 
                                   new MatrixTransform(transform.Value * nextTransform.Value));
        }

        /// <summary>
        /// Convenience function to resize the outline and maintain the existing
        /// center point.
        /// </summary>
        /// <param name="scaleX">scale x-axis</param>
        /// <param name="scaleY">scale y-axis</param>
        /// <returns>resized outline</returns>
        public TextOutline Resize(double scaleX, double scaleY)
        {
            var center = GetCenter();
            var t = Matrix.Identity;
            t.ScaleAtPrepend(scaleX, scaleY, center.X, center.Y);
            return Transform(new MatrixTransform(t));
        }

        /// <summary>
        /// Convenience function to translate the outline.
        /// </summary>
        /// <param name="translateX">x-axis translation</param>
        /// <param name="translateY">y-axis translation</param>
        /// <returns>translated outline</returns>
        public TextOutline Translate(double translateX, double translateY)
        {
            var m = new TranslateTransform(translateX, translateY);
            return Transform(m);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var bounds = GetBounds();
            var sb = new StringBuilder(25);
            sb.Append(text);
            sb.Append(" [x=").Append(FormatDouble(bounds.X));
            sb.Append(", y=").Append(FormatDouble(bounds.Y));
            sb.Append(", w=").Append(FormatDouble(bounds.Width));
            sb.Append(", h=").Append(FormatDouble(bounds.Height));
            sb.Append(']');
            return sb.ToString();
        }

        /// <summary>
        /// Format a double - displayed as two decimal places.
        /// </summary>
        /// <param name="value">number value</param>
        /// <returns>string of formatted double</returns>
        public static string FormatDouble(double value)
        {
            return value.ToString("F2");
        }
    }
}
