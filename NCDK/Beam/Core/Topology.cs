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
using System.Text;
using static NCDK.Beam.Configuration;
using static NCDK.Beam.Configuration.ConfigurationType;

namespace NCDK.Beam
{
    /// <summary>
    /// Defines the relative topology around a vertex (atom).
    /// </summary>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    abstract class Topology
    {
        /// <summary>
        /// The vertex/atom which this topology describes.
        /// </summary>
        /// <exception cref="ArgumentException">Unknown topology</exception>
        public abstract int Atom { get; }

        /// <summary>
        /// The configuration of the topology.
        /// </summary>
        public abstract Configuration Configuration { get; }

        /// <summary>
        /// The configuration of the topology when it's carriers have the specified
        /// ranks.
        /// </summary>
        /// <param name="rank"></param>
        /// <returns>configuration for this topology</returns>
        public virtual Configuration ConfigurationOf(int[] rank)
        {
            Topology topology = OrderBy(rank);
            return topology != null ? topology.Configuration : Configuration.Unknown;
        }

        /// <summary>
        /// What type of configuration is defined by this topology (e.g. Tetrahedral,
        /// DoubleBond etc).
        /// </summary>
        /// <returns>the type of the configuration</returns>
        public virtual Configuration.ConfigurationType Type => Configuration.Type;

        /// <summary>
        /// Arrange the topology relative to a given ranking of vertices.
        /// </summary>
        /// <param name="rank">Ordering of vertices</param>
        /// <returns>a new topology with the neighbors arranged by the given rank</returns>
        public abstract Topology OrderBy(int[] rank);

        /// <summary>
        /// Transform the topology to one with the given <paramref name="mapping"/>.
        /// </summary>
        /// <param name="mapping">the mapping used to transform the topology</param>
        /// <returns>a new topology with it's vertices mapped</returns>
        public abstract Topology Transform(int[] mapping);

        public abstract void Copy(int[] dest);

        /// <summary>
        /// Compute the permutation parity of the vertices <paramref name="vs"/> for the
        /// given <paramref name="rank"/>. The parity defines the oddness or evenness of a
        /// permutation and is the number of inversions (swaps) one would need to
        /// make to place the 'vs' in the Order specified by rank.
        /// </summary>
        /// <param name="vs">  array of vertices</param>
        /// <param name="rank">rank of vertices</param>, |R| = Max(vs) + 1
        /// <returns>sign of the permutation, -1=odd or 1=even</returns>
        /// <seealso href="http://en.wikipedia.org/wiki/Parity_of_a_permutation">Parity of a Permutation</seealso>
        public static int Parity(int[] vs, int[] rank)
        {
            // count elements which are out of Order and by how much
            int count = 0;
            for (int i = 0; i < vs.Length; i++)
            {
                for (int j = i + 1; j < vs.Length; j++)
                {
                    if (rank[vs[i]] > rank[vs[j]])
                        count++;
                }
            }
            // odd parity = -1, even parity = 1
            return (count & 0x1) == 1 ? -1 : 1;
        }

        // help the compiler, array is a fixed size!
        public static int Parity4(int[] vs, int[] rank)
        {
            // count elements which are out of order and by how much
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                 int prev = rank[vs[i]];
                for (int j = i + 1; j < 4; j++)
                {
                    if (prev > rank[vs[j]])
                        count++;
                }
            }

            // odd parity = -1, even parity = 1
            return (count & 0x1) == 1 ? -1 : 1;
        }

        /// <summary>
        /// Sorts the array <paramref name="vs"/> into the Order given by the <paramref name="rank"/>.
        /// </summary>
        /// <param name="vs">vertices to sort</param>
        /// <param name="rank">rank of vertices</param>
        /// <returns>sorted array (cpy of vs)</returns>
        public static int[] Sort(int[] vs, int[] rank)
        {
            int[] ws = (int[])vs.Clone();    // Arrays.CopyOf(vs, vs.Length);

            // insertion sort using rank for the Ordering
            for (int i = 0, j = i; i < vs.Length - 1; j = ++i)
            {
                int v = ws[i + 1];
                while (rank[v] < rank[ws[j]])
                {
                    ws[j + 1] = ws[j];
                    if (--j < 0)
                        break;
                }
                ws[j + 1] = v;
            }
            return ws;
        }

        /// <summary>
        /// Specify Unknown configuration on atom - there is no vertex data stored.
        /// </summary>
        /// <returns>Unknown topology</returns>
        public static Topology Unknown => unknown;

        private static readonly Topology unknown = new UnknownTopology();

        /// <summary>
        /// Define tetrahedral topology of the given configuration.
        /// </summary>
        /// <param name="u">central atom</param>
        /// <param name="vs">vertices surrounding u, the first is the vertex we are looking from</param>
        /// <param name="configuration">the tetrahedral configuration, @TH1, @TH2, @ or @@</param>
        /// <returns>topology instance for that configuration</returns>
        /// <seealso cref="Configuration"/>
        public static Topology CreateTetrahedral(int u, int[] vs, Configuration configuration)
        {
            if (configuration.Type != ConfigurationType.Implicit
                    && configuration.Type != ConfigurationType.Tetrahedral)
                throw new ArgumentException(configuration.Type
                    + "invalid tetrahedral configuration");

            int p = configuration.Shorthand == Clockwise ? 1 : -1;

            return new Tetrahedral(u, (int[])vs.Clone(), p);
        }

        public static Topology CreateExtendedTetrahedral(int u, int[] vs, Configuration configuration)
        {
            if (configuration.Type != Implicit
                    && configuration.Type != ConfigurationType.ExtendedTetrahedral)
                throw new ArgumentException(configuration.Type
                                                           + "invalid extended tetrahedral configuration");

            int p = configuration.Shorthand == Clockwise ? 1 : -1;

            return new ExtendedTetrahedral(u, 
                                           Arrays.CopyOf(vs, vs.Length),
                                           p);
        }

        /// <summary>
        /// Define trigonal topology of the given configuration.
        /// </summary>
        /// <param name="u">central atom</param>
        /// <param name="vs">vertices surrounding u, the first is the vertex we are looking from</param>
        /// <param name="configuration">the trigonal configuration, @DB1, @Db1, @ or @@</param>
        /// <returns>topology instance for that configuration</returns>
        /// <seealso cref="Configuration"/>
        public static Topology CreateTrigonal(int u, int[] vs, Configuration configuration)
        {
            if (configuration.Type != Implicit
                    && configuration.Type != ConfigurationType.DoubleBond)
                throw new ArgumentException(configuration.Type
                                                           + "invalid tetrahedral configuration");

            int p = configuration.Shorthand == Clockwise ? 1 : -1;

            return new Trigonal(u,
                                Arrays.CopyOf(vs, vs.Length),
                                p);
        }

