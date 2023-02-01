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

using NCDK.Graphs;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// Vento-Foggia (VF) state for matching isomorphisms (identity) <token>cdk-cite-Cordella04</token>.</summary>
    /// <remarks>
    /// <note type="note">
    /// No heuristics or sorting are performed as all and
    /// should be checked externally</note>
    /// </remarks>
    // @author John May
    // @cdk.module isomorphism
    internal sealed class VFState : AbstractVFState
    {
        /// <summary>
        /// The query (container1) and target (container2) of the isomorphism
        /// matching.
        /// </summary>
        private readonly IAtomContainer container1, container2;

        /// <summary>
        /// Lookup for the query bonds (bonds1) and target bonds (bonds2) of the
        /// isomorphism matching.
        /// </summary>
        private readonly EdgeToBondMap bonds1, bonds2;

        /// <summary>Defines how atoms are matched.</summary>
        private readonly AtomMatcher atomMatcher;

        /// <summary>Defines how bonds are matched.</summary>
        private readonly BondMatcher bondMatcher;

        /// <summary>
        /// Create a VF state for matching isomorphisms. The query is passed first
        /// and should read as, find container1 in container2.
        /// </summary>
        /// <param name="container1">the molecule to search for (query)</param>
        /// <param name="container2">the molecule to search in (target)</param>
        /// <param name="g1">adjacency list of the query</param>
        /// <param name="g2">adjacency list of the target</param>
        /// <param name="bonds1">bond lookup of the query</param>
        /// <param name="bonds2">bond lookup of the target</param>
        /// <param name="atomMatcher">what semantic attributes (symbol, charge, query) determines atoms to be compatible</param>
        /// <param name="bondMatcher">what semantic attributes (order/aromatic, query) determines bonds to be compatible</param>
        public VFState(IAtomContainer container1, IAtomContainer container2, int[][] g1, int[][] g2, EdgeToBondMap bonds1,
                EdgeToBondMap bonds2, AtomMatcher atomMatcher, BondMatcher bondMatcher)
            : base(g1, g2)
        {
            this.container1 = container1;
            this.container2 = container2;
            this.bonds1 = bonds1;
            this.bonds2 = bonds2;
            this.atomMatcher = atomMatcher;
            this.bondMatcher = bondMatcher;
        }

        /// <summary>
        /// Check the feasibility of the candidate pair {n, m}. A candidate pair is
        /// syntactically feasible iff all k-look-ahead rules hold. These look ahead
        /// rules check adjacency relation of the mapping. If an edge is mapped in g1
        /// it should also be mapped in g2 and vise-versa (0-look-ahead). If an edge
        /// in g1 is unmapped but the edge is adjacent to an another mapped vertex
        /// (terminal) then the number of such edges should be equal in g1 compared
        /// to g2 (1-look-ahead). If the edge is unmapped and non-terminal then the
        /// number of such edges should be equal in g1 compared to g2 (2-look-ahead).
        /// </summary>
        /// <param name="n">a candidate vertex from g1</param>
        /// <param name="m">a candidate vertex from g2</param>
        /// <returns>the mapping is feasible</returns>
        public override bool Feasible(int n, int m)
        {
            // verify atom semantic feasibility
            if (!atomMatcher.Matches(container1.Atoms[n], container2.Atoms[m])) return false;

            // unmapped terminal vertices n and m are adjacent to
            int nTerminal1 = 0, nTerminal2 = 0;
            // unmapped non-terminal (remaining) vertices n and m are adjacent to
            int nRemain1 = 0, nRemain2 = 0;

            // 0-look-ahead: check each adjacent edge for being mapped, and count
            // terminal or remaining
            foreach (var n_prime in g1[n])
            {
                int m_prime = m1[n_prime];

                // v is already mapped, there should be an edge {m, w} in g2.
                if (m_prime != UNMAPPED)
                {
                    IBond bond2 = bonds2[m, m_prime];
                    // the bond is not present in the target
                    if (bond2 == null) return false;
                    // verify bond semantic feasibility
                    if (!bondMatcher.Matches(bonds1[n, n_prime], bond2)) return false;
                }
                else
                {
                    if (t1[n_prime] > 0)
                        nTerminal1++;
                    else
                        nRemain1++;
                }
            }

            // 0-look-ahead: check each adjacent edge for being mapped, and count
            // terminal or remaining
            foreach (var m_prime in g2[m])
            {
                int n_prime = m2[m_prime];

                if (n_prime != UNMAPPED)
                {
                    IBond bond1 = bonds1[n, n_prime];
                    // the bond is not present in the query
                    if (bond1 == null) return false;
                    // verify bond semantic feasibility
                    if (!bondMatcher.Matches(bond1, bonds2[m, m_prime])) return false;
                }
                else
                {
                    if (t2[m_prime] > 0)
                        nTerminal2++;
                    else
                        nRemain2++;
                }
            }

            // 1-look-ahead : the mapping {n, m} is feasible iff the number of
            // terminal vertices (t1) adjacent to n is equal to the
            // number of terminal vertices (t2) adjacent to m.
            //
            // 2-look-ahead: the mapping {n, m} is feasible iff the number of
            // vertices adjacent to n that are neither in m1 or t1 is equal to
            // the number of the number of vertices adjacent to m that are neither
            // in m2 or t2.
            return nTerminal1 == nTerminal2 && nRemain1 == nRemain2;
        }
    }
}
