/*  Copyright (C) 2009  Stefan Kuhn <shk3@users.sf.net>
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
    public class ReactionBoxGenerator : IGenerator<IReaction>
    {
        /// <inheritdoc/>
        public IRenderingElement Generate(IReaction reaction, RendererModel model)
        {
            if (!model.GetShowReactionBoxes())
                return null;
            var separation = model.GetBondLength() / model.GetScale();
            var totalBounds = BoundsCalculator.CalculateBounds(reaction);
            if (totalBounds.IsEmpty)
                return null;

            var diagram = new ElementGroup();
            var foregroundColor = model.GetForegroundColor();
            diagram.Add(new RectangleElement(new Rect(totalBounds.Left - separation, totalBounds.Top - separation,
                                                      totalBounds.Right + separation, totalBounds.Bottom + separation), 
                                             foregroundColor));
            if (reaction.Id != null)
            {
                diagram.Add(new TextElement(new Point((totalBounds.Left + totalBounds.Right) / 2, totalBounds.Top - separation),
                                            reaction.Id, foregroundColor));
            }
            return diagram;
        }
    }
}