        static Topology CreateSquarePlanar(int u, int[] vs, Configuration configuration)
        {
            switch (configuration.Ordinal)
            {
                case Configuration.O.SP1:
                    return new SquarePlanar(u,
                                            Arrays.CopyOf(vs, vs.Length),
                                            1);
                case Configuration.O.SP2:
                    return new SquarePlanar(u,
                                            Arrays.CopyOf(vs, vs.Length),
                                            2);
                case Configuration.O.SP3:
                    return new SquarePlanar(u,
                                            Arrays.CopyOf(vs, vs.Length),
                                            3);
                default:
                    return null;
            }
        }

        private static Topology CreateTrigonalBipyramidal(int u, int[] vs, Configuration c)
        {
            if (Configuration.TB1.Ordinal <= c.Ordinal &&
                    Configuration.TB20.Ordinal >= c.Ordinal)
            {
                int order = 1 + c.Ordinal - Configuration.TB1.Ordinal;
                return new TrigonalBipyramidal(u, vs, order);
            }
            return null;
        }

        private static Topology CreateOctahedral(int u, int[] vs, Configuration c)
        {
            if (Configuration.OH1.Ordinal <= c.Ordinal &&
                    Configuration.OH30.Ordinal >= c.Ordinal)
            {
                int order = 1 + c.Ordinal - Configuration.OH1.Ordinal;
                return new Octahedral(u, vs, order);
            }
            return null;
        }

        /// <summary>
        /// Convert an implicit configuration ('@' or '@@') c, to an explicit one
        /// (e.g. @TH1).
        /// </summary>
        /// <remarks>
        /// Implicit Valence Explicit Example
        /// <list type="table">
        /// <item><term>@ 4</term><term>@TH1</term><term>@TH1</term></item>
        /// <item><term>@@ 4</term><term>@TH2</term><term>@TH2</term></item>
        /// <item><term>@ 3</term><term>@TH1</term><term>@TH1</term></item>
        /// <item><term>@@ 3</term><term>@TH2</term><term>@TH2</term></item>
        /// <item><term>@ 2</term><term>@AL1</term><term>@AL1</term></item>
        /// <item><term>@ 2</term><term>@AL2</term><term>@AL2</term></item>
        /// <item><term>@ 5</term><term>@TB1</term><term>@TB1</term></item>
        /// <item><term>@@ 5</term><term>@TB2</term><term>@TB2</term></item>
        /// <item><term>@ 5</term><term>@OH1</term><term>@OH1</term></item>
        /// <item><term>@@ 5</term><term>@OH2</term><term>@OH2</term></item>
        /// </list>
        /// </remarks>
        /// <param name="g">chemical graph</param>
        /// <param name="u">the atom to which the configuration is associated</param>
        /// <param name="c">implicit configuration (<see cref="Configuration.AntiClockwise"/> or <see cref="Configuration.Clockwise"/>)</param>
        /// <returns>an explicit configuration or <see cref="Configuration.Unknown"/></returns>
        public static Configuration ToExplicit(Graph g, int u, Configuration c)
        {
            // already explicit
            if (c.Type != Implicit)
                return c;

            int deg = g.Degree(u);
            int valence = deg + g.GetAtom(u).NumOfHydrogens;

            // tetrahedral topology, square planar must always be explicit
            if (valence == 4)
            {
                return c == AntiClockwise ? TH1 : TH2;
            }

            // tetrahedral topology with implicit lone pair or double bond (Sp2)
            // atoms (todo)
            else if (valence == 3)
            {
                // XXX: sulfoxide and selenium special case... would be better to compute
                // hybridization don't really like doing this here but is sufficient
                // for now
                if (g.GetAtom(u).Element == Element.Sulfur || g.GetAtom(u).Element == Element.Selenium)
                {
                    int sb = 0, db = 0;
                    int dd = g.Degree(u);
                    for (int j = 0; j < dd; ++j)
                    {
                        Edge e = g.EdgeAt(u, j);
                        if (e.Bond.Order == 1)
                            sb++;
                        else if (e.Bond.Order == 2)
                            db++;
                        else return Configuration.Unknown;
                    }
                    int q = g.GetAtom(u).Charge;
                    if ((q == 0 && sb == 2 && db == 1) || (q == 1 && sb == 3))
                        return c == AntiClockwise ? TH1 : TH2;
                    else
                        return Configuration.Unknown;
                }

                if (g.GetAtom(u).Element == Element.Phosphorus ||
                    g.GetAtom(u).Element == Element.Nitrogen)
                {
                    if (g.BondedValence(u) == 3 && g.ImplHCount(u) == 0 && g.GetAtom(u).Charge == 0)
                    {
                        return c == AntiClockwise ? TH1 : TH2;
                    }
                }

                // for the atom centric double bond configuration check there is
                // a double bond and it's not sill tetrahedral specification such
                // as [C@-](N)(O)C
                int nDoubleBonds = 0;
                int d = g.Degree(u);
                for (int j = 0; j < d; ++j)
                {
                     Edge e = g.EdgeAt(u, j);
                    if (e.Bond == Bond.Double)
                        nDoubleBonds++;
                }

                if (nDoubleBonds == 1)
                {
                    return c == AntiClockwise ? DB1 : DB2;
                }
                else
                {
                    return Configuration.Unknown;
                }
            }

            // odd number of cumulated double bond systems (e.g. allene)
            else if (deg == 2)
            {
                int nDoubleBonds = 0;

                // check both bonds are double
                int d = g.Degree(u);
                for (int j = 0; j < d; ++j)
                {
                    Edge e = g.EdgeAt(u, j);
                    if (e.Bond != Bond.Double)
                        nDoubleBonds++;
                }
                if (nDoubleBonds == 1)
                {
                    return c == AntiClockwise ? DB1 : DB2;
                }
                else
                {
                    return c == AntiClockwise ? AL1 : AL2;
                }
            }

            // trigonal bipyramidal
            else if (valence == 5)
            {
                return c == AntiClockwise ? TB1 : TB2;
            }

            // octahedral
            else if (valence == 6)
            {
                return c == AntiClockwise ? OH1 : OH2;
            }

            return Configuration.Unknown;
        }

        public static Topology Create(int u, int[] vs, IList<Edge> es, Configuration c)
        {
            if (c.Type == ConfigurationType.Implicit)
                throw new ArgumentOutOfRangeException(nameof(c), "configuration must be explicit, @TH1/@TH2 instead of @/@@");

            // only tetrahedral is handled for now
            if (c.Type == ConfigurationType.Tetrahedral)
            {
                return CreateTetrahedral(u, vs, c);
            }
            else if (c.Type == ConfigurationType.DoubleBond)
            {
                return CreateTrigonal(u, vs, c);
            }
            else if (c.Type == ConfigurationType.ExtendedTetrahedral)
            {
                return CreateExtendedTetrahedral(u, vs, c);
            }
            else if (c.Type == ConfigurationType.SquarePlanar)
            {
                return CreateSquarePlanar(u, vs, c);
            }
            else if (c.Type == ConfigurationType.TrigonalBipyramidal)
            {
                return CreateTrigonalBipyramidal(u, vs, c);
            }
            else if (c.Type == Configuration.ConfigurationType.Octahedral)
            {
                return CreateOctahedral(u, vs, c);
            }

            return Unknown;
        }

