/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;

namespace NCDK.Graphs.Matrix
{
    /// <summary>
    /// Calculator for a adjacency matrix representation of this <see cref="IAtomContainer"/>. An
    /// adjacency matrix is a matrix of square NxN matrix, where N is the number of
    /// atoms in the <see cref="IAtomContainer"/>. The element i,j of the matrix is 1, if the i-th
    /// and the j-th atom in the <see cref="IAtomContainer"/> share a bond. Otherwise it is zero.
    /// See <token>cdk-cite-TRI92</token>.
    /// </summary>
    // @cdk.module  core
    // @cdk.keyword adjacency matrix
    // @author      steinbeck
    // @cdk.created 2004-07-04
    // @cdk.dictref blue-obelisk:calculateAdjecencyMatrix
    public class AdjacencyMatrix
        : IGraphMatrix
    {
        /// <summary>
        /// Returns the adjacency matrix for the given <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="container">The <see cref="IAtomContainer"/> for which the matrix is calculated</param>
        /// <returns>An adjacency matrix representing this <see cref="IAtomContainer"/></returns>
        public static int[][] GetMatrix(IAtomContainer container)
        {
            IBond bond;
            int indexAtom1;
            int indexAtom2;
            int[][] conMat = Arrays.CreateJagged<int>(container.Atoms.Count, container.Atoms.Count);
            for (int f = 0; f < container.Bonds.Count; f++)
            {
                bond = container.Bonds[f];
                indexAtom1 = container.Atoms.IndexOf(bond.Begin);
                indexAtom2 = container.Atoms.IndexOf(bond.End);
                conMat[indexAtom1][indexAtom2] = 1;
                conMat[indexAtom2][indexAtom1] = 1;
            }
            return conMat;
        }
    }
}
