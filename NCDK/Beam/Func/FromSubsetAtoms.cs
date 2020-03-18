using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Given a chemical graph with 0 or more atoms. Convert that graph to one where
    /// all atoms are fully specified bracket atoms.
    /// </summary>
    // @author John May
    internal sealed class FromSubsetAtoms
        : AbstractFunction<Graph, Graph>
    {
        public override Graph Apply(Graph g)
        {
            Graph h = new Graph(g.Order);

            for (int u = 0; u < g.Order; u++)
            {
                h.AddAtom(FromSubset(g.GetAtom(u),
                                     BondOrderSum(g.GetEdges(u), g),
                                     g.Degree(u)));
                h.AddTopology(g.TopologyOf(u));
            }

            // edges are unchanged
            foreach (var e in g.Edges)
                h.AddEdge(e);

            return h;
        }

        private static int BondOrderSum(IList<Edge> es, Graph g)
        {
            int sum = 0;
            foreach (var e in es)
            {
                sum += e.Bond.Order;
            }
            return sum;
        }

        public static IAtom FromSubset(IAtom a, int sum, int deg)
        {
            // atom is already a non-subset atom
            if (!a.Subset)
                return a;

            Element e = a.Element;
            if (a.IsAromatic())
                sum++;
            int hCount = a.IsAromatic() ? e.NumOfAromaticImplicitHydrogens(sum)
                                      : e.NumOfImplicitHydrogens(sum);

            // XXX: if there was an odd number of availableElectrons there was an odd number
            // or aromatic bonds (usually 1 or 3) - if there was one it was
            // only a single bond it's likely a spouting from a ring - otherwise
            // someones making our life difficult (e.g. c1=cc=cc=c1) in which we
            // 'give' back 2 free availableElectrons for use indeterminacy the hCount
            //        int hCount = (electrons & 0x1) == 1 ? deg > 1 ? (electrons + 2) / 2
            //                                                      : electrons / 2
            //                                            : electrons / 2;

            return new AtomImpl.BracketAtom(-1,
                                            a.Element,
                                            hCount,
                                            0,
                                            0,
                                            a.IsAromatic());
        }
    }
}
