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

using System.Collections;
using System.Collections.Generic;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Interface for bit fingerprint representations.
    /// </summary>
    // @author jonalv
    // @cdk.module     core
    public interface IBitFingerprint
    {
        /// <summary>
        /// The number of bits set to true in the fingerprint.
        /// </summary>
        int Cardinality { get; }

        /// <summary>
        /// The size of the fingerprint, i.e., the number of hash bins.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Performs a logical <b>AND</b> of the bits in this target bit set with
        /// the bits in the argument fingerprint. This fingerprint is modified so
        /// that each bit in it has the value <see langword="true"/> if and only if 
        /// it both initially had the value <see langword="true"/> and the
        /// corresponding bit in the fingerprint argument also had the value
        /// <see langword="true"/>.
        /// </summary>
        /// <param name="fingerprint">the fingerprint with which to perform the AND operation</param>
        /// <exception cref="System.ArgumentException">if the two fingerprints are not of same size</exception>
        void And(IBitFingerprint fingerprint);

        /// <summary>
        /// Performs a logical <b>OR</b> of the bits in this target bit set with
        /// the bits in the argument fingerprint. This operation can also be seen
        /// as merging two fingerprints. This fingerprint is modified so
        /// that each bit in it has the value <see langword="true"/> if and only if
        /// it either already had the value <see langword="true"/> or the corresponding
        /// bit in the bit set argument has the value <see langword="true"/>.
        /// </summary>        
        /// <param name="fingerprint">the fingerprint with which to perform the OR operation</param>
        /// <exception cref="System.ArgumentException">if the two fingerprints are not of same size</exception>
        void Or(IBitFingerprint fingerprint);

        /// <summary>
        /// The value of the bit with the specified index. The value
        /// is <see langword="true"/> if the bit with the index <paramref name="index"/>
        /// is currently set in this fingerprint; otherwise, the result
        /// is <see langword="false"/>.
        /// </summary>
        /// <param name="index">the index of the bit to return the value for</param>
        bool this[int index] { get; set; }

        /// <summary>
        /// Returns a <see cref="BitArray"/> representation of the fingerprint.
        /// This might take significantly more memory!
        /// </summary>
        /// <returns>the fingerprint as a <see cref="BitArray"/></returns>
        BitArray AsBitSet();

        /// <summary>
        /// Sets the bit at the specified index to true.
        /// <param name="i">index</param>
        /// </summary>
        void Set(int i);

        /// <summary>
        /// Returns a listing of the bits in the fingerprint that are set to true.
        /// </summary>
        /// <returns>listing of all bits that are set</returns>
        IEnumerable<int> GetSetBits();
    }
}
