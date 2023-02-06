using CompMs.Common.Extension;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.DataStructure
{
    public class KdTree<T>
    {
        private readonly Node root;
        private readonly int maxDepth;
        private readonly Func<T, double>[] funcs;

        private KdTree(Node root, IDistanceCalculator calculator, Func<T, double>[] funcs) {
            this.root = root;
            this.funcs = funcs;
            maxDepth = funcs.Length;
            Calculator = calculator;
        }

        public IDistanceCalculator Calculator { get; }

        public static KdTree<T> Build(IEnumerable<T> source, params Func<T, double>[] funcs) {
            return Build(source, new EuclideanDistance(), funcs);
        }

        public static KdTree<T> Build(IEnumerable<T> source, IDistanceCalculator calculator, params Func<T, double>[] funcs) {
            var s = source.ToList();
            var m = s.Count;
            var xs = new Node[m];
            for(int i = 0; i < m; i++) {
                xs[i] = new Node(s[i], funcs);
            }

            var n = funcs.Length;
            return new KdTree<T>(Recurse(xs, 0, xs.Length, 0, n), calculator, funcs);
        }

        private static Node Recurse(Node[] xs, int left, int right, int depth, int maxDepth) {
            if (left >= right)
                return Node.nil;

            int center = GetMedian(xs, left, right - left, depth % maxDepth);
            var centernode = xs[center];
            centernode.Depth = depth;
            centernode.Left = Recurse(xs, left, center, depth + 1, maxDepth);
            centernode.Right = Recurse(xs, center + 1, right, depth + 1, maxDepth);
            return centernode;
        }

        private static int GetMedian(Node[] xs, int index, int length, int depth) {
            if (length == 0)
                throw new ArgumentException("length is zero.");
            if (index < 0 || index + length > xs.Length)
                throw new ArgumentException("index is out of array.");
            if (length == 1)
                return index;
            Array.Sort(xs, index, length, GetComparer(depth));
            return index + length / 2;
        }

        public T NearestNeighbor(T data) {
            var ys = funcs.Select(func => func(data)).ToArray();
            var minDistance = double.MaxValue;
            (var node, var _) = NearestNeighborImpl(ys, root, minDistance);
            return node.data;
        }

        public T NearestNeighbor(double[] ys) {
            var minDistance = double.MaxValue;
            (var node, var _) = NearestNeighborImpl(ys, root, minDistance);
            return node.data;
        }

        private (Node, double) NearestNeighborImpl(double[] ys, Node current, double minDistance) {
            if (current == Node.nil) {
                return (current, double.MaxValue);
            }

            var depth = current.Depth;
            Node child1 = current.Left, child2 = current.Right;
            if (current.xs[depth % maxDepth] < ys[depth % maxDepth]) {
                child1 = current.Right;
                child2 = current.Left;
            }

            var nn = current;
            var d = Calculator.Distance(ys, current.xs);
            minDistance = Math.Min(minDistance, d);

            (var nn1, var d1) = NearestNeighborImpl(ys, child1, minDistance);
            if (d1 < d) {
                nn = nn1;
                d = d1;
                minDistance = Math.Min(minDistance, d);
            }

            if (Calculator.RoughDistance(ys, current.xs, depth % maxDepth) <= minDistance) {
                (var nn2, var d2) = NearestNeighborImpl(ys, child2, minDistance);
                if (d2 < d) {
                    nn = nn2;
                    d = d2;
                }
            }

            return (nn, d);
        }

        public List<T> RangeSearch(IReadOnlyList<double> mins, IReadOnlyList<double> maxs) {
            return RangeSearchImpl(mins, maxs, root).ToList();
        }

        private IEnumerable<T> RangeSearchImpl(IReadOnlyList<double> mins, IReadOnlyList<double> maxs, Node current) {
            if (current == Node.nil) {
                yield break;
            }

            var depth = current.Depth;
            var v = current.xs[depth % maxDepth];
            if (Isin(current.xs, mins, maxs)) {
                yield return current.data;
            }
            if (mins[depth % maxDepth] <= v) {
                foreach (var d in RangeSearchImpl(mins, maxs, current.Left)) {
                    yield return d;
                }
            }
            if (v <= maxs[depth % maxDepth]) {
                foreach (var d in RangeSearchImpl(mins, maxs, current.Right)) {
                    yield return d;
                }
            }
        }

        private static bool Isin(double[] xs, IEnumerable<double> mins, IEnumerable<double> maxs) {
            return xs.Zip(mins, maxs, (x, min, max) => min <= x && x <= max).All(v => v);
        }

        public List<T> NeighborSearch(T data, double threshold) {
            var ys = funcs.Select(func => func(data)).ToArray();
            return NeighborSearchImpl(ys, root, threshold).ToList();
        }

        private IEnumerable<T> NeighborSearchImpl(double[] ys, Node current, double threshold) {
            if (current == Node.nil) {
                yield break;
            }

            var d = Calculator.Distance(ys, current.xs);
            if (d <= threshold) {
                yield return current.data;
            }

            var depth = current.Depth;
            Node child1 = current.Left, child2 = current.Right;
            if (current.xs[depth % maxDepth] < ys[depth % maxDepth]) {
                child1 = current.Right;
                child2 = current.Left;
            }
            
            foreach (var v in NeighborSearchImpl(ys, child1, threshold)) {
                yield return v;
            }
            if (Calculator.RoughDistance(ys, current.xs, depth % maxDepth) <= threshold) {
                foreach (var v in NeighborSearchImpl(ys, child2, threshold)) {
                    yield return v;
                }
            }
        }

        private static ConcurrentDictionary<int, IComparer<Node>> comparersCache = new ConcurrentDictionary<int, IComparer<Node>>();
        private static IComparer<Node> GetComparer(int depth) {
            return comparersCache.GetOrAdd(depth, i => new NodeDepthComparer(i));
        }

        class Node
        {
            public static readonly Node nil;
            static Node() {
                nil = new Node();
                nil.Left = nil;
                nil.Right = nil;
            }

            private Node() {
                data = default;
                xs = new double[0];
                Depth = 1 << 60;
            }

            public Node(T data, Func<T, double>[] funcs) {
                this.data = data;
                xs = funcs.Select(func => func(data)).ToArray();
            }

            public Node Left { get; set; }
            public Node Right { get; set; }

            public int Depth { get; set; }

            internal readonly T data;
            internal readonly double[] xs;
        }

        class NodeDepthComparer : IComparer<Node>
        {
            private readonly int depth;
            public NodeDepthComparer(int depth) {
                this.depth = depth;
            }

            public int Compare(Node x, Node y) {
                return x.xs[depth].CompareTo(y.xs[depth]);
            }
        }
    }

    public interface IDistanceCalculator
    {
        // for all xs, ys, i, RoughDistance(xs, ys, i) <= Distance(xs, ys).
        double Distance(double[] xs, double[] ys);
        double RoughDistance(double[] xs, double[] ys, int i);
    }

    public class EuclideanDistance : IDistanceCalculator
    {
        public double Distance(double[] xs, double[] ys) {
            return Mathematics.Statistics.StatisticsMathematics.CalculateEuclideanDistance(xs, ys);
        }

        public double RoughDistance(double[] xs, double[] ys, int i) {
            return Math.Abs(xs[i] - ys[i]);
        }
    }

    public class ManhattanDistance : IDistanceCalculator
    {
        public double Distance(double[] xs, double[] ys) {
            return xs.Zip(ys, (x, y) => Math.Abs(x - y)).Sum();
        }

        public double RoughDistance(double[] xs, double[] ys, int i) {
            return Math.Abs(xs[i] - ys[i]);
        }
    }

    public static class KdTree
    {
        public static KdTree<T> Build<T>(IEnumerable<T> source, IDistanceCalculator calculator, params Func<T, double>[] funcs) {
            return KdTree<T>.Build(source, calculator, funcs);
        }

        public static KdTree<T> Build<T>(IEnumerable<T> source, params Func<T, double>[] funcs) {
            return KdTree<T>.Build(source, funcs);
        }
    }
}
