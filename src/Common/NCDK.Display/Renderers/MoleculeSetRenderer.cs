/*  Copyright (C) 2008-2009  Gilleain Torrance <gilleain.torrance@gmail.com>
 *                2008-2009  Arvid Berg <goglepox@users.sf.net>
 *                     2009  Stefan Kuhn <shk3@users.sf.net>
 *                     2009  Egon Willighagen <egonw@users.sf.net>
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
using NCDK.Renderers.Fonts;
using NCDK.Renderers.Generators;
using NCDK.Renderers.Visitors;
using System;
using System.Collections.Generic;
using System.Windows;

namespace NCDK.Renderers
{
    /// <summary>
    /// A general renderer for <see cref="IChemModel"/>s, <see cref="IReaction"/>s, and
    /// <see cref="IAtomContainer"/>s. The chem object
    /// is converted into a 'diagram' made up of <see cref="IRenderingElement"/>s. It takes
    /// an <see cref="IDrawVisitor"/> to do the drawing of the generated diagram. Various
    /// display properties can be set using the <see cref="RendererModel"/>.
    /// </summary>
    /// <example>
    /// This class has several usage patterns. For just painting fit-to-screen do:
    /// <code>
    ///   renderer.PaintMolecule(molecule, visitor, drawArea)
    /// </code>
    /// for painting at a scale determined by the bond length in the RendererModel:
    /// <code>
    ///   if (moleculeIsNew) {
    ///     renderer.Setup(molecule, drawArea);
    ///   }
    ///   Rectangle diagramSize = renderer.PaintMolecule(molecule, visitor);
    ///   // ...update scroll bars here
    /// </code>
    /// to paint at full screen size, but not resize with each change:
    /// <code>
    ///   if (moleculeIsNew) {
    ///     renderer.SetScale(molecule);
    ///     Rectangle diagramBounds = renderer.CalculateDiagramBounds(molecule);
    ///     renderer.SetZoomToFit(diagramBounds, drawArea);
    ///     renderer.PaintMolecule(molecule, visitor);
    ///   } else {
    ///     Rectangle diagramSize = renderer.PaintMolecule(molecule, visitor);
    ///   // ...update scroll bars here
    ///   }
    /// </code>
    /// finally, if you are scrolling, and have not changed the diagram:
    /// <code>
    ///   renderer.Repaint(visitor)
    /// </code>
    /// will just repaint the previously generated diagram, at the same scale.
    /// </example>
    /// <remarks>
    /// <para>
    /// There are two sets of methods for painting IChemObjects - those that take
    /// a Rectangle that represents the desired draw area, and those that return a
    /// Rectangle that represents the actual draw area. The first are intended for
    /// drawing molecules fitted to the screen (where 'screen' means any drawing
    /// area) while the second type of method are for drawing bonds at the length
    /// defined by the <see cref="RendererModel"/> parameter bondLength.
    /// </para>
    /// <para>
    /// There are two numbers used to transform the model so that it fits on screen.
    /// The first is <tt>scale</tt>, which is used to map model coordinates to
    /// screen coordinates. The second is <tt>zoom</tt> which is used to, well,
    /// zoom the on screen coordinates. If the diagram is fit-to-screen, then the
    /// ratio of the bounds when drawn using bondLength and the bounds of
    /// the screen is used as the zoom.
    /// </para>
    /// <para>
    /// So, if the bond length on screen is set to 40, and the average bond length
    /// of the model is 2 (unitless, but roughly Ångström scale) then the
    /// scale will be 20. If the model is 10 units wide, then the diagram drawn at
    /// 100% zoom will be 10/// 20 = 200 in width on screen. If the screen is 400
    /// pixels wide, then fitting it to the screen will make the zoom 200%. Since the
    /// zoom is just a floating point number, 100% = 1 and 200% = 2.
    /// </para>
    /// </remarks>
    // @author maclean
    // @cdk.module renderextra
    public class MoleculeSetRenderer : AbstractRenderer<IChemObjectSet<IAtomContainer>>, IRenderer<IChemObjectSet<IAtomContainer>>
    {
        private IRenderer<IAtomContainer> atomContainerRenderer;

        /// <summary>
        /// A renderer that generates diagrams using the specified
        /// generators and manages fonts with the supplied font manager.
        /// </summary>
        /// <param name="generators">a list of classes that implement the IGenerator interface</param>
        /// <param name="fontManager">a class that manages mappings between zoom and font sizes</param>
        public MoleculeSetRenderer(IEnumerable<IGenerator<IAtomContainer>> generators, IFontManager fontManager)
            : this(new RendererModel(), generators, fontManager)
        { }

        public MoleculeSetRenderer(RendererModel rendererModel, IEnumerable<IGenerator<IAtomContainer>> generators, IFontManager fontManager)
            : base(rendererModel)
        {
            this.fontManager = fontManager;
            atomContainerRenderer = new AtomContainerRenderer(rendererModel, generators, fontManager);
            this.generators = Array.Empty<IGenerator<IChemObjectSet<IAtomContainer>>>();
            this.Setup();
        }

        /// <summary>
        /// Setup the transformations necessary to draw this <see cref="IChemObjectSet{T}"/>.
        /// </summary>
        /// <param name="moleculeSet">the <see cref="IChemObjectSet{T}"/> for what to set the scale</param>
        /// <param name="screen">the <see cref="Rect"/> for which to calculate the scale</param>
        public void Setup(IChemObjectSet<IAtomContainer> moleculeSet, Rect screen)
        {
            this.SetScale(moleculeSet);
            var bounds = BoundsCalculator.CalculateBounds(moleculeSet);
            if (bounds != null) this.modelCenter = new Point(bounds.CenterX(), bounds.CenterY());
            this.drawCenter = new Point(screen.CenterX(), screen.CenterY());
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
        public override Rect Shift(Rect screenBounds, Rect diagramBounds)
        {
            var screenMaxX = screenBounds.X + screenBounds.Width;
            var screenMaxY = screenBounds.Y + screenBounds.Height;
            var diagramMaxX = diagramBounds.X + diagramBounds.Width;
            var diagramMaxY = diagramBounds.Y + diagramBounds.Height;

            var leftOverlap = screenBounds.X - diagramBounds.X;
            var rightOverlap = diagramMaxX - screenMaxX;
            var topOverlap = screenBounds.Y - diagramBounds.Y;
            var bottomOverlap = diagramMaxY - screenMaxY;

            double diffx = 0;
            double diffy = 0;
            var width = screenBounds.Width;
            var height = screenBounds.Height;

            if (leftOverlap > 0)
            {
                diffx = leftOverlap;
            }

            if (rightOverlap > 0)
            {
                width += rightOverlap;
            }

            if (topOverlap > 0)
            {
                diffy = topOverlap;
            }

            if (bottomOverlap > 0)
            {
                height += bottomOverlap;
            }

            if (diffx != 0 || diffy != 0)
            {
                this.ShiftDrawCenter(diffx, diffy);
            }

            return new Rect(diffx, diffy, width, height);
        }

        /// <summary>
        /// Set the scale for an <see cref="IChemObjectSet{T}"/>. It calculates the average bond length
        /// of the model and calculates the multiplication factor to transform this
        /// to the bond length that is set in the RendererModel.
        /// </summary>
        /// <param name="moleculeSet">the <see cref="IChemObjectSet{T}"/> for what to set the scale</param>
        public void SetScale(IChemObjectSet<IAtomContainer> moleculeSet)
        {
            var bondLength = AverageBondLengthCalculator.CalculateAverageBondLength(moleculeSet);
            var scale = this.CalculateScaleForBondLength(bondLength);

            // store the scale so that other components can access it
            this.rendererModel.SetScale(scale);
        }

        /// <inheritdoc/>
        public Rect Paint(IChemObjectSet<IAtomContainer> moleculeSet, IDrawVisitor drawVisitor)
        {
            // total up the bounding boxes
            var totalBounds = BoundsCalculator.CalculateBounds(moleculeSet);

            // setup and draw
            this.SetupTransformNatural(totalBounds);

            IRenderingElement diagram = this.GenerateDiagram(moleculeSet);
            this.Paint(drawVisitor, diagram);

            return this.ConvertToDiagramBounds(totalBounds);
        }

        /// <summary>
        /// Paint a set of molecules.
        /// </summary>
        /// <param name="molecules">the <see cref="IChemObjectSet{T}"/> to paint</param>
        /// <param name="drawVisitor">the visitor that does the drawing</param>
        /// <param name="bounds">the bounds on the screen</param>
        /// <param name="resetCenter">if true, set the draw center to be the center of bounds</param>
        public void Paint(IChemObjectSet<IAtomContainer> molecules, IDrawVisitor drawVisitor, Rect bounds, bool resetCenter)
        {
            // total up the bounding boxes
            var totalBounds = BoundsCalculator.CalculateBounds(molecules);

            this.SetupTransformToFit(bounds, totalBounds,
                    AverageBondLengthCalculator.CalculateAverageBondLength(molecules), resetCenter);

            IRenderingElement diagram = this.GenerateDiagram(molecules);
            this.Paint(drawVisitor, diagram);
        }

        /// <inheritdoc/>
        public override IRenderingElement GenerateDiagram(IChemObjectSet<IAtomContainer> molecules)
        {
            var diagram = new ElementGroup();
            foreach (var molecule in molecules)
            {
                diagram.Add(atomContainerRenderer.GenerateDiagram(molecule));
            }
            return diagram;
        }

        /// <inheritdoc/>
        public Rect CalculateDiagramBounds(IChemObjectSet<IAtomContainer> moleculeSet)
        {
            if (moleculeSet == null)
                return this.CalculateScreenBounds(new Rect());
            return this.CalculateScreenBounds(BoundsCalculator.CalculateBounds(moleculeSet));
        }

        /// <summary>
        /// Given a bond length for a model, calculate the scale that will transform
        /// this length to the on screen bond length in RendererModel.
        /// </summary>
        /// <param name="modelBondLength">the wanted model bond length in screen length</param>
        /// <returns>returns the scale that causes the drawn bond lengths in pixels to match the given model bond length</returns>
        public override double CalculateScaleForBondLength(double modelBondLength)
        {
            if (double.IsNaN(modelBondLength) || modelBondLength == 0)
            {
                return rendererModel.GetScale();
            }
            else
            {
                return this.rendererModel.GetBondLength() / modelBondLength;
            }
        }

        /// <inheritdoc/>
        public IList<IGenerator<IChemObjectSet<IAtomContainer>>> GetGenerators()
        {
            return new List<IGenerator<IChemObjectSet<IAtomContainer>>>(generators);
        }
    }
}
