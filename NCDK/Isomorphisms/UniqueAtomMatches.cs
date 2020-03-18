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
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// A predicate for filtering atom-mapping results. This class is intended for
    /// use with <see cref="Pattern"/>.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.UniqueAtomMatches_Example.cs"]/*' />
    /// </example>
    // @author John May
    // @cdk.module isomorphism
    internal sealed class UniqueAtomMatches
    {
        /// <summary>Which mappings have we seen already.</summary>
        private readonly HashSet<BitArray> unique;

        /// <summary>
        /// Create filter for the expected number of unique matches. The number
        /// of matches can grow if required.
        /// </summary>
        /// <param name="expectedHits">expected number of unique matches</param>
        private UniqueAtomMatches(int expectedHits)
        {
            this.unique = new HashSet<BitArray>(BitArrays.EqualityComparer);
        }

        /// <summary>
        /// Create filter for unique matches.
        /// </summary>
        public UniqueAtomMatches()
            : this(10)
        {
        }

        /// <inheritdoc/> 
        public bool Apply(int[] input)
        {
            return unique.Add(ToBitArray(input));
        }

        /// <summary>
        /// Convert a mapping to a bitset.
        /// </summary>
        /// <param name="mapping">an atom mapping</param>
        /// <returns>a bit set of the mapped vertices (values in array)</returns>
        private static BitArray ToBitArray(int[] mapping)
        {
            var hits = new BitArray(0);
            foreach (var v in mapping)
                BitArrays.SetValue(hits, v, true);
            return hits;
        }
    }
}
