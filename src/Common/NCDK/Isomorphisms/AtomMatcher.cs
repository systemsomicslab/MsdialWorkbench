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
 */

using NCDK.Isomorphisms.Matchers;
using System;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// Defines compatibility checking of atoms for (subgraph)-isomorphism mapping.
    /// </summary>
    // @author John May
    // @cdk.module isomorphism
    public abstract class AtomMatcher
    {
        /// <summary>
        /// Are the semantics of <paramref name="atom1"/> compatible with <paramref name="atom2"/>.
        /// </summary>
        /// <param name="atom1">an atom from a query container</param>
        /// <param name="atom2">an atom from the target container</param>
        /// <returns>the <paramref name="atom1"/> can be paired with <paramref name="atom2"/></returns>
        public abstract bool Matches(IAtom atom1, IAtom atom2);

        /// <summary>
        /// Atoms are always compatible.
        /// </summary>
        /// <returns>a matcher for which all atoms match</returns>
        public static AtomMatcher CreateAnyMatcher()
        {
            return new AnyMatcher();
        }

        /// <summary>
        /// Atoms are compatible if they are the same element.
        /// </summary>
        /// <returns>a matcher which checks element compatibility</returns>
        public static AtomMatcher CreateElementMatcher()
        {
            return new ElementMatcher();
        }

        /// <summary>
        /// Atoms are compatible if the second atom (<c>atom2</c>) is accepted by
        /// the <see cref="IQueryAtom"/>, <c>atom1</c>.
        /// </summary>
        /// <returns>a matcher which checks query atom compatibility</returns>
        public static AtomMatcher CreateQueryMatcher()
        {
            return new QueryMatcher();
        }

        /// <summary>A matcher defines all atoms as compatible.</summary>
        private sealed class AnyMatcher : AtomMatcher
        {
            /// <inheritdoc/>
            public override bool Matches(IAtom atom1, IAtom atom2)
            {
                return true;
            }
        }

        /// <summary>
        /// A matcher to use when all atoms are <see cref="IQueryAtom"/>s. <c>atom1</c> is
        /// cast to a query atom and matched against <c>atom2</c> .
        /// </summary>
        private sealed class QueryMatcher : AtomMatcher
        {
            /// <inheritdoc/>
            public override bool Matches(IAtom atom1, IAtom atom2)
            {
                return ((IQueryAtom)atom1).Matches(atom2);
            }
        }

        /// <summary>
        /// A matcher to use when all atoms are <see cref="IQueryAtom"/>s. <c>atom1</c> is
        /// cast to a query atom and matched against <c>atom2</c>.
        /// </summary>
        private sealed class ElementMatcher : AtomMatcher
        {
            /// <inheritdoc/>
            public override bool Matches(IAtom atom1, IAtom atom2)
            {
                return GetAtomicNumber(atom1) == GetAtomicNumber(atom2);
            }

            /// <summary>
            /// Null safe atomic number access.
            /// </summary>
            /// <param name="atom">an atom</param>
            /// <returns>the atomic number</returns>
            private static int GetAtomicNumber(IAtom atom)
            {
                int? elem = atom.AtomicNumber;
                if (elem != null) return elem.Value;
                if (atom is IPseudoAtom) return 0;
                throw new NullReferenceException("an atom had unset atomic number");
            }
        }
    }
}
