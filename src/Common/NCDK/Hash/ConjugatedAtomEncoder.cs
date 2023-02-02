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
using System.Collections.Generic;

namespace NCDK.Hash
{
    /// <summary>
    /// An atom encoder which takes several atom encodes and combines the encodings
    /// into a single encoder. The order of the encoders matter and for persistent
    /// results should be ordered before construction.
    /// </summary>
    /// <example><code>
    /// // import org.openscience.cdk.hash.seed.BasicAtomEncoder.*
    /// AtomEncoder encoder = new ConjugatedAtomEncoder(Arrays.asList(ATOMIC_NUMBER, FORMAL_CHARGE));
    ///
    /// // convenience constructor using var-args
    /// AtomEncoder encoder = ConjugatedAtomEncoder.Create(ATOMIC_NUMBER, FORMAL_CHARGE);
    ///
    /// // specifying a custom encoder
    /// AtomEncoder encoder =
    ///   ConjugatedAtomEncoder.Create(ATOMIC_NUMBER, FORMAL_CHARGE,
    ///                                new AtomEncoder(){
    ///                                  public int Encode(IAtom a, IAtomContainer c){
    ///                                    return a.Symbol.HashCode();
    ///                                  }
    ///                                });
    /// </code></example>
    // @author John May
    // @cdk.module hash
    internal sealed class ConjugatedAtomEncoder : IAtomEncoder
    {
        /* ordered list of encoders */
        private readonly List<IAtomEncoder> encoders;

        /// <summary>
        /// Create a new conjugated encoder for the specified list of atom encoders.
        /// The encoders are combined in an order dependant manner.
        /// </summary>
        /// <param name="encoders">non-empty list of encoders</param>
        /// <exception cref="ArgumentNullException">the list of encoders was null</exception>
        /// <exception cref="ArgumentException">the list of encoders was empty</exception>
        public ConjugatedAtomEncoder(IEnumerable<IAtomEncoder> encoders)
        {
            if (encoders == null)
                throw new ArgumentNullException(nameof(encoders), "null list of encoders");
            this.encoders = new List<IAtomEncoder>(encoders);
            if (this.encoders.Count == 0)
                throw new ArgumentException("no encoders provided");
        }

        public int Encode(IAtom atom, IAtomContainer container)
        {
            int hash = 179426549;
            foreach (var encoder in encoders)
                hash = 31 * hash + encoder.Encode(atom, container);
            return hash;
        }

        /// <summary>
        /// Convenience method for creating a conjugated encoder from one or more
        /// <see cref="IAtomEncoder"/>s.
        /// </summary>
        /// <example><code>
        /// // import org.openscience.cdk.hash.seed.BasicAtomEncoder.*
        /// AtomEncoder encoder = ConjugatedAtomEncoder.Create(ATOMIC_NUMBER, FORMAL_CHARGE);
        /// </code></example>
        /// <param name="encoder">the first encoder</param>
        /// <param name="encoders">the other encoders</param>
        /// <returns>a new conjugated encoder</returns>
        /// <exception cref="ArgumentNullException">either argument was null</exception>
        public static IAtomEncoder Create(IAtomEncoder encoder, params IAtomEncoder[] encoders)
        {
            if (encoder == null)
                throw new ArgumentNullException(nameof(encoder), "null encoder provided");
            if (encoders == null)
                throw new ArgumentNullException(nameof(encoders), "null encoders provided");
            var tmp = new List<IAtomEncoder>(encoders.Length + 1) { encoder };
            tmp.AddRange(encoders);
            return new ConjugatedAtomEncoder(tmp);
        }
    }
}
