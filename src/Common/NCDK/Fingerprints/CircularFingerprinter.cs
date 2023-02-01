/* Copyright (c) 2014 Collaborative Drug Discovery, Inc. <alex@collaborativedrug.com>
 *
 * Implemented by Alex M. Clark, produced by Collaborative Drug Discovery, Inc.
 * Made available to the CDK community under the terms of the GNU LGPL.
 *
 *    http://collaborativedrug.com
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

using NCDK.Common.Hash;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NCDK.Fingerprints
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1717:Only FlagsAttribute enums should have plural names", Justification = "Ignored")]
    public enum CircularFingerprinterClass
    {
        // identity by literal atom environment
        ECFP0 = 1,
        ECFP2 = 2,
        ECFP4 = 3,
        ECFP6 = 4,
        // identity by functional character of the atom
        FCFP0 = 5,
        FCFP2 = 6,
        FCFP4 = 7,
        FCFP6 = 8,
    }

    public sealed class CircularFingerprint
    {
        public CircularFingerprint(int hashCode, int iteration, IEnumerable<int> atoms)
        {
            this.Hash = hashCode;
            this.Iteration = iteration;
            this.Atoms = atoms.ToReadOnlyList();
        }

        public int Hash { get; }
        public int Iteration { get; }
        public IReadOnlyList<int> Atoms { get; }
    }

    /// <summary>
    /// Circular fingerprints: for generating fingerprints that are functionally equivalent to ECFP-2/4/6 and FCFP-2/4/6
    /// fingerprints, which are partially described by Rogers et al. <token>cdk-cite-Rogers2010</token>.
    /// </summary>
    /// <remarks>
    /// <para>While the literature describes the method in detail, it does not disclose either the hashing technique for converting
    /// lists of integers into 32-bit codes, nor does it describe the scheme used to classify the atom types for creating
    /// the FCFP-class of descriptors. For this reason, the fingerprints that are created are not binary compatible with
    /// the reference implementation. They do, however, achieve effectively equal performance for modelling purposes.</para>
    ///
    /// <para>The resulting fingerprint bits are presented as a list of unique bits, each with a 32-bit hashcode; typically there
    /// are no more than a hundred or so unique bit hashcodes per molecule. These identifiers can be folded into a smaller
    /// array of bits, such that they can be represented as a single long binary number, which is often more convenient.</para>
    ///
    /// <para>The integer hashing is done using the CRC32 algorithm, using the Java CRC32 class, which is the same
    /// formula/parameters as used by PNG files, and described in:</para>
    ///
    /// <see href="http://www.w3.org/TR/PNG/#D-CRCAppendix">http://www.w3.org/TR/PNG/#D-CRCAppendix</see>
    ///
    /// <para>Implicit vs. explicit hydrogens are handled, i.e. it doesn't matter whether the incoming molecule is hydrogen
    /// suppressed or not.</para>
    ///
    /// <para>Implementation note: many of the algorithms involved in the generation of fingerprints (e.g. aromaticity, atom
    /// typing) have been coded up explicitly for use by this class, rather than making use of comparable functionality
    /// elsewhere in the CDK. This is to ensure that the CDK implementation of the algorithm is strictly equal to other
    /// implementations: dependencies on CDK functionality that could be modified or improved in the future would break
    /// binary compatibility with formerly identical implementations on other platforms.</para>
    ///
    /// <para>For the FCFP class of fingerprints, atom typing is done using a scheme similar to that described by
    /// Green et al (1994).</para>
    ///  
    /// <para>The fingerprints and their uses have been described in Clark et al. <token>cdk-cite-Clark2014</token>.</para>
    ///  
    /// <see href="http://www.jcheminf.com/content/6/1/38">http://www.jcheminf.com/content/6/1/38</see>
    /// </remarks>
    // @author         am.clark
    // @cdk.created    2014-01-01
    // @cdk.keyword    fingerprint
    // @cdk.keyword    similarity
    // @cdk.module     standard
    public class CircularFingerprinter : AbstractFingerprinter, IFingerprinter
    {
        // ------------ private members ------------
        
        enum AtomClasses
        {
            ECFP = 1,
            FCFP = 2,
        }

        private IAtomContainer mol;
        private readonly int length;

        private int[] identity;
        private bool[] resolvedChiral;
        private int[][] atomGroup;
        private List<CircularFingerprint> fplist = new List<CircularFingerprint>();

        // summary information about the molecule, for quick access
        private bool[] amask;                               // true for all heavy atoms, i.e. hydrogens and non-elements are excluded
        private int[] hcount;                              // total hydrogen count, including explicit and implicit hydrogens
        private int[][] atomAdj, bondAdj;                    // precalculated adjacencies, including only those qualifying with 'amask'
        private int[] ringBlock;                           // ring block identifier; 0=not in a ring
        private int[][] smallRings;                          // all rings of size 3 through 7
        private int[] bondOrder;                           // numeric bond order for easy reference
        private bool[] atomArom, bondArom;                  // aromaticity precalculated
        private int[][] tetra;                               // tetrahedral rubric, a precursor to chirality

        // stored information for bio-typing; only defined for FCFP-class fingerprints
        private bool[] maskDon, maskAcc, maskPos, maskNeg, maskAro, maskHal; // functional property flags
        private int[] bondSum;                                             // sum of bond orders for each atom (including explicit H's)
        private bool[] hasDouble;                                           // true if an atom has any double bonds
        private bool[] aliphatic;                                           // true for carbon atoms with only sigma bonds
        private bool[] isOxide;                                             // true if the atom has a double bond to oxygen
        private bool[] lonePair;                                            // true if the atom is N,O,S with octet valence and at least one lone pair
        private bool[] tetrazole;                                           // special flag for being in a tetrazole (C1=NN=NN1) ring

        // ------------ options -------------------
        private readonly CircularFingerprinterClass classType;
        private AtomClasses atomClass;

        // ------------ methods ------------

        /// <summary>
        /// Default constructor: uses the ECFP6 type.
        /// </summary>
        public CircularFingerprinter()
            : this(CircularFingerprinterClass.ECFP6)
        {
        }

        /// <summary>
        /// Specific constructor: initializes with descriptor class type, one of ECFP_{p} or FCFP_{p}, where ECFP is
        /// for the extended-connectivity fingerprints, FCFP is for the functional class version, and {p} is the
        /// path diameter, and may be 0, 2, 4 or 6.
        /// </summary>
        /// <param name="classType">one of Classes.ECFP{n} or Classes.FCFP{n}</param>
        public CircularFingerprinter(CircularFingerprinterClass classType)
            : this(classType, 1024)
        { }

        /// <summary>
        /// Specific constructor: initializes with descriptor class type, one of ECFP_{p} or FCFP_{p}, where ECFP is
        /// for the extended-connectivity fingerprints, FCFP is for the functional class version, and {p} is the
        /// path diameter, and may be 0, 2, 4 or 6.
        /// </summary>
        /// <param name="classType">one of Classes.ECFP{n} or Classes.FCFP{n}</param>
        /// <param name="len">size of folded (binary) fingerprint</param>
        public CircularFingerprinter(CircularFingerprinterClass classType, int len)
        {
            this.classType = classType;
            this.length = len;
        }

        /// <summary>
        /// Whether stereochemistry should be re-perceived from 2D/3D
        /// coordinates. By default stereochemistry encoded as <see cref="IStereoElement{TFocus, TCarriers}"/>s
        /// are used.
        /// </summary>
        public bool PerceiveStereo { get; set; } = false;

        protected override IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            string type = null;
            switch (classType)
            {
                case CircularFingerprinterClass.ECFP0: type = "ECFP0"; break;
                case CircularFingerprinterClass.ECFP2: type = "ECFP2"; break;
                case CircularFingerprinterClass.ECFP4: type = "ECFP4"; break;
                case CircularFingerprinterClass.ECFP6: type = "ECFP6"; break;
                case CircularFingerprinterClass.FCFP0: type = "FCFP0"; break;
                case CircularFingerprinterClass.FCFP2: type = "FCFP2"; break;
                case CircularFingerprinterClass.FCFP4: type = "FCFP4"; break;
                case CircularFingerprinterClass.FCFP6: type = "FCFP6"; break;
                default:
                    break;
            }
            yield return new KeyValuePair<string, string>("classType", type);
            yield return new KeyValuePair<string, string>("perceiveStereochemistry", PerceiveStereo.ToString(NumberFormatInfo.InvariantInfo).ToLowerInvariant()); // True/False to true/false
            yield break;
        }

        /// <summary>
        /// Calculates the fingerprints for the given <see cref="IAtomContainer"/>, and stores them for subsequent retrieval.
        /// </summary>
        /// <param name="mol">chemical structure; all nodes should be known legitimate elements</param>
        public void Calculate(IAtomContainer mol)
        {
            this.mol = mol;
            fplist.Clear();
            atomClass = classType <= CircularFingerprinterClass.ECFP6 ? AtomClasses.ECFP : AtomClasses.FCFP;

            ExcavateMolecule();
            if (atomClass == AtomClasses.FCFP)
                CalculateBioTypes();

            int na = mol.Atoms.Count;
            identity = new int[na];
            resolvedChiral = new bool[na];
            atomGroup = new int[na][];

            for (int n = 0; n < na; n++)
                if (amask[n])
                {
                    if (atomClass == AtomClasses.ECFP)
                        identity[n] = InitialIdentityECFP(n);
                    else
                        // atomClass==ATOMClasses.FCFP
                        identity[n] = InitialIdentityFCFP(n);
                    atomGroup[n] = new int[] { n };
                    fplist.Add(new CircularFingerprint(identity[n], 0, atomGroup[n]));
                }

            int niter = 
                classType == CircularFingerprinterClass.ECFP2 || classType == CircularFingerprinterClass.FCFP2 ? 1 
              : classType == CircularFingerprinterClass.ECFP4 || classType == CircularFingerprinterClass.FCFP4 ? 2
              : classType == CircularFingerprinterClass.ECFP6 || classType == CircularFingerprinterClass.FCFP6 ? 3 
              : 0;

            // iterate outward
            for (int iter = 1; iter <= niter; iter++)
            {
                var newident = new int[na];
                for (int n = 0; n < na; n++)
                    if (amask[n])
                        newident[n] = CircularIterate(iter, n);
                identity = newident;

                for (int n = 0; n < na; n++)
                {
                    if (amask[n])
                    {
                        atomGroup[n] = GrowAtoms(atomGroup[n]);
                        ConsiderNewFP(new CircularFingerprint(identity[n], iter, atomGroup[n]));
                    }
                }
            }
        }

        /// <summary>
        /// Returns the number of fingerprints generated.
        /// </summary>
        /// <returns>total number of unique fingerprint hashes generated</returns>
        public int FPCount => fplist.Count;

        /// <summary>
        /// Returns the requested fingerprint.
        /// </summary>
        /// <param name="N">index of fingerprint (0-based)</param>
        /// <returns>instance of a fingerprint hash</returns>
        public CircularFingerprint GetFP(int N)
        {
            return fplist[N];
        }

        /// <summary>
        /// Calculates the circular fingerprint for the given <see cref="IAtomContainer"/>, and <b>folds</b> the result into a single bitset
        /// (see GetSize()).
        /// </summary>
        /// <param name="mol">IAtomContainer for which the fingerprint should be calculated.</param>
        /// <returns>the fingerprint</returns>
        public override IBitFingerprint GetBitFingerprint(IAtomContainer mol)
        {
            Calculate(mol);
            var bits = new BitArray(length);
            for (int n = 0; n < fplist.Count; n++)
            {
                int i = fplist[n].Hash;
                long b = i >= 0 ? i : ((i & 0x7FFFFFFF) | (1L << 31));
                bits.Set((int)(b % length), true);
            }
            return new BitSetFingerprint(bits);
        }

        private class CountFingerprint : ICountFingerprint
        {
            IReadOnlyDictionary<int, int> map;
            private readonly int[] hash;
            private readonly int[] count;

            public CountFingerprint(IReadOnlyDictionary<int, int> map, int[] hash, int[] count)
            {
                this.map = map;
                this.hash = hash;
                this.count = count;
            }

            public long Length => 4294967296L;
            public int GetNumberOfPopulatedBins() => map.Count;
            public int GetCount(int index) => count[index];
            public int GetHash(int index) => hash[index];
            public void Merge(ICountFingerprint fp) { }
            public void SetBehaveAsBitFingerprint(bool behaveAsBitFingerprint) { }
            public bool HasHash(int hash) => map.ContainsKey(hash);
            public int GetCountForHash(int hash) => map.ContainsKey(hash) ? map[hash] : 0;
        }

        /// <summary>
        /// Calculates the circular fingerprint for the given <see cref="IAtomContainer"/>, and returns a datastructure that enumerates all
        /// of the fingerprints, and their counts (i.e. does <b>not</b> fold them into a bitmask).
        /// </summary>
        /// <param name="mol">IAtomContainer for which the fingerprint should be calculated.</param>
        /// <returns>the count fingerprint</returns>
        public override ICountFingerprint GetCountFingerprint(IAtomContainer mol)
        {
            Calculate(mol);

            // extract a convenient {hash:count} datastructure
            var map = new SortedDictionary<int, int>();
            foreach (var fp in fplist)
            {
                if (map.ContainsKey(fp.Hash))
                    map.Add(fp.Hash, map[fp.Hash] + 1);
                else
                    map.Add(fp.Hash, 1);
            }
            int sz = map.Count;
            var hash = new int[sz];
            var count = new int[sz];
            int n = 0;
            foreach (var h in map.Keys)
            {
                hash[n] = h;
                count[n++] = map[h];
            }

            // implement a custom instance that provides a window directly into the summary content
            return new CountFingerprint(map, hash, count);
        }

        /// <summary>
        /// Invalid: it is not appropriate to convert the integer hash codes into strings.
        /// </summary>
        public override IReadOnlyDictionary<string, int> GetRawFingerprint(IAtomContainer mol)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Returns the extent of the folded fingerprints.
        /// </summary>
        /// <returns>the size of the fingerprint</returns>
        public override int Length => length;

        // ------------ private methods ------------

        private static readonly int[] ELEMENT_BONDING = new int[] 
            {
                0, 1, 0, 1, 2, 3, 4, 3, 2, 1, 0, 1, 2, 3, 4, 3, 2, 1, 0, 1, 2, 3, 4, 5, 6, 7, 8,
                9, 10, 11, 12, 3, 4, 3, 2, 1, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 3, 4, 3, 2, 1, 0, 1, 2, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 3, 4, 5, 6, 7, 8, 1, 1, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12,
            };

        /// <summary>
        /// calculates an integer number that stores the bit-packed identity of the given atom
        /// </summary>
        private int InitialIdentityECFP(int aidx)
        {
            // <summary>
            // Atom properties from the source reference: (1) number of heavy atom
            // neighbours (2) atom degree: valence minus # hydrogens (3) atomic
            // number (4) atomic mass (5) atom charge (6) number of hydrogen
            // neighbours (7) whether the atom is in a ring
            // </summary>
            var atom = mol.Atoms[aidx];

            var nheavy = atomAdj[aidx].Length;
            var nhydr = hcount[aidx];
            var atno = atom.AtomicNumber;

            var degree = (atno > 0 && atno < ELEMENT_BONDING.Length ? ELEMENT_BONDING[atno] : 0) - nhydr;
            var chg = atom.FormalCharge.Value;
            var inring = ringBlock[aidx] > 0 ? 1 : 0;
            return (int)Crc32.Compute(new byte[]
                {
                    (byte)((nheavy << 4) | degree),
                    (byte)atno,
                    (byte)(chg + 0x80),
                    (byte)((nhydr << 4) | inring),
                });
        }

        private int InitialIdentityFCFP(int aidx)
        {
            return (maskDon[aidx] ? 0x01 : 0) 
                 | (maskAcc[aidx] ? 0x02 : 0) 
                 | (maskPos[aidx] ? 0x04 : 0)
                 | (maskNeg[aidx] ? 0x08 : 0) 
                 | (atomArom[aidx] ? 0x10 : 0) 
                 | // strictly bond aromaticity more accurate rendition
                   (maskHal[aidx] ? 0x20 : 0);
        }

        /// takes the current identity values
        private int CircularIterate(int iter, int atom)
        {
            var adj = atomAdj[atom];
            var adjb = bondAdj[atom];

            // build out a sequence, formulated as
            //     {iteration,original#, adj0-bondorder,adj0-identity, ..., [chiral?]}
            var seq = new int[2 + 2 * adj.Length];
            seq[0] = iter;
            seq[1] = identity[atom];
            for (int n = 0; n < adj.Length; n++)
            {
                seq[2 * n + 2] = bondArom[adjb[n]] ? 0xF : bondOrder[adjb[n]];
                seq[2 * n + 3] = identity[adj[n]];
            }

            // now sort the adjacencies by bond order first, then identity second
            int p = 0;
            while (p < adj.Length - 1)
            {
                var i = 2 + 2 * p;
                if (seq[i] > seq[i + 2] || (seq[i] == seq[i + 2] && seq[i + 1] > seq[i + 3]))
                {
                    var sw = seq[i];
                    seq[i] = seq[i + 2];
                    seq[i + 2] = sw;
                    sw = seq[i + 1];
                    seq[i + 1] = seq[i + 3];
                    seq[i + 3] = sw;
                    if (p > 0)
                        p--;
                }
                else
                    p++;
            }

            // roll it up into a hash code
            using (var array = new MemoryStream())
            {
                for (int n = 0; n < seq.Length; n += 2)
                {
                    array.WriteByte((byte)seq[n]);
                    var v = (uint)seq[n + 1];
                    array.WriteByte((byte)(v >> 24));
                    array.WriteByte((byte)((v >> 16) & 0xFF));
                    array.WriteByte((byte)((v >> 8) & 0xFF));
                    array.WriteByte((byte)(v & 0xFF));
                }

                // chirality flag: one chance to resolve it
                if (!resolvedChiral[atom] && tetra[atom] != null)
                {
                    var ru = tetra[atom];
                    var par = new int []
                        {
                            ru[0] < 0 ? 0 : identity[ru[0]],
                            ru[1] < 0 ? 0 : identity[ru[1]],
                            ru[2] < 0 ? 0 : identity[ru[2]],
                            ru[3] < 0 ? 0 : identity[ru[3]],
                        };
                    if (par[0] != par[1]
                     && par[0] != par[2] 
                     && par[0] != par[3] 
                     && par[1] != par[2]
                     && par[1] != par[3]
                     && par[2] != par[3])
                    {
                        int rp = 0;
                        if (par[0] < par[1]) rp++;
                        if (par[0] < par[2]) rp++;
                        if (par[0] < par[3]) rp++;
                        if (par[1] < par[2]) rp++;
                        if (par[1] < par[3]) rp++;
                        if (par[2] < par[3]) rp++;

                        // add 1 or 2 to the end of the list, depending on the parity
                        array.WriteByte((byte)((rp & 1) + 1));
                        resolvedChiral[atom] = true;
                    }
                }

                return (int)Crc32.Compute(array.ToArray());
            }
        }

        /// <summary>
        /// takes a set of atom indices and adds all atoms that are adjacent to at least one of them; the resulting list of
        /// atom indices is sorted
        /// </summary>
        private int[] GrowAtoms(int[] atoms)
        {
            var na = mol.Atoms.Count;
            var mask = new bool[na];
            for (int n = 0; n < atoms.Length; n++)
            {
                mask[atoms[n]] = true;
                var adj = atomAdj[atoms[n]];
                for (int i = 0; i < adj.Length; i++)
                    mask[adj[i]] = true;
            }
            int sz = 0;
            for (int n = 0; n < na; n++)
                if (mask[n])
                    sz++;
            var newList = new int[sz];
            for (int n = na - 1; n >= 0; n--)
                if (mask[n])
                    newList[--sz] = n;
            return newList;
        }

        /// <summary>
        /// consider adding a new fingerprint: if it's a duplicate with regard to the atom list, either replace the match or
        /// discard it
        /// </summary>
        private void ConsiderNewFP(CircularFingerprint newFP)
        {
            int hit = -1;
            CircularFingerprint fp = null;
            for (int n = 0; n < fplist.Count; n++)
            {
                fp = fplist[n];
                var equal = fp.Atoms.Count == newFP.Atoms.Count;
                for (int i = fp.Atoms.Count - 1; equal && i >= 0; i--)
                    if (fp.Atoms[i] != newFP.Atoms[i])
                        equal = false;
                if (equal)
                {
                    hit = n;
                    break;
                }
            }
            if (hit < 0)
            {
                fplist.Add(newFP);
                return;
            }

            // if the preexisting fingerprint is from an earlier iteration, or has a lower hashcode, discard
            if (fp.Iteration < newFP.Iteration || fp.Hash < newFP.Hash)
                return;
            fplist[hit] = newFP;
        }

        // ------------ molecule analysis: cached cheminformatics ------------

        internal static readonly IReadOnlyDictionary<int?, int> HYVALENCES = new Dictionary<int?, int>()
            {
                [AtomicNumbers.C] = 4,
                [AtomicNumbers.N] = 3,
                [AtomicNumbers.O] = 2,
                [AtomicNumbers.S] = 2,
                [AtomicNumbers.P] = 3,
            };

        /// <summary>
        /// summarize preliminary information about the molecular structure, to make sure the rest all goes quickly
        /// </summary>
        private void ExcavateMolecule()
        {
            var na = mol.Atoms.Count;
            var nb = mol.Bonds.Count;

            // create the mask of heavy atoms (amask) and the adjacency graphs, index-based, that are used to traverse
            // the heavy part of the graph
            amask = new bool[na];
            for (int n = 0; n < na; n++)
                amask[n] = mol.Atoms[n].AtomicNumber > 1; // true for heavy elements
            atomAdj = new int[na][];
            bondAdj = new int[na][];
            bondOrder = new int[nb];
            hcount = new int[na];
            for (int n = 0; n < mol.Bonds.Count; n++)
            {
                var bond = mol.Bonds[n];
                if (bond.Atoms.Count != 2)
                    continue;
                int a1 = mol.Atoms.IndexOf(bond.Begin), a2 = mol.Atoms.IndexOf(bond.End);
                if (amask[a1] && amask[a2])
                {
                    atomAdj[a1] = AppendInteger(atomAdj[a1], a2);
                    bondAdj[a1] = AppendInteger(bondAdj[a1], n);
                    atomAdj[a2] = AppendInteger(atomAdj[a2], a1);
                    bondAdj[a2] = AppendInteger(bondAdj[a2], n);
                    switch (bond.Order)
                    {
                        case BondOrder.Single:
                            bondOrder[n] = 1;
                            break;
                        case BondOrder.Double:
                            bondOrder[n] = 2;
                            break;
                        case BondOrder.Triple:
                            bondOrder[n] = 3;
                            break;
                        case BondOrder.Quadruple:
                            bondOrder[n] = 4;
                            break;
                        default:
                            // (look for zero-order bonds later on)
                            break;
                    }
                }
                else
                {
                    if (!amask[a1]) hcount[a2]++;
                    if (!amask[a2]) hcount[a1]++;
                }
            }
            for (int n = 0; n < na; n++)
            {
                if (amask[n] && atomAdj[n] == null)
                {
                    atomAdj[n] = Array.Empty<int>();
                    bondAdj[n] = atomAdj[n];
                }
            }

            // calculate implicit hydrogens, using a very conservative formula
            for (int n = 0; n < na; n++)
            {
                var atom = mol.Atoms[n];
                var an = atom.AtomicNumber;
                if (!HYVALENCES.TryGetValue(an, out int hy))
                    continue;
                int ch = atom.FormalCharge.Value;
                if (an.Equals(AtomicNumbers.C))
                    ch = -Math.Abs(ch);
                int unpaired = 0; // (not current available, maybe introduce later)
                hy += ch - unpaired;
                // (needs to include actual H's) for (int i=0;i<bondAdj[n].Length;i++) hy-=bondOrder[bondAdj[n][i]];
                foreach (var bond in mol.GetConnectedBonds(atom))
                {
                    switch (bond.Order)
                    {
                        case BondOrder.Single:
                            hy -= 1;
                            break;
                        case BondOrder.Double:
                            hy -= 2;
                            break;
                        case BondOrder.Triple:
                            hy -= 3;
                            break;
                        case BondOrder.Quadruple:
                            hy -= 4;
                            break;
                        default:
                            // (look for zero-bonds later on)
                            break;
                    }
                }
                hcount[n] += Math.Max(0, hy);
            }

            MarkRingBlocks();

            var rings = new List<int[]>();
            for (int rsz = 3; rsz <= 7; rsz++)
            {
                var path = new int[rsz];
                for (int n = 0; n < na; n++)
                    if (ringBlock[n] > 0)
                    {
                        path[0] = n;
                        RecursiveRingFind(path, 1, rsz, ringBlock[n], rings);
                    }
            }
            smallRings = rings.ToArray();

            DetectStrictAromaticity();

            tetra = new int[na][];
            if (PerceiveStereo)
            {
                for (int n = 0; n < na; n++)
                    tetra[n] = RubricTetrahedral(n);
            }
            else
            {
                RubricTetrahedralsCdk();
            }
        }

        /// assign a ring block ID to each atom (0=not in ring)
        private void MarkRingBlocks()
        {
            int na = mol.Atoms.Count;
            ringBlock = new int[na];

            var visited = new bool[na];
            for (int n = 0; n < na; n++)
                visited[n] = !amask[n]; // skip hydrogens

            var path = new int[na + 1];
            int plen = 0;
            while (true)
            {
                int last, current;

                if (plen == 0) // find an element of a new component to visit
                {
                    last = -1;
                    for (current = 0; current < na && visited[current]; current++)
                    {
                    }
                    if (current >= na)
                        break;
                }
                else
                {
                    last = path[plen - 1];
                    current = -1;
                    for (int n = 0; n < atomAdj[last].Length; n++)
                        if (!visited[atomAdj[last][n]])
                        {
                            current = atomAdj[last][n];
                            break;
                        }
                }

                if (current >= 0 && plen >= 2) // path is at least 2 items long, so look for any not-previous visited neighbours
                {
                    int back = path[plen - 1];
                    for (int n = 0; n < atomAdj[current].Length; n++)
                    {
                        int join = atomAdj[current][n];
                        if (join != back && visited[join])
                        {
                            path[plen] = current;
                            for (int i = plen; i == plen || path[i + 1] != join; i--)
                            {
                                int id = ringBlock[path[i]];
                                if (id == 0)
                                    ringBlock[path[i]] = last;
                                else if (id != last)
                                {
                                    for (int j = 0; j < na; j++)
                                        if (ringBlock[j] == id) ringBlock[j] = last;
                                }
                            }
                        }
                    }
                }
                if (current >= 0) // can mark the new one as visited
                {
                    visited[current] = true;
                    path[plen++] = current;
                }
                else // otherwise, found nothing and must rewind the path
                {
                    plen--;
                }
            }

            // the ring ID's are not necessarily consecutive, so reassign them to 0=none, 1..NBlocks
            int nextID = 0;
            for (int i = 0; i < na; i++)
                if (ringBlock[i] > 0)
                {
                    nextID--;
                    for (int j = na - 1; j >= i; j--)
                        if (ringBlock[j] == ringBlock[i]) ringBlock[j] = nextID;
                }
            for (int i = 0; i < na; i++)
                ringBlock[i] = -ringBlock[i];
        }

        /// <summary>
        /// hunt for ring recursively: start with a partially defined path, and go exploring
        /// </summary>
        private void RecursiveRingFind(int[] path, int psize, int capacity, int rblk, List<int[]> rings)
        {
            // not enough atoms yet, so look for new possibilities
            if (psize < capacity)
            {
                int last_ = path[psize - 1];
                for (int n = 0; n < atomAdj[last_].Length; n++)
                {
                    int adj = atomAdj[last_][n];
                    if (ringBlock[adj] != rblk)
                        continue;
                    bool fnd_ = false;
                    for (int i = 0; i < psize; i++)
                        if (path[i] == adj)
                        {
                            fnd_ = true;
                            break;
                        }
                    if (!fnd_)
                    {
                        var newPath = new int[capacity];
                        for (int i = 0; i < psize; i++)
                            newPath[i] = path[i];
                        newPath[psize] = adj;
                        RecursiveRingFind(newPath, psize + 1, capacity, rblk, rings);
                    }
                }
                return;
            }

            // path is full, so make sure it eats its tail
            int last = path[psize - 1];
            bool fnd = false;
            for (int n = 0; n < atomAdj[last].Length; n++)
                if (atomAdj[last][n] == path[0])
                {
                    fnd = true;
                    break;
                }
            if (!fnd)
                return;

            // make sure every element in the path has exactly 2 neighbours within the path; otherwise it is spanning a bridge, which
            // is an undesirable ring definition
            for (int n = 0; n < path.Length; n++)
            {
                int count = 0, p = path[n];
                for (int i = 0; i < atomAdj[p].Length; i++)
                    for (int j = 0; j < path.Length; j++)
                        if (atomAdj[p][i] == path[j])
                        {
                            count++;
                            break;
                        }
                if (count != 2)
                    return; // invalid
            }

            // reorder the array (there are 2N possible ordered permutations) then look for duplicates
            int first = 0;
            for (int n = 1; n < psize; n++)
                if (path[n] < path[first]) first = n;
            int fm = (first - 1 + psize) % psize, fp = (first + 1) % psize;
            bool flip = path[fm] < path[fp];
            if (first != 0 || flip)
            {
                int[] newPath = new int[psize];
                for (int n = 0; n < psize; n++)
                    newPath[n] = path[(first + (flip ? psize - n : n)) % psize];
                path = newPath;
            }

            for (int n = 0; n < rings.Count(); n++)
            {
                int[] look = rings[n];
                bool same = true;
                for (int i = 0; i < psize; i++)
                    if (look[i] != path[i])
                    {
                        same = false;
                        break;
                    }
                if (same) return;
            }

            rings.Add(path);
        }

        /// <summary>
        /// aromaticity detection: uses a very narrowly defined algorithm, which detects 6-membered rings with alternating double bonds;
        /// rings that are chained together (e.g. anthracene) will also be detected by the extended followup; note that this will NOT mark
        /// rings such as thiophene, imidazolium, porphyrins, etc.: these systems will be left in their original single/double bond form
        /// </summary>
        private void DetectStrictAromaticity()
        {
            var na = mol.Atoms.Count;
            var nb = mol.Bonds.Count;
            atomArom = new bool[na];
            bondArom = new bool[nb];

            if (smallRings.Length == 0)
                return;

            var piAtom = new bool[na];
            for (int n = 0; n < nb; n++)
                if (bondOrder[n] == 2)
                {
                    var bond = mol.Bonds[n];
                    piAtom[mol.Atoms.IndexOf(bond.Begin)] = true;
                    piAtom[mol.Atoms.IndexOf(bond.End)] = true;
                }

            var maybe = new List<int[]>(); // rings which may yet be aromatic
            foreach (var r in smallRings)
                if (r.Length == 6)
                {
                    bool consider = true;
                    for (int n = 0; n < 6; n++)
                    {
                        var a = r[n];
                        if (!piAtom[a])
                        {
                            consider = false;
                            break;
                        }
                        var b = FindBond(a, r[n == 5 ? 0 : n + 1]);
                        if (bondOrder[b] != 1 && bondOrder[b] != 2)
                        {
                            consider = false;
                            break;
                        }
                    }
                    if (consider) maybe.Add(r);
                }

            // keep classifying rings as aromatic until no change; this needs to be done iteratively, for the benefit of highly
            // embedded ring systems, that can't be classified as aromatic until it is known that their neighbours obviously are
            while (true)
            {
                bool anyChange = false;

                for (int n = maybe.Count() - 1; n >= 0; n--)
                {
                    var r = maybe[n];
                    bool phase1 = true, phase2 = true; // has to go 121212 or 212121; already arom=either is OK
                    for (int i = 0; i < 6; i++)
                    {
                        int b = FindBond(r[i], r[i == 5 ? 0 : i + 1]);
                        if (bondArom[b])
                            continue; // valid for either phase
                        phase1 = phase1 && bondOrder[b] == (2 - (i & 1));
                        phase2 = phase2 && bondOrder[b] == (1 + (i & 1));
                    }
                    if (!phase1 && !phase2)
                        continue;

                    // the ring is deemed aromatic: mark the flags and remove from the maybe list
                    for (int i = 0; i < r.Length; i++)
                    {
                        atomArom[r[i]] = true;
                        bondArom[FindBond(r[i], r[i == 5 ? 0 : i + 1])] = true;
                    }
                    maybe.RemoveAt(n);
                    anyChange = true;
                }

                if (!anyChange)
                    break;
            }
        }

        // tetrahedral 'rubric': for any sp3 atom that has stereo defined
        // in the CDK's object model.
        private void RubricTetrahedralsCdk()
        {
            foreach (var se in mol.StereoElements)
            {
                if (se.Class == StereoClass.Tetrahedral)
                {
                    var th = (IStereoElement<IAtom, IAtom>)se;
                    var focus = th.Focus;
                    var carriers = th.Carriers;
                    var adj = new int[4];

                    for (int i = 0; i < 4; i++)
                    {
                        if (focus.Equals(carriers[i]))
                            adj[i] = -1; // impl H
                        else
                            adj[i] = mol.Atoms.IndexOf(carriers[i]);
                    }
                    switch (th.Configure)
                    {
                        case StereoConfigurations.Left:
                            int i = adj[2];
                            adj[2] = adj[3];
                            adj[3] = i;
                            tetra[mol.Atoms.IndexOf(focus)] = adj;
                            break;
                        case StereoConfigurations.Right:
                            tetra[mol.Atoms.IndexOf(focus)] = adj;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private static readonly int[] ELEMENT_BLOCKS = {0, 1, 2, 1, 1, 2, 2, 2, 2, 2, 2, 1, 1, 2, 2, 2, 2, 2, 2, 1, 1, 3, 3, 3, 3, 3, 3,
                    3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 1, 1, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 1, 1, 4, 4, 4, 4,
                    4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 1, 1, 4, 4, 4, 4, 4, 4,
                    4, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3};

        /// tetrahedral 'rubric': for any sp3 atom that has enough neighbours and appropriate wedge bond/3D geometry information,
        /// build up a list of neighbours in a certain permutation order; the resulting array of size 4 can have a total of
        /// 24 permutations; there are two groups of 12 that can be mapped onto each other by tetrahedral rotations, hence this
        /// is a partioning technique for chirality; it can be thought of as all but the last step of determination of chiral
        /// parity, except that the raw information is required for the circular fingerprint chirality resolution; note that this
        /// does not consider the possibility of lone-pair chirality (e.g. sp3 phosphorus)
        private int[] RubricTetrahedral(int aidx)
        {
            if (hcount[aidx] > 1)
                return null;

            // make sure the atom has an acceptable environment
            var atom = mol.Atoms[aidx];
            var atno = atom.AtomicNumber;
            if (atno <= 1 || atno >= ELEMENT_BLOCKS.Length)
                return null;
            if (ELEMENT_BLOCKS[atno] != 2 /* p-block */)
                return null;

            var adjc = atomAdj[aidx].Length;
            var hc = hcount[aidx];
            if (!(adjc == 3 && hc == 1) && !(adjc == 4 && hc == 0))
                return null;

            // must have 3D coordinates or a wedge bond to qualify
            bool wedgeOr3D = false;
            var a3d = atom.Point3D;
            for (int n = 0; n < adjc; n++)
            {
                var stereo = mol.Bonds[bondAdj[aidx][n]].Stereo;
                if (stereo == BondStereo.Up || stereo == BondStereo.Down)
                {
                    wedgeOr3D = true;
                    break;
                }
                if (stereo == BondStereo.UpOrDown)
                    return null; // squiggly line: definitely not
                var o3d = atom.Point3D;
                if (a3d != null && o3d != null && a3d.Value.Z != o3d.Value.Z)
                {
                    wedgeOr3D = true;
                    break;
                }
            }
            if (!wedgeOr3D)
                return null;

            // fill in existing positions, including "fake" Z coordinate if wedges are being used
            var a2d = atom.Point2D;

            // for safety in case the bond type (bond stereo) is set but no coords are
            if (a2d == null && a3d == null)
                return null;

            var x0 = a3d != null ? a3d.Value.X : a2d.Value.X;
            var y0 = a3d != null ? a3d.Value.Y : a2d.Value.Y;
            var z0 = a3d != null ? a3d.Value.Z : 0;
            var xp = new double[] { 0, 0, 0, 0 };
            var yp = new double[] { 0, 0, 0, 0 };
            var zp = new double[] { 0, 0, 0, 0 };
            int numShort = 0;
            for (int n = 0; n < adjc; n++)
            {
                var other = mol.Atoms[atomAdj[aidx][n]];
                var bond = mol.Bonds[bondAdj[aidx][n]];
                var o3d = other.Point3D;
                var o2d = other.Point2D;
                if (o3d != null)
                {
                    xp[n] = o3d.Value.X - x0;
                    yp[n] = o3d.Value.Y - y0;
                    zp[n] = o3d.Value.Z - z0;
                }
                else if (o2d != null)
                {
                    var stereo = bond.Stereo;
                    xp[n] = o2d.Value.X - x0;
                    yp[n] = o2d.Value.Y - y0;
                    zp[n] = other == bond.Begin ? 0 : stereo == BondStereo.Up ? 1 : stereo == BondStereo.Down ? -1 : 0;
                }
                else
                {
                    return null; // no 2D coordinates on some atom
                }

                var dx = xp[n] - x0;
                var dy = yp[n] - y0;
                var dz = zp[n] - z0;
                var dsq = dx * dx + dy * dy + dz * dz;
                if (dsq < 0.01f * 0.01f)
                {
                    numShort++;
                    if (numShort > 1)
                        return null; // second one's the dealbreaker
                }
            }

            // build an implicit H if necessary
            var adj = atomAdj[aidx];
            if (adjc == 3)
            {
                adj = AppendInteger(adj, -1);
                xp[3] = 0;
                yp[3] = 0;
                zp[3] = 0;
            }

            // make the call on permutational parity
            double one = 0;
            double two = 0;
            for (int i = 1; i <= 6; i++)
            {
                int a = 0, b = 0;
                if (i == 1)
                {
                    a = 1;
                    b = 2;
                }
                else if (i == 2)
                {
                    a = 2;
                    b = 3;
                }
                else if (i == 3)
                {
                    a = 3;
                    b = 1;
                }
                else if (i == 4)
                {
                    a = 2;
                    b = 1;
                }
                else if (i == 5)
                {
                    a = 3;
                    b = 2;
                }
                else if (i == 6)
                {
                    a = 1;
                    b = 3;
                }
                var xx = yp[a] * zp[b] - yp[b] * zp[a] - xp[0];
                var yy = zp[a] * xp[b] - zp[b] * xp[a] - yp[0];
                var zz = xp[a] * yp[b] - xp[b] * yp[a] - zp[0];
                if (i <= 3)
                    one += xx * xx + yy * yy + zz * zz;
                else
                    two += xx * xx + yy * yy + zz * zz;
            }

            if (two > one)
            {
                var i = adj[2];
                adj[2] = adj[3];
                adj[3] = i;
            }
            return adj;
        }

        /// biotypes: when generating FCFP-type descriptors, atoms are initially labelled according to their functional
        /// capabilities, that being defined by centers of biological interactions, such as hydrogen bonding and electrostatics
        private void CalculateBioTypes()
        {
            var na = mol.Atoms.Count;
            var nb = mol.Bonds.Count;

            maskDon = new bool[na];
            maskAcc = new bool[na];
            maskPos = new bool[na];
            maskNeg = new bool[na];
            maskAro = new bool[na];
            maskHal = new bool[na];

            aliphatic = new bool[na];
            bondSum = new int[na];
            for (int n = 0; n < na; n++)
                if (amask[n])
                {
                    aliphatic[n] = mol.Atoms[n].AtomicNumber.Equals(AtomicNumbers.C);
                    bondSum[n] = hcount[n];
                }

            hasDouble = new bool[na];
            isOxide = new bool[na];
            for (int n = 0; n < nb; n++)
            {
                var bond = mol.Bonds[n];
                if (bond.Atoms.Count != 2)
                    continue;
                var a1 = mol.Atoms.IndexOf(bond.Begin);
                var a2 = mol.Atoms.IndexOf(bond.End);
                var o = bondOrder[n];
                if (!amask[a1] || !amask[a2])
                    continue;
                bondSum[a1] += o;
                bondSum[a2] += o;
                if (o == 2)
                {
                    hasDouble[a1] = true;
                    hasDouble[a2] = true;
                    if (mol.Atoms[a1].AtomicNumber.Equals(AtomicNumbers.O))
                        isOxide[a2] = true;
                    if (mol.Atoms[a2].AtomicNumber.Equals(AtomicNumbers.O))
                        isOxide[a1] = true;
                }
                if (o != 1)
                {
                    aliphatic[a1] = false;
                    aliphatic[a2] = false;
                }
            }

            lonePair = new bool[na];
            for (int n = 0; n < na; n++)
            {
                var atom = mol.Atoms[n];
                var el = atom.Symbol;
                int valence;
                switch (atom.AtomicNumber)
                {
                    case AtomicNumbers.N:
                        valence = 3;
                        break;
                    case AtomicNumbers.O: 
                    case AtomicNumbers.S:
                        valence = 2;
                        break;
                    default:
                        valence = 0;
                        break;
                }
                if (valence > 0 && bondSum[n] + atom.FormalCharge <= valence)
                    lonePair[n] = true;
            }

            // preprocess small rings
            tetrazole = new bool[na];
            foreach (var r in smallRings)
                if (r.Length >= 5 && r.Length <= 7)
                {
                    ConsiderBioTypeAromaticity(r);
                    if (r.Length == 5)
                        ConsiderBioTypeTetrazole(r);
                }

            // calculate each remaining property
            for (int n = 0; n < na; n++)
                if (amask[n])
                {
                    maskDon[n] = DetermineDonor(n);
                    maskAcc[n] = DetermineAcceptor(n);
                    maskPos[n] = DeterminePositive(n);
                    maskNeg[n] = DetermineNegative(n);
                    maskHal[n] = DetermineHalide(n);
                }
        }

        /// <summary>
        /// if the given ring is aromatic, mark the atoms accordingly: note that this "biotype" definition of aromaticity is
        /// different to the one used in the rest of this class: any ring of size 5 to 7 that has a lone pair or pi bond on every
        /// atom is labelled as aromatic, because the concept required is physical behaviour, i.e. ring current and effect on
        /// neighbouring functional groups, rather than disambiguating conjugational equivalence
        /// </summary>
        private void ConsiderBioTypeAromaticity(int[] ring)
        {
            var rsz = ring.Length;
            int countDouble = 0;
            for (int n = 0; n < rsz; n++)
            {
                int a = ring[n];
                if (hasDouble[a])
                {
                    countDouble++;
                    continue;
                }
                if (!lonePair[a])
                    return;
            }
            if (countDouble < rsz - 2)
                return;
            for (int n = 0; n < rsz; n++)
                maskAro[ring[n]] = true;
        }

        /// if the given ring is a tetrazole, mark the aroms accordingly; must be ring size length 5; it's possible to fool the
        /// tetrazole test with a non-sane/invalid molecule
        private void ConsiderBioTypeTetrazole(int[] ring)
        {
            int countC = 0, countN = 0, ndbl = 0;
            for (int n = 0; n < 5; n++)
            {
                var atom = mol.Atoms[ring[n]];
                if (atom.FormalCharge != 0)
                    return;
                string el = atom.Symbol;
                switch (atom.AtomicNumber)
                {
                    case AtomicNumbers.C:
                        countC++;
                        break;
                    case AtomicNumbers.N:
                        countN++;
                        break;
                }
                if (bondOrder[FindBond(ring[n], ring[n == 4 ? 0 : n + 1])] == 2)
                    ndbl++;
            }
            if (countC != 1 || countN != 4 || ndbl != 2)
                return;
            for (int n = 0; n < 5; n++)
                if (string.Equals(mol.Atoms[ring[n]].Symbol, "N", StringComparison.Ordinal))
                    tetrazole[ring[n]] = true;
        }

        /// hydrogen bond donor
        private bool DetermineDonor(int aidx)
        {
            // must have a hydrogen atom, either implicit or explicit
            if (hcount[aidx] == 0)
                return false;

            var atom = mol.Atoms[aidx];
            var el = atom.Symbol;
            switch (atom.AtomicNumber)
            {
                case AtomicNumbers.N:
                case AtomicNumbers.O:
                    // tetrazoles do not donate
                    if (tetrazole[aidx])
                        return false;

                    // see if any of the neighbours is an oxide of some sort; this is grounds for disqualification, with the exception
                    // of amides, which are consider nonacidic
                    for (int n = 0; n < atomAdj[aidx].Length; n++)
                        if (isOxide[atomAdj[aidx][n]])
                        {
                            if (!mol.Atoms[atomAdj[aidx][n]].AtomicNumber.Equals(AtomicNumbers.C) || !el.Equals("N", StringComparison.Ordinal))
                                return false;
                        }
                    return true;
                case AtomicNumbers.S:
                    // any kind of adjacent double bond disqualifies -SH
                    for (int n = 0; n < atomAdj[aidx].Length; n++)
                        if (hasDouble[atomAdj[aidx][n]])
                            return false;
                    return true;
                case AtomicNumbers.C:
                    // terminal alkynes qualify
                    for (int n = 0; n < bondAdj[aidx].Length; n++)
                        if (BondOrderBioType(bondAdj[aidx][n]) == 3)
                            return true;
                    return false;
            }

            return false;
        }

        /// hydrogen bond acceptor
        private bool DetermineAcceptor(int aidx)
        {
            var atom = mol.Atoms[aidx];

            // must have an N,O,S lonepair and nonpositive charge for starters
            if (!lonePair[aidx] || mol.Atoms[aidx].FormalCharge > 0)
                return false;

            // basic nitrogens do not qualify
            switch (atom.AtomicNumber)
            {
                case AtomicNumbers.N:
                    bool basic = true;
                    for (int n = 0; n < atomAdj[aidx].Length; n++)
                        if (!aliphatic[atomAdj[aidx][n]])
                        {
                            basic = false;
                            break;
                        }
                    if (basic)
                        return false;
                    break;
            }

            return true;
        }

        /// positive charge centre
        private bool DeterminePositive(int aidx)
        {
            var atom = mol.Atoms[aidx];

            // consider formal ionic charge first
            int chg = atom.FormalCharge.Value;
            if (chg < 0)
                return false;
            if (chg > 0)
            {
                for (int n = 0; n < atomAdj[aidx].Length; n++)
                    if (mol.Atoms[atomAdj[aidx][n]].FormalCharge < 0)
                        return false;
                return true;
            }
            var el = atom.Symbol;

            switch (atom.AtomicNumber)
            {
                case AtomicNumbers.N:
                    {
                        // basic amines, i.e. aliphatic neighbours
                        bool basic = true;
                        for (int n = 0; n < atomAdj[aidx].Length; n++)
                            if (!aliphatic[atomAdj[aidx][n]])
                            {
                                basic = false;
                                break;
                            }
                        if (basic)
                            return true;

                        // imines with N=C-N motif: the carbon atom must be bonded to at least one amine, and both other substituents
                        // have to be without double bonds, i.e. R-N=C(R)NR2 or R-N=C(NR2)NR2 (R=not hydrogen)
                        if (hasDouble[aidx] && hcount[aidx] == 0)
                        {
                            int other = -1;
                            for (int n = 0; n < atomAdj[aidx].Length; n++)
                                if (BondOrderBioType(bondAdj[aidx][n]) == 2)
                                {
                                    other = atomAdj[aidx][n];
                                    break;
                                }
                            if (other >= 0)
                            {
                                int amines = 0;
                                for (int n = 0; n < atomAdj[other].Length; n++)
                                {
                                    int a = atomAdj[other][n];
                                    if (a == aidx)
                                        continue;
                                    if (hasDouble[a])
                                    {
                                        amines = 0;
                                        break;
                                    }
                                    var aan = mol.Atoms[a].AtomicNumber;
                                    switch (aan)
                                    {
                                        case AtomicNumbers.N:
                                            if (hcount[a] > 0)
                                            {
                                                amines = 0;
                                                goto break_for;
                                            }
                                            amines++;
                                            break;
                                        case AtomicNumbers.C:
                                            break;
                                        default:
                                            amines = 0;
                                            goto break_for;
                                    }
                                }
                            break_for:
                                if (amines > 0)
                                    return true;
                            }
                        }
                    }
                    break;
                case AtomicNumbers.C:
                    {
                        // carbon-centred charge if imine & H-containing amine present, i.e. =NR and -N[H]R both
                        bool imine = false, amine = false;
                        for (int n = 0; n < atomAdj[aidx].Length; n++)
                        {
                            int a = atomAdj[aidx][n];
                            if (tetrazole[a])
                            {
                                imine = false;
                                amine = false;
                                break;
                            }
                            if (!mol.Atoms[a].AtomicNumber.Equals(AtomicNumbers.N))
                                continue;
                            if (BondOrderBioType(bondAdj[aidx][n]) == 2)
                                imine = true;
                            else if (hcount[a] == 1)
                                amine = true;
                        }
                        if (imine && amine)
                            return true;
                    }
                    break;
            }

            return false;
        }

        /// negative charge centre
        private bool DetermineNegative(int aidx)
        {
            var atom = mol.Atoms[aidx];

            // consider formal ionic charge first
            int chg = atom.FormalCharge.Value;
            if (chg > 0)
                return false;
            if (chg < 0)
            {
                for (int n = 0; n < atomAdj[aidx].Length; n++)
                    if (mol.Atoms[atomAdj[aidx][n]].FormalCharge > 0)
                        return false;
                return true;
            }

            string el = atom.Symbol;

            // tetrazole nitrogens get negative charges
            if (tetrazole[aidx] && atom.AtomicNumber.Equals(AtomicNumbers.N))
                return true;

            // centres with an oxide and an -OH group qualify as negative
            if (isOxide[aidx])
            {
                switch (atom.AtomicNumber)
                {
                    case AtomicNumbers.C:
                    case AtomicNumbers.S:
                    case AtomicNumbers.P:
                        for (int n = 0; n < atomAdj[aidx].Length; n++)
                            if (BondOrderBioType(bondAdj[aidx][n]) == 1)
                            {
                                int a = atomAdj[aidx][n];
                                if (mol.Atoms[a].AtomicNumber.Equals(AtomicNumbers.O) && hcount[a] > 0)
                                    return true;
                            }
                        break;
                }
            }

            return false;
        }

        /// halide
        private bool DetermineHalide(int aidx)
        {
            switch (mol.Atoms[aidx].AtomicNumber)
            {
                case AtomicNumbers.F:
                case AtomicNumbers.Cl:
                case AtomicNumbers.Br:
                case AtomicNumbers.I:
                    return true;
            }
            return false;
        }

        /// returns either the bond order in the molecule, or -1 if the atoms are both labelled as aromatic
        private int BondOrderBioType(int bidx)
        {
            var bond = mol.Bonds[bidx];
            if (bond.Atoms.Count != 2)
                return 0;
            int a1 = mol.Atoms.IndexOf(bond.Begin), a2 = mol.Atoms.IndexOf(bond.End);
            if (maskAro[a1] && maskAro[a2])
                return -1;
            return bondOrder[bidx];
        }

        /// convenience: appending to an int array
        private static int[] AppendInteger(int[] a, int v)
        {
            if (a == null || a.Length == 0)
                return new int[] { v };
            var b = new int[a.Length + 1];
            for (int n = a.Length - 1; n >= 0; n--)
                b[n] = a[n];
            b[a.Length] = v;
            return b;
        }

        /// convenience: scans the atom adjacency to grab the bond index
        private int FindBond(int a1, int a2)
        {
            for (int n = atomAdj[a1].Length - 1; n >= 0; n--)
                if (atomAdj[a1][n] == a2)
                    return bondAdj[a1][n];
            return -1;
        }
    }
}
