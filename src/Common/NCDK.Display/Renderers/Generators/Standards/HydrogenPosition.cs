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

using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static NCDK.Renderers.Generators.Standards.HydrogenPosition;
using static NCDK.Renderers.Generators.Standards.VecmathUtil;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// Enumeration of hydrogen label position for 2D depictions. The best placement of the
    /// label can depend on a variety of factors. Currently, the <see cref="HydrogenPositionTools.Position(IAtom, IList{IAtom})"/>
    /// method decides the position based on the atom and neighbouring atom coordinates.
    /// </summary>
    // @author John May
    public enum HydrogenPosition
    {
         Right,
         Left,
         Above,
         Below,
    }

    public static class HydrogenPositionTools
    {
        internal static HydrogenPosition[] Values { get; } = new[]
        {
             Right,
             Left,
             Above,
             Below,
        };

        private static readonly string[] names = new string[]
        {
            "Right",
            "Left",
            "Above",
            "Below",
        };

        public static string Name(this HydrogenPosition value)
            => names[(int)value];

        /// <summary>
        /// When a single atom is displayed in isolation the position defaults to the
        /// right unless the element is listed here. This allows us to correctly
        /// displayed H2O not OH2 and CH4 not H4C.
        /// </summary>
        private static readonly ISet<int> PrefixedH
            = new HashSet<int>()
                {
                    AtomicNumbers.Oxygen,
                    AtomicNumbers.Sulfur,
                    AtomicNumbers.Selenium,
                    AtomicNumbers.Tellurium,
                    AtomicNumbers.Fluorine,
                    AtomicNumbers.Chlorine,
                    AtomicNumbers.Bromine,
                    AtomicNumbers.Iodine,
                };

        /// <summary>
        /// When an atom has a single bond, the position is left or right depending
        /// only on this bond. This threshold defines the position at which we flip
        /// from positioning hydrogens on the right to positioning them on the left.
        /// A positive value favours placing them on the right, a negative on the
        /// left.
        /// </summary>
        private const double VerticalThreshold = 0.1;

        /// <summary>
        /// Tau = 2π.
        /// </summary>
        private const double Tau = Math.PI + Math.PI;

        public static HydrogenPosition Parse(string str)
        {
            var f = Array.FindIndex(names, n => n == str);
            if (f < 0)
                throw new ArgumentException();
            return (HydrogenPosition)f;
        }

        internal struct C
        {
            /// <summary>
            /// Direction this position is pointing in radians.
            /// </summary>
            public double Direction { get; private set; }

            /// <summary>
            /// The directional vector for this hydrogen position.
            /// </summary>
            public Vector2 Vector { get; private set; }

            /// <summary>
            /// Internal - create a hydrogen position pointing int he specified direction.
            /// </summary>
            /// <param name="direction">angle of the position in radians</param>
            public C(double direction, Vector2 vector)
            {
                Direction = direction;
                Vector = vector;
            }
        }

        static readonly C[] V = new[]
        {
            new C(0, new Vector2(1, 0)),
            new C(Math.PI, new Vector2(-1, 0)),
            new C(Math.PI / 2, new Vector2(0, 1)),
            new C(Math.PI + (Math.PI / 2), new Vector2(0, -1)),
        };

        /// <summary>
        /// The directional vector for this hydrogen position.
        /// </summary>
        public static Vector2 Vector(this HydrogenPosition value)
            => V[(int)value].Vector;

        /// <summary>
        /// Determine an appropriate position for the hydrogen label of an atom with
        /// the specified neighbors.
        /// </summary>
        /// <param name="atom">the atom to which the hydrogen position is being determined</param>
        /// <param name="neighbors">atoms adjacent to the 'atom'</param>
        /// <returns>a hydrogen position</returns>
        internal static HydrogenPosition Position(IAtom atom, IList<IAtom> neighbors)
        {
            var vectors = NewUnitVectors(atom, neighbors);

            if (neighbors.Count > 2)
            {
                return UsingAngularExtent(vectors);
            }
            else if (neighbors.Count > 1)
            {
                return UsingCardinalDirection(Average(vectors));
            }
            else if (neighbors.Count == 1)
            {
                return vectors[0].X > VerticalThreshold ? Left : Right;
            }
            else
            {
                return UsingDefaultPlacement(atom);
            }
        }

        /// <summary>
        /// Using the angular extents of vectors, determine the best position for a hydrogen label. The
        /// position with the most space is selected first. If multiple positions have the same amount of
        /// space, the one where the hydrogen position is most centred is selected. If all position are
        /// okay, the priority is Right > Left > Above > Below.
        /// </summary>
        /// <param name="vectors">directional vectors for each bond from an atom</param>
        /// <returns>best hydrogen position</returns>
        internal static HydrogenPosition UsingAngularExtent(IList<Vector2> vectors)
        {
            var extents = VecmathUtil.Extents(vectors);
            Array.Sort(extents);

            var extentMap = new Dictionary<HydrogenPosition, OffsetExtent>();

            for (int i = 0; i < extents.Length; i++)
            {
                double before = extents[i];
                double after = extents[(i + 1) % extents.Length];

                foreach (var position in Values)
                {
                    // adjust the extents such that this position is '0'
                    var bias = Tau - V[(int)position].Direction;
                    var afterBias = after + bias;
                    var beforeBias = before + bias;

                    // ensure values are 0 <= x < Tau
                    if (beforeBias >= Tau)
                        beforeBias -= Tau;
                    if (afterBias >= Tau)
                        afterBias -= Tau;

                    // we can now determine the extents before and after this
                    // hydrogen position
                    var afterExtent = afterBias;
                    var beforeExtent = Tau - beforeBias;

                    // the total extent is amount of space between these two bonds
                    // when sweeping round. The offset is how close this hydrogen
                    // position is to the center of the extent.
                    var totalExtent = afterExtent + beforeExtent;
                    var offset = Math.Abs(totalExtent / 2 - beforeExtent);

                    // for each position keep the one with the smallest extent this is
                    // the most space available without another bond getting in the way
                    if (!extentMap.TryGetValue(position, out OffsetExtent offsetExtent) || totalExtent < offsetExtent.Extent)
                    {
                        extentMap[position] = new OffsetExtent(totalExtent, offset);
                    }
                }
            }

            // we now have the offset extent for each position that we can sort and prioritise
            var extentEntries = extentMap;
            KeyValuePair<HydrogenPosition, OffsetExtent>? best = null;
            foreach (var e in extentEntries)
            {
                if (best == null || ExtentPriority.Instance.Compare(e, best.Value) < 0)
                    best = e;
            }

            Debug.Assert(best != null);
            return best.Value.Key;
        }

        /// <summary>
        /// A simple value class that stores a tuple of an angular extent and an offset.
        /// </summary>
        private sealed class OffsetExtent
        {
            public double Extent { get; private set; }
            public double Offset { get; private set; }

            /// <summary>
            /// Internal - create pairing of angular extent and offset.
            /// <param name="extent">the angular extent</param>
            /// <param name="offset">offset from the centre of the extent</param>
            /// </summary>
            public OffsetExtent(double extent, double offset)
            {
                Extent = extent;
                Offset = offset;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return string.Format($"{Extent.ToString("F2")}, {Offset.ToString("F2")}");
            }
        }

        /// <summary>
        /// Comparator to prioritise <see cref="OffsetExtent"/>s.
        /// </summary>
        private class ExtentPriority 
            : IComparer<KeyValuePair<HydrogenPosition, OffsetExtent>>
        {
            public static ExtentPriority Instance = new ExtentPriority();

            public int Compare(KeyValuePair<HydrogenPosition, OffsetExtent> a, KeyValuePair<HydrogenPosition, OffsetExtent> b)
            {
                var aExtent = a.Value;
                var bExtent = b.Value;

                // if difference in extents is noticeable, favour the one
                // with a larger extent
                var extentDiff = bExtent.Extent - aExtent.Extent;
                if (Math.Abs(extentDiff) > 0.05)
                    return (int)Math.Sign(extentDiff);

                // if the difference in offset is noticeable, favour the one
                // with the smaller offset (position is more centered)
                var offsetDiff = bExtent.Offset - aExtent.Offset;
                if (Math.Abs(offsetDiff) > 0.05)
                    return (int)-Math.Sign(offsetDiff);

                // favour Right > Left > Above > Below
                return a.Key.CompareTo(b.Key);
            }
        }

        /// <summary>
        /// By snapping to the cardinal direction (compass point) of the provided
        /// vector, return the position opposite the 'snapped' coordinate.
        /// </summary>
        /// <param name="opposite">position the hydrogen label opposite to this vector</param>
        /// <returns>the position</returns>
        internal static HydrogenPosition UsingCardinalDirection(Vector2 opposite)
        {
            var theta = Math.Atan2(opposite.Y, opposite.X);
            var direction = (int)Math.Round(theta / (Math.PI / 4));

            switch (direction)
            {
                case -4: // W
                case -3: // SW
                    return Right;
                case -2: // S
                    return Above;
                case -1: // SE
                case 0: // E
                case 1: // NE
                    return Left;
                case 2: // N
                    return Below;
                case 3: // NW
                case 4: // W?
                    return Right;
            }

            return Right; // never reached
        }

        /// <summary>
        /// Access the default position of the hydrogen label when the atom has no bonds.
        /// </summary>
        /// <param name="atom">hydrogens will be labelled</param>
        /// <returns>the position</returns>
        internal static HydrogenPosition UsingDefaultPlacement(IAtom atom)
        {
            if (PrefixedH.Contains(atom.AtomicNumber))
                return Left;
            return Right;
        }
    }
}
