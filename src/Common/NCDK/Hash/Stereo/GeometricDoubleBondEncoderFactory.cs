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

using System;
using System.Collections.Generic;
using System.Linq;
using NCDK.Numerics;

namespace NCDK.Hash.Stereo
{
    /// <summary>
    /// A stereo encoder factory encoding double bond configurations by 2D and 3D
    /// coordinates. This factory will attempt to encode all double bonds that meet
    /// the following conditions. Are not "-N=N-" bonds, non-cumulated,
    /// non-query and have each double bonded atom has at least one substituent. In
    /// future the encoding rules may be more strict or even configurable but
    /// currently they may be over zealous when encoding configurations with 3D
    /// coordinates.
    /// <para>This class is intended to be used with a the hash
    /// encoding classes and is easier used via the <see cref="HashGeneratorMaker"/>.</para>
    /// </summary>
    /// <seealso cref="HashGeneratorMaker"/>
    // @author John May
    // @cdk.module hash
    public sealed class GeometricDoubleBondEncoderFactory : IStereoEncoderFactory
    {
        /// <summary>
        /// Create a stereo encoder for all potential 2D and 3D double bond stereo
        /// configurations.
        /// </summary>
        /// <param name="container">an atom container</param>
        /// <param name="graph">adjacency list representation of the container</param>
        /// <returns>a new encoder for tetrahedral elements</returns>
        public IStereoEncoder Create(IAtomContainer container, int[][] graph)
        {
            var encoders = new List<IStereoEncoder>(5);

            foreach (var bond in container.Bonds)
            {

                // if double bond and not E or Z query bond
                if (BondOrder.Double.Equals(bond.Order) && !BondStereo.EOrZ.Equals(bond.Stereo))
                {

                    IAtom left = bond.Begin;
                    IAtom right = bond.End;

                    // skip -N=N- double bonds which exhibit inversion
                    if (7.Equals(left.AtomicNumber)
                            && 7.Equals(right.AtomicNumber)) continue;

                    IStereoEncoder encoder = NewEncoder(container, left, right, right, left, graph);

                    if (encoder != null)
                    {
                        encoders.Add(encoder);
                    }
                }
            }

            return encoders.Count == 0 ? StereoEncoder.Empty : new MultiStereoEncoder(encoders);
        }

        /// <summary>
        /// Create a new encoder for the specified left and right atoms. The parent
        /// is the atom which is connected by a double bond to the left and right
        /// atom. For simple double bonds the parent of each is the other atom, in
        /// cumulenes the parents are not the same.
        /// </summary>
        /// <param name="container">the molecule</param>
        /// <param name="left">the left atom</param>
        /// <param name="leftParent">the left atoms parent (usually <paramref name="right"/>)</param>
        /// <param name="right">the right atom</param>
        /// <param name="rightParent">the right atoms parent (usually <paramref name="left"/>)</param>
        /// <param name="graph">adjacency list representation of the molecule</param>
        /// <returns>a stereo encoder (or null)</returns>
        internal static IStereoEncoder NewEncoder(IAtomContainer container, IAtom left, IAtom leftParent, IAtom right, IAtom rightParent, int[][] graph)
        {
            var leftBonds = container.GetConnectedBonds(left);
            var rightBonds = container.GetConnectedBonds(right);

            // check the left and right bonds are acceptable
            if (Accept(left, leftBonds) && Accept(right, rightBonds))
            {
                int leftIndex = container.Atoms.IndexOf(left);
                int rightIndex = container.Atoms.IndexOf(right);

                int leftParentIndex = container.Atoms.IndexOf(leftParent);
                int rightParentIndex = container.Atoms.IndexOf(rightParent);

                // neighbors of u/v with the bonded atoms (left,right) moved
                // to the back of each array. this is important as we can
                // drop it when we build the permutation parity
                int[] leftNeighbors = MoveToBack(graph[leftIndex], leftParentIndex);
                int[] rightNeighbors = MoveToBack(graph[rightIndex], rightParentIndex);

                int l1 = leftNeighbors[0];
                int l2 = leftNeighbors[1] == leftParentIndex ? leftIndex : leftNeighbors[1];
                int r1 = rightNeighbors[0];
                int r2 = rightNeighbors[1] == rightParentIndex ? rightIndex : rightNeighbors[1];

                // make 2D/3D geometry
                GeometricParity geometric = Geometric(container, leftIndex, rightIndex, l1, l2, r1, r2);

                // geometric is null if there were no coordinates
                if (geometric != null)
                {
                    return new GeometryEncoder(new int[] { leftIndex, rightIndex }, new CombinedPermutationParity(
                            Permutation(leftNeighbors), Permutation(rightNeighbors)), geometric);
                }
            }
            return null;
        }

