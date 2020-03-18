using NCDK.Common.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Beam
{
    /// <summary>
    /// Simple matching greedily chooses edges and matches. The produced matching is
    /// not guaranteed to be maximum but provides a starting point for improvement
    /// through augmentation.
    /// </summary>
    // @author John May
    internal sealed class ArbitraryMatching
    {
        /// <summary>
        /// Create an arbitrary matching on the subset of vertices ('s') of provided
        /// graph. The provided matching should be empty.
        ///
        /// <param name="g">graph to match</param>
        /// <param name="m">empty matching (presumed)</param>
        /// <param name="s">subset of vertices</param>
        /// <returns>number of vertices matched</returns>
        /// </summary>
        public static int Initial(Graph g, Matching m, BitArray s)
        {
            int nMatched = 0;

            for (int v = BitArrays.NextSetBit(s, 0); v >= 0; v = BitArrays.NextSetBit(s, v + 1))
            {
                // skip if already matched
                if (m.Matched(v))
                    continue;

                // find a single edge which is not matched and match it
                int d = g.Degree(v);
                for (int j = 0; j < d; ++j)
                {
                    Edge e = g.EdgeAt(v, j);
                    int w = e.Other(v);
                    if ((e.Bond != Bond.Single) && m.Unmatched(w) && s[w])
                    {
                        m.Match(v, w);
                        nMatched += 2;
                        break;
                    }
                }
            }

            return nMatched;
        }

        public static int Dfs(Graph g, Matching m, BitArray s)
        {

            int nMatched = 0;
            BitArray unvisited = (BitArray)s.Clone();

            // visit those with degree 1 first and expand out matching
            for (int v = BitArrays.NextSetBit(unvisited, 0); v >= 0; v = BitArrays.NextSetBit(unvisited, v + 1))
            {
                if (!m.Matched(v))
                {
                    int cnt = 0;
                    int d = g.Degree(v);
                    while (--d >= 0)
                    {
                        int w = g.EdgeAt(v, d).Other(v);
                        if (unvisited[w])
                            ++cnt;
                    }
                    if (cnt == 1)
                        nMatched += DfsVisit(g, v, m, unvisited, true);
                }
            }

            // now those which aren't degree 1
            for (int v = BitArrays.NextSetBit(unvisited, 0); v >= 0; v = BitArrays.NextSetBit(unvisited, v + 1))
            {
                if (!m.Matched(v))
                {
                    nMatched += DfsVisit(g, v, m, unvisited, true);
                }
            }

            return nMatched;
        }

        public static int DfsVisit(Graph g, int v, Matching m, BitArray unvisited, bool match)
        {
            unvisited.Set(v, false);
            int nMatched = 0;
            int d = g.Degree(v);
            while (--d >= 0)
            {
                int w = g.EdgeAt(v, d).Other(v);
                if (unvisited[w])
                {
                    if (match)
                    {
                        m.Match(v, w);
                        return 2 + DfsVisit(g, w, m, unvisited, false);
                    }
                    else
                    {
                        nMatched += DfsVisit(g, w, m, unvisited, true);
                    }
                }
            }
            return nMatched;
        }

        /// <summary>
        /// When precisely two vertices are unmatched we only need to find a single
        /// augmenting path. Rather than run through edmonds with blossoms etc we
        /// simple do a targest DFS for the path.
        ///
        /// <param name="g">graph</param>
        /// <param name="m">matching</param>
        /// <param name="nMatched">current matching cardinality must be |s|-nMathced == 2</param>
        /// <param name="s">subset size</param>
        /// <returns>new match cardinality</returns>
        /// </summary>
        public static int AugmentOnce(Graph g, Matching m, int nMatched, BitArray s)
        {

            int vStart = BitArrays.NextSetBit(s, 0);
            while (vStart >= 0)
            {
                if (!m.Matched(vStart)) break;
                vStart = BitArrays.NextSetBit(s, vStart + 1);
            }
            int vEnd = BitArrays.NextSetBit(s, vStart + 1);
            while (vEnd >= 0)
            {
                if (!m.Matched(vEnd)) break;
                vEnd = BitArrays.NextSetBit(s, vEnd + 1);
            }

            // find an augmenting path between vStart and vEnd
            int[] path = new int[g.Order];
            int len = FindPath(g, vStart, vEnd, s, path, 0, m, false);
            if (len > 0)
            {
                // augment
                for (int i = 0; i < len; i += 2)
                {
                    m.Match(path[i], path[i + 1]);
                }
                nMatched += 2;
            }

            return nMatched;
        }

        public static int FindPath(Graph g, int v, int end, BitArray unvisited, int[] path, int len, Matching m, bool matchNeeded)
        {
            unvisited.Set(v, false);
            path[len++] = v;
            int l;
            int d = g.Degree(v);
            for (int j = 0; j < d; ++j)
            {
                Edge e = g.EdgeAt(v, j);
                // explicit single bond can not be augmented along!!
                if (e.Bond == Bond.Single)
                    continue;
                int w = e.Other(v);
                if (unvisited[w])
                {
                    if (w == end)
                    {
                        path[len] = w;
                        len++;
                        unvisited.Set(v, true);
                        // odd length path no good
                        return ((len & 0x1) == 1) ? 0 : len;
                    }
                    else if ((m.Other(w) == v) == matchNeeded)
                    {
                        if ((l = FindPath(g, w, end, unvisited, path, len, m, !matchNeeded)) > 0)
                        {
                            unvisited.Set(v, true);
                            return l;
                        }
                    }
                }
            }
            unvisited.Set(v, true);
            return 0;
        }
    }
}