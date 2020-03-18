/* Copyright (C) 2009  Gilleain Torrance <gilleain.torrance@gmail.com>
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
using NCDK.Numerics;
using NCDK.Renderers.Elements;
using System.Collections.Generic;
using System.Windows.Media;
using static NCDK.Renderers.Generators.Standards.VecmathUtil;
using WPF = System.Windows;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// Generate the symbols for radicals.
    /// </summary>
    // @author maclean
    // @cdk.module renderextra
    public class RadicalGenerator : IGenerator<IAtomContainer>
    {
        public RadicalGenerator() { }

        /// <inheritdoc/>
        public IRenderingElement Generate(IAtomContainer container, RendererModel model)
        {
            var group = new ElementGroup();

            // TODO : put into RendererModel
            const double ScreenRadius = 2.0;
            var RadicalColor = WPF.Media.Colors.Black;

            // XXX : is this the best option?
            var AtomRadius = model.GetAtomRadius() / model.GetScale();

            var modelRadius = ScreenRadius / model.GetScale();
            var modelSpacing = modelRadius * 2.5;
            var singleElectronsPerAtom = new Dictionary<IAtom, int>();
            foreach (var electron in container.SingleElectrons)
            {
                var atom = electron.Atom;
                if (!singleElectronsPerAtom.ContainsKey(atom))
                    singleElectronsPerAtom[atom] = 0;
                var point = atom.Point2D.Value;
                var align = GeometryUtil.GetBestAlignmentForLabelXY(container, atom);
                var center = ToPoint(point);
                if (align == 1)
                {
                    center.X += AtomRadius * 4 + singleElectronsPerAtom[atom] * modelSpacing;
                }
                else if (align == -1)
                {
                    center.X -= AtomRadius * 4 + singleElectronsPerAtom[atom] * modelSpacing;
                }
                else if (align == 2)
                {
                    center.Y += AtomRadius * 4 + singleElectronsPerAtom[atom] * modelSpacing;
                }
                else if (align == -2)
                {
                    center.Y -= AtomRadius * 4 + singleElectronsPerAtom[atom] * modelSpacing;
                }
                singleElectronsPerAtom[atom] = singleElectronsPerAtom[atom] + 1;
                group.Add(new OvalElement(center, modelRadius, true, RadicalColor));
            }
            return group;
        }
    }
}
