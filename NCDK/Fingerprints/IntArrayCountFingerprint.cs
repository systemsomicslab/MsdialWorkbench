/* Copyright (C) 2011  Jonathan Alvarsson <jonalv@users.sf.net>
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

using System;
using System.Collections.Generic;

namespace NCDK.Fingerprints
{
    // @author jonalv
    // @cdk.module     standard
    public class IntArrayCountFingerprint : ICountFingerprint
    {
        internal int[] hitHashes;
        internal int[] numOfHits;
        private bool behaveAsBitFingerprint;

        public IntArrayCountFingerprint()
        {
            hitHashes = Array.Empty<int>();
            numOfHits = Array.Empty<int>();
            behaveAsBitFingerprint = false;
        }

        public IntArrayCountFingerprint(IReadOnlyDictionary<string, int> rawFingerprint)
        {
            var hashedFP = new Dictionary<int, int>();
            foreach (var key in rawFingerprint.Keys)
            {
                int hashedKey = key.GetHashCode();
                if (!hashedFP.TryGetValue(hashedKey, out int count))
                    count = 0;
                hashedFP.Add(hashedKey, count + rawFingerprint[key]);
            }
            var keys = new List<int>(hashedFP.Keys);
            keys.Sort();
            hitHashes = new int[keys.Count];
            numOfHits = new int[keys.Count];
            int i = 0;
            foreach (var key in keys)
            {
                hitHashes[i] = key;
                numOfHits[i] = hashedFP[key];
                i++;
            }
        }

        /// <summary>
        /// Create an <see cref="IntArrayCountFingerprint"/> from a rawFingerprint
        /// and if <paramref name="behaveAsBitFingerprint"/> make it only return 0 or 1
        /// as count thus behaving like a bit finger print.
        /// </summary>
        /// <param name="rawFingerprint">the raw fp</param>
        /// <param name="behaveAsBitFingerprint">whether to behave as binary fp or not</param>
        public IntArrayCountFingerprint(IReadOnlyDictionary<string, int> rawFingerprint, bool behaveAsBitFingerprint)
            : this(rawFingerprint)
        {
            this.behaveAsBitFingerprint = behaveAsBitFingerprint;
        }

        public long Length => 4294967296L;

        public int GetCount(int index)
        {
            if (behaveAsBitFingerprint)
            {
                return numOfHits[index] == 0 ? 0 : 1;
            }
            return numOfHits[index];
        }

        public int GetHash(int index)
        {
            return hitHashes[index];
        }

        public int GetNumberOfPopulatedBins()
        {
            return hitHashes.Length;
        }

        public void Merge(ICountFingerprint fp)
        {
            var newFp = new Dictionary<int, int>();
            {
                for (int i = 0; i < hitHashes.Length; i++)
                {
                    newFp.Add(hitHashes[i], numOfHits[i]);
                }
            }
            {
                for (int i = 0; i < fp.GetNumberOfPopulatedBins(); i++)
                {
                    if (!newFp.TryGetValue(fp.GetHash(i), out int count))
                        count = 0;
                    newFp[fp.GetHash(i)] = count + fp.GetCount(i);
                }
            }
            var keys = new List<int>(newFp.Keys);
            keys.Sort();
            hitHashes = new int[keys.Count];
            numOfHits = new int[keys.Count];
            {
                int i = 0;
                foreach (var key in keys)
                {
                    hitHashes[i] = key;
                    numOfHits[i++] = newFp[key];
                }
            }
        }

        public void SetBehaveAsBitFingerprint(bool behaveAsBitFingerprint)
        {
            this.behaveAsBitFingerprint = behaveAsBitFingerprint;
        }

        public bool HasHash(int hash)
        {
            return Array.BinarySearch(hitHashes, hash) >= 0;
        }

        public int GetCountForHash(int hash)
        {
            int index = Array.BinarySearch(hitHashes, hash);
            if (index >= 0)
            {
                return numOfHits[index];
            }
            return 0;
        }
    }
}
