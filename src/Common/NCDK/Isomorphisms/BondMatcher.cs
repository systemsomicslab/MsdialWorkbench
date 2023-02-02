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

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// Defines compatibility checking of bonds for (subgraph)-isomorphism mapping.
    /// </summary>
    // @author John May
    // @cdk.module isomorphism
    public abstract class BondMatcher
    {
        /// <summary>
        /// Determines if <paramref name="bond1"/> is compatible with <paramref name="bond2"/>.
        /// </summary>
        /// <param name="bond1">a bond from the query structure</param>
        /// <param name="bond2">a bond from the target structure</param>
        /// <returns>the bonds are compatible</returns>
        public abstract bool Matches(IBond bond1, IBond bond2);

        /// <summary>
        /// All bonds are compatible.
        ///
        /// <returns>a bond matcher</returns>
        /// </summary>
        public static BondMatcher CreateAnyMatcher() => new AnyMatcher();

        /// <summary>
        /// Bonds are compatible if they are both aromatic or their orders are equal
        /// and they are non-aromatic. Under this matcher a single/double bond will
        /// not match a single/double bond which is aromatic.
        /// </summary>
        /// <returns>a bond matcher</returns>
        public static BondMatcher CreateStrictOrderMatcher()
        {
            return new StrictOrderMatcher();
        }

        /// <summary>
        /// Bonds are compatible if they are both aromatic or their orders are equal.
        /// This matcher allows a single/double bond to match a single/double
        /// aromatic bond.
        /// </summary>
        /// <returns>a bond matcher</returns>
        public static BondMatcher CreateOrderMatcher()
        {
            return new OrderMatcher();
        }

        /// <summary>
        /// Bonds are compatible if the first <c>bond1</c> (an <see cref="IQueryBond"/>)
        /// matches the second, <c>bond2</c>.
        /// </summary>
        /// <returns>a bond matcher</returns>
        public static BondMatcher CreateQueryMatcher()
        {
            return new QueryMatcher();
        }

        /// <summary>
        /// Bonds are compatible if they are both aromatic or their orders are
        /// equal.
        /// </summary>
        private sealed class OrderMatcher : BondMatcher
        {
            /// <inheritdoc/>
            public override bool Matches(IBond bond1, IBond bond2)
            {
                return bond1.IsAromatic && bond2.IsAromatic || bond1.Order == bond2.Order;
            }
        }

        /// <summary>
        /// Bonds are compatible if they are both aromatic or their orders are equal
        /// and they are non-aromatic. In this matcher a single or double bond will
        /// not match a single or double bond which is part of an aromatic system.
        /// </summary>
        private sealed class StrictOrderMatcher : BondMatcher
        {
            /// <inheritdoc/>
            public override bool Matches(IBond bond1, IBond bond2)
            {
                return bond1.IsAromatic == bond2.IsAromatic
                        && (bond1.Order == bond2.Order || bond1.IsAromatic && bond2.IsAromatic);
            }
        }

        /// <summary>All bonds are considered compatible.</summary>
        private sealed class AnyMatcher : BondMatcher
        {
            /// <inheritdoc/>
            public override bool Matches(IBond bond1, IBond bond2)
            {
                return true;
            }
        }

        /// <summary>
        /// Bonds are compatible if the first <c>bond1</c> (an <see cref="IQueryBond"/>)
        /// matches the second, <c>bond2</c>.
        /// </summary>
        private sealed class QueryMatcher : BondMatcher
        {
            /// <inheritdoc/>
            public override bool Matches(IBond bond1, IBond bond2)
            {
                return ((IQueryBond)bond1).Matches(bond2);
            }
        }
    }
}
