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

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// Defines a state for matching (subgraph-)isomorphism from a query graph
    /// (<i>G1</i>) to a target graph (<i>G2</i>). The mutable state allows
    /// generation and adding and removal of mappings. A mapping {n, m} indicates a
    /// query vertex (from <i>G1</i>), n, is paired (mapped) with the target vertex,
    /// m (from <i>G2</i>). Candidate pairs are generated using <see cref="NextN(int)"/> 
    /// and <see cref="NextM(int, int)"/>. Each candidate pair {n, m} is then <see cref="Add(int, int)"/>ed if
    /// the mapping was feasible.
    /// </summary>
    // @author John May
    // @cdk.module isomorphism
    internal abstract class State
    {
        /// <summary>
        /// Given the previous candidate generate the next query candidate. The first
        /// candidate passed is always -1.
        /// </summary>
        /// <param name="n">the previous candidate</param>
        /// <returns>next candidate</returns>
        public abstract int NextN(int n);

        /// <summary>
        /// Given the previous candidate generate the next target candidate. The
        /// first candidate passed is always -1.
        /// </summary>
        /// <param name="n">the current n vertex</param>
        /// <param name="m">the previous candidate</param>
        /// <returns>next candidate</returns>
        public abstract int NextM(int n, int m);

        /// <summary>
        /// The max query candidate (number of vertices in the query).
        /// </summary>
        /// <returns><i>|V| ∈ G1</i></returns>
        public abstract int NMax();

        /// <summary>
        /// The max target candidate (number of vertices in the target).
        /// </summary>
        /// <returns><i>|V| ∈ G2</i></returns>
        public abstract int MMax();

        /// <summary>
        /// Add a mapping between n (a vertex G1) and m (a vertex in G2). If the
        /// mapping was not feasible the mapping is not added.
        /// </summary>
        /// <param name="n">a vertex in G1</param>
        /// <param name="m">a vertex in G2</param>
        /// <returns>the mapping was added</returns>
        public abstract bool Add(int n, int m);

        /// <summary>
        /// Remove a mapping (backtrack) between n (a vertex G1) and m (a vertex in
        /// G2).
        /// </summary>
        /// <param name="n">a vertex in G1</param>
        /// <param name="m">a vertex in G2</param>
        public abstract void Remove(int n, int m);

        /// <summary>
        /// Access a copy of the current mapping.
        /// </summary>
        /// <returns>mapping of vertices from <i>G1</i> to <i>G2</i></returns>
        public abstract int[] Mapping();

        /// <summary>
        /// Current size of the state. If <i>size</i> is the current number of mapped
        /// candidates.
        /// </summary>
        /// <returns>the size of the state</returns>
        public abstract int Count { get; }
    }
}
