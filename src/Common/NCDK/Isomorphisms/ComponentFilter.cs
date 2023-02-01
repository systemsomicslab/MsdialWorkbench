/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *               2018 John May <jwmay@users.sf.net>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
 */

using NCDK.Graphs;
using System;
using System.Linq;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// A predicate for verifying component level grouping in query/target structure
    /// matching. The grouping is used by SMARTS and is critical to querying
    /// reactions. The grouping specifies that substructures in the query should
    /// match to separate components in the target. The grouping specification is
    /// indicated by an <see cref="int"/>[] array of length (|V(query)| + 1). The final
    /// index indicates the maximum component group (in the query). A specification
    /// of '0' indicates there are no grouping restrictions.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.ComponentFilter_Example.cs"]/*' />
    /// </example>
    /// <seealso cref="Pattern"/>
    // @author John May
    // @cdk.module isomorphism
    public sealed class ComponentFilter
    {
        /// <summary>
        /// Key indicates where the grouping should be store in the query
        /// properties.
        /// </summary>
        public const string Key = "COMPONENT.GROUPING";

        /// <summary>The required  (query) and the targetComponents of the target.</summary>
        private readonly int[] queryComponents, targetComponents;

        private readonly int maxComponentIdx;

        /// <summary>
        /// Create a predicate to match components for the provided query and target.
        /// The target is converted to an adjacency list (<see cref="GraphUtil.ToAdjList(IAtomContainer)"/> 
        /// ) and the query components extracted
        /// from the property <see cref="Key"/> in the query.
        /// </summary>
        /// <param name="query">query structure</param>
        /// <param name="target">target structure</param>
        public ComponentFilter(IAtomContainer query, IAtomContainer target)
            : this(query.GetProperty<int[]>(Key) ?? DetermineComponents(query, false),
                   DetermineComponents(target, true))
        { }

        private static int[] DetermineComponents(IAtomContainer target, bool auto)
        {
            int[] components = null;
            // no atoms -> no components
            if (target.IsEmpty())
                components = new int[0];
            // defined by reaction grouping
            if (components == null && target.Atoms[0].GetProperty<int?>(CDKPropertyName.ReactionGroup) !=null)
            {
                int max = 0;
                components = new int[target.Atoms.Count + 1];
                for (int i = 0; i < target.Atoms.Count; i++)
                {
                    var grp = target.Atoms[i].GetProperty<int?>(CDKPropertyName.ReactionGroup);
                    if (grp == null)
                        grp = 0;
                    components[i] = grp.Value;
                    if (grp > max)
                        max = grp.Value;
                }
                components[target.Atoms.Count] = max;
            }
            // calculate from connection table
            if (components == null && auto)
            {
                int max = 0;
                components = new int[target.Atoms.Count + 1];
                int i = 0;
                foreach (int grp in new ConnectedComponents(GraphUtil.ToAdjList(target)).GetComponents())
                {
                    components[i++] = grp;
                    if (grp > max)
                        max = grp;
                }
                components[target.Atoms.Count] = max;
            }
            return components;
        }

        /// <summary>
        /// Create a predicate to match components for the provided query (grouping)
        /// and target (connected components).
        /// </summary>
        /// <param name="grouping">query grouping</param>
        /// <param name="targetComponents">connected component of the target</param>
        public ComponentFilter(int[] grouping, int[] targetComponents)
        {
            this.queryComponents = grouping;
            this.targetComponents = targetComponents;
            int max = 0;
            if (targetComponents != null)
            {
                for (int i = 0; i < targetComponents.Length; i++)
                    if (targetComponents[i] > max)
                        max = targetComponents[i];
            }
            this.maxComponentIdx = max;
        }

        /// <summary>
        /// Does the mapping respected the component grouping specified by the
        /// query.
        /// </summary>
        /// <param name="mapping">a permutation of the query vertices</param>
        /// <returns>the mapping preserves the specified grouping</returns>
        public bool Apply(int[] mapping)
        {
            // no grouping required
            if (queryComponents == null) return true;

            // bidirectional map of query/target components, last index
            // of query components holds the count
            int[] usedBy = new int[maxComponentIdx + 1];
            int[] usedIn = new int[queryComponents[mapping.Length] + 1];

            // verify we don't have any collisions
            for (int v = 0; v < mapping.Length; v++)
            {
                if (queryComponents[v] == 0) continue;

                int w = mapping[v];

                int queryComponent = queryComponents[v];
                int targetComponent = targetComponents[w];

                // is the target component already used by a query component?
                if (usedBy[targetComponent] == 0)
                    usedBy[targetComponent] = queryComponent;
                else if (usedBy[targetComponent] != queryComponent) return false;

                // is the query component already used in a target component?
                if (usedIn[queryComponent] == 0)
                    usedIn[queryComponent] = targetComponent;
                else if (usedIn[queryComponent] != targetComponent) return false;

            }

            return true;
        }
    }
}
