/* Copyright (C) 2006-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Reactions.Types.Parameters;
using System;
using System.Collections.Generic;

namespace NCDK.Reactions
{
    /// <summary>
    /// Classes that implement this interface are Reactions types.
    /// </summary>
    // @author      Miguel Rojas
    // @cdk.module  reaction
    public interface IReactionProcess
    {
        /// <summary>
        ///  The specification attribute of this object
        /// </summary>
        /// <returns>The specification value</returns>
        ReactionSpecification Specification { get; }

        /// <summary>
        /// the parameters for this reaction.
        /// </summary>
        /// <remarks>Must be done before calling calculate as the parameters influence the calculation outcome.</remarks>
        IList<IParameterReaction> ParameterList { get; set; }

        /// <summary>
        /// Initiates the process for the given reaction.
        /// Optionally, parameters may be set which can affect the course of the process.
        /// </summary>
        /// <param name="reactants">An <see cref="IAtomContainerSet"/> for which this process should be initiate.</param>
        /// <param name="agents">An <see cref="IAtomContainerSet"/> for which this process should be initiate.</param>
        /// <returns>The set of reactions.</returns>
        /// <exception cref="CDKException">if an error occurs during the reaction process. See documentation for individual reaction processes</exception>
        IReactionSet Initiate(IChemObjectSet<IAtomContainer> reactants, IChemObjectSet<IAtomContainer> agents);

        /// <summary>
        /// Return the <see cref="IParameterReaction"/> if it exists given the class.
        /// </summary>
        /// <param name="paramClass"></param>
        /// <returns></returns>
        IParameterReaction GetParameterClass(Type paramClass);
    }
}
