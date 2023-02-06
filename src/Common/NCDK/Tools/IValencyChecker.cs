/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
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

namespace NCDK.Tools
{
    /// <summary>
    /// A common interface for SaturationChecker and ValencyChecker. Mainly created
    /// to be able to have HydrogenAdder use both.
    /// </summary>
    // @author         Egon Willighagen
    // @cdk.created    2004-01-08
    // @cdk.module     valencycheck
    public interface IValencyChecker
    {
        /// <summary>
        /// Determines of all atoms on <paramref name="container"/> are saturated.
        /// </summary>
        /// <param name="container">Atom container to check</param>
        /// <returns><see langword="true"/>, if it's right saturated</returns>
        bool IsSaturated(IAtomContainer container);

        /// <summary>
        /// Checks if <paramref name="atom"/> in <paramref name="container"/> is saturated 
        /// by comparing it with known atom types.
        /// </summary>
        /// <param name="atom">Atom to check</param>
        /// <param name="container">Atom container to check</param>
        /// <returns><see langword="true"/>, if it's right saturated</returns>
        bool IsSaturated(IAtom atom, IAtomContainer container);
    }
}
