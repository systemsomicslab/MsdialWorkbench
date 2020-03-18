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

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Interface for count fingerprint representations. The fingerprint is
    /// regarded as a list of hashes and a list of counts where the the list of
    /// counts keeps track of how many times the corresponding hash is found in
    /// the fingerprint. So index refers to position in the list. The list must
    /// be sorted in natural order (ascending).
    /// </summary>
    // @author jonalv
    // @cdk.module     core
    public interface ICountFingerprint
    {
        /// <summary>
        /// The number of bits of this fingerprint.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Returns the number of bins that are populated. This number is typically smaller
        /// than the total number of bins.
        /// </summary>
        /// <returns>the number of populated bins</returns>
        /// <see cref="Length"/>
        int GetNumberOfPopulatedBins();

        /// <summary>
        /// Returns the count value for the bin with the given index.
        /// </summary>
        /// <param name="index">the index of the bin to return the number of hits for.</param>
        /// <returns>the count for the bin with given index.</returns>
        int GetCount(int index);

        /// <summary>
        /// Returns the hash corresponding to the given index in the fingerprint.
        /// </summary>
        /// <param name="index">the index of the bin to return the hash for.</param>
        /// <returns>the hash for the bin with the given index.</returns>
        int GetHash(int index);

        /// <summary>
        /// Merge all from <paramref name="fp"/> into the current fingerprint.
        /// </summary>
        /// <param name="fp">to be merged</param>
        void Merge(ICountFingerprint fp);

        /// <summary>
        /// Changes behaviour, if true is given the count fingerprint will
        /// behave as a bit fingerprint and return 0 or 1 for counts.
        /// </summary>
        /// <param name="behaveAsBitFingerprint"></param>
        void SetBehaveAsBitFingerprint(bool behaveAsBitFingerprint);

        /// <summary>
        /// Whether the fingerprint contains the given hash.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns>true if the fingerprint contains the given hash, otherwise false.</returns>
        bool HasHash(int hash);

        /// <summary>
        /// Get the number of times a certain hash exists in the fingerprint.
        /// </summary>
        /// <param name="hash"></param>
        /// <returns>the number associated with the given hash</returns>
        int GetCountForHash(int hash);
    }
}
