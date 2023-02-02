/* Copyright (C) 2008-2009  Gilleain Torrance <gilleain.torrance@gmail.com>
 *               2008-2009  Arvid Berg <goglepox@users.sf.net>
 *               2009-2010  Egon Willighagen <egonw@users.sf.net>
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
using NCDK.Renderers.Fonts;
using NCDK.Renderers.Generators;
using NCDK.Renderers.Visitors;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace NCDK.Renderers
{
    /// <summary>
    /// The base class for all renderers, handling the core aspects of rendering
    /// such as the location of the model in 'model space' and the location on
    /// the screen to draw the model. It also holds a reference to the list of
    /// <see cref="IGenerator{T}"/> instances that are used to create the diagram. These
    /// generators are accessed through the generateDiagram method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The terminology 'model space' and 'screen space' refer to the coordinate
    /// systems for the model and the drawing, respectively. So the 2D points for
    /// atoms in the model might be 1 unit apart (roughly representing Ångström,
    /// perhaps) but the circles in the diagram that represent those atoms might be
    /// 10 pixels apart on screen. Therefore screen space will be 10 times model
    /// space for this example.
    /// </para>
    /// <para>
    /// The abstract method <see cref="CalculateScaleForBondLength(double)"/> is
    /// needed to determine the scale. For the model example just given, this would
    /// return '10.0' for an input of '10.0', as that is the scale for that desired
    /// bond length.
    /// </para>
    /// </remarks>
    // @cdk.module renderbasic
    // @author maclean
    public abstract class AbstractRenderer<T> where T : IChemObject
    {
        /// <summary>
        /// The renderer model is final as it is not intended to be replaced.
        /// </summary>
        protected readonly RendererModel rendererModel;

        /// <summary>
        /// Font managers change the font size depending on the zoom.
        /// </summary>
        protected IFontManager fontManager;

        /// <summary>
        /// The center point of the model (IAtomContainer, IAtomContainerSet, etc).
        /// </summary>
        protected Point modelCenter = new Point(0, 0);

        /// <summary>
        /// The center of the desired position on screen to draw.
        /// </summary>
        protected Point drawCenter = new Point(150, 200);

        /// <summary>
        /// Generators for diagram elements.
        /// </summary>
        protected IList<IGenerator<T>> generators;

        /// <summary>
        /// Used when repainting an unchanged model.
        /// </summary>
        protected IRenderingElement cachedDiagram;

        /// <summary>
        /// Converts between model coordinates and screen coordinates.
        /// </summary>
        protected Transform transform;

        public AbstractRenderer(RendererModel rendererModel)
        {
            this.rendererModel = rendererModel;
        }

        /// <summary>
        /// The main method of the renderer, that uses each of the generators
        /// to create a different set of <see cref="IRenderingElement"/>s grouped
        /// together into a tree.
        /// </summary>
        /// <param name="obj">the object of type T to draw</param>
        /// <returns>the diagram as a tree of <see cref="IRenderingElement"/>s</returns>
        public virtual IRenderingElement GenerateDiagram(T obj)
        {
            var diagram = new ElementGroup();
            foreach (var generator in this.generators)
            {
                diagram.Add(generator.Generate(obj, this.rendererModel));
            }
            return diagram;
        }

        /// <summary>
        /// Calculate the scale to convert the model bonds into bonds of the length
        /// supplied. A standard way to do this is to calculate the average bond
        /// length (mean, or median) in model space, and divide the supplied screen
        /// distance by this average to give a scale.
        /// </summary>
        /// <param name="bondLength">the desired length on screen</param>
        /// <returns>a multiplication factor, or 'scale'</returns>
        public abstract double CalculateScaleForBondLength(double bondLength);

        /// <summary>
        /// Converts a bounding rectangle in 'model space' into the equivalent
        /// bounds in 'screen space'. Used to determine how much space the model
        /// will take up on screen given a particular scale, zoom, and margin.
        /// </summary>
        /// <param name="modelBounds">the bounds of the model</param>
        /// <returns>the bounds of the diagram as drawn on screen</returns>
        public virtual Rect CalculateScreenBounds(Rect modelBounds)
        {
            var scale = rendererModel.GetScale();
            var zoom = rendererModel.GetZoomFactor();
            var margin = rendererModel.GetMargin();
            var modelScreenCenter = this.ToScreenCoordinates(
                modelBounds.X + modelBounds.Width / 2,
                modelBounds.Y + modelBounds.Height / 2);
            var width = (scale * zoom * modelBounds.Width) + (2 * margin);
            var height = (scale * zoom * modelBounds.Height) + (2 * margin);
            return new Rect(modelScreenCenter.X - width / 2,
                            modelScreenCenter.Y - height / 2,
                            width, height);
        }

        /// <summary>
        /// Convert a point in screen space into a point in model space.
        /// </summary>
        /// <param name="screenX">the screen x-coordinate</param>
        /// <param name="screenY">the screen y-coordinate</param>
        /// <returns>the equivalent point in model space, or (0,0) if there is an error</returns>
        public virtual Point ToModelCoordinates(double screenX, double screenY)
        {
            var inv = transform.Value;
            if (inv.HasInverse)
            {
                inv.Invert();
                var src = new Point(screenX, screenY);
                var dest = inv.Transform(src);
                return dest;
            }
            else
            {
                return new Point(0, 0);
            }
        }

        /// <summary>
        /// Convert a point in model space into a point in screen space.
        /// </summary>
        /// <param name="modelX">the model x-coordinate</param>
        /// <param name="modelY">the model y-coordinate</param>
        /// <returns>the equivalent point in screen space</returns>
        public virtual Point ToScreenCoordinates(double modelX, double modelY)
        {
            var dest = transform.Transform(new Point(modelX, modelY));
            return dest;
        }

        /// <summary>
        /// Set the position of the center of the model.
        /// </summary>
        /// <param name="modelX">the x-coordinate of the model center</param>
        /// <param name="modelY">the y-coordinate of the model center</param>
        public virtual void SetModelCenter(double modelX, double modelY)
        {
            this.modelCenter = new Point(modelX, modelY);
            Setup();
        }

        /// <summary>
        /// Set the point on the screen to draw the diagram.
        /// </summary>
        /// <param name="modelX">the x-coordinate of the point to draw at</param>
        /// <param name="modelY">the y-coordinate of the point to draw at</param>
        public virtual void SetDrawCenter(double modelX, double modelY)
        {
            this.drawCenter = new Point(modelX, modelY);
            Setup();
        }

        /// <summary>
        /// Set the zoom, where 1.0 is 100% zoom.
        /// </summary>
        /// <param name="zoom">the zoom as a double value</param>
        public virtual void SetZoom(double zoom)
        {
            rendererModel.SetZoomFactor(zoom);
            Setup();
        }

        /// <summary>
        /// Creates the transform using the scale, zoom, drawCenter, and modelCenter.
        /// In order to scale (and zoom) all elements in a uniform way, a point is
        /// first moved so that the center of the model is at the origin, scaled,
        /// then moved to the correct place on screen.
        /// </summary>
        protected virtual void Setup()
        {
            var scale = rendererModel.GetScale();
            var zoom = rendererModel.GetZoomFactor();
            // set the transform
            try
            {
                var matrix = new Matrix();
                matrix.Translate(-this.modelCenter.X, -this.modelCenter.Y);
                matrix.Scale(zoom, zoom);
                matrix.Scale(scale, scale);
                //matrix.Scale(1, -1); // Converts between CDK Y-up & Java2D Y-down coordinate-systems
                matrix.Translate(drawCenter.X, drawCenter.Y);

                transform = Transform.Parse(matrix.ToString());
            }
            catch (NullReferenceException)
            {
                // one of the drawCenter or modelCenter points have not been set!
                Console.Error.WriteLine($"null pointer when setting transform: drawCenter={drawCenter} scale={scale} zoom={zoom} modelCenter={modelCenter}");
            }
        }

        /// <summary>
        /// Get the <see cref="RendererModel"/> used by this renderer, which provides
        /// access to the various parameters used to generate and draw the diagram.
        /// </summary>
        /// <returns>a reference to the RendererModel</returns>
        public virtual RendererModel GetRenderer2DModel()
        {
            return this.rendererModel;
        }

        /// <summary>
        /// Move the draw center by dx and dy.
        /// </summary>
        /// <param name="shiftX">the x shift</param>
        /// <param name="shiftY">the y shift</param>
        public virtual void ShiftDrawCenter(double shiftX, double shiftY)
        {
            drawCenter = new Point(drawCenter.X + shiftX, drawCenter.Y + shiftY);
            Setup();
        }

        /// <summary>
        /// Get the position on screen that the diagram will be drawn.
        /// </summary>
        /// <returns>the draw center</returns>
        public virtual Point GetDrawCenter()
        {
            return drawCenter;
        }

        /// <summary>
        /// Get the center of the model.
        /// </summary>
        /// <returns>the model center</returns>
        public virtual Point GetModelCenter()
        {
            return modelCenter;
        }

        /// <summary>
        /// Calculate and set the zoom factor needed to completely fit the diagram
        /// onto the screen bounds.
        /// </summary>
        /// <param name="drawWidth">the width of the area to draw onto</param>
        /// <param name="drawHeight">the height of the area to draw onto</param>
        /// <param name="diagramWidth">the width of the diagram</param>
        /// <param name="diagramHeight">the height of the diagram</param>
        public virtual void SetZoomToFit(double drawWidth, double drawHeight, double diagramWidth, double diagramHeight)
        {
            var margin = rendererModel.GetMargin();

            // determine the zoom needed to fit the diagram to the screen
            var widthRatio = drawWidth / (diagramWidth + (2 * margin));
            var heightRatio = drawHeight / (diagramHeight + (2 * margin));

            var zoom = Math.Min(widthRatio, heightRatio);

            this.fontManager.Zoom = zoom;

            // record the zoom in the model, so that generators can use it
            rendererModel.SetZoomFactor(zoom);
        }

        /// <summary>
        /// Repaint using the cached diagram.
        /// </summary>
        /// <param name="drawVisitor">the wrapper for the graphics object that draws</param>
        public virtual void Repaint(IDrawVisitor drawVisitor)
        {
            this.Paint(drawVisitor, cachedDiagram);
        }

        /// <summary>
        /// The target method for paintChemModel, paintReaction, and paintMolecule.
        /// </summary>
        /// <param name="drawVisitor">the visitor to draw with</param>
        /// <param name="diagram">the IRenderingElement tree to render</param>
        protected virtual void Paint(IDrawVisitor drawVisitor, IRenderingElement diagram)
        {
            if (diagram == null)
                return;

            // cache the diagram for quick-redraw
            this.cachedDiagram = diagram;

            fontManager.FontName = rendererModel.GetFontName();
            fontManager.FontWeight = rendererModel.GetUsedFontStyle();

            drawVisitor.FontManager = this.fontManager;
            drawVisitor.RendererModel = this.rendererModel;
            diagram.Accept(drawVisitor, this.transform);
        }

        /// <summary>
        /// Set the transform for a non-fit to screen paint.
        /// </summary>
        /// <param name="modelBounds">the bounding box of the model</param>
        protected virtual void SetupTransformNatural(Rect modelBounds)
        {
            var zoom = rendererModel.GetZoomFactor();
            this.fontManager.Zoom = zoom;
            this.Setup();
        }

        /// <summary>
        /// Determine the overlap of the diagram with the screen, and shift (if
        /// necessary) the diagram draw center. It returns a rectangle only because
        /// that is a convenient class to hold the four parameters calculated, but it
        /// is not a rectangle representing an area...
        /// </summary>
        /// <param name="screenBounds">the bounds of the screen</param>
        /// <param name="diagramBounds">the bounds of the diagram</param>
        /// <returns>the shape that the screen should be</returns>
        public virtual Rect Shift(Rect screenBounds, Rect diagramBounds)
        {
            var screenMaxX = screenBounds.X + screenBounds.Width;
            var screenMaxY = screenBounds.Y + screenBounds.Height;
            var diagramMaxX = diagramBounds.X + diagramBounds.Width;
            var diagramMaxY = diagramBounds.Y + diagramBounds.Height;

            var leftOverlap = screenBounds.X - diagramBounds.X;
            var rightOverlap = diagramMaxX - screenMaxX;
            var topOverlap = screenBounds.Y - diagramBounds.Y;
            var bottomOverlap = diagramMaxY - screenMaxY;

            var width = screenBounds.Width;
            var height = screenBounds.Height;

            var dx = leftOverlap > 0 ? leftOverlap : 0;
            if (rightOverlap > 0)
            {
                width += rightOverlap;
            }

            var dy = topOverlap > 0 ? topOverlap : 0;
            if (bottomOverlap > 0)
            {
                height += bottomOverlap;
            }

            if (dx != 0 || dy != 0)
            {
                this.ShiftDrawCenter(dx, dy);
            }

            return new Rect(dx, dy, width, height);
        }

        /// <summary>
        /// Calculate the bounds of the diagram on screen, given the current scale,
        /// zoom, and margin.
        /// </summary>
        /// <param name="modelBounds">the bounds in model space of the chem object</param>
        /// <returns>the bounds in screen space of the drawn diagram</returns>
        protected virtual Rect ConvertToDiagramBounds(Rect modelBounds)
        {
            if (modelBounds.IsEmpty)
            {
                return Rect.Empty;
            }
            var xCenter = modelBounds.X + modelBounds.Width / 2;
            var yCenter = modelBounds.Y + modelBounds.Height / 2;
            var modelWidth = modelBounds.Width;
            var modelHeight = modelBounds.Height;

            var scale = rendererModel.GetScale();
            var zoom = rendererModel.GetZoomFactor();

            var screenCoord = this.ToScreenCoordinates(xCenter, yCenter);

            // special case for 0 or 1 atoms
            if (modelWidth == 0 && modelHeight == 0)
            {
                return new Rect(screenCoord.X, screenCoord.Y, 0, 0);
            }

            var margin = rendererModel.GetMargin();
            var width = ((scale * zoom * modelWidth) + (2 * margin));
            var height = ((scale * zoom * modelHeight) + (2 * margin));
            var xCoord = (screenCoord.X - width / 2);
            var yCoord = (screenCoord.Y - height / 2);

            return new Rect(xCoord, yCoord, width, height);
        }

        /// <summary>
        /// Sets the transformation needed to draw the model on the canvas when
        /// the diagram needs to fit the screen.
        /// </summary>
        /// <param name="screenBounds">the bounding box of the draw area</param>
        /// <param name="modelBounds">the bounding box of the model</param>
        /// <param name="reset">if true, model center will be set to the modelBounds center and the scale will be re-calculated</param>
        protected internal virtual void SetupTransformToFit(Rect screenBounds, Rect modelBounds, bool reset)
        {
            var scale = rendererModel.GetScale();

            if (screenBounds == null)
                return;

            SetDrawCenter(screenBounds.X + screenBounds.Width / 2, screenBounds.Y + screenBounds.Height / 2);

            var drawWidth = screenBounds.Width;
            var drawHeight = screenBounds.Height;

            var diagramWidth = modelBounds.Width * scale;
            var diagramHeight = modelBounds.Height * scale;

            SetZoomToFit(drawWidth, drawHeight, diagramWidth, diagramHeight);

            // this controls whether editing a molecule causes it to re-center
            // with each change or not
            if (reset || rendererModel.GetFitToScreen())
            {
                SetModelCenter(modelBounds.X + modelBounds.Width / 2, modelBounds.Y + modelBounds.Height / 2);
            }

            Setup();
        }

        /// <summary>
        /// Sets the transformation needed to draw the model on the canvas when
        /// the diagram needs to fit the screen.
        /// </summary>
        /// <param name="screenBounds">the bounding box of the draw area</param>
        /// <param name="modelBounds">the bounding box of the model</param>
        /// <param name="bondLength">the average bond length of the model</param>
        /// <param name="reset">if true, model center will be set to the modelBounds center and the scale will be re-calculated</param>
        protected virtual void SetupTransformToFit(Rect screenBounds, Rect modelBounds, double bondLength, bool reset)
        {
            if (screenBounds == null)
                return;

            SetDrawCenter(screenBounds.X + screenBounds.Width / 2, screenBounds.Y + screenBounds.Height / 2);

            var scale = this.CalculateScaleForBondLength(bondLength);

            var drawWidth = screenBounds.Width;
            var drawHeight = screenBounds.Height;

            var diagramWidth = modelBounds.Width * scale;
            var diagramHeight = modelBounds.Height * scale;

            SetZoomToFit(drawWidth, drawHeight, diagramWidth, diagramHeight);

            // this controls whether editing a molecule causes it to re-center
            // with each change or not
            if (reset || rendererModel.GetFitToScreen())
            {
                SetModelCenter(modelBounds.X + modelBounds.Width / 2, modelBounds.Y + modelBounds.Height / 2);
            }

            // set the scale in the renderer model for the generators
            rendererModel.SetScale(scale);

            Setup();
        }

        /// <summary>
        /// Given a rendering element, traverse the elements compute required bounds
        /// to full display all elements. The method searches for <see cref="Bounds"/>
        /// elements which act to specify the required bounds when adjunct labels
        /// are considered.
        /// </summary>
        /// <param name="element">a rendering element</param>
        /// <returns>the bounds required (<see cref="Rect.Empty"/> if unspecified)</returns>
        public virtual Rect GetBounds(IRenderingElement element)
        {
            if (element == null)
                return Rect.Empty;
            var bounds = new Bounds(element);
            return new Rect(bounds.MinX, bounds.MinY,
                            bounds.Width, bounds.Height);
        }
    }
}
