/*
 * Copyright (c) 2017 John Mayfield <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System.Collections.Generic;
using System.Linq;

namespace NCDK.Stereo
{
    /// <summary>
    /// Restricted axial rotation around Aryl-Aryl bonds.
    /// </summary>
    /// <remarks>
    /// The atropisomer is
    /// stored in a similar manner to <see cref="ExtendedTetrahedral"/> (and
    /// <see cref="TetrahedralChirality"/>) except instead of storing the central atom
    /// we store the sigma bond around which the rotation is restricted and the
    /// four carriers are connect to either end atom of the 'focus' bond.
    /// <para>
    /// <pre>
    ///      a     b'
    ///     /       \
    ///    Ar --f-- Ar
    ///     \      /
    ///      a'   b
    /// f: focus
    /// Ar: Aryl (carriers connected to either end of 'f')
    /// a,a',b,b': ortho substituted on the Aryl
    /// </pre>
    /// </para>
    /// <para>
    /// Typical examples include <see href="https://en.wikipedia.org/wiki/BINOL">
    /// BiNOL</see>, and <see href="https://en.wikipedia.org/wiki/BINAP">BiNAP</see>.
    /// </para>
    /// </remarks>
    /// <seealso href="http://opensmiles.org/opensmiles.html#_octahedral_centers">Octahedral Centers, OpenSMILES</seealso>
    public class Atropisomeric
        : AbstractStereo<IBond, IAtom>
    {
        /// <summary>
        /// Define a new atropisomer using the focus bond and the carrier atoms.
        /// </summary>
        /// <param name="focus">the focus bond</param>
        /// <param name="carriers">the carriers</param>
        /// <param name="value">the configuration <see cref="StereoConfigurations.Left"/> or <see cref="StereoConfigurations.Right"/></param>
        public Atropisomeric(IBond focus, IEnumerable<IAtom> carriers, StereoConfigurations value)
            : base(focus, carriers.ToReadOnlyList(), new StereoElement(StereoClass.Atropisomeric, value))
        {
        }

        /// <inheritdoc/>
        protected override IStereoElement<IBond, IAtom> Create(IBond focus, IReadOnlyList<IAtom> carriers, StereoElement stereo)
        {
            return new Atropisomeric(focus, carriers.ToArray(), stereo.Configuration);
        }
    }
}
