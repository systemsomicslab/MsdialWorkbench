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
using System.Linq;
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
    /// of the model is 2 (unitless, but roughly Angstrom scale) then the
    /// scale will be 20. If the model is 10 units wide, then the diagram drawn at
    /// 100% zoom will be 10/// 20 = 200 in width on screen. If the screen is 400
    /// pixels wide, then fitting it to the screen will make the zoom 200%. Since the
    /// zoom is just a floating point number, 100% = 1 and 200% = 2.
    /// </para>
    /// </remarks>
    // @author maclean
    // @cdk.module renderextra
    public class ReactionRenderer : AbstractRenderer<IReaction>, IRenderer<IReaction>
    {
        private IRenderer<IChemObjectSet<IAtomContainer>> moleculeSetRenderer;

        /// <summary>
        /// A renderer that generates diagrams using the specified
        /// generators and manages fonts with the supplied font manager.
        /// </summary>
        /// <param name="generators">a list of classes that implement the IGenerator interface</param>
        /// <param name="fontManager">a class that manages mappings between zoom and font sizes</param>
        public ReactionRenderer(IEnumerable<IGenerator<IAtomContainer>> generators, IFontManager fontManager)
            : this(new RendererModel(), generators, fontManager)
        {
        }

        public ReactionRenderer(RendererModel rendererModel, IEnumerable<IGenerator<IAtomContainer>> generators, IFontManager fontManager)
            : base(rendererModel)
        {
            this.fontManager = fontManager;
            moleculeSetRenderer = new MoleculeSetRenderer(rendererModel, generators, fontManager);
        }

        /// <summary>
        /// A renderer that draws <see cref="IReaction"/>s with the passed <see cref="IGenerator{T}"/>
        /// for both <see cref="IAtomContainer"/> and <see cref="IReaction"/> content.
        /// </summary>
        /// <param name="generators">a list of classes that implement the <see cref="IGenerator{T}"/> interface</param>
        /// <param name="reactionGenerators">a list of <see cref="IGenerator{T}"/> related to <see cref="IReaction"/>s</param>
        /// <param name="fontManager">a class that manages mappings between zoom and font sizes</param>
        public ReactionRenderer(IEnumerable<IGenerator<IAtomContainer>> generators, IEnumerable<IGenerator<IReaction>> reactionGenerators, IFontManager fontManager)
            : this(generators, fontManager)
        {
            this.generators = reactionGenerators.ToList();
            this.Setup();
        }

        /// <summary>
        /// Setup the transformations necessary to draw this Reaction.
        /// </summary>
        /// <param name="reaction"></param>
        /// <param name="screen"></param>
        public void Setup(IReaction reaction, Rect screen)
        {
            this.SetScale(reaction);
            var bounds = BoundsCalculator.CalculateBounds(reaction);
            this.modelCenter = new Point(bounds.CenterX(), bounds.CenterY());
            this.drawCenter = new Point(screen.CenterX(), screen.CenterY());
            this.Setup();
        }

        /// <summary>
        /// Set the scale for an IReaction. It calculates the average bond length
        /// of the model and calculates the multiplication factor to transform this
        /// to the bond length that is set in the RendererModel.
        /// </summary>
        /// <param name="reaction"></param>
        public void SetScale(IReaction reaction)
        {
            double bondLength = AverageBondLengthCalculator.CalculateAverageBondLength(reaction);
            double scale = this.CalculateScaleForBondLength(bondLength);

            // store the scale so that other components can access it
            this.rendererModel.SetScale(scale);
        }

        /// <inheritdoc/>
        public Rect Paint(IReaction reaction, IDrawVisitor drawVisitor)
        {
            // calculate the bounds
            var modelBounds = BoundsCalculator.CalculateBounds(reaction);

            // setup and draw
            this.SetupTransformNatural(modelBounds);
            var diagram = this.GenerateDiagram(reaction);
            this.Paint(drawVisitor, diagram);

            return this.ConvertToDiagramBounds(modelBounds);
        }

        /// <summary>
        /// Paint a reaction.
        /// </summary>
        /// <param name="reaction">the reaction to paint</param>
        /// <param name="drawVisitor">the visitor that does the drawing</param>
        /// <param name="bounds">the bounds on the screen</param>
        /// <param name="resetCenter">///     if true, set the draw center to be the center of bounds</param>
        public void Paint(IReaction reaction, IDrawVisitor drawVisitor, Rect bounds, bool resetCenter)
        {
            // calculate the bounds
            var modelBounds = BoundsCalculator.CalculateBounds(reaction);

            this.SetupTransformToFit(bounds, modelBounds, AverageBondLengthCalculator.CalculateAverageBondLength(reaction), resetCenter);

            // generate the elements
            var diagram = this.GenerateDiagram(reaction);

            // paint it
            this.Paint(drawVisitor, diagram);
        }

        /// <inheritdoc/>
        public Rect CalculateDiagramBounds(IReaction reaction)
        {
            return this.CalculateScreenBounds(BoundsCalculator.CalculateBounds(reaction));
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
        public override IRenderingElement GenerateDiagram(IReaction reaction)
        {
            var diagram = new ElementGroup();

            foreach (var generator in this.generators)
            {
                diagram.Add(generator.Generate(reaction, rendererModel));
            }

            diagram.Add(moleculeSetRenderer.GenerateDiagram(reaction.Reactants));
            diagram.Add(moleculeSetRenderer.GenerateDiagram(reaction.Products));

            return diagram;
        }

        /// <inheritdoc/>
        public IList<IGenerator<IReaction>> GetGenerators()
        {
            return new List<IGenerator<IReaction>>(generators);
        }
    }
}
