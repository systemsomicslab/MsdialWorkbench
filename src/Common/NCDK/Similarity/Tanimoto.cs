/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;
using NCDK.Fingerprints;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Similarity
{
    /// <summary>
    ///  Calculates the Tanimoto coefficient for a given pair of two
    ///  fingerprint bitsets or real valued feature vectors.
    /// </summary>
    /// <remarks>
    /// The Tanimoto coefficient is one way to
    /// quantitatively measure the "distance" or similarity of
    /// two chemical structures.
    /// <para>You can use the FingerPrinter class to retrieve two fingerprint bitsets.
    /// We assume that you have two structures stored in cdk.Molecule objects.
    /// A tanimoto coefficient can then be calculated like:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Similarity.Tanimoto_Example.cs+1"]/*' />
    /// </para>
    /// <para>The FingerPrinter assumes that hydrogens are explicitely given, if this
    /// is desired!</para>
    /// <para>Note that the continuous Tanimoto coefficient does not lead to a metric space</para>
    /// </remarks>
    // @author steinbeck
    // @cdk.created    2005-10-19
    // @cdk.keyword    jaccard
    // @cdk.keyword    similarity, tanimoto
    // @cdk.module fingerprint
    public static class Tanimoto
    {
        /// <summary>
        /// Evaluates Tanimoto coefficient for two bit sets.
        /// </summary>
        /// <param name="bitset1">A bitset (such as a fingerprint) for the first molecule</param>
        /// <param name="bitset2">A bitset (such as a fingerprint) for the second molecule</param>
        /// <returns>The Tanimoto coefficient</returns>
        /// <exception cref="CDKException"> if bitsets are not of the same length</exception>
        public static double Calculate(BitArray bitset1, BitArray bitset2)
        {
            double _bitset1_cardinality = BitArrays.Cardinality(bitset1);
            double _bitset2_cardinality = BitArrays.Cardinality(bitset2);
            if (bitset1.Count != bitset2.Count)
            {
                throw new CDKException($"{nameof(bitset1)} and {nameof(bitset2)} must have the same bit length");
            }
            BitArray one_and_two = (BitArray)bitset1.Clone();
            one_and_two.And(bitset2);
            double _common_bit_count = BitArrays.Cardinality(one_and_two);
            return _common_bit_count / (_bitset1_cardinality + _bitset2_cardinality - _common_bit_count);
        }

        /// <summary>
        /// Evaluates Tanimoto coefficient for two <see cref="IBitFingerprint"/>.
        /// </summary>
        /// <param name="fingerprint1">fingerprint for the first molecule</param>
        /// <param name="fingerprint2">fingerprint for the second molecule</param>
        /// <returns>The Tanimoto coefficient</returns>
        /// <exception cref="ArgumentException">if bitsets are not of the same length</exception>
        public static double Calculate(IBitFingerprint fingerprint1, IBitFingerprint fingerprint2)
        {
            if (fingerprint1.Length != fingerprint2.Length)
            {
                throw new ArgumentException("Fingerprints must have the same size");
            }
            int cardinality1 = fingerprint1.Cardinality;
            int cardinality2 = fingerprint2.Cardinality;
            // If the fingerprint is an IntArrayFingeprint that could mean a big
            // fingerprint so let's take the safe way out and create a
            // new IntArrayfingerprint
            IBitFingerprint one_and_two = fingerprint1 is IntArrayFingerprint ? (IBitFingerprint)new IntArrayFingerprint(
                    fingerprint1) : (IBitFingerprint)new BitSetFingerprint(fingerprint1);
            one_and_two.And(fingerprint2);
            double cardinalityCommon = one_and_two.Cardinality;
            return cardinalityCommon / (cardinality1 + cardinality2 - cardinalityCommon);
        }

        /// <summary>
        /// Evaluates the continuous Tanimoto coefficient for two real valued vectors.
        /// </summary>
        /// <param name="features1">The first feature vector</param>
        /// <param name="features2">The second feature vector</param>
        /// <returns>The continuous Tanimoto coefficient</returns>
        /// <exception cref="CDKException"> if the features are not of the same length</exception>
        public static double Calculate(double[] features1, double[] features2)
        {
            if (features1.Length != features2.Length)
            {
                throw new CDKException("Features vectors must be of the same length");
            }

            int n = features1.Length;
            double ab = 0.0;
            double a2 = 0.0;
            double b2 = 0.0;

            for (int i = 0; i < n; i++)
            {
                ab += features1[i] * features2[i];
                a2 += features1[i] * features1[i];
                b2 += features2[i] * features2[i];
            }
            return ab / (a2 + b2 - ab);
        }

        /// <summary>
        /// Evaluate continuous Tanimoto coefficient for two feature, count fingerprint representations.
        /// </summary>
        /// <remarks>
        /// Note that feature/count type fingerprints may be of different length.
        /// Uses Tanimoto method from 10.1021/ci800326z
        /// </remarks>
        /// <param name="features1">The first feature map</param>
        /// <param name="features2">The second feature map</param>
        /// <returns>The Tanimoto coefficient</returns>
        public static double Calculate(IReadOnlyDictionary<string, int> features1, IReadOnlyDictionary<string, int> features2)
        {
            var common = features1.Keys.Intersect(features2.Keys);
            double xy = 0, x = 0, y = 0;
            foreach (string s in common)
            {
                int c1 = features1[s], c2 = features2[s];
                xy += c1 * c2;
            }
            foreach (var c in features1.Values)
            {
                x += c * c;
            }
            foreach (var c in features2.Values)
            {
                y += c * c;
            }
            return xy / (x + y - xy);
        }

        /// <summary>
        /// Evaluate continuous Tanimoto coefficient for two feature, count fingerprint representations.
        /// </summary>
        /// <remarks>
        /// Note that feature/count type fingerprints may be of different length.
        /// Uses Tanimoto method from 10.1021/ci800326z
        /// </remarks>
        /// <param name="fp1">The first fingerprint</param>
        /// <param name="fp2">The second fingerprint</param>
        /// <returns>The Tanimoto coefficient</returns>
        /// <seealso cref="Method1(ICountFingerprint, ICountFingerprint)"/>
        /// <seealso cref="Method2(ICountFingerprint, ICountFingerprint)"/>
        public static double Calculate(ICountFingerprint fp1, ICountFingerprint fp2)
        {
            return Method2(fp1, fp2);
        }

        /// <summary>
        /// Calculates Tanimoto distance for two count fingerprints using method 1.
        /// </summary>
        /// <remarks>
        /// The feature/count type fingerprints may be of different length.
        /// Uses Tanimoto method from <token>cdk-cite-Steffen09</token>.
        /// </remarks>
        /// <param name="fp1">count fingerprint 1</param>
        /// <param name="fp2">count fingerprint 2</param>
        /// <returns>a Tanimoto distance</returns>
        public static double Method1(ICountFingerprint fp1, ICountFingerprint fp2)
        {
            long xy = 0, x = 0, y = 0;
            for (int i = 0; i < fp1.GetNumberOfPopulatedBins(); i++)
            {
                int hash = fp1.GetHash(i);
                for (int j = 0; j < fp2.GetNumberOfPopulatedBins(); j++)
                {
                    if (hash == fp2.GetHash(j))
                    {
                        xy += fp1.GetCount(i) * fp2.GetCount(j);
                    }
                }
                x += fp1.GetCount(i) * fp1.GetCount(i);
            }
            for (int j = 0; j < fp2.GetNumberOfPopulatedBins(); j++)
            {
                y += fp2.GetCount(j) * fp2.GetCount(j);
            }
            return ((double)xy / (x + y - xy));
        }

        /// <summary>
        /// Calculates Tanimoto distance for two count fingerprints using method 2.
        /// </summary>
        /// <remarks>
        /// <token>cdk-cite-Grant06</token>.
        /// </remarks>
        /// <param name="fp1">count fingerprint 1</param>
        /// <param name="fp2">count fingerprint 2</param>
        /// <returns>a Tanimoto distance</returns>
        public static double Method2(ICountFingerprint fp1, ICountFingerprint fp2)
        {
            long maxSum = 0, minSum = 0;
            int i = 0, j = 0;
            while (i < fp1.GetNumberOfPopulatedBins() || j < fp2.GetNumberOfPopulatedBins())
            {
                int? hash1 = i < fp1.GetNumberOfPopulatedBins() ? fp1.GetHash(i) : (int?)null;
                int? hash2 = j < fp2.GetNumberOfPopulatedBins() ? fp2.GetHash(j) : (int?)null;
                int? count1 = i < fp1.GetNumberOfPopulatedBins() ? fp1.GetCount(i) : (int?)null;
                int? count2 = j < fp2.GetNumberOfPopulatedBins() ? fp2.GetCount(j) : (int?)null;

                if (count2 == null || (hash1 != null && hash1 < hash2))
                {
                    maxSum += count1.Value;
                    i++;
                    continue;
                }
                if (count1 == null || (hash2 != null && hash1 > hash2))
                {
                    maxSum += count2.Value;
                    j++;
                    continue;
                }

                if (hash1.Equals(hash2))
                {
                    maxSum += Math.Max(count1.Value, count2.Value);
                    minSum += Math.Min(count1.Value, count2.Value);
                    i++;
                    j++;
                }
            }
            return ((double)minSum) / maxSum;
        }
    }
}
