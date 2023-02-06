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

namespace NCDK.Renderers.Fonts
{
    /// <summary>
    /// Implements the common parts of the <see cref="IFontManager"/> interface.
    /// </summary>
    // @cdk.module renderbasic
    public abstract class AbstractFontManager : IFontManager
    {
        /// <summary>The default font family</summary>
        private string fontName = "Arial";

        /// <summary>The font weight</summary>
        private FontWeight fontWeight = FontWeight.Normal;
        
        /// <summary>The mapping between zoom levels and font sizes</summary>
        private IDictionary<double, int> zoomToFontSizeMap;

        // these two values track the font position if it falls
        // off the end of the array so that font and scale are always in synch
        private int lowerVirtualCount;
        private int upperVirtualCount;
        protected int currentFontIndex;

        /// <summary>
        /// Call this in subclasses with the super() constructor.
        /// </summary>
        public AbstractFontManager()
        {
            this.zoomToFontSizeMap = new SortedDictionary<double, int>();
        }

        /// <summary>
        /// Make widget-specific fonts.
        /// </summary>
        protected abstract void MakeFonts();

        /// <summary>
        /// The font family name used in this font manager.
        /// </summary>
        public virtual string FontName
        {
            get
            {
                return this.fontName;
            }
            set
            {
                if (this.fontName.Equals(value))
                {
                    return;
                }
                else
                {
                    this.fontName = value;
                    MakeFonts();
                }
            }
        }

        /// <summary>
        /// The font style, defined in the <see cref="IFontManager"/> interface.
        /// </summary>
        public virtual FontWeight FontWeight
        {
            get
            {
                return this.fontWeight;
            }
            set
            {
                if (this.fontWeight == value)
                {
                    return;
                }
                else
                {
                    this.fontWeight = value;
                    MakeFonts();
                }
            }
        }

        public abstract double Zoom { get; set; }

        /// <summary>
        /// For a particular zoom level, register a font point-size so that this
        /// size of font will be used when the zoom is at this level.
        /// </summary>
        /// <param name="zoom">the zoom level</param>
        /// <param name="size">the font size</param>
        public void RegisterFontSizeMapping(double zoom, int size)
        {
            this.zoomToFontSizeMap[zoom] = size;
        }

        /// <summary>
        /// For a particular zoom, get the appropriate font size.
        /// </summary>
        /// <param name="zoom">the zoom level</param>
        /// <returns>an integer font size</returns>
        protected int GetFontSizeForZoom(double zoom)
        {
            double lower = -1;
            foreach (var upper in this.zoomToFontSizeMap.Keys)
            {
                if (lower == -1)
                {
                    lower = upper;
                    if (zoom <= lower) return this.zoomToFontSizeMap[upper];
                    continue;
                }
                if (zoom > lower && zoom <= upper)
                {
                    return this.zoomToFontSizeMap[upper];
                }
                lower = upper;
            }

            return this.zoomToFontSizeMap[lower];
        }

        /// <summary>
        /// Get the number of font sizes used.
        /// </summary>
        /// <returns>the size of the zoom to font map</returns>
        public int GetNumberOfFontSizes()
        {
            return this.zoomToFontSizeMap.Count;
        }

        /// <summary>
        /// Reset the virtual counts.
        /// </summary>
        public void ResetVirtualCounts()
        {
            this.lowerVirtualCount = 0;
            this.upperVirtualCount = this.GetNumberOfFontSizes() - 1;
        }

        /// <summary>
        /// Set the font size pointer to the middle of the range.
        /// </summary>
        public void ToMiddle()
        {
            this.currentFontIndex = this.GetNumberOfFontSizes() / 2;
        }

        /// <summary>
        /// Move the font size pointer up. If this would move the pointer past
        /// the maximum font size, track this increase with a virtual size.
        /// </summary>
        public void IncreaseFontSize()
        {
            // move INTO range if we have just moved OUT of lower virtual
            if (InRange() || (AtMin() && AtLowerBoundary()))
            {
                currentFontIndex++;
            }
            else if (AtMax())
            {
                upperVirtualCount++;
            }
            else if (AtMin() && InLower())
            {
                lowerVirtualCount++;
            }
        }

        /// <summary>
        /// Move the font size pointer down. If this would move the pointer past
        /// the minimum font size, track this increase with a virtual size.
        /// </summary>
        public void DecreaseFontSize()
        {
            // move INTO range if we have just moved OUT of upper virtual
            if (InRange() || (AtMax() && AtUpperBoundary()))
            {
                currentFontIndex--;
            }
            else if (AtMin())
            {
                lowerVirtualCount--;
            }
            else if (AtMax() && InUpper())
            {
                upperVirtualCount--;
            }
        }

        /// <summary>
        /// Check that the font pointer is in the range (0, numberOfFonts - 1).
        /// </summary>
        /// <returns>true if the current font index is between 0 and |fonts| - 1</returns>
        public bool InRange()
        {
            return currentFontIndex > 0 && currentFontIndex < GetNumberOfFontSizes() - 1;
        }

        /// <summary>
        /// Test the virtual font pointer to see if it is at the lower boundary of
        /// the font size range (0).
        /// </summary>
        /// <returns>true if the lower virtual count is zero</returns>
        public bool AtLowerBoundary()
        {
            return this.lowerVirtualCount == 0;
        }

        /// <summary>
        /// Test the virtual font pointer to see if it is at the upper boundary of
        /// the font size range (|fonts| - 1).
        /// </summary>
        /// <returns>true if the upper virtual count is |fonts| - 1</returns>
        public bool AtUpperBoundary()
        {
            return this.upperVirtualCount == this.GetNumberOfFontSizes() - 1;
        }

        /// <summary>
        /// Test to see if the lower virtual pointer is in use.
        /// </summary>
        /// <returns>true if the lower virtual count is less than zero</returns>
        public bool InLower()
        {
            return this.lowerVirtualCount < 0;
        }

        /// <summary>
        /// Test to see if the upper virtual pointer is in use.
        /// </summary>
        /// <returns>true if the upper virtual count is greater than |fonts| - 1</returns>
        public bool InUpper()
        {
            return this.upperVirtualCount > this.GetNumberOfFontSizes() - 1;
        }

        /// <summary>
        /// Check if the font pointer is as the maximum value.
        /// </summary>
        /// <returns>true if the current font index is equal to |fonts| - 1</returns>
        public bool AtMax()
        {
            return this.currentFontIndex == this.GetNumberOfFontSizes() - 1;
        }

        /// <summary>
        /// Check if the font pointer is as the minimum value.
        /// </summary>
        /// <returns>true if the current font index is equal to zero</returns>
        public bool AtMin()
        {
            return this.currentFontIndex == 0;
        }
    }
}
