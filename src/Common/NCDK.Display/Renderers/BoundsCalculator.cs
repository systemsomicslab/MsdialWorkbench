/* Copyright (C) 2008-2009  Gilleain Torrance <gilleain@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All I ask is that proper credit is given for my work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Windows;

namespace NCDK.Renderers
{
    /// <summary>
    /// Utility class for calculating the 2D bounding rectangles (bounds)
    /// of various IChemObject subtypes - IChemModel, IReactionSet, IReaction,
    /// IAtomContainerSet, and IAtomContainer.
    /// </summary>
    // @author maclean
    // @cdk.module renderbasic
    public class BoundsCalculator
    {
        /// <summary>
        /// Calculate the bounding rectangle for a chem model.
        /// </summary>
        /// <param name="chemModel">the chem model to use</param>
        /// <returns>the bounding rectangle of the chem model</returns>
        public static Rect CalculateBounds(IChemModel chemModel)
        {
            var moleculeSet = chemModel.MoleculeSet;
            var reactionSet = chemModel.ReactionSet;
            var totalBounds = Rect.Empty;
            if (moleculeSet != null)
            {
                totalBounds = CalculateBounds(moleculeSet);
            }
            if (reactionSet != null)
            {
                if (totalBounds.IsEmpty)
                {
                    totalBounds = CalculateBounds(moleculeSet);
                }
                else
                {
                    totalBounds = Rect.Union(totalBounds, CalculateBounds(reactionSet));
                }
            }
            if (totalBounds.IsEmpty)
                totalBounds = new Rect();
            return totalBounds;
        }

        /// <summary>
        /// Calculate the bounding rectangle for a reaction set.
        /// </summary>
        /// <param name="reactionSet">the reaction set to use</param>
        /// <returns>the bounding rectangle of the reaction set</returns>
        public static Rect CalculateBounds(IReactionSet reactionSet)
        {
            var totalBounds = Rect.Empty;
            foreach (var reaction in reactionSet)
            {
                var reactionBounds = CalculateBounds(reaction);
                if (totalBounds.IsEmpty)
                {
                    totalBounds = reactionBounds;
                }
                else
                {
                    totalBounds = Rect.Union(totalBounds, reactionBounds);
                }
            }
            if (totalBounds.IsEmpty)
                totalBounds = new Rect();
            return totalBounds;
        }

        /// <summary>
        /// Calculate the bounding rectangle for a reaction.
        /// </summary>
        /// <param name="reaction">the reaction to use</param>
        /// <returns>the bounding rectangle of the reaction</returns>
        public static Rect CalculateBounds(IReaction reaction)
        {
            // get the participants in the reaction
            var reactants = reaction.Reactants;
            var products = reaction.Products;
            if (reactants == null || products == null)
                return Rect.Empty;

            // determine the bounds of everything in the reaction
            var reactantsBounds = CalculateBounds(reactants);
            return Rect.Union(reactantsBounds, CalculateBounds(products));
        }

        /// <summary>
        /// Calculate the bounding rectangle for a molecule set.
        /// </summary>
        /// <param name="moleculeSet">the molecule set to use</param>
        /// <returns>the bounding rectangle of the molecule set</returns>
        public static Rect CalculateBounds(IChemObjectSet<IAtomContainer> moleculeSet)
        {
            var totalBounds = Rect.Empty;
            foreach (var container in moleculeSet)
            {
                var acBounds = CalculateBounds(container);
                if (totalBounds.IsEmpty)
                {
                    totalBounds = acBounds;
                }
                else
                {
                    totalBounds = Rect.Union(totalBounds, acBounds);
                }
            }
            if (totalBounds.IsEmpty)
                totalBounds = new Rect();
            return totalBounds;
        }

        /// <summary>
        /// Calculate the bounding rectangle for an atom container.
        /// </summary>
        /// <param name="atomContainer">the atom container to use</param>
        /// <returns>the bounding rectangle of the atom container</returns>
        public static Rect CalculateBounds(IAtomContainer atomContainer)
        {
            // this is essential, otherwise a rectangle
            // of (+INF, -INF, +INF, -INF) is returned!
            if (atomContainer.Atoms.Count == 0)
            {
                return new Rect();
            }
            else if (atomContainer.Atoms.Count == 1)
            {
                var p_point = atomContainer.Atoms[0].Point2D;
                if (p_point == null)
                {
                    throw new ArgumentException("Cannot calculate bounds when 2D coordinates are missing.");
                }
                var point = p_point.Value;
                return new Rect(point.X, point.Y, 0, 0);
            }

            var xmin = double.PositiveInfinity;
            var xmax = double.NegativeInfinity;
            var ymin = double.PositiveInfinity;
            var ymax = double.NegativeInfinity;

            foreach (var atom in atomContainer.Atoms)
            {
                if (atom.Point2D == null)
                {
                    throw new ArgumentException("Cannot calculate bounds when 2D coordinates are missing.");
                }
                var point = atom.Point2D.Value;
                xmin = Math.Min(xmin, point.X);
                xmax = Math.Max(xmax, point.X);
                ymin = Math.Min(ymin, point.Y);
                ymax = Math.Max(ymax, point.Y);
            }
            var width = xmax - xmin;
            var height = ymax - ymin;
            return new Rect(xmin, ymin, width, height);
        }
    }
}
