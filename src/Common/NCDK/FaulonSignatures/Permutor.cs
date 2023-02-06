using System;

namespace NCDK.FaulonSignatures
{
    /// <summary>
    /// General permutation generator, that uses orderly generation by ranking and
    /// unranking. The basic idea is that all permutations of length N can be ordered
    /// (lexicographically) like:
    /// <pre>
    /// 0 [0, 1, 2]
    /// 1 [0, 2, 1]
    /// 2 [1, 0, 2]
    /// ...
    /// </pre>
    /// where the number to the left of each permutation is the <i>rank</i> - really
    /// just the index in this ordered list. The list is created on demand, by a
    /// process called <i>unranking</i> where the rank is converted to the 
    /// permutation that appears at that point in the list.
    /// 
    /// <para>The algorithms used are from the book "Combinatorial Generation : 
    /// Algorithms, Generation, and Search" (or C.A.G.E.S.) by D.L. Kreher and D.R.
    /// Stinson</para>
    /// </summary>
    // @author maclean
    [Obsolete("This class is part of SMSD and either duplicates functionality elsewhere in the CDK or provides public access to internal implementation details. SMSD has been deprecated from the CDK with a newer, more recent version of SMSD is available at http://github.com/asad/smsd .")]
    public class Permutor
    {
        /// <summary>
        /// The current rank of the permutation to use 
        /// </summary>
        private int currentRank;

        /// <summary>
        /// The maximum rank possible, given the size
        /// </summary>
        private int maxRank;

        /// <summary>
        /// The number of objects to permute
        /// </summary>
        private int size;

        /// <summary>
        /// For accessing part of the permutation space
        /// </summary>
        private Random random;

        /// <summary>
        /// Create a permutor that will generate permutations of numbers up to <paramref name="size"/>.
        /// </summary>
        /// <param name="size">the size of the permutations to generate</param>
        public Permutor(int size)
        {
            this.currentRank = 0;
            this.size = size;
            this.maxRank = this.CalculateMaxRank();
            this.random = new Random();
        }

        public bool HasNext()
        {
            return this.currentRank < this.maxRank;
        }

        /// <summary>
        /// Set the permutation to use, given its rank.
        /// </summary>
        /// <param name="rank">the order of the permutation in the list</param>
        public void SetRank(int rank)
        {
            this.currentRank = rank;
        }

        public int GetRank()
        {
            return this.currentRank;
        }

        /// <summary>
        /// Set the currently used permutation.
        /// </summary>
        /// <param name="permutation">the permutation to use, as an int array</param>
        public void SetPermutation(int[] permutation)
        {
            //        this.currentRank = this.RankPermutationLexicographically(permutation);
            currentRank = RankPermutationLexicographically(permutation) - 1;    // TMP
        }

        /// <summary>
        /// Randomly skip ahead in the list of permutations.
        /// </summary>
        /// <returns>a permutation in the range (current, N!)</returns>
        public int[] GetRandomNextPermutation()
        {
            int d = maxRank - currentRank;
            int r = this.random.Next(d);
            this.currentRank += Math.Max(1, r);
            return this.GetCurrentPermutation();
        }

        /// <summary>
        /// Get the next permutation in the list.
        /// </summary>
        /// <returns>the next permutation</returns>
        public int[] GetNextPermutation()
        {
            this.currentRank++;
            return this.GetCurrentPermutation();
        }

        /// <summary>
        /// Get the permutation that is currently being used.
        /// </summary>
        /// <returns>the permutation as an int array</returns>
        public int[] GetCurrentPermutation()
        {
            return this.UnrankPermutationLexicographically(currentRank, size);
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
            Array.Copy(permutation, 0, counter, 1, n);
            for (int j = 1; j <= n; j++)
            {
                rank = rank + ((counter[j] - 1) * Factorial(n - j));
                for (int i = j + 1; i < n; i++)
                {
                    if (counter[i] > counter[j])
                    {
                        counter[i]--;
                    }
                }
            }
            return rank;
        }

        /// <summary>
        /// Performs the opposite to the rank method, producing the permutation that
        /// has the order <paramref name="rank"/> in the lexicographically ordered list.
        /// 
        /// As an implementation note, the algorithm assumes that the permutation is
        /// in the form [1,...N] not the more usual [0,...N-1] for a list of size N.
        /// This is why there is the final step of 'shifting' the permutation. The
        /// shift also reduces the numbers by one to make them array indices.
        /// </summary>
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
