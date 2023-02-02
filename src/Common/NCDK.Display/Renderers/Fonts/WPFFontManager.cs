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

using System.Windows.Media;
using WPF = System.Windows;

namespace NCDK.Renderers.Fonts
{
    /// <summary>
    /// WPF-specific font manager.
    /// </summary>
    // @cdk.module renderbasic
    public class WPFFontManager : AbstractFontManager
    {
        private int minFontSize;

        /// <summary>
        /// The current type face
        /// </summary>
        public Typeface Typeface { get; private set; }

        public double Size { get; private set; }

        /// <summary>
        /// Make a manager for fonts in AWT, with a minimum font size of 9.
        /// </summary>
        public WPFFontManager()
        {
            // apparently 9 pixels per em is the minimum
            // but I don't know if (size 9 == 9 px.em-1)...
            this.minFontSize = 9;
            this.MakeFonts();
            this.ToMiddle();
            this.ResetVirtualCounts();
        }

        /// <inheritdoc/>
        protected override void MakeFonts()
        {
            Typeface = new Typeface(
                new FontFamily(this.FontName),
                WPF::FontStyles.Normal,
                this.FontWeight == FontWeight.Bold ? WPF.FontWeights.Bold : WPF.FontWeights.Normal,
                WPF::FontStretches.Normal);
            Size = 9;

            int size = this.minFontSize;
            double scale = 0.5;

            for (int i = 0; i < 20; i++)
            {
                // WPF's Typeface does not contain size information. 
                this.RegisterFontSizeMapping(scale, size);
                size += 1;
                scale += 0.1;
            }
        }

        private double zoom;

        /// <inheritdoc/>
        public override double Zoom
        {
            get
            {
                return zoom;
            }

            set
            {
                zoom = value;
                int size = this.GetFontSizeForZoom(zoom);
                if (size != -1)
                {
                    this.Size = size;
                }
            }
        }

        /// <summary>
        /// Get the current font.
        /// </summary>
        /// <returns>the font at this zoom level</returns>
        public Typeface CureentTypeface => Typeface;
    }
}
