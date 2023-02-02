/* Copyright (C) 2008 Gilleain Torrance <gilleain.torrance@gmail.com>
 *               2011 Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Renderers.Elements;
using NCDK.Renderers.Fonts;
using System.Globalization;
using System.Windows.Media;
using WPF = System.Windows;

namespace NCDK.Renderers.Visitors
{
    /// <summary>
    /// Partial implementation of the <see cref="IDrawVisitor"/> interface for the WPF
    /// widget toolkit, allowing molecules to be rendered with toolkits based on WPF.
    /// </summary>
    // @cdk.module renderawt
    public abstract class AbstractWPFDrawVisitor : IDrawVisitor
    {
        /// <summary>
        /// Calculates the boundaries of a text string in screen coordinates.
        /// </summary>
        /// <param name="text">the text string</param>
        /// <param name="coord">the world x-coordinate of where the text should be placed</param>
        /// <returns>the screen coordinates</returns>
        internal static WPF.Rect GetTextBounds(string text, WPF::Point coord, Typeface typeface, double emSize)
        {
            var ft = new FormattedText(text, CultureInfo.CurrentCulture, WPF.FlowDirection.LeftToRight, typeface, emSize, Brushes.Black);
            var bounds = new WPF.Rect(0, 0, ft.Width, ft.Height);

            double widthPad = 3;
            double heightPad = 1;

            var width = bounds.Width + widthPad;
            var height = bounds.Height + heightPad;
            var point = coord;
            return new WPF.Rect(point.X - width / 2, point.Y - height / 2, width, height);
        }

        /// <summary>
        /// Calculates the base point where text should be rendered, as text in Java
        /// is typically placed using the left-lower corner point in screen coordinates.
        /// However, because the Java coordinate system is inverted in the y-axis with
        /// respect to scientific coordinate systems (Java has 0,0 in the top left
        /// corner, while in science we have 0,0 in the lower left corner), some
        /// special action is needed, involving the size of the text.
        /// </summary>
        /// <param name="text">the text string</param>
        /// <param name="coord">the world coordinate of where the text should be placed</param>
        /// <returns>the screen coordinates</returns>
        internal static WPF.Point GetTextBasePoint(string text, WPF.Point coord, Typeface typeface, double emSize)
        {
            var ft = new FormattedText(text, CultureInfo.CurrentCulture, WPF.FlowDirection.LeftToRight, typeface, emSize, Brushes.Black);
            var stringBounds = new WPF.Rect(0, 0, ft.Width, ft.Height);
            var point = coord;
            var baseX = point.X - (stringBounds.Width / 2);

            // correct the baseline by the ascent
            var baseY = point.Y + (typeface.CapsHeight - stringBounds.Height / 2);
            return new WPF.Point(baseX, baseY);
        }

        /// <summary>
        /// Obtain the exact bounding box of the <paramref name="text"/> in the provided
        /// graphics environment.
        /// </summary>
        /// <param name="text">the text to obtain the bounds of</param>
        /// <returns>bounds of the text</returns>
        /// <seealso cref="Typeface"/>
        internal static WPF.Rect GetTextBounds(string text, Typeface typeface, double emSize)
        {
            var ft = new FormattedText(text, CultureInfo.CurrentCulture, WPF.FlowDirection.LeftToRight, typeface, emSize, Brushes.Black);
            return new WPF.Rect(0, 0, ft.Width, ft.Height);
        }

        public abstract IFontManager FontManager { get; set; }
        public abstract RendererModel RendererModel { get; set; }
        public abstract void Visit(IRenderingElement element);
        public abstract void Visit(IRenderingElement element, Transform transform);
    }
}
