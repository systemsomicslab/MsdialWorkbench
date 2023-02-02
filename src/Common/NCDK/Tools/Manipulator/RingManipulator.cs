/* Copyright (C) 2007  Egon Willighagen <egonw@users.sf.net>
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
 
namespace NCDK.Tools.Manipulator
{
    // @cdk.module standard
    public static class RingManipulator
    {
        /// <summary>
        /// Marks the ring aromatic if all atoms and all bonds are aromatic.
        ///
        /// The method assumes that aromaticity of atoms and bonds have been
        /// detected beforehand
        /// </summary>
        /// <param name="ring">The ring to examine</param>
        public static void MarkAromaticRings(IRing ring)
        {
            // return as soon as the conditions are not met:
            // 1. all atoms are labeled aromatic
            foreach (var atom in ring.Atoms)
                if (!atom.IsAromatic)
                    return;

            // 2. all bonds are labeled aromatic
            foreach (var bond in ring.Bonds)
                if (!bond.IsAromatic)
                    return;

            // OK, all conditions are met, so ring is aromatic
            ring.IsAromatic = true;
        }
    }
}
