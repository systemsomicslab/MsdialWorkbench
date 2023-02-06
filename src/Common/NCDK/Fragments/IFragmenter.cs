/* Copyright (C) 2010  Rajarshi Guha <rajarshi.guha@gmail.com>
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

namespace NCDK.Fragments
{
    /// <summary>
    /// An interface for classes implementing fragmentation algorithms.
    /// </summary>
    // @author Rajarshi Guha
    // @cdk.module  fragment
    // @cdk.keyword fragment
    public interface IFragmenter
    {
        /// <summary>
        /// Generate fragments for the input molecule.
        /// </summary>
        /// <param name="atomContainer">The input molecule</param>
        /// <exception cref="CDKException">if ring detection fails</exception>
        void GenerateFragments(IAtomContainer atomContainer);

        /// <summary>
        /// Get the fragments generated as SMILES strings.
        /// </summary>
        /// <returns>a string[] of the fragments.</returns>
        IEnumerable<string> GetFragments();

        /// <summary>
        /// Get fragments generated as <see cref="IAtomContainer"/> objects.
        /// </summary>
        /// <returns>an IAtomContainer[] of fragments</returns>
        IEnumerable<IAtomContainer> GetFragmentsAsContainers();
    }
}