        class UnknownTopology : Topology
        {
            public override int Atom
            {
                get
                {
                    throw new NotSupportedException("Unknown topology");
                }
            }
            public override Configuration Configuration => Configuration.Unknown;

            public override Topology OrderBy(int[] rank)
            {
                return this;
            }

            public override Topology Transform(int[] mapping)
            {
                return this;
            }

            public override void Copy(int[] dest)
            {
            }
        }

        private sealed class Tetrahedral : Topology
        {
            private readonly int u;
            private readonly int[] vs;
            private readonly int p;

            public Tetrahedral(int u, int[] vs, int p)
            {
                if (vs.Length != 4)
                    throw new ArgumentOutOfRangeException(nameof(vs), "Tetrahedral topology requires 4 vertices - use the 'centre' vertex to mark implicit verticies");
                this.u = u;
                this.vs = vs;
                this.p = p;
            }

            /// <inheritdoc/>
            public override int Atom => u;

            /// <inheritdoc/>
            public override Configuration Configuration
                => p < 0 ? Configuration.TH1 : Configuration.TH2;

            /// <inheritdoc/>
            public override Topology OrderBy(int[] rank)
            {
                return new Tetrahedral(u,
                                       Sort(vs, rank),
                                       p * Parity4(vs, rank));
            }

            /// <inheritdoc/>
            public override Topology Transform(int[] mapping)
            {
                int[] ws = new int[vs.Length];
                for (int i = 0; i < vs.Length; i++)
                    ws[i] = mapping[vs[i]];
                return new Tetrahedral(mapping[u], ws, p);
            }

            public override void Copy(int[] dest)
            {
                Array.Copy(vs, 0, dest, 0, 4);
            }

            public override Configuration ConfigurationOf(int[] rank)
            {
                return p * Parity4(vs, rank) < 0 ? TH1 : TH2;
            }

            public override string ToString()
            {
                return u + " " + Arrays_ToString(vs) + ":" + p;
            }
        }

        private sealed class ExtendedTetrahedral : Topology
        {
            private readonly int u;
            private readonly int[] vs;
            private readonly int p;

            public ExtendedTetrahedral(int u, int[] vs, int p)
            {
                if (vs.Length != 4)
                    throw new ArgumentOutOfRangeException(nameof(vs), "Tetrahedral topology requires 4 vertices - use the 'centre' vertex to mark implicit verticies");
                this.u = u;
                this.vs = vs;
                this.p = p;
            }

            /// <inheritdoc/>
            public override int Atom => u;

            /// <inheritdoc/>
            public override Configuration Configuration
                =>  p < 0 ? Configuration.AL1 : Configuration.AL2;

            /// <inheritdoc/>
            public override Topology OrderBy(int[] rank)
            {
                return new ExtendedTetrahedral(u,
                                               Sort(vs, rank),
                                               p * Parity(vs, rank));
            }

            /// <inheritdoc/>
            public override Topology Transform(int[] mapping)
            {
                int[] ws = new int[vs.Length];
                for (int i = 0; i < vs.Length; i++)
                    ws[i] = mapping[vs[i]];
                return new ExtendedTetrahedral(mapping[u], ws, p);
            }

            public override void Copy(int[] dest)
            {
                Array.Copy(vs, 0, dest, 0, 4);
            }

            public override string ToString()
            {
                return u + " " + Arrays_ToString(vs) + ":" + p;
            }
        }

        private const int A = 0;
        private const int B = 1;
        private const int C = 2;
        private const int D = 3;
        private const int E = 4;
        private const int F = 5;

        private static bool Check(int[] dest, int[] src, int[] perm, int step, int skip)
        {
            for (int i = 0; i < perm.Length;)
            {
                int j;
                for (j = 0; j < step; j++)
                {
                    if (dest[perm[i + j]] != src[j])
                        break;
                }
                if (j == 0)
                    i += skip * step;
                else if (j == step)
                    return true;
                else
                    i += step;
            }
            return false;
        }

        private static void Swap(int[] arr, int i, int j)
        {
            int tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }

        private static void IndirectSort(int[] dst, int[] rank)
        {
            for (int i = 0; i < dst.Length; i++)
                for (int j = i; j > 0 && rank[dst[j - 1]] > rank[dst[j]]; j--)
                    Topology.Swap(dst, j, j - 1);
        }

        private static int[] ApplyInv(int[] src, int[] perm)
        {
            int[] res = new int[src.Length];
            for (int i = 0; i < src.Length; i++)
                res[i] = src[perm[i]];
            return res;
        }

