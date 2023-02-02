/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
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

using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// Represents the idea of a chemical reaction. The reaction consists of
    /// a set of reactants and a set of products.
    /// 
    /// <para>The class mostly represents abstract reactions, such as 2D diagrams,
    /// and is not intended to represent reaction trajectories.Such can better
    /// be represented with a ChemSequence.</para>
    /// </summary>
    // @cdk.module  interfaces
    // @author      Egon Willighagen <elw38@cam.ac.uk>
    // @cdk.created 2003-02-13
    // @cdk.keyword reaction
    public interface IReaction
        : IChemObject
    {
        /// <summary>
        /// The reactants in this reaction.
        /// </summary>
        IChemObjectSet<IAtomContainer> Reactants { get; }

        /// <summary>
        /// The products in this reaction.
        /// </summary>
        IChemObjectSet<IAtomContainer> Products { get; }

        /// <summary>
        /// The agents in this reaction.
        /// </summary>
        IChemObjectSet<IAtomContainer> Agents { get; }

        /// <summary>
        /// The <see cref="ReactionDirection"/> of the reaction.
        /// </summary>
        ReactionDirection Direction { get; set; }

        /// <summary>
        /// Returns the mappings between the reactant and the product side.
        /// </summary>
        IList<IMapping> Mappings { get; }
    }
}
