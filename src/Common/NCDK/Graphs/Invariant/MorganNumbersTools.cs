/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *                    2011  Thorsten Flügel <thorsten.fluegel@tu-dortmund.de>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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
using NCDK.Common.Primitives;
using System;

namespace NCDK.Graphs.Invariant
{
    /// <summary>
    /// Compute the extended connectivity values (Morgan Numbers) <token>cdk-cite-MOR65</token>.
    /// The tool does not produce the lexicographic smallest labelling on the graph
    /// and should not be used as a robust canonical labelling tool. To canonical
    /// label a graph please use <see cref="InChINumbersTools"/> or <see cref="CanonicalLabeler"/>.
    /// To determine equivalent classes of atoms please use <see cref="HuLuIndexTool"/>
    /// or one of the discrete refines available in the 'cdk-group'
    /// module.
    /// </summary>
    /// <seealso cref="InChINumbersTools"/>
    /// <seealso cref="CanonicalLabeler"/>
    /// <seealso cref="HuLuIndexTool"/>
    // @author shk3
    // @cdk.module standard
    // @cdk.created 2003-06-30
    // @cdk.keyword Morgan number
    public static class MorganNumbersTools
    {
        /// <summary>Default size of adjacency lists.</summary>
        private const int InitialDegree = 4;

        /// <summary>
        /// Makes an array containing the morgan numbers of the atoms of
        /// atomContainer. These number are the extended connectivity values and not
        /// the lexicographic smallest labelling on the graph.
        /// </summary>
        /// <param name="molecule">the molecule to analyse.</param>
        /// <returns>The morgan numbers value.</returns>
        public static long[] GetMorganNumbers(IAtomContainer molecule)
        {
            int order = molecule.Atoms.Count;

            long[] currentInvariants = new long[order];
            long[] previousInvariants = new long[order];

            int[][] graph = Arrays.CreateJagged<int>(order, InitialDegree);
            int[] degree = new int[order];

            // which atoms are the non-hydrogens.
            int[] nonHydrogens = new int[order];

            for (int v = 0; v < order; v++)
                nonHydrogens[v] = molecule.Atoms[v].AtomicNumber.Equals(AtomicNumbers.H) ? 0 : 1;

            // build the graph and initialise the current connectivity
            // value to the number of connected non-hydrogens
            foreach (var bond in molecule.Bonds)
            {
                int u = molecule.Atoms.IndexOf(bond.Begin);
                int v = molecule.Atoms.IndexOf(bond.End);
                graph[u] = Ints.EnsureCapacity(graph[u], degree[u] + 1, InitialDegree);
                graph[v] = Ints.EnsureCapacity(graph[v], degree[v] + 1, InitialDegree);
                graph[u][degree[u]++] = v;
                graph[v][degree[v]++] = u;
                currentInvariants[u] += nonHydrogens[v];
                currentInvariants[v] += nonHydrogens[u];
            }

            // iteratively sum the connectivity values for each vertex
            for (int i = 0; i < order; i++)
            {
                Array.Copy(currentInvariants, 0, previousInvariants, 0, order);
                for (int u = 0; u < order; u++)
                {
                    currentInvariants[u] = 0;

                    // for each of the vertices adjacent to <paramref name="u"/> sum their
                    // previous connectivity value
                    int[] neighbors = graph[u];
                    for (int j = 0; j < degree[u]; j++)
                    {
                        int v = neighbors[j];
                        currentInvariants[u] += previousInvariants[v] * nonHydrogens[v];
                    }
                }
            }
            return currentInvariants;
        }

        /// <summary>
        /// Makes an array containing the morgan numbers+element symbol of the atoms
        /// of <paramref name="atomContainer"/>. This method puts the element symbol before the
        /// morgan number, useful for finding out how many different rests are
        /// connected to an atom.
        /// </summary>
        /// <param name="atomContainer">The atomContainer to analyse.</param>
        /// <returns>The morgan numbers value.</returns>
        public static string[] GetMorganNumbersWithElementSymbol(IAtomContainer atomContainer)
        {
            long[] morgannumbers = GetMorganNumbers(atomContainer);
            string[] morgannumberswithelement = new string[morgannumbers.Length];
            for (int i = 0; i < morgannumbers.Length; i++)
            {
                morgannumberswithelement[i] = atomContainer.Atoms[i].Symbol + "-" + morgannumbers[i];
            }
            return (morgannumberswithelement);
        }
    }
}
