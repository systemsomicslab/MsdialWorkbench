/*
 * Copyright (C) 2012 John May <jwmay@users.sf.net>
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

using System;

namespace NCDK.Graphs
{
    /// <summary>
    /// Find and reconstruct the shortest paths from a given start atom to any other
    /// connected atom. The number of shortest paths (<see cref="GetNPathsTo(int)"/>) and the
    /// distance (<see cref="GetDistanceTo(int)"/>) can be accessed before reconstructing all
    /// the paths. When no path is found (i.e. not-connected) an empty path is always
    /// returned. 
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs"]/*' />
    /// </example>
    /// <remarks>
    /// If shortest paths from multiple start atoms are required 
    /// <see cref="AllPairsShortestPaths"/> will have a small performance advantage. Please use
    /// <see cref="Matrix.TopologicalMatrix"/> if only the
    /// shortest distances between atoms is required.
    /// </remarks>
    /// <seealso cref="AllPairsShortestPaths"/>
    /// <seealso cref="Matrix.TopologicalMatrix"/>
    // @author John May
    // @cdk.module core
    public sealed class ShortestPaths
    {
        /* empty path when no valid path was found */
        private static readonly int[] EmptyPath = Array.Empty<int>();

        /* empty paths when no valid path was found */
        private static readonly int[][] EmptyPaths = Array.Empty<int[]>();

        /* route to each vertex */
        private readonly IRoute[] routeTo;

        /* distance to each vertex */
        private readonly int[] distTo;

        /* number of paths to each vertex */
        private readonly int[] nPathsTo;

        /* low order paths */
        private readonly bool[] precedes;

        private readonly int start, limit;
        private readonly IAtomContainer container;

        /// <summary>
        /// Create a new shortest paths tool for a single start atom. If shortest
        /// paths from multiple start atoms are required <see cref="AllPairsShortestPaths"/>
        /// will have a small performance advantage.
        /// </summary>
        /// <param name="container">an atom container to find the paths of</param>
        /// <param name="start">the start atom to which all shortest paths will be computed</param>
        /// <seealso cref="AllPairsShortestPaths"/>
        public ShortestPaths(IAtomContainer container, IAtom start)
            : this(GraphUtil.ToAdjList(container), container, container.Atoms.IndexOf(start))
        {
        }

        /// <summary>
        /// Internal constructor for use by <see cref="AllPairsShortestPaths"/>. This
        /// constructor allows the passing of adjacency list directly so the
        /// representation does not need to be rebuilt for a different start atom.
        /// </summary>
        /// <param name="adjacent">adjacency list representation - built from <see cref="GraphUtil.ToAdjList(IAtomContainer)"/></param>
        /// <param name="container">container used to access atoms and their indices</param>
        /// <param name="start">the start atom index of the shortest paths</param>
        public ShortestPaths(int[][] adjacent, IAtomContainer container, int start)
            : this(adjacent, container, start, null)
        { }

        /// <summary>
        /// Create a new shortest paths search for the given graph from the <paramref name="start"/>
        /// vertex. The ordering for use by <see cref="IsPrecedingPathTo(int)"/>
        /// can also be specified.
        /// </summary>
        /// <param name="adjacent">adjacency list representation - built from <see cref="GraphUtil.ToAdjList(IAtomContainer)"/></param>
        /// <param name="container">container used to access atoms and their indices</param>
        /// <param name="start">the start atom index of the shortest paths</param>
        /// <param name="ordering">vertex ordering for preceding path (null = don't use)</param>
        public ShortestPaths(int[][] adjacent, IAtomContainer container, int start, int[] ordering)
            : this(adjacent, container, start, adjacent.Length, ordering)
        { }

        /// <summary>
        /// Create a new shortest paths search for the given graph from the <paramref name="start"/>
        /// vertex. The ordering for use by <see cref="IsPrecedingPathTo(int)"/>
        /// can also be specified.
        /// </summary>
        /// <param name="adjacent">adjacency list representation - built from <see cref="GraphUtil.ToAdjList(IAtomContainer)"/></param>
        /// <param name="container">container used to access atoms and their indices</param>
        /// <param name="start">the start atom index of the shortest paths</param>
        /// <param name="limit">the maximum length path to find</param>
        /// <param name="ordering">vertex ordering for preceding path (null = don't use)</param>
        public ShortestPaths(int[][] adjacent, IAtomContainer container, int start, int limit, int[] ordering)
        {
            int n = adjacent.Length;

            this.container = container;
            this.start = start;
            this.limit = limit;

            this.distTo = new int[n];
            this.routeTo = new IRoute[n];
            this.nPathsTo = new int[n];
            this.precedes = new bool[n];

            // skip computation for empty molecules
            if (n == 0) return;
            if (start == -1) throw new ArgumentException("invalid vertex start - atom not found container");

            for (int i = 0; i < n; i++)
            {
                distTo[i] = int.MaxValue;
            }

            // initialise source vertex
            distTo[start] = 0;
            routeTo[start] = new Source(start);
            nPathsTo[start] = 1;
            precedes[start] = true;

            if (ordering != null)
            {
                Compute(adjacent, ordering);
            }
            else
            {
                Compute(adjacent);
            }
        }

        /// <summary>
        /// Perform a breath-first-search (BFS) from the start atom. The <see cref="distTo"/>[]
        /// is updated on each iteration. The <see cref="routeTo"/>[] keeps track of our route back
        /// to the source. The method has aspects similar to Dijkstra's shortest path
        /// but we are working with vertices and thus our edges are unweighted and is
        /// more similar to a simple BFS.
        /// </summary>
        private void Compute(int[][] adjacent)
        {
            // queue is filled as we process each vertex
            int[] queue = new int[adjacent.Length];
            queue[0] = start;
            int n = 1;

            for (int i = 0; i < n; i++)
            {
                int v = queue[i];
                int dist = distTo[v] + 1;
                foreach (int w in adjacent[v])
                {
                    if (dist > limit) continue;

                    // distance is less then the current closest distance
                    if (dist < distTo[w])
                    {
                        distTo[w] = dist;
                        routeTo[w] = new SequentialRoute(this, routeTo[v], w);
                        nPathsTo[w] = nPathsTo[v];
                        queue[n++] = w;
                    }
                    else if (distTo[w] == dist)
                    {
                        routeTo[w] = new Branch(routeTo[w], new SequentialRoute(this, routeTo[v], w));
                        nPathsTo[w] += nPathsTo[v];
                    }
                }
            }
        }

        /// <summary>
        /// Perform a breath-first-search (BFS) from the start atom. The <see cref="distTo"/>[]
        /// is updated on each iteration. The <see cref="routeTo"/>[] keeps track of our route back
        /// to the source. The method has aspects similar to Dijkstra's shortest path
        /// but we are working with vertices and thus our edges are unweighted and is
        /// more similar to a simple BFS. The ordering limits the paths found to only
        /// those in which all vertices precede the <see cref="start"/> in the given ordering.
        /// This ordering limits ensure we only generate paths in one direction.
        /// </summary>
        private void Compute(int[][] adjacent, int[] ordering)
        {
            // queue is filled as we process each vertex
            int[] queue = new int[adjacent.Length];
            queue[0] = start;
            int n = 1;

            for (int i = 0; i < n; i++)
            {
                int v = queue[i];
                int dist = distTo[v] + 1;
                foreach (int w in adjacent[v])
                {
                    // distance is less then the current closest distance
                    if (dist < distTo[w])
                    {
                        distTo[w] = dist;
                        routeTo[w] = new SequentialRoute(this, routeTo[v], w); // append w to the route to v
                        nPathsTo[w] = nPathsTo[v];
                        precedes[w] = precedes[v] && ordering[w] < ordering[start];
                        queue[n++] = w;
                    }
                    else if (distTo[w] == dist)
                    {
                        // shuffle paths around depending on whether there is
                        // already a preceding path
                        if (precedes[v] && ordering[w] < ordering[start])
                        {
                            if (precedes[w])
                            {
                                routeTo[w] = new Branch(routeTo[w], new SequentialRoute(this, routeTo[v], w));
                                nPathsTo[w] += nPathsTo[v];
                            }
                            else
                            {
                                precedes[w] = true;
                                routeTo[w] = new SequentialRoute(this, routeTo[v], w);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Reconstruct a shortest path to the provided <paramref name="end"/> vertex. 
        /// </summary>
        /// <remarks>
        /// The path
        /// is an inclusive fixed size array of vertex indices. If there are multiple
        /// shortest paths the first shortest path is determined by vertex storage
        /// order. When there is no path an empty array is returned. It is considered
        /// there to be no path if the end vertex belongs to the same container but
        /// is a member of a different fragment, or the vertex is not present in the
        /// container at all.
        /// </remarks>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetPathTo_int"]/*' />
        /// </example>
        /// <param name="end">the <paramref name="end"/> vertex to find a path to</param>
        /// <returns>path from the <see cref="start"/> to the <paramref name="end"/> vertex</returns>
        /// <seealso cref="GetPathTo(IAtom)"/>
        /// <seealso cref="GetAtomsTo(int)"/>
        /// <seealso cref="GetAtomsTo(IAtom)"/>
        public int[] GetPathTo(int end)
        {
            if (end < 0 || end >= routeTo.Length) return EmptyPath;

            return routeTo[end] != null ? routeTo[end].GetToPath(distTo[end] + 1) : EmptyPath;
        }

        /// <summary>
        /// Reconstruct a shortest path to the provided <paramref name="end"/> atom. The path is
        /// an inclusive fixed size array of vertex indices. If there are multiple
        /// shortest paths the first shortest path is determined by vertex storage
        /// order. When there is no path an empty array is returned. It is considered
        /// there to be no path if the end atom belongs to the same container but is
        /// a member of a different fragment, or the atom is not present in the
        /// container at all.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetPathTo_IAtom"]/*' />
        /// </example>
        /// <param name="end">the <paramref name="end"/> vertex to find a path to</param>
        /// <returns>path from the <see cref="start"/> to the <paramref name="end"/> vertex</returns>
        /// <seealso cref="GetAtomsTo(IAtom)"/>
        /// <seealso cref="GetAtomsTo(int)"/>
        /// <seealso cref="GetPathTo(int)"/>
        public int[] GetPathTo(IAtom end)
        {
            return GetPathTo(container.Atoms.IndexOf(end));
        }

        /// <summary>
        /// Returns whether the first shortest path from the <see cref="start"/> to a given
        /// <paramref name="end"/> vertex which only passed through vertices smaller then
        /// <see cref="start"/>. This is useful for reducing the search space, the idea is
        /// used by <token>cdk-cite-Vismara97</token> in the computation of cycle prototypes.
        /// </summary>
        /// <param name="end">the end vertex</param>
        /// <returns>whether the path to the <paramref name="end"/> only passed through vertices preceding the <see cref="start"/></returns>
        public bool IsPrecedingPathTo(int end)
        {
            return (end >= 0 && end < routeTo.Length) && precedes[end];
        }

        /// <summary>
        /// Reconstruct all shortest paths to the provided <paramref name="end"/> vertex. The
        /// paths are <i>n</i> (where n is <see cref="GetNPathsTo(int)"/>) inclusive fixed
        /// size arrays of vertex indices. When there is no path an empty array is
        /// returned. It is considered there to be no path if the end vertex belongs
        /// to the same container but is a member of a different fragment, or the
        /// vertex is not present in the container at all.
        /// </summary>
        /// <remarks>
        /// <b>Important:</b> for every possible branch the number of possible paths
        /// doubles and could be in the order of tens of thousands. Although the
        /// chance of finding such a molecule is highly unlikely (C720 fullerene has
        /// at maximum 1024 paths). It is safer to check the number of paths (
        /// <see cref="GetNPathsTo(int)"/>) before attempting to reconstruct all shortest paths.
        /// </remarks>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetPathsTo_int"]/*' />
        /// </example>
        /// <param name="end">the end vertex</param>
        /// <returns>all shortest paths from the start to the end vertex</returns>
        public int[][] GetPathsTo(int end)
        {
            if (end < 0 || end >= routeTo.Length) return EmptyPaths;

            return routeTo[end] != null ? routeTo[end].GetToPaths(distTo[end] + 1) : EmptyPaths;
        }

        /// <summary>
        /// Reconstruct all shortest paths to the provided <paramref name="end"/> vertex. The
        /// paths are <i>n</i> (where n is <see cref="GetNPathsTo(int)"/>) inclusive fixed
        /// size arrays of vertex indices. When there is no path an empty array is
        /// returned. It is considered there to be no path if the end vertex belongs
        /// to the same container but is a member of a different fragment, or the
        /// vertex is not present in the container at all. 
        /// </summary>
        /// <remarks>
        /// <note type="important">
        /// For every possible branch the number of possible paths
        /// doubles and could be in the order of tens of thousands. Although the
        /// chance of finding such a molecule is highly unlikely (C720 fullerene has
        /// at maximum 1024 paths). It is safer to check the number of paths 
        /// (<see cref="GetNPathsTo(int)"/>) before attempting to reconstruct all shortest paths.
        /// </note>
        /// </remarks>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetPathsTo_IAtom"]/*' />
        /// </example>
        /// <param name="end">the end atom</param>
        /// <returns>all shortest paths from the start to the end vertex</returns>
        public int[][] GetPathsTo(IAtom end)
        {
            return GetPathsTo(container.Atoms.IndexOf(end));
        }

        /// <summary>
        /// Reconstruct a shortest path to the provided <paramref name="end"/> vertex. The path
        /// is an inclusive fixed size array <see cref="IAtom"/>s. If there are multiple
        /// shortest paths the first shortest path is determined by vertex storage
        /// order. When there is no path an empty array is returned. It is considered
        /// there to be no path if the end vertex belongs to the same container but
        /// is a member of a different fragment, or the vertex is not present in the
        /// container at all.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetAtomsTo_int"]/*' />
        /// </example>
        /// <param name="end">the <paramref name="end"/> vertex to find a path to</param>
        /// <returns>path from the <see cref="start"/> to the <paramref name="end"/> atoms as fixed size array of <see cref="IAtom"/>s</returns>
        /// <seealso cref="GetAtomsTo(int)"/>
        /// <seealso cref="GetPathTo(int)"/>
        /// <seealso cref="GetPathTo(IAtom)"/>
        public IAtom[] GetAtomsTo(int end)
        {
            int[] path = GetPathTo(end);
            IAtom[] atoms = new IAtom[path.Length];

            // copy the atoms from the path indices to the array of atoms
            for (int i = 0, n = path.Length; i < n; i++)
                atoms[i] = container.Atoms[path[i]];

            return atoms;
        }

        /// <summary>
        /// Reconstruct a shortest path to the provided <paramref name="end"/> atom. The path is
        /// an inclusive fixed size array <see cref="IAtom"/>s. If there are multiple
        /// shortest paths the first shortest path is determined by vertex storage
        /// order. When there is no path an empty array is returned. It is considered
        /// there to be no path if the end atom belongs to the same container but is
        /// a member of a different fragment, or the atom is not present in the
        /// container at all.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetAtomsTo_IAtom"]/*' />
        /// </example>
        /// <param name="end">the <paramref name="end"/> atom to find a path to</param>
        /// <returns>path from the <see cref="start"/> to the <paramref name="end"/> atoms as fixed size array of <see cref="IAtom"/>s.</returns>
        /// <seealso cref="GetAtomsTo(int)"/>
        /// <seealso cref="GetPathTo(int)"/>
        /// <seealso cref="GetPathTo(IAtom)"/>
        public IAtom[] GetAtomsTo(IAtom end)
        {
            return GetAtomsTo(container.Atoms.IndexOf(end));
        }

        /// <summary>
        /// Access the number of possible paths to the <paramref name="end"/> vertex. When there
        /// is no path 0 is returned. It is considered there to be no path if the end
        /// vertex belongs to the same container but is a member of a different
        /// fragment, or the vertex is not present in the container at all.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetNPathsTo_int"]/*' />
        /// </example>
        /// <param name="end">the <paramref name="end"/> vertex to which the number of paths will be returned</param>
        /// <returns>the number of paths to the end vertex</returns>
        public int GetNPathsTo(int end)
        {
            return (end < 0 || end >= nPathsTo.Length) ? 0 : nPathsTo[end];
        }

        /// <summary>
        /// Access the number of possible paths to the <paramref name="end"/> atom. When there is
        /// no path 0 is returned. It is considered there to be no path if the end
        /// atom belongs to the same container but is a member of a different
        /// fragment, or the atom is not present in the container at all.<p/>
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetNPathsTo_IAtom"]/*' />
        /// </example>
        /// <param name="end">the <paramref name="end"/> vertex to which the number of paths will be returned</param>
        /// <returns>the number of paths to the end vertex</returns>
        public int GetNPathsTo(IAtom end)
        {
            return GetNPathsTo(container.Atoms.IndexOf(end));
        }

        /// <summary>
        /// Access the distance to the provided <paramref name="end"/> vertex. If the two are not
        /// connected the distance is returned as <see cref="int.MaxValue"/>.
        /// Formally, there is a path if the distance is less then the number of
        /// vertices.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetDistanceTo_int_1"]/*' />
        /// Conveniently the distance is also the index of the last vertex in the path.
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetDistanceTo_int_2"]/*' />
        /// </example>
        /// <param name="end">vertex to measure the distance to</param>
        /// <returns>distance to this vertex</returns>
        /// <seealso cref="GetDistanceTo(IAtom)"/>
        public int GetDistanceTo(int end)
        {
            return (end < 0 || end >= nPathsTo.Length) ? int.MaxValue : distTo[end];
        }

        /// <summary>
        /// Access the distance to the provided <paramref name="end"/> atom. If the two are not
        /// connected the distance is returned as <see cref="int.MaxValue"/>.
        /// Formally, there is a path if the distance is less then the number of
        /// atoms.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetDistanceTo_IAtom_1"]/*' />
        /// Conveniently the distance is also the index of the last vertex in the path.
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ShortestPaths_Example.cs+GetDistanceTo_IAtom_2"]/*' />
        /// </example>
        /// <param name="end">atom to measure the distance to</param>
        /// <returns>distance to the given atom</returns>
        /// <seealso cref="GetDistanceTo(int)"/>
        public int GetDistanceTo(IAtom end)
        {
            return GetDistanceTo(container.Atoms.IndexOf(end));
        }

        /// <summary>Helper class for building a route to the shortest path</summary>
        private interface IRoute
        {
            /// <summary>
            /// Recursively convert this route to all possible shortest paths. The length
            /// is passed down the methods until the source is reached and the first path
            /// created
            /// </summary>
            /// <param name="n">length of the path</param>
            /// <returns>2D array of all shortest paths</returns>
            int[][] GetToPaths(int n);

            /// <summary>
            /// Recursively convert this route to the first shortest path. The length is
            /// passed down the methods until the source is reached and the first path
            /// created
            /// </summary>
            /// <param name="n">length of the path</param>
            /// <returns>first shortest path</returns>
            int[] GetToPath(int n);
        }

        /// <summary>The source of a route, the source is always the start atom.</summary>
        private sealed class Source
            : IRoute
        {
            private readonly int v;

            /// <summary>
            /// Create new source with a given vertex.
            /// </summary>
            /// <param name="v">start vertex</param>
            public Source(int v)
            {
                this.v = v;
            }

            /// <inheritdoc/>
            public int[][] GetToPaths(int n)
            {
                // only every one shortest path at source
                return new int[][] { GetToPath(n) };
            }

            /// <inheritdoc/>
            public int[] GetToPath(int n)
            {
                // create the path of the given length
                // and set the vertex at the first index
                int[] path = new int[n];
                path[0] = v;
                return path;
            }
        }

        /// <summary>A sequential route is vertex appended to a parent route.</summary>
        private class SequentialRoute
            : IRoute
        {
            private readonly ShortestPaths parentObject;

            private readonly int v;
            private readonly IRoute parent;

            /// <summary>
            /// Create a new sequential route from the parent and include the new vertex <paramref name="v"/>.
            /// </summary>
            /// <param name="parentObject"></param>
            /// <param name="parent">parent route</param>
            /// <param name="v">additional vertex</param>
            public SequentialRoute(ShortestPaths parentObject, IRoute parent, int v)
            {
                this.parentObject = parentObject;

                this.v = v;
                this.parent = parent;
            }

            /// <inheritdoc/>
            public int[][] GetToPaths(int n)
            {
                int[][] paths = parent.GetToPaths(n);
                int i = parentObject.distTo[v];

                // for all paths from the parent set the vertex at the given index
                foreach (int[] path in paths)
                    path[i] = v;

                return paths;
            }

            /// <inheritdoc/>
            public int[] GetToPath(int n)
            {
                int[] path = parent.GetToPath(n);
                // for all paths from the parent set vertex at the correct index (given by distance)
                path[parentObject.distTo[v]] = v;
                return path;
            }

        }

        /// <summary>
        /// A more complex route which represents a branch in our path. A branch is
        /// composed of a left and a right route. A n-way branches can be constructed by
        /// simply nesting a branch within a branch.
        /// </summary>
        private class Branch
            : IRoute
        {
            private readonly IRoute left, right;

            /// <summary>
            /// Create a branch with a left and right
            /// </summary>
            /// <param name="left">route to the left</param>
            /// <param name="right">route to the right</param>
            public Branch(IRoute left, IRoute right)
            {
                this.left = left;
                this.right = right;
            }

            /// <inheritdoc/>
            public int[][] GetToPaths(int n)
            {
                // get all shortest paths from the left and right
                int[][] leftPaths = left.GetToPaths(n);
                int[][] rightPaths = right.GetToPaths(n);

                // expand the left paths to a capacity which can also accommodate the right paths
                int[][] paths = new int[leftPaths.Length + rightPaths.Length][];
                Array.Copy(leftPaths, paths, leftPaths.Length);

                // copy the right paths in to the expanded left paths
                Array.Copy(rightPaths, 0, paths, leftPaths.Length, rightPaths.Length);

                return paths;
            }

            /// <inheritdoc/>
            public int[] GetToPath(int n)
            {
                // use the left as the first path
                return left.GetToPath(n);
            }
        }
    }
}
