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

using NCDK.Common.Mathematics;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using WPF = System.Windows;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// A collection of static utilities for Java 3D javax.vecmath.* objects.
    /// </summary>
    // @author John May
    internal static class VecmathUtil
    {
        /// <summary>
        /// Convert a <see cref="NCDK.Numerics.Vector2"/> point to a WPF point.
        /// </summary>
        /// <param name="point"><see cref="NCDK.Numerics.Vector2"/> point</param>
        /// <returns>WPF point</returns>
        public static Point ToPoint(Vector2 point) => new Point(point.X, point.Y);

        /// <summary>
        /// Convert a WPF point to a <see cref="NCDK.Numerics.Vector2"/>.
        /// </summary>
        /// <param name="vector">WPF point</param>
        /// <returns><see cref="NCDK.Numerics.Vector2"/> point</returns>
        public static Vector2 ToVector(Point vector) => new Vector2(vector.X, vector.Y);

        /// <summary>
        /// Convert a WPF point to a <see cref="NCDK.Numerics.Vector2"/>.
        /// </summary>
        /// <param name="vector">WPF point</param>
        /// <returns><see cref="NCDK.Numerics.Vector2"/> point</returns>
        public static Vector2 ToVector2(WPF::Vector vector) => new Vector2(vector.X, vector.Y);

        /// <summary>
        /// Create a unit vector between two points.
        /// </summary>
        /// <param name="from">start of vector</param>
        /// <param name="to">end of vector</param>
        /// <returns>unit vector</returns>
        public static Vector2 NewUnitVector(Vector2 from, Vector2 to)
        {
            var vector = new Vector2(to.X - from.X, to.Y - from.Y);
            vector = Vector2.Normalize(vector);
            return vector;
        }

        public static WPF::Vector NewUnitVector(Point from, Point to)
        {
            var v = from - to;
            v.Normalize();
            return v;
        }

        /// <summary>
        /// Create a unit vector for a bond with the start point being the specified atom.
        /// </summary>
        /// <param name="atom">start of vector</param>
        /// <param name="bond">the bond used to create the vector</param>
        /// <returns>unit vector</returns>
        public static Vector2 NewUnitVector(IAtom atom, IBond bond)
        {
            return NewUnitVector(atom.Point2D.Value, bond.GetOther(atom).Point2D.Value);
        }

        /// <summary>
        /// Create unit vectors from one atom to all other provided atoms.
        /// </summary>
        /// <param name="fromAtom">reference atom (will become 0,0)</param>
        /// <param name="toAtoms">list of to atoms</param>
        /// <returns>unit vectors</returns>
        public static IList<Vector2> NewUnitVectors(IAtom fromAtom, IEnumerable<IAtom> toAtoms)
        {
            var unitVectors = new List<Vector2>();
            foreach (var toAtom in toAtoms)
            {
                unitVectors.Add(NewUnitVector(fromAtom.Point2D.Value, toAtom.Point2D.Value));
            }
            return unitVectors;
        }

        /// <summary>
        /// Create a new vector perpendicular (at a right angle) to the provided
        /// vector. In 2D, there are two perpendicular vectors, the other
        /// perpendicular vector can be obtained by negation.
        /// </summary>
        /// <param name="vector">reference to which a perpendicular vector is returned</param>
        /// <returns>perpendicular vector</returns>
        public static Vector2 NewPerpendicularVector(Vector2 vector)
        {
            return new Vector2(-vector.Y, vector.X);
        }

        public static WPF::Vector NewPerpendicularVector(WPF::Vector vector)
        {
            return new WPF::Vector(-vector.Y, vector.X);
        }

        /// <summary>
        /// Midpoint of a line described by two points, a and b.
        /// </summary>
        /// <param name="a">first point</param>
        /// <param name="b">second point</param>
        /// <returns>the midpoint</returns>
        public static Vector2 Midpoint(Vector2 a, Vector2 b)
        {
            return new Vector2((a.X + b.X) / 2, (a.Y + b.Y) / 2);
        }

        /// <summary>
        /// Scale a vector by a given factor, the input vector is not modified.
        /// </summary>
        /// <param name="vector">a vector to scale</param>
        /// <param name="factor">how much the input vector should be scaled</param>
        /// <returns>scaled vector</returns>
        public static Vector2 Scale(Vector2 vector, double factor)
        {
            return vector * factor;
        }

        /// <summary>
        /// Sum the components of two vectors, the input is not modified.
        /// </summary>
        /// <param name="a">first vector</param>
        /// <param name="b">second vector</param>
        /// <returns>scaled vector</returns>
        public static Vector2 Sum(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        /// <summary>
        /// Negate a vector, the input is not modified. Equivalent to <c>Scale(vector, -1)</c>
        /// </summary>
        /// <param name="vector">a vector to negate</param>
        /// <returns>the negated vector</returns>
        public static Vector2 Negate(Vector2 vector)
        {
            return new Vector2(-vector.X, -vector.Y);
        }

        /// <summary>
        /// Calculate the intersection of two vectors given their starting positions.
        /// </summary>
        /// <param name="p1">position vector 1</param>
        /// <param name="d1">direction vector 1</param>
        /// <param name="p2">position vector 2</param>
        /// <param name="d2">direction vector 2</param>
        /// <returns>the intersection</returns>
        public static Vector2 Intersection(Vector2 p1, Vector2 d1, Vector2 p2, Vector2 d2)
        {
            var p1End = Sum(p1, d1);
            var p2End = Sum(p2, d2);
            return Intersection(p1.X, p1.Y, p1End.X, p1End.Y, p2.X, p2.Y, p2End.X, p2End.Y);
        }

        /// <summary>
        /// Calculate the intersection of two lines described by the points (x1,y1 -> x2,y2) and (x3,y3
        /// -> x4,y4).
        /// </summary>
        /// <param name="x1">first x coordinate of line 1</param>
        /// <param name="y1">first y coordinate of line 1</param>
        /// <param name="x2">second x coordinate of line 1</param>
        /// <param name="y2">second y coordinate of line 1</param>
        /// <param name="x3">first x coordinate of line 2</param>
        /// <param name="y3">first y coordinate of line 2</param>
        /// <param name="x4">first x coordinate of line 2</param>
        /// <param name="y4">first y coordinate of line 2</param>
        /// <returns>the point where the two lines intersect (or null)</returns>
        /// <seealso href="http://en.wikipedia.org/wiki/Line–line_intersection">Line-line intersection, Wikipedia</seealso>
        public static Vector2 Intersection(double x1, double y1, double x2, double y2, double x3, double y3, double x4, double y4)
        {
            var x = ((x2 - x1) * (x3 * y4 - x4 * y3) - (x4 - x3) * (x1 * y2 - x2 * y1))
                  / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
            var y = ((y3 - y4) * (x1 * y2 - x2 * y1) - (y1 - y2) * (x3 * y4 - x4 * y3))
                  / ((x1 - x2) * (y3 - y4) - (y1 - y2) * (x3 - x4));
            return new Vector2(x, y);
        }

        /// <summary>
        /// Given vectors for the hypotenuse and adjacent side of a right angled
        /// triangle and the length of the opposite side, determine how long the
        /// adjacent side size.
        /// </summary>
        /// <param name="hypotenuse">vector for the hypotenuse</param>
        /// <param name="adjacent">vector for the adjacent side</param>
        /// <param name="oppositeLength">length of the opposite side of a triangle</param>
        /// <returns>length of the adjacent side</returns>
        public static double AdjacentLength(Vector2 hypotenuse, Vector2 adjacent, double oppositeLength)
        {
            return Math.Tan(Vectors.Angle(hypotenuse, adjacent)) * oppositeLength;
        }

        /// <summary>
        /// Average a collection of vectors.
        /// </summary>
        /// <param name="vectors">one or more vectors</param>
        /// <returns>average vector</returns>
        public static Vector2 Average(ICollection<Vector2> vectors)
        {
            var average = new Vector2(0, 0);
            foreach (var vector in vectors)
            {
                average += vector;
            }
            average *= (1.0 / vectors.Count);
            return average;
        }

        /// <summary>
        /// Given a list of unit vectors, find the vector which is nearest to a
        /// provided reference.
        /// </summary>
        /// <param name="reference">a target vector</param>
        /// <param name="vectors">list of vectors</param>
        /// <returns>the nearest vector</returns>
        /// <exception cref="ArgumentException">no vectors provided</exception>
        public static Vector2 GetNearestVector(Vector2 reference, IList<Vector2> vectors)
        {
            if (!vectors.Any())
                throw new ArgumentException("No vectors provided", nameof(vectors));

            // to find the closest vector we find use the dot product,
            // for the general case (non-unit vectors) one can use the
            // cosine similarity
            var closest = vectors[0];
            double maxProd = Vector2.Dot(reference, closest);

            for (int i = 1; i < vectors.Count; i++)
            {
                var newProd = Vector2.Dot(reference, vectors[i]);
                if (newProd > maxProd)
                {
                    maxProd = newProd;
                    closest = vectors[i];
                }
            }

            return closest;
        }

        /// <summary>
        /// Given a list of bonds, find the bond which is nearest to a provided
        /// reference and return the vector for this bond.
        /// </summary>
        /// <param name="reference">a target vector</param>
        /// <param name="fromAtom">an atom (will be 0,0)</param>
        /// <param name="bonds">list of bonds containing 'fromAtom'</param>
        /// <returns>the nearest vector</returns>
        public static Vector2 GetNearestVector(Vector2 reference, IAtom fromAtom, IList<IBond> bonds)
        {
            var toAtoms = new List<IAtom>();
            foreach (var bond in bonds)
            {
                toAtoms.Add(bond.GetOther(fromAtom));
            }

            return GetNearestVector(reference, NewUnitVectors(fromAtom, toAtoms));
        }

        /// <summary>
        /// Tau = (2π) ~ 6.283 radians ~ 360 degrees
        /// </summary>
        private const double TAU = 2 * Math.PI;

        /// <summary>
        /// Calculate the angular extent of a vector (0-2π radians) anti clockwise from
        /// east {1,0}.
        /// </summary>
        /// <param name="vector">a vector</param>
        /// <returns>the extent (radians)</returns>
        public static double Extent(Vector2 vector)
        {
            var radians = Math.Atan2(vector.Y, vector.X);
            return radians < 0 ? TAU + radians : radians;
        }

        /// <summary>
        /// Obtain the extents for a list of vectors.
        /// </summary>
        /// <param name="vectors">list of vectors</param>
        /// <returns>array of extents (not sorted)</returns>
        /// <seealso cref="Extent(Vector2)"/>
        public static double[] Extents(IList<Vector2> vectors)
        {
            var n = vectors.Count;
            var extents = new double[n];
            for (int i = 0; i < n; i++)
                extents[i] = VecmathUtil.Extent(vectors[i]);
            return extents;
        }

        /// <summary>
        /// Generate a 2D directional vector that is located in the middle of the largest angular extent
        /// (i.e. has the most space). For example if we have a two vectors, one pointing up and one
        /// pointing right we have to extents (.5π and 1.5π). The new vector would be pointing down and
        /// to the left in the middle of the 1.5π extent.
        /// </summary>
        /// <param name="vectors">list of vectors</param>
        /// <returns>the new vector</returns>
        public static Vector2 NewVectorInLargestGap(IList<Vector2> vectors)
        {
            Debug.Assert(vectors.Count > 1);
            var extents = VecmathUtil.Extents(vectors);
            Array.Sort(extents);

            // find and store the index of the largest extent
            double max = -1;
            int index = -1;
            for (int i = 0; i < vectors.Count; i++)
            {
                var extent = extents[(i + 1) % vectors.Count] - extents[i];
                if (extent < 0)
                    extent += TAU;
                if (extent > max)
                {
                    max = extent;
                    index = i;
                }
            }

            Debug.Assert(index >= 0);

            var mid = (max / 2);
            var theta = extents[index] + mid;

            return new Vector2(Math.Cos(theta), Math.Sin(theta));
        }
    }
}