        /// <summary>
        /// Generate a new geometric parity (2D or 3D) for the given molecule and
        /// atom indices. This method ensure that 2D and 3D coordinates are available
        /// on the specified atoms and returns null if the 2D or 3D coordinates are
        /// not fully available.
        /// </summary>
        /// <param name="mol">a molecule</param>
        /// <param name="l">left double bonded atom</param>
        /// <param name="r">right double bonded atom</param>
        /// <param name="l1">first substituent atom of <i>l</i></param>
        /// <param name="l2">second substituent atom of <i>l</i> or <i>l</i> if there is none</param>
        /// <param name="r1">first substituent atom of <paramref name="r"/></param>
        /// <param name="r2">second substituent atom of <paramref name="r"/> or <paramref name="r"/> if there is none</param>
        /// <returns>geometric parity or null</returns>
        internal static GeometricParity Geometric(IAtomContainer mol, int l, int r, int l1, int l2, int r1, int r2)
        {
            // we need all points for 2D as they may be skewed, i.e.
            //
            // \
            //  C=C
            //    |\
            //    C H
            Vector2? l2d = mol.Atoms[l].Point2D;
            Vector2? r2d = mol.Atoms[r].Point2D;
            Vector2? l12d = mol.Atoms[l1].Point2D;
            Vector2? l22d = mol.Atoms[l2].Point2D;
            Vector2? r12d = mol.Atoms[r1].Point2D;
            Vector2? r22d = mol.Atoms[r2].Point2D;

            if (l2d != null && r2d != null && l12d != null && l22d != null && r12d != null && r22d != null)
            {
                return new DoubleBond2DParity(l2d.Value, r2d.Value, l12d.Value, l22d.Value, r12d.Value, r22d.Value);
            }

            // we only need the first point, we presume the 3D angles are all correct
            Vector3? l3d = mol.Atoms[l].Point3D;
            Vector3? r3d = mol.Atoms[r].Point3D;
            Vector3? l13d = mol.Atoms[l1].Point3D;
            Vector3? r13d = mol.Atoms[r1].Point3D;
            if (l3d != null && r3d != null && l13d != null && r13d != null)
                return new DoubleBond3DParity(l3d.Value, r3d.Value, l13d.Value, r13d.Value);

            return null;
        }

        /// <summary>
        /// Create a permutation parity for the given neighbors. The neighbor list
        /// should include the other double bonded atom but in the last index.
        ///
        /// <pre>
        /// c3
        ///  \
        ///   c2 = c1  = [c3,c4,c1]
        ///  /
        /// c4
        /// </pre>
        /// </summary>
        /// <param name="neighbors">neighbors of a double bonded atom specified by index</param>
        /// <returns>a new permutation parity</returns>
        internal static PermutationParity Permutation(int[] neighbors)
        {
            if (neighbors.Length == 2)
                return PermutationParity.Identity;
            var xNeighbors = new int[neighbors.Length - 1];
            Array.Copy(neighbors, xNeighbors, neighbors.Length - 1);
            return new BasicPermutationParity(xNeighbors);
        }

        /// <summary>
        /// Utility method for shifting a specified value in an index to the back
        /// (see <see cref="Permutation(int[])"/> ).
        /// </summary>
        /// <param name="neighbors">list of neighbors</param>
        /// <param name="v">the value to shift to the back</param>
        /// <returns><i>neighbors</i> array</returns>
        internal static int[] MoveToBack(int[] neighbors, int v)
        {
            int j = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i] != v)
                {
                    neighbors[j++] = neighbors[i];
                }
            }
            neighbors[neighbors.Length - 1] = v;
            return neighbors;
        }

        /// <summary>
        /// Test whether we accept atom and it's connected bonds for inclusion in a
        /// double bond configuration. This method checks for query bonds (up/down)
        /// as well as double bond counts. If there is more then one double bond in
        /// the connect bonds then it cannot have Z/E configuration.
        /// </summary>
        /// <param name="atom">a double bonded atom</param>
        /// <param name="bonds">all bonds connected to the atom</param>
        /// <returns>whether the atom is accepted for configuration</returns>
        internal static bool Accept(IAtom atom, IEnumerable<IBond> bonds)
        {
            int dbCount = 0;

            // not SP2
            if (!Hybridization.SP2.Equals(atom.Hybridization)) return false;

            // only have one neighbour (which is the other atom) -> this is no configurable
            if (bonds.Count() == 1) return false;

            foreach (var bond in bonds)
            {
                // increment the number of double bonds
                if (BondOrder.Double.Equals(bond.Order)) dbCount++;

                // up/down bonds sometimes used to indicate E/Z
                BondStereo stereo = bond.Stereo;
                if (BondStereo.UpOrDown.Equals(stereo) || BondStereo.UpOrDownInverted.Equals(stereo))
                    return false;
            }

            // not cumulated
            return dbCount == 1;
        }
    }
}
