using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.DataStructure
{
    public class Edge
    {
        public int From { get; }
        public int To { get; }
        public double Distance { get; }

        public Edge(int from, int to, double distance)
        {
            this.From = from;
            this.To = to;
            this.Distance = distance;
        }

        public Edge(int from, int to)
            : this(from, to, 0) { }
        public Edge(Edge e)
            : this(e.From, e.To, e.Distance) { }
    }

    public class Edges : IEnumerable<Edge>
    {
        private List<Edge> edges;

        public Edges(int capacity)
        {
            edges = new List<Edge>(capacity);
        }
        public Edges()
        {
            edges = new List<Edge>();
        }
        public Edges(Edges es)
        {
            edges = es.Select(e => new Edge(e)).ToList();
        }
        public Edges(IEnumerable<Edge> es)
        {
            edges = es.ToList();
        }

        public void Add(Edge e)
        {
            edges.Add(e);
        }
        public IEnumerator<Edge> GetEnumerator()
        {
            return edges.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Edge>)edges).GetEnumerator();
        }
    }

    public class Graph : IList<Edges>, IReadOnlyList<Edges>
    {
        private List<Edges> g = new List<Edges>();

        public Graph(int n)
        {
            for(int i = 0; i < n; ++i)
            {
                g.Add(new Edges());
            }
        }
        public Graph(IEnumerable<Edges> g_)
        {
            this.g = g_.Select(es => new Edges(es)).ToList();
        }

        public void AddEdge(int from, int to, double distance)
        {
            g[from].Add(new Edge(from, to, distance));
            g[to].Add(new Edge(to, from, 0));
        }

        #region // IList methods and properties
        public Edges this[int index] { get => ((IList<Edges>)g)[index]; set => ((IList<Edges>)g)[index] = value; }

        public int Count => ((IList<Edges>)g).Count;

        public bool IsReadOnly => ((IList<Edges>)g).IsReadOnly;

        public void Add(Edges item)
        {
            ((IList<Edges>)g).Add(item);
        }

        public void Clear()
        {
            ((IList<Edges>)g).Clear();
        }

        public bool Contains(Edges item)
        {
            return ((IList<Edges>)g).Contains(item);
        }

        public void CopyTo(Edges[] array, int arrayIndex)
        {
            ((IList<Edges>)g).CopyTo(array, arrayIndex);
        }

        public IEnumerator<Edges> GetEnumerator()
        {
            return ((IList<Edges>)g).GetEnumerator();
        }

        public int IndexOf(Edges item)
        {
            return ((IList<Edges>)g).IndexOf(item);
        }

        public void Insert(int index, Edges item)
        {
            ((IList<Edges>)g).Insert(index, item);
        }

        public bool Remove(Edges item)
        {
            return ((IList<Edges>)g).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<Edges>)g).RemoveAt(index);
        }
        #endregion

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<Edges>)g).GetEnumerator();
        }
    }

    public class Digraph : IList<Edges>, IReadOnlyList<Edges>
    {
        protected List<Edges> g = new List<Edges>();

        public Digraph(int n)
        {
            g = new List<Edges>(n);
            foreach(var _ in Enumerable.Range(0, n))
            {
                g.Add(new Edges());
            }
        }

        public Digraph(IEnumerable<Edges> g_)
        {
            g = g_.Select(es => new Edges(es)).ToList();
        }

        public Edges this[int index] { get => ((IList<Edges>)g)[index]; set => ((IList<Edges>)g)[index] = value; }

        public int Count => ((IList<Edges>)g).Count;

        public bool IsReadOnly => ((IList<Edges>)g).IsReadOnly;

        public void Add(Edges item)
        {
            ((IList<Edges>)g).Add(item);
        }

        public void AddEdge(int from, int to, double distance = 0d)
        {
            g[from].Add(new Edge(from, to, distance));
        }

        public void Clear()
        {
            ((IList<Edges>)g).Clear();
        }

        public bool Contains(Edges item)
        {
            return ((IList<Edges>)g).Contains(item);
        }

        public void CopyTo(Edges[] array, int arrayIndex)
        {
            ((IList<Edges>)g).CopyTo(array, arrayIndex);
        }

        public IEnumerator<Edges> GetEnumerator()
        {
            return ((IList<Edges>)g).GetEnumerator();
        }

        public int IndexOf(Edges item)
        {
            return ((IList<Edges>)g).IndexOf(item);
        }

        public void Insert(int index, Edges item)
        {
            ((IList<Edges>)g).Insert(index, item);
        }

        public bool Remove(Edges item)
        {
            return ((IList<Edges>)g).Remove(item);
        }

        public void RemoveAt(int index)
        {
            ((IList<Edges>)g).RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<Edges>)g).GetEnumerator();
        }
    }

    public class DirectedTree : Digraph
    {
        public DirectedTree(int n) : base(n) { }
        public DirectedTree(IEnumerable<Edges> g_) : base(g_) { }

        public int Root
        {
            get
            {
                if (IsValid) return getRoot();
                throw new InvalidOperationException("DirectedTree doesn't have root node.");
            }
        }

        public IReadOnlyList<int> Leaves
            => g.Select((edges, index) => (index, edges)).Where(p => p.edges.Count() == 0).Select(p => p.index).ToArray();

        public bool IsValid
        {
            get
            {
                var count = new int[g.Count];
                foreach(var es in g)
                    foreach(var e in es)
                        ++count[e.To];
                if (count.Sum() != g.Count - 1 || count.Where(c => c == 0).Count() != 1)
                    return false;
                var alived = new bool[g.Count];
                var root = Array.IndexOf(count, 0);
                alived[root] = true;
                PreOrder(root, e => alived[e.To] = true);
                return alived.All(b => b);
            }
        }
        
        int getRoot()
        {
            var count = new int[g.Count];
            foreach (var es in g)
                foreach (var e in es)
                    ++count[e.To];
            return Array.IndexOf(count, 0);
        }

        public void PostOrder(int node, Action<Edge> f)
        {
            void dfs(int v)
            {
                foreach(var e in g[v])
                {
                    dfs(e.To);
                    f(e);
                }
            }
            dfs(node);
        }
        public void PostOrder(Action<Edge> f) => PostOrder(Root, f);

        public void PreOrder(int node, Action<Edge> f)
        {
            void dfs(int v)
            {
                foreach(var e in g[v])
                {
                    f(e);
                    dfs(e.To);
                }
            }
            dfs(node);
        }
        public void PreOrder(Action<Edge> f) => PreOrder(Root, f);

        public void BfsEdge(int node, Action<Edge> f)
        {
            var q = new PriorityQueue<(double dist, Edge edge)>((a, b) => a.dist.CompareTo(b.dist));
            q.Push((0, new Edge(-1, node, 0)));
            while (q.Length != 0)
            {
                (double d, Edge e) = q.Pop();
                f(e);

                foreach(var v in g[e.To])
                    q.Push((d + v.Distance, v));
            }
        }
        public void BfsEdge(Action<Edge> f) => BfsEdge(Root, f);

        public void BfsNode(int node, Action<int> f)
        {
            var q = new PriorityQueue<(double dist, int node)>((a, b) => a.dist.CompareTo(b.dist));
            q.Push((0, node));
            while (q.Length != 0)
            {
                (double d, int v) = q.Pop();
                f(v);

                foreach(var e in g[v])
                    q.Push((d + e.Distance, e.To));
            }
        }
        public void BfsNode(Action<int> f) => BfsNode(Root, f);
    }
}
