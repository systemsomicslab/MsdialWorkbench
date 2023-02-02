/* Copyright (C) 2004-2007  Matteo Floris <mfe4@users.sf.net>
 *                    2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using System.Collections.Generic;

namespace NCDK.QSAR
{
    /// <summary>
    /// This class returns the valence of an atom.
    /// </summary>
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  standard
    // @cdk.dictref valence, atom
    public static class AtomValenceTool
    {
        private static readonly Dictionary<int, int> valencesTable = new Dictionary<int, int>
        {
            { AtomicNumbers.H, 1 },
            { AtomicNumbers.He, 8 },
            { AtomicNumbers.Ne, 8 },
            { AtomicNumbers.Ar, 8 },
            { AtomicNumbers.Kr, 8 },
            { AtomicNumbers.Xe, 8 },
            { AtomicNumbers.Hg, 2 },
            { AtomicNumbers.Rn, 8 },
            { AtomicNumbers.Li, 1 },
            { AtomicNumbers.Be, 2 },
            { AtomicNumbers.B, 3 },
            { AtomicNumbers.C, 4 },
            { AtomicNumbers.N, 5 },
            { AtomicNumbers.O, 6 },
            { AtomicNumbers.F, 7 },
            { AtomicNumbers.Na, 1 },
            { AtomicNumbers.Mg, 2 },
            { AtomicNumbers.Al, 3 },
            { AtomicNumbers.Si, 4 },
            { AtomicNumbers.P, 5 },
            { AtomicNumbers.S, 6 },
            { AtomicNumbers.Cl, 7 },
            { AtomicNumbers.K, 1 },
            { AtomicNumbers.Ca, 2 },
            { AtomicNumbers.Ga, 3 },
            { AtomicNumbers.Ge, 4 },
            { AtomicNumbers.As, 5 },
            { AtomicNumbers.Se, 6 },
            { AtomicNumbers.Br, 7 },
            { AtomicNumbers.Rb, 1 },
            { AtomicNumbers.Sr, 2 },
            { AtomicNumbers.In, 3 },
            { AtomicNumbers.Sn, 4 },
            { AtomicNumbers.Sb, 5 },
            { AtomicNumbers.Te, 6 },
            { AtomicNumbers.I, 7 },
            { AtomicNumbers.Cs, 1 },
            { AtomicNumbers.Ba, 2 },
            { AtomicNumbers.Tl, 3 },
            { AtomicNumbers.Pb, 4 },
            { AtomicNumbers.Bi, 5 },
            { AtomicNumbers.Po, 6 },
            { AtomicNumbers.At, 7 },
            { AtomicNumbers.Fr, 1 },
            { AtomicNumbers.Ra, 2 },
            { AtomicNumbers.Cu, 2 },
            { AtomicNumbers.Mn, 2 },
            { AtomicNumbers.Co, 2 }
        };

        public static int GetValence(IAtom atom)
        {
            if (!valencesTable.TryGetValue(atom.AtomicNumber, out int ret))
                throw new NoSuchAtomException();
            return ret;
        }
    }
}
