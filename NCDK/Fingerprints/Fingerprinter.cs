/* Copyright (C) 2002-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Aromaticities;
using NCDK.Common.Mathematics;
using NCDK.Common.Primitives;
using NCDK.Graphs;
using NCDK.RingSearches;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Generates a fingerprint for a given <see cref="IAtomContainer"/>. Fingerprints are
    /// one-dimensional bit arrays, where bits are set according to a the
    /// occurrence of a particular structural feature (See for example the
    /// Daylight inc. theory manual for more information). Fingerprints allow for
    /// a fast screening step to exclude candidates for a substructure search in a
    /// database. They are also a means for determining the similarity of chemical
    /// structures. 
    /// <para>
    /// The FingerPrinter assumes that hydrogens are explicitly given! 
    /// Furthermore, if pseudo atoms or atoms with malformed symbols are present,
    /// their atomic number is taken as one more than the last element currently 
    /// supported in <see cref="PeriodicTable"/>.</para>
    /// </summary>
    /// <example>
    /// A fingerprint is generated for an <see cref="IAtomContainer"/> with this code: 
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Fingerprints.Fingerprinter_Example.cs"]/*' />
    /// </example>
    /// <remarks>
    /// <note type="warning">
    /// The aromaticity detection for this
    /// FingerPrinter relies on AllRingsFinder, which is known to take very long
    /// for some molecules with many cycles or special cyclic topologies. Thus,
    /// the AllRingsFinder has a built-in timeout of 5 seconds after which it
    /// aborts and Exception. If you want your SMILES generated at any
    /// expense, you need to create your own AllRingsFinder, set the timeout to a
    /// higher value, and assign it to this FingerPrinter. In the vast majority of
    /// cases, however, the defaults will be fine. </note>
    /// <note type="warning">The daylight manual says:
    /// "Fingerprints are not so definite: if a fingerprint indicates a pattern is
    /// missing then it certainly is, but it can only indicate a pattern's presence
    /// with some probability." In the case of very small molecules, the
    /// probability that you get the same fingerprint for different molecules is
    /// high.</note>
    /// </remarks>
    // @author         steinbeck
    // @cdk.created    2002-02-24
    // @cdk.keyword    fingerprint
    // @cdk.keyword    similarity
    // @cdk.module     standard
    public class Fingerprinter : AbstractFingerprinter, IFingerprinter
    {
        /// <summary>Throw an exception if too many paths (per atom) are generated.</summary>
        private const int DefaultPathLimit = 42000;

        /// <summary>The default length of created fingerprints.</summary>
        public const int DefaultSize = 1024;
        /// <summary>The default search depth used to create the fingerprints.</summary>
        public const int DefaultSearchDepth = 7;

        private readonly int size;
        private int pathLimit = DefaultPathLimit;

        private bool hashPseudoAtoms = false;

        /// <summary>
        /// Creates a fingerprint generator of length <see cref="DefaultSize"/> 
        /// and with a search depth of <see cref="DefaultSearchDepth"/>.
        /// </summary>
        public Fingerprinter()
            : this(DefaultSize, DefaultSearchDepth)
        { }

        public Fingerprinter(int size)
            : this(size, DefaultSearchDepth)
        { }

        /// <summary>
        /// Constructs a fingerprint generator that creates fingerprints of
        /// the given size, using a generation algorithm with the given search
        /// depth.
        /// </summary>
        /// <param name="size">The desired size of the fingerprint</param>
        /// <param name="searchDepth">The desired depth of search (number of bonds)</param>
        public Fingerprinter(int size, int searchDepth)
        {
            this.size = size;
            this.SearchDepth = searchDepth;
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            yield return new KeyValuePair<string, string>("searchDepth", SearchDepth.ToString(NumberFormatInfo.InvariantInfo));
            yield return new KeyValuePair<string, string>("pathLimit", pathLimit.ToString(NumberFormatInfo.InvariantInfo));
            yield return new KeyValuePair<string, string>("hashPseudoAtoms", hashPseudoAtoms.ToString(NumberFormatInfo.InvariantInfo));
            yield break;
        }

        /// <summary>
        /// Generates a fingerprint of the default size for the given AtomContainer.
        /// </summary>
        /// <param name="container">The AtomContainer for which a Fingerprint is generated</param>
        /// <param name="ringFinder">An instance of <see cref="AllRingsFinder"/></param>
        /// <exception cref="CDKException">if there is a timeout in ring or aromaticity perception</exception>
        /// <returns>A <see cref="BitArray"/> representing the fingerprint</returns>
        public IBitFingerprint GetBitFingerprint(IAtomContainer container, AllRingsFinder ringFinder)
        {
            Debug.WriteLine("Entering Fingerprinter");
            Debug.WriteLine("Starting Aromaticity Detection");
            var before = DateTime.Now.Ticks;
            if (!HasPseudoAtom(container.Atoms))
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                Aromaticity.CDKLegacy.Apply(container);
            }
            var after = DateTime.Now.Ticks;
            Debug.WriteLine($"time for aromaticity calculation: {after - before} ticks");
            Debug.WriteLine("Finished Aromaticity Detection");
            BitArray bitSet = new BitArray(size);
            EncodePaths(container, SearchDepth, bitSet, size);

            return new BitSetFingerprint(bitSet);
        }

        /// <summary>
        /// Generates a fingerprint of the default size for the given AtomContainer.
        /// </summary>
        /// <param name="container">The AtomContainer for which a Fingerprint is generated</param>
        public override IBitFingerprint GetBitFingerprint(IAtomContainer container)
        {
            return GetBitFingerprint(container, null);
        }

        /// <inheritdoc/>
        public override IReadOnlyDictionary<string, int> GetRawFingerprint(IAtomContainer iAtomContainer)
        {
            throw new NotSupportedException();
        }

        private static IBond FindBond(List<IBond> bonds, IAtom beg, IAtom end)
        {
            foreach (IBond bond in bonds)
                if (bond.Contains(beg) && bond.Contains(end))
                    return bond;
            return null;
        }

        private string EncodePath(IAtomContainer mol, Dictionary<IAtom, List<IBond>> cache, List<IAtom> path, StringBuilder buffer)
        {
            buffer.Clear();
            var prev = path[0];
            buffer.Append(GetAtomSymbol(prev));
            for (int i = 1; i < path.Count; i++)
            {
                var next = path[i];
                var bonds = cache[prev];

                if (bonds == null)
                {
                    bonds = mol.GetConnectedBonds(prev).ToList();
                    cache[prev] = bonds;
                }

                var bond = FindBond(bonds, next, prev);
                if (bond == null)
                    throw new InvalidOperationException("FATAL - Atoms in patch were connected?");
                buffer.Append(GetBondSymbol(bond));
                buffer.Append(GetAtomSymbol(next));
                prev = next;
            }
            return buffer.ToString();
        }

        private string EncodePath(List<IAtom> apath, List<IBond> bpath, StringBuilder buffer)
        {
            buffer.Clear();
            var prev = apath[0];
            buffer.Append(GetAtomSymbol(prev));
            for (int i = 1; i < apath.Count; i++)
            {
                var next = apath[i];
                var bond = bpath[i - 1];
                buffer.Append(GetBondSymbol(bond));
                buffer.Append(GetAtomSymbol(next));
            }
            return buffer.ToString();
        }

        private static int AppendHash(int hash, string str)
        {
            var len = str.Length;
            for (int i = 0; i < len; i++)
                hash = 31 * hash + str[0];
            return hash;
        }

        private int HashPath(List<IAtom> apath, List<IBond> bpath)
        {
            int hash = 0;
            hash = AppendHash(hash, GetAtomSymbol(apath[0]));
            for (int i = 1; i < apath.Count; i++)
            {
                var next = apath[i];
                var bond = bpath[i - 1];
                hash = AppendHash(hash, GetBondSymbol(bond));
                hash = AppendHash(hash, GetAtomSymbol(next));
            }
            return hash;
        }

        private int HashRevPath(List<IAtom> apath, List<IBond> bpath)
        {
            int hash = 0;
            int last = apath.Count - 1;
            hash = AppendHash(hash, GetAtomSymbol(apath[last]));
            for (int i = last - 1; i >= 0; i--)
            {
                var next = apath[i];
                var bond = bpath[i];
                hash = AppendHash(hash, GetBondSymbol(bond));
                hash = AppendHash(hash, GetAtomSymbol(next));
            }
            return hash;
        }

        private class State
        {
            internal int numPaths = 0;
            private JavaRandom rand = new JavaRandom(0);
            private BitArray fp;
            private IAtomContainer mol;
            private HashSet<IAtom> visited = new HashSet<IAtom>();
            internal List<IAtom> apath = new List<IAtom>();
            internal List<IBond> bpath = new List<IBond>();
            internal readonly int maxDepth;
            private readonly int fpsize;
            private Dictionary<IAtom, List<IBond>> cache = new Dictionary<IAtom, List<IBond>>();
            public StringBuilder buffer = new StringBuilder();

            public State(IAtomContainer mol, BitArray fp, int fpsize, int maxDepth)
            {
                this.mol = mol;
                this.fp = fp;
                this.fpsize = fpsize;
                this.maxDepth = maxDepth;
            }

            internal List<IBond> GetBonds(IAtom atom)
            {
                if (!cache.TryGetValue(atom, out List<IBond> bonds))
                {
                    bonds = mol.GetConnectedBonds(atom).ToList();
                    cache[atom] = bonds;
                }
                return bonds;
            }

            internal bool Visit(IAtom a)
            {
                return visited.Add(a);
            }

            internal bool Unvisit(IAtom a)
            {
                return visited.Remove(a);
            }

            internal void Push(IAtom atom, IBond bond)
            {
                apath.Add(atom);
                if (bond != null)
                    bpath.Add(bond);
            }

            internal void Pop()
            {
                if (apath.Any())
                    apath.RemoveAt(apath.Count - 1);
                if (bpath.Any())
                    bpath.RemoveAt(bpath.Count - 1);
            }

            internal void AddHash(int x)
            {
                numPaths++;
                rand = new JavaRandom(x);
                // XXX: fp.set(x % size); would work just as well but would encode a
                //      different bit
                fp.Set(rand.Next(fpsize), true);
            }
        }

        private void TraversePaths(State state, IAtom beg, IBond prev)
        {
            if (!hashPseudoAtoms && IsPseudo(beg))
                return;
            state.Push(beg, prev);
            state.AddHash(EncodeUniquePath(state.apath, state.bpath, state.buffer));
            if (state.numPaths > pathLimit)
                throw new CDKException("Too many paths! Structure is likely a cage, reduce path length or increase path limit");
            if (state.apath.Count < state.maxDepth)
            {
                foreach (IBond bond in state.GetBonds(beg))
                {
                    if (bond.Equals(prev))
                        continue;
                    IAtom nbr = bond.GetOther(beg);
                    if (state.Visit(nbr))
                    {
                        TraversePaths(state, nbr, bond);
                        state.Unvisit(nbr); // traverse all paths
                    }
                }
            }
            state.Pop();
        }

        /// <summary>
        /// Get all paths of lengths 0 to the specified length.
        /// </summary>
        /// <remarks>
        /// This method will find all paths up to length N starting from each
        /// atom in the molecule and return the unique set of such paths.
        /// </remarks>
        /// <param name="container">The molecule to search</param>
        /// <param name="searchDepth">The maximum path length desired</param>
        /// <returns>A array of path strings, keyed on themselves</returns>
        [Obsolete("Use " + nameof(EncodePath) + "(IAtomContainer, int, " + nameof(BitArray) + ", int)")]
        protected int[] FindPathes(IAtomContainer container, int searchDepth)
        {
            var hashes = new HashSet<int>();

            var cache = new Dictionary<IAtom, List<IBond>>();
            var buffer = new StringBuilder();
            foreach (IAtom startAtom in container.Atoms)
            {
                var p = PathTools.GetLimitedPathsOfLengthUpto(container, startAtom, searchDepth, pathLimit);
                foreach (List<IAtom> path in p)
                {
                    if (hashPseudoAtoms || !HasPseudoAtom(path))
                        hashes.Add(EncodeUniquePath(container, cache, path, buffer));
                }
            }

            int pos = 0;
            int[] result = new int[hashes.Count];
            foreach (var hash in hashes)
                result[pos++] = hash;

            return result;
        }

        protected void EncodePaths(IAtomContainer mol, int depth, BitArray fp, int size)
        {
            var state = new State(mol, fp, size, depth + 1);
            foreach (IAtom atom in mol.Atoms)
            {
                state.numPaths = 0;
                state.Visit(atom);
                TraversePaths(state, atom, null);
                state.Unvisit(atom);
            }
        }

        private static bool IsPseudo(IAtom a)
        {
            return GetElem(a) == 0;
        }

        private static bool HasPseudoAtom(IEnumerable<IAtom> path)
        {
            foreach (var atom in path)
                if (IsPseudo(atom))
                    return true;
            return false;
        }

        private int EncodeUniquePath(IAtomContainer container, Dictionary<IAtom, List<IBond>> cache, List<IAtom> path, StringBuilder buffer)
        {
            if (path.Count == 1)
                return GetAtomSymbol(path[0]).GetHashCode();
            var forward = EncodePath(container, cache, path, buffer);
            path.Reverse();
            var reverse = EncodePath(container, cache, path, buffer);
            path.Reverse();

            int x;
            if (string.CompareOrdinal(reverse, forward) < 0)
                x = forward.GetHashCode();
            else
                x = reverse.GetHashCode();
            return x;
        }

        /// <summary>
        /// Compares atom symbols lexicographical
        /// </summary>
        /// <param name="a">atom a</param>
        /// <param name="b">atom b</param>
        /// <returns>comparison &lt;0 a is less than b, &gt;0 a is more than b</returns>
        private static int Compare(IAtom a, IAtom b)
        {
            var elemA = GetElem(a);
            var elemB = GetElem(b);
            if (elemA == elemB)
                return 0;
            return string.CompareOrdinal(GetAtomSymbol(a), GetAtomSymbol(b));
        }

        /// <summary>
        /// Compares bonds symbols lexicographical
        /// </summary>
        /// <param name="a">bond a</param>
        /// <param name="b">bond b</param>
        /// <returns>comparison &lt;0 a is less than b, &gt;0 a is more than b</returns>
        private int Compare(IBond a, IBond b)
        {
            return string.CompareOrdinal(GetBondSymbol(a), GetBondSymbol(b));
        }

        /// <summary>
        /// Compares a path of atoms with it's self to give the
        /// lexicographically lowest traversal (forwards or backwards).
        /// </summary>
        /// <param name="apath">path of atoms</param>
        /// <param name="bpath">path of bonds</param>
        /// <returns>&lt;0 forward is lower &gt;0 reverse is lower</returns>
        private int Compare(List<IAtom> apath, List<IBond> bpath)
        {
            int i = 0;
            var len = apath.Count;
            var j = len - 1;
            var cmp = Compare(apath[i], apath[j]);
            if (cmp != 0)
                return cmp;
            i++;
            j--;
            while (j != 0)
            {
                cmp = Compare(bpath[i - 1], bpath[j]);
                if (cmp != 0)
                    return cmp;
                cmp = Compare(apath[i], apath[j]);
                if (cmp != 0)
                    return cmp;
                i++;
                j--;
            }
            return 0;
        }

        private int EncodeUniquePath(List<IAtom> apath, List<IBond> bpath, StringBuilder buffer)
        {
            if (bpath.Count == 0)
                return Strings.GetJavaHashCode(GetAtomSymbol(apath[0]));
            int x;
            if (Compare(apath, bpath) >= 0)
            {
                x = HashPath(apath, bpath);
            }
            else
            {
                x = HashRevPath(apath, bpath);
            }
            return x;
        }

        private static int GetElem(IAtom atom)
        {
            return atom.AtomicNumber;
        }

        private static string GetAtomSymbol(IAtom atom)
        {
            // XXX: backwards compatibility
            // This is completely random, I believe the intention is because
            // paths were reversed with string manipulation to de-duplicate
            // (only the lowest lexicographically is stored) however this
            // doesn't work with multiple atom symbols:
            // e.g. Fe-C => C-eF vs C-Fe => eF-C
            // A dirty hack is to replace "common" symbols with single letter
            // equivalents so the reversing is less wrong
            switch (GetElem(atom))
            {
                case 0:  // *
                    return "*";
                case 6:  // C
                    return "C";
                case 7:  // N
                    return "N";
                case 8:  // O
                    return "O";
                case 17: // Cl
                    return "X";
                case 35: // Br
                    return "Z";
                case 14: // Si
                    return "Y";
                case 33: // As
                    return "D";
                case 3: // Li
                    return "L";
                case 34: // Se
                    return "E";
                case 11:  // Na
                    return "G";
                case 20:  // Ca
                    return "J";
                case 13:  // Al
                    return "A";
            }
            return atom.Symbol;
        }

        /// <summary>
        /// Gets the bondSymbol attribute of the Fingerprinter class
        /// </summary>
        /// <param name="bond">Description of the Parameter</param>
        /// <returns>The bondSymbol value</returns>
        protected virtual string GetBondSymbol(IBond bond)
        {
            if (bond.IsAromatic)
                return ":";
            switch (bond.Order)
            {
                case BondOrder.Single:
                    return "-";
                case BondOrder.Double:
                    return "=";
                case BondOrder.Triple:
                    return "#";
                default:
                    return "";
            }
        }

        public void SetPathLimit(int limit)
        {
            this.pathLimit = limit;
        }

        public void SetHashPseudoAtoms(bool value)
        {
            this.hashPseudoAtoms = value;
        }

        public int SearchDepth { get; }

        public override int Length => size;

        public override ICountFingerprint GetCountFingerprint(IAtomContainer container)
        {
            throw new NotSupportedException();
        }
    }
}
