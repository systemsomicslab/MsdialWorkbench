using NCDK.Common.Collections;
using System;
using System.Collections;
using static NCDK.Beam.Element;

namespace NCDK.Beam
{
    /// <summary>
    /// Utility to localise aromatic bonds.
    /// </summary>
    // @author John May
    internal sealed class Localise
    {
        public static Graph GenerateKekuleForm(Graph g, BitArray subset, BitArray aromatic, bool inplace)
        {
            // make initial (empty) matching - then improve it, first
            // by matching the first edges we find, most of time this
            // gives us a perfect matching if not we maximise it
            // with Edmonds' algorithm

            Matching m = Matching.CreateEmpty(g);
            int n = BitArrays.Cardinality(subset);
            int nMatched = ArbitraryMatching.Initial(g, m, subset);
            if (nMatched < n)
            {
                if (n - nMatched == 2)
                    nMatched = ArbitraryMatching.AugmentOnce(g, m, nMatched, subset);
                if (nMatched < n)
                    nMatched = MaximumMatching.Maximise(g, m, nMatched, IntSet.FromBitArray(subset));
                if (nMatched < n)
                    throw new InvalidSmilesException("Could not Kekulise");
            }
            return inplace ? Assign(g, subset, aromatic, m)
                           : CopyAndAssign(g, subset, aromatic, m);
        }

        // invariant, m is a perfect matching
        private static Graph CopyAndAssign(Graph delocalised, BitArray subset, BitArray aromatic, Matching m)
        {
            Graph localised = new Graph(delocalised.Order);
            localised.SetFlags(delocalised.GetFlags() & ~Graph.HAS_AROM);
            for (int u = 0; u < delocalised.Order; u++)
            {
                localised.AddAtom(delocalised.GetAtom(u).AsAliphaticForm());
                localised.AddTopology(delocalised.TopologyOf(u));
                int d = delocalised.Degree(u);
                for (int j = 0; j < d; ++j)
                {
                    Edge e = delocalised.EdgeAt(u, j);
                    int v = e.Other(u);
                    if (v < u)
                    {
                        var aa = e.Bond;
                        if (aa == Bond.Single)
                        {
                            if (aromatic[u] && aromatic[v])
                            {
                                localised.AddEdge(Bond.Single.CreateEdge(u, v));
                            }
                            else
                            {
                                localised.AddEdge(Bond.Implicit.CreateEdge(u, v));
                            }
                        }
                        else if (aa == Bond.Aromatic)
                        {
                            if (subset[u] && m.Other(u) == v)
                            {
                                localised.AddEdge(Bond.DoubleAromatic.CreateEdge(u, v));
                            }
                            else if (aromatic[u] && aromatic[v])
                            {
                                localised.AddEdge(Bond.ImplicitAromatic.CreateEdge(u, v));
                            }
                            else
                            {
                                localised.AddEdge(Bond.Implicit.CreateEdge(u, v));
                            }
                        }
                        else if (aa == Bond.Implicit)
                        {
                            if (subset[u] && m.Other(u) == v)
                            {
                                localised.AddEdge(Bond.DoubleAromatic.CreateEdge(u, v));
                            }
                            else if (aromatic[u] && aromatic[v])
                            {
                                localised.AddEdge(Bond.ImplicitAromatic.CreateEdge(u, v));
                            }
                            else
                            {
                                localised.AddEdge(e);
                            }
                        }
                        else
                        {
                            localised.AddEdge(e);
                        }
                    }
                }
            }
            return localised;
        }

        // invariant, m is a perfect matching
        private static Graph Assign(Graph g, BitArray subset, BitArray aromatic, Matching m)
        {
            g.SetFlags(g.GetFlags() & ~Graph.HAS_AROM);
            for (int u = BitArrays.NextSetBit(aromatic, 0); u >= 0; u = BitArrays.NextSetBit(aromatic, u + 1))
            {
                g.SetAtom(u, g.GetAtom(u).AsAliphaticForm());
                int deg = g.Degree(u);
                for (int j = 0; j < deg; ++j)
                {
                    Edge e = g.EdgeAt(u, j);
                    int v = e.Other(u);
                    if (v < u)
                    {
                        var aa = e.Bond;
                        if (aa == Bond.Single)
                        {
                            if (aromatic[u] && aromatic[v])
                            {
                                e.SetBond(Bond.Single);
                            }
                            else
                            {
                                e.SetBond(Bond.Implicit);
                            }
                        }
                        else if (aa == Bond.Aromatic)
                        {
                            if (subset[u] && m.Other(u) == v)
                            {
                                e.SetBond(Bond.DoubleAromatic);
                                g.UpdateBondedValence(u, +1);
                                g.UpdateBondedValence(v, +1);
                            }
                            else if (aromatic[v])
                            {
                                e.SetBond(Bond.ImplicitAromatic);
                            }
                            else
                            {
                                e.SetBond(Bond.Implicit);
                            }
                        }
                        else if (aa == Bond.Implicit)
                        {
                            if (subset[u] && m.Other(u) == v)
                            {
                                e.SetBond(Bond.DoubleAromatic);
                                g.UpdateBondedValence(u, +1);
                                g.UpdateBondedValence(v, +1);
                            }
                            else if (aromatic[u] && aromatic[v])
                            {
                                e.SetBond(Bond.ImplicitAromatic);
                            }
                        }
                    }
                }
            }
            return g;
        }

