/*
 * Note: I adapted this fingerprint from Yap Chun Wei's PaDEL source code, which can be found here:
 * http://www.yapcwsoft.com/dd/padeldescriptor/
 * 
 * Author: Lyle D. Burgoon, Ph.D. (lyle.d.burgoon@usace.army.mil)
 * 
 * This is the work of a US Government employee. This code is in the public domain.
 */

using NCDK.Graphs;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Generates an atom pair 2D fingerprint as implemented in PaDEL given an <see cref="IAtomContainer"/>, that
    /// extends the <see cref="Fingerprinter"/>.
    /// </summary>
    /// <seealso cref="Fingerprinter"/>
    // @author Lyle Burgoon (lyle.d.burgoon@usace.army.mil)
    // @cdk.created 2018-02-05
    // @cdk.keyword fingerprint
    // @cdk.keyword similarity
    // @cdk.module fingerprint
    public class AtomPairs2DFingerprinter
        : AbstractFingerprinter, IFingerprinter
    {
        private const int MAX_DISTANCE = 10;
        private static readonly string[] atypes = { "C", "N", "O", "S", "P", "F", "Cl", "Br", "I", "B", "Si", "X" };

        private readonly Dictionary<string, int> pathToBit = new Dictionary<string, int>();
        private readonly Dictionary<int, string> bitToPath = new Dictionary<int, string>();

        public AtomPairs2DFingerprinter()
        {
            for (int dist = 1; dist <= MAX_DISTANCE; dist++)
            {
                for (int i = 0; i < atypes.Length; i++)
                {
                    for (int j = i; j < atypes.Length; j++)
                    {
                        string key_name = $"{dist}_{atypes[i]}_{atypes[j]}";
                        pathToBit[key_name] = pathToBit.Count;
                        bitToPath[bitToPath.Count] = key_name;
                    }
                }
            }
        }

        public override int Length => pathToBit.Count;

        /// <summary>
        /// Checks if an atom is a halogen
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        private static bool IsHalogen(IAtom atom)
        {
            switch (atom.AtomicNumber)
            {
                case 9:  // F
                case 17: // Cl
                case 35: // Br
                case 53: // I
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Atoms that we are using in the fingerprint
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        private static bool Include(IAtom atom)
        {
            switch (atom.AtomicNumber)
            {
                case 5:  // B
                case 6:  // C
                case 7:  // N
                case 8:  // O
                case 14: // Si
                case 15: // P
                case 16: // S
                case 9:  // F
                case 17: // Cl
                case 35: // Br
                case 53: // I
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Creates the fingerprint name which is used as a key in our hashes
        /// </summary>
        /// <param name="dist"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static string EncodePath(int dist, IAtom a, IAtom b)
        {
            return dist + "_" + a.Symbol + "_" + b.Symbol;
        }

        /// <summary>
        /// Encodes name for halogen paths
        /// </summary>
        /// <param name="dist"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static string EncodeHalPath(int dist, IAtom a, IAtom b)
        {
            return $"{dist}_{(IsHalogen(a) ? "X" : a.Symbol)}_{(IsHalogen(b) ? "X" : b.Symbol)}";
        }

        /// <summary>
        /// This performs the calculations used to generate the fingerprint 
        /// </summary>
        private static void Calculate(IList<string> paths, IAtomContainer mol)
        {
            var apsp = new AllPairsShortestPaths(mol);
            int numAtoms = mol.Atoms.Count;
            for (int i = 0; i < numAtoms; i++)
            {
                if (!Include(mol.Atoms[i]))
                    continue;
                for (int j = i + 1; j < numAtoms; j++)
                {
                    if (!Include(mol.Atoms[j]))
                        continue;
                    int dist = apsp.From(i).GetDistanceTo(j);
                    if (dist > MAX_DISTANCE)
                        continue;
                    var beg = mol.Atoms[i];
                    var end = mol.Atoms[j];
                    paths.Add(EncodePath(dist, beg, end));
                    paths.Add(EncodePath(dist, end, beg));
                    if (IsHalogen(mol.Atoms[i]) || IsHalogen(mol.Atoms[j]))
                    {
                        paths.Add(EncodeHalPath(dist, beg, end));
                        paths.Add(EncodeHalPath(dist, end, beg));
                    }
                }
            }
        }

        public override IBitFingerprint GetBitFingerprint(IAtomContainer container)
        {
            var fp = new BitArray(pathToBit.Count);
            var paths = new List<string>();
            Calculate(paths, container);
            foreach (string path in paths)
            {
                if (pathToBit.TryGetValue(path, out int value))
                    fp.Set(value, true);
            }
            return new BitSetFingerprint(fp);
        }

        public override IReadOnlyDictionary<string, int> GetRawFingerprint(IAtomContainer mol)
        {
            var raw = new Dictionary<string, int>();
            var paths = new List<string>();
            Calculate(paths, mol);

            paths.Sort();
            int count = 0;
            string prev = null;
            foreach (string path in paths)
            {
                if (prev == null || !string.Equals(path, prev, StringComparison.Ordinal))
                {
                    if (count > 0)
                        raw[prev] = count;
                    count = 1;
                    prev = path;
                }
                else
                {
                    ++count;
                }
            }
            if (count > 0)
                raw[prev] = count;

            return raw;
        }

        class CountFingerprintImpl : ICountFingerprint
        {
            readonly AtomPairs2DFingerprinter parent;
            readonly IReadOnlyDictionary<string, int> raw;
            readonly List<string> keys;

            public CountFingerprintImpl(
                AtomPairs2DFingerprinter parent,
                IReadOnlyDictionary<string, int> raw,
                List<string> keys)
            {
                this.parent = parent;
                this.raw = raw;
                this.keys = keys;
            }

            public long Length => parent.pathToBit.Count;
            public int GetNumberOfPopulatedBins() => keys.Count;
            public int GetCount(int index) => raw[keys[index]];
            public int GetHash(int index) => parent.pathToBit[keys[index]];
            public void Merge(ICountFingerprint fp) { }
            public void SetBehaveAsBitFingerprint(bool behaveAsBitFingerprint) { }
            public bool HasHash(int hash) => parent.bitToPath.ContainsKey(hash);
            public int GetCountForHash(int hash) => raw[parent.bitToPath[hash]];
        }

        public override ICountFingerprint GetCountFingerprint(IAtomContainer mol)
        {
            var raw = GetRawFingerprint(mol);
            var keys = new List<string>(raw.Keys);
            return new CountFingerprintImpl(this, raw, keys);
        }
    }
}