        private static int?[] ToObjArray(int?[] arr)
        {
            int?[] res = new int?[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                res[i] = arr[i];
            return res;
        }

        private static int[] ToIntArray(int[] arr)
        {
            int[] res = new int[arr.Length];
            for (int i = 0; i < arr.Length; i++)
                res[i] = arr[i];
            return res;
        }

        private sealed class SquarePlanar : Topology
        {
            private readonly int u;
            private readonly int[] vs;
            private readonly int order;

            private static readonly int[][] PERMUTATIONS = new int[][]
            {
                new[]
                    {
                        A, B, C, D,  A, D, C, B,
                        B, C, D, A,  B, A, D, C,
                        C, D, A, B,  C, B, A, D,
                        D, C, B, A,  D, A, B, C
                    }, // SP1 (U)
                new[]
                    {
                        A, C, B, D,  A, D, B, C,
                        B, D, A, C,  B, C, A, D,
                        C, A, D, B,  C, B, D, A,
                        D, B, C, A,  D, A, C, B
                    }, // SP2 (4)
                new[]
                    {
                        A, B, D, C,  A, C, D, B,
                        B, A, C, D,  B, D, C, A,
                        C, D, B, A,  C, A, B, D,
                        D, C, A, B,  D, B, A, C
                    }  // SP3 (Z)
            };

            public SquarePlanar(int u, int[] vs, int p)
            {
                if (vs.Length != 4)
                    throw new ArgumentException("SquarePlanar topology requires 4 vertices");
                this.u = u;
                this.vs = vs;
                this.order = p;
            }

            /// <inheritdoc/>
            public override int Atom => u;

            /// <inheritdoc/>
            public override Configuration Configuration
            {
                get
                {
                    switch (order)
                    {
                        case 1: return SP1;
                        case 2: return SP2;
                        case 3: return SP3;
                        default: return Configuration.Unknown;
                    }
                }
            }

            /// <inheritdoc/>
            public override Topology OrderBy(int[] rank)
            {
                int[] src = Topology.ApplyInv(vs, PERMUTATIONS[order - 1]);
                int[] dst = Arrays.Clone(src);
                IndirectSort(dst, rank);
                if (order < 1 || order > 20)
                    return null;

                for (int i = 1; i <= 3; i++)
                {
                    if (Topology.Check(dst, src, PERMUTATIONS[i - 1], 4, 2))
                        return new SquarePlanar(u, dst, i);
                }

                return null;
            }

            /// <inheritdoc/>
            public override Topology Transform(int[] mapping)
            {
                int[] ws = new int[vs.Length];
                for (int i = 0; i < vs.Length; i++)
                    ws[i] = mapping[vs[i]];
                return new SquarePlanar(mapping[u], ws, order);
            }

            public override void Copy(int[] dest)
            {
                Array.Copy(vs, 0, dest, 0, vs.Length);
            }

            public override string ToString()
            {
                return u + " " + Arrays.ToJavaString(vs) + ":" + order;
            }
        }

        private sealed class TrigonalBipyramidal : Topology
        {
            private readonly int u;
            private readonly int[] vs;
            private readonly int order;

            private static readonly int[][] PERMUTATIONS = new int[][]
            {
                new[] {A, B, C, D, E,  A, C, D, B, E,  A, D, B, C, E, E, D, C, B, A,  E, B, D, C, A,  E, C, B, D, A }, // TB1 a -> e @
                new[] {A, D, C, B, E,  A, C, B, D, E,  A, B, D, C, E, E, B, C, D, A,  E, D, B, C, A,  E, C, D, B, A }, // TB2 a -> e @@
                new[] {A, B, C, E, D,  A, C, E, B, D,  A, E, B, C, D, D, E, C, B, A,  D, B, E, C, A,  D, C, B, E, A }, // TB3 a -> d @
                new[] {A, E, C, B, D,  A, C, B, E, D,  A, B, E, C, D, D, B, C, E, A,  D, E, B, C, A,  D, C, E, B, A }, // TB4 a -> d @@
                new[] {A, B, D, E, C,  A, D, E, B, C,  A, E, B, D, C, C, E, D, B, A,  C, B, E, D, A,  C, D, B, E, A }, // TB5 a -> c @
                new[] {A, E, D, B, C,  A, D, B, E, C,  A, B, E, D, C, C, B, D, E, A,  C, E, B, D, A,  C, D, E, B, A }, // TB6 a -> c @@
                new[] {A, C, D, E, B,  A, D, E, C, B,  A, E, C, D, B, B, E, D, C, A,  B, C, E, D, A,  B, D, C, E, A }, // TB7 a -> b @
                new[] {A, E, D, C, B,  A, D, C, E, B,  A, C, E, D, B, B, C, D, E, A,  B, E, C, D, A,  B, D, E, C, A }, // TB8 a -> b @@
                new[] {B, A, C, D, E,  B, C, D, A, E,  B, D, A, C, E, E, D, C, A, B,  E, A, D, C, B,  E, C, A, D, B }, // TB9 b -> e @
                new[] {B, A, C, E, D,  B, C, E, A, D,  B, E, A, C, D, D, E, C, A, B,  D, A, E, C, B,  D, C, A, E, B }, // TB10 b -> d @
                new[] {B, D, C, A, E,  B, C, A, D, E,  B, A, D, C, E, E, A, C, D, B,  E, D, A, C, B,  E, C, D, A, B }, // TB11 b -> e @@
                new[] {B, E, C, A, D,  B, C, A, E, D,  B, A, E, C, D, D, A, C, E, B,  D, E, A, C, B,  D, C, E, A, B }, // TB12 b -> d @@
                new[] {B, A, D, E, C,  B, D, E, A, C,  B, E, A, D, C, C, E, D, A, B,  C, A, E, D, B,  C, D, A, E, B }, // TB13 b -> c @
                new[] {B, E, D, A, C,  B, D, A, E, C,  B, A, E, D, C, C, A, D, E, B,  C, E, A, D, B,  C, D, E, A, B }, // TB14 b -> c @@
                new[] {C, A, B, D, E,  C, B, D, A, E,  C, D, A, B, E, E, D, B, A, C,  E, A, D, B, C,  E, B, A, D, C }, // TB15 c -> e @
                new[] {C, A, B, E, D,  C, B, E, A, D,  C, E, A, B, D, D, E, B, A, C,  D, A, E, B, C,  D, B, A, E, C }, // TB16 c -> d @
                new[] {D, A, B, C, E,  D, B, C, A, E,  D, C, A, B, E, E, C, B, A, D,  E, A, C, B, D,  E, B, A, C, D }, // TB17 d -> e @
                new[] {D, C, B, A, E,  D, B, A, C, E,  D, A, C, B, E, E, A, B, C, D,  E, C, A, B, D,  E, B, C, A, D }, // TB18 d -> e @@
                new[] {C, E, B, A, D,  C, B, A, E, D,  C, A, E, B, D, D, A, B, E, C,  D, E, A, B, C,  D, B, E, A, C }, // TB19 c -> d @@
                new[] {C, D, B, A, E,  C, B, A, D, E,  C, A, D, B, E, E, A, B, D, C,  E, D, A, B, C,  E, B, D, A, C }, // TB20 c -> e @@
            };

            public TrigonalBipyramidal(int u, int[] vs, int order)
            {
                if (vs.Length != 5)
                    throw new ArgumentException("TrigonalBipyramidal topology requires 5 vertices");
                this.u = u;
                this.vs = vs;
                this.order = order;
            }

            /// <inheritdoc/>
            public override int Atom => u;

            /// <inheritdoc/>
            public override Configuration Configuration
            {
                get
                {
                    if (order >= 1 && order <= 20)
                        return Configuration.Values[Configuration.TB1.Ordinal + order - 1];
                    return Configuration.Unknown;
                }
            }

            /// <inheritdoc/>
            public override Topology OrderBy(int[] rank)
            {
                int[] src = Topology.ApplyInv(vs, PERMUTATIONS[order - 1]);
                int[] dst = Arrays.Clone(src);
                IndirectSort(dst, rank);

                for (int i = 1; i <= 20; i++)
                {
                    if (Topology.Check(dst, src, PERMUTATIONS[i - 1], 5, 3))
                        return new TrigonalBipyramidal(u, dst, i);
                }

                return null;
            }

            /// <inheritdoc/>
            public override Topology Transform(int[] mapping)
            {
                int[] ws = new int[vs.Length];
                for (int i = 0; i < vs.Length; i++)
                    ws[i] = mapping[vs[i]];
                return new TrigonalBipyramidal(mapping[u], ws, order);
            }

            public override void Copy(int[] dest)
            {
                Array.Copy(vs, 0, dest, 0, vs.Length);
            }

            public override string ToString()
            {
                return u + " " + Arrays.ToJavaString(vs) + ":" + order;
            }
        }

        private sealed class Octahedral : Topology
        {
            private readonly int u;
            private readonly int[] vs;
            private readonly int order;

            private static readonly int[][] PERMUTATIONS = new int[][]
            {
                // @OH1
                new[] {
                    A, B, C, D, E, F,  A, C, D, E, B, F,  A, D, E, B, C, F,  A, E, B, C, D, F,
                    B, A, E, F, C, D,  B, C, A, E, F, D,  B, E, F, C, A, D,  B, F, C, A, E, D,
                    C, A, B, F, D, E,  C, B, F, D, A, E,  C, D, A, B, F, E,  C, F, D, A, B, E,
                    D, A, C, F, E, B,  D, C, F, E, A, B,  D, E, A, C, F, B,  D, F, E, A, C, B,
                    E, A, D, F, B, C,  E, B, A, D, F, C,  E, D, F, B, A, C,  E, F, B, A, D, C,
                    F, B, E, D, C, A,  F, C, B, E, D, A,  F, D, C, B, E, A,  F, E, D, C, B, A},
                // @OH2
                new[] {
                    A, B, E, D, C, F,  A, C, B, E, D, F,  A, D, C, B, E, F,  A, E, D, C, B, F,
                    B, A, C, F, E, D,  B, C, F, E, A, D,  B, E, A, C, F, D,  B, F, E, A, C, D,
                    C, A, D, F, B, E,  C, B, A, D, F, E,  C, D, F, B, A, E,  C, F, B, A, D, E,
                    D, A, E, F, C, B,  D, C, A, E, F, B,  D, E, F, C, A, B,  D, F, C, A, E, B,
                    E, A, B, F, D, C,  E, B, F, D, A, C,  E, D, A, B, F, C,  E, F, D, A, B, C,
                    F, B, C, D, E, A,  F, C, D, E, B, A,  F, D, E, B, C, A,  F, E, B, C, D, A},
                // @OH3
                new[] {
                    A, B, C, D, F, E,  A, C, D, F, B, E,  A, D, F, B, C, E,  A, F, B, C, D, E,
                    B, A, F, E, C, D,  B, C, A, F, E, D,  B, E, C, A, F, D,  B, F, E, C, A, D,
                    C, A, B, E, D, F,  C, B, E, D, A, F,  C, D, A, B, E, F,  C, E, D, A, B, F,
                    D, A, C, E, F, B,  D, C, E, F, A, B,  D, E, F, A, C, B,  D, F, A, C, E, B,
                    E, B, F, D, C, A,  E, C, B, F, D, A,  E, D, C, B, F, A,  E, F, D, C, B, A,
                    F, A, D, E, B, C,  F, B, A, D, E, C,  F, D, E, B, A, C,  F, E, B, A, D, C},
                // @OH4
                new[] {
                    A, B, C, E, D, F,  A, C, E, D, B, F,  A, D, B, C, E, F,  A, E, D, B, C, F,
                    B, A, D, F, C, E,  B, C, A, D, F, E,  B, D, F, C, A, E,  B, F, C, A, D, E,
                    C, A, B, F, E, D,  C, B, F, E, A, D,  C, E, A, B, F, D,  C, F, E, A, B, D,
                    D, A, E, F, B, C,  D, B, A, E, F, C,  D, E, F, B, A, C,  D, F, B, A, E, C,
                    E, A, C, F, D, B,  E, C, F, D, A, B,  E, D, A, C, F, B,  E, F, D, A, C, B,
                    F, B, D, E, C, A,  F, C, B, D, E, A,  F, D, E, C, B, A,  F, E, C, B, D, A},
                // @OH5
                new[] {
                    A, B, C, F, D, E,  A, C, F, D, B, E,  A, D, B, C, F, E,  A, F, D, B, C, E,
                    B, A, D, E, C, F,  B, C, A, D, E, F,  B, D, E, C, A, F,  B, E, C, A, D, F,
                    C, A, B, E, F, D,  C, B, E, F, A, D,  C, E, F, A, B, D,  C, F, A, B, E, D,
                    D, A, F, E, B, C,  D, B, A, F, E, C,  D, E, B, A, F, C,  D, F, E, B, A, C,
                    E, B, D, F, C, A,  E, C, B, D, F, A,  E, D, F, C, B, A,  E, F, C, B, D, A,
                    F, A, C, E, D, B,  F, C, E, D, A, B,  F, D, A, C, E, B,  F, E, D, A, C, B},
                // @OH6
                new[] {
                    A, B, C, E, F, D,  A, C, E, F, B, D,  A, E, F, B, C, D,  A, F, B, C, E, D,
                    B, A, F, D, C, E,  B, C, A, F, D, E,  B, D, C, A, F, E,  B, F, D, C, A, E,
                    C, A, B, D, E, F,  C, B, D, E, A, F,  C, D, E, A, B, F,  C, E, A, B, D, F,
                    D, B, F, E, C, A,  D, C, B, F, E, A,  D, E, C, B, F, A,  D, F, E, C, B, A,
                    E, A, C, D, F, B,  E, C, D, F, A, B,  E, D, F, A, C, B,  E, F, A, C, D, B,
                    F, A, E, D, B, C,  F, B, A, E, D, C,  F, D, B, A, E, C,  F, E, D, B, A, C},
                // @OH7
                new[] {
                    A, B, C, F, E, D,  A, C, F, E, B, D,  A, E, B, C, F, D,  A, F, E, B, C, D,
                    B, A, E, D, C, F,  B, C, A, E, D, F,  B, D, C, A, E, F,  B, E, D, C, A, F,
                    C, A, B, D, F, E,  C, B, D, F, A, E,  C, D, F, A, B, E,  C, F, A, B, D, E,
                    D, B, E, F, C, A,  D, C, B, E, F, A,  D, E, F, C, B, A,  D, F, C, B, E, A,
                    E, A, F, D, B, C,  E, B, A, F, D, C,  E, D, B, A, F, C,  E, F, D, B, A, C,
                    F, A, C, D, E, B,  F, C, D, E, A, B,  F, D, E, A, C, B,  F, E, A, C, D, B},
                // @OH8
                new[] {
                    A, B, D, C, E, F,  A, C, E, B, D, F,  A, D, C, E, B, F,  A, E, B, D, C, F,
                    B, A, E, F, D, C,  B, D, A, E, F, C,  B, E, F, D, A, C,  B, F, D, A, E, C,
                    C, A, D, F, E, B,  C, D, F, E, A, B,  C, E, A, D, F, B,  C, F, E, A, D, B,
                    D, A, B, F, C, E,  D, B, F, C, A, E,  D, C, A, B, F, E,  D, F, C, A, B, E,
                    E, A, C, F, B, D,  E, B, A, C, F, D,  E, C, F, B, A, D,  E, F, B, A, C, D,
                    F, B, E, C, D, A,  F, C, D, B, E, A,  F, D, B, E, C, A,  F, E, C, D, B, A},
                // @OH9
                new[] {
                    A, B, D, C, F, E,  A, C, F, B, D, E,  A, D, C, F, B, E,  A, F, B, D, C, E,
                    B, A, F, E, D, C,  B, D, A, F, E, C,  B, E, D, A, F, C,  B, F, E, D, A, C,
                    C, A, D, E, F, B,  C, D, E, F, A, B,  C, E, F, A, D, B,  C, F, A, D, E, B,
                    D, A, B, E, C, F,  D, B, E, C, A, F,  D, C, A, B, E, F,  D, E, C, A, B, F,
                    E, B, F, C, D, A,  E, C, D, B, F, A,  E, D, B, F, C, A,  E, F, C, D, B, A,
                    F, A, C, E, B, D,  F, B, A, C, E, D,  F, C, E, B, A, D,  F, E, B, A, C, D},
                // @OH10
                new[] {
                    A, B, E, C, D, F,  A, C, D, B, E, F,  A, D, B, E, C, F,  A, E, C, D, B, F,
                    B, A, D, F, E, C,  B, D, F, E, A, C,  B, E, A, D, F, C,  B, F, E, A, D, C,
                    C, A, E, F, D, B,  C, D, A, E, F, B,  C, E, F, D, A, B,  C, F, D, A, E, B,
                    D, A, C, F, B, E,  D, B, A, C, F, E,  D, C, F, B, A, E,  D, F, B, A, C, E,
                    E, A, B, F, C, D,  E, B, F, C, A, D,  E, C, A, B, F, D,  E, F, C, A, B, D,
                    F, B, D, C, E, A,  F, C, E, B, D, A,  F, D, C, E, B, A,  F, E, B, D, C, A},
                // @OH11
                new[] {
                    A, B, F, C, D, E,  A, C, D, B, F, E,  A, D, B, F, C, E,  A, F, C, D, B, E,
                    B, A, D, E, F, C,  B, D, E, F, A, C,  B, E, F, A, D, C,  B, F, A, D, E, C,
                    C, A, F, E, D, B,  C, D, A, F, E, B,  C, E, D, A, F, B,  C, F, E, D, A, B,
                    D, A, C, E, B, F,  D, B, A, C, E, F,  D, C, E, B, A, F,  D, E, B, A, C, F,
                    E, B, D, C, F, A,  E, C, F, B, D, A,  E, D, C, F, B, A,  E, F, B, D, C, A,
                    F, A, B, E, C, D,  F, B, E, C, A, D,  F, C, A, B, E, D,  F, E, C, A, B, D},
                // @OH12
                new[] {
                    A, B, E, C, F, D,  A, C, F, B, E, D,  A, E, C, F, B, D,  A, F, B, E, C, D,
                    B, A, F, D, E, C,  B, D, E, A, F, C,  B, E, A, F, D, C,  B, F, D, E, A, C,
                    C, A, E, D, F, B,  C, D, F, A, E, B,  C, E, D, F, A, B,  C, F, A, E, D, B,
                    D, B, F, C, E, A,  D, C, E, B, F, A,  D, E, B, F, C, A,  D, F, C, E, B, A,
                    E, A, B, D, C, F,  E, B, D, C, A, F,  E, C, A, B, D, F,  E, D, C, A, B, F,
                    F, A, C, D, B, E,  F, B, A, C, D, E,  F, C, D, B, A, E,  F, D, B, A, C, E},
                // @OH13
                new[] {
                    A, B, F, C, E, D,  A, C, E, B, F, D,  A, E, B, F, C, D,  A, F, C, E, B, D,
                    B, A, E, D, F, C,  B, D, F, A, E, C,  B, E, D, F, A, C,  B, F, A, E, D, C,
                    C, A, F, D, E, B,  C, D, E, A, F, B,  C, E, A, F, D, B,  C, F, D, E, A, B,
                    D, B, E, C, F, A,  D, C, F, B, E, A,  D, E, C, F, B, A,  D, F, B, E, C, A,
                    E, A, C, D, B, F,  E, B, A, C, D, F,  E, C, D, B, A, F,  E, D, B, A, C, F,
                    F, A, B, D, C, E,  F, B, D, C, A, E,  F, C, A, B, D, E,  F, D, C, A, B, E},
                // @OH14
                new[] {
                    A, B, D, E, C, F,  A, C, B, D, E, F,  A, D, E, C, B, F,  A, E, C, B, D, F,
                    B, A, C, F, D, E,  B, C, F, D, A, E,  B, D, A, C, F, E,  B, F, D, A, C, E,
                    C, A, E, F, B, D,  C, B, A, E, F, D,  C, E, F, B, A, D,  C, F, B, A, E, D,
                    D, A, B, F, E, C,  D, B, F, E, A, C,  D, E, A, B, F, C,  D, F, E, A, B, C,
                    E, A, D, F, C, B,  E, C, A, D, F, B,  E, D, F, C, A, B,  E, F, C, A, D, B,
                    F, B, C, E, D, A,  F, C, E, D, B, A,  F, D, B, C, E, A,  F, E, D, B, C, A},
                // @OH15
                new[] {
                    A, B, D, F, C, E,  A, C, B, D, F, E,  A, D, F, C, B, E,  A, F, C, B, D, E,
                    B, A, C, E, D, F,  B, C, E, D, A, F,  B, D, A, C, E, F,  B, E, D, A, C, F,
                    C, A, F, E, B, D,  C, B, A, F, E, D,  C, E, B, A, F, D,  C, F, E, B, A, D,
                    D, A, B, E, F, C,  D, B, E, F, A, C,  D, E, F, A, B, C,  D, F, A, B, E, C,
                    E, B, C, F, D, A,  E, C, F, D, B, A,  E, D, B, C, F, A,  E, F, D, B, C, A,
                    F, A, D, E, C, B,  F, C, A, D, E, B,  F, D, E, C, A, B,  F, E, C, A, D, B},
                // @OH16
                new[] {
                    A, B, F, D, C, E,  A, C, B, F, D, E,  A, D, C, B, F, E,  A, F, D, C, B, E,
                    B, A, C, E, F, D,  B, C, E, F, A, D,  B, E, F, A, C, D,  B, F, A, C, E, D,
                    C, A, D, E, B, F,  C, B, A, D, E, F,  C, D, E, B, A, F,  C, E, B, A, D, F,
                    D, A, F, E, C, B,  D, C, A, F, E, B,  D, E, C, A, F, B,  D, F, E, C, A, B,
                    E, B, C, D, F, A,  E, C, D, F, B, A,  E, D, F, B, C, A,  E, F, B, C, D, A,
                    F, A, B, E, D, C,  F, B, E, D, A, C,  F, D, A, B, E, C,  F, E, D, A, B, C},
                // @OH17
                new[] {
                    A, B, E, F, C, D,  A, C, B, E, F, D,  A, E, F, C, B, D,  A, F, C, B, E, D,
                    B, A, C, D, E, F,  B, C, D, E, A, F,  B, D, E, A, C, F,  B, E, A, C, D, F,
                    C, A, F, D, B, E,  C, B, A, F, D, E,  C, D, B, A, F, E,  C, F, D, B, A, E,
                    D, B, C, F, E, A,  D, C, F, E, B, A,  D, E, B, C, F, A,  D, F, E, B, C, A,
                    E, A, B, D, F, C,  E, B, D, F, A, C,  E, D, F, A, B, C,  E, F, A, B, D, C,
                    F, A, E, D, C, B,  F, C, A, E, D, B,  F, D, C, A, E, B,  F, E, D, C, A, B},
                // @OH18
                new[] {
                    A, B, F, E, C, D,  A, C, B, F, E, D,  A, E, C, B, F, D,  A, F, E, C, B, D,
                    B, A, C, D, F, E,  B, C, D, F, A, E,  B, D, F, A, C, E,  B, F, A, C, D, E,
                    C, A, E, D, B, F,  C, B, A, E, D, F,  C, D, B, A, E, F,  C, E, D, B, A, F,
                    D, B, C, E, F, A,  D, C, E, F, B, A,  D, E, F, B, C, A,  D, F, B, C, E, A,
                    E, A, F, D, C, B,  E, C, A, F, D, B,  E, D, C, A, F, B,  E, F, D, C, A, B,
                    F, A, B, D, E, C,  F, B, D, E, A, C,  F, D, E, A, B, C,  F, E, A, B, D, C},
                // @OH19
                new[] {
                    A, B, D, E, F, C,  A, D, E, F, B, C,  A, E, F, B, D, C,  A, F, B, D, E, C,
                    B, A, F, C, D, E,  B, C, D, A, F, E,  B, D, A, F, C, E,  B, F, C, D, A, E,
                    C, B, F, E, D, A,  C, D, B, F, E, A,  C, E, D, B, F, A,  C, F, E, D, B, A,
                    D, A, B, C, E, F,  D, B, C, E, A, F,  D, C, E, A, B, F,  D, E, A, B, C, F,
                    E, A, D, C, F, B,  E, C, F, A, D, B,  E, D, C, F, A, B,  E, F, A, D, C, B,
                    F, A, E, C, B, D,  F, B, A, E, C, D,  F, C, B, A, E, D,  F, E, C, B, A, D},
                // @OH20
                new[] {
                    A, B, D, F, E, C,  A, D, F, E, B, C,  A, E, B, D, F, C,  A, F, E, B, D, C,
                    B, A, E, C, D, F,  B, C, D, A, E, F,  B, D, A, E, C, F,  B, E, C, D, A, F,
                    C, B, E, F, D, A,  C, D, B, E, F, A,  C, E, F, D, B, A,  C, F, D, B, E, A,
                    D, A, B, C, F, E,  D, B, C, F, A, E,  D, C, F, A, B, E,  D, F, A, B, C, E,
                    E, A, F, C, B, D,  E, B, A, F, C, D,  E, C, B, A, F, D,  E, F, C, B, A, D,
                    F, A, D, C, E, B,  F, C, E, A, D, B,  F, D, C, E, A, B,  F, E, A, D, C, B},
                // @OH21
                new[] {
                    A, B, E, D, F, C,  A, D, F, B, E, C,  A, E, D, F, B, C,  A, F, B, E, D, C,
                    B, A, F, C, E, D,  B, C, E, A, F, D,  B, E, A, F, C, D,  B, F, C, E, A, D,
                    C, B, F, D, E, A,  C, D, E, B, F, A,  C, E, B, F, D, A,  C, F, D, E, B, A,
                    D, A, E, C, F, B,  D, C, F, A, E, B,  D, E, C, F, A, B,  D, F, A, E, C, B,
                    E, A, B, C, D, F,  E, B, C, D, A, F,  E, C, D, A, B, F,  E, D, A, B, C, F,
                    F, A, D, C, B, E,  F, B, A, D, C, E,  F, C, B, A, D, E,  F, D, C, B, A, E},
                // @OH22
                new[] {
                    A, B, F, D, E, C,  A, D, E, B, F, C,  A, E, B, F, D, C,  A, F, D, E, B, C,
                    B, A, E, C, F, D,  B, C, F, A, E, D,  B, E, C, F, A, D,  B, F, A, E, C, D,
                    C, B, E, D, F, A,  C, D, F, B, E, A,  C, E, D, F, B, A,  C, F, B, E, D, A,
                    D, A, F, C, E, B,  D, C, E, A, F, B,  D, E, A, F, C, B,  D, F, C, E, A, B,
                    E, A, D, C, B, F,  E, B, A, D, C, F,  E, C, B, A, D, F,  E, D, C, B, A, F,
                    F, A, B, C, D, E,  F, B, C, D, A, E,  F, C, D, A, B, E,  F, D, A, B, C, E},
                // @OH23
                new[] {
                    A, B, E, F, D, C,  A, D, B, E, F, C,  A, E, F, D, B, C,  A, F, D, B, E, C,
                    B, A, D, C, E, F,  B, C, E, A, D, F,  B, D, C, E, A, F,  B, E, A, D, C, F,
                    C, B, D, F, E, A,  C, D, F, E, B, A,  C, E, B, D, F, A,  C, F, E, B, D, A,
                    D, A, F, C, B, E,  D, B, A, F, C, E,  D, C, B, A, F, E,  D, F, C, B, A, E,
                    E, A, B, C, F, D,  E, B, C, F, A, D,  E, C, F, A, B, D,  E, F, A, B, C, D,
                    F, A, E, C, D, B,  F, C, D, A, E, B,  F, D, A, E, C, B,  F, E, C, D, A, B},
                // @OH24
                new[] {
                    A, B, F, E, D, C,  A, D, B, F, E, C,  A, E, D, B, F, C,  A, F, E, D, B, C,
                    B, A, D, C, F, E,  B, C, F, A, D, E,  B, D, C, F, A, E,  B, F, A, D, C, E,
                    C, B, D, E, F, A,  C, D, E, F, B, A,  C, E, F, B, D, A,  C, F, B, D, E, A,
                    D, A, E, C, B, F,  D, B, A, E, C, F,  D, C, B, A, E, F,  D, E, C, B, A, F,
                    E, A, F, C, D, B,  E, C, D, A, F, B,  E, D, A, F, C, B,  E, F, C, D, A, B,
                    F, A, B, C, E, D,  F, B, C, E, A, D,  F, C, E, A, B, D,  F, E, A, B, C, D},
                // @OH25
                new[] {
                    A, C, D, E, F, B,  A, D, E, F, C, B,  A, E, F, C, D, B,  A, F, C, D, E, B,
                    B, C, F, E, D, A,  B, D, C, F, E, A,  B, E, D, C, F, A,  B, F, E, D, C, A,
                    C, A, F, B, D, E,  C, B, D, A, F, E,  C, D, A, F, B, E,  C, F, B, D, A, E,
                    D, A, C, B, E, F,  D, B, E, A, C, F,  D, C, B, E, A, F,  D, E, A, C, B, F,
                    E, A, D, B, F, C,  E, B, F, A, D, C,  E, D, B, F, A, C,  E, F, A, D, B, C,
                    F, A, E, B, C, D,  F, B, C, A, E, D,  F, C, A, E, B, D,  F, E, B, C, A, D},
                // @OH26
                new[] {
                    A, C, D, F, E, B,  A, D, F, E, C, B,  A, E, C, D, F, B,  A, F, E, C, D, B,
                    B, C, E, F, D, A,  B, D, C, E, F, A,  B, E, F, D, C, A,  B, F, D, C, E, A,
                    C, A, E, B, D, F,  C, B, D, A, E, F,  C, D, A, E, B, F,  C, E, B, D, A, F,
                    D, A, C, B, F, E,  D, B, F, A, C, E,  D, C, B, F, A, E,  D, F, A, C, B, E,
                    E, A, F, B, C, D,  E, B, C, A, F, D,  E, C, A, F, B, D,  E, F, B, C, A, D,
                    F, A, D, B, E, C,  F, B, E, A, D, C,  F, D, B, E, A, C,  F, E, A, D, B, C},
                // @OH27
                new[] {
                    A, C, E, D, F, B,  A, D, F, C, E, B,  A, E, D, F, C, B,  A, F, C, E, D, B,
                    B, C, F, D, E, A,  B, D, E, C, F, A,  B, E, C, F, D, A,  B, F, D, E, C, A,
                    C, A, F, B, E, D,  C, B, E, A, F, D,  C, E, A, F, B, D,  C, F, B, E, A, D,
                    D, A, E, B, F, C,  D, B, F, A, E, C,  D, E, B, F, A, C,  D, F, A, E, B, C,
                    E, A, C, B, D, F,  E, B, D, A, C, F,  E, C, B, D, A, F,  E, D, A, C, B, F,
                    F, A, D, B, C, E,  F, B, C, A, D, E,  F, C, A, D, B, E,  F, D, B, C, A, E},
                // @OH28
                new[] {
                    A, C, F, D, E, B,  A, D, E, C, F, B,  A, E, C, F, D, B,  A, F, D, E, C, B,
                    B, C, E, D, F, A,  B, D, F, C, E, A,  B, E, D, F, C, A,  B, F, C, E, D, A,
                    C, A, E, B, F, D,  C, B, F, A, E, D,  C, E, B, F, A, D,  C, F, A, E, B, D,
                    D, A, F, B, E, C,  D, B, E, A, F, C,  D, E, A, F, B, C,  D, F, B, E, A, C,
                    E, A, D, B, C, F,  E, B, C, A, D, F,  E, C, A, D, B, F,  E, D, B, C, A, F,
                    F, A, C, B, D, E,  F, B, D, A, C, E,  F, C, B, D, A, E,  F, D, A, C, B, E},
                // @OH29
                new[] {
                    A, C, E, F, D, B,  A, D, C, E, F, B,  A, E, F, D, C, B,  A, F, D, C, E, B,
                    B, C, D, F, E, A,  B, D, F, E, C, A,  B, E, C, D, F, A,  B, F, E, C, D, A,
                    C, A, D, B, E, F,  C, B, E, A, D, F,  C, D, B, E, A, F,  C, E, A, D, B, F,
                    D, A, F, B, C, E,  D, B, C, A, F, E,  D, C, A, F, B, E,  D, F, B, C, A, E,
                    E, A, C, B, F, D,  E, B, F, A, C, D,  E, C, B, F, A, D,  E, F, A, C, B, D,
                    F, A, E, B, D, C,  F, B, D, A, E, C,  F, D, A, E, B, C,  F, E, B, D, A, C},
                // @OH30
                new[] {
                    A, C, F, E, D, B,  A, D, C, F, E, B,  A, E, D, C, F, B,  A, F, E, D, C, B,
                    B, C, D, E, F, A,  B, D, E, F, C, A,  B, E, F, C, D, A,  B, F, C, D, E, A,
                    C, A, D, B, F, E,  C, B, F, A, D, E,  C, D, B, F, A, E,  C, F, A, D, B, E,
                    D, A, E, B, C, F,  D, B, C, A, E, F,  D, C, A, E, B, F,  D, E, B, C, A, F,
                    E, A, F, B, D, C,  E, B, D, A, F, C,  E, D, A, F, B, C,  E, F, B, D, A, C,
                    F, A, C, B, E, D,  F, B, E, A, C, D,  F, C, B, E, A, D,  F, E, A, C, B, D}
            };

            public Octahedral(int u, int[] vs, int order)
            {
                if (vs.Length != 6)
                    throw new ArgumentException("Octahedral topology requires 6 vertices");
                this.u = u;
                this.vs = vs;
                this.order = order;
            }

            /// <inheritdoc/>
            public override int Atom => u;

            /// <inheritdoc/>
            public override Configuration Configuration
            {
                get
                {
                    if (order >= 1 && order <= 30)
                        return Configuration.Values[Configuration.OH1.Ordinal + order - 1];
                    return Configuration.Unknown;
                }
            }

            /// <inheritdoc/>
            public override Topology OrderBy(int[] rank)
            {
                int[] src = Topology.ApplyInv(vs, PERMUTATIONS[order - 1]);
                int[] dst = Arrays.Clone(src);
                IndirectSort(dst, rank);

                for (int i = 1; i <= 30; i++)
                {
                    if (Topology.Check(dst, src, PERMUTATIONS[i - 1], 6, 4))
                        return new Octahedral(u, dst, i);
                }

                return null;
            }

            /// <inheritdoc/>
            public override Topology Transform(int[] mapping)
            {
                int[] ws = new int[vs.Length];
                for (int i = 0; i < vs.Length; i++)
                    ws[i] = mapping[vs[i]];
                return new Octahedral(mapping[u], ws, order);
            }

            public override void Copy(int[] dest)
            {
                Array.Copy(vs, 0, dest, 0, vs.Length);
            }

            public override string ToString()
            {
                return u + " " + Arrays.ToJavaString(vs) + ":" + order;
            }
        }

        private sealed class Trigonal : Topology
        {
            private readonly int u;
            private readonly int[] vs;
            private readonly int p;

            public Trigonal(int u, int[] vs, int p)
            {
                if (vs.Length != 3)
                    throw new ArgumentOutOfRangeException(nameof(vs), "Trigonal topology requires 3 vertices - use the 'centre' vertex to mark implicit verticies");
                this.u = u;
                this.vs = vs;
                this.p = p;
            }

            /// <inheritdoc/>
            public override int Atom => u;

            /// <inheritdoc/>
            public override Configuration Configuration
                => p < 0 ? Configuration.DB1 : Configuration.DB2;

            /// <inheritdoc/>
            public override Topology OrderBy(int[] rank)
            {
                return new Trigonal(u,
                                    Sort(vs, rank),
                                    p * Parity(vs, rank));
            }

            /// <inheritdoc/>
            public override Topology Transform(int[] mapping)
            {
                int[] ws = new int[vs.Length];
                for (int i = 0; i < vs.Length; i++)
                    ws[i] = mapping[vs[i]];
                return new Trigonal(mapping[u], ws, p);
            }

            public override void Copy(int[] dest)
            {
                Array.Copy(vs, 0, dest, 0, 3);
            }

            public override string ToString()
            {
                return u + " " + Arrays_ToString(vs) + ":" + p;
            }
        }

        private static string Arrays_ToString(int[] vs)
        {
            var sb = new StringBuilder();
            sb.Append("[");
            foreach (var v in vs)
            {
                if (sb.Length > 1)
                    sb.Append(", ");
                sb.Append(v);
            }
            sb.Append("]");
            return sb.ToString();
        }
    }
}

