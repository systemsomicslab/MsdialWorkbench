/* Copyright (C) 2005-2007  Matteo Floris <mfe4@users.sf.net>
 *                    2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;

namespace NCDK.AtomTypes
{
    /// <summary>
    /// Classes that implement this interface are atom type matchers. They find the
    /// most appropriate AtomType matching the given Atom in a given atom type list.
    /// </summary>
    /// <seealso cref="IAtomTypeGuesser"/>
    // @author      mfe4
    // @cdk.created 2004-12-02
    // @cdk.module  core
    public interface IAtomTypeMatcher
    {
        /// <summary>
        /// Method that assigns an atom type to a given atom belonging to an atom container.
        /// </summary>
        /// <param name="container">AtomContainer of which the <paramref name="atom"/> is part</param>
        /// <param name="atom">Atom for which a matching atom type is searched</param>
        /// <returns>The matching AtomType</returns>
        /// <exception cref="CDKException">when something went wrong with going through the AtomType's</exception>
        IAtomType FindMatchingAtomType(IAtomContainer container, IAtom atom);

        /// <summary>
        /// Method that assigns atom types to atoms in the given atom container.
        /// </summary>
        /// <param name="container">AtomContainer for which atom types are perceived</param>
        /// <returns>The matching AtomType</returns>      
        /// <exception cref="CDKException"> when something went wrong with going through the AtomType's</exception>        
        IEnumerable<IAtomType> FindMatchingAtomTypes(IAtomContainer container);
    }
}
