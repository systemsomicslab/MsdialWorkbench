/* Copyright (C) 2008 Gilleain Torrance <gilleain.torrance@gmail.com>
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

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// Combination generator for basic drawing of molecules. It only creates drawing
    /// elements for atoms and bonds, using the <see cref="BasicAtomGenerator"/> and
    /// <see cref="BasicBondGenerator"/>.
    /// </summary>
    // @author maclean
    // @cdk.module renderbasic
    public class BasicGenerator : IGenerator<IAtomContainer>
    {
        /// <summary>Holder for various parameters, such as background color</summary>
        private BasicSceneGenerator sceneGenerator;

        /// <summary>Generates elements for each atom in a container</summary>
        private BasicAtomGenerator atomGenerator;

        /// <summary>Generates elements for each bond in a container</summary>
        private BasicBondGenerator bondGenerator;

        /// <summary>
        /// Make a basic generator that creates elements for atoms and bonds.
        /// </summary>
        public BasicGenerator()
        {
            this.atomGenerator = new BasicAtomGenerator();
            this.bondGenerator = new BasicBondGenerator();
            this.sceneGenerator = new BasicSceneGenerator();
        }

        /// <inheritdoc/>
        public IRenderingElement Generate(IAtomContainer ac, RendererModel model)
        {
            var diagram = new ElementGroup
            {
                this.sceneGenerator.Generate(ac, model),
                this.bondGenerator.Generate(ac, model),
                this.atomGenerator.Generate(ac, model)
            };
            return diagram;
        }
    }
}
