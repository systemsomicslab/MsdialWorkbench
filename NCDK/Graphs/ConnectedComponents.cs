using NCDK.Common.Collections;

namespace NCDK.Graphs
{
    /// <summary>
    /// Compute the connected components of an adjacency list.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ConnectedComponents_Example.cs"]/*' />
    /// </example>
    // @author John May
    // @cdk.module core
    public sealed class ConnectedComponents
    {
        /// <summary>Adjacency-list representation of a graph.</summary>
        private readonly int[][] g;

        /// <summary>Stores the component of each vertex.</summary>
        private readonly int[] component;

        /// <summary>The number of components.</summary>
        private readonly int components;

        /// <summary>The number remaining vertices.</summary>
        private int remaining;

        /// <summary>
        /// Compute the connected components of an adjacency list, <paramref name="g"/>.
        /// </summary>
        /// <param name="g">graph (adjacency list representation)</param>
        public ConnectedComponents(int[][] g)
        {
            this.g = g;
            this.component = new int[g.Length];
            this.remaining = g.Length;
            for (int i = 0; remaining > 0 && i < g.Length; i++)
                if (component[i] == 0)
                    Visit(i, ++components);
        }

        /// <summary>
        /// Visit a vertex and mark it a member of component <paramref name="c"/>.
        /// </summary>
        /// <param name="v">vertex</param>
        /// <param name="c">component</param>
        private void Visit(int v, int c)
        {
            remaining--;
            component[v] = c;
            foreach (var w in g[v])
                if (component[w] == 0)
                    Visit(w, c);
        }

        /// <summary>
        /// Access the components each vertex belongs to.
        /// </summary>
        /// <returns>component labels</returns>
        public int[] GetComponents()
        {
            return Arrays.CopyOf(component, component.Length);
        }

        public int NumberOfComponents => components;
    }
}
