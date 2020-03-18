/* Copyright (C) 2008-2009  Gilleain Torrance <gilleain.torrance@gmail.com>
 *               2008-2009  Arvid Berg <goglepox@users.sf.net>
 *                    2009  Stefan Kuhn <shk3@users.sf.net>
 *               2009-2010  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@list.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using NCDK.Geometries;

namespace NCDK.Renderers
{
    /// <summary>
    /// Utility class for calculating the average bond length for various IChemObject subtypes : IReaction, IAtomContainerSet, IChemModel, and IReactionSet.
    /// </summary>
    // @author maclean
    // @cdk.module renderbasic
    public class AverageBondLengthCalculator
    {
        /// <summary>
        /// Calculate the average bond length for the bonds in a reaction.
        /// </summary>
        /// <param name="reaction">the reaction to use</param>
        /// <returns>the average bond length</returns>
        public static double CalculateAverageBondLength(IReaction reaction)
        {
            var reactants = reaction.Reactants;
            double reactantAverage = 0.0;
            if (reactants != null)
            {
                reactantAverage = CalculateAverageBondLength(reactants) / reactants.Count;
            }

            var products = reaction.Products;
            double productAverage = 0.0;
            if (products != null)
            {
                productAverage = CalculateAverageBondLength(products) / products.Count;
            }

            if (productAverage == 0.0 && reactantAverage == 0.0)
            {
                return 1.0;
            }
            else
            {
                return (productAverage + reactantAverage) / 2.0;
            }
        }

        /// <summary>
        /// Calculate the average bond length for the bonds in a molecule set.
        /// </summary>
        /// <param name="moleculeSet">the molecule set to use</param>
        /// <returns>the average bond length</returns>
        public static double CalculateAverageBondLength(IChemObjectSet<IAtomContainer> moleculeSet)
        {
            double averageBondModelLength = 0.0;
            foreach (var atomContainer in moleculeSet)
            {
                averageBondModelLength += GeometryUtil.GetBondLengthAverage(atomContainer);
            }
            return averageBondModelLength / moleculeSet.Count;
        }

        /// <summary>
        /// Calculate the average bond length for the bonds in a chem model.
        /// </summary>
        /// <param name="model">the model for which to calculate the average bond length</param>
        /// <returns>the average bond length</returns>
        public static double CalculateAverageBondLength(IChemModel model)
        {
            // empty models have to have a scale
            var moleculeSet = model.MoleculeSet;
            if (moleculeSet == null)
            {
                IReactionSet reactionSet = model.ReactionSet;
                if (reactionSet != null)
                {
                    return CalculateAverageBondLength(reactionSet);
                }
                return 0.0;
            }

            return CalculateAverageBondLength(moleculeSet);
        }

        /// <summary>
        /// Calculate the average bond length for the bonds in a reaction set.
        /// </summary>
        /// <param name="reactionSet">the reaction set to use</param>
        /// <returns>the average bond length</returns>
        public static double CalculateAverageBondLength(IReactionSet reactionSet)
        {
            double averageBondModelLength = 0.0;
            foreach (var reaction in reactionSet)
            {
                averageBondModelLength += CalculateAverageBondLength(reaction);
            }
            return averageBondModelLength / reactionSet.Count;
        }
    }
}
