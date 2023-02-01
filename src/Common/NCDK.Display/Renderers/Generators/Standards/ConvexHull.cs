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

using NCDK.Common.Collections;
using NCDK.Common.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// Immutable convex hull that is the smallest set of convex points that surround a shape.
    /// </summary>
    /// <example>
    /// <code>
    /// ConvexHull hull = ConvexHull.OfShape(shape);
    /// // the hull can be transformed
    /// hull = hull.Transform(Transform.Identity);
    /// // given a line, a point on the hull can be found that intersects the line
    /// Point point = hull.Intersect(new Point(px1, py1), new Point(px2, py2));
    /// </code>
    /// </example>
    // @author John May
    internal sealed class ConvexHull
    {
        /// <summary>The convex hull.</summary>
        private readonly Geometry hull;

        /// <summary>
        /// Internal constructor, the hull is an argument.
        /// </summary>
        /// <param name="hull">the convex hull</param>
        private ConvexHull(Geometry hull)
        {
            this.hull = hull;
        }

        /// <summary>
        /// Calculate the convex hull of a shape.
        /// </summary>
        /// <param name="shape">a shape</param>
        /// <returns>the convex hull</returns>
        public static ConvexHull OfShape(Geometry shape)
        {
            return new ConvexHull(ShapeOf(GrahamScan(PointsOf(shape))));
        }

        /// <summary>
        /// Calculate the convex hull of multiple shapes.
        /// </summary>
        /// <param name="shapes">shapes</param>
        /// <returns>the convex hull</returns>
        public static ConvexHull OfShapes(IList<Geometry> shapes)
        {
            var combined = new GeometryGroup
            {
                Children = new GeometryCollection(shapes)
            };
            return OfShape(combined);
        }

        /// <summary>
        /// The outline of the hull as a <see cref="Geometry"/>.
        /// </summary>
        public Geometry Outline => hull;

        /// <summary>
        /// Apply the provided transformation to the convex hull.
        /// </summary>
        /// <param name="transform">a transform</param>
        /// <returns>a new transformed hull</returns>
        public ConvexHull Transform(Transform transform)
        {
            var g = hull.Clone();
            g.Transform = new MatrixTransform(g.Transform.Value * transform.Value);
            return new ConvexHull(g);
        }

        /// <summary>
        /// Convert a list of points to a shape.
        /// </summary>
        /// <param name="points">list of points</param>
        /// <returns>a shape</returns>
        public static PathGeometry ShapeOf(IList<Point> points)
        {
            var g = new PathGeometry();
            if (points.Any())
            {
                var f = new PathFigure(points.First(), points.Skip(1).Select(n => new LineSegment(n, false)), true);
                g.Figures = new PathFigureCollection(new[] { f });
            }
            return g;
        }

        /// <summary>
        /// Convert a <see cref="Geometry"/> to a <see cref="IList{Point}"/>
        /// </summary>
        /// <param name="shape">a shape</param>
        /// <returns>list of point</returns>
        public static List<Point> PointsOf(Geometry shape)
        {
            var points = new List<Point>();
            PathGeometry path = shape.GetOutlinedPathGeometry();
            foreach (var figure in path.Figures)
            {
                var newPoints = new List<Point>
                {
                    figure.StartPoint
                };
                foreach (var seg in figure.Segments)
                {
                    switch (seg)
                    {
                        case ArcSegment s:
                            newPoints.Add(s.Point);
                            break;
                        case BezierSegment s:
                            newPoints.Add(s.Point1);
                            newPoints.Add(s.Point2);
                            newPoints.Add(s.Point3);
                            break;
                        case LineSegment s:
                            newPoints.Add(s.Point);
                            break;
                        case PolyBezierSegment s:
                            newPoints.AddRange(s.Points);
                            break;
                        case PolyLineSegment s:
                            newPoints.AddRange(s.Points);
                            break;
                        case PolyQuadraticBezierSegment s:
                            newPoints.AddRange(s.Points);
                            break;
                        default:
                            break;
                    }
                }
                if (newPoints.Count > 2 && newPoints.Last().Equals(figure.StartPoint))
                {
                    newPoints.RemoveAt(newPoints.Count - 1);
                }
                points.AddRange(newPoints);
            }
            return points;
        }

        /// <summary>
        /// The Graham Scan algorithm determines the points belonging to the convex hull in O(n lg n).
        /// </summary>
        /// <param name="points">set of points</param>
        /// <returns>points in the convex hull</returns>
        /// <seealso href="http://en.wikipedia.org/wiki/Graham_scan">Graham scan, Wikipedia</seealso>
        public static List<Point> GrahamScan(List<Point> points)
        {
            if (points.Count <= 3)
                return new List<Point>(points);

            var ps = new List<Point>(points);
            ps.Sort(new CompareYThenX());
            ps.Sort(new PolarComparator(ps[0]));

            Deque<Point> hull = new ArrayDeque<Point>();

            hull.Push(ps[0]);
            hull.Push(ps[1]);
            hull.Push(ps[2]);

            for (int i = 3; i < points.Count; i++)
            {
                var top = hull.Pop();
                while (hull.Any() && !IsLeftTurn(hull.Peek(), top, ps[i]))
                {
                    top = hull.Pop();
                }
                hull.Push(top);
                hull.Push(ps[i]);
            }

            return new List<Point>(hull);
        }

        /// <summary>
        /// Determine the minimum intersection of a line and the outline of a shape (specified as a list of points).
        /// </summary>
        /// <param name="outline">the outline of a shape</param>
        /// <param name="point1">start of the line</param>
        /// <param name="point2">end of the line</param>
        /// <returns>the intersection</returns>
        private Point Intersect(List<Point> outline, Point point1, Point point2)
        {
            var previousPoint = outline[outline.Count - 1];
            foreach (var point in outline)
            {
                if (Vectors.LinesIntersect(
                    point1.X, point1.Y,
                    point2.X, point2.Y,
                    point.X, point.Y,
                    previousPoint.X, previousPoint.Y))
                {
                    return LineLineIntersect(
                        point.X, point.Y,
                        previousPoint.X, previousPoint.Y,
                        point1.X, point1.Y,
                        point2.X, point2.Y);
                }
                previousPoint = point;
            }
            return point1;
        }

        /// <summary>
        /// Determine the intersect of a line (between <paramref name="a"/> and <paramref name="b"/>) with the hull.
        /// </summary>
        /// <param name="a">first point of the line</param>
        /// <param name="b">second points of the line</param>
        /// <returns>intersection, or null if not found</returns>
        public Point Intersect(Point a, Point b)
        {
            return Intersect(PointsOf(hull), new Point(a.X, a.Y), new Point(b.X, b.Y));
        }

        /// <summary>
        /// Calculate the intersection of two lines.
        /// </summary>
        /// <param name="lineA1">start of the line</param>
        /// <param name="lineA2">end of the line</param>
        /// <param name="lineB1">start of another line</param>
        /// <param name="lineB2">end of another line</param>
        /// <returns>the point where the two lines intersect (or null)</returns>
        public static Point LineLineIntersect(Point lineA1, Point lineA2, Point lineB1, Point lineB2)
        {
            return LineLineIntersect(
                lineA1.X, lineA1.Y,
                lineA2.X, lineA2.Y,
                lineB1.X, lineB1.Y,
                lineB2.X, lineB2.Y);
        }

        /// <summary>
        /// Calculate the intersection of two lines described by the points (x1,y1 -> x2,y2) and (x3,y3 -> x4,y4).
        /// </summary>
        /// <seealso href="http://en.wikipedia.org/wiki/Line–line_intersection">Line-line intersection, Wikipedia</seealso> 
        /// <param name="x1">first x coordinate of line 1</param>
        /// <param name="y1">first y coordinate of line 1</param>
        /// <param name="x2">second x coordinate of line 1</param>
        /// <param name="y2">second y coordinate of line 1</param>
        /// <param name="x3">first x coordinate of line 2</param>
        /// <param name="y3">first y coordinate of line 2</param>
        /// <param name="x4">first x coordinate of line 2</param>
        /// <param name="y4">first y coordinate of line 2</param>
        /// <returns>the point where the two lines intersect (or null)</returns>
        public static Point LineLineIntersect(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            double x = ((x2 - x1) * (x3 * y4 - x4 * y3) - (x4 - x3) * (x1 * y2 - x2 * y1)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
            double y = ((y3 - y4) * (x1 * y2 - x2 * y1) - (y1 - y2) * (x3 * y4 - x4 * y3)) / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));

            return new Point(x, y);
        }

        /// <summary>
        /// Sort points counter clockwise (polar order) around a reference point.
        /// </summary>
        public sealed class PolarComparator : IComparer<Point>
        {
            private Point reference;

            public PolarComparator(Point reference)
            {
                this.reference = reference;
            }

            /// <inheritdoc/>
            public int Compare(Point a, Point b)
            {
                double deltaX1 = a.X - reference.X;
                double deltaY1 = a.Y - reference.Y;
                double deltaX2 = b.X - reference.X;
                double deltaY2 = b.Y - reference.Y;

                if (deltaY1 >= 0 && deltaY2 < 0)
                    return -1;
                else if (deltaY2 >= 0 && deltaY1 < 0)
                    return +1;
                else if (deltaY1 == 0 && deltaY2 == 0)
                { // corner case
                    if (deltaX1 >= 0 && deltaX2 < 0)
                        return -1;
                    else if (deltaX2 >= 0 && deltaX1 < 0)
                        return +1;
                    else
                        return 0;
                }
                else
                    return -Winding(reference, a, b); // both above or below
            }
        }

        /// <summary>
        /// Compares points by the y coordinate and then the x if the y's are equal.
        /// </summary>
        public sealed class CompareYThenX : IComparer<Point>
        {
            /// <inheritdoc/>
            public int Compare(Point a, Point b)
            {
                if (a.Y < b.Y) return -1;
                if (a.Y > b.Y) return +1;
                if (a.X < b.X) return -1;
                if (a.X > b.X) return +1;
                return 0;
            }
        }

        /// <summary>
        /// Determine if the three points make a left turn.
        /// </summary>
        /// <param name="a">first point</param>
        /// <param name="b">second point</param>
        /// <param name="c">third point</param>
        /// <returns>whether the points make a left turn</returns>
        private static bool IsLeftTurn(Point a, Point b, Point c)
        {
            return Winding(a, b, c) > 0;
        }

        /// <summary>
        /// Determine the winding of three points. The winding is the sign of the space - the parity.
        /// </summary>
        /// <param name="a">first point</param>
        /// <param name="b">second point</param>
        /// <param name="c">third point</param>
        /// <returns>winding, -1=cw, 0=straight, +1=ccw</returns>
        private static int Winding(Point a, Point b, Point c)
        {
            return (int)Math.Sign((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X));
        }
    }
}
