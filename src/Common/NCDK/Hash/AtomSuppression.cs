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
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Config;
using System;
using System.Collections;

namespace NCDK.Hash
{
    /// <summary>
    /// Defines a method of suppressing certain atoms from an <see cref="IAtomContainer"/>
    /// when computing the hash codes for the molecule or its atoms.
    /// </summary>
    // @author John May
    // @cdk.module hash
    internal abstract class AtomSuppression
    {
        /// <summary>
        /// Returns a new instance indicating which atoms are suppressed for this
        /// suppression method.
        /// </summary>
        /// <param name="container">molecule with 0 or more atoms</param>
        /// <returns>the vertices (atom index) which should be suppressed</returns>
        public abstract Suppressed Suppress(IAtomContainer container);

        /// <summary>Default implementation - don't suppress anything.</summary>
        private sealed class UnsuppressedAtomSuppression : AtomSuppression
        {
            public override Suppressed Suppress(IAtomContainer container)
            {
                return Suppressed.None;
            }
        }

        /// <summary>
        /// Suppresses any explicit hydrogen regardless of whether the atom is a
        /// hydrogen ion or isotope.
        /// </summary>
        private sealed class AnyHydrogensAtomSuppression : AtomSuppression
        {
            public override Suppressed Suppress(IAtomContainer container)
            {
                BitArray hydrogens = new BitArray(container.Atoms.Count);
                for (int i = 0; i < container.Atoms.Count; i++)
                {
                    IAtom atom = container.Atoms[i];
                    hydrogens.Set(i, atom.AtomicNumber.Equals(AtomicNumbers.H));
                }
                return Suppressed.FromBitSet(hydrogens);
            }
        }

        /// <summary>Suppresses any pseudo atom.</summary>
        private sealed class AnyPseudosAtomSuppression : AtomSuppression
        {
            public override Suppressed Suppress(IAtomContainer container)
            {
                BitArray hydrogens = new BitArray(container.Atoms.Count);
                for (int i = 0; i < container.Atoms.Count; i++)
                {
                    IAtom atom = container.Atoms[i];
                    hydrogens.Set(i, atom is IPseudoAtom);
                }
                return Suppressed.FromBitSet(hydrogens);
            }
        }

        /// <summary>
        /// A suppression which wont' suppress anything.
        /// Do not suppress any atoms.
        /// </summary>
        public static AtomSuppression Unsuppressed { get; } = new UnsuppressedAtomSuppression();

        /// <summary>
        /// A a suppression which will mark 'all' explicit hydrogens.
        /// Suppress all hydrogens even if they are charged or an isotope.
        /// </summary>
        public static AtomSuppression AnyHydrogens { get; } = new AnyHydrogensAtomSuppression();

        /// <summary>
        /// A suppression which will mark 'all' pseudo atoms.
        /// Suppress all pseudo atoms regardless of what their label is.
        /// </summary>
        public static AtomSuppression AnyPseudos = new AnyPseudosAtomSuppression();
    }
}
