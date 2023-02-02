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
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Hash.Stereo;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Hash
{
    /// <summary>
    /// Defines an internal super-class for AtomHashGenerators. The single required
    /// method allows atom hash generators to either ignore 'suppressed' atoms or use
    /// the information.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal abstract class AbstractAtomHashGenerator : AbstractHashGenerator, IAtomHashGenerator
    {
        public AbstractAtomHashGenerator(Pseudorandom pseudorandom)
            : base(pseudorandom)
        {
        }

        public abstract long[] Generate(IAtomContainer container);

        /// <summary>
        /// Internal method invoked by 'molecule' hash generators.
        /// </summary>
        /// <param name="current">the current invariants</param>
        /// <param name="encoder">encoder used for encoding stereo-chemistry</param>
        /// <param name="graph">adjacency list representation of the molecule</param>
        /// <param name="suppressed">bit set marks vertices which are 'suppressed' (may be ignored)</param>
        /// <returns>the atom hash values</returns>
        public abstract long[] Generate(long[] current, IStereoEncoder encoder, int[][] graph, Suppressed suppressed);
    }
}
