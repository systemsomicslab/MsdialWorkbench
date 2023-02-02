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

using System;

namespace NCDK.Hash
{
    /// <summary>
    /// A generator for basic molecule hash codes <token>cdk-cite-Ihlenfeldt93</token>. The
    /// provided <see cref="IAtomHashGenerator"/> is used to produce individual atom hash
    /// codes. These are then combined together in an order independent manner to
    /// generate a single hash code for the molecule.
    /// </summary>
    /// <example><code>
    /// AtomHashGenerator atomGenerator = ...;
    /// MoleculeHashGenerator generator = new BasicMoleculeHashGenerator(atomGenerator)
    ///
    /// IAtomContainer benzene  = MoleculeFactory.MakeBenzene();
    /// long hashCode = generator.Generate(benzene);
    /// </code></example>
    /// <seealso cref="IAtomHashGenerator"/>
    /// <seealso cref="BasicAtomHashGenerator"/>
    // @author John May
    // @cdk.module hash
    internal sealed class BasicMoleculeHashGenerator : IMoleculeHashGenerator
    {
        /* generator for atom hashes */
        private readonly IAtomHashGenerator generator;

        /* pseudorandom number generator */
        private readonly Pseudorandom pseudorandom;

        /// <summary>
        /// Create a new molecule hash using the provided atom hash generator.
        /// </summary>
        /// <param name="generator">a generator for atom hash codes</param>
        /// <exception cref="ArgumentNullException">no generator provided</exception>
        public BasicMoleculeHashGenerator(IAtomHashGenerator generator)
            : this(generator, new Xorshift())
        { }

        /// <summary>
        /// Create a new molecule hash using the provided atom hash generator and
        /// pseudorandom number generator.
        /// </summary>
        /// <param name="generator">a generator for atom hash codes</param>
        /// <param name="pseudorandom">pseudorandom number generator</param>
        /// <exception cref="ArgumentNullException">no atom hash generator or pseudorandom number generator provided</exception>
        internal BasicMoleculeHashGenerator(IAtomHashGenerator generator, Pseudorandom pseudorandom)
        {
            this.generator = generator ?? throw new ArgumentNullException(nameof(generator), "no AtomHashGenerator provided");
            this.pseudorandom = pseudorandom ?? throw new ArgumentNullException(nameof(pseudorandom), "no Pseudorandom number generator provided");
        }

        public long Generate(IAtomContainer container)
        {
            long[] hashes = generator.Generate(container);
            long[] rotated = new long[hashes.Length];

            Array.Sort(hashes);

            // seed with Mersenne prime 2^31-1
            long hash = 2147483647L;

            for (int i = 0; i < hashes.Length; i++)
            {
                // if non-unique, then get the next random number
                if (i > 0 && hashes[i] == hashes[i - 1])
                {
                    hash ^= rotated[i] = pseudorandom.Next(rotated[i - 1]);
                }
                else
                {
                    hash ^= rotated[i] = hashes[i];
                }
            }

            return hash;
        }
    }
}
