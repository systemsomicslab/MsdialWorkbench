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

namespace NCDK.Renderers.Fonts
{
    /// <summary>
    /// Weight of the font to use to draw text.
    /// </summary>
    public enum FontWeight
    {
        /// <summary>Regular font style.</summary>
        Normal,
        /// <summary>Bold font style.</summary>
        Bold,
    }

    /// <summary>
    /// An interface for managing the drawing of fonts at different zoom levels.
    /// </summary>
    // @author maclean
    // @cdk.module render
    public interface IFontManager
    {
        /// <summary>
        /// A particular zoom level, set the appropriate font size to use.
        /// a real number in the range (0.0, INF)
        /// </summary>
        double Zoom { get; set; }

        /// <summary>
        /// The font weight.
        /// </summary>
        FontWeight FontWeight { get; set; }

        /// <summary>
        /// The font name ('Arial', 'Times New Roman' and so on).
        /// </summary>
        string FontName { get; set; }
    }
}
