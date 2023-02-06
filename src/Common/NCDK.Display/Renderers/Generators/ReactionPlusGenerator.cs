/* Copyright (C) 2009  Stefan Kuhn <shk3@users.sf.net>
 *               2009  Gilleain Torrance <gilleain@users.sf.net>
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
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// Generate the arrow for a reaction.
    /// </summary>
    // @author maclean
    // @cdk.module renderextra
    public class ReactionPlusGenerator : IGenerator<IReaction>
    {
        /// <inheritdoc/>
        public IRenderingElement Generate(IReaction reaction, RendererModel model)
        {
            var diagram = new ElementGroup();

            var color = model.GetForegroundColor();
            var reactants = reaction.Reactants;

            // only draw + signs when there are more than one reactant
            if (reactants.Count > 1)
            {
                var totalBoundsReactants = BoundsCalculator.CalculateBounds(reactants);
                var bounds1 = BoundsCalculator.CalculateBounds(reactants[0]);
                var axis = totalBoundsReactants.CenterY();
                foreach (var reactant in reaction.Reactants.Skip(1))
                {
                    var bounds2 = BoundsCalculator.CalculateBounds(reactant);
                    diagram.Add(MakePlus(bounds1, bounds2, axis, color));
                    bounds1 = bounds2;
                }
            }

            // only draw + signs when there are more than one products
            var products = reaction.Products;
            if (products.Count > 1)
            {
                var totalBoundsProducts = BoundsCalculator.CalculateBounds(products);
                var axis = totalBoundsProducts.CenterY();
                var bounds1 = BoundsCalculator.CalculateBounds(reactants[0]);
                foreach (var product in reaction.Products.Skip(1))
                {
                    var bounds2 = BoundsCalculator.CalculateBounds(product);

                    diagram.Add(MakePlus(bounds1, bounds2, axis, color));
                    bounds1 = bounds2;
                }
            }
            return diagram;
        }

        /// <summary>Place a '+' sign between two molecules.</summary>
        private TextElement MakePlus(Rect moleculeBox1, Rect moleculeBox2, double axis, Color color)
        {
            var arrowCenter = (moleculeBox1.CenterX() + moleculeBox2.CenterX()) / 2;
            return new TextElement(new Point(arrowCenter, axis), "+", color);
        }
    }
}
