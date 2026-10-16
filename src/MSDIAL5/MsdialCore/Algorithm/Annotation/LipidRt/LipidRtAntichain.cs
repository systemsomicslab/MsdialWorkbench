using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.LipidRt
{
    /// <summary>
    /// The exact maximum-weight consistent subset of a discordance order (weighted Dilworth/Konig by max-flow).
    /// </summary>
    /// <remarks>
    /// Within one class, element i dominates j when C_i &gt;= C_j, DBe_i &lt;= DBe_j and O_i &lt;= O_j (not all equal):
    /// i is predicted to elute later. The pair is discordant when RT_i &lt; RT_j - eps. Discordance is a strict
    /// partial order, so a consistent subset (no discordant pair inside) is an antichain of it, and the maximum-weight
    /// consistent subset is a maximum-weight antichain: W* = sum(w) - maxflow on the network s -&gt; L_i (w_i),
    /// L_i -&gt; R_j (infinite) for each discordant pair, R_j -&gt; t (w_j).
    ///
    /// Membership comes from one maximum flow and residual reachability: i is in EVERY maximum antichain when L_i is
    /// reachable from s and R_i reaches t; in SOME when the residual closure of {s, L_i} contains neither t nor R_i;
    /// otherwise in NONE. Membership is defined over all maximum antichains, so the result does not depend on the order
    /// of the input or on which maximum flow the algorithm happens to find.
    ///
    /// Margins in weight units: e_i = W* - W*(without i) for an element in every maximum antichain, and
    /// d_i = W* - (w_i + W*(without i and its discordant partners)) for an element in none. Each margin is a maximum
    /// flow on the network with some elements removed; it starts from the component's maximum flow with the flow
    /// through the removed elements cancelled (every path is s -&gt; L_a -&gt; R_b -&gt; t, so cancelling is local),
    /// which gives the same exact value as a flow from scratch at a fraction of the cost.
    ///
    /// Integer capacities keep every status and margin exact. One Dinic network per connected component.
    /// </remarks>
    public static class LipidRtAntichain
    {
        private const long Infinity = 1L << 60;

        public sealed class Result
        {
            internal Result(LipidRtFeatureStatus[] status, long[] essential, long[] deficit, long weight, int[] component, HashSet<int>[] partners) {
                Status = status;
                Essential = essential;
                Deficit = deficit;
                Weight = weight;
                Component = component;
                Partners = partners;
            }

            public IReadOnlyList<LipidRtFeatureStatus> Status { get; }

            /// <summary>e_i for core elements, the own weight for free elements, 0 otherwise.</summary>
            public IReadOnlyList<long> Essential { get; }

            /// <summary>d_i for excluded elements, 0 otherwise.</summary>
            public IReadOnlyList<long> Deficit { get; }

            /// <summary>The maximum consistent weight W* of the whole set.</summary>
            public long Weight { get; }

            /// <summary>The connected component of the discordance graph, or -1 for a free element.</summary>
            public IReadOnlyList<int> Component { get; }

            /// <summary>The discordant partners of each element, in both directions.</summary>
            public IReadOnlyList<HashSet<int>> Partners { get; }
        }

        /// <summary>
        /// Analyses <paramref name="n"/> elements. <paramref name="pairs"/> are the discordant pairs (i, j) with i the
        /// predicted-later element; the relation must be transitively closed, which discordance is.
        /// <paramref name="weights"/> are positive integers.
        /// </summary>
        public static Result Analyze(int n, IEnumerable<(int Later, int Earlier)> pairs, IReadOnlyList<long> weights, bool margins = true) {
            var below = new List<int>[n];
            var partners = new HashSet<int>[n];
            for (int i = 0; i < n; i++) {
                below[i] = new List<int>();
                partners[i] = new HashSet<int>();
            }
            var seenPairs = new HashSet<(int, int)>();
            foreach (var (i, j) in pairs) {
                if (!seenPairs.Add((i, j))) {
                    continue;
                }
                below[i].Add(j);
                partners[i].Add(j);
                partners[j].Add(i);
            }
            // connected components of the comparability graph
            var component = Enumerable.Repeat(-1, n).ToArray();
            var componentCount = 0;
            for (int i = 0; i < n; i++) {
                if (component[i] >= 0 || partners[i].Count == 0) {
                    continue;
                }
                var stack = new Stack<int>();
                stack.Push(i);
                component[i] = componentCount;
                while (stack.Count > 0) {
                    var u = stack.Pop();
                    foreach (var v in partners[u]) {
                        if (component[v] < 0) {
                            component[v] = componentCount;
                            stack.Push(v);
                        }
                    }
                }
                componentCount++;
            }

            var status = new LipidRtFeatureStatus[n];
            var essential = new long[n];
            var deficit = new long[n];
            long total = 0;
            for (int i = 0; i < n; i++) {
                status[i] = LipidRtFeatureStatus.Free;
                essential[i] = weights[i];
                if (partners[i].Count == 0) {
                    total += weights[i];
                }
            }
            var members = new List<int>[componentCount];
            for (int c = 0; c < componentCount; c++) {
                members[c] = new List<int>();
            }
            for (int i = 0; i < n; i++) {
                if (component[i] >= 0) {
                    members[component[i]].Add(i);
                }
            }
            foreach (var index in members) {
                var network = new ComponentNetwork(index, weights, below);
                var w = network.Sum - network.MaxFlow();
                total += w;
                var fromSource = network.ReachFrom(network.Source);
                var toSink = network.CoReach(network.Sink);
                var m = index.Count;
                for (int k = 0; k < m; k++) {
                    var x = index[k];
                    int l = k, r = m + k;
                    if (fromSource[l] && toSink[r]) {
                        status[x] = LipidRtFeatureStatus.Core;
                        continue;
                    }
                    bool some;
                    if (fromSource[l]) {
                        some = !fromSource[r];
                    }
                    else {
                        var closure = network.ReachFrom(l);
                        for (int v = 0; v < closure.Length; v++) {
                            closure[v] |= fromSource[v];
                        }
                        some = !closure[network.Sink] && !closure[r];
                    }
                    status[x] = some ? LipidRtFeatureStatus.Ambiguous : LipidRtFeatureStatus.Excluded;
                }
                if (margins) {
                    network.Snapshot();
                    for (int k = 0; k < m; k++) {
                        var x = index[k];
                        if (status[x] == LipidRtFeatureStatus.Core) {
                            essential[x] = w - network.WeightWithout(new[] { k });
                        }
                        else {
                            essential[x] = 0;
                        }
                        if (status[x] == LipidRtFeatureStatus.Excluded) {
                            var removed = new List<int> { k };
                            for (int j = 0; j < m; j++) {
                                if (partners[x].Contains(index[j])) {
                                    removed.Add(j);
                                }
                            }
                            deficit[x] = w - (weights[x] + network.WeightWithout(removed));
                        }
                    }
                }
            }
            return new Result(status, essential, deficit, total, component, partners);
        }

        /// <summary>
        /// The network of one component: s -&gt; L_k (w), L_k -&gt; R_j (infinite) per discordant pair, R_k -&gt; t (w), for
        /// the component's elements k = 0..m-1. L_k is node k, R_k is node m + k.
        /// </summary>
        private sealed class ComponentNetwork
        {
            private readonly int _m;
            private readonly long[] _weights;
            private readonly int[] _sourceEdge; // s -> L_k
            private readonly int[] _sinkEdge;   // R_k -> t
            private readonly Dinic _dinic;
            private long[] _snapshot;

            public ComponentNetwork(List<int> index, IReadOnlyList<long> weights, List<int>[] below) {
                _m = index.Count;
                _weights = index.Select(x => weights[x]).ToArray();
                var position = new Dictionary<int, int>(_m);
                for (int k = 0; k < _m; k++) {
                    position[index[k]] = k;
                }
                _dinic = new Dinic(2 * _m + 2);
                _sourceEdge = new int[_m];
                _sinkEdge = new int[_m];
                for (int k = 0; k < _m; k++) {
                    _sourceEdge[k] = _dinic.Add(Source, k, _weights[k]);
                    _sinkEdge[k] = _dinic.Add(_m + k, Sink, _weights[k]);
                    Sum += _weights[k];
                }
                for (int k = 0; k < _m; k++) {
                    foreach (var j in below[index[k]]) {
                        _dinic.Add(k, _m + position[j], Infinity);
                    }
                }
                _dinic.Seal();
            }

            public int Source => 2 * _m;
            public int Sink => 2 * _m + 1;
            public long Sum { get; }

            public long MaxFlow() => _dinic.MaxFlow(Source, Sink);

            public bool[] ReachFrom(int start) => _dinic.ReachFrom(start);

            public bool[] CoReach(int target) => _dinic.CoReach(target);

            /// <summary>Keeps the residual of the component's maximum flow as the start of every margin flow.</summary>
            public void Snapshot() {
                _snapshot = _dinic.Capacities();
            }

            /// <summary>The maximum antichain weight of the component without the elements <paramref name="removed"/>.</summary>
            public long WeightWithout(IReadOnlyCollection<int> removed) {
                _dinic.Restore(_snapshot);
                var gone = new bool[_m];
                foreach (var k in removed) {
                    gone[k] = true;
                }
                // Cancel the flow of every path s -> L_a -> R_b -> t through a removed element, keeping the flow of the
                // remaining paths, then cut the removed elements off.
                for (int a = 0; a < _m; a++) {
                    foreach (var e in _dinic.Edges(a)) {
                        var b = _dinic.Head(e) - _m;
                        if (b < 0 || b >= _m || !_dinic.IsForward(e)) {
                            continue; // not an L_a -> R_b edge
                        }
                        var flow = _dinic.Flow(e);
                        if (flow == 0 || (!gone[a] && !gone[b])) {
                            continue;
                        }
                        if (!gone[a]) {
                            _dinic.Push(_sourceEdge[a], -flow);
                        }
                        if (!gone[b]) {
                            _dinic.Push(_sinkEdge[b], -flow);
                        }
                        _dinic.Push(e, -flow);
                    }
                }
                long sum = 0;
                for (int k = 0; k < _m; k++) {
                    if (gone[k]) {
                        _dinic.Isolate(k);
                        _dinic.Isolate(_m + k);
                    }
                    else {
                        sum += _weights[k];
                    }
                }
                _dinic.MaxFlow(Source, Sink);
                long flowValue = 0;
                for (int k = 0; k < _m; k++) {
                    if (!gone[k]) {
                        flowValue += _dinic.Flow(_sourceEdge[k]);
                    }
                }
                return sum - flowValue;
            }
        }

        /// <summary>Dinic's maximum flow on integer capacities. Edge e and its reverse are e and e ^ 1.</summary>
        private sealed class Dinic
        {
            private readonly int _n;
            private readonly List<int>[] _building;
            private readonly List<int> _toList = new List<int>();
            private readonly List<long> _capList = new List<long>();
            private int[][] _graph;
            private int[] _to;
            private long[] _capacity;
            private long[] _original;

            public Dinic(int n) {
                _n = n;
                _building = new List<int>[n];
                for (int i = 0; i < n; i++) {
                    _building[i] = new List<int>();
                }
            }

            public int Add(int u, int v, long capacity) {
                var e = _toList.Count;
                _building[u].Add(e);
                _toList.Add(v);
                _capList.Add(capacity);
                _building[v].Add(e + 1);
                _toList.Add(u);
                _capList.Add(0);
                return e;
            }

            public void Seal() {
                _graph = _building.Select(l => l.ToArray()).ToArray();
                _to = _toList.ToArray();
                _capacity = _capList.ToArray();
                _original = _capList.ToArray();
            }

            public IReadOnlyList<int> Edges(int u) => _graph[u];
            public int Head(int e) => _to[e];
            public bool IsForward(int e) => (e & 1) == 0;

            /// <summary>The flow on a forward edge.</summary>
            public long Flow(int e) => _original[e] - _capacity[e];

            /// <summary>Changes the flow on forward edge <paramref name="e"/> by <paramref name="amount"/>.</summary>
            public void Push(int e, long amount) {
                _capacity[e] -= amount;
                _capacity[e ^ 1] += amount;
            }

            /// <summary>Cuts every edge at <paramref name="u"/>, in both directions.</summary>
            public void Isolate(int u) {
                foreach (var e in _graph[u]) {
                    _capacity[e] = 0;
                    _capacity[e ^ 1] = 0;
                }
            }

            public long[] Capacities() => (long[])_capacity.Clone();

            public void Restore(long[] capacities) => Array.Copy(capacities, _capacity, capacities.Length);

            public long MaxFlow(int s, int t) {
                long flow = 0;
                var level = new int[_n];
                var iterator = new int[_n];
                var queue = new Queue<int>();
                var path = new List<int>();
                while (true) {
                    for (int i = 0; i < _n; i++) {
                        level[i] = -1;
                    }
                    level[s] = 0;
                    queue.Enqueue(s);
                    while (queue.Count > 0) {
                        var u = queue.Dequeue();
                        foreach (var e in _graph[u]) {
                            if (_capacity[e] > 0 && level[_to[e]] < 0) {
                                level[_to[e]] = level[u] + 1;
                                queue.Enqueue(_to[e]);
                            }
                        }
                    }
                    if (level[t] < 0) {
                        return flow;
                    }
                    Array.Clear(iterator, 0, _n);
                    while (true) {
                        var f = Augment(s, t, level, iterator, path);
                        if (f == 0) {
                            break;
                        }
                        flow += f;
                    }
                }
            }

            // Iterative depth-first search along the level graph; returns the bottleneck pushed, 0 if none.
            private long Augment(int s, int t, int[] level, int[] iterator, List<int> path) {
                path.Clear();
                var u = s;
                while (true) {
                    if (u == t) {
                        var f = Infinity;
                        foreach (var e in path) {
                            if (_capacity[e] < f) {
                                f = _capacity[e];
                            }
                        }
                        foreach (var e in path) {
                            _capacity[e] -= f;
                            _capacity[e ^ 1] += f;
                        }
                        return f;
                    }
                    var advanced = false;
                    var edges = _graph[u];
                    while (iterator[u] < edges.Length) {
                        var e = edges[iterator[u]];
                        if (_capacity[e] > 0 && level[_to[e]] == level[u] + 1) {
                            path.Add(e);
                            u = _to[e];
                            advanced = true;
                            break;
                        }
                        iterator[u]++;
                    }
                    if (!advanced) {
                        level[u] = -1; // dead end
                        if (path.Count == 0) {
                            return 0;
                        }
                        var last = path[path.Count - 1];
                        path.RemoveAt(path.Count - 1);
                        u = _to[last ^ 1];
                        iterator[u]++;
                    }
                }
            }

            /// <summary>Nodes reachable from <paramref name="start"/> in the residual graph.</summary>
            public bool[] ReachFrom(int start) {
                var seen = new bool[_n];
                seen[start] = true;
                var queue = new Queue<int>();
                queue.Enqueue(start);
                while (queue.Count > 0) {
                    var u = queue.Dequeue();
                    foreach (var e in _graph[u]) {
                        var v = _to[e];
                        if (_capacity[e] > 0 && !seen[v]) {
                            seen[v] = true;
                            queue.Enqueue(v);
                        }
                    }
                }
                return seen;
            }

            /// <summary>Nodes that can reach <paramref name="target"/> in the residual graph.</summary>
            public bool[] CoReach(int target) {
                var seen = new bool[_n];
                seen[target] = true;
                var queue = new Queue<int>();
                queue.Enqueue(target);
                while (queue.Count > 0) {
                    var v = queue.Dequeue();
                    foreach (var e in _graph[v]) {
                        var u = _to[e];
                        // the edge u -> v is the reverse of e
                        if (!seen[u] && _capacity[e ^ 1] > 0) {
                            seen[u] = true;
                            queue.Enqueue(u);
                        }
                    }
                }
                return seen;
            }
        }
    }
}
