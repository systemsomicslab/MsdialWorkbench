using System;

namespace NCDK.Beam
{
    internal static class AtomMapsRenumberer
    {
        private sealed class State
        {
            public Graph g;
            public bool[] visit;
            public int[] map;
            public int nMaps;

            public State(Graph g, int maxidx)
            {
                this.g = g;
                this.visit = new bool[g.Order];
                this.map = new int[maxidx + 1];
                this.nMaps = 0;
            }
        }

        private static void Traverse(State s, int idx)
        {
            s.visit[idx] = true;
            var mapIdx = s.g.GetAtom(idx).AtomClass;
            if (mapIdx != 0)
            {
                if (s.map[mapIdx] == 0)
                    s.map[mapIdx] = ++s.nMaps;
                mapIdx = s.map[mapIdx];
                s.g.SetAtom(idx, AtomBuilder.FromExisting(s.g.GetAtom(idx)).AtomClass(mapIdx).Build());
            }
            foreach (var e in s.g.GetEdges(idx))
            {
                int nbr = e.Other(idx);
                if (!s.visit[nbr])
                    Traverse(s, nbr);
            }
        }

        public static void Renumber(Graph g)
        {
            int maxMapIdx = 0;
            for (int i = 0; i < g.Order; i++)
                maxMapIdx = Math.Max(maxMapIdx, g.GetAtom(i).AtomClass);
            if (maxMapIdx == 0)
                return;
            var state = new State(g, maxMapIdx);
            for (int i = 0; i < g.Order; i++)
                if (!state.visit[i])
                    Traverse(state, i);
        }
    }
}
