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

using NCDK.Common.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Fingerprints
{
    // @author jonalv
    // @cdk.module     standard
    public class BitSetFingerprint : IBitFingerprint
    {
        private BitArray bitset;

        public BitSetFingerprint(BitArray bitset)
        {
            this.bitset = bitset;
        }

        public BitSetFingerprint()
        {
            bitset = new BitArray(0);
        }

        public BitSetFingerprint(int size)
        {
            bitset = new BitArray(size);
        }

        public BitSetFingerprint(IBitFingerprint fingerprint)
        {
            if (fingerprint is BitSetFingerprint)
            {
                bitset = (BitArray)((BitSetFingerprint)fingerprint).bitset.Clone();
            }
            else
            {
                var bitSet = new BitArray((int)fingerprint.Length);
                for (int i = 0; i < fingerprint.Length; i++)
                {
                    bitSet.Set(i, fingerprint[i]);
                }
                this.bitset = bitSet;
            }
        }

        public int Cardinality => BitArrays.Cardinality(bitset);

        public long Length => bitset.Count;

        public void And(IBitFingerprint fingerprint)
        {
            if (bitset.Count != fingerprint.Length)
            {
                throw new ArgumentException("Fingerprints must have same size");
            }
            if (fingerprint is BitSetFingerprint)
            {
                bitset.And(((BitSetFingerprint)fingerprint).bitset);
            }
            else
            {
                for (int i = 0; i < bitset.Count; i++)
                {
                    bitset.Set(i, bitset[i] && fingerprint[i]);
                }
            }
        }

        public void Or(IBitFingerprint fingerprint)
        {
            if (bitset.Count != fingerprint.Length)
            {
                throw new ArgumentException("Fingerprints must have same size");
            }
            if (fingerprint is BitSetFingerprint)
            {
                bitset.Or(((BitSetFingerprint)fingerprint).bitset);
            }
            else
            {
                for (int i = 0; i < bitset.Count; i++)
                {
                    bitset.Set(i, bitset[i] || fingerprint[i]);
                }
            }
        }

        public bool this[int index]
        {
            get
            {
                return BitArrays.GetValue(bitset, index);
            }
            set
            {
                BitArrays.SetValue(bitset, index, value);
            }
        }

        public BitArray AsBitSet() => (BitArray)bitset.Clone();

        public void Set(int i)
        {
            BitArrays.SetValue(bitset, i, true);
        }

        public override int GetHashCode()
        {
            return BitArrays.GetHashCode(bitset);
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
                return true;
            if (obj == null)
                return false;
            if (GetType() != obj.GetType())
                return false;
            var other = (BitSetFingerprint)obj;
            if (bitset == null)
            {
                if (other.bitset != null)
                    return false;
            }
            else if (!BitArrays.Equals(bitset, other.bitset))
                return false;
            return true;
        }

        public IEnumerable<int> GetSetBits()
        {
            for (int i = 0; i < bitset.Length; i++)
            {
                if (bitset[i])
                    yield return i;
            }
            yield break;
        }
    }
}
