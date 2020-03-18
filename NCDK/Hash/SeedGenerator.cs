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
    /// Generate a seed value for each atom of a molecule. The provided <see cref="IAtomEncoder"/> 
    /// is used to encode invariant attributes of the atoms. This value
    /// is then modified by the size of the molecule and pseudorandomly distributed.
    /// The seed values should be used with another <see cref="IAtomHashGenerator"/> which
    /// will differentiate atoms experiencing different environments, such as, <see cref="BasicAtomHashGenerator"/>.
    /// </summary>
    /// <example><code>
    /// // create a new seed generator
    /// AtomEncoder encoder   = ConjugatedAtomEncoder.Create(ATOMIC_NUMBER, MASS_NUMBER);
    /// AtomHashGenerator generator = new SeedGenerator(encoder);
    ///
    /// // generate six hash codes for each atom of benzene
    /// IAtomContainer benzene   = MoleculeFactory.MakeBenzene();
    /// long[]         hashCodes = generator.Generate(benzene);
    /// </code></example>
    /// <seealso cref="BasicAtomHashGenerator"/>
    /// <seealso cref="ConjugatedAtomEncoder"/>
    // @author John May
    // @cdk.module hash
    internal sealed class SeedGenerator : AbstractHashGenerator, IAtomHashGenerator
    {
        /* used to encode atom attributes */
        private readonly IAtomEncoder encoder;

        /// <summary>Optional suppression of atoms.</summary>
        private readonly AtomSuppression suppression;

        /// <summary>
        /// Create a new seed generator using the provided <see cref="IAtomEncoder"/>.
        /// </summary>
        /// <param name="encoder">a method for encoding atom invariant properties</param>
        /// <exception cref="ArgumentNullException">encoder was null</exception>
        /// <seealso cref="ConjugatedAtomEncoder"/>
        public SeedGenerator(IAtomEncoder encoder)
                : this(encoder, new Xorshift(), AtomSuppression.Unsuppressed)
        { }

        /// <summary>
        /// Create a new seed generator using the provided <see cref="IAtomEncoder"/>.
        /// </summary>
        /// <param name="encoder">a method for encoding atom invariant properties</param>
        /// <param name="suppression"></param>
        /// <exception cref="ArgumentNullException">encoder was null</exception>
        /// <seealso cref="ConjugatedAtomEncoder"/>
        public SeedGenerator(IAtomEncoder encoder, AtomSuppression suppression)
            : this(encoder, new Xorshift(), suppression)
        { }

        /// <summary>
        /// Create a new seed generator using the provided <see cref="IAtomEncoder"/> and
        /// pseudorandom number generator.
        /// </summary>
        /// <param name="encoder">a method for encoding atom invariant properties</param>
        /// <param name="pseudorandom">number generator to randomise initial invariants</param>
        /// <param name="suppression">indicates which vertices should be suppressed</param>
        /// <exception cref="ArgumentNullException">encoder or pseudorandom number generator was null</exception>
        public SeedGenerator(IAtomEncoder encoder, Pseudorandom pseudorandom, AtomSuppression suppression)
            : base(pseudorandom)
        {
            this.encoder = encoder ?? throw new ArgumentNullException(nameof(encoder), "encoder cannot be null");
            this.suppression = suppression ?? throw new ArgumentNullException(nameof(suppression), "suppression cannot be null, use AtomSuppression.Unsuppressed()");
        }

        public long[] Generate(IAtomContainer container)
        {
            Suppressed suppressed = suppression.Suppress(container);

            int n = container.Atoms.Count;
            int m = n - suppressed.Count; // number of non-suppressed vertices
            int seed = m > 1 ? 9803 % m : 1;

            long[] hashes = new long[n];

            for (int i = 0; i < n; i++)
            {
                hashes[i] = Distribute(seed * encoder.Encode(container.Atoms[i], container));
            }
            return hashes;
        }
    }
}
