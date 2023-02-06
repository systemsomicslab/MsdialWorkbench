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
using NCDK.Smiles.Isomorphisms;
using NCDK.Tools.Manipulator;
using System.Linq;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// A structural pattern for finding an exact matching in a target compound.
    /// </summary>
    // @author John May
    // @cdk.module isomorphism
    public abstract class Pattern
    {
        /// <summary>
        /// Additional filters on results.
        /// </summary>
        private bool hasStereo;
        private bool hasQueryStereo;
        private bool hasComponentGrouping;
        private bool hasReactionMap;

        internal void DetermineFilters(IAtomContainer query)
        {
            hasStereo = query.StereoElements.Any();
            hasComponentGrouping = query.GetProperty<int[]>(ComponentFilter.Key) != null;
            foreach (var atom in query.Atoms)
            {
                var compId = atom.GetProperty<int?>(CDKPropertyName.ReactionGroup);
                var mapIdx = atom.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
                if (mapIdx != null && mapIdx != 0)
                    hasReactionMap = true;
                if (compId != null && compId != 0)
                    hasComponentGrouping = true;
                if (atom is IQueryAtom)
                    hasQueryStereo = true;
                if (hasReactionMap && hasComponentGrouping && hasQueryStereo)
                    break;
            }
        }

        internal Mappings Filter(Mappings mappings, IAtomContainer query, IAtomContainer target)
        {
            // apply required post-match filters
            if (hasStereo)
            {
                mappings = hasQueryStereo
                    ? mappings.Filter(new QueryStereoFilter(query, target).Apply)
                    : mappings.Filter(new StereoMatch(query, target).Apply);
            }
            if (hasComponentGrouping)
                mappings = mappings.Filter(new ComponentFilter(query, target).Apply);
            if (hasReactionMap)
                mappings = mappings.Filter(new AtomMapFilter(query, target).Apply);
            return mappings;
        }

        /// <summary>
        /// Find a matching of this pattern in the <paramref name="target"/>. If no such order
        /// exist an empty mapping is returned. Depending on the implementation
        /// stereochemistry may be checked (recommended).
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Pattern_Example.cs+Match"]/*' />
        /// </example>
        /// <param name="target">the container to search for the pattern in</param>
        /// <returns>the mapping from the pattern to the target or an empty array</returns>
        public abstract int[] Match(IAtomContainer target);

        /// <summary>
        /// Determine if there is a mapping of this pattern in the <paramref name="target"/>.
        /// Depending on the implementation stereochemistry may be checked
        /// (recommended).
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Pattern_Example.cs+Matches"]/*' />
        /// </example>
        /// <param name="target">the container to search for the pattern in</param>
        /// <returns>the mapping from the pattern to the target</returns>
        public virtual bool Matches(IAtomContainer target)
        {
            return Match(target).Length > 0;
        }

        /// <summary>
        /// Determine if there is a mapping of this pattern in the <paramref name="target"/>
        /// reaction.
        /// </summary>
        /// <example>
        /// <code>
        /// Pattern pattern = ...; // create pattern
        /// foreach (IReaction r in rs) 
        /// {
        ///     if (pattern.Matches(r)) 
        ///     {
        ///         // found mapping!
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="target">the reaction to search for the pattern in</param>
        /// <returns>the mapping from the pattern to the target</returns>
        public bool Matches(IReaction target)
        {
            return Matches(ReactionManipulator.ToMolecule(target));
        }

        /// <summary>
        /// Find all mappings of this pattern in the <paramref name="target"/>. Stereochemistry
        /// should not be checked to allow filtering with <see cref="Mappings.GetStereochemistry"/>. 
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Pattern_Example.cs+MatchAll1"]/*' />
        /// Using the fluent interface (see <see cref="Mappings"/>) we can search and
        /// manipulate the mappings. Here's an example of finding the first 5
        /// mappings and creating an array. If the mapper is lazy other states are
        /// simply not explored.
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.Pattern_Example.cs+MatchAll2"]/*' />
        /// </example>
        /// <param name="target">the container to search for the pattern in</param>
        /// <returns>the mapping from the pattern to the target</returns>
        /// <seealso cref="Mappings"/>
        public abstract Mappings MatchAll(IAtomContainer target);

        /// <summary>
        /// Find all mappings of this pattern in the <paramref name="target"/> reaction.
        /// </summary>
        /// <example>
        /// <code>
        /// Pattern pattern = Pattern.CreateSubstructureFinder(query);
        /// foreach (IReaction r in rs) 
        /// {
        ///     foreach (var mapping in pattern.MatchAll(r))
        ///     {
        ///         // found mapping
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <remarks>
        /// The reaction is inlined into a molecule and vs mapped id's correspond
        /// to the absolute atom index in the reaction when considered as reactants, agents,
        /// products <see cref="ReactionManipulator.ToMolecule(IReaction)"/>.
        /// </remarks>
        /// <param name="target">the reaction to search for the pattern in</param>
        /// <returns>the mapping from the pattern to the target</returns>
        /// <seealso cref="Mappings"/>
        /// <seealso cref="ReactionManipulator.ToMolecule(IReaction)"/>
        public Mappings MatchAll(IReaction target)
        {
            return MatchAll(ReactionManipulator.ToMolecule(target));
        }

        /// <summary>
        /// Create a pattern which can be used to find molecules which contain the
        /// <paramref name="query"/> structure. The default structure search implementation is
        /// <see cref="VentoFoggia"/>.
        /// </summary>
        /// <param name="query">the substructure to find</param>
        /// <returns>a pattern for finding the <paramref name="query"/></returns>
        /// <seealso cref="VentoFoggia"/>
        public static Pattern CreateSubstructureFinder(IAtomContainer query)
        {
            return VentoFoggia.CreateSubstructureFinder(query);
        }

        /// <summary>
        /// Create a pattern which can be used to find molecules which are the same
        /// as the <paramref name="query"/> structure. The default structure search
        /// implementation is <see cref="VentoFoggia"/>.
        /// </summary>
        /// <param name="query">the substructure to find</param>
        /// <returns>a pattern for finding the <paramref name="query"/></returns>
        /// <seealso cref="VentoFoggia"/>
        public static Pattern CreateIdenticalFinder(IAtomContainer query)
        {
            return VentoFoggia.CreateIdenticalFinder(query);
        }
    }
}
