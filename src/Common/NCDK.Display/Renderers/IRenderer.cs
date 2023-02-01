/* Copyright (C) 2009  Egon Willighagen <egonw@users.lists.sf>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All I ask is that proper credit is given for my work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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
using NCDK.Renderers.Generators;
using NCDK.Renderers.Visitors;
using System.Collections.Generic;
using System.Windows;

namespace NCDK.Renderers
{
    /// <summary>
    /// Interface that all 2D renderers implement.
    /// </summary>
    // @author egonw
    // @cdk.module render
    interface IRenderer<T> where T : IChemObject
    {
        /// <summary>
        /// Internal method to generate the intermediate format.
        /// </summary>
        /// <param name="obj">the IChemObject to generate a diagram for</param>
        /// <returns>a tree of rendering elements</returns>
        IRenderingElement GenerateDiagram(T obj);

        /// <summary>
        /// Returns the drawing model, giving access to drawing parameters.
        /// </summary>
        /// <returns>the rendering model</returns>
        RendererModel GetRenderer2DModel();

        /// <summary>
        /// Converts screen coordinates into model (or world) coordinates.
        /// </summary>
        /// <param name="screenXTo">the screen's x coordinate</param>
        /// <param name="screenYTo">the screen's y coordinate</param>
        /// <returns>the matching model coordinates</returns>
        /// <seealso cref="ToScreenCoordinates(double, double)"/>
        Point ToModelCoordinates(double screenXTo, double screenYTo);

        /// <summary>
        /// Converts model (or world) coordinates into screen coordinates.
        /// </summary>
        /// <param name="screenXTo">the model's x coordinate</param>
        /// <param name="screenYTo">the model's y coordinate</param>
        /// <returns>the matching screen coordinates</returns>
        /// <seealso cref="ToModelCoordinates(double, double)"/>
        Point ToScreenCoordinates(double screenXTo, double screenYTo);

        /// <summary>
        /// Set a new zoom factor.
        /// </summary>
        /// <param name="zoomFactor">the new zoom factor</param>
        void SetZoom(double zoomFactor);

        /// <summary>
        /// Set a new drawing center in screen coordinates.
        /// </summary>
        /// <param name="screenX">the x screen coordinate of the drawing center</param>
        /// <param name="screenY">the y screen coordinate of the drawing center</param>
        void ShiftDrawCenter(double screenX, double screenY);

        /// <summary>
        /// Paint an IChemObject.
        /// </summary>
        /// <param name="obj">the chem obj to paint</param>
        /// <param name="drawVisitor">the class that visits the generated elements</param>
        /// <returns>the rectangular area where was drawn</returns>
        Rect Paint(T obj, IDrawVisitor drawVisitor);

        /// <summary>
        /// Paint the chem obj within the specified bounds.
        /// </summary>
        /// <param name="obj">Object to draw</param>
        /// <param name="drawVisitor">the visitor to draw to</param>
        /// <param name="bounds">the screen bounds between which to draw</param>
        /// <param name="resetCenter">a bool indicating the the drawing center needs to be reset</param>
        void Paint(T obj, IDrawVisitor drawVisitor, Rect bounds, bool resetCenter);

        /// <summary>
        /// Setup the transformations necessary to draw the <see cref="IChemObject"/>
        /// matching this <see cref="IRenderer{T}"/> implementation.
        /// </summary>
        /// <param name="obj"><see cref="IChemObject"/> to be drawn</param>
        /// <param name="screen"><see cref="Rect"/> to draw the obj to</param>
        void Setup(T obj, Rect screen);

        /// <summary>
        /// Set the scale for an <see cref="IChemObject"/>. It calculates the average bond
        /// length of the model and calculates the multiplication factor to transform
        /// this to the bond length that is set in the <see cref="RendererModel"/>.
        ///
        /// <param name="obj">the <see cref="IChemObject"/> to draw.</param>
        /// </summary>
        void SetScale(T obj);

        /// <summary>
        /// Given a <see cref="IChemObject"/>, calculates the bounding rectangle in screen
        /// space.
        ///
        /// <param name="obj">the <see cref="IChemObject"/> to draw.</param>
        /// <returns>a rectangle in screen space.</returns>
        /// </summary>
        Rect CalculateDiagramBounds(T obj);

        /// <summary>
        /// Returns a <see cref="IList{T}"/> of <see cref="IGenerator{T}"/>s for this renderer.
        /// </summary>
        /// <returns>the list of generators for this renderer.</returns>
        IList<IGenerator<T>> GetGenerators();
    }
}
