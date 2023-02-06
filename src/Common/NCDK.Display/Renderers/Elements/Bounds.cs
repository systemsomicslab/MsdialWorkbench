/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using WPF = System.Windows;

namespace NCDK.Renderers.Elements
{
    /// <summary>
    /// Defines a bounding box element which the renderer can use to determine the true
    /// drawing limits. Using only atom coordinates adjuncts (e.g. hydrogen labels)
    /// may be truncated. If a generator provide a bounding box element, then the
    /// min/max bounds of all bounding boxes are utilised.
    /// </summary>
    // @author John May
    // @cdk.module renderbasic
    public sealed class Bounds : IRenderingElement
    {
        /// <summary>
        /// Minimum x/y coordinates.
        /// </summary>
        public double MinX, MinY;

        /// <summary>
        /// Maximum x/y coordinates.
        /// </summary>
        public double MaxX, MaxY;

        /// <summary>
        /// Know which elements are within this bound box.
        /// </summary>
        private readonly ElementGroup elements = new ElementGroup();

        /// <summary>
        /// Specify the min/max coordinates of the bounding box.
        /// </summary>
        /// <param name="x1">min x coordinate</param>
        /// <param name="y1">min y coordinate</param>
        /// <param name="x2">max x coordinate</param>
        /// <param name="y2">max y coordinate</param>
        public Bounds(double x1, double y1, double x2, double y2)
        {
            this.MinX = x1;
            this.MinY = y1;
            this.MaxX = x2;
            this.MaxY = y2;
        }

        /// <summary>
        /// An empty bounding box.
        /// </summary>
        public Bounds()
            : this(double.MaxValue, double.MaxValue,
                   double.MinValue, double.MinValue)
        { }

        /// <summary>
        /// An bounding box around the specified element.
        /// </summary>
        public Bounds(IRenderingElement element)
            : this()
        {
            Add(element);
        }

        /// <summary>
        /// Add the specified element bounds.
        /// </summary>
        public void Add(IRenderingElement element)
        {
            elements.Add(element);
            Traverse(element);
        }

        /// <summary>
        /// Ensure the point x,y is included in the bounding box.
        /// </summary>
        /// <param name="p">coordinate</param>
        public void Add(Point p)
        {
            if (p.X < MinX) MinX = p.X;
            if (p.Y < MinY) MinY = p.Y;
            if (p.X > MaxX) MaxX = p.X;
            if (p.Y > MaxY) MaxY = p.Y;
        }

        /// <summary>
        /// Add one bounds to another.
        /// </summary>
        /// <param name="bounds">other bounds</param>
        public void Add(Bounds bounds)
        {
            if (bounds.MinX < MinX) MinX = bounds.MinX;
            if (bounds.MinY < MinY) MinY = bounds.MinY;
            if (bounds.MaxX > MaxX) MaxX = bounds.MaxX;
            if (bounds.MaxY > MaxY) MaxY = bounds.MaxY;
        }

        /// <summary>
        /// Add the provided general path to the bounding box.
        /// </summary>
        /// <param name="path">general path</param>
        private void Add(GeneralPath path)
        {
            var b = path.Elements.Bounds;
            if (b.IsEmpty)
                return;
            Add(b.BottomLeft);
            Add(b.BottomRight);
            Add(b.TopLeft);
            Add(b.TopRight);
        }

        private void Traverse(IRenderingElement newElement)
        {
            var stack = new Deque<IRenderingElement>();
            stack.Push(newElement);
            while (stack.Any())
            {
                var element = stack.Poll();
                switch (element)
                {
                    case Bounds e:
                        Add(e);
                        break;
                    case GeneralPath e:
                        Add(e);
                        break;
                    case LineElement lineElem:
                        var vec = lineElem.SecondPoint - lineElem.FirstPoint;
                        var ortho = new WPF::Vector(-vec.Y, vec.X);
                        ortho.Normalize();
                        vec.Normalize();
                        ortho *= lineElem.Width / 2;  // stroke width
                        vec *= lineElem.Width / 2;    // stroke rounded also makes line longer
                        Add(lineElem.FirstPoint - vec + ortho);
                        Add(lineElem.SecondPoint + vec + ortho);
                        Add(lineElem.FirstPoint - vec - ortho);
                        Add(lineElem.SecondPoint + vec - ortho);
                        break;
                    case OvalElement oval:
                        Add(new Point(oval.Coord.X - oval.Radius, oval.Coord.Y));
                        Add(new Point(oval.Coord.X + oval.Radius, oval.Coord.Y));
                        Add(new Point(oval.Coord.X, oval.Coord.Y - oval.Radius));
                        Add(new Point(oval.Coord.X, oval.Coord.Y + oval.Radius));
                        break;
                    case ElementGroup elementGroup:
                        stack.AddRange(elementGroup);
                        break;
                    case MarkedElement e:
                        stack.Add(e.Element());
                        break;
                    default:
                        // ignored from bounds calculation, we don't really
                        // care but log we skipped it
                        Trace.TraceWarning($"{element.GetType()} not included in bounds calculation");
                        break;
                }
            }
        }

        /// <summary>
        /// Access the root rendering element, it contains all
        /// elements added to the bounds so far.
        /// </summary>
        /// <returns>root rendering element</returns>
        public IRenderingElement Root => elements;

        /// <summary>
        /// Specifies the width of the bounding box.
        /// </summary>
        /// <returns>the width of the bounding box</returns>
        public double Width => MaxX - MinX;

        /// <summary>
        /// Specifies the height of the bounding box.
        /// </summary>
        /// <returns>the height of the bounding box</returns>
        public double Height => MaxY - MinY;

        /// <summary>
        /// The bounds are empty and contain no elements.
        ///
        /// <returns>bounds are empty (true) or not (false)</returns>
        /// </summary>
        public bool IsEmpty() => MinX > MaxX || MinY > MaxY;

        public void Accept(IRenderingVisitor visitor, Transform transform)
        {
            visitor.Visit(this, transform);
        }

        public void Accept(IRenderingVisitor visitor)
        {
            visitor.Visit(this);
        }

        public override string ToString()
        {
            return "{{" + MinX + ", " + MinY + "} - {" + MaxX + ", " + MaxY + "}}";
        }
    }
}
