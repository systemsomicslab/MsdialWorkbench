/*  Copyright (C) 2009  Gilleain Torrance <gilleain@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
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

using NCDK.Numerics;
using NCDK.Renderers.Elements;
using static NCDK.Renderers.Generators.Standards.VecmathUtil;

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// <see cref="IGenerator{T}"/> for <see cref="IAtomContainer"/>s that will draw atom numbers for the atoms.
    /// </summary>
    // @author      maclean
    // @cdk.module  renderextra
    public class AtomNumberGenerator : IGenerator<IAtomContainer>
    {
        /// <inheritdoc/>
        public IRenderingElement Generate(IAtomContainer container, RendererModel model)
        {
            var numbers = new ElementGroup();
            if (!model.GetWillDrawAtomNumbers())
                return numbers;

            var _offset = model.GetAtomNumberOffset();
            var offset = new Vector2(_offset.X, -_offset.Y);
            offset *= (1 / model.GetScale());

            int number = 1;
            foreach (var atom in container.Atoms)
            {
                var point = atom.Point2D.Value + offset;
                numbers.Add(new TextElement(ToPoint(point), number.ToString(),
                                            model.GetAtomNumberColorByType() 
                                          ? model.GetAtomNumberColorer().GetAtomColor(atom)
                                          : model.GetAtomNumberTextColor()));
                number++;
            }
            return numbers;
        }
    }
}
