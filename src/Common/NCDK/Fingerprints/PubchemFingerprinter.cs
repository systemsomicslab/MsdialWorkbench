/* Copyright (C) 2009  Rajarshi Guha <rajarshi.guha@gmail.com>
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

using NCDK.Graphs;
using NCDK.SMARTS;
using NCDK.Tools;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Generates a Pubchem fingerprint for a molecule.
    /// These fingerprints are described
    /// <see href="ftp://ftp.ncbi.nlm.nih.gov/pubchem/specifications/pubchem_fingerprints.txt">here</see> and are of the structural key type, of length 881. 
    /// See <see cref="Fingerprinter"/> for a more detailed description of fingerprints in general. This implementation is
    /// based on the domain code made available by the NCGC
    /// <see href="http://www.ncgc.nih.gov/pub/openhts/code/NCGC_PubChemFP.java.txt">here</see>.
    /// </summary>
    /// <example>
    /// A fingerprint is generated for an <see cref="IAtomContainer"/> with this code: 
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Fingerprints.PubchemFingerprinter_Example.cs"]/*' />
    /// </example>
    /// <remarks>
    /// <note type="note">
    /// The fingerprinter assumes that you have detected aromaticity and
    /// atom types before evaluating the fingerprint. Also the fingerprinter
    /// expects that explicit H's are present
    /// </note>
    /// <note type="note">
    /// Note that this fingerprint is not particularly fast, as it will perform
    /// ring detection using <see cref="RingSearches.AllRingsFinder"/>
    /// as well as multiple SMARTS queries.
    /// </note>
    /// <para>
    /// Some SMARTS patterns have been modified from the original code, since they
    /// were based on explicit H matching. As a result, we replace the explicit H's
    /// with a query of the '#&lt;N&gt;&amp;!H0' where '&lt;N&gt;' is the atomic number. Thus bit 344 was
    /// with a query of the #N&amp;!H0 where N is the atomic number. Thus bit 344 was
    /// originally "[#6](~[#6])([H])" but is written here as
    /// "[#6&amp;!H0]~[#6]". In some cases, where the H count can be reduced
    /// to single possibility we directly use that H count. An example is bit 35,
    /// which was "[#6](~[#6])(~[#6])(~[#6])([H])" and is rewritten as
    /// "[#6H1](~[#6])(~[#6])(~[#6])".
    /// </para>
    /// <note type="warning">
    /// This class is not thread-safe and uses stores intermediate steps
    /// internally. Please use a separate instance of the class for each thread.
    /// </note>
    /// </remarks>
    /// <threadsafety static="true" instance="false"/>
    // @author Rajarshi Guha
    // @cdk.keyword fingerprint
    // @cdk.keyword similarity
    // @cdk.module fingerprint
    // @cdk.threadnonsafe
    public class PubchemFingerprinter : AbstractFingerprinter, IFingerprinter
    {
        /// <summary>
        /// Number of bits in this fingerprint.
        /// </summary>
        const int FPSize = 881;

        private byte[] m_bits;

        private Dictionary<string, SmartsPattern> cache = new Dictionary<string, SmartsPattern>();

        public PubchemFingerprinter()
        {
            m_bits = new byte[(FPSize + 7) >> 3];
        }

        /// <summary>
        /// Calculate 881 bit Pubchem fingerprint for a molecule.
        /// </summary>
        /// <remarks>
        /// See <see href="ftp://ftp.ncbi.nlm.nih.gov/pubchem/specifications/pubchem_fingerprints.txt">here</see>
        /// for a description of each bit position.</remarks>
        /// <param name="atomContainer">the molecule to consider</param>
        /// <returns>the fingerprint</returns>
        /// <exception cref="CDKException">if there is an error during substructure searching or atom typing</exception>
        /// <see cref="GetFingerprintAsBytes"/>
        public override IBitFingerprint GetBitFingerprint(IAtomContainer atomContainer)
        {
            GenerateFp(atomContainer);
            BitArray fp = new BitArray(FPSize);
            for (int i = 0; i < FPSize; i++)
            {
                if (IsBitOn(i))
                    fp.Set(i, true);
            }
            return new BitSetFingerprint(fp);
        }

        /// <inheritdoc/>
        public override IReadOnlyDictionary<string, int> GetRawFingerprint(IAtomContainer iAtomContainer)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// the size of the fingerprint.
        /// </summary>
        public override int Length => FPSize;

        class ElementsCounter
        {
            private readonly int[] counts = new int[120];

            public ElementsCounter(IAtomContainer m)
            {
                for (int i = 0; i < m.Atoms.Count; i++)
                    ++counts[m.Atoms[i].AtomicNumber];
            }

            public int GetCount(int atno)
            {
                return counts[atno];
            }

            public int GetCount(string symb)
            {
                return counts[PeriodicTable.GetAtomicNumber(symb)];
            }
        }

        class RingsCounter
        {
            readonly IRingSet ringSet;

            public RingsCounter(IAtomContainer m)
            {
                ringSet = Cycles.FindSSSR(m).ToRingSet();
            }

            public int CountAnyRing(int size)
            {
                int c = 0;
                foreach (var ring in ringSet)
                {
                    if (ring.Atoms.Count == size) c++;
                }
                return c;
            }

            private static bool IsCarbonOnlyRing(IAtomContainer ring)
            {
                foreach (var ringAtom in ring.Atoms)
                {
                    if (!ringAtom.AtomicNumber.Equals(AtomicNumbers.C))
                        return false;
                }
                return true;
            }

            private static bool IsRingSaturated(IAtomContainer ring)
            {
                foreach (var ringBond in ring.Bonds)
                {
                    if (ringBond.Order != BondOrder.Single
                     || ringBond.IsAromatic
                     || ringBond.IsSingleOrDouble)
                        return false;
                }
                return true;
            }

            private static bool IsRingUnsaturated(IAtomContainer ring)
            {
                return !IsRingSaturated(ring);
            }

            private static int CountNitrogenInRing(IAtomContainer ring)
            {
                int c = 0;
                foreach (var ringAtom in ring.Atoms)
                {
                    if (ringAtom.AtomicNumber.Equals(AtomicNumbers.N))
                        c++;
                }
                return c;
            }

            private static int CountHeteroInRing(IAtomContainer ring)
            {
                int c = 0;
                foreach (var ringAtom in ring.Atoms)
                {
                    switch (ringAtom.AtomicNumber)
                    {
                        case AtomicNumbers.C:
                        case AtomicNumbers.H:
                            break;
                        default:
                            c++;
                            break;
                    }
                }
                return c;
            }

            private static bool IsAromaticRing(IAtomContainer ring)
            {
                foreach (var bond in ring.Bonds)
                    if (!bond.IsAromatic)
                        return false;
                return true;
            }

            public int CountAromaticRing()
            {
                int c = 0;
                foreach (var ring in ringSet)
                {
                    if (IsAromaticRing(ring)) c++;
                }
                return c;
            }

            public int CountHeteroAromaticRing()
            {
                int c = 0;
                foreach (var ring in ringSet)
                {
                    if (!IsCarbonOnlyRing(ring) && IsAromaticRing(ring)) c++;
                }
                return c;
            }

            public int CountSaturatedOrAromaticCarbonOnlyRing(int size)
            {
                int c = 0;
                foreach (var ring in ringSet)
                {
                    if (ring.Atoms.Count == size && IsCarbonOnlyRing(ring)
                            && (IsRingSaturated(ring) || IsAromaticRing(ring))) c++;
                }
                return c;
            }

            public int CountSaturatedOrAromaticNitrogenContainingRing(int size)
            {
                int c = 0;
                foreach (var ring in ringSet)
                {
                    if (ring.Atoms.Count == size && (IsRingSaturated(ring) || IsAromaticRing(ring))
                            && CountNitrogenInRing(ring) > 0) ++c;
                }
                return c;
            }

            public int CountSaturatedOrAromaticHeteroContainingRing(int size)
            {
                int c = 0;
                foreach (var ring in ringSet)
                {
                    if (ring.Atoms.Count == size && (IsRingSaturated(ring) || IsAromaticRing(ring))
                            && CountHeteroInRing(ring) > 0) ++c;
                }
                return c;
            }

            public int CountUnsaturatedCarbonOnlyRing(int size)
            {
                int c = 0;
                foreach (var ring in ringSet)
                {
                    if (ring.Atoms.Count == size && IsRingUnsaturated(ring) && !IsAromaticRing(ring)
                            && IsCarbonOnlyRing(ring)) ++c;
                }
                return c;
            }

            public int CountUnsaturatedNitrogenContainingRing(int size)
            {
                int c = 0;
                foreach (var ring in ringSet)
                {
                    if (ring.Atoms.Count == size && IsRingUnsaturated(ring) && !IsAromaticRing(ring)
                            && CountNitrogenInRing(ring) > 0) ++c;
                }
                return c;
            }

            public int CountUnsaturatedHeteroContainingRing(int size)
            {
                int c = 0;
                foreach (var ring in ringSet)
                {
                    if (ring.Atoms.Count == size 
                     && IsRingUnsaturated(ring) 
                     && !IsAromaticRing(ring)
                     && CountHeteroInRing(ring) > 0)
                        ++c;
                }
                return c;
            }
        }

        class SubstructuresCounter
        {
            PubchemFingerprinter parent;
            private readonly IAtomContainer mol;

            public SubstructuresCounter(PubchemFingerprinter parent, IAtomContainer m)
            {
                this.parent = parent;
                mol = m;
            }

            public int CountSubstructure(string smarts)
            {
                if (!parent.cache.TryGetValue(smarts, out SmartsPattern ptrn))
                {
                    ptrn = SmartsPattern.Create(smarts);
                    ptrn.SetPrepare(false);
                    parent.cache[smarts] = ptrn;
                }
                return ptrn.MatchAll(mol).CountUnique();
            }
        }

        private void _GenerateFp(byte[] fp, IAtomContainer mol)
        {
            SmartsPattern.Prepare(mol);
            CountElements(fp, mol);
            CountRings(fp, mol);
            CountSubstructures(fp, mol);
        }

        private void GenerateFp(IAtomContainer mol)
        {
            for (int i = 0; i < m_bits.Length; ++i)
            {
                m_bits[i] = 0;
            }
            _GenerateFp(m_bits, mol);
        }

        private bool IsBitOn(int bit)
        {
            return (m_bits[bit >> 3] & (byte)Mask[bit % 8]) != 0;
        }

        /// <summary>
        /// Returns the fingerprint generated for a molecule as a byte[].
        /// </summary>
        /// <remarks>
        /// Note that this should be immediately called after calling <see cref="GetRawFingerprint(IAtomContainer)"/>.
        /// </remarks>
        /// <returns>The fingerprint as a byte array</returns>
        /// <seealso cref="GetRawFingerprint(IAtomContainer)"/>
        public byte[] GetFingerprintAsBytes()
        {
            return m_bits;
        }

        /// <summary>
        /// Returns a fingerprint from a Base64 encoded Pubchem fingerprint.
        /// </summary>
        /// <param name="enc">The Base64 encoded fingerprint</param>
        /// <returns>A BitArray corresponding to the input fingerprint</returns>
        public static BitArray Decode(string enc)
        {
            byte[] fp;
            try
            {
                fp = Convert.FromBase64String(enc);
            }
            catch (FormatException)
            {
                throw new ArgumentException("Input is not a proper PubChem base64 encoded fingerprint", nameof(enc));
            }

            int len = (fp[0] << 24) | (fp[1] << 16) | (fp[2] << 8) | (fp[3] & 0xff);
            if (len != FPSize)
            {
                throw new ArgumentException("Input is not a proper PubChem base64 encoded fingerprint");
            }

            // note the IChemObjectBuilder is passed as null because the SMARTSQueryTool
            // isn't needed when decoding
            var pc = new PubchemFingerprinter();
            for (int i = 0; i < pc.m_bits.Length; ++i)
            {
                pc.m_bits[i] = fp[i + 4];
            }

            var ret = new BitArray(FPSize);
            for (int i = 0; i < FPSize; i++)
            {
                if (pc.IsBitOn(i))
                    ret.Set(i, true);
            }
            return ret;
        }

        /// the first four bytes contains the length of the fingerprint
        private string Encode()
        {
            byte[] pack = new byte[4 + m_bits.Length];

            pack[0] = (byte)((FPSize & 0xffffffff) >> 24);
            pack[1] = (byte)((FPSize & 0x00ffffff) >> 16);
            pack[2] = (byte)((FPSize & 0x0000ffff) >> 8);
            pack[3] = (byte)(FPSize & 0x000000ff);
            for (int i = 0; i < m_bits.Length; ++i)
            {
                pack[i + 4] = m_bits[i];
            }
            return Base64Encode(pack);
        }

        private const string BASE64_LUT = "ABCDEFGHIJKLMNOPQRSTUVWXYZ" + "abcdefghijklmnopqrstuvwxyz0123456789+/=";

        /// based on NCBI C implementation
        private static string Base64Encode(byte[] data)
        {
            char[] c64 = new char[data.Length * 4 / 3 + 5];
            for (int i = 0, k = 0; i < data.Length; i += 3, k += 4)
            {
                c64[k + 0] = (char)(data[i] >> 2);
                c64[k + 1] = (char)((data[i] & 0x03) << 4);
                c64[k + 2] = c64[k + 3] = (char)64;
                if ((i + i) < data.Length)
                {
                    c64[k + 1] |= (char)(data[i + 1] >> 4);
                    c64[k + 2] = (char)((data[i + 1] & 0x0f) << 2);
                }
                if ((i + 2) < data.Length)
                {
                    c64[k + 2] |= (char)(data[i + 2] >> 6);
                    c64[k + 3] = (char)(data[i + 2] & 0x3f);
                }
                for (int j = 0; j < 4; ++j)
                {
                    c64[k + j] = BASE64_LUT[c64[k + j]];
                }
            }
            return new string(c64);
        }

        static readonly int[] Mask = new int[] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 };

        // Section 1: Hierarchic Element Counts - These bs test for the presence or
        // count of individual chemical atoms represented by their atomic symbol.
        private static void CountElements(byte[] fp, IAtomContainer mol)
        {
            int b;
            var ce = new ElementsCounter(mol);

            b = 0;
            if (ce.GetCount("H") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 1;
            if (ce.GetCount("H") >= 8) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 2;
            if (ce.GetCount("H") >= 16) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 3;
            if (ce.GetCount("H") >= 32) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 4;
            if (ce.GetCount("Li") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 5;
            if (ce.GetCount("Li") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 6;
            if (ce.GetCount("B") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 7;
            if (ce.GetCount("B") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 8;
            if (ce.GetCount("B") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 9;
            if (ce.GetCount("C") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 10;
            if (ce.GetCount("C") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 11;
            if (ce.GetCount("C") >= 8) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 12;
            if (ce.GetCount("C") >= 16) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 13;
            if (ce.GetCount("C") >= 32) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 14;
            if (ce.GetCount("N") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 15;
            if (ce.GetCount("N") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 16;
            if (ce.GetCount("N") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 17;
            if (ce.GetCount("N") >= 8) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 18;
            if (ce.GetCount("O") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 19;
            if (ce.GetCount("O") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 20;
            if (ce.GetCount("O") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 21;
            if (ce.GetCount("O") >= 8) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 22;
            if (ce.GetCount("O") >= 16) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 23;
            if (ce.GetCount("F") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 24;
            if (ce.GetCount("F") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 25;
            if (ce.GetCount("F") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 26;
            if (ce.GetCount("Na") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 27;
            if (ce.GetCount("Na") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 28;
            if (ce.GetCount("Si") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 29;
            if (ce.GetCount("Si") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 30;
            if (ce.GetCount("P") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 31;
            if (ce.GetCount("P") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 32;
            if (ce.GetCount("P") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 33;
            if (ce.GetCount("S") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 34;
            if (ce.GetCount("S") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 35;
            if (ce.GetCount("S") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 36;
            if (ce.GetCount("S") >= 8) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 37;
            if (ce.GetCount("Cl") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 38;
            if (ce.GetCount("Cl") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 39;
            if (ce.GetCount("Cl") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 40;
            if (ce.GetCount("Cl") >= 8) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 41;
            if (ce.GetCount("K") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 42;
            if (ce.GetCount("K") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 43;
            if (ce.GetCount("Br") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 44;
            if (ce.GetCount("Br") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 45;
            if (ce.GetCount("Br") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 46;
            if (ce.GetCount("I") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 47;
            if (ce.GetCount("I") >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 48;
            if (ce.GetCount("I") >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 49;
            if (ce.GetCount("Be") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 50;
            if (ce.GetCount("Mg") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 51;
            if (ce.GetCount("Al") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 52;
            if (ce.GetCount("Ca") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 53;
            if (ce.GetCount("Sc") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 54;
            if (ce.GetCount("Ti") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 55;
            if (ce.GetCount("V") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 56;
            if (ce.GetCount("Cr") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 57;
            if (ce.GetCount("Mn") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 58;
            if (ce.GetCount("Fe") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 59;
            if (ce.GetCount("Co") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 60;
            if (ce.GetCount("Ni") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 61;
            if (ce.GetCount("Cu") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 62;
            if (ce.GetCount("Zn") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 63;
            if (ce.GetCount("Ga") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 64;
            if (ce.GetCount("Ge") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 65;
            if (ce.GetCount("As") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 66;
            if (ce.GetCount("Se") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 67;
            if (ce.GetCount("Kr") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 68;
            if (ce.GetCount("Rb") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 69;
            if (ce.GetCount("Sr") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 70;
            if (ce.GetCount("Y") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 71;
            if (ce.GetCount("Zr") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 72;
            if (ce.GetCount("Nb") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 73;
            if (ce.GetCount("Mo") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 74;
            if (ce.GetCount("Ru") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 75;
            if (ce.GetCount("Rh") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 76;
            if (ce.GetCount("Pd") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 77;
            if (ce.GetCount("Ag") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 78;
            if (ce.GetCount("Cd") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 79;
            if (ce.GetCount("In") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 80;
            if (ce.GetCount("Sn") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 81;
            if (ce.GetCount("Sb") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 82;
            if (ce.GetCount("Te") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 83;
            if (ce.GetCount("Xe") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 84;
            if (ce.GetCount("Cs") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 85;
            if (ce.GetCount("Ba") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 86;
            if (ce.GetCount("Lu") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 87;
            if (ce.GetCount("Hf") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 88;
            if (ce.GetCount("Ta") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 89;
            if (ce.GetCount("W") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 90;
            if (ce.GetCount("Re") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 91;
            if (ce.GetCount("Os") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 92;
            if (ce.GetCount("Ir") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 93;
            if (ce.GetCount("Pt") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 94;
            if (ce.GetCount("Au") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 95;
            if (ce.GetCount("Hg") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 96;
            if (ce.GetCount("Tl") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 97;
            if (ce.GetCount("Pb") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 98;
            if (ce.GetCount("Bi") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 99;
            if (ce.GetCount("La") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 100;
            if (ce.GetCount("Ce") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 101;
            if (ce.GetCount("Pr") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 102;
            if (ce.GetCount("Nd") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 103;
            if (ce.GetCount("Pm") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 104;
            if (ce.GetCount("Sm") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 105;
            if (ce.GetCount("Eu") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 106;
            if (ce.GetCount("Gd") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 107;
            if (ce.GetCount("Tb") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 108;
            if (ce.GetCount("Dy") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 109;
            if (ce.GetCount("Ho") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 110;
            if (ce.GetCount("Er") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 111;
            if (ce.GetCount("Tm") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 112;
            if (ce.GetCount("Yb") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 113;
            if (ce.GetCount("Tc") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 114;
            if (ce.GetCount("U") >= 1) fp[b >> 3] |= (byte)Mask[b % 8];
        }

        // Section 2: Rings in a canonic ESSR ring set-These bs test for the
        // presence or count of the described chemical ring system. An ESSR ring is
        // any ring which does not share three consecutive atoms with any other ring
        // in the chemical structure. For example, naphthalene has three ESSR rings
        // (two phenyl fragments and the 10-membered envelope), while biphenyl will
        // yield a count of only two ESSR rings.
        private static void CountRings(byte[] fp, IAtomContainer mol)
        {
            RingsCounter cr = new RingsCounter(mol);
            int b;

            b = 115;
            if (cr.CountAnyRing(3) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 116;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(3) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 117;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(3) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 118;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(3) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 119;
            if (cr.CountUnsaturatedCarbonOnlyRing(3) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 120;
            if (cr.CountUnsaturatedNitrogenContainingRing(3) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 121;
            if (cr.CountUnsaturatedHeteroContainingRing(3) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 122;
            if (cr.CountAnyRing(3) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 123;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(3) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 124;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(3) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 125;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(3) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 126;
            if (cr.CountUnsaturatedCarbonOnlyRing(3) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 127;
            if (cr.CountUnsaturatedNitrogenContainingRing(3) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 128;
            if (cr.CountUnsaturatedHeteroContainingRing(3) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 129;
            if (cr.CountAnyRing(4) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 130;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(4) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 131;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(4) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 132;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(4) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 133;
            if (cr.CountUnsaturatedCarbonOnlyRing(4) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 134;
            if (cr.CountUnsaturatedNitrogenContainingRing(4) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 135;
            if (cr.CountUnsaturatedHeteroContainingRing(4) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 136;
            if (cr.CountAnyRing(4) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 137;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(4) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 138;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(4) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 139;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(4) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 140;
            if (cr.CountUnsaturatedCarbonOnlyRing(4) >= 2) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 141;
            if (cr.CountUnsaturatedNitrogenContainingRing(4) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 142;
            if (cr.CountUnsaturatedHeteroContainingRing(4) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 143;
            if (cr.CountAnyRing(5) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 144;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(5) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 145;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(5) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 146;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(5) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 147;
            if (cr.CountUnsaturatedCarbonOnlyRing(5) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 148;
            if (cr.CountUnsaturatedNitrogenContainingRing(5) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 149;
            if (cr.CountUnsaturatedHeteroContainingRing(5) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 150;
            if (cr.CountAnyRing(5) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 151;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(5) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 152;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(5) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 153;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(5) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 154;
            if (cr.CountUnsaturatedCarbonOnlyRing(5) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 155;
            if (cr.CountUnsaturatedNitrogenContainingRing(5) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 156;
            if (cr.CountUnsaturatedHeteroContainingRing(5) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 157;
            if (cr.CountAnyRing(5) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 158;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(5) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 159;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(5) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 160;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(5) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 161;
            if (cr.CountUnsaturatedCarbonOnlyRing(5) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 162;
            if (cr.CountUnsaturatedNitrogenContainingRing(5) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 163;
            if (cr.CountUnsaturatedHeteroContainingRing(5) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 164;
            if (cr.CountAnyRing(5) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 165;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(5) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 166;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(5) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 167;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(5) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 168;
            if (cr.CountUnsaturatedCarbonOnlyRing(5) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 169;
            if (cr.CountUnsaturatedNitrogenContainingRing(5) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 170;
            if (cr.CountUnsaturatedHeteroContainingRing(5) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 171;
            if (cr.CountAnyRing(5) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 172;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(5) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 173;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(5) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 174;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(5) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 175;
            if (cr.CountUnsaturatedCarbonOnlyRing(5) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 176;
            if (cr.CountUnsaturatedNitrogenContainingRing(5) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 177;
            if (cr.CountUnsaturatedHeteroContainingRing(5) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 178;
            if (cr.CountAnyRing(6) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 179;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(6) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 180;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(6) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 181;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(6) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 182;
            if (cr.CountUnsaturatedCarbonOnlyRing(6) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 183;
            if (cr.CountUnsaturatedNitrogenContainingRing(6) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 184;
            if (cr.CountUnsaturatedHeteroContainingRing(6) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 185;
            if (cr.CountAnyRing(6) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 186;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(6) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 187;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(6) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 188;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(6) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 189;
            if (cr.CountUnsaturatedCarbonOnlyRing(6) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 190;
            if (cr.CountUnsaturatedNitrogenContainingRing(6) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 191;
            if (cr.CountUnsaturatedHeteroContainingRing(6) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 192;
            if (cr.CountAnyRing(6) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 193;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(6) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 194;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(6) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 195;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(6) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 196;
            if (cr.CountUnsaturatedCarbonOnlyRing(6) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 197;
            if (cr.CountUnsaturatedNitrogenContainingRing(6) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 198;
            if (cr.CountUnsaturatedHeteroContainingRing(6) >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 199;
            if (cr.CountAnyRing(6) >= 4) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 200;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(6) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 201;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(6) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 202;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(6) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 203;
            if (cr.CountUnsaturatedCarbonOnlyRing(6) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 204;
            if (cr.CountUnsaturatedNitrogenContainingRing(6) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 205;
            if (cr.CountUnsaturatedHeteroContainingRing(6) >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 206;
            if (cr.CountAnyRing(6) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 207;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(6) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 208;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(6) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 209;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(6) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 210;
            if (cr.CountUnsaturatedCarbonOnlyRing(6) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 211;
            if (cr.CountUnsaturatedNitrogenContainingRing(6) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 212;
            if (cr.CountUnsaturatedHeteroContainingRing(6) >= 5)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 213;
            if (cr.CountAnyRing(7) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 214;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(7) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 215;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(7) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 216;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(7) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 217;
            if (cr.CountUnsaturatedCarbonOnlyRing(7) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 218;
            if (cr.CountUnsaturatedNitrogenContainingRing(7) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 219;
            if (cr.CountUnsaturatedHeteroContainingRing(7) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 220;
            if (cr.CountAnyRing(7) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 221;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(7) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 222;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(7) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 223;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(7) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 224;
            if (cr.CountUnsaturatedCarbonOnlyRing(7) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 225;
            if (cr.CountUnsaturatedNitrogenContainingRing(7) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 226;
            if (cr.CountUnsaturatedHeteroContainingRing(7) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 227;
            if (cr.CountAnyRing(8) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 228;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(8) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 229;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(8) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 230;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(8) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 231;
            if (cr.CountUnsaturatedCarbonOnlyRing(8) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 232;
            if (cr.CountUnsaturatedNitrogenContainingRing(8) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 233;
            if (cr.CountUnsaturatedHeteroContainingRing(8) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 234;
            if (cr.CountAnyRing(8) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 235;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(8) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 236;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(8) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 237;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(8) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 238;
            if (cr.CountUnsaturatedCarbonOnlyRing(8) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 239;
            if (cr.CountUnsaturatedNitrogenContainingRing(8) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 240;
            if (cr.CountUnsaturatedHeteroContainingRing(8) >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 241;
            if (cr.CountAnyRing(9) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 242;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(9) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 243;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(9) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 244;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(9) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 245;
            if (cr.CountUnsaturatedCarbonOnlyRing(9) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 246;
            if (cr.CountUnsaturatedNitrogenContainingRing(9) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 247;
            if (cr.CountUnsaturatedHeteroContainingRing(9) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 248;
            if (cr.CountAnyRing(10) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 249;
            if (cr.CountSaturatedOrAromaticCarbonOnlyRing(10) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 250;
            if (cr.CountSaturatedOrAromaticNitrogenContainingRing(10) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 251;
            if (cr.CountSaturatedOrAromaticHeteroContainingRing(10) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 252;
            if (cr.CountUnsaturatedCarbonOnlyRing(10) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 253;
            if (cr.CountUnsaturatedNitrogenContainingRing(10) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 254;
            if (cr.CountUnsaturatedHeteroContainingRing(10) >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];

            b = 255;
            if (cr.CountAromaticRing() >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 256;
            if (cr.CountHeteroAromaticRing() >= 1)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 257;
            if (cr.CountAromaticRing() >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 258;
            if (cr.CountHeteroAromaticRing() >= 2)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 259;
            if (cr.CountAromaticRing() >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 260;
            if (cr.CountHeteroAromaticRing() >= 3)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 261;
            if (cr.CountAromaticRing() >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 262;
            if (cr.CountHeteroAromaticRing() >= 4)
                fp[b >> 3] |= (byte)Mask[b % 8];
        }

        private void CountSubstructures(byte[] fp, IAtomContainer mol)
        {
            var cs = new SubstructuresCounter(this, mol);
            int b;

            // Section 3: Simple atom pairs. These bits test for the presence of
            // patterns of bonded atom pairs, regardless of bond order or count.
            b = 263;
            if (cs.CountSubstructure("[Li&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 264;
            if (cs.CountSubstructure("[Li]~[Li]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 265;
            if (cs.CountSubstructure("[Li]~[#5]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 266;
            if (cs.CountSubstructure("[Li]~[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 267;
            if (cs.CountSubstructure("[Li]~[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 268;
            if (cs.CountSubstructure("[Li]~[F]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 269;
            if (cs.CountSubstructure("[Li]~[#15]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 270;
            if (cs.CountSubstructure("[Li]~[#16]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 271;
            if (cs.CountSubstructure("[Li]~[Cl]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 272;
            if (cs.CountSubstructure("[#5&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 273;
            if (cs.CountSubstructure("[#5]~[#5]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 274;
            if (cs.CountSubstructure("[#5]~[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 275;
            if (cs.CountSubstructure("[#5]~[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 276;
            if (cs.CountSubstructure("[#5]~[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 277;
            if (cs.CountSubstructure("[#5]~[F]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 278;
            if (cs.CountSubstructure("[#5]~[#14]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 279;
            if (cs.CountSubstructure("[#5]~[#15]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 280;
            if (cs.CountSubstructure("[#5]~[#16]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 281;
            if (cs.CountSubstructure("[#5]~[Cl]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 282;
            if (cs.CountSubstructure("[#5]~[Br]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 283;
            if (cs.CountSubstructure("[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 284;
            if (cs.CountSubstructure("[#6]~[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 285;
            if (cs.CountSubstructure("[#6]~[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 286;
            if (cs.CountSubstructure("[#6]~[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 287;
            if (cs.CountSubstructure("[#6]~[F]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 288;
            if (cs.CountSubstructure("[#6]~[Na]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 289;
            if (cs.CountSubstructure("[#6]~[Mg]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 290;
            if (cs.CountSubstructure("[#6]~[Al]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 291;
            if (cs.CountSubstructure("[#6]~[#14]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 292;
            if (cs.CountSubstructure("[#6]~[#15]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 293;
            if (cs.CountSubstructure("[#6]~[#16]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 294;
            if (cs.CountSubstructure("[#6]~[Cl]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 295;
            if (cs.CountSubstructure("[#6]~[#33]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 296;
            if (cs.CountSubstructure("[#6]~[#34]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 297;
            if (cs.CountSubstructure("[#6]~[Br]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 298;
            if (cs.CountSubstructure("[#6]~[I]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 299;
            if (cs.CountSubstructure("[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 300;
            if (cs.CountSubstructure("[#7]~[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 301;
            if (cs.CountSubstructure("[#7]~[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 302;
            if (cs.CountSubstructure("[#7]~[F]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 303;
            if (cs.CountSubstructure("[#7]~[#14]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 304;
            if (cs.CountSubstructure("[#7]~[#15]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 305;
            if (cs.CountSubstructure("[#7]~[#16]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 306;
            if (cs.CountSubstructure("[#7]~[Cl]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 307;
            if (cs.CountSubstructure("[#7]~[Br]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 308;
            if (cs.CountSubstructure("[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 309;
            if (cs.CountSubstructure("[#8]~[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 310;
            if (cs.CountSubstructure("[#8]~[Mg]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 311;
            if (cs.CountSubstructure("[#8]~[Na]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 312;
            if (cs.CountSubstructure("[#8]~[Al]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 313;
            if (cs.CountSubstructure("[#8]~[#14]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 314;
            if (cs.CountSubstructure("[#8]~[#15]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 315;
            if (cs.CountSubstructure("[#8]~[K]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 316;
            if (cs.CountSubstructure("[F]~[#15]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 317;
            if (cs.CountSubstructure("[F]~[#16]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 318;
            if (cs.CountSubstructure("[Al&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 319;
            if (cs.CountSubstructure("[Al]~[Cl]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 320;
            if (cs.CountSubstructure("[#14&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 321;
            if (cs.CountSubstructure("[#14]~[#14]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 322;
            if (cs.CountSubstructure("[#14]~[Cl]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 323;
            if (cs.CountSubstructure("[#15&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 324;
            if (cs.CountSubstructure("[#15]~[#15]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 325;
            if (cs.CountSubstructure("[#33&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 326;
            if (cs.CountSubstructure("[#33]~[#33]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];

            // Section 4: Simple atom nearest neighbors. These bits test for the
            // presence of atom nearest neighbor patterns, regardless of bond order
            // or count, but where bond aromaticity (denoted by "~") is significant.
            b = 327;
            if (cs.CountSubstructure("[#6](~Br)(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 328;
            if (cs.CountSubstructure("[#6](~Br)(~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 329;
            if (cs.CountSubstructure("[#6&!H0]~[Br]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 330;
            if (cs.CountSubstructure("[#6](~[Br])(:[c])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 331;
            if (cs.CountSubstructure("[#6](~[Br])(:[n])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 332;
            if (cs.CountSubstructure("[#6](~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 333;
            if (cs.CountSubstructure("[#6](~[#6])(~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 334;
            if (cs.CountSubstructure("[#6](~[#6])(~[#6])(~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 335;
            if (cs.CountSubstructure("[#6H1](~[#6])(~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 336;
            if (cs.CountSubstructure("[#6](~[#6])(~[#6])(~[#6])(~[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 337;
            if (cs.CountSubstructure("[#6](~[#6])(~[#6])(~[#6])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 338;
            if (cs.CountSubstructure("[#6H1](~[#6])(~[#6])(~[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 339;
            if (cs.CountSubstructure("[#6H1](~[#6])(~[#6])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 340;
            if (cs.CountSubstructure("[#6](~[#6])(~[#6])(~[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 341;
            if (cs.CountSubstructure("[#6](~[#6])(~[#6])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 342;
            if (cs.CountSubstructure("[#6](~[#6])(~[Cl])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 343;
            if (cs.CountSubstructure("[#6&!H0](~[#6])(~[Cl])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 344;
            if (cs.CountSubstructure("[#6H,#6H2,#6H3,#6H4]~[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 345;
            if (cs.CountSubstructure("[#6&!H0](~[#6])(~[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 346;
            if (cs.CountSubstructure("[#6&!H0](~[#6])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 347;
            if (cs.CountSubstructure("[#6H1](~[#6])(~[#8])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 348;
            if (cs.CountSubstructure("[#6&!H0](~[#6])(~[#15])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 349;
            if (cs.CountSubstructure("[#6&!H0](~[#6])(~[#16])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 350;
            if (cs.CountSubstructure("[#6](~[#6])(~[I])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 351;
            if (cs.CountSubstructure("[#6](~[#6])(~[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 352;
            if (cs.CountSubstructure("[#6](~[#6])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 353;
            if (cs.CountSubstructure("[#6](~[#6])(~[#16])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 354;
            if (cs.CountSubstructure("[#6](~[#6])(~[#14])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 355;
            if (cs.CountSubstructure("[#6](~[#6])(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 356;
            if (cs.CountSubstructure("[#6](~[#6])(:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 357;
            if (cs.CountSubstructure("[#6](~[#6])(:c)(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 358;
            if (cs.CountSubstructure("[#6](~[#6])(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 359;
            if (cs.CountSubstructure("[#6](~[#6])(:n)(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 360;
            if (cs.CountSubstructure("[#6](~[Cl])(~[Cl])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 361;
            if (cs.CountSubstructure("[#6&!H0](~[Cl])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 362;
            if (cs.CountSubstructure("[#6](~[Cl])(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 363;
            if (cs.CountSubstructure("[#6](~[F])(~[F])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 364;
            if (cs.CountSubstructure("[#6](~[F])(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 365;
            if (cs.CountSubstructure("[#6&!H0](~[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 366;
            if (cs.CountSubstructure("[#6&!H0](~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 367;
            if (cs.CountSubstructure("[#6&!H0](~[#8])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 368;
            if (cs.CountSubstructure("[#6&!H0](~[#16])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 369;
            if (cs.CountSubstructure("[#6&!H0](~[#14])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 370;
            if (cs.CountSubstructure("[#6&!H0]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 371;
            if (cs.CountSubstructure("[#6&!H0](:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 372;
            if (cs.CountSubstructure("[#6&!H0](:c)(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 373;
            if (cs.CountSubstructure("[#6&!H0](:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 374;
            if (cs.CountSubstructure("[#6H3]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 375;
            if (cs.CountSubstructure("[#6](~[#7])(~[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 376;
            if (cs.CountSubstructure("[#6](~[#7])(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 377;
            if (cs.CountSubstructure("[#6](~[#7])(:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 378;
            if (cs.CountSubstructure("[#6](~[#7])(:c)(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 379;
            if (cs.CountSubstructure("[#6](~[#7])(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 380;
            if (cs.CountSubstructure("[#6](~[#8])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 381;
            if (cs.CountSubstructure("[#6](~[#8])(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 382;
            if (cs.CountSubstructure("[#6](~[#8])(:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 383;
            if (cs.CountSubstructure("[#6](~[#16])(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 384;
            if (cs.CountSubstructure("[#6](:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 385;
            if (cs.CountSubstructure("[#6](:c)(:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 386;
            if (cs.CountSubstructure("[#6](:c)(:c)(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 387;
            if (cs.CountSubstructure("[#6](:c)(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 388;
            if (cs.CountSubstructure("[#6](:c)(:n)(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 389;
            if (cs.CountSubstructure("[#6](:n)(:n)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 390;
            if (cs.CountSubstructure("[#7](~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 391;
            if (cs.CountSubstructure("[#7](~[#6])(~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 392;
            if (cs.CountSubstructure("[#7&!H0](~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 393;
            if (cs.CountSubstructure("[#7&!H0](~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 394;
            if (cs.CountSubstructure("[#7&!H0](~[#6])(~[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 395;
            if (cs.CountSubstructure("[#7](~[#6])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 396;
            if (cs.CountSubstructure("[#7](~[#6])(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 397;
            if (cs.CountSubstructure("[#7](~[#6])(:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 398;
            if (cs.CountSubstructure("[#7&!H0](~[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 399;
            if (cs.CountSubstructure("[#7&!H0](:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 400;
            if (cs.CountSubstructure("[#7&!H0](:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 401;
            if (cs.CountSubstructure("[#7](~[#8])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 402;
            if (cs.CountSubstructure("[#7](~[#8])(:o)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 403;
            if (cs.CountSubstructure("[#7](:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 404;
            if (cs.CountSubstructure("[#7](:c)(:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 405;
            if (cs.CountSubstructure("[#8](~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 406;
            if (cs.CountSubstructure("[#8&!H0](~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 407;
            if (cs.CountSubstructure("[#8](~[#6])(~[#15])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 408;
            if (cs.CountSubstructure("[#8&!H0](~[#16])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 409;
            if (cs.CountSubstructure("[#8](:c)(:c)") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 410;
            if (cs.CountSubstructure("[#15](~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 411;
            if (cs.CountSubstructure("[#15](~[#8])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 412;
            if (cs.CountSubstructure("[#16](~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 413;
            if (cs.CountSubstructure("[#16&!H0](~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 414;
            if (cs.CountSubstructure("[#16](~[#6])(~[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 415;
            if (cs.CountSubstructure("[#14](~[#6])(~[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];

            // Section 5: Detailed atom neighborhoods - These bits test for the
            // presence of detailed atom neighborhood patterns, regardless of count,
            // but where bond orders are specific, bond aromaticity matches both
            // single and double bonds, and where "-", "=", and "#" matches a single
            // bond, double bond, and triple bond order, respectively.

            b = 416;
            if (cs.CountSubstructure("[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 417;
            if (cs.CountSubstructure("[#6]#[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 418;
            if (cs.CountSubstructure("[#6]=,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 419;
            if (cs.CountSubstructure("[#6]#[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 420;
            if (cs.CountSubstructure("[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 421;
            if (cs.CountSubstructure("[#6]=,:[#16]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 422;
            if (cs.CountSubstructure("[#7]=,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 423;
            if (cs.CountSubstructure("[#7]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 424;
            if (cs.CountSubstructure("[#7]=,:[#15]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 425;
            if (cs.CountSubstructure("[#15]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 426;
            if (cs.CountSubstructure("[#15]=,:[#15]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 427;
            if (cs.CountSubstructure("[#6](#[#6])(-,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 428;
            if (cs.CountSubstructure("[#6&!H0](#[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 429;
            if (cs.CountSubstructure("[#6](#[#7])(-,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 430;
            if (cs.CountSubstructure("[#6](-,:[#6])(-,:[#6])(=,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 431;
            if (cs.CountSubstructure("[#6](-,:[#6])(-,:[#6])(=,:[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 432;
            if (cs.CountSubstructure("[#6](-,:[#6])(-,:[#6])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 433;
            if (cs.CountSubstructure("[#6](-,:[#6])([Cl])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 434;
            if (cs.CountSubstructure("[#6&!H0](-,:[#6])(=,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 435;
            if (cs.CountSubstructure("[#6&!H0](-,:[#6])(=,:[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 436;
            if (cs.CountSubstructure("[#6&!H0](-,:[#6])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 437;
            if (cs.CountSubstructure("[#6](-,:[#6])(-,:[#7])(=,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 438;
            if (cs.CountSubstructure("[#6](-,:[#6])(-,:[#7])(=,:[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 439;
            if (cs.CountSubstructure("[#6](-,:[#6])(-,:[#7])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 440;
            if (cs.CountSubstructure("[#6](-,:[#6])(-,:[#8])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 441;
            if (cs.CountSubstructure("[#6](-,:[#6])(=,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 442;
            if (cs.CountSubstructure("[#6](-,:[#6])(=,:[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 443;
            if (cs.CountSubstructure("[#6](-,:[#6])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 444;
            if (cs.CountSubstructure("[#6]([Cl])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 445;
            if (cs.CountSubstructure("[#6&!H0](-,:[#7])(=,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 446;
            if (cs.CountSubstructure("[#6&!H0](=,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 447;
            if (cs.CountSubstructure("[#6&!H0](=,:[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 448;
            if (cs.CountSubstructure("[#6&!H0](=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 449;
            if (cs.CountSubstructure("[#6](-,:[#7])(=,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 450;
            if (cs.CountSubstructure("[#6](-,:[#7])(=,:[#7])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 451;
            if (cs.CountSubstructure("[#6](-,:[#7])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 452;
            if (cs.CountSubstructure("[#6](-,:[#8])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 453;
            if (cs.CountSubstructure("[#7](-,:[#6])(=,:[#6])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 454;
            if (cs.CountSubstructure("[#7](-,:[#6])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 455;
            if (cs.CountSubstructure("[#7](-,:[#8])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 456;
            if (cs.CountSubstructure("[#15](-,:[#8])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 457;
            if (cs.CountSubstructure("[#16](-,:[#6])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 458;
            if (cs.CountSubstructure("[#16](-,:[#8])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 459;
            if (cs.CountSubstructure("[#16](=,:[#8])(=,:[#8])") > 0) fp[b >> 3] |= (byte)Mask[b % 8];

            // Section 6: Simple SMARTS patterns - These bits test for the presence
            // of simple SMARTS patterns, regardless of count, but where bond orders
            // are specific and bond aromaticity matches both single and double
            // bonds.
            b = 460;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]#[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 461;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]=,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 462;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 463;
            if (cs.CountSubstructure("[#7]:[#6]-,:[#16&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 464;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 465;
            if (cs.CountSubstructure("[#8]=,:[#16]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 466;
            if (cs.CountSubstructure("[#7]#[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 467;
            if (cs.CountSubstructure("[#6]=,:[#7]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 468;
            if (cs.CountSubstructure("[#8]=,:[#16]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 469;
            if (cs.CountSubstructure("[#16]-,:[#16]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 470;
            if (cs.CountSubstructure("[#6]:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 471;
            if (cs.CountSubstructure("[#16]:[#6]:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 472;
            if (cs.CountSubstructure("[#6]:[#7]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 473;
            if (cs.CountSubstructure("[#16]-,:[#6]:[#7]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 474;
            if (cs.CountSubstructure("[#16]:[#6]:[#6]:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 475;
            if (cs.CountSubstructure("[#16]-,:[#6]=,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 476;
            if (cs.CountSubstructure("[#6]-,:[#8]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 477;
            if (cs.CountSubstructure("[#7]-,:[#7]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 478;
            if (cs.CountSubstructure("[#16]-,:[#6]=,:[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 479;
            if (cs.CountSubstructure("[#16]-,:[#6]-,:[#16]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 480;
            if (cs.CountSubstructure("[#6]:[#16]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 481;
            if (cs.CountSubstructure("[#8]-,:[#16]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 482;
            if (cs.CountSubstructure("[#6]:[#7]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 483;
            if (cs.CountSubstructure("[#7]-,:[#16]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 484;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#7]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 485;
            if (cs.CountSubstructure("[#7]:[#6]:[#6]:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 486;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#7]:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 487;
            if (cs.CountSubstructure("[#7]-,:[#6]=,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 488;
            if (cs.CountSubstructure("[#7]-,:[#6]=,:[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 489;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#16]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 490;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 491;
            if (cs.CountSubstructure("[#6]-,:[#7]:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 492;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#8]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 493;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 494;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 495;
            if (cs.CountSubstructure("[#6]-,:[#7]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 496;
            if (cs.CountSubstructure("[#7]:[#7]-,:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 497;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 498;
            if (cs.CountSubstructure("[#8]-,:[#6]=,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 499;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#6]:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 500;
            if (cs.CountSubstructure("[#6]-,:[#16]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 501;
            if (cs.CountSubstructure("[Cl]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 502;
            if (cs.CountSubstructure("[#7]-,:[#6]=,:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 503;
            if (cs.CountSubstructure("[Cl]-,:[#6]:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 504;
            if (cs.CountSubstructure("[#7]:[#6]:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 505;
            if (cs.CountSubstructure("[Cl]-,:[#6]:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 506;
            if (cs.CountSubstructure("[#6]-,:[#6]:[#7]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 507;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#16]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 508;
            if (cs.CountSubstructure("[#16]=,:[#6]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 509;
            if (cs.CountSubstructure("[Br]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 510;
            if (cs.CountSubstructure("[#7&!H0]-,:[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 511;
            if (cs.CountSubstructure("[#16]=,:[#6]-,:[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 512;
            if (cs.CountSubstructure("[#6]-,:[#33]-[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 513;
            if (cs.CountSubstructure("[#16]:[#6]:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 514;
            if (cs.CountSubstructure("[#8]-,:[#7]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 515;
            if (cs.CountSubstructure("[#7]-,:[#7]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 516;
            if (cs.CountSubstructure("[#6H,#6H2,#6H3]=,:[#6H,#6H2,#6H3]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 517;
            if (cs.CountSubstructure("[#7]-,:[#7]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 518;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#7]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 519;
            if (cs.CountSubstructure("[#7]=,:[#6]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 520;
            if (cs.CountSubstructure("[#6]=,:[#6]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 521;
            if (cs.CountSubstructure("[#6]:[#7]-,:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 522;
            if (cs.CountSubstructure("[#6]-,:[#7]-,:[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 523;
            if (cs.CountSubstructure("[#7]:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 524;
            if (cs.CountSubstructure("[#6]-,:[#6]=,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 525;
            if (cs.CountSubstructure("[#33]-,:[#6]:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 526;
            if (cs.CountSubstructure("[Cl]-,:[#6]:[#6]-,:[Cl]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 527;
            if (cs.CountSubstructure("[#6]:[#6]:[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 528;
            if (cs.CountSubstructure("[#7&!H0]-,:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 529;
            if (cs.CountSubstructure("[Cl]-,:[#6]-,:[#6]-,:[Cl]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 530;
            if (cs.CountSubstructure("[#7]:[#6]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 531;
            if (cs.CountSubstructure("[#16]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 532;
            if (cs.CountSubstructure("[#16]-,:[#6]:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 533;
            if (cs.CountSubstructure("[#16]-,:[#6]:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 534;
            if (cs.CountSubstructure("[#16]-,:[#6]:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 535;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 536;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 537;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 538;
            if (cs.CountSubstructure("[#7]=,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 539;
            if (cs.CountSubstructure("[#7]=,:[#6]-,:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 540;
            if (cs.CountSubstructure("[#6]-,:[#7]-,:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 541;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 542;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 543;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 544;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 545;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 546;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 547;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 548;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 549;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 550;
            if (cs.CountSubstructure("[Cl]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 551;
            if (cs.CountSubstructure("[Cl]-,:[#6]-,:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 552;
            if (cs.CountSubstructure("[#6]:[#6]-,:[#6]:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 553;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 554;
            if (cs.CountSubstructure("[Br]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 555;
            if (cs.CountSubstructure("[#7]=,:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 556;
            if (cs.CountSubstructure("[#6]=,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 557;
            if (cs.CountSubstructure("[#7]:[#6]-,:[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 558;
            if (cs.CountSubstructure("[#8]=,:[#7]-,:c:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 559;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 560;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 561;
            if (cs.CountSubstructure("[Cl]-,:[#6]-,:[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 562;
            if (cs.CountSubstructure("[Br]-,:[#6]-,:[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 563;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 564;
            if (cs.CountSubstructure("[#6]=,:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 565;
            if (cs.CountSubstructure("[#6]:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 566;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 567;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 568;
            if (cs.CountSubstructure("N#[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 569;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 570;
            if (cs.CountSubstructure("[#6]:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 571;
            if (cs.CountSubstructure("[#6&!H0]-,:[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 572;
            if (cs.CountSubstructure("n:c:n:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 573;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 574;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 575;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 576;
            if (cs.CountSubstructure("[#7]=,:[#6]-,:[#6]:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 577;
            if (cs.CountSubstructure("c:c-,:[#7]-,:c:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 578;
            if (cs.CountSubstructure("[#6]-,:[#6]:[#6]-,:c:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 579;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 580;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 581;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 582;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 583;
            if (cs.CountSubstructure("[Cl]-,:[#6]:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 584;
            if (cs.CountSubstructure("c:c-,:[#6]=,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 585;
            if (cs.CountSubstructure("[#6]-,:[#6]:[#6]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 586;
            if (cs.CountSubstructure("[#6]-,:[#16]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 587;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#6]-,:[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 588;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 589;
            if (cs.CountSubstructure("[#6]-,:[#6]:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 590;
            if (cs.CountSubstructure("[#6]-,:[#6]:[#6]-,:[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 591;
            if (cs.CountSubstructure("[Cl]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 592;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 593;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 594;
            if (cs.CountSubstructure("[#6]-,:[#8]-,:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 595;
            if (cs.CountSubstructure("c:c-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 596;
            if (cs.CountSubstructure("[#7]=,:[#6]-,:[#7]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 597;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:c:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 598;
            if (cs.CountSubstructure("[Cl]-,:[#6]:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 599;
            if (cs.CountSubstructure("[#6H,#6H2,#6H3]-,:[#6]=,:[#6H,#6H2,#6H3]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 600;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 601;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#6]:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 602;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 603;
            if (cs.CountSubstructure("[#6]-,:c:c:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 604;
            if (cs.CountSubstructure("[#6]-,:[#8]-,:[#6]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 605;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 606;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 607;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 608;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 609;
            if (cs.CountSubstructure("[Cl]-,:[#6]-,:[#6]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 610;
            if (cs.CountSubstructure("[#6]-,:[#8]-,:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 611;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 612;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#8]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 613;
            if (cs.CountSubstructure("[#6]-,:[#7]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 614;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#8]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 615;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 616;
            if (cs.CountSubstructure("c:c:n:n:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 617;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]-,:[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 618;
            if (cs.CountSubstructure("c:[#6]-,:[#6]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 619;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]=,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 620;
            if (cs.CountSubstructure("c:c-,:[#8]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 621;
            if (cs.CountSubstructure("[#7]-,:[#6]:c:c:n") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 622;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#8]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 623;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 624;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 625;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 626;
            if (cs.CountSubstructure("[#6]-,:[#8]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 627;
            if (cs.CountSubstructure("[#8]=,:[#33]-,:[#6]:c:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 628;
            if (cs.CountSubstructure("[#6]-,:[#7]-,:[#6]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 629;
            if (cs.CountSubstructure("[#16]-,:[#6]:c:c-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 630;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 631;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]-,:[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 632;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#8]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 633;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 634;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 635;
            if (cs.CountSubstructure("[#7]-,:[#7]-,:[#6]-,:[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 636;
            if (cs.CountSubstructure("[#6]-,:[#7]-,:[#6]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 637;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 638;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 639;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 640;
            if (cs.CountSubstructure("[#6]=,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 641;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 642;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 643;
            if (cs.CountSubstructure("[#6&!H0]-,:[#6]-,:[#7&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 644;
            if (cs.CountSubstructure("[#6]-,:[#6]=,:[#7]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 645;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#7]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 646;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#7]-,:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 647;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#7]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 648;
            if (cs.CountSubstructure("[#8]=,:[#7]-,:[#6]:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 649;
            if (cs.CountSubstructure("[#8]=,:[#7]-,:c:c-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 650;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#7]-,:[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 651;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 652;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 653;
            if (cs.CountSubstructure("[#8]-,:[#6]:[#6]:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 654;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#7]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 655;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 656;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#7]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 657;
            if (cs.CountSubstructure("[#6]-,:[#7]-,:[#6]:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 658;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#16]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 659;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#7]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 660;
            if (cs.CountSubstructure("[#6]-,:[#6]=,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 661;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#8]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 662;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 663;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 664;
            if (cs.CountSubstructure("[#6]-,:[#6]=,:[#6]-,:[#6]=,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 665;
            if (cs.CountSubstructure("[#7]-,:[#6]:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 666;
            if (cs.CountSubstructure("[#6]=,:[#6]-,:[#6]-,:[#8]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 667;
            if (cs.CountSubstructure("[#6]=,:[#6]-,:[#6]-,:[#8&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 668;
            if (cs.CountSubstructure("[#6]-,:[#6]:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 669;
            if (cs.CountSubstructure("[Cl]-,:[#6]:[#6]-,:[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 670;
            if (cs.CountSubstructure("[Br]-,:[#6]:c:c-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 671;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]=,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 672;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]=,:[#6&!H0]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 673;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]=,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 674;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#7]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 675;
            if (cs.CountSubstructure("[Br]-,:[#6]-,:[#6]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 676;
            if (cs.CountSubstructure("[#7]#[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 677;
            if (cs.CountSubstructure("[#6]-,:[#6]=,:[#6]-,:[#6]:c") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 678;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]=,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 679;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 680;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 681;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 682;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 683;
            if (cs.CountSubstructure("[#7]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 684;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 685;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 686;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 687;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 688;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 689;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 690;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 691;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 692;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 693;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 694;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]=,:[#8]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 695;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#7]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 696;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 697;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6](-,:[#6])-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 698;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 699;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6](-,:[#6])-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 700;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#8]-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 701;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6](-,:[#8])-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 702;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#7]-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 703;
            if (cs.CountSubstructure("[#8]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6](-,:[#7])-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 704;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 705;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6](-,:[#8])-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 706;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6](=,:[#8])-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 707;
            if (cs.CountSubstructure("[#8]=,:[#6]-,:[#6]-,:[#6]-,:[#6]-,:[#6](-,:[#7])-,:[#6]") > 0)
                fp[b >> 3] |= (byte)Mask[b % 8];
            b = 708;
            if (cs.CountSubstructure("[#6]-,:[#6](-,:[#6])-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 709;
            if (cs.CountSubstructure("[#6]-,:[#6](-,:[#6])-,:[#6]-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 710;
            if (cs.CountSubstructure("[#6]-,:[#6]-,:[#6](-,:[#6])-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 711;
            if (cs.CountSubstructure("[#6]-,:[#6](-,:[#6])(-,:[#6])-,:[#6]-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 712;
            if (cs.CountSubstructure("[#6]-,:[#6](-,:[#6])-,:[#6](-,:[#6])-,:[#6]") > 0) fp[b >> 3] |= (byte)Mask[b % 8];

            // Section 7: Complex SMARTS patterns - These bits test for the presence
            // of complex SMARTS patterns, regardless of count, but where bond
            // orders and bond aromaticity are specific.

            b = 713;
            if (cs.CountSubstructure("[#6]c1ccc([#6])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 714;
            if (cs.CountSubstructure("[#6]c1ccc([#8])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 715;
            if (cs.CountSubstructure("[#6]c1ccc([#16])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 716;
            if (cs.CountSubstructure("[#6]c1ccc([#7])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 717;
            if (cs.CountSubstructure("[#6]c1ccc(Cl)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 718;
            if (cs.CountSubstructure("[#6]c1ccc(Br)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 719;
            if (cs.CountSubstructure("[#8]c1ccc([#8])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 720;
            if (cs.CountSubstructure("[#8]c1ccc([#16])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 721;
            if (cs.CountSubstructure("[#8]c1ccc([#7])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 722;
            if (cs.CountSubstructure("[#8]c1ccc(Cl)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 723;
            if (cs.CountSubstructure("[#8]c1ccc(Br)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 724;
            if (cs.CountSubstructure("[#16]c1ccc([#16])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 725;
            if (cs.CountSubstructure("[#16]c1ccc([#7])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 726;
            if (cs.CountSubstructure("[#16]c1ccc(Cl)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 727;
            if (cs.CountSubstructure("[#16]c1ccc(Br)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 728;
            if (cs.CountSubstructure("[#7]c1ccc([#7])cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 729;
            if (cs.CountSubstructure("[#7]c1ccc(Cl)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 730;
            if (cs.CountSubstructure("[#7]c1ccc(Br)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 731;
            if (cs.CountSubstructure("Clc1ccc(Cl)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 732;
            if (cs.CountSubstructure("Clc1ccc(Br)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 733;
            if (cs.CountSubstructure("Brc1ccc(Br)cc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 734;
            if (cs.CountSubstructure("[#6]c1cc([#6])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 735;
            if (cs.CountSubstructure("[#6]c1cc([#8])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 736;
            if (cs.CountSubstructure("[#6]c1cc([#16])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 737;
            if (cs.CountSubstructure("[#6]c1cc([#7])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 738;
            if (cs.CountSubstructure("[#6]c1cc(Cl)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 739;
            if (cs.CountSubstructure("[#6]c1cc(Br)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 740;
            if (cs.CountSubstructure("[#8]c1cc([#8])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 741;
            if (cs.CountSubstructure("[#8]c1cc([#16])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 742;
            if (cs.CountSubstructure("[#8]c1cc([#7])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 743;
            if (cs.CountSubstructure("[#8]c1cc(Cl)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 744;
            if (cs.CountSubstructure("[#8]c1cc(Br)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 745;
            if (cs.CountSubstructure("[#16]c1cc([#16])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 746;
            if (cs.CountSubstructure("[#16]c1cc([#7])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 747;
            if (cs.CountSubstructure("[#16]c1cc(Cl)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 748;
            if (cs.CountSubstructure("[#16]c1cc(Br)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 749;
            if (cs.CountSubstructure("[#7]c1cc([#7])ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 750;
            if (cs.CountSubstructure("[#7]c1cc(Cl)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 751;
            if (cs.CountSubstructure("[#7]c1cc(Br)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 752;
            if (cs.CountSubstructure("Clc1cc(Cl)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 753;
            if (cs.CountSubstructure("Clc1cc(Br)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 754;
            if (cs.CountSubstructure("Brc1cc(Br)ccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 755;
            if (cs.CountSubstructure("[#6]c1c([#6])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 756;
            if (cs.CountSubstructure("[#6]c1c([#8])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 757;
            if (cs.CountSubstructure("[#6]c1c([#16])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 758;
            if (cs.CountSubstructure("[#6]c1c([#7])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 759;
            if (cs.CountSubstructure("[#6]c1c(Cl)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 760;
            if (cs.CountSubstructure("[#6]c1c(Br)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 761;
            if (cs.CountSubstructure("[#8]c1c([#8])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 762;
            if (cs.CountSubstructure("[#8]c1c([#16])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 763;
            if (cs.CountSubstructure("[#8]c1c([#7])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 764;
            if (cs.CountSubstructure("[#8]c1c(Cl)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 765;
            if (cs.CountSubstructure("[#8]c1c(Br)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 766;
            if (cs.CountSubstructure("[#16]c1c([#16])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 767;
            if (cs.CountSubstructure("[#16]c1c([#7])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 768;
            if (cs.CountSubstructure("[#16]c1c(Cl)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 769;
            if (cs.CountSubstructure("[#16]c1c(Br)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 770;
            if (cs.CountSubstructure("[#7]c1c([#7])cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 771;
            if (cs.CountSubstructure("[#7]c1c(Cl)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 772;
            if (cs.CountSubstructure("[#7]c1c(Br)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 773;
            if (cs.CountSubstructure("Clc1c(Cl)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 774;
            if (cs.CountSubstructure("Clc1c(Br)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 775;
            if (cs.CountSubstructure("Brc1c(Br)cccc1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 776;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6][#6]([#6])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 777;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6][#6]([#8])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 778;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6][#6]([#16])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 779;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6][#6]([#7])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 780;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 781;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 782;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6][#6]([#8])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 783;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6][#6]([#16])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 784;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6][#6]([#7])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 785;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 786;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 787;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6][#6]([#16])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 788;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6][#6]([#7])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 789;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 790;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 791;
            if (cs.CountSubstructure("[#7][#6]1[#6][#6][#6]([#7])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 792;
            if (cs.CountSubstructure("[#7][#6]1[#6][#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 793;
            if (cs.CountSubstructure("[#7][#6]1[#6][#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 794;
            if (cs.CountSubstructure("Cl[#6]1[#6][#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 795;
            if (cs.CountSubstructure("Cl[#6]1[#6][#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 796;
            if (cs.CountSubstructure("Br[#6]1[#6][#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 797;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6]([#6])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 798;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6]([#8])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 799;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6]([#16])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 800;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6]([#7])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 801;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6](Cl)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 802;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 803;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6]([#8])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 804;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6]([#16])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 805;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6]([#7])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 806;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6](Cl)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 807;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 808;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6]([#16])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 809;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6]([#7])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 810;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6](Cl)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 811;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 812;
            if (cs.CountSubstructure("[#7][#6]1[#6][#6]([#7])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 813;
            if (cs.CountSubstructure("[#7][#6]1[#6][#6](Cl)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 814;
            if (cs.CountSubstructure("[#7][#6]1[#6][#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 815;
            if (cs.CountSubstructure("Cl[#6]1[#6][#6](Cl)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 816;
            if (cs.CountSubstructure("Cl[#6]1[#6][#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 817;
            if (cs.CountSubstructure("Br[#6]1[#6][#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 818;
            if (cs.CountSubstructure("[#6][#6]1[#6]([#6])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 819;
            if (cs.CountSubstructure("[#6][#6]1[#6]([#8])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 820;
            if (cs.CountSubstructure("[#6][#6]1[#6]([#16])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 821;
            if (cs.CountSubstructure("[#6][#6]1[#6]([#7])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 822;
            if (cs.CountSubstructure("[#6][#6]1[#6](Cl)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 823;
            if (cs.CountSubstructure("[#6][#6]1[#6](Br)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 824;
            if (cs.CountSubstructure("[#8][#6]1[#6]([#8])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 825;
            if (cs.CountSubstructure("[#8][#6]1[#6]([#16])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 826;
            if (cs.CountSubstructure("[#8][#6]1[#6]([#7])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 827;
            if (cs.CountSubstructure("[#8][#6]1[#6](Cl)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 828;
            if (cs.CountSubstructure("[#8][#6]1[#6](Br)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 829;
            if (cs.CountSubstructure("[#16][#6]1[#6]([#16])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 830;
            if (cs.CountSubstructure("[#16][#6]1[#6]([#7])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 831;
            if (cs.CountSubstructure("[#16][#6]1[#6](Cl)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 832;
            if (cs.CountSubstructure("[#16][#6]1[#6](Br)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 833;
            if (cs.CountSubstructure("[#7][#6]1[#6]([#7])[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 834;
            if (cs.CountSubstructure("[#7][#6]1[#6](Cl)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 835;
            if (cs.CountSubstructure("[#7][#6]1[#6](Br)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 836;
            if (cs.CountSubstructure("Cl[#6]1[#6](Cl)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 837;
            if (cs.CountSubstructure("Cl[#6]1[#6](Br)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 838;
            if (cs.CountSubstructure("Br[#6]1[#6](Br)[#6][#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 839;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6]([#6])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 840;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6]([#8])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 841;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6]([#16])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 842;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6]([#7])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 843;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 844;
            if (cs.CountSubstructure("[#6][#6]1[#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 845;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6]([#8])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 846;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6]([#16])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 847;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6]([#7])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 848;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 849;
            if (cs.CountSubstructure("[#8][#6]1[#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 850;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6]([#16])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 851;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6]([#7])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 852;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 853;
            if (cs.CountSubstructure("[#16][#6]1[#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 854;
            if (cs.CountSubstructure("[#7][#6]1[#6][#6]([#7])[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 855;
            if (cs.CountSubstructure("[#7][#6]1[#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 856;
            if (cs.CountSubstructure("[#7][#6]1[#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 857;
            if (cs.CountSubstructure("Cl[#6]1[#6][#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 858;
            if (cs.CountSubstructure("Cl[#6]1[#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 859;
            if (cs.CountSubstructure("Br[#6]1[#6][#6](Br)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 860;
            if (cs.CountSubstructure("[#6][#6]1[#6]([#6])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 861;
            if (cs.CountSubstructure("[#6][#6]1[#6]([#8])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 862;
            if (cs.CountSubstructure("[#6][#6]1[#6]([#16])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 863;
            if (cs.CountSubstructure("[#6][#6]1[#6]([#7])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 864;
            if (cs.CountSubstructure("[#6][#6]1[#6](Cl)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 865;
            if (cs.CountSubstructure("[#6][#6]1[#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 866;
            if (cs.CountSubstructure("[#8][#6]1[#6]([#8])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 867;
            if (cs.CountSubstructure("[#8][#6]1[#6]([#16])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 868;
            if (cs.CountSubstructure("[#8][#6]1[#6]([#7])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 869;
            if (cs.CountSubstructure("[#8][#6]1[#6](Cl)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 870;
            if (cs.CountSubstructure("[#8][#6]1[#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 871;
            if (cs.CountSubstructure("[#16][#6]1[#6]([#16])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 872;
            if (cs.CountSubstructure("[#16][#6]1[#6]([#7])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 873;
            if (cs.CountSubstructure("[#16][#6]1[#6](Cl)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 874;
            if (cs.CountSubstructure("[#16][#6]1[#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 875;
            if (cs.CountSubstructure("[#7][#6]1[#6]([#7])[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 876;
            if (cs.CountSubstructure("[#7][#6]1[#6](Cl)[#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 877;
            if (cs.CountSubstructure("[#7][#6]1[#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 878;
            if (cs.CountSubstructure("Cl[#6]1[#6](Cl)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 879;
            if (cs.CountSubstructure("Cl[#6]1[#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
            b = 880;
            if (cs.CountSubstructure("Br[#6]1[#6](Br)[#6][#6][#6]1") > 0) fp[b >> 3] |= (byte)Mask[b % 8];
        }

        public override ICountFingerprint GetCountFingerprint(IAtomContainer container)
        {
            throw new NotSupportedException();
        }
    }
}
