/* Copyright (C) 2008  Miguel Rojas <miguelrojasch@yahoo.es>
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

using System.Collections.Generic;

namespace NCDK.Reactions
{
    /// <summary>
    /// Classes that implement this interface are reaction mechanisms.
    /// </summary>
    // @author      Miguel Rojas
    // @cdk.module  reaction
    public interface IReactionMechanism
    {
        /// <summary>
        /// Initiates the process for the given mechanism. The atoms to apply are mapped between
        /// reactants and products.
        /// </summary>
        /// <param name="atomContainerSet"></param>
        /// <param name="atomList">The list of atoms taking part in the mechanism</param>
        /// <param name="bondList">The list of bonds taking part in the mechanism</param>
        /// <returns>The Reaction mechanism</returns>
        /// <exception cref="CDKException">if an error occurs during the reaction process. See documentation for individual reaction processes</exception>
        IReaction Initiate(IChemObjectSet<IAtomContainer> atomContainerSet, IList<IAtom> atomList, IList<IBond> bondList);
    }
}
