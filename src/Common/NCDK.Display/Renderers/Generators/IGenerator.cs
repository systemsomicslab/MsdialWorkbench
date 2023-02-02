/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
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

namespace NCDK.Renderers.Generators
{
    /// <summary>
    /// An <see cref="IGenerator{T}"/> converts chemical entities into parts of the
    /// chemical drawing expressed as <see cref="IRenderingElement"/>s.
    /// </summary>
    /// <remarks>
    /// Note that some generators have explicit empty constructors (like:
    /// <c>public MyGenerator() { }</c>) which can be useful in some situations where
    /// reflection is required. It is not, however, necessary for most normal
    /// drawing situations.
    /// </remarks>
    // @cdk.module  render
    public interface IGenerator<T> where T: IChemObject
    {
        /// <summary>
        /// Converts a <see cref="IChemObject"/> from the chemical data model into
        /// something that can be drawn in the chemical drawing.
        /// </summary>
        /// <param name="obj">the chemical entity to be depicted</param>
        /// <param name="model">the rendering parameters</param>
        /// <returns>a drawable chemical depiction component</returns>
        IRenderingElement Generate(T obj, RendererModel model);
    }
}
