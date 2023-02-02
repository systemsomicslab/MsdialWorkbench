/*
 * Copyright (c) 2013 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using System;

namespace NCDK.Hash
{
    /// <summary>
    /// An abstract hash function providing several utility methods to be used by
    /// other hashing functions.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal class AbstractHashGenerator
    {
        /* pseudorandom number generator */
        private readonly Pseudorandom pseudorandom;

        /// <summary>
        /// Construct an abstract hash function providing the pseudorandom number
        /// generator.
        /// </summary>
        /// <param name="pseudorandom">a pseudorandom number generator</param>
        /// <exception cref="ArgumentNullException">the pseudorandom number generator was null</exception>
        public AbstractHashGenerator(Pseudorandom pseudorandom)
        {
            this.pseudorandom = pseudorandom ?? throw new ArgumentNullException(nameof(pseudorandom), "null pseduorandom number generator provided");
        }

        /// <summary>
        /// Create a copy of the array of long values.
        /// </summary>
        /// <param name="src">original values</param>
        /// <returns>copy of the original values</returns>
        /// <seealso cref="Array.Copy(Array, Array, int)"/>
        public static long[] Copy(long[] src)
        {
            var ret = new long[src.Length];
            Array.Copy(src, ret, src.Length);
            return ret;
        }

        /// <summary>
        /// Copy the values from the source (src) array to the destination (dest).
        /// </summary>
        /// <param name="src">source values</param>
        /// <param name="dest">destination of the source copy</param>
        /// <seealso cref="Array.ConstrainedCopy(Array, int, Array, int, int)"/>
        public static void Copy(long[] src, long[] dest)
        {
            Array.Copy(src, 0, dest, 0, dest.Length);
        }

        /// <summary>
        /// Generate the next random number.
        /// </summary>
        /// <param name="seed">a <see cref="long"/> value to seed a pseudorandom number generator</param>
        /// <returns>next pseudorandom number</returns>
        public long Rotate(long seed)
        {
            return pseudorandom.Next(seed);
        }

        /// <summary>
        /// Rotate a <paramref name="value"/>, <paramref name="n"/> times. The rotation uses a pseudorandom
        /// number generator to sequentially generate values seed on the previous
        /// value.
        /// </summary>
        /// <param name="value">the <see cref="long"/> value to rotate</param>
        /// <param name="n">the number of times to rotate the value</param>
        /// <returns>the <see cref="long"/> value rotated the specified number of times</returns>
        public long Rotate(long value, int n)
        {
            while (n-- > 0)
                value = pseudorandom.Next(value);
            return value;
        }

        /// <summary>
        /// Returns the value of the lowest three bits. This value is between 0 and 7
        /// inclusive.
        /// </summary>
        /// <param name="value">a <see cref="long"/> value</param>
        /// <returns>the <see cref="int"/> value of the lowest three bits.</returns>
        public static int LowestThreeBits(long value)
        {
            return (int)(value & 0x7);
        }

        /// <summary>
        /// Distribute the provided value across the set of <see cref="long"/> values.
        /// </summary>
        /// <param name="value">a <see cref="long"/> value to distribute</param>
        /// <returns>the <see cref="long"/> value distributed a set amount</returns>
        public long Distribute(long value)
        {
            // rotate 1-8 times
            return Rotate(value, 1 + LowestThreeBits(value));
        }

        /// <summary>
        /// Convert an <see cref="IAtomContainer"/> to an adjacency list.
        /// </summary>
        /// <param name="container">the container to convert</param>
        /// <returns>adjacency list representation</returns>
        public static int[][] ToAdjList(IAtomContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container), "atom container was null");

            int n = container.Atoms.Count;
            int[][] graph = Arrays.CreateJagged<int>(n, 16);
            int[] degree = new int[n];

            foreach (var bond in container.Bonds)
            {
                int v = container.Atoms.IndexOf(bond.Begin);
                int w = container.Atoms.IndexOf(bond.End);

                if (v < 0 || w < 0)
                    throw new ArgumentException("bond at index " + container.Bonds.IndexOf(bond)
                            + " contained an atom not pressent in molecule");

                graph[v][degree[v]++] = w;
                graph[w][degree[w]++] = v;

                // if the vertex degree of v or w reaches capacity, double the size
                if (degree[v] == graph[v].Length) graph[v] = Arrays.CopyOf(graph[v], degree[v] * 2);
                if (degree[w] == graph[w].Length) graph[w] = Arrays.CopyOf(graph[w], degree[w] * 2);
            }

            for (int v = 0; v < n; v++)
            {
                graph[v] = Arrays.CopyOf(graph[v], degree[v]);
            }

            return graph;
        }
    }
}