        public static BitArray BuildSet(Graph g, BitArray aromatic)
        {

            BitArray undecided = new BitArray(g.Order);

            for (int v = 0; v < g.Order; v++)
            {
                if (g.GetAtom(v).IsAromatic())
                {
                    aromatic.Set(v, true);
                    if (!Predetermined(g, v))
                        undecided.Set(v, true);
                }
            }

            return undecided;
        }

        internal static bool Predetermined(Graph g, int v)
        {
            IAtom a = g.GetAtom(v);

            int q = a.Charge;
            int deg = g.Degree(v) + g.ImplHCount(v);

            if (g.BondedValence(v) > g.Degree(v))
            {
                int d = g.Degree(v);
                for (int j = 0; j < d; ++j)
                {
                    Edge e = g.EdgeAt(v, j);
                    if (e.Bond == Bond.Double)
                    {
                        if (q == 0 && (a.Element == Element.Nitrogen || (a.Element == Element.Sulfur && deg > 3)))
                            return false;
                        return true;
                    }
                    // triple or quadruple bond - we don't need to assign anymore p electrons
                    else if (e.Bond.Order > 2)
                    {
                        return true;
                    }
                }
            }

            // no pi bonds does the degree and charge indicate that
            // there can be no other pi bonds
            var aa = a.Element;
            if (aa == Boron)
            {
                return (q == 0) && deg == 3;
            }
            else if (aa == Carbon)
            {
                return (q == 1 || q == -1) && deg == 3;
            }
            else if (aa == Silicon || aa == Germanium)
            {
                return q < 0;
            }
            else if (aa == Nitrogen || aa == Phosphorus || aa == Arsenic || aa == Antimony)
            {
                if (q == 0)
                    return deg == 3 || deg > 4;
                else if (q == 1)
                    return deg > 3;
                else
                    return true;
            }
            else if (aa == Oxygen || aa == Sulfur || aa == Selenium || aa == Tellurium)
            {
                if (q == 0)
                    return deg == 2 || deg == 4 || deg > 5;
                else if (q == -1 || q == +1)
                    return deg == 3 || deg == 5 || deg > 6;
                else
                    return false;
            }
            return false;
        }

        internal static bool InSmallRing(Graph g, Edge e)
        {
            BitArray visit = new BitArray(g.Order);
            return InSmallRing(g, e.Either(), e.Other(e.Either()), e.Other(e.Either()), 1, new BitArray(g.Order));
        }

        internal static bool InSmallRing(Graph g, int v, int prev, int t, int d, BitArray visit)
        {
            if (d > 7)
                return false;
            if (v == t)
                return true;
            if (visit[v])
                return false;
            visit.Set(v, true);
            int deg = g.Degree(v);
            for (int j = 0; j < deg; ++j)
            {
                Edge e = g.EdgeAt(v, j);
                int w = e.Other(v);
                if (w == prev) continue;
                if (InSmallRing(g, w, v, t, d + 1, visit))
                {
                    return true;
                }
            }
            return false;
        }

