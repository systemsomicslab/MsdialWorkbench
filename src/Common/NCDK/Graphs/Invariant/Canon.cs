/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace NCDK.Graphs.Invariant
{
    /// <summary>
    /// An implementation based on the canon algorithm <token>cdk-cite-WEI89</token>. The
    /// algorithm uses an initial set of of invariants which are assigned a rank.
    /// Equivalent ranks are then shattered using an unambiguous function (in this
    /// case, the product of primes of adjacent ranks). Once no more equivalent ranks
    /// can be shattered ties are artificially broken and rank shattering continues.
    /// Unlike the original description rank stability is not maintained reducing
    /// the number of values to rank at each stage to only those which are equivalent.
    /// </summary>
    /// <remarks>
    /// The initial set of invariants is basic and are - <i>
    /// "sufficient for the purpose of obtaining unique notation for simple SMILES,
    ///  but it is not necessarily a “complete” set. No “perfect” set of invariants
    ///  is known that will distinguish all possible graph asymmetries. However,
    ///  for any given set of structures, a set of invariants can be devised to
    ///  provide the necessary discrimination"</i> <token>cdk-cite-WEI89</token>. As such this
    ///  producer should not be considered a complete canonical labelled but in
    ///  practice performs well. For a more accurate and computationally expensive
    ///  labelling, please using the <see cref="InChINumbersTools"/>.
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.Invariant.Canon_Example.cs"]/*' />
    /// </example>
    // @author John May
    // @cdk.module standard
    public sealed class Canon
    {
        private const int N_PRIMES = 10000;
        /// <summary>
        /// Graph, adjacency list representation.
        /// </summary>
        private readonly int[][] g;

        /// <summary>
        /// Storage of canon labelling and symmetry classes.
        /// </summary>
        private readonly long[] labelling, symmetry;

        /// <summary>Only compute the symmetry classes.</summary>
        private readonly bool symOnly = false;

        /// <summary>
        /// Create a canon labelling for the graph (<paramref name="g"/>) with the specified invariants.
        /// </summary>
        /// <param name="g">a graph (adjacency list representation)</param>
        /// <param name="hydrogens">binary vector of terminal hydrogens</param>
        /// <param name="partition">an initial partition of the vertices</param>
        /// <param name="symOnly"></param>
        private Canon(int[][] g, long[] partition, bool[] hydrogens, bool symOnly)
        {
            this.g = g;
            this.symOnly = symOnly;
            labelling = (long[])partition.Clone();
            symmetry = Refine(labelling, hydrogens);
        }

        /// <summary>
        /// Compute the canonical labels for the provided structure. The labelling
        /// does not consider isomer information or stereochemistry. The current
        /// implementation does not fully distinguish all structure topologies
        /// but in practise performs well in the majority of cases. A complete
        /// canonical labelling can be obtained using the <see cref="InChINumbersTools"/>
        /// but is computationally much more expensive.
        /// </summary>
        /// <param name="container">structure</param>
        /// <param name="g">adjacency list graph representation</param>
        /// <returns>the canonical labelling</returns>
        /// <seealso cref="EquivalentClassPartitioner"/>
        /// <seealso cref="InChINumbersTools"/>
        public static long[] Label(IAtomContainer container, int[][] g)
        {
            return Label(container, g, BasicInvariants(container, g));
        }

        /// <summary>
        /// Compute the canonical labels for the provided structure. The labelling
        /// does not consider isomer information or stereochemistry. This method
        /// allows provision of a custom array of initial invariants.
        /// </summary>
        /// <remarks>
        /// The current
        /// implementation does not fully distinguish all structure topologies
        /// but in practise performs well in the majority of cases. A complete
        /// canonical labelling can be obtained using the <see cref="InChINumbersTools"/>
        /// but is computationally much more expensive.
        /// </remarks>
        /// <param name="container">structure</param>
        /// <param name="g">adjacency list graph representation</param>
        /// <param name="initial    ">initial seed invariants</param>
        /// <returns>the canonical labelling</returns>
        /// <seealso cref="EquivalentClassPartitioner"/>
        /// <seealso cref="InChINumbersTools"/>
        public static long[] Label(IAtomContainer container, int[][] g, long[] initial)
        {
            if (initial.Length != g.Length)
                throw new ArgumentException("number of initial != number of atoms");
            return new Canon(g, initial, TerminalHydrogens(container, g), false).labelling;
        }

        /// <summary>
        /// Compute the canonical labels for the provided structure. The initial
        /// labelling is seed-ed with the provided atom comparator <paramref name="cmp"/>
        /// allowing arbitary properties to be distinguished or ignored.
        /// </summary>
        /// <param name="container">structure</param>
        /// <param name="g">adjacency list graph representation</param>
        /// <param name="cmp">comparator to compare atoms</param>
        /// <returns>the canonical labelling</returns>
        public static long[] Label(IAtomContainer container,
                                   int[][] g,
                                   IComparer<IAtom> cmp)
        {
            if (g.Length == 0)
                return new long[0];
            var atoms = new List<IAtom>(container.Atoms);
            atoms.Sort(cmp);
            var initial = new long[atoms.Count];
            long part = 1;
            initial[container.Atoms.IndexOf(atoms[0])] = part;
            for (int i = 1; i < atoms.Count; i++)
            {
                if (cmp.Compare(atoms[i], atoms[i - 1]) != 0)
                    ++part;
                initial[container.Atoms.IndexOf(atoms[i])] = part;
            }
            return Label(container, g, initial);
        }

        /// <summary>
        /// Compute the symmetry classes for the provided structure. There are known
        /// examples where symmetry is incorrectly found. The <see cref="EquivalentClassPartitioner"/> 
        /// gives more accurate symmetry perception but
        /// this method is very quick and in practise successfully portions the
        /// majority of chemical structures.
        /// </summary>
        /// <param name="container">structure</param>
        /// <param name="g">adjacency list graph representation</param>
        /// <returns>symmetry classes</returns>
        /// <seealso cref="EquivalentClassPartitioner"/>
        public static long[] Symmetry(IAtomContainer container, int[][] g)
        {
            return new Canon(g, BasicInvariants(container, g), TerminalHydrogens(container, g), true).symmetry;
        }

        /// <summary>
        /// Internal - refine invariants to a canonical labelling and symmetry classes.
        /// </summary>
        /// <param name="invariants">the invariants to refine (canonical labelling gets written here)</param>
        /// <param name="hydrogens">binary vector of terminal hydrogens</param>
        /// <returns>the symmetry classes</returns>
        private long[] Refine(long[] invariants, bool[] hydrogens)
        {
            int ord = g.Length;

            InvariantRanker ranker = new InvariantRanker(ord);

            // current/next vertices, these only hold the vertices which are
            // equivalent
            int[] currVs = new int[ord];
            int[] nextVs = new int[ord];

            // fill with identity (also set number of non-unique)
            int nnu = ord;
            for (int i = 0; i < ord; i++)
                currVs[i] = i;

            long[] prev = invariants;
            long[] curr = Arrays.CopyOf(invariants, ord);

            // initially all labels are 1, the input invariants are then used to
            // refine this coarse partition
            Arrays.Fill(prev, 1L);

            // number of ranks
            int n = 0, m = 0;

            // storage of symmetry classes
            long[] symmetry = null;

            while (n < ord)
            {
                // refine the initial invariants using product of primes from
                // adjacent ranks
                while ((n = ranker.Rank(currVs, nextVs, nnu, curr, prev)) > m && n < ord)
                {
                    nnu = 0;
                    for (int i = 0; i < ord && nextVs[i] >= 0; i++)
                    {
                        int v = nextVs[i];
                        currVs[nnu++] = v;
                        curr[v] = hydrogens[v] ? prev[v] : PrimeProduct(g[v], prev, hydrogens);
                    }
                    m = n;
                }

                if (symmetry == null)
                {
                    // After symmetry classes have been found without hydrogens we add
                    // back in the hydrogens and assign ranks. We don't refine the
                    // partition until the next time round the while loop to avoid
                    // artificially splitting due to hydrogen representation, for example
                    // the two hydrogens are equivalent in this SMILES for ethane '[H]CC'
                    for (int i = 0; i < g.Length; i++)
                    {
                        if (hydrogens[i])
                        {
                            curr[i] = prev[g[i][0]];
                            hydrogens[i] = false;
                        }
                    }
                    n = ranker.Rank(currVs, nextVs, nnu, curr, prev);
                    symmetry = Arrays.CopyOf(prev, ord);

                    // Update the buffer of non-unique vertices as hydrogens next
                    // to discrete heavy atoms are also discrete (and removed from
                    // 'nextVs' during ranking.
                    nnu = 0;
                    for (int i = 0; i < ord && nextVs[i] >= 0; i++)
                    {
                        currVs[nnu++] = nextVs[i];
                    }
                }

                // partition is discrete or only symmetry classes are needed
                if (symOnly || n == ord) return symmetry;

                // artificially split the lowest cell, we perturb the value
                // of all vertices with equivalent rank to the lowest non-unique
                // vertex
                int lo = nextVs[0];
                for (int i = 1; i < ord && nextVs[i] >= 0 && prev[nextVs[i]] == prev[lo]; i++)
                    prev[nextVs[i]]++;

                // could also swap but this is cleaner
                Array.Copy(nextVs, 0, currVs, 0, nnu);
            }

            return symmetry;
        }

        /// <summary>
        /// Compute the prime product of the values (ranks) for the given
        /// adjacent neighbors (ws).
        /// </summary>
        /// <param name="ws">indices (adjacent neighbors)</param>
        /// <param name="ranks">invariant ranks</param>
        /// <param name="hydrogens"></param>
        /// <returns>the prime product</returns>
        private static long PrimeProduct(int[] ws, long[] ranks, bool[] hydrogens)
        {
            long prod = 1;
            foreach (var w in ws)
            {
                if (!hydrogens[w])
                {
                    prod *= PRIMES[(int)ranks[w]];
                }
            }
            return prod;
        }

        /// <summary>
        /// Generate the initial invariants for each atom in the <paramref name="container"/>.
        /// The labels use the invariants described in <token>cdk-cite-WEI89</token>. 
        /// </summary>
        /// <remarks>
        /// The bits in the low 32-bits are: "0000000000xxxxXXXXeeeeeeescchhhh"
        /// where:
        /// <list type="bullet">
        ///     <item>0: padding</item>
        ///     <item>x: number of connections</item>
        ///     <item>X: number of non-hydrogens bonds</item>
        ///     <item>e: atomic number</item>
        ///     <item>s: sign of charge</item>
        ///     <item>c: absolute charge</item>
        ///     <item>h: number of attached hydrogens</item>
        /// </list>
        /// <b>Important: These invariants are <i>basic</i> and there are known
        /// examples they don't distinguish. One trivial example to consider is
        /// "[O]C=O" where both oxygens have no hydrogens and a single
        /// connection but the atoms are not equivalent. Including a better
        /// initial partition is more expensive</b>
        /// </remarks>
        /// <param name="container">an atom container to generate labels for</param>
        /// <param name="graph">graph representation (adjacency list)</param>
        /// <returns>initial invariants</returns>
        /// <exception cref="NullReferenceException">an atom had unset atomic number, hydrogen count or formal charge</exception>
        public static long[] BasicInvariants(IAtomContainer container, int[][] graph)
        {
            long[] labels = new long[graph.Length];

            for (int v = 0; v < graph.Length; v++)
            {
                IAtom atom = container.Atoms[v];

                int deg = graph[v].Length;
                int impH = GetImplH(atom);
                int expH = 0;
                int elem = GetAtomicNumber(atom);
                int chg = Charge(atom);

                // count non-suppressed (explicit) hydrogens
                foreach (var w in graph[v])
                    if (GetAtomicNumber(container.Atoms[w]) == 1) expH++;

                long label = 0; // connectivity (first in)
                label |= (long)(deg + impH & 0xf);
                label <<= 4; // connectivity (heavy) <= 15 (4 bits)
                label |= (long)(deg - expH & 0xf);
                label <<= 7; // atomic number <= 127 (7 bits)
                label |= (long)(elem & 0x7f);
                label <<= 1; // charge sign == 1 (1 bit)
                label |= (long)(chg >> 31 & 0x1);
                label <<= 2; // charge <= 3 (2 bits)
                label |= (long)(Math.Abs(chg) & 0x3);
                label <<= 4; // hydrogen count <= 15 (4 bits)
                label |= (long)(impH + expH & 0xf);

                labels[v] = label;
            }
            return labels;
        }

        /// <summary>
        /// Access atomic number of atom defaulting to 0 for pseudo atoms.
        /// </summary>
        /// <param name="atom">an atom</param>
        /// <returns>the atomic number</returns>
        /// <exception cref="NullReferenceException">the atom was non-pseudo at did not have an atomic number</exception>
        private static int GetAtomicNumber(IAtom atom)
        {
            int? elem = atom.AtomicNumber;
            if (elem.HasValue) return elem.Value;
            if (atom is IPseudoAtom) return 0;
            throw new NullReferenceException("a non-pseudoatom had unset atomic number");
        }

        /// <summary>
        /// Access implicit hydrogen count of the atom defaulting to 0 for pseudo atoms.
        /// </summary>
        /// <param name="atom">an atom</param>
        /// <returns>the implicit hydrogen count</returns>
        /// <exception cref="NullReferenceException">the atom was non-pseudo at did not have an implicit hydrogen count</exception>
        private static int GetImplH(IAtom atom)
        {
            int? h = atom.ImplicitHydrogenCount;
            if (h.HasValue) return h.Value;
            if (atom is IPseudoAtom) return 0;
            throw new NullReferenceException("a non-pseudoatom had unset hydrogen count");
        }

        /// <summary>
        /// Access formal charge of an atom defaulting to 0 if undefined.
        /// </summary>
        /// <param name="atom">an atom</param>
        /// <returns>the formal charge</returns>
        private static int Charge(IAtom atom)
        {
            int? charge = atom.FormalCharge;
            if (charge.HasValue) return charge.Value;
            return 0;
        }

        /// <summary>
        /// Locate explicit hydrogens that are attached to exactly one other atom.
        /// </summary>
        /// <param name="ac">a structure</param>
        /// <param name="g"></param>
        /// <returns>binary set of terminal hydrogens</returns>
        public static bool[] TerminalHydrogens(IAtomContainer ac, int[][] g)
        {
            bool[] hydrogens = new bool[ac.Atoms.Count];

            // we specifically don't check for null atomic number, this must be set.
            // if not, something major is wrong
            for (int i = 0; i < ac.Atoms.Count; i++)
            {
                var atom = ac.Atoms[i];
                hydrogens[i] = atom.AtomicNumber == 1 &&
                               atom.MassNumber == null &&
                               g[i].Length == 1;
            }

            return hydrogens;
        }

        /// <summary>
        /// The first 10,000 primes.
        /// </summary>
        private static readonly int[] PRIMES = LoadPrimes();

        private static int[] LoadPrimes()
        {
            try
            {
                int[] primes = new int[N_PRIMES];
                using (var br = new StreamReader(ResourceLoader.GetAsStream(typeof(Canon), "primes.dat")))
                {
                    int i = 0;
                    string line = null;
                    while ((line = br.ReadLine()) != null)
                    {
                        primes[i++] = int.Parse(line, NumberFormatInfo.InvariantInfo);
                    }
                    Trace.Assert(i == N_PRIMES);
                }
                return primes;
            }
            catch (FormatException)
            {
                Console.Error.WriteLine("Critical - could not load primes table for canonical labelling!");
                return Array.Empty<int>();
            }
        }
    }
}
