/* Copyright (C) 2009  Gilleain Torrance <gilleain@users.sf.net>
 *               2009  Stefan Kuhn <sh3@users.sf.net>
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
using System.Windows.Media;
using static NCDK.Renderers.Generators.Standards.VecmathUtil;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// <see cref="IGenerator{T}"/> that will show how atoms map between the reactant and product side.
    /// </summary>
    // @cdk.module renderextra
    public class MappingGenerator : IGenerator<IReaction>
    {
        public MappingGenerator() { }

        /// <inheritdoc/>
        public IRenderingElement Generate(IReaction reaction, RendererModel model)
        {
            if (!model.GetShowAtomAtomMapping())
                return null;
            var elementGroup = new ElementGroup();
            var mappingColor = model.GetAtomAtomMappingLineColor();
            foreach (var mapping in reaction.Mappings)
            {
                // XXX assume that there are only 2 endpoints!
                // XXX assume that the ChemObjects are actually IAtoms...
                var endPointA = (IAtom)mapping[0];
                var endPointB = (IAtom)mapping[1];
                var pointA = ToPoint(endPointA.Point2D.Value);
                var pointB = ToPoint(endPointB.Point2D.Value);
                elementGroup.Add(new LineElement(pointA, pointB, GetWidthForMappingLine(model), mappingColor));
            }
            return elementGroup;
        }

        /// <summary>
        /// Determine the width of an atom atom mapping, returning the width defined
        /// in the model. Note that this will be scaled
        /// to the space of the model.
        /// </summary>
        /// <param name="model">the renderer model</param>
        /// <returns>a double in chem-model space</returns>
        private double GetWidthForMappingLine(RendererModel model)
        {
            var scale = model.GetScale();
            return model.GetMappingLineWidth() / scale;
        }
    }
}