        public static Graph Resonate(Graph g, BitArray cyclic, bool ordered)
        {
            BitArray subset = new BitArray(g.Order);

            for (int u = BitArrays.NextSetBit(cyclic, 0); u >= 0; u = BitArrays.NextSetBit(cyclic, u + 1))
            {
                // candidates must have a bonded
                // valence of one more than their degree
                // and in a ring
                int uExtra = g.BondedValence(u) - g.Degree(u);
                if (uExtra > 0)
                {

                    int other = -1;
                    Edge target = null;

                    int d = g.Degree(u);
                    for (int j = 0; j < d; ++j)
                    {

                        Edge e = g.EdgeAt(u, j);
                        int v = e.Other(u);
                        // check for bond validity
                        if (e.Bond.Order == 2)
                        {

                            int vExtra = g.BondedValence(v) - g.Degree(v);
                            if (cyclic[v] && vExtra > 0)
                            {

                                if (HasAdjDirectionalLabels(g, e, cyclic) && !InSmallRing(g, e))
                                {
                                    other = -1;
                                    break;
                                }
                                if (vExtra > 1 && HasAdditionalCyclicDoubleBond(g, cyclic, u, v))
                                {
                                    other = -1;
                                    break;
                                }
                                if (other == -1)
                                {
                                    other = v;  // first one
                                    target = e;
                                }
                                else
                                {
                                    other = -2; // found more than one
                                }
                            }
                            // only one double bond don't check any more
                            if (uExtra == 1)
                                break;
                        }
                    }

                    if (other >= 0)
                    {
                        subset.Set(u, true);
                        subset.Set(other, true);
                        target.SetBond(Bond.Implicit);
                    }
                }
            }

            if (!ordered)
                g = g.Sort(new Graph.CanOrderFirst());

            Matching m = Matching.CreateEmpty(g);
            int n = BitArrays.Cardinality(subset);
            int nMatched = ArbitraryMatching.Dfs(g, m, subset);
            if (nMatched < n)
            {
                if (n - nMatched == 2)
                    nMatched = ArbitraryMatching.AugmentOnce(g, m, nMatched, subset);
                if (nMatched < n)
                    nMatched = MaximumMatching.Maximise(g, m, nMatched, IntSet.FromBitArray(subset));
                if (nMatched < n)
                    throw new ApplicationException("Could not Kekulise");
            }

            // assign new double bonds
            for (int v = BitArrays.NextSetBit(subset, 0); v >= 0; v = BitArrays.NextSetBit(subset, v + 1))
            {
                int w = m.Other(v);
                subset.Set(w, false);
                g.CreateEdge(v, w).SetBond(Bond.Double);
            }

            return g;
        }

        /// <summary>
        /// Resonate double bonds in a cyclic system such that given a molecule with the same ordering
        /// produces the same resonance assignment. This procedure provides a canonical Kekulé
        /// representation for conjugated rings.
        /// </summary>
        /// <param name="g">graph</param>
        /// <returns>the input graph (same reference)</returns>
        public static Graph Resonate(Graph g)
        {
            return Resonate(g, new BiconnectedComponents(g).Cyclic, false);
        }

        private static bool HasAdditionalCyclicDoubleBond(Graph g, BitArray cyclic, int u, int v)
        {
            foreach (var f in g.GetEdges(v))
            {
                if (f.Bond == Bond.Double && f.Other(v) != u && cyclic[u])
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasAdjDirectionalLabels(Graph g, Edge e, BitArray cyclic)
        {
            int u = e.Either();
            int v = e.Other(u);
            return HasAdjDirectionalLabels(g, u, cyclic) && HasAdjDirectionalLabels(g, v, cyclic);
        }

        private static bool HasAdjDirectionalLabels(Graph g, int u, BitArray cyclic)
        {
            int d = g.Degree(u);
            for (int j = 0; j < d; ++j)
            {
                Edge f = g.EdgeAt(u, j);
                int v = f.Other(u);
                if (f.Bond.IsDirectional && cyclic[v])
                {
                    return true;
                }
            }
            return false;
        }

        public static Graph GenerateLocalise(Graph delocalised)
        {
            // nothing to do, return fast
            if (delocalised.GetFlags(Graph.HAS_AROM) == 0)
                return delocalised;

            BitArray aromatic = new BitArray(delocalised.Order);
            BitArray subset = BuildSet(delocalised, aromatic);
            if (HasOddCardinality(subset))
                throw new InvalidSmilesException("a valid kekulé structure could not be assigned");
            return Localise.GenerateKekuleForm(delocalised, subset, aromatic, false);
        }

        public static Graph LocaliseInPlace(Graph delocalised)
        {
            // nothing to do, return fast
            if (delocalised.GetFlags(Graph.HAS_AROM) == 0)
                return delocalised;

            BitArray aromatic = new BitArray(delocalised.Order);
            BitArray subset = BuildSet(delocalised, aromatic);
            if (HasOddCardinality(subset))
                throw new InvalidSmilesException("a valid kekulé structure could not be assigned");
            return Localise.GenerateKekuleForm(delocalised, subset, aromatic, true);
        }

        private static bool HasOddCardinality(BitArray s)
        {
            return (BitArrays.Cardinality(s) & 0x1) == 1;
        }
    }
}
