/* Copyright (C) 2002-2007  Egon Willighagen <egonw@users.sf.net>
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

namespace NCDK.Fingerprints
{
    /// <summary>
    /// Specialized version of the <see cref="Fingerprinter"/> which does not take bond orders
    /// into account.
    /// </summary>
    /// <seealso cref="Fingerprinter"/>
    // @author         egonw
    // @cdk.created    2007-01-11
    // @cdk.keyword    fingerprint
    // @cdk.keyword    similarity
    // @cdk.module     standard
    public class GraphOnlyFingerprinter : Fingerprinter
    {
        /// <summary>
        /// Creates a fingerprint generator of length <see cref="Fingerprinter.DefaultSize"/>
        /// and with a search depth of <see cref="Fingerprinter.DefaultSearchDepth"/>.
        /// </summary>
        public GraphOnlyFingerprinter()
            : base(DefaultSize, DefaultSearchDepth)
        { }

        public GraphOnlyFingerprinter(int size)
            : base(size, DefaultSearchDepth)
        { }

        public GraphOnlyFingerprinter(int size, int searchDepth)
                : base(size, searchDepth)
        { }

        /// <summary>
        /// Gets the bondSymbol attribute of the Fingerprinter class. Because we do
        /// not consider bond orders to be important, we just return "";
        /// </summary>
        /// <param name="bond">Description of the Parameter</param>
        /// <returns>The bondSymbol value</returns>
        protected override string GetBondSymbol(IBond bond)
        {
            return "";
        }

        public BitArray GetBitFingerprint(IAtomContainer container, int size)
        {
            BitArray bitSet = new BitArray(size);
            EncodePaths(container, base.SearchDepth, bitSet, size);
            return bitSet;
        }
    }
}
