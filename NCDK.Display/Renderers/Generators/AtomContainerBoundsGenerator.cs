/*  Copyright (C) 2008  Arvid Berg <goglepox@users.sf.net>
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

using NCDK.Geometries;
using NCDK.Renderers.Elements;
using System.Windows;
using System.Windows.Media;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// <see cref="IGenerator{T}"/> that draws a rectangular around the <see cref="IAtomContainer"/>.
    /// </summary>
    // @cdk.module renderextra
    public class AtomContainerBoundsGenerator : IGenerator<IAtomContainer>
    {
        /// <inheritdoc/>
        public IRenderingElement Generate(IAtomContainer container, RendererModel model)
        {
            double[] minMax = GeometryUtil.GetMinMax(container);
            return new RectangleElement(new Point(minMax[0], minMax[1]), new Point(minMax[2], minMax[3]), Color.FromArgb(255, (int)(255 * 0.7), (int)(255 * 0.7), 255));
        }
    }
}
