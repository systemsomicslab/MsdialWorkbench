/* Copyright (C) 2012   Syed Asad Rahman <asad@ebi.ac.uk>
 *
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;

namespace NCDK.Fingerprints
{
    /// <summary>
    /// This code returns a sorted set of atoms for a container according to its
    /// symbol and hybridization states. This will aid in finding a deterministic
    /// path rather than Stochastic one.
    /// </summary>
    // @author Syed Asad Rahman (2012)
    // @cdk.keyword fingerprint
    // @cdk.keyword similarity
    // @cdk.module fingerprint
    public static class SimpleAtomCanonicalizer
    {
        /// <param name="atoms">the container</param>
        /// <returns>canonicalized atoms</returns>
        public static IReadOnlyList<IAtom> CanonicalizeAtoms(IEnumerable<IAtom> atoms)
        {
            var canonicalizedVertexList = new List<IAtom>();
            foreach (var atom in atoms)
            {
                canonicalizedVertexList.Add(atom);
            }
            canonicalizedVertexList.Sort(new SimpleAtomComparator());
            return canonicalizedVertexList;
        }
    }
}
