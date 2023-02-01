/*  Copyright (C) 2005-2007  Christian Hoppe <chhoppe@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Graphs;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Counts the number of atoms in the longest aliphatic chain.
    /// </summary>
    // @author      chhoppe from EUROSCREEN
    // @author John Mayfield
    // @cdk.created 2006-1-03
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:largestAliphaticChain
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#longestAliphaticChain")]
    public class LongestAliphaticChainDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private readonly bool checkRingSystem;

        /// <param name="checkRingSystem"><see langword="true"/> is the <see cref="IMolecularEntity.IsInRing"/> has to be set</param>
        public LongestAliphaticChainDescriptor(bool checkRingSystem = false)
        {
            this.checkRingSystem = checkRingSystem;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.NumberOfAtoms = value;
            }

            /// <summary>
            /// The number of atoms in the largest chain.
            /// </summary>
            [DescriptorResultProperty("nAtomLAC")]
            public int NumberOfAtoms { get; private set; }

            public int Value => NumberOfAtoms;
        }

        private static bool IsAcyclicCarbon(IAtom atom)
        {
            return atom.AtomicNumber == 6 && !atom.IsInRing;
        }

        /// <summary>
        /// Depth-First-Search on an acyclic graph. Since we have no cycles we
        /// don't need the visit flags and only need to know which atom we came from.
        /// </summary>
        /// <param name="adjlist">adjacency list representation of grah</param>
        /// <param name="v">the current atom index</param>
        /// <param name="prev">the previous atom index</param>
        /// <returns>the max length traversed</returns>
        private static int GetMaxDepth(int[][] adjlist, int v, int prev)
        {
            int longest = 0;
            foreach (var w in adjlist[v])
            {
                if (w == prev)
                    continue;
                // no cycles so don't need to check previous
                int length = GetMaxDepth(adjlist, w, v);
                if (length > longest)
                    longest = length;
            }
            return 1 + longest;
        }

        /// <summary>
        /// Calculate the count of atoms of the longest aliphatic chain in the supplied <see cref="IAtomContainer"/>.
        /// </summary>
        /// <remarks>
        /// The method require one parameter:
        /// if checkRingSyste is true the <see cref="IMolecularEntity.IsInRing"/> will be set
        /// </remarks>
        /// <returns>the number of atoms in the longest aliphatic chain of this AtomContainer</returns>
        public Result Calculate(IAtomContainer container)
        {
            if (checkRingSystem)
            {
                container = (IAtomContainer)container.Clone();
                Cycles.MarkRingAtomsAndBonds(container);
            }

            var aliphaticParts = CDK.Builder.NewAtomContainer();
            foreach (var atom in container.Atoms)
            {
                if (IsAcyclicCarbon(atom))
                    aliphaticParts.Atoms.Add(atom);
            }
            foreach (var bond in container.Bonds)
            {
                if (IsAcyclicCarbon(bond.Begin)
                 && IsAcyclicCarbon(bond.End))
                    aliphaticParts.Bonds.Add(bond);
            }

            int longest = 0;
            var adjlist = GraphUtil.ToAdjList(aliphaticParts);
            for (int i = 0; i < adjlist.Length; i++)
            {
                // atom deg > 1 can't find the longest chain
                if (adjlist[i].Length != 1)
                    continue;
                int length = GetMaxDepth(adjlist, i, -1);
                if (length > longest)
                    longest = length;
            }

            return new Result(longest);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
