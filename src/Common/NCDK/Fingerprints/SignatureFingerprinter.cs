/* Copyright (C) 2011  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All I ask is that proper credit is given for my work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Signatures;
using System.Collections.Generic;
using System.Globalization;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// An implementation of a <see cref="AtomSignature"/>-based fingerprint.
    /// </summary>
    // @cdk.module  signature
    // @cdk.keyword fingerprint
    public class SignatureFingerprinter : AbstractFingerprinter, IFingerprinter
    {
        private int signatureDepth;

        /// <summary>
        /// Initialize the fingerprinter with a default signature depth of 1.
        /// </summary>
        public SignatureFingerprinter()
                : this(1)
        { }

        /// <summary>
        /// Initialize the fingerprinter with a certain signature depth.
        /// </summary>
        /// <param name="depth">The depth of the signatures to calculate.</param>
        public SignatureFingerprinter(int depth)
        {
            this.signatureDepth = depth;
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetParameters()
        {
            yield return new KeyValuePair<string, string>("signatureDepth", signatureDepth.ToString(NumberFormatInfo.InvariantInfo));
            yield break;
        }

        public override IBitFingerprint GetBitFingerprint(IAtomContainer atomContainer)
        {
            return new IntArrayFingerprint(GetRawFingerprint(atomContainer));
        }

        public override IReadOnlyDictionary<string, int> GetRawFingerprint(IAtomContainer atomContainer)
        {
            var map = new Dictionary<string, int>();
            foreach (var atom in atomContainer.Atoms)
            {
                string signature = new AtomSignature(atom, signatureDepth, atomContainer).ToCanonicalString();
                if (map.ContainsKey(signature))
                {
                    map[signature] = map[signature] + 1;
                }
                else
                {
                    map[signature] = 1;
                }
            }
            return map;
        }

        public override int Length => -1;

        public override ICountFingerprint GetCountFingerprint(IAtomContainer container)
        {
            return new IntArrayCountFingerprint(GetRawFingerprint(container));
        }
    }
}
