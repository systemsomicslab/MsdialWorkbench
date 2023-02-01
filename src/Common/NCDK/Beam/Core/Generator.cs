/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project.
 */

using NCDK.Common.Collections;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace NCDK.Beam
{
    /// <summary>
    /// (internal) Generate a SMILES line notation for a given chemical graph.
    /// </summary>
    // @author John May
    [EditorBrowsable(EditorBrowsableState.Never)]
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    sealed class Generator
    {
        private readonly Graph g;
        private readonly StringBuilder sb;

        private readonly int[] visitedAt;
        private readonly int[] tmp;
        private int nVisit;
        private readonly AtomToken[] tokens;
        private readonly Dictionary<int, IList<RingClosure>> rings;
        private readonly IRingNumbering rnums;

        /// <summary>
        /// Create a new generator the given chemical graph.
        /// </summary>
        /// <param name="g">chemical graph</param>
        /// <param name="rnums"></param>
        public Generator(Graph g, IRingNumbering rnums)
            : this(g, new int[g.Order], rnums)
        {
        }

        /// <summary>
        /// Create a new generator the given chemical graph.
        /// </summary>
        /// <param name="g">chemical graph</param>
        /// <param name="visitedAt">the index of the atom in the output</param>
        /// <param name="rnums"></param>
        Generator(Graph g, int[] visitedAt, IRingNumbering rnums)
        {
            this.g = g;
            this.rnums = rnums;
            this.sb = new StringBuilder(g.Order * 2);
            this.visitedAt = visitedAt;
            this.tmp = new int[4];
            this.tokens = new AtomToken[g.Order];
            this.rings = new Dictionary<int, IList<RingClosure>>();

            // prepare ring closures and topologies
            Arrays.Fill(visitedAt, -1);
            for (int u = 0; u < g.Order && nVisit < g.Order; u++)
            {
                if (visitedAt[u] < 0)
                    Prepare(u, u);
            }

            if (g.GetFlags(Graph.HAS_EXT_STRO) != 0)
            {
                for (int u = 0; u < g.Order; u++)
                {
                    if (g.TopologyOf(u).Configuration.Type == Configuration.ConfigurationType.ExtendedTetrahedral)
                    {
                        SetAllenalStereo(g, visitedAt, u);
                    }
                }
            }

            // write notation
            nVisit = 0;
            Arrays.Fill(visitedAt, -1);
            for (int u = 0; u < g.Order && nVisit < g.Order; u++)
            {
                if (visitedAt[u] < 0)
                {
                    if (u > 0)
                    {
                        rnums.Reset();
                        Write(u, u, Bond.Dot);
                    }
                    else
                    {
                        Write(u, u, Bond.Implicit);
                    }
                }
            }
        }

        private void SetAllenalStereo(Graph g, int[] visitedAt, int u)
        {
            Trace.Assert(g.Degree(u) == 2);
            Edge a = g.EdgeAt(u, 0);
            Edge b = g.EdgeAt(u, 1);
            Trace.Assert(a.Bond == Bond.Double &&
                   b.Bond == Bond.Double);

            int aAtom = a.Other(u);
            int bAtom = b.Other(u);

            if (!rings.ContainsKey(aAtom) && !rings.ContainsKey(bAtom))
            {
                // no rings on either end, this is simply the order we visited the
                // atoms in
                tokens[u].Configure(g.TopologyOf(u).ConfigurationOf(visitedAt));
            }
            else
            {
                // hokay this case is harder... this makes me wince but BEAM v2
                // has a much better way of handling this

                // we can be clever here rollback any changes we make (see the
                // tetrahedral handling) however since this is a very rare
                // operation it much simpler to copy the array
                int[] tmp = Arrays.CopyOf(visitedAt, visitedAt.Length);

                if (visitedAt[aAtom] > visitedAt[bAtom])
                {
                    int swap = aAtom;
                    aAtom = bAtom;
                    bAtom = swap;
                }

                Trace.Assert(!rings.ContainsKey(aAtom) || rings[aAtom].Count == 1);
                Trace.Assert(!rings.ContainsKey(bAtom) || rings[bAtom].Count == 1);

                if (rings.ContainsKey(aAtom))
                {
                    tmp[rings[aAtom][0].Other(aAtom)] = visitedAt[aAtom];
                }
                if (rings.ContainsKey(bAtom))
                {
                    tmp[rings[bAtom][0].Other(bAtom)] = visitedAt[bAtom];
                }

                tokens[u].Configure(g.TopologyOf(u).ConfigurationOf(tmp));
            }
        }

        /// <summary>
        /// First traversal of the molecule assigns ring bonds (numbered later) and
        /// configures topologies.
        /// </summary>
        /// <param name="u">the vertex to visit</param>
        /// <param name="p">the atom we came from</param>
        void Prepare(int u, int p)
        {
            visitedAt[u] = nVisit++;
            tokens[u] = g.GetAtom(u).Token;
            tokens[u].Graph = g;
            tokens[u].Index = u;

            int d = g.Degree(u);
            for (int j = 0; j < d; ++j)
            {
                Edge e = g.EdgeAt(u, j);
                int v = e.Other(u);
                if (visitedAt[v] < 0)
                {
                    Prepare(v, u);
                }
                else if (v != p && visitedAt[v] < visitedAt[u])
                {
                    CyclicEdge(v, u, e.GetBond(v));
                }
            }

            PrepareStereochemistry(u, p);
        }

        private void PrepareStereochemistry(int u, int prev)
        {
            Topology topology = g.TopologyOf(u);
            if (topology != Topology.Unknown)
            {
                if (rings.TryGetValue(u, out IList<RingClosure> closures))
                {
                    // most of time we only have a single closure, we can
                    // handle this easily by moving the ranks of the prev
                    // and curr atom back and using the curr rank for the
                    // ring
                    if (closures.Count == 1)
                    {
                        int ring = closures[0].Other(u);
                        int uAt = visitedAt[u];
                        int rAt = visitedAt[ring];
                        visitedAt[prev]--;
                        visitedAt[u]--;
                        visitedAt[ring] = uAt;
                        tokens[u].Configure(topology.ConfigurationOf(visitedAt));
                        // restore
                        visitedAt[prev]++;
                        visitedAt[u]++;
                        visitedAt[ring] = rAt;
                    }
                    else
                    {
                        // more complicated, we first move the other two atoms out
                        // the way then store and change the current ranks of the 
                        // ring atoms. We restore all visitedAt once we exit
                        Trace.Assert(closures.Count <= 4);

                        visitedAt[prev] -= 4;
                        visitedAt[u] -= 4;
                        int rank = visitedAt[u];
                        for (int i = 0; i < closures.Count; ++i)
                        {
                            int v = closures[i].Other(u);
                            tmp[i] = visitedAt[v];
                            visitedAt[v] = ++rank;
                        }

                        tokens[u].Configure(topology.ConfigurationOf(visitedAt));
                        // restore
                        for (int i = 0; i < closures.Count; ++i)
                            visitedAt[closures[i].Other(u)] = tmp[i];
                        visitedAt[prev] += 4;
                        visitedAt[u] += 4;
                    }
                }
                else
                {
                    tokens[u].Configure(topology.ConfigurationOf(visitedAt));
                }
            }
        }

        /// <summary>
        /// Second traversal writes the bonds and atoms to the SMILES string.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="p">previous vertex</param>
        /// <param name="b">the bond from the previous vertex to this vertex</param>
        public void Write(int u, int p, Bond b)
        {
            visitedAt[u] = nVisit++;

            int remaining = g.Degree(u);

            if (u != p)
                remaining--;

            // assign ring numbers
            if (rings.TryGetValue(u, out IList<RingClosure> closures))
            {
                foreach (var rc in closures)
                {
                    // as we are composing tokens, make sure apply in reverse
                    int rnum = rnums.Next();
                    if (rc.Register(rnum))
                    {
                        int v = rc.Other(u);
                        tokens[u] = new RingNumberToken(new RingBondToken(tokens[u],
                                                                          rc.GetBond(u)),
                                                        rnum);
                        rnums.Use(rnum);
                    }
                    else
                    {
                        tokens[u] = new RingNumberToken(tokens[u],
                                                        rc.RNum);
                        rnums.Free(rc.RNum);
                    }
                    remaining--;
                }
            }

            sb.Append(b.Token);
            tokens[u].Append(sb);

            int d = g.Degree(u);
            for (int j = 0; j < d; ++j)
            {
                Edge e = g.EdgeAt(u, j);
                int v = e.Other(u);
                if (visitedAt[v] < 0)
                {
                    if (--remaining > 0)
                    {
                        sb.Append('(');
                        Write(v, u, e.GetBond(u));
                        sb.Append(')');
                    }
                    else
                    {
                        Write(v, u, e.GetBond(u));
                    }
                }
            }
        }

        /// <summary>
        /// Indicate that the edge connecting the vertices u and v forms a ring.
        /// </summary>
        /// <param name="u">a vertex</param>
        /// <param name="v">a vertex connected to u</param>
        /// <param name="b">bond type connecting u to v</param>
        private void CyclicEdge(int u, int v, Bond b)
        {
            RingClosure r = new RingClosure(u, v, b);
            AddRing(r.u, r);
            AddRing(r.v, r);
        }

        /// <summary>
        /// Add a ring closure to the the vertex 'u'.
        /// </summary>
        /// <param name="u"> a vertex</param>
        /// <param name="rc">ring closure</param>
        private void AddRing(int u, RingClosure rc)
        {
            if (!rings.TryGetValue(u, out IList<RingClosure> closures))
                if (closures == null)
                {
                    closures = new List<RingClosure>(2);
                    rings.Add(u, closures);
                }
            closures.Add(rc);
        }

        /// <summary>
        /// Access the generated SMILES string.
        /// </summary>
        /// <returns>smiles string</returns>
        public string GetString()
        {
            return sb.ToString();
        }

        /// <summary>
        /// Convenience method for generating a SMILES string for the specified chemical graph.
        /// </summary>
        /// <param name="g">the graph to generate the SMILE for</param>
        /// <returns>SMILES gor the provided chemical graph</returns>
        public static string Generate(Graph g)
        {
            return new Generator(g, new IterativeRingNumbering(1)).GetString();
        }

        /// <summary>
        /// Convenience method for generating a SMILES string for the specified chemical graph.
        /// </summary>
        /// <param name="g">the graph to generate the SMILE for</param>
        /// <param name="visitedAt">store when each atom was visited</param>
        /// <returns>SMILES gor the provided chemical graph</returns>
        public static string Generate(Graph g, int[] visitedAt)
        {
            return new Generator(g, visitedAt, new IterativeRingNumbering(1)).GetString();
        }

        public sealed class RingClosure
        {
            public int u, v;
            Bond b;
            public int RNum { get; set; } = -1;

            public RingClosure(int u, int v, Bond b)
            {
                this.u = u;
                this.v = v;
                this.b = b;
            }

            public int Other(int x)
            {
                if (x == u) return v;
                if (x == v) return u;
                throw new ArgumentException("non edge endpoint");
            }

            public Bond GetBond(int x)
            {
                if (x == u) return b;
                else if (x == v) return b.Inverse();
                throw new ArgumentException("invalid endpoint");
            }

            public bool Register(int rnum)
            {
                if (this.RNum < 0)
                {
                    this.RNum = rnum;
                    return true;
                }
                return false;
            }
        }

        public abstract class AtomToken
        {
            public Graph Graph { get; set; }
            public int Index { get; set; }
            public abstract void Configure(Configuration c);
            public abstract void Append(StringBuilder sb);
        }

        public sealed class SubsetToken : AtomToken
        {
            private readonly string str;

            public SubsetToken(string str)
            {
                this.str = str;
            }

            public override void Configure(Configuration c)
            {
                // do nothing
            }

            public override void Append(StringBuilder sb)
            {
                sb.Append(str);
            }
        }

        public sealed class BracketToken : AtomToken
        {
            private IAtom atom;
            private Configuration c = Configuration.Unknown;

            public BracketToken(IAtom a)
            {
                this.atom = a;
            }

            public override void Configure(Configuration c)
            {
                this.c = c;
            }

            public override void Append(StringBuilder sb)
            {
                bool hExpand = atom.Element == Element.Hydrogen &&
                  Graph.Degree(Index) == 0;

                sb.Append('[');
                if (atom.Isotope >= 0)
                    sb.Append(atom.Isotope);
                sb.Append(
                    atom.IsAromatic() ? 
                        atom.Element.Symbol.ToLowerInvariant() : 
                        atom.Element.Symbol);
                if (c != Configuration.Unknown)
                    sb.Append(c.Shorthand.Symbol);
                if (atom.NumOfHydrogens > 0 && !hExpand)
                    sb.Append(Element.Hydrogen.Symbol);
                if (atom.NumOfHydrogens > 1 && !hExpand)
                    sb.Append(atom.NumOfHydrogens);
                if (atom.Charge != 0)
                {
                    sb.Append(atom.Charge > 0 ? '+' : '-');
                    int absCharge = Math.Abs(atom.Charge);
                    if (absCharge > 1)
                        sb.Append(absCharge);
                }
                if (atom.AtomClass != 0)
                    sb.Append(':').Append(atom.AtomClass);
                sb.Append(']');
                if (hExpand)
                {
                    int h = atom.NumOfHydrogens;
                    while (h > 1)
                    {
                        sb.Append("([H])");
                        h--;
                    }
                    if (h > 0)
                        sb.Append("[H]");
                }
            }
        }

        abstract class TokenAdapter : AtomToken
        {
            private AtomToken parent;

            public TokenAdapter(AtomToken parent)
            {
                this.parent = parent;
            }

            public override void Configure(Configuration c)
            {
                this.parent.Configure(c);
            }

            public override void Append(StringBuilder sb)
            {
                parent.Append(sb);
            }
        }

        sealed class RingNumberToken : TokenAdapter
        {
            readonly int rnum;

            public RingNumberToken(AtomToken p, int rnum)
                : base(p)
            {
                this.rnum = rnum;
            }

            public override void Append(StringBuilder sb)
            {
                base.Append(sb);
                if (rnum > 9)
                    sb.Append('%');
                sb.Append(rnum);
            }
        }

        sealed class RingBondToken : TokenAdapter
        {
            readonly Bond bond;

            public RingBondToken(AtomToken p, Bond bond)
            : base(p)
            {
                this.bond = bond;
            }

            public override void Append(StringBuilder sb)
            {
                base.Append(sb);
                sb.Append(bond);
            }
        }

        /// <summary>Defines how ring numbering proceeds.</summary>
        public interface IRingNumbering
        {
            /// <summary>
            /// The next ring number in the sequence.
            ///
            /// <returns>ring number</returns>
            /// </summary>
            int Next();

            /// <summary>
            /// Mark the specified ring number as used.
            ///
            /// <param name="rnum">ring number</param>
            /// </summary>
            void Use(int rnum);

            /// <summary>
            /// Mark the specified ring number as no longer used.
            ///
            /// <param name="rnum">ring number</param>
            /// </summary>
            void Free(int rnum);

            /// <summary>Reset ring number usage</summary>
            void Reset();
        }

        /// <summary>Labelling of ring opening/closures always using the lowest ring number.</summary>
        public sealed class ReuseRingNumbering : IRingNumbering
        {
            private bool[] used = new bool[100];
            private readonly int offset;

            public ReuseRingNumbering(int first)
            {
                this.offset = first;
            }

            public int Next()
            {
                for (int i = offset; i < used.Length; i++)
                {
                    if (!used[i])
                    {
                        return i;
                    }
                }
                throw new InvalidSmilesException("no available ring numbers");
            }

            public void Use(int rnum)
            {
                used[rnum] = true;
            }

            public void Free(int rnum)
            {
                used[rnum] = false;
            }

            public void Reset()
            {
                // do nothing 
            }
        }

        /// <summary>
        /// Iterative labelling of ring opening/closures. Once the number 99 has been
        /// used the number restarts using any free numbers.
        /// </summary>
        public sealed class IterativeRingNumbering : IRingNumbering
        {
            private readonly bool[] used = new bool[100];
            private readonly int offset;
            private int pos;

            public IterativeRingNumbering(int first)
            {
                this.offset = first;
                this.pos = offset;
            }

            public int Next()
            {
                while (pos < 100 && used[pos])
                    pos++;
                if (pos < 100)
                    return pos;
                pos = offset;
                while (pos < 100 && used[pos])
                    pos++;
                if (pos < 100)
                    return pos;
                else
                    throw new InvalidSmilesException("no more ring numbers can be assigned");
            }

            public void Use(int rnum)
            {
                used[rnum] = true;
            }

            public void Free(int rnum)
            {
                used[rnum] = false;
            }

            public void Reset()
            {
                pos = 1;
            }
        }
    }
}
