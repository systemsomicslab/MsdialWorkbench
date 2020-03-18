using NCDK.Common.Collections;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// see. http://en.wikipedia.org/wiki/Biconnected_component
    /// </summary>
    // @author John May
    internal class BiconnectedComponents
    {
        private readonly int[] depth;

        private readonly Graph g;
        private readonly Edge[] stack;
        private int nstack = 0;

        private readonly List<IReadOnlyList<Edge>> components = new List<IReadOnlyList<Edge>>(2);

        private readonly BitArray cyclic;
        private readonly BitArray simple;

        int count = 0;
        readonly int numfrags = 0;

        public BiconnectedComponents(Graph g)
            : this(g, true)
        {
        }

        public BiconnectedComponents(Graph g, bool storeComponents)
        {
            this.cyclic = new BitArray(g.Order);
            this.simple = new BitArray(g.Order);

            this.depth = new int[g.Order];
            this.g = g;
            this.stack = new Edge[g.Size];

            if (storeComponents)
            {
                for (int u = 0; count < g.Order; u++)
                {
                    if (depth[u] == 0)
                    {
                        VisitWithComp(u, null);
                        ++numfrags;
                    }
                }
            }
            else
            {
                for (int u = 0; count < g.Order; u++)
                {
                    if (depth[u] == 0)
                    {
                        Visit(u, null);
                        ++numfrags;
                    }
                }
            }
        }

        private int Visit(int u, Edge from)
        {
            depth[u] = ++count;
            int d = g.Degree(u);
            int lo = count + 1;

            while (--d >= 0)
            {
                Edge e = g.EdgeAt(u, d);
                if (e == from) continue;
                int v = e.Other(u);
                if (depth[v] == 0)
                {
                    int res = Visit(v, e);
                    if (res < lo)
                        lo = res;
                }
                else if (depth[v] < lo)
                {
                    lo = depth[v];
                }
            }
            if (lo <= depth[u])
                cyclic.Set(u, true);
            return lo;
        }

        private int VisitWithComp(int u, Edge from)
        {
            depth[u] = ++count;
            int j = g.Degree(u);
            int lo = count + 1;
            while (--j >= 0)
            {

                 Edge e = g.EdgeAt(u, j);
                if (e == from) continue;

                 int v = e.Other(u);
                if (depth[v] == 0)
                {
                    stack[nstack] = e;
                    ++nstack;
                    int tmp = VisitWithComp(v, e);
                    if (tmp == depth[u])
                        StoreWithComp(e);
                    else if (tmp > depth[u])
                        --nstack;
                    if (tmp < lo)
                        lo = tmp;
                }
                else if (depth[v] < depth[u])
                {
                    // back edge
                    stack[nstack] = e;
                    ++nstack;
                    if (depth[v] < lo)
                        lo = depth[v];
                }
            }
            return lo;
        }

        private void StoreWithComp(Edge e)
        {
            List<Edge> component = new List<Edge>(6);
            Edge f;

             BitArray tmp = new BitArray(g.Order);

            // count the number of unique vertices and edges
            int numEdges = 0;
            bool spiro = false;

            do
            {
                f = stack[--nstack];
                int v = f.Either();
                int w = f.Other(v);

                if (cyclic[v] || cyclic[w])
                    spiro = true;

                tmp.Set(v, true);
                tmp.Set(w, true);

                component.Add(f);
                numEdges++;
            } while (f != e);

            cyclic.Or(tmp);

            if (!spiro && BitArrays.Cardinality(tmp) == numEdges)
                simple.Or(tmp);

            components.Add(component);
        }

        public IReadOnlyList<IReadOnlyList<Edge>> Components => components;

        public BitArray Cyclic => cyclic;

        public bool Connected => numfrags < 2;
    }
}

