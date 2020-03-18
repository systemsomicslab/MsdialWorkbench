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

using NCDK.Hash.Stereo;
using System;

namespace NCDK.Hash
{
    /// <summary>
    /// A generator for atom hash codes where atoms maybe be <i>suppressed</i>. A
    /// common usage would be compute the hash code for a molecule with explicit
    /// hydrogens but ignore any values for the explicit hydrogens. This particularly
    /// useful for stereo-centres where by removing explicit hydrogens could affect
    /// the configuration.
    /// </summary>
    /// <remarks>
    /// The suppress atom hashes are returned as '0'.
    /// </remarks>
    /// <seealso cref="SeedGenerator"/>
    // @author John May
    // @cdk.module hash
    internal sealed class SuppressedAtomHashGenerator : AbstractAtomHashGenerator, IAtomHashGenerator
    {
        /* a generator for the initial atom seeds */
        private readonly IAtomHashGenerator seedGenerator;

        /* creates stereo encoders for IAtomContainers */
        private readonly IStereoEncoderFactory factory;

        /* number of cycles to include adjacent invariants */
        private readonly int depth;

        /// <summary>
        /// Function used to indicate which atoms should be suppressed. One can think
        /// of this as 'masking' out a value.
        /// </summary>
        private readonly AtomSuppression suppression;

        /// <summary>
        /// Create a basic hash generator using the provided seed generator to
        /// initialise atom invariants and using the provided stereo factory.
        /// </summary>
        /// <param name="seedGenerator">generator to seed the initial values of atoms</param>
        /// <param name="pseudorandom">pseudorandom number generator used to randomise hash distribution</param>
        /// <param name="factory">a stereo encoder factory</param>
        /// <param name="suppression">defines which atoms are suppressed - that is masked from the hash</param>
        /// <param name="depth">depth of the hashing function, larger values take longer</param>
        /// <exception cref="ArgumentException">depth was less then 0</exception>
        /// <exception cref="ArgumentNullException">    seed generator or pseudo random was null</exception>
        /// <seealso cref="SeedGenerator"/>
        public SuppressedAtomHashGenerator(
            IAtomHashGenerator seedGenerator, Pseudorandom pseudorandom,
            IStereoEncoderFactory factory, AtomSuppression suppression, int depth)
            : base(pseudorandom)
        {
            if (depth < 0)
                throw new ArgumentOutOfRangeException(nameof(depth), "depth cannot be less then 0");
            this.seedGenerator = seedGenerator ?? throw new ArgumentNullException(nameof(seedGenerator), "seed generator cannot be null");
            this.factory = factory;
            this.suppression = suppression;
            this.depth = depth;
        }

        /// <summary>
        /// Create a basic hash generator using the provided seed generator to
        /// initialise atom invariants and no stereo configuration.
        /// </summary>
        /// <param name="seedGenerator">generator to seed the initial values of atoms</param>
        /// <param name="pseudorandom">pseudorandom number generator used to randomise hash distribution</param>
        /// <param name="suppression">defines which atoms are suppressed (i.e. masked) from the hash code</param>
        /// <param name="depth">depth of the hashing function, larger values take longer</param>
        /// <exception cref="ArgumentException">depth was less then 0</exception>
        /// <exception cref="ArgumentNullException">seed generator or pseudo random was null</exception>
        /// <seealso cref="SeedGenerator"/>
        public SuppressedAtomHashGenerator(IAtomHashGenerator seedGenerator, Pseudorandom pseudorandom,
                AtomSuppression suppression, int depth)
                : this(seedGenerator, pseudorandom, StereoEncoderFactory.Empty, suppression, depth)
        {
        }

        public override long[] Generate(IAtomContainer container)
        {
            int[][] graph = ToAdjList(container);
            Suppressed suppressed = suppression.Suppress(container);
            return Generate(seedGenerator.Generate(container), factory.Create(container, graph), graph, suppressed);
        }

        /// <summary>
        /// Package-private method for generating the hash for the given molecule.
        /// The initial invariants are passed as to the method along with an
        /// adjacency list representation of the graph.
        /// </summary>
        /// <param name="current">initial invariants</param>
        /// <param name="encoder"></param>
        /// <param name="graph">adjacency list representation</param>
        /// <param name="suppressed"></param>
        /// <returns>hash codes for atoms</returns>
        public override long[] Generate(long[] current, IStereoEncoder encoder, int[][] graph, Suppressed suppressed)
        {
            // for the stereo perception depending on how the
            // (BasicPermutationParity) is done we need to set the value to be as
            // high (or low) as possible
            foreach (var i in suppressed.ToArray())
            {
                current[i] = long.MaxValue;
            }

            int n = graph.Length;
            long[] next = Copy(current);

            // buffers for including adjacent invariants
            long[] unique = new long[n];
            long[] included = new long[n];

            while (encoder.Encode(current, next))
            {
                Copy(next, current);
            }

            for (int d = 0; d < depth; d++)
            {
                for (int v = 0; v < n; v++)
                {
                    next[v] = Next(graph, v, current, unique, included, suppressed);
                }

                Copy(next, current);

                while (encoder.Encode(current, next))
                {
                    Copy(next, current);
                }
            }

            // zero all suppressed values so they are not combined in any molecule
            // hash
            foreach (var i in suppressed.ToArray())
            {
                current[i] = 0L;
            }

            return current;
        }

        /// <summary>
        /// Determine the next value of the atom at index <paramref name="v"/>. The value is
        /// calculated by combining the current values of adjacent atoms. When a
        /// duplicate value is found it can not be directly included and is
        /// <i>rotated</i> the number of times it has previously been seen.
        /// </summary>
        /// <param name="graph">adjacency list representation of connected atoms</param>
        /// <param name="v">the atom to calculate the next value for</param>
        /// <param name="current">the current values</param>
        /// <param name="unique">buffer for working out which adjacent values are unique</param>
        /// <param name="included">buffer for storing the rotated <i>unique</i> value, this value is <i>rotated</i> each time the same value is found.</param>
        /// <param name="suppressed">bit set indicates which atoms are 'suppressed'</param>
        /// <returns>the next value for <paramref name="v"/></returns>
        internal long Next(int[][] graph, int v, long[] current, long[] unique, long[] included, Suppressed suppressed)
        {
            if (suppressed.Contains(v)) return current[v];

            long invariant = Distribute(current[v]);
            int nUnique = 0;

            foreach (var w in graph[v])
            {

                // skip suppressed atom
                if (suppressed.Contains(w)) continue;

                long adjInv = current[w];

                // find index of already included neighbor
                int i = 0;
                while (i < nUnique && unique[i] != adjInv)
                {
                    ++i;
                }

                // no match, then the value is unique, use adjInv
                // match, then rotate the previously included value
                included[i] = (i == nUnique) ? unique[nUnique++] = adjInv : Rotate(included[i]);

                invariant ^= included[i];
            }

            return invariant;
        }
    }
}
