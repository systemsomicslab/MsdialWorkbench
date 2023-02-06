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
    /// There are two sets of methods for painting IChemObjects - those that take
    /// a Rectangle that represents the desired draw area, and those that return a
    /// Rectangle that represents the actual draw area. The first are intended for
    /// drawing molecules fitted to the screen (where 'screen' means any drawing
    /// area) while the second type of method are for drawing bonds at the length
    /// defined by the <see cref="RendererModel"/> parameter bondLength.
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
    public class ChemModelRenderer : AbstractRenderer<IChemModel>, IRenderer<IChemModel>
    {
        private IRenderer<IChemObjectSet<IAtomContainer>> moleculeSetRenderer;
        private IRenderer<IReactionSet> reactionSetRenderer;

        /// <summary>
        /// A renderer that generates diagrams using the specified
        /// generators and manages fonts with the supplied font manager.
        /// </summary>
        /// <param name="generators">a list of classes that implement the IGenerator interface</param>
        /// <param name="fontManager">a class that manages mappings between zoom and font sizes</param>
        public ChemModelRenderer(IEnumerable<IGenerator<IAtomContainer>> generators, IFontManager fontManager)
            : base(new RendererModel())
        {
            this.fontManager = fontManager;
            moleculeSetRenderer = new MoleculeSetRenderer(rendererModel, generators, fontManager);
            reactionSetRenderer = new ReactionSetRenderer(rendererModel, generators, fontManager);
            this.Setup();
        }

        public ChemModelRenderer(List<IGenerator<IAtomContainer>> generators, IList<IGenerator<IReaction>> reactionGenerators, IFontManager fontManager)
                : base(new RendererModel())
        {
            this.fontManager = fontManager;
			moleculeSetRenderer = new MoleculeSetRenderer(rendererModel, generators, fontManager);
            reactionSetRenderer = new ReactionSetRenderer(rendererModel, generators, reactionGenerators, fontManager);
            this.Setup();
        }

        /// <summary>
        /// Setup the transformations necessary to draw this Chem Model.
        /// </summary>
        /// <param name="chemModel"></param>
        /// <param name="screen"></param>
        public void Setup(IChemModel chemModel, Rect screen)
        {
            this.SetScale(chemModel);
            var bounds = BoundsCalculator.CalculateBounds(chemModel);
            if (bounds != null)
                this.modelCenter = new Point((bounds.Left + bounds.Right) / 2, 
                                             (bounds.Top + bounds.Bottom) / 2);
            this.drawCenter = new Point((screen.Left + screen.Right) / 2, (screen.Top + screen.Bottom) / 2);
            this.Setup();
        }

        /// <summary>
        /// Set the scale for an IChemModel. It calculates the average bond length of
        /// the model and calculates the multiplication factor to transform this
        /// to the bond length that is set in the RendererModel.
        /// </summary>
        /// <param name="chemModel"></param>
        public void SetScale(IChemModel chemModel)
        {
            var bondLength = AverageBondLengthCalculator.CalculateAverageBondLength(chemModel);
            var scale = this.CalculateScaleForBondLength(bondLength);

            // store the scale so that other components can access it
            this.rendererModel.SetScale(scale);
        }

        /// <summary>
        /// Paint an IChemModel using the IDrawVisitor at a scale determined by the
        /// bond length in RendererModel.
        /// </summary>
        /// <param name="chemModel">the chem model to draw</param>
        /// <param name="drawVisitor">the visitor used to draw with</param>
        /// <returns>the rectangular area that the diagram will occupy on screen</returns>
        public Rect Paint(IChemModel chemModel, IDrawVisitor drawVisitor)
        {
            var moleculeSet = chemModel.MoleculeSet;
            var reactionSet = chemModel.ReactionSet;

            if (moleculeSet == null && reactionSet != null)
            {
                var totalBounds = BoundsCalculator.CalculateBounds(reactionSet);
                this.SetupTransformNatural(totalBounds);
                var diagram = reactionSetRenderer.GenerateDiagram(reactionSet);
                this.Paint(drawVisitor, diagram);
                return this.ConvertToDiagramBounds(totalBounds);
            }

            if (moleculeSet != null && reactionSet == null)
            {
                var totalBounds = BoundsCalculator.CalculateBounds(moleculeSet);
                this.SetupTransformNatural(totalBounds);
                var diagram = moleculeSetRenderer.GenerateDiagram(moleculeSet);
                this.Paint(drawVisitor, diagram);
                return this.ConvertToDiagramBounds(totalBounds);
            }

            if (moleculeSet != null && reactionSet != null)
            {
                var totalBounds = BoundsCalculator.CalculateBounds(chemModel);

                this.SetupTransformNatural(totalBounds);

                var diagram = new ElementGroup
                {
                    reactionSetRenderer.GenerateDiagram(reactionSet),
                    moleculeSetRenderer.GenerateDiagram(moleculeSet)
                };

                this.Paint(drawVisitor, diagram);

                // the size of the painted diagram is returned
                return this.ConvertToDiagramBounds(totalBounds);
            }
            return new Rect(0, 0, 0, 0);
        }

        //    public Rectangle PaintReactionSet(
        //            IReactionSet reactionSet, IDrawVisitor drawVisitor) {
        //        // total up the bounding boxes
        //        Rectangle2D totalBounds = CreateRectangle();
        //        foreach (var reaction in reactionSet.reactions()) {
        //            Rectangle2D modelBounds = BoundsCalculator.CalculateBounds(reaction);
        //            if (totalBounds == null) {
        //                totalBounds = modelBounds;
        //            } else {
        //                totalBounds = totalBounds.CreateUnion(modelBounds);
        //            }
        //        }
        //
        //        // setup and draw
        //        this.SetupTransformNatural(totalBounds);
        //        ElementGroup diagram = new ElementGroup();
        //        foreach (var reaction in reactionSet.reactions()) {
        //            diagram.Add(this.GenerateDiagram(reaction));
        //        }
        //        this.Paint(drawVisitor, diagram);
        //
        //        // the size of the painted diagram is returned
        //        return this.ConvertToDiagramBounds(totalBounds);
        //    }
        //
        //    public Rectangle PaintReaction(
        //            IReaction reaction, IDrawVisitor drawVisitor) {
        //
        //        // calculate the bounds
        //        Rectangle2D modelBounds = BoundsCalculator.CalculateBounds(reaction);
        //
        //        // setup and draw
        //        this.SetupTransformNatural(modelBounds);
        //        IRenderingElement diagram = this.GenerateDiagram(reaction);
        //        this.Paint(drawVisitor, diagram);
        //
        //        return this.ConvertToDiagramBounds(modelBounds);
        //    }

        /// <summary>
        /// Paint a ChemModel.
        /// </summary>
        /// <param name="chemModel"></param>
        /// <param name="drawVisitor">the visitor that does the drawing</param>
        /// <param name="bounds">the bounds of the area to paint on.</param>
        /// <param name="resetCenter">if true, set the modelCenter to the center of the ChemModel's bounds.</param>
        public void Paint(IChemModel chemModel, IDrawVisitor drawVisitor, Rect bounds, bool resetCenter)
        {
            // check for an empty model
            var moleculeSet = chemModel.MoleculeSet;
            IReactionSet reactionSet = chemModel.ReactionSet;

            // nasty, but it seems that reactions can be read in as ChemModels
            // with BOTH a ReactionSet AND a MoleculeSet...
            if (moleculeSet == null || reactionSet != null)
            {
                if (reactionSet != null)
                {
                    reactionSetRenderer.Paint(reactionSet, drawVisitor, bounds, resetCenter);
                }
                return;
            }

            // calculate the total bounding box
            var modelBounds = BoundsCalculator.CalculateBounds(moleculeSet);

            this.SetupTransformToFit(bounds, modelBounds, 
                                     AverageBondLengthCalculator.CalculateAverageBondLength(chemModel), 
                                     resetCenter);

            // generate the elements
            var diagram = moleculeSetRenderer.GenerateDiagram(moleculeSet);

            // paint it
            this.Paint(drawVisitor, diagram);
        }

        /// <summary>
        /// Repaint using the cached diagram.
        /// </summary>
        /// <param name="drawVisitor">the wrapper for the graphics object that draws</param>
        public override void Repaint(IDrawVisitor drawVisitor)
        {
            this.Paint(drawVisitor, cachedDiagram);
        }

        /// <summary>
        /// Given a chem model, calculates the bounding rectangle in screen space.
        /// </summary>
        /// <param name="model">the model to draw.</param>
        /// <returns>a rectangle in screen space.</returns>
        public Rect CalculateDiagramBounds(IChemModel model)
        {
            var moleculeSet = model.MoleculeSet;
            var reactionSet = model.ReactionSet;
            if ((moleculeSet == null && reactionSet == null))
            {
                return Rect.Empty;
            }

            var moleculeBounds = Rect.Empty;
            var reactionBounds = Rect.Empty;
            if (moleculeSet != null)
            {
                moleculeBounds = BoundsCalculator.CalculateBounds(moleculeSet);
            }
            if (reactionSet != null)
            {
                reactionBounds = BoundsCalculator.CalculateBounds(reactionSet);
            }

            if (moleculeBounds.IsEmpty)
            {
                return this.CalculateScreenBounds(reactionBounds);
            }
            else if (reactionBounds.IsEmpty)
            {
                return this.CalculateScreenBounds(moleculeBounds);
            }
            else
            {
                var allbounds = Rect.Union(moleculeBounds, reactionBounds);
                return this.CalculateScreenBounds(allbounds);
            }
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
        public IList<IGenerator<IChemModel>> GetGenerators()
        {
            return null;
        }
    }
}
