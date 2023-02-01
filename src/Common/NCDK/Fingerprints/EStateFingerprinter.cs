/* Copyright (C) 2008 Rajarshi Guha <rajarshi@users.sourceforge.net>
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

using NCDK.Config.Fragments;
using NCDK.SMARTS;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// This fingerprinter generates 79 bit fingerprints using the E-State fragments.
    /// </summary>
    /// <remarks>
    /// <para>The E-State fragments are those described in <token>cdk-cite-HALL1995</token> and
    /// the SMARTS patterns were taken from
    /// <see href="http://www.rdkit.org">RDKit</see>. Note that this fingerprint simply
    /// indicates the presence or occurrence of the fragments. If you need counts
    /// of the fragments take a look at <see cref="QSAR.Descriptors.Moleculars.KierHallSmartsDescriptor"/>,
    /// which also lists the substructures corresponding to each bit position.
    /// </para>
    /// <para>This class assumes that aromaticity perception and atom typing have
    /// been performed prior to generating the fingerprint.
    /// </para>
    /// <note type="warning">
    /// ESTATE substructure keys cannot be used for substructure
    /// filtering. It is possible for some keys to match substructures and not match
    /// the superstructures. Some keys check for hydrogen counts which may not be
    /// preserved in a superstructure.
    /// </note>
    /// </remarks>
    // @author Rajarhi Guha
    // @cdk.created 2008-07-23
    // @cdk.keyword fingerprint
    // @cdk.keyword similarity
    // @cdk.keyword estate
    // @cdk.module fingerprint
    public class EStateFingerprinter : AbstractFingerprinter, IFingerprinter
    {
        private static readonly IReadOnlyList<SmartsPattern> PATTERNS = EStateFragments.Patterns;

        public EStateFingerprinter() { }

        /// <inheritdoc/>
        public override IBitFingerprint GetBitFingerprint(IAtomContainer atomContainer)
        {
            int bitsetLength = PATTERNS.Count;
            var fingerPrint = new BitArray(bitsetLength);

            SmartsPattern.Prepare(atomContainer);
            for (int i = 0; i < PATTERNS.Count; i++)
            {
                if (PATTERNS[i].Matches(atomContainer))
                    fingerPrint.Set(i, true);
            }
            return new BitSetFingerprint(fingerPrint);
        }

        /// <inheritdoc/>
        public override IReadOnlyDictionary<string, int> GetRawFingerprint(IAtomContainer mol)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int Length => PATTERNS.Count;

        /// <inheritdoc/>
        public override ICountFingerprint GetCountFingerprint(IAtomContainer container)
        {
            throw new NotSupportedException();
        }
    }
}
