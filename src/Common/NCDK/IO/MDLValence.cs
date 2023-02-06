/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
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
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 *
 * Additionally the 'MDLValence' method has the following licence/copyright.
 *
 * Copyright (C) 2012 NextMove Software
 *
 * @@ All Rights Reserved @@ This file is part of the RDKit. The contents
 * are covered by the terms of the BSD license which is included in the file
 * license.txt, found at the root of the RDKit source tree.
 */

using System.Collections.Generic;

namespace NCDK.IO
{
    /// <summary>
    /// Adds implicit hydrogens and specifies valency using the MDL valence model.
    /// </summary>
    /// <seealso href="http://nextmovesoftware.com/blog/2013/02/27/explicit-and-implicit-hydrogens-taking-liberties-with-valence/">Explicit and Implicit Hydrogens: taking liberties with valence</seealso>
    // @author John May
    // @cdk.module io
    internal static class MDLValence
    {
        /// <summary>
        /// Apply the MDL valence model to the provided atom container.
        /// </summary>
        /// <param name="container">an atom container loaded from an MDL format</param>
        /// <returns>the container (for convenience)</returns>
        public static IAtomContainer Apply(IAtomContainer container)
        {
            var n = container.Atoms.Count;

            var valences = new int[n];

            var atomToIndex = new Dictionary<IAtom, int>(n);
            foreach (var atom in container.Atoms)
                atomToIndex[atom] = atomToIndex.Count;

            // compute the bond order sums
            foreach (var bond in container.Bonds)
            {
                int u = atomToIndex[bond.Begin];
                int v = atomToIndex[bond.End];

                int bondOrder = bond.Order.Numeric();

                valences[u] += bondOrder;
                valences[v] += bondOrder;
            }

            for (int i = 0; i < n; i++)
            {
                var atom = container.Atoms[i];
                var charge = atom.FormalCharge;
                var element = atom.AtomicNumber;

                var explicit_ = valences[i];

                // if there was a valence read from the mol file use that otherwise
                // use the default value from the valence model to set the correct
                // number of implied hydrogens
                if (atom.Valency != null)
                {
                    atom.ImplicitHydrogenCount = atom.Valency - explicit_;
                }
                else
                {
                    var implicit_ = ImplicitValence(element, charge ?? 0, valences[i]);
                    atom.ImplicitHydrogenCount = implicit_ - explicit_;
                    atom.Valency = implicit_;
                }
            }

            return container;
        }

