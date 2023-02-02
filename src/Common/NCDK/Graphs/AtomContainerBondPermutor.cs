/* Copyright (C) 2009  Gilleain Torrance <gilleain.torrance@gmail.com>
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

namespace NCDK.Graphs
{
    /// <summary>
    /// This class allows the user to iterate through the set of all possible
    /// permutations of the bond order in a given atom container. This provides a
    /// means to check the dependency of an algorithm's results on the bond order of
    /// the input atom container.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.AtomContainerBondPermutor_Example.cs"]/*' />
    /// </example>
    // @author         maclean
    // @cdk.created    2009-09-09
    // @cdk.keyword    permutation
    // @cdk.module     standard
    public class AtomContainerBondPermutor : AtomContainerPermutor
    {
        /// <summary>
        /// A permutor wraps the original atom container, and produces cloned
        /// (and permuted!) copies on demand.
        /// </summary>
        /// <param name="atomContainer">the atom container to permute</param>
        public AtomContainerBondPermutor(IAtomContainer atomContainer)
            : base(atomContainer.Bonds.Count, atomContainer)
        {
        }
        
        /// <seealso cref="AtomContainerPermutor.ContainerFromPermutation(int[])"/>
        public override IAtomContainer ContainerFromPermutation(int[] permutation)
        {
            IAtomContainer permutedContainer = (IAtomContainer)base.AtomContainer.Clone();
            int n = permutedContainer.Bonds.Count;
            IBond[] permutedBonds = new IBond[n];
            for (int i = 0; i < n; i++)
            {
                permutedBonds[i] = permutedContainer.Bonds[permutation[i]];
            }
            permutedContainer.Bonds.Clear();
            foreach (var bond in permutedBonds)
                permutedContainer.Bonds.Add(bond);
            return permutedContainer;
        }
    }
}
