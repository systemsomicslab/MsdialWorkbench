/*
 * Copyright (C) 2018 NextMove Software
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

using NCDK.Isomorphisms.Matchers;
using System;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// The depth-first (DF) backtracking sub-structure matching algorithm so named
    /// because it matches the molecule in a depth-first manner (bond by bond). The
    /// algorithm is a simple but elegant backtracking search iterating over the
    /// bonds of a query. Like the popular VF2 the algorithm, it uses linear memory
    /// but unlike VF2 bonded atoms are selected from the neighbor lists of already
    /// mapped atoms.
    /// </summary>
    /// <remarks>
    /// In practice VF2 take O(N<sup>2</sup>) to match a linear chain against it's
    /// self whilst this algorithm is O(N).
    /// <list type="bullet">
    /// <listheader>References</listheader>
    /// <item><token>cdk-cite-Ray57</token></item>
    /// <item><token>cdk-cite-Ullmann76</token></item>
    /// <item><token>cdk-cite-Cordella04</token></item>
    /// <item><token>cdk-cite-Jeliazkova18</token></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.DfPattern_Example.cs"]/*' />
    /// </example>
    /// <seealso cref="Mappings"/>
    // @author John Mayfield
    public class DfPattern : Pattern
    {
        private readonly IQueryAtomContainer query;
        private readonly DfState state;

        private DfPattern(IQueryAtomContainer query)
        {
            this.query = query;
            DetermineFilters(query);
            state = new DfState(query);
        }

        private static void CheckCompatibleAPI(IAtom atom)
        {
            if (atom.Container == null)
            {
                throw new ArgumentException(
                        "This API can only be used with the option " +
                        "CdkUseLegacyAtomContainer=false (default). The atoms in " +
                        "the molecule provided do not know about their parent " +
                        "molecule"
                );
            }
        }

        /// <inheritdoc/>
        public override int[] Match(IAtomContainer target)
        {
            return MatchAll(target).First();
        }

        /// <inheritdoc/>
        public override bool Matches(IAtomContainer target)
        {
            return MatchAll(target).AtLeast(1);
        }

        /// <inheritdoc/>
        public override Mappings MatchAll(IAtomContainer mol)
        {
            if (mol.Atoms.Count < query.Atoms.Count)
                return new Mappings(query, mol, Array.Empty<int[]>());
            if (mol.Atoms.Count > 0)
                CheckCompatibleAPI(mol.Atoms[0]);
            var local = new DfState(state);
            local.SetMol(mol);
            var mappings = new Mappings(query, mol, local);
            return Filter(mappings, query, mol);
        }

        /// <summary>
        /// Match the pattern at the provided root.
        /// </summary>
        /// <param name="root">the root atom of the molecule</param>
        /// <returns>mappings</returns>
        /// <see cref="Mappings"/>
        Mappings MatchRoot(IAtom root)
        {
            CheckCompatibleAPI(root);
            var mol = root.Container;
            if (query.Atoms.Count > 0 && ((IQueryAtom)query.Atoms[0]).Matches(root))
            {
                var local = new DfState(state);
                local.SetRoot(root);
                return Filter(new Mappings(query, mol, local), query, mol);
            }
            else
            {
                return new Mappings(query, mol, Array.Empty<int[]>());
            }
        }

        /// <summary>
        /// Test whether the pattern matches at the provided atom.
        /// </summary>
        /// <param name="root">the root atom of the molecule</param>
        /// <returns>the pattern matches</returns>
        public bool MatchesRoot(IAtom root)
        {
            return MatchRoot(root).AtLeast(1);
        }

        /// <summary>
        /// Create a pattern which can be used to find molecules which contain the <paramref name="query"/>
        /// structure. If a 'real' molecule is provided is is converted
        /// with <see cref="QueryAtomContainer.Create(IAtomContainer, ExprType[])"/>
        /// matching elements, aromaticity status, and bond orders.
        /// </summary>
        /// <param name="query">the substructure to find</param>
        /// <returns>a pattern for finding the <paramref name="query"/></returns>
        /// <seealso cref="QueryAtomContainer.Create(IAtomContainer, ExprType[])"/>
        public static new DfPattern CreateSubstructureFinder(IAtomContainer query)
        {
            if (query is IQueryAtomContainer)
                return new DfPattern((IQueryAtomContainer)query);
            else
                return new DfPattern(QueryAtomContainer.Create(
                    query,
                    ExprType.AliphaticElement,
                    ExprType.AromaticElement,
                    ExprType.SingleOrAromatic,
                    ExprType.AliphaticOrder,
                    ExprType.Stereochemistry));
        }
    }
}
