/* Copyright (C) 2009  Stefan Kuhn
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
using System.Windows;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// Generate the symbols for radicals.
    /// </summary>
    // @author maclean
    // @cdk.module renderextra
    public class ProductsBoxGenerator : IGenerator<IReaction>
    {
        /// <inheritdoc/>
        public IRenderingElement Generate(IReaction reaction, RendererModel model)
        {
            if (!model.GetShowReactionBoxes())
                return null;
            if (reaction.Products.Count == 0)
                return new ElementGroup();
            var distance = model.GetBondLength() / model.GetScale() / 2;
            Rect totalBounds = Rect.Empty;
            foreach (var molecule in reaction.Products)
            {
                var bounds = BoundsCalculator.CalculateBounds(molecule);
                if (totalBounds == null)
                {
                    totalBounds = bounds;
                }
                else
                {
                    totalBounds = Rect.Union(totalBounds, bounds);
                }
            }
            if (totalBounds.IsEmpty)
                return null;

            var diagram = new ElementGroup();
            var foregroundColor = model.GetForegroundColor();
            diagram.Add(new RectangleElement(
                new Rect(
                    totalBounds.Left - distance,
                    totalBounds.Top - distance,
                    totalBounds.Right + distance,
                    totalBounds.Bottom + distance),
                foregroundColor));
            diagram.Add(new TextElement(new Point((totalBounds.Left + totalBounds.Right) / 2, 
                                                  totalBounds.Top - distance),
                                        "Products", foregroundColor));
            return diagram;
        }
   }
}
