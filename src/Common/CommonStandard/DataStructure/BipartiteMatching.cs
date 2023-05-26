using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.DataStructure
{
    public sealed class BipartiteMatching
    {
        private readonly int _v;
        private readonly List<int>[] _g;

        public BipartiteMatching(int v) {
            _v = v;
            _g = Enumerable.Repeat(0, v).Select(_ => new List<int>()).ToArray();
        }

        public void AddEdge(int u, int v) {
            _g[u].Add(v);
            _g[v].Add(u);
        }

        public int Match() {
            int res = 0;
            var matches = Enumerable.Repeat(-1, _v).ToArray();
            for (int v = 0; v < _v; v++) {
                if (matches[v] < 0) {
                    var used = new bool[_v];
                    if (Dfs(v, used, matches)) {
                        ++res;
                    }
                }
            }
            return res;
        }

        private bool Dfs(int v, bool[] used, int[] matches) {
            used[v] = true;
            for (int i = 0; i < _g[v].Count; i++) {
                int u = _g[v][i], w = matches[u];
                if (w < 0 || !used[w] && Dfs(w, used, matches)) {
                    matches[v] = u;
                    matches[u] = v;
                    return true;
                }
            }
            return false;
        }
    }
}
