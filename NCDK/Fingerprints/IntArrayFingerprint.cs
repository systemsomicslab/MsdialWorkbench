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

using NCDK.Common.Base;
using NCDK.Common.Collections;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Fingerprints
{
    // @author jonalv
    // @cdk.module     standard
    public class IntArrayFingerprint : IBitFingerprint
    {
        private volatile int[] trueBits;

        public IntArrayFingerprint(IReadOnlyDictionary<string, int> rawFingerPrint)
        {
            trueBits = new int[rawFingerPrint.Count];
            int i = 0;
            foreach (var key in rawFingerPrint.Keys)
            {
                trueBits[i++] = key.GetHashCode();
            }
            Array.Sort(trueBits);
        }

        public IntArrayFingerprint(int[] setBits)
        {
            this.trueBits = setBits;
        }

        public IntArrayFingerprint()
        {
            trueBits = Array.Empty<int>();
        }

        public IntArrayFingerprint(IBitFingerprint fingerprint)
        {
            // if it is an IntArrayFingerprint we can do faster (Array.Copy)
            if (fingerprint is IntArrayFingerprint iaFP)
            {
                trueBits = new int[iaFP.trueBits.Length];
                Array.Copy(iaFP.trueBits, 0, trueBits, 0, trueBits.Length);
            }
            else
            {
                trueBits = new int[fingerprint.Cardinality];
                int index = 0;
                for (int i = 0; i < fingerprint.Length; i++)
                {
                    if (fingerprint[i])
                    {
                        trueBits[index++] = i;
                    }
                }
            }
        }

        public int Cardinality => trueBits.Length;

        public long Length => 4294967296L;

        public void And(IBitFingerprint fingerprint)
        {
            if (fingerprint is IntArrayFingerprint)
            {
                And((IntArrayFingerprint)fingerprint);
            }
            else
            {
                //TODO add support for this?
                throw new NotSupportedException("AND on IntArrayFingerPrint only supported for other "
                        + "IntArrayFingerPrints for the moment");
            }
        }

        public void And(IntArrayFingerprint fingerprint)
        {
            List<int> tmp = new List<int>();
            int i = 0;
            int j = 0;
            while (i < trueBits.Length && j < fingerprint.trueBits.Length)
            {
                int local = trueBits[i];
                int remote = fingerprint.trueBits[j];
                if (local == remote)
                {
                    tmp.Add(local);
                    i++;
                    j++;
                }
                else if (local < remote)
                {
                    i++;
                }
                else
                {
                    j++;
                }
            }
            trueBits = new int[tmp.Count];
            i = 0;
            foreach (var t in tmp)
            {
                trueBits[i] = t;
            }
            Array.Sort(trueBits);
        }


        public void Or(IBitFingerprint fingerprint)
        {
            if (fingerprint is IntArrayFingerprint)
            {
                Or((IntArrayFingerprint)fingerprint);
            }
            else
            {
                //TODO add support for this?
                throw new NotSupportedException("OR on IntArrayFingerPrint only supported for other IntArrayFingerPrints for the moment");
            }
        }

        public void Or(IntArrayFingerprint fingerprint)
        {
            var tmp = new HashSet<int>();
            {
                foreach (var trueBit in trueBits)
                {
                    tmp.Add(trueBit);
                }
            }
            {
                for (int i = 0; i < fingerprint.trueBits.Length; i++)
                {
                    tmp.Add(fingerprint.trueBits[i]);
                }
            }
            trueBits = new int[tmp.Count];
            {
                int i = 0;
                foreach (var t in tmp)
                {
                    trueBits[i++] = t;
                }
            }
            Array.Sort(trueBits);
        }

       public bool this[int index]
        {
            get
            {
                return (Array.BinarySearch(trueBits, index) >= 0);
            }

            // This method is VERY INNEFICIENT when called multiple times. It is the
            // cost of keeping down the memory footprint. Avoid using it for building up
            // IntArrayFingerprints -- instead use the constructor taking a so called
            // raw fingerprint.
            set
            {
                int i = Array.BinarySearch(trueBits, index);
                // bit at index is set to true and shall be set to false
                if (i >= 0 && !value)
                {
                    int[] tmp = new int[trueBits.Length - 1];
                    Array.Copy(trueBits, 0, tmp, 0, i);
                    Array.Copy(trueBits, i + 1, tmp, i, trueBits.Length - i - 1);
                    trueBits = tmp;
                }
                // bit at index is set to false and shall be set to true
                else if (i < 0 && value)
                {
                    int[] tmp = new int[trueBits.Length + 1];
                    Array.Copy(trueBits, 0, tmp, 0, trueBits.Length);
                    tmp[tmp.Length - 1] = index;
                    trueBits = tmp;
                    Array.Sort(trueBits);
                }
            }
        }

        public BitArray AsBitSet()
        {
            //TODO support this?
            throw new NotSupportedException();
        }

        public void Set(int i)
        {
            this[i] = true;
        }

        public override int GetHashCode()
        {
            return Arrays.GetHashCode(trueBits);
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null) return false;
            if (GetType() != obj.GetType()) return false;
            IntArrayFingerprint other = (IntArrayFingerprint)obj;
            if (!Compares.AreEqual(trueBits, other.trueBits)) return false;
            return true;
        }

        public IEnumerable<int> GetSetBits()
        {
            return trueBits;
        }
    }
}
