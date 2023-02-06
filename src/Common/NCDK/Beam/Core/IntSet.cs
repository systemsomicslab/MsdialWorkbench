/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met: 
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer. 
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution. 
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies, 
 * either expressed or implied, of the FreeBSD Project.
 */

using System.Collections;

namespace NCDK.Beam
{
    /// <summary>
    /// Abstraction allows simple definitions of integer sets. Generally for this
    /// library we are dealing with small bounded integer ranges (vertices of a
    /// graph) which are most efficiently represented as a binary set. For
    /// convenience the <see cref="AllOf(int[])"/> method can be used to construct a
    /// binary set from varargs.
    /// </summary>
    // @author John May
    internal abstract class IntSet
    {
        /// <summary>
        /// Determine if value 'x' is a member of this set.
        /// </summary>
        /// <param name="x">a value to</param>
        /// <returns>x is included in this set</returns>.
        public abstract bool Contains(int x);

        /// <summary>
        /// The universe is a set which includes every int value.
        /// </summary>
        /// <returns>int set with every item</returns>
        public static IntSet Universe => UNIVERSE;

        /// <summary>
        /// The empty set is a set which includes no int values.
        /// <returns>int set with no items</returns>
        /// </summary>
        public static IntSet IsEmpty => CreateComplement(Universe);

        /// <summary>
        /// Convenience method to create a set with the specified contents.
        /// </summary>
        /// <example><code>
        ///     IntSet.AllOf(0, 2, 5); // a set with 0,2 and 5
        /// </code></example>
        /// <param name="xs">values</param>
        /// <returns>int set with specified items</returns>
        public static IntSet AllOf(params int[] xs)
        {
            return NAllOf(64, xs);
        }

        public static IntSet NAllOf(int size, params int[] xs)
        {
            BitArray s = new BitArray(size);
            foreach (var v in xs)
                s.Set(v, true);
            return new BinarySet(s);
        }

        /// <summary>
        /// Convenience method to create a set without the specified contents.
        /// </summary>
        /// <example><code>
        ///     IntSet.NoneOf(0, 2, 5); // a set with all but 0,2 and 5
        /// </code></example>
        /// <param name="xs">values</param>
        /// <returns>int set without the specified items</returns>
        public static IntSet NoneOf(params int[] xs)
        {
            return NNoneOf(64, xs);
        }

        public static IntSet NNoneOf(int size, params int[] xs)
        {
            return CreateComplement(NAllOf(size, xs));
        }

        /// <summary>
        /// Create an set from a BitArray.
        /// </summary>
        /// <param name="s">bitset</param>
        /// <returns>int set which uses the bit set to test for membership</returns>
        public static IntSet FromBitArray(BitArray s)
        {
            return new BinarySet((BitArray)s.Clone());
        }

        /// <summary>
        /// Make a complement of the specified set.
        /// </summary>
        /// <param name="set">a set</param>
        /// <returns>complement of the set</returns>
        private static IntSet CreateComplement(IntSet set)
        {
            return new Complement(set);
        }

        /// <summary>An integer set based on the contents of a bit set.</summary>
        private sealed class BinarySet : IntSet
        {
            private readonly BitArray s;

            public BinarySet(BitArray s)
            {
                this.s = s;
            }

            public override bool Contains(int x)
            {
                return s[x];
            }
        }

        /// <summary>Complement of a set - invert any membership of the provided 'delegate'</summary>
        private sealed class Complement : IntSet
        {
            private readonly IntSet delegate_;

            public Complement(IntSet delegate_)
            {
                this.delegate_ = delegate_;
            }

            public override bool Contains(int x)
            {
                return !delegate_.Contains(x);
            }
        }

        /// <summary>The universe - every object is a member of the set.</summary>
        private static readonly IntSet UNIVERSE = new UNIVERSE_IntSet();

        private class UNIVERSE_IntSet : IntSet
        {
            public override bool Contains(int x)
            {
                return true;
            }
        }
    }
}
