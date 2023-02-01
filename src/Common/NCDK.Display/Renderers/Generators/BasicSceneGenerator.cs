/* Copyright (C) 2008  Arvid Berg <goglepox@users.sf.net>
 *               2010  Egon Willighagen <egonw@users.sf.net>
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
    /// This generator does not create any elements, but acts as a holding place
    /// for various generator parameters used by most drawings, such as the zoom,
    /// background color, margin, etc.
    /// </summary>
    // @cdk.module renderbasic
    public class BasicSceneGenerator : IGenerator<IAtomContainer>
    {
        /// <summary>
        /// An empty constructor necessary for reflection.
        /// </summary>
        public BasicSceneGenerator() { }

        /// <inheritdoc/>
        public IRenderingElement Generate(IAtomContainer ac, RendererModel model)
        {
            return new ElementGroup();
        }
    }
}
