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

using NCDK.Common.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Tool with helper methods for IFingerprint.
    /// </summary>
    // @author         steinbeck
    // @cdk.created    2002-02-24
    // @cdk.keyword    fingerprint
    // @cdk.module     standard
    public static class FingerprinterTool
    {
        /// <summary>
        /// Checks whether all the positive bits in BitArray bs2 occur in BitArray bs1. If
        /// so, the molecular structure from which bs2 was generated is a possible
        /// substructure of bs1. 
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Fingerprints.FingerprinterTool_Example.cs+IsSubset"]/*' />
        /// </example>
        /// <param name="bs1">The reference <see cref="BitArray"/></param>
        /// <param name="bs2">The <see cref="BitArray"/> which is compared with bs1</param>
        /// <returns>True, if bs2 is a subset of bs1</returns>
        // @cdk.keyword    substructure search
        public static bool IsSubset(BitArray bs1, BitArray bs2)
        {
            BitArray clone = (BitArray)bs1.Clone();
            BitArrays.And(clone, bs2);
            if (BitArrays.Equals(clone, bs2))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// This lists all bits set in bs2 and not in bs2 (other way round not considered) in a list and to logger.
        /// See. <see cref="Differences(BitArray, BitArray)"/> for a method to list all differences,
        /// including those missing present in bs2 but not bs1.
        /// </summary>
        /// <param name="bs1">First bitset</param>
        /// <param name="bs2">Second bitset</param>
        /// <returns>An arrayList of Integers</returns>
        /// <seealso cref="Differences(BitArray, BitArray)"/>
        public static IReadOnlyList<int> ListDifferences(BitArray bs1, BitArray bs2)
        {
            var u = (BitArray)bs1.Clone();
            var v = (BitArray)bs2.Clone();
            var len = Math.Max(u.Length, v.Length);
            if (u.Length < len) u.Length = len;
            if (v.Length < len) v.Length = len;

            var l = new List<int>();
            Debug.WriteLine("Listing bit positions set in bs2 but not in bs1");
            for (int f = 0; f < v.Count; f++)
            {
                if (v[f] && !u[f])
                {
                    l.Add(f);
                    Debug.WriteLine("Bit " + f + " not set in bs1");
                }
            }
            return l;
        }

        /// <summary>
        /// List all differences between the two bit vectors. Unlike 
        /// <see cref="ListDifferences(BitArray, BitArray)"/> which only list
        /// those which are set in <paramref name="s"/> but not in <paramref name="t"/>.
        /// </summary>
        /// <param name="s">a bit vector</param>
        /// <param name="t">another bit vector</param>
        /// <returns>all differences between <paramref name="s"/> and <paramref name="t"/></returns>
        public static IReadOnlyCollection<int> Differences(BitArray s, BitArray t)
        {
            var u = (BitArray)s.Clone();
            var v = (BitArray)t.Clone();
            var len = Math.Max(u.Length, v.Length);
            if (u.Length < len) u.Length = len;
            if (v.Length < len) v.Length = len;
            u.Xor(v);

            var differences = new SortedSet<int>();

            for (int i = BitArrays.NextSetBit(u, 0); i >= 0; i = BitArrays.NextSetBit(u, i + 1))
            {
                differences.Add(i);
            }

            return differences;
        }

        /// <summary>
        /// Convert a mapping of features and their counts to a 1024-bit binary fingerprint. A single 
        /// bit is set for each pattern.
        /// </summary>
        /// <param name="features">features to include</param>
        /// <returns>the continuous fingerprint</returns>
        /// <seealso cref="MakeBitFingerprint(IReadOnlyDictionary{string, int}, int, int)"/>
        public static IBitFingerprint MakeBitFingerprint(IReadOnlyDictionary<string, int> features)
        {
            return MakeBitFingerprint(features, 1024, 1);
        }

        /// <summary>
        /// Convert a mapping of features and their counts to a binary fingerprint. A single bit is
        /// set for each pattern.
        /// </summary>
        /// <param name="features">features to include</param>
        /// <param name="len">fingerprint length</param>
        /// <returns>the continuous fingerprint</returns>
        /// <seealso cref="MakeBitFingerprint(IReadOnlyDictionary{string, int}, int, int)"/>
        public static IBitFingerprint MakeBitFingerprint(IReadOnlyDictionary<string, int> features, int len)
        {
            return MakeBitFingerprint(features, len, 1);
        }

        /// <summary>
        /// Convert a mapping of features and their counts to a binary fingerprint. Each feature
        /// can set 1-n hashes, the amount is modified by the <paramref name="bits"/> operand.
        /// </summary>
        /// <param name="features">features to include</param>
        /// <param name="len">fingerprint length</param>
        /// <param name="bits">number of bits to set for each pattern</param>
        /// <returns>the continuous fingerprint</returns>
        public static IBitFingerprint MakeBitFingerprint(IReadOnlyDictionary<string, int> features, int len, int bits)
        {
            var fingerprint = new BitSetFingerprint(len);
            foreach (var feature in features.Keys)
            {
                int hash = feature.GetHashCode();
                fingerprint.Set((int)((uint)hash % (uint)len));
                for (int i = 1; i < bits; i++)
                {
                    var rand = new Random(hash);
                    fingerprint.Set(hash = rand.Next(len));
                }
            }
            return fingerprint;
        }

        /// <summary>
        /// Wrap a mapping of features and their counts to a continuous (count based) fingerprint.
        /// </summary>
        /// <param name="features">features to include</param>
        /// <returns>the continuous fingerprint</returns>
        public static ICountFingerprint MakeCountFingerprint(IReadOnlyDictionary<string, int> features)
        {
            return new IntArrayCountFingerprint(features);
        }
    }
}
