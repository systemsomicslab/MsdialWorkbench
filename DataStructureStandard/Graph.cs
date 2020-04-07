using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Common.DataStructure
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

    public class Graph : IList<Edges>
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
}
