using System;

namespace NCDK.Beam
{
    // @author John May 
    internal sealed class BondBasedConfiguration
    {
        public static Configuration.ConfigurationDoubleBond ConfigurationOf(Graph g,
                                                        int x, int u, int v, int y)
        {
            Edge e = g.CreateEdge(u, v);

            if (e.Bond != Bond.Double)
                throw new ArgumentException("atoms u,v are not labelled as a double bond");

            Edge e1 = g.CreateEdge(u, x);
            Edge e2 = g.CreateEdge(v, y);

            Bond b1 = e1.GetBond(u);
            Bond b2 = e2.GetBond(v);

            if (b1 == Bond.Implicit || b1 == Bond.Single)
                return Configuration.ConfigurationDoubleBond.Unspecified;
            if (b2 == Bond.Implicit || b2 == Bond.Single)
                return Configuration.ConfigurationDoubleBond.Unspecified;

            return b1 == b2 ? Configuration.ConfigurationDoubleBond.Together
                            : Configuration.ConfigurationDoubleBond.Opposite;
        }
    }
}
