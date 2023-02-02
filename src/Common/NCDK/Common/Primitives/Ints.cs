/*
 * Copyright (C) 2008 The Guava Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF Any KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using static NCDK.Common.Base.Preconditions;

namespace NCDK.Common.Primitives
{
    public static partial class Ints
    {
        /// <summary>
        /// Compares the two specified <see cref="int"/> values. The sign of the value returned is the same as
        /// that of <c><paramref name="a"/>.CompareTo(<paramref name="b"/>)</c>.
        /// <para><b>Note for Java 7 and later:</b> this method should be treated as deprecated; use the
        /// equivalent <see cref="int.CompareTo(int)"/> method instead.</para>
        /// </summary>
        /// <param name="a">the first <see cref="int"/> to compare</param>
        /// <param name="b">the second <see cref="int"/> to compare</param>
        /// <returns>a negative value if <paramref name="a"/> is less than <paramref name="b"/>; a positive value if <paramref name="a"/> is greater than <paramref name="b"/>; or zero if they are equal</returns>
        public static int Compare(int a, int b)
        {
            return (a < b) ? -1 : ((a > b) ? 1 : 0);
        }

        /// <summary>
        /// Returns an array containing the same values as <paramref name="array"/>, but
        /// guaranteed to be of a specified minimum length. If <paramref name="array"/> already
        /// has a length of at least <paramref name="minLength"/>, it is returned directly.
        /// Otherwise, a new array of size <paramref name="array"/> + <paramref name="padding"/> is returned,
        /// containing the values of <paramref name="array"/>, and zeroes in the remaining places.
        /// </summary>
        /// <param name="array">the source array</param>
        /// <param name="minLength">the minimum length the returned array must guarantee</param>
        /// <param name="padding">an extra amount to "grow" the array by if growth is necessary</param>
        /// <returns>an array containing the values of <paramref name="array"/>, with guaranteed minimum length <paramref name="minLength"/></returns>
        /// <exception cref="ArgumentException">if <paramref name="minLength"/> or <paramref name="padding"/> is negative</exception>
        public static int[] EnsureCapacity(
            int[] array, int minLength, int padding)
        {
            CheckArgument(minLength >= 0, "Invalid minLength: {0}", minLength);
            CheckArgument(padding >= 0, "Invalid padding: {0}", padding);
            return (array.Length < minLength)
                ? CopyOf(array, minLength + padding)
                : array;
        }

        private static int[] CopyOf(int[] original, int length)
        {
            int[] copy = new int[length];
            Array.Copy(original, 0, copy, 0, Math.Min(original.Length, length));
            return copy;
        }       
    }
}

/*
 * Copyright 1994-2006 Sun Microsystems, Inc.  All Rights Reserved.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * This code is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 2 only, as
 * published by the Free Software Foundation.  Sun designates this
 * particular file as subject to the "Classpath" exception as provided
 * by Sun in the LICENSE file that accompanied this code.
 *
 * This code is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * version 2 for more details (a copy is included in the LICENSE file that
 * accompanied this code).
 *
 * You should have received a copy of the GNU General Public License version
 * 2 along with this work; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 * Please contact Sun Microsystems, Inc., 4150 Network Circle, Santa Clara,
 * CA 95054 USA or visit www.sun.com if you need additional information or
 * have any questions.
 */
namespace NCDK.Common.Primitives
{
    public static partial class Ints
    {
        /// <summary>
        /// Returns an <see cref="int"/> value with at most a single one-bit, in the
        /// position of the highest-order("leftmost") one-bit in the specified
        /// <see cref="int"/> value.Returns zero if the specified value has no
        /// one-bits in its two's complement binary representation, that is, if it
        /// is equal to zero.
        /// </summary>
        /// <param name="i"></param>
        /// <returns>an <see cref="int"/> value with a single one-bit, in the position
        /// of the highest-order one-bit in the specified value, or zero if
        /// the specified value is itself equal to zero.
        /// </returns>
        public static int HighestOneBit(int i)
        {
            uint ui = (uint)i;
            // HD, Figure 3-1
            ui |= (ui >> 1);
            ui |= (ui >> 2);
            ui |= (ui >> 4);
            ui |= (ui >> 8);
            ui |= (ui >> 16);
            return (int)(ui - (ui >> 1));
        }

        /// <summary>
        /// Returns the number of zero bits preceding the highest-order
        /// ("leftmost") one-bit in the two's complement binary representation
        /// of the specified <see cref="int"/> value.  Returns 32 if the
        /// specified value has no one-bits in its two's complement representation,
        /// in other words if it is equal to zero.
        /// </summary>
        /// <remarks>
        /// <para>Note that this method is closely related to the logarithm base 2.
        /// For all positive <see cref="Int32"/> values x:
        /// <list type="bullet">
        /// <item>floor(log<sub>2</sub>(x)) = <c>31 - NumberOfLeadingZeros(x)</c></item>
        /// <item>ceil(log<sub>2</sub>(x)) = <c>32 - NumberOfLeadingZeros(x - 1)</c></item>
        /// </list> 
        /// </para>
        /// </remarks>
        /// <param name="i"></param>
        /// <returns>the number of zero bits preceding the highest-order
        ///     ("leftmost") one-bit in the two's complement binary representation
        ///     of the specified <see cref="Int32"/> value, or 32 if the value
        ///     is equal to zero.</returns>
        public static int NumberOfLeadingZeros(int i)
        {
            uint ui = (uint)i;
            // HD, Figure 5-6
            if (ui == 0)
                return 32;
            uint n = 1;
            if (ui >> 16 == 0) { n += 16; ui <<= 16; }
            if (ui >> 24 == 0) { n += 8; ui <<= 8; }
            if (ui >> 28 == 0) { n += 4; ui <<= 4; }
            if (ui >> 30 == 0) { n += 2; ui <<= 2; }
            n -= ui >> 31;
            return (int)n;
        }
    }
}
