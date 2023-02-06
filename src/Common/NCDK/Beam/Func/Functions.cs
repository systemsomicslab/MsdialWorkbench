using System;
using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Collection of utilities for transforming chemical graphs.
    /// </summary>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    sealed class Functions
    {
        // convert to atom-based double-bond configurations
        private static readonly ToTrigonalTopology ttt = new ToTrigonalTopology();

        // convert to bond-based double-bond configuration
        private static readonly FromTrigonalTopology ftt = new FromTrigonalTopology();

        // bond label conversion -> to implicit
        private static readonly ExplicitToImplicit eti = new ExplicitToImplicit();

        // bond label conversion -> to explicit
        private static readonly ImplicitToExplicit ite = new ImplicitToExplicit();

        // use organic subset
        private static readonly ToSubsetAtoms tsa = new ToSubsetAtoms();

        // expand organic subset
        private static readonly FromSubsetAtoms fsa = new FromSubsetAtoms();

        // normalise directional labels
        private static readonly NormaliseDirectionalLabels ndl = new NormaliseDirectionalLabels();

        /// non-instantiable
        private Functions()
        {
        }

        /// <summary>
        /// Randomise the atom Order of the provided chemical graph.
        /// </summary>
        /// <param name="g">chemical graph</param>
        /// <returns>a copy of the original graph with the Order of the atoms randomised</returns>
        public static Graph Randomise(Graph g)
        {
            return g.Permute(Random(g.Order));
        }

        /// <summary>
        /// Reverse the atom Order of the provided chemical graph.
        /// </summary>
        /// <param name="g">chemical graph</param>
        /// <returns>a copy of the original graph with the Order of the atoms reversed</returns>
        public static Graph Reverse(Graph g)
        {
            return g.Permute(Reverse(g.Order));
        }

        /// <summary>
        /// Convert any directional bond based stereo configuration to atom-based
        /// specification.
        /// </summary>
        /// <param name="g">chemical graph graph</param>
        /// <returns>a copy of the original graph but with directional bonds removed
        ///         and atom-based double-bond stereo configruation.</returns>
        public static Graph AtomBasedDBStereo(Graph g)
        {
            return eti.Apply(ttt.Apply(ite.Apply(g)));
        }

        /// <summary>
        /// Convert a graph with atom-based double-bond stereo configuration to
        /// bond-based specification (direction Up and Down bonds).
        /// </summary>
        /// <param name="g">chemical graph graph</param>
        /// <returns>a copy of the original graph but with bond-based
        ///         stereo-chemistry</returns>
        public static Graph BondBasedDBStereo(Graph g)
        {
            return eti.Apply(ftt.Apply(ite.Apply(g)));
        }

        /// <summary>
        /// Expand a graph with organic subsets to one with specified atom
        /// properties.
        /// </summary>
        /// <param name="g">a chemical graph</param>
        /// <returns>the chemical graph expanded</returns>
        public static Graph Expand(Graph g)
        {
            return eti.Apply(fsa.Apply(ite.Apply(g)));
        }

        /// <summary>
        /// Collapse a graph with specified atom properties to one with organic
        /// subset atoms.
        /// </summary>
        /// <param name="g">a chemical graph</param>
        /// <returns>the chemical graph expanded</returns>
        public static Graph Collapse(Graph g)
        {
            return eti.Apply(tsa.Apply(ite.Apply(g)));
        }

        public static Graph NormaliseDirectionalLabels(Graph g)
        {
            return ndl.Apply(g);
        }

        private static int[] Ident(int n)
        {
            int[] p = new int[n];
            for (int i = 0; i < n; i++)
                p[i] = i;
            return p;
        }

        private class S : IComparer<int>
        {
            private readonly long[] labels;

            public S(long[] labels)
            {
                this.labels = labels;
            }

            public int Compare(int i, int j)
            {
                if (labels[i] > labels[j])
                    return +1;
                else if (labels[i] < labels[j])
                    return -1;
                return 0;
            }
        }

        /// <summary>
        /// Apply the labeling <paramref name="labels"/> to the graph <paramref name="g"/>. The labels
        /// are converted to a permutation which is then applied to the Graph and
        /// rearrange it's vertex Order.
        /// </summary>
        /// <param name="g">the graph to permute</param>
        /// <param name="labels">the vertex labels - for example from a cannibalisation algorithm</param>
        /// <returns>a copy of the original graph with it's vertices permuted by the labelling</returns>
        public static Graph Canonicalize(Graph g, long[] labels)
        {
            int[] is_ = new int[g.Order];

            for (int i = 0; i < is_.Length; i++)
                is_[i] = i;

            // TODO: replace with radix sort (i.e. using a custom comparator)
            Array.Sort(is_, new S(labels));

            int[] p = new int[g.Order];
            for (int i = 0; i < is_.Length; i++)
                p[is_[i]] = i;
            return g.Permute(p);
        }

        /// <summary>
        /// Renumbers atom-atom maps using a depth-first traversal. Note this function modifies the input graph.
        /// </summary>
        /// <param name="g">the graph</param>
        /// <returns>the input graph</returns>
        public static Graph RenumberAtomMaps(Graph g)
        {
            AtomMapsRenumberer.Renumber(g);
            return g;
        }

        private static int[] Random(int n)
        {
            int[] p = Ident(n);
            Random rnd = new Random();
            for (int i = n; i > 1; i--)
                Swap(p, i - 1, rnd.Next(i));
            return p;
        }

        private static int[] Reverse(int n)
        {
            int[] p = new int[n];
            for (int i = 0; i < n; i++)
                p[i] = n - i - 1;
            return p;
        }

        // inverse of permutation
        private static int[] Inv(int[] p)
        {
            int[] q = (int[])p.Clone();
            for (int i = 0; i < p.Length; i++)
                q[p[i]] = i;
            return q;
        }

        private static void Swap(int[] p, int i, int j)
        {
            int tmp = p[i];
            p[i] = p[j];
            p[j] = tmp;
        }
    }
}
