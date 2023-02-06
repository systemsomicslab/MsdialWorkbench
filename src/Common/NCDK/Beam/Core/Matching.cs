using NCDK.Common.Collections;
using System;
using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Defines a matching on a graph. A matching or independent edge set is a set of
    /// edges without common vertices. A matching is perfect if every vertex in the
    /// graph is matched. Another way of thinking about the matching is that each
    /// vertex is incident to exactly one matched edge. 
    /// <para>
    /// This class provides storage and manipulation of a matching. A new match is
    /// added with <see cref="Match(int, int)"/>, any existing match for the newly matched
    /// vertices is non-longer available. For convenience <see cref="GetMatches()"/> provides
    /// the current independent edge set.
    /// </para>
    /// </summary>
    // @author John May
    internal sealed class Matching
    {

        /// <summary>Indicates an unmatched vertex.</summary>
        private const int UNMATCHED = -1;

        /// <summary>Storage of which each vertex is matched with.</summary>
        private readonly int[] match;

        /// <summary>
        /// Create a matching of the given size.
        /// </summary>
        /// <param name="n">number of items</param>
        private Matching(int n)
        {
            this.match = new int[n];
            Arrays.Fill(match, UNMATCHED);
        }

        public bool Matched(int v)
        {
            return !Unmatched(v);
        }

        /// <summary>
        /// Is the vertex v 'unmatched'.
        /// </summary>
        /// <param name="v">a vertex</param>
        /// <returns>the vertex has no matching</returns>
        public bool Unmatched(int v)
        {
            int w = match[v];
            return w < 0 || match[w] != v;
        }

        /// <summary>
        /// Access the vertex matched with 'v'.
        /// </summary>
        /// <param name="v">a vertex</param>
        /// <returns>matched vertex</returns>
        /// <exception cref="ArgumentException">the vertex is currently unmatched</exception>
        public int Other(int v)
        {
            if (Unmatched(v))
                throw new ArgumentException(v + " is not matched");
            return match[v];
        }

        /// <summary>
        /// Add the edge '{u,v}' to the matched edge set. Any existing matches for
        /// 'u' or 'v' are removed from the matched set.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">another vertex</param>
        public void Match(int u, int v)
        {
            // set the new match, don't need to update existing - we only provide
            // access to bidirectional mappings
            match[u] = v;
            match[v] = u;
        }

        /// <summary>
        /// Access the current non-redundant set of edges.
        /// </summary>
        /// <returns>matched pairs</returns>
        public ICollection<Tuple> GetMatches()
        {
            List<Tuple> tuples = new List<Tuple>(match.Length / 2);

            for (int v = 0; v < match.Length; v++)
            {
                int w = match[v];
                if (w > v && match[w] == v)
                {
                    tuples.Add(Tuple.Of(v, w));
                }
            }

            return tuples;
        }

        /// <summary>
        /// Allocate a matching with enough capacity for the given graph.
        /// </summary>
        /// <param name="g">a graph</param>
        /// <returns>matching</returns>
        public static Matching CreateEmpty(Graph g)
        {
            return new Matching(g.Order);
        }
    }
}
