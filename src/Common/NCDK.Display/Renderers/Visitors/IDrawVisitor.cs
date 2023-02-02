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

using NCDK.Renderers.Elements;
using NCDK.Renderers.Fonts;

namespace NCDK.Renderers.Visitors
{
    /// <summary>
    /// An <see cref="IDrawVisitor"/> is an <see cref="IRenderingVisitor"/> that can be
    /// customized and knows about fonts and other rendering parameters.
    /// </summary>
    // @cdk.module render
    public interface IDrawVisitor : IRenderingVisitor
    {
        /// <summary>
        /// The <see cref="IFontManager"/> this <see cref="IDrawVisitor"/> should use.
        /// </summary>
        IFontManager FontManager { get; set; }

        /// <summary>
        /// The <see cref="RendererModel"/> this <see cref="IDrawVisitor"/> should use.
        /// </summary>
        RendererModel RendererModel { get; set; }
    }
}
