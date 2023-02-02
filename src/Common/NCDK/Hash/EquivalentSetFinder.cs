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

using System.Collections.Generic;

namespace NCDK.Hash
{
    /// <summary>
    /// Describes a function which identifies a set of equivalent atoms base on the
    /// provided invariants. Given some other pre-conditions this set is filtered
    /// down and an array of length 0 to n is returned. It is important to note that
    /// the atoms may not actually be equivalent and are only equivalent by the
    /// provided invariants. An example of a pre-condition could be that we only
    /// return the vertices which are present in rings (cyclic). This condition
    /// removes all terminal atoms which although equivalent are not relevant.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal abstract class EquivalentSetFinder
    {
        /// <summary>
        /// Find a set of equivalent vertices (atoms) and return this set as an array of indices.
        /// </summary>
        /// <param name="invariants">the values for each vertex</param>
        /// <param name="container">the molecule which which the graph is based on</param>
        /// <param name="graph">adjacency list representation of the graph</param>
        /// <returns>set of equivalent vertices</returns>
        public abstract ISet<int> Find(long[] invariants, IAtomContainer container, int[][] graph);
    }
}
