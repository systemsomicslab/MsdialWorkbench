/* Copyright (C) 2001-2007  The Chemistry Development Kit (CDK) project
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
using NCDK.Graphs.Matrix;
using System;
using System.Collections.Generic;

namespace NCDK.Graphs
{
    /// <summary>
    /// Tools class with methods for handling molecular graphs.
    /// </summary>
    // @author      steinbeck
    // @cdk.module  core
    // @cdk.created 2001-06-17
    public static class PathTools
    {
        /// <summary>
        /// Sums up the columns in a 2D int matrix.
        /// </summary>
        /// <param name="apsp">The 2D int matrix</param>
        /// <returns>A 1D matrix containing the column sum of the 2D matrix</returns>
        public static int[] GetInt2DColumnSum(int[][] apsp)
        {
            int[] colSum = new int[apsp.Length];
            int sum;
            for (int i = 0; i < apsp.Length; i++)
            {
                sum = 0;
                for (int j = 0; j < apsp.Length; j++)
                {
                    sum += apsp[i][j];
                }
                colSum[i] = sum;
            }
            return colSum;
        }
        
        /// <summary>
        /// All-Pairs-Shortest-Path computation based on Floyd's
        /// algorithm <token>cdk-cite-FLO62</token>. It takes an nxn
        /// matrix C of edge costs and produces an nxn matrix A of lengths of shortest paths.
        /// </summary>
        /// <param name="costMatrix">edge cost matrix</param>
        /// <returns>the topological distance matrix</returns>
        public static int[][] ComputeFloydAPSP(int[][] costMatrix)
        {
            int nrow = costMatrix.Length;
            int[][] distMatrix = Arrays.CreateJagged<int>(nrow, nrow);
            //Debug.WriteLine($"Matrix size: {n}");
            for (int i = 0; i < nrow; i++)
            {
                for (int j = 0; j < nrow; j++)
                {
                    if (costMatrix[i][j] == 0)
                    {
                        distMatrix[i][j] = 999999999;
                    }
                    else
                    {
                        distMatrix[i][j] = 1;
                    }
                }
            }
            for (int i = 0; i < nrow; i++)
            {
                distMatrix[i][i] = 0;
                // no self cycle
            }
            for (int k = 0; k < nrow; k++)
            {
                for (int i = 0; i < nrow; i++)
                {
                    for (int j = 0; j < nrow; j++)
                    {
                        if (distMatrix[i][k] + distMatrix[k][j] < distMatrix[i][j])
                        {
                            distMatrix[i][j] = distMatrix[i][k] + distMatrix[k][j];
                            //P[i][j] = k;        // k is included in the shortest path
                        }
                    }
                }
            }
            return distMatrix;
        }

        /// <summary>
        /// All-Pairs-Shortest-Path computation based on Floyd's
        /// algorithm <token>cdk-cite-FLO62</token>. It takes an nxn
        /// matrix C of edge costs and produces an nxn matrix A of lengths of shortest
        /// paths.
        /// </summary>
        /// <param name="costMatrix">edge cost matrix</param>
        /// <returns>the topological distance matrix</returns>
        public static int[][] ComputeFloydAPSP(double[][] costMatrix)
        {
            int nrow = costMatrix.Length;
            int[][] distMatrix = Arrays.CreateJagged<int>(nrow, nrow);
            //Debug.WriteLine($"Matrix size: {n}");
            for (int i = 0; i < nrow; i++)
            {
                for (int j = 0; j < nrow; j++)
                {
                    if (costMatrix[i][j] == 0)
                    {
                        distMatrix[i][j] = 0;
                    }
                    else
                    {
                        distMatrix[i][j] = 1;
                    }
                }
            }
            return ComputeFloydAPSP(distMatrix);
        }

        /// <summary>
        /// Recursively performs a depth first search in a molecular graphs contained in
        /// the AtomContainer molecule, starting at the root atom and returning when it
        /// hits the target atom.
        /// <para>
        /// CAUTION: This recursive method sets the VISITED flag of each atom
        /// does not reset it after finishing the search. If you want to do the
        /// operation on the same collection of atoms more than once, you have
        /// to set all the VISITED flags to false before each operation
        /// by looping of the atoms and doing a
        /// "atom.Flag = (CDKConstants.VISITED, false);"
        /// </para>
        /// <para>
        /// Note that the path generated by the search will not contain the root atom,
        /// but will contain the target atom
        /// </para>
        /// </summary>
        /// <param name="molecule">The AtomContainer to be searched</param>
        /// <param name="root">The root atom to start the search at</param>
        /// <param name="target">The target</param>
        /// <param name="path">An AtomContainer to be filled with the path</param>
        /// <returns><see langword="true"/> if the target atom was found during this function call</returns>
        public static bool DepthFirstTargetSearch(IAtomContainer molecule, IAtom root, IAtom target, IAtomContainer path)
        {
            var bonds = molecule.GetConnectedBonds(root);
            IAtom nextAtom;
            root.IsVisited = true;
            bool first = path.IsEmpty();
            if (first)
                path.Atoms.Add(root);
            foreach (var bond in bonds)
            {
                nextAtom = bond.GetOther(root);
                if (!nextAtom.IsVisited)
                {
                    path.Atoms.Add(nextAtom);
                    path.Bonds.Add(bond);
                    if (nextAtom.Equals(target))
                    {
                        if (first)
                            path.Atoms.Remove(root);
                        return true;
                    }
                    else
                    {
                        if (!DepthFirstTargetSearch(molecule, nextAtom, target, path))
                        {
                            // we did not find the target
                            path.Atoms.Remove(nextAtom);
                            path.Bonds.Remove(bond);
                        }
                        else
                        {
                            if (first)
                                path.Atoms.Remove(root);
                            return true;
                        }
                    }
                }
            }
            if (first)
                path.Atoms.Remove(root);
            return false;
        }

        /// <summary>
        /// Performs a BreadthFirstSearch in an AtomContainer starting with a
        /// particular sphere, which usually consists of one start atom. While
        /// searching the graph, the method marks each visited atom. It then puts all
        /// the atoms connected to the atoms in the given sphere into a new vector
        /// which forms the sphere to search for the next recursive method call. All
        /// atoms that have been visited are put into a molecule container. This
        /// BreadthFirstSearch does thus find the connected graph for a given start
        /// atom.
        /// </summary>
        /// <param name="atomContainer">The AtomContainer to be searched</param>
        /// <param name="sphere">A sphere of atoms to start the search with</param>
        /// <param name="molecule">A molecule into which all the atoms and bonds are stored that are found during search</param>
        public static void BreadthFirstSearch(IAtomContainer atomContainer, IEnumerable<IAtom> sphere, IAtomContainer molecule)
        {
            // Debug.WriteLine($"Staring partitioning with this ac: {ac}");
            BreadthFirstSearch(atomContainer, sphere, molecule, -1);
        }

        /// <summary>
        /// Returns the atoms which are closest to an atom in an AtomContainer by bonds.
        /// If number of atoms in or below sphere x&lt;max and number of atoms in or below sphere x+1&gt;max then
        /// atoms in or below sphere x+1 are returned.
        /// </summary>
        /// <param name="atomContainer">The AtomContainer to examine</param>
        /// <param name="atom">the atom to start from</param>
        /// <param name="max">the number of neighbours to return</param>
        /// <returns> the average bond length</returns>
        public static IEnumerable<IAtom> FindClosestByBond(IAtomContainer atomContainer, IAtom atom, int max)
        {
            IAtomContainer mol = atomContainer.Builder.NewAtomContainer();
            BreadthFirstSearch(atomContainer, new[] { atom }, mol, max);
            IAtom[] returnValue = new IAtom[mol.Atoms.Count - 1];
            int k = 0;
            for (int i = 0; i < mol.Atoms.Count; i++)
            {
                if (!mol.Atoms[i].Equals(atom))
                {
                    returnValue[k] = mol.Atoms[i];
                    k++;
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Performs a BreadthFirstSearch in an AtomContainer starting with a
        /// particular sphere, which usually consists of one start atom. While
        /// searching the graph, the method marks each visited atom. It then puts all
        /// the atoms connected to the atoms in the given sphere into a new vector
        /// which forms the sphere to search for the next recursive method call. All
        /// atoms that have been visited are put into a molecule container. This
        /// BreadthFirstSearch does thus find the connected graph for a given start
        /// atom.
        /// <para>IMPORTANT: this method does not reset the VISITED flags, which must be
        /// done if calling this method twice!
        /// </para>
        /// </summary>
        /// <param name="atomContainer">The AtomContainer to be searched</param>
        /// <param name="sphere">A sphere of atoms to start the search with</param>
        /// <param name="molecule">A molecule into which all the atoms and bonds are stored that are found during search</param>
        /// <param name="max">max</param>
        public static void BreadthFirstSearch(IAtomContainer atomContainer, IEnumerable<IAtom> sphere, IAtomContainer molecule, int max)
        {
            IAtom nextAtom;
            List<IAtom> newSphere = new List<IAtom>();
            foreach (var atom in sphere)
            {
                molecule.Atoms.Add(atom);
                // first copy LonePair's and SingleElectron's of this Atom as they need
                // to be copied too
                var lonePairs = atomContainer.GetConnectedLonePairs(atom);
                foreach (var lonePair in lonePairs)
                    molecule.LonePairs.Add(lonePair);

                var singleElectrons = atomContainer.GetConnectedSingleElectrons(atom);
                foreach (var singleElectron in singleElectrons)
                    molecule.SingleElectrons.Add(singleElectron);

                // now look at bonds
                var bonds = atomContainer.GetConnectedBonds(atom);
                foreach (var bond in bonds)
                {
                    nextAtom = bond.GetOther(atom);
                    if (!bond.IsVisited)
                    {
                        molecule.Atoms.Add(nextAtom);
                        molecule.Bonds.Add(bond);
                        bond.IsVisited = true;
                    }
                    if (!nextAtom.IsVisited)
                    {
                        newSphere.Add(nextAtom);
                        nextAtom.IsVisited = true;
                    }
                }
                if (max > -1 && molecule.Atoms.Count > max) return;
            }
            if (newSphere.Count > 0)
            {
                BreadthFirstSearch(atomContainer, newSphere, molecule, max);
            }
        }

        /// <summary>
        /// Performs a BreadthFirstTargetSearch in an AtomContainer starting with a
        /// particular sphere, which usually consists of one start atom. While
        /// searching the graph, the method marks each visited atom. It then puts all
        /// the atoms connected to the atoms in the given sphere into a new vector
        /// which forms the sphere to search for the next recursive method call.
        /// The method keeps track of the sphere count and returns it as soon
        /// as the target atom is encountered.
        /// </summary>
        /// <param name="atomContainer">The AtomContainer in which the path search is to be performed.</param>
        /// <param name="sphere">The sphere of atoms to start with. Usually just the starting atom</param>
        /// <param name="target">The target atom to be searched</param>
        /// <param name="pathLength">The current path length, incremented and passed in recursive calls. Call this method with "zero".</param>
        /// <param name="cutOff">Stop the path search when this cutOff sphere count has been reatomContainerhed</param>
        /// <returns>The shortest path between the starting sphere and the target atom</returns>
        public static int BreadthFirstTargetSearch(IAtomContainer atomContainer, IEnumerable<IAtom> sphere, IAtom target, int pathLength, int cutOff)
        {
            if (pathLength == 0) ResetFlags(atomContainer);
            pathLength++;
            if (pathLength > cutOff)
            {
                return -1;
            }

            IAtom nextAtom;
            List<IAtom> newSphere = new List<IAtom>();
            foreach (var atom in sphere)
            {
                var bonds = atomContainer.GetConnectedBonds(atom);
                foreach (var bond in bonds)
                {
                    if (!bond.IsVisited)
                    {
                        bond.IsVisited = true;
                    }
                    nextAtom = bond.GetOther(atom);
                    if (!nextAtom.IsVisited)
                    {
                        if (nextAtom == target)
                        {
                            return pathLength;
                        }
                        newSphere.Add(nextAtom);
                        nextAtom.IsVisited = true;
                    }
                }
            }
            if (newSphere.Count > 0)
            {
                return BreadthFirstTargetSearch(atomContainer, newSphere, target, pathLength, cutOff);
            }
            return -1;
        }

        internal static void ResetFlags(IAtomContainer atomContainer)
        {
            for (int f = 0; f < atomContainer.Atoms.Count; f++)
            {
                atomContainer.Atoms[f].IsVisited = false;
            }
            for (int f = 0; f < atomContainer.Bonds.Count; f++)
            {
                atomContainer.Bonds[f].IsVisited = false;
            }
        }

        /// <summary>
        /// Returns the radius of the molecular graph.
        /// </summary>
        /// <param name="atomContainer">The molecule to consider</param>
        /// <returns>The topological radius</returns>
        public static int GetMolecularGraphRadius(IAtomContainer atomContainer)
        {
            int natom = atomContainer.Atoms.Count;

            int[][] admat = AdjacencyMatrix.GetMatrix(atomContainer);
            int[][] distanceMatrix = ComputeFloydAPSP(admat);

            int[] eta = new int[natom];
            for (int i = 0; i < natom; i++)
            {
                int max = -99999;
                for (int j = 0; j < natom; j++)
                {
                    if (distanceMatrix[i][j] > max) max = distanceMatrix[i][j];
                }
                eta[i] = max;
            }
            int min = 999999;
            foreach (var anEta in eta)
            {
                if (anEta < min) min = anEta;
            }
            return min;
        }

        /// <summary>
        /// Returns the diameter of the molecular graph.
        /// </summary>
        /// <param name="atomContainer">The molecule to consider</param>
        /// <returns>The topological diameter</returns>
        public static int GetMolecularGraphDiameter(IAtomContainer atomContainer)
        {
            int natom = atomContainer.Atoms.Count;

            int[][] admat = AdjacencyMatrix.GetMatrix(atomContainer);
            int[][] distanceMatrix = ComputeFloydAPSP(admat);

            int[] eta = new int[natom];
            for (int i = 0; i < natom; i++)
            {
                int mmax = -99999;
                for (int j = 0; j < natom; j++)
                {
                    if (distanceMatrix[i][j] > mmax) mmax = distanceMatrix[i][j];
                }
                eta[i] = mmax;
            }
            int max = -999999;
            foreach (var anEta in eta)
            {
                if (anEta > max) max = anEta;
            }
            return max;
        }
        
        /// <summary>
        /// Returns the number of vertices that are a distance 'd' apart.
        /// <para>In this method, d is the topological distance (i.e. edge count).</para>
        /// </summary>
        /// <param name="atomContainer">The molecule to consider</param>
        /// <param name="distance">The distance to consider</param>
        /// <returns>The number of vertices</returns>
        public static int GetVertexCountAtDistance(IAtomContainer atomContainer, int distance)
        {
            int natom = atomContainer.Atoms.Count;

            int[][] admat = AdjacencyMatrix.GetMatrix(atomContainer);
            int[][] distanceMatrix = ComputeFloydAPSP(admat);

            int matches = 0;

            for (int i = 0; i < natom; i++)
            {
                for (int j = 0; j < natom; j++)
                {
                    if (distanceMatrix[i][j] == distance) matches++;
                }
            }
            return matches / 2;
        }

        /// <summary>
        /// Returns a list of atoms in the shortest path between two atoms.
        /// <para>This method uses the Djikstra algorithm to find all the atoms in the shortest
        /// path between the two specified atoms. The start and end atoms are also included
        /// in the path returned</para>
        /// </summary>
        /// <remarks>This implementation recalculates all shortest paths from the start atom
        /// for each method call and does not indicate if there are equally short paths
        /// from the start to the end.</remarks>
        /// <param name="atomContainer">The molecule to search in</param>
        /// <param name="start">The starting atom</param>
        /// <param name="end">The ending atom</param>
        /// <returns>A <see cref="IList{IAtom}"/> containing the atoms in the shortest path between <paramref name="start"/> and
        /// <paramref name="end"/> inclusive</returns>
        /// <seealso cref="ShortestPaths"/>
        /// <seealso cref="ShortestPaths.GetAtomsTo(IAtom)"/>
        /// <seealso cref="AllPairsShortestPaths"/>
        [Obsolete("Use " + nameof(ShortestPaths) + "." + nameof(ShortestPaths.GetPathsTo) + "(" + nameof(IAtom) + ")")]
        public static IReadOnlyList<IAtom> GetShortestPath(IAtomContainer atomContainer, IAtom start, IAtom end)
        {
            int natom = atomContainer.Atoms.Count;
            int endNumber = atomContainer.Atoms.IndexOf(end);
            int startNumber = atomContainer.Atoms.IndexOf(start);
            int[] dist = new int[natom];
            int[] previous = new int[natom];
            for (int i = 0; i < natom; i++)
            {
                dist[i] = 99999999;
                previous[i] = -1;
            }
            dist[atomContainer.Atoms.IndexOf(start)] = 0;

            List<int> qList = new List<int>();
            for (int i = 0; i < natom; i++)
                qList.Add(i);

            while (true)
            {
                if (qList.Count == 0) break;

                // extract min
                int u = 999999;
                int index = 0;
                foreach (var ttmp in qList)
                {
                    if (dist[ttmp] < u)
                    {
                        u = dist[ttmp];
                        index = ttmp;
                    }
                }
                qList.Remove(index); // it means 'qList.RemoveAt(qList.IndexOf(index))'
               
                if (index == endNumber) break;

                // relaxation
                var connected = atomContainer.GetConnectedAtoms(atomContainer.Atoms[index]);
                foreach (var aConnected in connected)
                {
                    int anum = atomContainer.Atoms.IndexOf(aConnected);
                    if (dist[anum] > dist[index] + 1)
                    { // all edges have equals weights
                        dist[anum] = dist[index] + 1;
                        previous[anum] = index;
                    }
                }
            }

            var tmp = new List<IAtom>();
            int tmpSerial = endNumber;
            while (true)
            {
                tmp.Insert(0, atomContainer.Atoms[tmpSerial]);
                tmpSerial = previous[tmpSerial];
                if (tmpSerial == startNumber)
                {
                    tmp.Insert(0, atomContainer.Atoms[tmpSerial]);
                    break;
                }
            }
            return tmp;
        }

        /// <summary>
        /// Get a list of all the paths between two atoms.
        /// <para>
        /// If the two atoms are the same an empty list is returned. Note that this problem
        /// is NP-hard and so can take a long time for large graphs.</para>
        /// </summary>
        /// <param name="atomContainer">The molecule to consider</param>
        /// <param name="start">The starting Atom of the path</param>
        /// <param name="end">The ending Atom of the path</param>
        /// <returns>A <see cref="IList{T}"/> containing all the paths between the specified atoms</returns>
        public static IList<IList<IAtom>> GetAllPaths(IAtomContainer atomContainer, IAtom start, IAtom end)
        {
            var allPaths = new List<IList<IAtom>>();
            if (start.Equals(end)) return allPaths;
            FindPathBetween(allPaths, atomContainer, start, end, new List<IAtom>());
            return allPaths;
        }

        private static void FindPathBetween(List<IList<IAtom>> allPaths, IAtomContainer atomContainer, IAtom start, IAtom end, List<IAtom> path)
        {
            if (start == end)
            {
                path.Add(start);
                allPaths.Add(new List<IAtom>(path));
                path.RemoveAt(path.Count - 1);
                return;
            }
            if (path.Contains(start)) return;
            path.Add(start);
            var nbrs = atomContainer.GetConnectedAtoms(start);
            foreach (var nbr in nbrs)
                FindPathBetween(allPaths, atomContainer, nbr, end, path);
            path.RemoveAt(path.Count - 1);
        }

        /// <summary>
        /// Get the paths starting from an atom of specified length.
        /// <para>This method returns a set of paths. Each path is a <see cref="IList{IAtom}"/> that make up the path (i.e. they are sequentially connected).</para>
        /// </summary>
        /// <param name="atomContainer">The molecule to consider</param>
        /// <param name="start">The starting atom</param>
        /// <param name="length">The length of paths to look for</param>
        /// <returns>A <see cref="IList{T}"/> of <see cref="IList{T}"/> of <see cref="IAtom"/> containing the paths found</returns>
        public static IList<IList<IAtom>> GetPathsOfLength(IAtomContainer atomContainer, IAtom start, int length)
        {
            IList<IAtom> curPath = new List<IAtom>();
            var paths = new List<IList<IAtom>>();
            curPath.Add(start);
            paths.Add(curPath);
            for (int i = 0; i < length; i++)
            {
                var tmpList = new List<IList<IAtom>>();
                foreach (var path in paths)
                {
                    curPath = path;
                    IAtom lastVertex = curPath[curPath.Count - 1];
                    var neighbors = atomContainer.GetConnectedAtoms(lastVertex);
                    foreach (var neighbor in neighbors)
                    {
                        List<IAtom> newPath = new List<IAtom>(curPath);
                        if (newPath.Contains(neighbor)) continue;
                        newPath.Add(neighbor);
                        tmpList.Add(newPath);
                    }
                }
                paths.Clear();
                paths.AddRange(tmpList);
            }
            return paths;
        }

        /// <summary>
        /// Get all the paths starting from an atom of length 0 up to the specified length.
        /// <para>This method returns a set of paths. Each path is a <see cref="IList{IAtom}"/> of atoms that make up the path (i.e. they are sequentially connected).</para>
        /// </summary>
        /// <param name="atomContainer">The molecule to consider</param>
        /// <param name="start">The starting atom</param>
        /// <param name="length">The maximum length of paths to look for</param>
        /// <returns>A <see cref="IList{T}"/> of <see cref="IList{T}"/> of <see cref="IAtom"/> containing the paths found</returns>
        public static IList<IList<IAtom>> GetPathsOfLengthUpto(IAtomContainer atomContainer, IAtom start, int length)
        {
            IList<IAtom> curPath = new List<IAtom>();
            List<IList<IAtom>> paths = new List<IList<IAtom>>();
            List<IList<IAtom>> allpaths = new List<IList<IAtom>>();
            curPath.Add(start);
            paths.Add(curPath);
            allpaths.Add(curPath);
            for (int i = 0; i < length; i++)
            {
                IList<IList<IAtom>> tmpList = new List<IList<IAtom>>();
                foreach (var path in paths)
                {
                    curPath = path;
                    IAtom lastVertex = curPath[curPath.Count - 1];
                    var neighbors = atomContainer.GetConnectedAtoms(lastVertex);
                    foreach (var neighbor in neighbors)
                    {
                        List<IAtom> newPath = new List<IAtom>(curPath);
                        if (newPath.Contains(neighbor)) continue;
                        newPath.Add(neighbor);
                        tmpList.Add(newPath);
                    }
                }
                paths.Clear();
                paths.AddRange(tmpList);
                allpaths.AddRange(tmpList);
            }
            return (allpaths);
        }

        /// <summary>
        /// Get all the paths starting from an atom of length 0 up to the specified
        /// length. If the number of paths exceeds the set <paramref name="limit"/> then an
        /// exception is thrown. <p/> This method returns a set of paths. Each path
        /// is a <see cref="IList{T}"/> of <see cref="IAtom"/> that make up the path (i.e. they are
        /// sequentially connected).
        /// </summary>
        /// <param name="atomContainer">The molecule to consider</param>
        /// <param name="start">The starting atom</param>
        /// <param name="length">The maximum length of paths to look for</param>
        /// <param name="limit">Limit the number of paths - thrown an exception if exceeded</param>
        /// <returns>A <see cref="IList{T}"/> of <see cref="IList{T}"/> of <see cref="IAtom"/> containing the paths found</returns>
        /// <exception cref="CDKException">if the number of paths generated was larger than the limit.</exception>
        public static IList<IList<IAtom>> GetLimitedPathsOfLengthUpto(IAtomContainer atomContainer, IAtom start, int length, int limit)
        {
            IList<IAtom> curPath = new List<IAtom>();
            List<IList<IAtom>> paths = new List<IList<IAtom>>();
            List<IList<IAtom>> allpaths = new List<IList<IAtom>>();
            curPath.Add(start);
            paths.Add(curPath);
            allpaths.Add(curPath);
            for (int i = 0; i < length; i++)
            {
                IList<IList<IAtom>> tmpList = new List<IList<IAtom>>();
                foreach (var path in paths)
                {
                    curPath = path;
                    IAtom lastVertex = curPath[curPath.Count - 1];
                    var neighbors = atomContainer.GetConnectedAtoms(lastVertex);
                    foreach (var neighbor in neighbors)
                    {
                        IList<IAtom> newPath = new List<IAtom>(curPath);
                        if (newPath.Contains(neighbor)) continue;
                        newPath.Add(neighbor);
                        tmpList.Add(newPath);
                    }
                }
                if (allpaths.Count + tmpList.Count > limit)
                    throw new CDKException(
                            "Too many paths generate. We're working making this faster but for now try generating paths with a smaller length");

                paths.Clear();
                paths.AddRange(tmpList);
                allpaths.AddRange(tmpList);
            }
            return (allpaths);
        }
    }
}
