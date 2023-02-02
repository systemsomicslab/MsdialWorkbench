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
using System;
using System.Collections;

namespace NCDK.Hash
{
    /// <summary>
    /// Defines a structure which indicates whether a vertex (int id) is suppressed
    /// when computing an atomic/molecular hash code.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal abstract class Suppressed
    {
        /// <summary>
        /// Is the vertex <paramref name="i"/> contained in the vertices which should be suppressed.
        /// </summary>
        /// <param name="i">vertex index</param>
        /// <returns>the vertex is supressed</returns>
        public abstract bool Contains(int i);

        /// <summary>
        /// The total number of suppressed vertices.
        /// </summary>
        /// <returns>number of suppressed vertices 0 .. |V|</returns>
        public abstract int Count { get; }

        /// <summary>
        /// Access which vertices are suppressed as a fixed-size array.
        /// </summary>
        /// <returns>the suppressed vertices</returns>
        public abstract int[] ToArray();

        /// <summary>Default 'empty' implementation always returns false.</summary>
        private sealed class Empty : Suppressed
        {
            public override bool Contains(int i) => false;
            public override int Count => 0;
            public override int[] ToArray() => Array.Empty<int>();
        }

        /// <summary>
        /// Implementation where the suppressed vertices are indicated with a BitArray.
        /// </summary>
        private sealed class SuppressedBitSet : Suppressed
        {
            /// <summary>Bits indicate suppressed vertices.</summary>
            private readonly BitArray set;

            /// <summary>
            /// Create a new suppressed instance with the specified vertices
            /// suppressed.
            /// </summary>
            /// <param name="set">bits indicates suppressed</param>
            internal SuppressedBitSet(BitArray set)
            {
                this.set = set;
            }

            public override bool Contains(int i) => set[i];
            public override int Count => BitArrays.Cardinality(set);
            public override int[] ToArray()
            {
                int[] xs = new int[Count];
                int n = 0;
                for (int i = BitArrays.NextSetBit(set, 0); i >= 0; i = BitArrays.NextSetBit(set, i + 1))
                {
                    xs[n++] = i;
                }
                return xs;
            }
        }

        /// <summary>default implementation.</summary>
        private static readonly Empty empty = new Empty();

        /// <summary>
        /// Access a suppressed implementation where no vertices are suppressed.
        /// </summary>
        /// <returns>implementation where all vertices are unsuppressed</returns>
        public static Suppressed None = empty;

        /// <summary>
        /// Create a suppressed implementation for the provided BitArray.
        /// </summary>
        /// <param name="set">bits indicated suppressed vertices</param>
        /// <returns>implementation using the BitArray to lookup suppressed vertices</returns>
        public static Suppressed FromBitSet(BitArray set)
        {
            return new SuppressedBitSet(set);
        }
    }
}
