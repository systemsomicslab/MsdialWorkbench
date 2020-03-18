/* Copyright (C) 2009  Gilleain Torrance <gilleain.torrance@gmail.com>
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;

namespace NCDK.Graphs
{
    /// <summary>
    /// General permutation generator, that uses orderly generation by ranking and
    /// unranking.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The basic idea is that all permutations of length N can be ordered
    /// (lexicographically) like:<br/>
    /// 0 [0, 1, 2]<br/>
    /// 1 [0, 2, 1]<br/>
    /// 2 [1, 0, 2]<br/>
    /// ...<br/>
    /// where the number to the left of each permutation is the <i>rank</i> - really
    /// just the index in this ordered list. The list is created on demand, by a
    /// process called <i>unranking</i> where the rank is converted to the
    /// permutation that appears at that point in the list.
    /// </para>
    /// <para>The algorithms used are from the book "Combinatorial Generation :
    /// Algorithms, Generation, and Search" (or C.A.G.E.S.) by D.L. Kreher and D.R.
    /// Stinson. CRC Press (18 Dec 1998). ISBN-13 : 978-0849339882.</para>
    /// </remarks>
    // @author         maclean
    // @cdk.created    2009-09-09
    // @cdk.keyword    permutation
    // @cdk.module     standard
    public class Permutor
    {
        /// <summary>
        /// The current rank of the permutation to use
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// The maximum rank possible, given the size
        /// </summary>
        private readonly int maxRank;

        /// <summary>
        /// The number of objects to permute
        /// </summary>
        private readonly int size;

        /// <summary>
        /// For accessing part of the permutation space
        /// </summary>
        private readonly Random random;

        /// <summary>
        /// Create a permutor that will generate permutations of numbers up to <paramref name="size"/>.
        /// </summary>
        /// <param name="size">the size of the permutations to generate</param>
        public Permutor(int size)
        {
            this.Rank = 0;
            this.size = size;
            this.maxRank = this.CalculateMaxRank();
            this.random = new Random();
        }

        protected internal bool HasNext()
        {
            return this.Rank < this.maxRank;
        }

        /// <summary>
        /// Set the currently used permutation.
        /// </summary>
        /// <param name="permutation">the permutation to use, as an int array</param>
        public void SetPermutation(int[] permutation)
        {
            this.Rank = this.RankPermutationLexicographically(permutation);
        }

        /// <summary>
        /// Randomly skip ahead in the list of permutations.
        /// </summary>
        /// <returns>a permutation in the range (current, N!)</returns>
        public int[] GetRandomNextPermutation()
        {
            int d = maxRank - Rank;
            int r = this.random.Next(d);
            this.Rank += Math.Max(1, r);
            return this.GetCurrentPermutation();
        }

        /// <summary>
        /// Get the next permutation in the list.
        /// </summary>
        /// <returns>the next permutation</returns>
        public int[] GetNextPermutation()
        {
            this.Rank++;
            return this.GetCurrentPermutation();
        }

        /// <summary>
        /// Get the permutation that is currently being used.
        /// </summary>
        /// <returns>the permutation as an int array</returns>
        public int[] GetCurrentPermutation()
        {
            return this.UnrankPermutationLexicographically(Rank, size);
        }

        /// <summary>
        /// Calculate the max possible rank for permutations of N numbers.
        /// </summary>
        /// <returns>the maximum number of permutations</returns>
        public int CalculateMaxRank()
        {
            return Factorial(size) - 1;
        }

        // much much more efficient to pre-calculate this (or lazily calculate)
        // and store in an array, at the cost of memory.
        private int Factorial(int i)
        {
            if (i > 0)
            {
                return i * Factorial(i - 1);
            }
            else
            {
                return 1;
            }
        }

        /// <summary>
        /// Convert a permutation (in the form of an int array) into a 'rank' - which
        /// is just a single number that is the order of the permutation in a lexico-
        /// graphically ordered list.
        /// </summary>
        /// <param name="permutation">the permutation to use</param>
        /// <returns>the rank as a number</returns>
        private int RankPermutationLexicographically(int[] permutation)
        {
            int rank = 0;
            int n = permutation.Length;
            int[] counter = new int[n + 1];
            for (int i = 1; i < permutation.Length; i++)
            {
                counter[i] = permutation[i - 1] + 1;
            }
            for (int j = 1; j <= n; j++)
            {
                rank += (counter[j] - 1) * Factorial(n - j);
                for (int i = j + 1; i <= n; i++)
                {
                    if (counter[i] > counter[j])
                    {
                        counter[i]--;
                    }
                }
            }
            return rank + 1;
        }

        /// <summary>
        /// Performs the opposite to the rank method, producing the permutation that
        /// has the order <paramref name="rank"/> in the lexicographically ordered list.
        /// </summary>
        /// <remarks>
        /// As an implementation note, the algorithm assumes that the permutation is
        /// in the form [1,...N] not the more usual [0,...N-1] for a list of size N.
        /// This is why there is the final step of 'shifting' the permutation. The
        /// shift also reduces the numbers by one to make them array indices.
        /// </remarks>
        /// <param name="rank">the order of the permutation to generate</param>
        /// <param name="size">the length/size of the permutation</param>
        /// <returns>a permutation as an int array</returns>
        private int[] UnrankPermutationLexicographically(int rank, int size)
        {
            int[] permutation = new int[size + 1];
            permutation[size] = 1;
            for (int j = 1; j < size; j++)
            {
                int d = (rank % Factorial(j + 1)) / Factorial(j);
                rank = rank - d * Factorial(j);
                permutation[size - j] = d + 1;
                for (int i = size - j + 1; i <= size; i++)
                {
                    if (permutation[i] > d)
                    {
                        permutation[i]++;
                    }
                }
            }

            // convert an array of numbers like [1...n] to [0...n-1]
            int[] shiftedPermutation = new int[size];
            for (int i = 1; i < permutation.Length; i++)
            {
                shiftedPermutation[i - 1] = permutation[i] - 1;
            }
            return shiftedPermutation;
        }
    }
}
