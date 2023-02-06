/*
 * Copyright (c) 2018 John Mayfield <jwmay@users.sf.net>
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
    /// Extended Cis/Trans double bond configuration.
    /// </summary>
    /// <remarks>This stereo element is
    /// used to represent configurations of odd numbers of double bonds:
    /// <pre>
    ///                  C
    ///                 /
    ///   C = C = C = C
    ///  /
    /// C
    /// </pre>
    /// </remarks>
    /// <see cref="ExtendedTetrahedral"/>
    // @author John Mayfield
    public sealed class ExtendedCisTrans
        : AbstractStereo<IBond, IBond>
    {

        public ExtendedCisTrans(IBond focus, IReadOnlyList<IBond> peripherals, StereoConfigurations config)
                : base(focus, peripherals, new StereoElement(StereoClass.Cumulene, config))
        { }

        // internal, find a neighbor connected to 'atom' that is not 'other'
        private static IAtom GetOtherAtom(IAtomContainer mol, IAtom atom, IAtom other)
        {
            var bonds = mol.GetConnectedBonds(atom).ToReadOnlyList();
            if (bonds.Count != 2)
                return null;
            if (bonds[0].Contains(other))
                return bonds[1].Order == BondOrder.Double
                     ? bonds[1].GetOther(atom) : null;
            return bonds[0].Order == BondOrder.Double
                     ? bonds[0].GetOther(atom) : null;
        }

        /// <summary>
        /// Helper method to locate two terminal atoms in a container for this
        /// extended Cis/Trans element. The atoms are ordered such that the first
        /// atom is closer to first carrier.
        /// </summary>
        /// <param name="container">structure representation</param>
        /// <param name="focus"></param>
        /// <returns>the terminal atoms (ordered)</returns>
        public static IAtom[] FindTerminalAtoms(IAtomContainer container, IBond focus)
        {
            var a = focus.Begin;
            var b = focus.End;
            var aPrev = a;
            var bPrev = b;
            IAtom aNext, bNext;
            aNext = GetOtherAtom(container, a, b);
            bNext = GetOtherAtom(container, b, a);
            while (aNext != null && bNext != null)
            {
                var tmp = GetOtherAtom(container, aNext, aPrev);
                aPrev = aNext;
                aNext = tmp;
                tmp = GetOtherAtom(container, bNext, bPrev);
                bPrev = bNext;
                bNext = tmp;
            }
            if (aPrev != null && bPrev != null)
                return new IAtom[] { aPrev, bPrev };
            return null;
        }


        /// <inheritdoc/>
        protected override IStereoElement<IBond, IBond> Create(IBond focus, IReadOnlyList<IBond> carriers, StereoElement stereo)
        {
            return new ExtendedCisTrans(focus, carriers, stereo.Configuration);
        }
    }
}
