/* Copyright (C) 2009  Stefan Kuhn <shk3@users.sf.net>
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
    public class ReactantsBoxGenerator : IGenerator<IReaction>
    {
        /// <inheritdoc/>
        public IRenderingElement Generate(IReaction reaction, RendererModel model)
        {
            if (!model.GetShowReactionBoxes())
                return null;
            if (reaction.Reactants.Count == 0)
                return new ElementGroup();

            var separation = model.GetBondLength() / model.GetScale() / 2;
            var totalBounds = BoundsCalculator.CalculateBounds(reaction.Reactants);

            var diagram = new ElementGroup();
            var minX = totalBounds.Left;
            var minY = totalBounds.Top;
            var maxX = totalBounds.Right;
            var maxY = totalBounds.Bottom;
            var foregroundColor = model.GetForegroundColor();
            diagram.Add(new RectangleElement(new Rect(minX - separation, minY - separation, maxX + separation, maxY + separation), foregroundColor));
            diagram.Add(new TextElement(new Point((minX + maxX) / 2, minY - separation), "Reactants", foregroundColor));
            return diagram;
        }
    }
}
