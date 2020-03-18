/* Copyright (C) 2017  Gilleain Torrance <gilleain.torrance@gmail.com>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

namespace NCDK.Groups
{
    /// <summary>
    /// Base class for discrete partition refiners of IAtomContainers.
    /// </summary>
    // @author maclean
    // @cdk.module group  
    internal abstract class AtomContainerDiscretePartitionRefinerImpl
       : AbstractDiscretePartitionRefiner, IAtomContainerDiscretePartitionRefiner
    {
        private IRefinable refinable;

        /// <summary>
        /// Refine an atom container, which has the side effect of calculating
        /// the automorphism group.
        /// 
        /// If the group is needed afterwards, call <see cref="IDiscretePartitionRefiner.GetAutomorphismGroup()"/>
        /// instead of <see cref="GetAutomorphismGroup(IAtomContainer)"/> otherwise the
        /// refine method will be called twice.
        /// </summary>
        /// <param name="atomContainer">the atomContainer to refine</param>
        public void Refine(IAtomContainer atomContainer)
        {
            Refine(atomContainer, GetRefinable(atomContainer).GetInitialPartition());
        }

        /// <summary>
        /// Refine an atom partition based on the connectivity in the atom container.
        /// </summary>
        /// <param name="atomContainer">the atom container to use</param>
        /// <param name="partition">the initial partition of the atoms</param>
        public void Refine(IAtomContainer atomContainer, Partition partition)
        {
            Setup(atomContainer);
            base.Refine(partition);
        }

        /// <summary>
        /// Checks if the atom container is canonical.</summary>
        /// <remarks>
        /// <note type="note">
        /// This calls <see cref="Refine(IAtomContainer)"/> first.
        /// </note></remarks>
        /// <param name="atomContainer">the atom container to check</param>
        /// <returns>true if the atom container is canonical</returns>
        public bool IsCanonical(IAtomContainer atomContainer)
        {
            Setup(atomContainer);
            base.Refine(refinable.GetInitialPartition());
            return IsCanonical();
        }

        /// <summary>
        /// Gets the automorphism group of the atom container. By default it uses an
        /// initial partition based on the element symbols (so all the carbons are in
        /// one cell, all the nitrogens in another, etc). If this behaviour is not
        /// desired, then use the <see cref="AtomDiscretePartitionRefiner.ignoreElements"/> flag in the constructor.
        /// </summary>
        /// <param name="atomContainer">the atom container to use</param>
        /// <returns>the automorphism group of the atom container</returns>
        public PermutationGroup GetAutomorphismGroup(IAtomContainer atomContainer)
        {
            Setup(atomContainer);
            base.Refine(refinable.GetInitialPartition());
            return base.GetAutomorphismGroup();
        }

        /// <summary>
        /// Speed up the search for the automorphism group using the automorphisms in
        /// the supplied group. Note that the behaviour of this method is unknown if
        /// the group does not contain automorphisms...
        /// </summary>
        /// <param name="atomContainer">the atom container to use</param>
        /// <param name="group">the group of known automorphisms</param>
        /// <returns>the full automorphism group</returns>
        public PermutationGroup GetAutomorphismGroup(IAtomContainer atomContainer, PermutationGroup group)
        {
            Setup(atomContainer, group);
            base.Refine(refinable.GetInitialPartition());
            return base.GetAutomorphismGroup();
        }

        /// <summary>
        /// Get the automorphism group of the molecule given an initial partition.
        /// </summary>
        /// <param name="atomContainer">the atom container to use</param>
        /// <param name="initialPartition">an initial partition of the atoms</param>
        /// <returns>the automorphism group starting with this partition</returns>
        public PermutationGroup GetAutomorphismGroup(IAtomContainer atomContainer, Partition initialPartition)
        {
            Setup(atomContainer);
            base.Refine(initialPartition);
            return base.GetAutomorphismGroup();
        }

        /// <summary>
        /// Get the automorphism partition (equivalence classes) of the atoms.
        /// </summary>
        /// <param name="atomContainer">the molecule to calculate equivalence classes for</param>
        /// <returns>a partition of the atoms into equivalence classes</returns>
        public Partition GetAutomorphismPartition(IAtomContainer atomContainer)
        {
            Setup(atomContainer);
            base.Refine(refinable.GetInitialPartition());
            return base.GetAutomorphismPartition();
        }

        protected abstract IRefinable CreateRefinable(IAtomContainer atomContainer);

        private IRefinable GetRefinable(IAtomContainer atomContainer)
        {
            refinable = CreateRefinable(atomContainer);
            return refinable;
        }

        /// <inheritdoc/>
        protected internal override int GetVertexCount()
        {
            return refinable.GetVertexCount();
        }

        /// <inheritdoc/>
        protected internal override int GetConnectivity(int vertexI, int vertexJ)
        {
            return refinable.GetConnectivity(vertexI, vertexJ);
        }

        private void Setup(IAtomContainer atomContainer)
        {
            // have to setup the connection table before making the group
            // otherwise the size may be wrong, but only setup if it doesn't exist
            IRefinable refinable = GetRefinable(atomContainer);

            int size = GetVertexCount();
            PermutationGroup group = new PermutationGroup(new Permutation(size));
            base.Setup(group, new EquitablePartitionRefiner(refinable));
        }

        private void Setup(IAtomContainer atomContainer, PermutationGroup group)
        {
            base.Setup(group, new EquitablePartitionRefiner(GetRefinable(atomContainer)));
        }
    }
}
