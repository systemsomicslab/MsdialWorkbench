/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// Intermediate between an <see cref="Elements.IRenderingElement"/> and the atom
    /// data. The atom symbol represents a visible atom element with zero of more
    /// adjuncts. The adjuncts are hydrogen count, charge, and mass. The atom symbol
    /// is immutable and modifying a property or transforming the symbol makes a new
    /// instance.
    /// </summary>
    // @author John May
    internal sealed class AtomSymbol
    {
        /// <summary>
        /// The element symbol.
        /// </summary>
        private readonly TextOutline element;

        /// <summary>
        /// Adjuncts to the symbol, hydrogen count, charge, and mass.
        /// </summary>
        private readonly IList<TextOutline> adjuncts;

        /// <summary>
        /// Annotation adjuncts.
        /// </summary>
        private readonly IList<TextOutline> annotationAdjuncts;

        /// <summary>
        /// Desired alignment of the symbol.
        /// </summary>
        private readonly SymbolAlignment alignment;

        /// <summary>
        /// The convex hull of the entire atom symbol.
        /// </summary>
        private readonly ConvexHull hull;

        /// <summary>
        /// Alignment of symbol, left aligned symbols are centered on the first
        /// character, right aligned on the last character.
        /// </summary>
        public enum SymbolAlignment
        {
            Left, Center, Right
        }

        /// <summary>
        /// Create a new atom symbol with the specified adjuncts.
        /// </summary>
        /// <param name="element">the element symbol (e.g. 'N' in 'NH4+')</param>
        /// <param name="adjuncts">the adjuncts</param>
        public AtomSymbol(TextOutline element, IList<TextOutline> adjuncts)
        {
            this.element = element;
            this.adjuncts = adjuncts;
            this.annotationAdjuncts = new List<TextOutline>();
            this.alignment = SymbolAlignment.Center;
            this.hull = ConvexHull.OfShapes(GetOutlines());
        }

        /// <summary>
        /// Internal constructor provides the attributes.
        /// </summary>
        /// <param name="element">the element label</param>
        /// <param name="adjuncts">the adjunct labels</param>
        /// <param name="alignment">left, center, or right alignment</param>
        /// <param name="hull">convex hull</param>
        private AtomSymbol(TextOutline element, IList<TextOutline> adjuncts, IList<TextOutline> annotationAdjuncts, SymbolAlignment alignment, ConvexHull hull)
        {
            this.element = element;
            this.adjuncts = adjuncts;
            this.annotationAdjuncts = annotationAdjuncts;
            this.alignment = alignment;
            this.hull = hull;
        }

        /// <summary>
        /// Create a new atom symbol (from this symbol) but with the specified alignment.
        /// </summary>
        /// <param name="alignment">element alignment</param>
        /// <returns>new atom symbol</returns>
        public AtomSymbol AlignTo(SymbolAlignment alignment)
        {
            return new AtomSymbol(element, adjuncts, annotationAdjuncts, alignment, hull);
        }

        /// <summary>
        /// Include a new annotation adjunct in the atom symbol.
        /// </summary>
        /// <param name="annotation">the new annotation adjunct</param>
        /// <returns>a new AtomSymbol instance including the annotation adjunct</returns>
        public AtomSymbol AddAnnotation(TextOutline annotation)
        {
            List<TextOutline> newAnnotations = new List<TextOutline>(annotationAdjuncts)
            {
                annotation
            };
            return new AtomSymbol(element, adjuncts, newAnnotations, alignment, hull);
        }

        /// <summary>
        /// Access the center point of the symbol. The center point is determined by
        /// the alignment.
        /// </summary>
        /// <returns>center point</returns>
        public Point GetAlignmentCenter()
        {
            if (alignment == SymbolAlignment.Left)
            {
                return element.GetFirstGlyphCenter();
            }
            else if (alignment == SymbolAlignment.Right)
            {
                return element.GetLastGlyphCenter();
            }
            else
            {
                return element.GetCenter();
            }
        }

        /// <summary>
        /// Access the element outline.
        /// </summary>
        /// <returns>immutable element outline</returns>
        public TextOutline ElementOutline()
        {
            return element;
        }

        /// <summary>
        /// Access the adjunct outlines.
        /// </summary>
        /// <returns>immutable adjunct outlines</returns>
        public IList<TextOutline> AdjunctOutlines()
        {
            return new ReadOnlyCollection<TextOutline>(adjuncts);
        }

        /// <summary>
        /// Access the Java 2D shape text outlines that display the atom symbol.
        /// </summary>
        /// <returns>shapes</returns>
        public List<Geometry> GetOutlines()
        {
            var shapes = new List<Geometry>
            {
                element.GetOutline()
            };
            foreach (var adjunct in adjuncts)
                shapes.Add(adjunct.GetOutline());
            return shapes;
        }

        /// <summary>
        /// Access the java.awt.Shape outlines of each annotation adjunct.
        /// </summary>
        /// <returns>annotation outlines</returns>
        public GeometryCollection GetAnnotationOutlines()
        {
            var shapes = new GeometryCollection();
            foreach (var adjunct in annotationAdjuncts)
                shapes.Add(adjunct.GetOutline());
            return shapes;
        }

        /// <summary>
        /// Access the convex hull of the whole atom symbol.
        /// </summary>
        /// <returns>convex hull</returns>
        public ConvexHull GetConvexHull()
        {
            return hull;
        }

        /// <summary>
        /// Transform the position and orientation of the symbol.
        /// </summary>
        /// <param name="transform">affine transform</param>
        /// <returns>the transformed symbol (new instance)</returns>
        public AtomSymbol Transform(Transform transform)
        {
            List<TextOutline> transformedAdjuncts = new List<TextOutline>(adjuncts.Count);
            foreach (var adjunct in adjuncts)
                transformedAdjuncts.Add(adjunct.Transform(transform));
            List<TextOutline> transformedAnnAdjuncts = new List<TextOutline>(adjuncts.Count);
            foreach (var adjunct in annotationAdjuncts)
                transformedAnnAdjuncts.Add(adjunct.Transform(transform));
            return new AtomSymbol(element.Transform(transform), transformedAdjuncts, transformedAnnAdjuncts, alignment, hull.Transform(transform));
        }

        /// <summary>
        /// Convenience function to resize an atom symbol.
        /// </summary>
        /// <param name="scaleX">x-axis scaling</param>
        /// <param name="scaleY">y-axis scaling</param>
        /// <returns>the resized symbol (new instance)</returns>
        public AtomSymbol Resize(double scaleX, double scaleY)
        {
            var center = element.GetCenter();
            var transform = Matrix.Identity;
            transform.ScaleAtPrepend(scaleX, scaleY, center.X, center.Y);
            return Transform(new MatrixTransform(transform));
        }

        /// <summary>
        /// Convenience function to center an atom symbol on a specified point. The
        /// centering depends on the symbol alignment.
        /// </summary>
        /// <param name="x">x-axis location</param>
        /// <param name="y">y-axis location</param>
        /// <returns>the centered symbol (new instance)</returns>
        public AtomSymbol Center(double x, double y)
        {
            var center = GetAlignmentCenter();
            return Translate(x - center.X, y - center.Y);
        }

        /// <summary>
        /// Convenience function to translate an atom symbol on a specified point.
        /// </summary>
        /// <param name="x">x-axis location</param>
        /// <param name="y">y-axis location</param>
        /// <returns>the translated symbol (new instance)</returns>
        public AtomSymbol Translate(double x, double y)
        {
            var m = new TranslateTransform(x, y);
            return Transform(m);
        }
    }
}