        /// <summary>
        /// Given an element (atomic number) its charge and the explicit valence
        /// (bond order sum) return the implicit valence for that atom. This valence
        /// is from the MDL valence model which was decoded by NextMove Software and
        /// licenced as below.
        /// </summary>
        /// <remarks>
        /// $Id: MDLValence.h 2288 2012-11-26 03:39:27Z glandrum $
        ///
        /// Copyright (C) 2012 NextMove Software
        ///
        /// @@ All Rights Reserved @@ This file is part of the RDKit. The contents
        /// are covered by the terms of the BSD license which is included in the file
        /// license.txt, found at the root of the RDKit source tree.
        /// </remarks>
        /// <seealso href="http://nextmovesoftware.com/blog/2013/02/27/explicit-and-implicit-hydrogens-taking-liberties-with-valence/">Explicit and Implicit Hydrogens taking liberties with valence</seealso>
        public static int ImplicitValence(int elem, int q, int val)
        {
            switch (elem)
            {
                case 1: // H
                case 3: // Li
                case 11: // Na
                case 19: // K
                case 37: // Rb
                case 55: // Cs
                case 87: // Fr
                    if (q == 0 && val <= 1) return 1;
                    break;

                case 4: // Be
                case 12: // Mg
                case 20: // Ca
                case 38: // Sr
                case 56: // Ba
                case 88: // Ra
                    switch (q)
                    {
                        case 0:
                            if (val <= 2) return 2;
                            break;
                        case 1:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 5: // B
                    switch (q)
                    {
                        case -4:
                            if (val <= 1) return 1;
                            break;
                        case -3:
                            if (val <= 2) return 2;
                            break;
                        case -2:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case -1:
                            if (val <= 4) return 4;
                            break;
                        case 0:
                            if (val <= 3) return 3;
                            break;
                        case 1:
                            if (val <= 2) return 2;
                            break;
                        case 2:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 6: // C
                    switch (q)
                    {
                        case -3:
                            if (val <= 1) return 1;
                            break;
                        case -2:
                            if (val <= 2) return 2;
                            break;
                        case -1:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 0:
                            if (val <= 4) return 4;
                            break;
                        case 1:
                            if (val <= 3) return 3;
                            break;
                        case 2:
                            if (val <= 2) return 2;
                            break;
                        case 3:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 7: // N
                    switch (q)
                    {
                        case -2:
                            if (val <= 1) return 1;
                            break;
                        case -1:
                            if (val <= 2) return 2;
                            break;
                        case 0:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 1:
                            if (val <= 4) return 4;
                            break;
                        case 2:
                            if (val <= 3) return 3;
                            break;
                        case 3:
                            if (val <= 2) return 2;
                            break;
                        case 4:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 8: // O
                    switch (q)
                    {
                        case -1:
                            if (val <= 1) return 1;
                            break;
                        case 0:
                            if (val <= 2) return 2;
                            break;
                        case 1:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 2:
                            if (val <= 4) return 4;
                            break;
                        case 3:
                            if (val <= 3) return 3;
                            break;
                        case 4:
                            if (val <= 2) return 2;
                            break;
                        case 5:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 9: // F
                    switch (q)
                    {
                        case 0:
                            if (val <= 1) return 1;
                            break;
                        case 1:
                            if (val <= 2) return 2;
                            break;
                        case 2:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 3:
                            if (val <= 4) return 4;
                            break;
                        case 4:
                            if (val <= 3) return 3;
                            break;
                        case 5:
                            if (val <= 2) return 2;
                            break;
                        case 6:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 13: // Al
                    switch (q)
                    {
                        case -4:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -3:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case -2:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case -1:
                            if (val <= 4) return 4;
                            break;
                        case 0:
                            if (val <= 3) return 3;
                            break;
                        case 1:
                            if (val <= 2) return 2;
                            break;
                        case 2:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 14: // Si
                    switch (q)
                    {
                        case -3:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -2:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case -1:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 0:
                            if (val <= 4) return 4;
                            break;
                        case 1:
                            if (val <= 3) return 3;
                            break;
                        case 2:
                            if (val <= 2) return 2;
                            break;
                        case 3:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 15: // P
                    switch (q)
                    {
                        case -2:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -1:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case 0:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 1:
                            if (val <= 4) return 4;
                            break;
                        case 2:
                            if (val <= 3) return 3;
                            break;
                        case 3:
                            if (val <= 2) return 2;
                            break;
                        case 4:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 16: // S
                    switch (q)
                    {
                        case -1:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case 0:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case 1:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 2:
                            if (val <= 4) return 4;
                            break;
                        case 3:
                            if (val <= 3) return 3;
                            break;
                        case 4:
                            if (val <= 2) return 2;
                            break;
                        case 5:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 17: // Cl
                    switch (q)
                    {
                        case 0:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case 1:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case 2:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 3:
                            if (val <= 4) return 4;
                            break;
                        case 4:
                            if (val <= 3) return 3;
                            break;
                        case 5:
                            if (val <= 2) return 2;
                            break;
                        case 6:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 31: // Ga
                    switch (q)
                    {
                        case -4:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -3:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case -2:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case -1:
                            if (val <= 4) return 4;
                            break;
                        case 0:
                            if (val <= 3) return 3;
                            break;
                        case 2:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 32: // Ge
                    switch (q)
                    {
                        case -3:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -2:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case -1:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 0:
                            if (val <= 4) return 4;
                            break;
                        case 1:
                            if (val <= 3) return 3;
                            break;
                        case 3:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 33: // As
                    switch (q)
                    {
                        case -2:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -1:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case 0:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 1:
                            if (val <= 4) return 4;
                            break;
                        case 2:
                            if (val <= 3) return 3;
                            break;
                        case 4:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 34: // Se
                    switch (q)
                    {
                        case -1:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case 0:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case 1:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 2:
                            if (val <= 4) return 4;
                            break;
                        case 3:
                            if (val <= 3) return 3;
                            break;
                        case 5:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 35: // Br
                    switch (q)
                    {
                        case 0:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case 1:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case 2:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 3:
                            if (val <= 4) return 4;
                            break;
                        case 4:
                            if (val <= 3) return 3;
                            break;
                        case 6:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 49: // In
                    switch (q)
                    {
                        case -4:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -3:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case -2:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case -1:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            break;
                        case 0:
                            if (val <= 3) return 3;
                            break;
                        case 2:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 50: // Sn
                case 82: // Pb
                    switch (q)
                    {
                        case -3:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -2:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case -1:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 0:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            break;
                        case 1:
                            if (val <= 3) return 3;
                            break;
                        case 3:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 51: // Sb
                case 83: // Bi
                    switch (q)
                    {
                        case -2:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -1:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case 0:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 1:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            break;
                        case 2:
                            if (val <= 3) return 3;
                            break;
                        case 4:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 52: // Te
                case 84: // Po
                    switch (q)
                    {
                        case -1:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case 0:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case 1:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 2:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            break;
                        case 3:
                            if (val <= 3) return 3;
                            break;
                        case 5:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 53: // I
                case 85: // At
                    switch (q)
                    {
                        case 0:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case 1:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case 2:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case 3:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            break;
                        case 4:
                            if (val <= 3) return 3;
                            break;
                        case 6:
                            if (val <= 1) return 1;
                            break;
                    }
                    break;

                case 81: // Tl
                    switch (q)
                    {
                        case -4:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            if (val <= 7) return 7;
                            break;
                        case -3:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            if (val <= 6) return 6;
                            break;
                        case -2:
                            if (val <= 3) return 3;
                            if (val <= 5) return 5;
                            break;
                        case -1:
                            if (val <= 2) return 2;
                            if (val <= 4) return 4;
                            break;
                        case 0:
                            if (val <= 1) return 1;
                            if (val <= 3) return 3;
                            break;
                    }
                    break;
            }
            return val;
        }
    }
}
