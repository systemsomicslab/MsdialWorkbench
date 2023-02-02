using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Given a chemical graph with 0 or more atoms. Convert that graph to one where
    /// fully specified bracket atoms which can be specified as organic subsets.
    /// </summary>
    // @author John May
    internal sealed class ToSubsetAtoms : AbstractFunction<Graph, Graph>
    {
        public override Graph Apply(Graph g)
        {
            Graph h = new Graph(g.Order);

            for (int u = 0; u < g.Order; u++)
            {

                // only attempt subset conversion if no known topology
                Topology t = g.TopologyOf(u);

                if (t.Type == Configuration.ConfigurationType.None)
                {
                    h.AddAtom(ToSubset(g.GetAtom(u), g, u));
                }
                else
                {
                    h.AddAtom(g.GetAtom(u));
                    h.AddTopology(t);
                }
            }

            // edges are unchanged
            foreach (var e in g.Edges)
                h.AddEdge(e);

            return h;
        }

        public static IAtom ToSubset(IAtom a, Graph g, int u)
        {
            // atom is already a subset atom
            if (a.Subset)
                return a;

            // element is not organic and thus cannot be part of the subset
            if (!a.Element.IsOrganic())
                return a;

            // if any of these values are set the atom cannot be a subset atom
            if (a.Charge != 0 || a.AtomClass != 0 || a.Isotope >= 0)
                return a;

            IAtom subset = a.IsAromatic() ? AtomImpl.AromaticSubset.OfElement(a.Element)
                                       : AtomImpl.AliphaticSubset.OfElement(a.Element);

            // does the implied availableElectrons from the bond Order sum match that
            // which was stored - if aromatic we only check the lowest valence state
            int impliedHCount = subset.GetNumberOfHydrogens(g, u);

            // mismatch in number of hydrogens we must write this as a bracket atom
            return impliedHCount != a.NumOfHydrogens ? a : subset;
        }
    }
}
