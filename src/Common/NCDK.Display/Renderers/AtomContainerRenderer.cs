/*  Copyright (C) 2008-2009  Gilleain Torrance <gilleain.torrance@gmail.com>
 *                2008-2009  Arvid Berg <goglepox@users.sf.net>
 *                2009-2010  Egon Willighagen <egonw@users.sf.net>
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
using NCDK.Renderers.Fonts;
using NCDK.Renderers.Generators;
using NCDK.Renderers.Visitors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace NCDK.Renderers
{
    /// <summary>
    /// A general renderer for <see cref="IAtomContainer"/>s. The chem object
    /// is converted into a 'diagram' made up of <see cref="IRenderingElement"/>s. It takes
    /// an <see cref="IDrawVisitor"/> to do the drawing of the generated diagram. Various
    /// display properties can be set using the <see cref="RendererModel"/>.
    /// </summary>
    /// <remarks>
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
    /// 100% zoom will be 10 * 20 = 200 in width on screen. If the screen is 400
    /// pixels wide, then fitting it to the screen will make the zoom 200%. Since the
    /// zoom is just a floating point number, 100% = 1 and 200% = 2.
    /// </para>
    /// </remarks>
    // @author maclean
    // @cdk.module renderbasic
    public class AtomContainerRenderer : AbstractRenderer<IAtomContainer>, IRenderer<IAtomContainer>
    {
        /// <summary>
        /// The default scale is used when the model is empty.
        /// </summary>
        public const double DefaultScale = 30.0;

        /// <summary>
        /// A renderer that generates diagrams using the specified
        /// generators and manages fonts with the supplied font manager.
        /// </summary>
        /// <param name="generators">a list of classes that implement the IGenerator interface</param>
        /// <param name="fontManager">a class that manages mappings between zoom and font sizes</param>
        public AtomContainerRenderer(IEnumerable<IGenerator<IAtomContainer>> generators, IFontManager fontManager)
            : this(new RendererModel(), generators, fontManager)
        {
        }

        public AtomContainerRenderer(RendererModel rendererModel, IEnumerable<IGenerator<IAtomContainer>> generators, IFontManager fontManager)
            : base(rendererModel)
        {
            this.generators = generators.ToList();
            this.fontManager = fontManager;
        }

        /// <summary>
        /// Setup the transformations necessary to draw this Atom Container.
        /// </summary>
        /// <param name="atomContainer">the atom container to use in the setup</param>
        /// <param name="screen">the area to draw on</param>
        public void Setup(IAtomContainer atomContainer, Rect screen)
        {
            this.SetScale(atomContainer);
            var bounds = BoundsCalculator.CalculateBounds(atomContainer);
            this.modelCenter = new Point(bounds.CenterX(), bounds.CenterY());
            this.drawCenter = new Point(screen.CenterX(), screen.CenterY());
            this.Setup();
        }

        /// <summary>
        /// Reset the draw center and model center, and set the zoom to 100%.
        /// </summary>
        public void Reset()
        {
            modelCenter = new Point(0, 0);
            drawCenter = new Point(200, 200);
            rendererModel.SetZoomFactor(1);
            Setup();
        }

        /// <summary>
        /// Set the scale for an IAtomContainer. It calculates the average bond
        /// length of the model and calculates the multiplication factor to transform
        /// this to the bond length that is set in the RendererModel.
        /// </summary>
        /// <param name="atomContainer">the atom container that will be drawn</param>
        public void SetScale(IAtomContainer atomContainer)
        {
            var bondLength = GeometryUtil.GetBondLengthAverage(atomContainer);
            rendererModel.SetScale(this.CalculateScaleForBondLength(bondLength));
        }

        /// <inheritdoc/>
        public Rect Paint(IAtomContainer atomContainer, IDrawVisitor drawVisitor)
        {
            // the bounds of the model
            var modelBounds = BoundsCalculator.CalculateBounds(atomContainer);

            // setup and draw
            this.SetupTransformNatural(modelBounds);
            var diagram = GenerateDiagram(atomContainer);
            this.Paint(drawVisitor, diagram);

            return this.ConvertToDiagramBounds(modelBounds);
        }

        /// <summary>
        /// Determine an estimated bond length for disconnected structures. The
        /// distance between all atoms is measured and then the minimum distance is
        /// divided by 1.5. This length is required for scaling renderings.
        /// </summary>
        /// <param name="container">a chemical structure with no bonds at least 2 atoms</param>
        /// <returns>the estimated bond length</returns>
        /// <exception cref="ArgumentException">the structure had a bond or less than two atoms</exception>
        private static double EstimatedBondLength(IAtomContainer container)
        {
            if (container.Bonds.Count > 0)
                throw new ArgumentException("structure has at least one bond - disconnected scaling not need");
            if (container.Atoms.Count < 2) throw new ArgumentException("structure must have at least two atoms");

            int nAtoms = container.Atoms.Count;
            double minDistance = int.MaxValue;

            for (int i = 0; i < nAtoms; i++)
                for (int j = i + 1; j < nAtoms; j++)
                    minDistance = Math.Min(Vector2.Distance(container.Atoms[i].Point2D.Value, container.Atoms[j].Point2D.Value), minDistance);

            return minDistance / 1.5; // non-bonded, if they were they would be closer
        }

        /// <summary>
        /// Paint a molecule (an IAtomContainer).
        /// </summary>
        /// <param name="atomContainer">the molecule to paint</param>
        /// <param name="drawVisitor">the visitor that does the drawing</param>
        /// <param name="bounds">the bounds on the screen</param>
        /// <param name="resetCenter">if true, set the draw center to be the center of bounds</param>
        public void Paint(IAtomContainer atomContainer, IDrawVisitor drawVisitor, Rect bounds, bool resetCenter)
        {
            if (atomContainer.Bonds.Count > 0 || atomContainer.Atoms.Count == 1)
            {
                rendererModel.SetScale(
                    CalculateScaleForBondLength(GeometryUtil.GetBondLengthAverage(atomContainer)));
            }
            else if (atomContainer.Atoms.Count > 1)
            {
                rendererModel.SetScale(
                    CalculateScaleForBondLength(EstimatedBondLength(atomContainer)));
            }

            // the diagram to draw
            var diagram = GenerateDiagram(atomContainer);

            // the bounds of the model from 'Bounds' elements
            // no bounding elements, use the atom coordinates
            var modelBounds = GetBounds(diagram);
            if (modelBounds.IsEmpty)
                modelBounds = BoundsCalculator.CalculateBounds(atomContainer);

            SetupTransformToFit(bounds, modelBounds, resetCenter);

            this.Paint(drawVisitor, diagram);
        }

        /// <inheritdoc/>
        public Rect CalculateDiagramBounds(IAtomContainer atomContainer)
        {
            return CalculateScreenBounds(BoundsCalculator.CalculateBounds(atomContainer));
        }

        /// <summary>
        /// Given a bond length for a model, calculate the scale that will transform
        /// this length to the on screen bond length in RendererModel.
        /// </summary>
        /// <param name="modelBondLength">the average bond length of the model</param>
        /// <returns>the scale necessary to transform this to a screen bond</returns>
        public override double CalculateScaleForBondLength(double modelBondLength)
        {
            if (double.IsNaN(modelBondLength) || modelBondLength == 0)
            {
                return DefaultScale;
            }
            else
            {
                return rendererModel.GetBondLength() / modelBondLength;
            }
        }

        /// <inheritdoc/>
        public IList<IGenerator<IAtomContainer>> GetGenerators()
        {
            return new List<IGenerator<IAtomContainer>>(generators);
        }
    }
}
