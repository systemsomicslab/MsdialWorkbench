/*
 * Copyright (C) 2012   Syed Asad Rahman <asad@ebi.ac.uk>
 *               2013   John May         <jwmay@users.sf.net>
 *
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;
using NCDK.Graphs;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace NCDK.Fingerprints
{
    // @author Syed Asad Rahman (2012)
    // @author John May (2013)
    // @cdk.keyword fingerprint
    // @cdk.keyword similarity
    // @cdk.module fingerprint
    public sealed class ShortestPathWalker
    {
        /* container which is being traversed */
        private readonly IAtomContainer container;

        /* set of encoded atom paths */
        private readonly List<string> paths;

        /* maximum number of shortest paths, when there is more then one path */
        private const int MAX_SHORTEST_PATHS = 5;

        /// <summary>
        /// Create a new shortest path walker for a given container.
        /// </summary>
        /// <param name="container">the molecule to encode the shortest paths</param>
        public ShortestPathWalker(IAtomContainer container)
        {
            this.container = container;
            this.paths = Traverse();
        }

        /// <summary>
        /// Access a set of all shortest paths.
        /// </summary>
        /// <returns>the paths</returns>
        public IReadOnlyList<string> GetPaths()
        {
            return paths;
        }

        /// <summary>
        /// Traverse all-pairs of shortest-paths within a chemical graph.
        /// </summary>
        private List<string> Traverse()
        {
            var paths = new SortedSet<string>();

            // All-Pairs Shortest-Paths (APSP)
            var apsp = new AllPairsShortestPaths(container);

            for (int i = 0, n = container.Atoms.Count; i < n; i++)
            {
                paths.Add(ToAtomPattern(container.Atoms[i]));

                // only do the comparison for i,j then reverse the path for j,i
                for (int j = i + 1; j < n; j++)
                {
                    int nPaths = apsp.From(i).GetNPathsTo(j);

                    // only encode when there is a manageable number of paths
                    if (nPaths > 0 && nPaths < MAX_SHORTEST_PATHS)
                    {
                        foreach (var path in apsp.From(i).GetPathsTo(j))
                        {
                            paths.Add(Encode(path));
                            paths.Add(Encode(Reverse(path)));
                        }
                    }
                }
            }

            return paths.ToList();
        }

        /// <summary>
        /// Reverse an array of integers.
        /// </summary>
        /// <param name="src">array to reverse</param>
        /// <returns>reversed copy of <paramref name="src"/></returns>
        private static int[] Reverse(int[] src)
        {
            var dest = Arrays.CopyOf(src, src.Length);
            int left = 0;
            int right = src.Length - 1;

            while (left < right)
            {
                // swap the values at the left and right indices
                dest[left] = src[right];
                dest[right] = src[left];

                // move the left and right index pointers in toward the center
                left++;
                right--;
            }
            return dest;
        }

        /// <summary>
        /// Encode the provided path of atoms to a string.
        /// </summary>
        /// <param name="path">inclusive array of vertex indices</param>
        /// <returns>encoded path</returns>
        private string Encode(int[] path)
        {
            var sb = new StringBuilder(path.Length * 3);
            for (int i = 0, n = path.Length - 1; i <= n; i++)
            {
                var atom = container.Atoms[path[i]];
                sb.Append(ToAtomPattern(atom));

                // if we are not at the last index, add the connecting bond
                if (i < n)
                {
                    var bond = container.GetBond(container.Atoms[path[i]], container.Atoms[path[i + 1]]);
                    sb.Append(GetBondSymbol(bond));
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Convert an atom to a string representation. Currently this method just
        /// returns the symbol but in future may include other properties, such as, stereo
        /// descriptor and charge.
        /// </summary>
        /// <param name="atom">The atom to encode</param>
        /// <returns>encoded atom</returns>
        private static string ToAtomPattern(IAtom atom)
        {
            return atom.Symbol;
        }

        /// <summary>
        /// Gets the bondSymbol attribute of the HashedFingerprinter class
        /// </summary>
        /// <param name="bond">Description of the Parameter</param>
        /// <returns>The bondSymbol value</returns>
        private static char GetBondSymbol(IBond bond)
        {
            if (IsSP2Bond(bond))
            {
                return '@';
            }
            else
            {
                switch (bond.Order.Numeric())
                {
                    case 1:
                        return '1';
                    case 2:
                        return '2';
                    case 3:
                        return '3';
                    case 4:
                        return '4';
                    default:
                        return '5';
                }
            }
        }

        /// <summary>
        /// Returns true if the bond binds two atoms, and both atoms are SP2 in a ring system.
        /// </summary>
        private static bool IsSP2Bond(IBond bond)
        {
            return bond.IsAromatic;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            var n = this.paths.Count();
            var paths = this.paths.ToArray();
            var sb = new StringBuilder(n * 5);

            for (int i = 0, last = n - 1; i < n; i++)
            {
                sb.Append(paths[i]);
                if (i != last) sb.Append("->");
            }

            return sb.ToString();
        }
    }
}
