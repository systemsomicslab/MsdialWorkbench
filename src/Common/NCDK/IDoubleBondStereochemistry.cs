/* Copyright (C) 2012  Egon Willighagen <egonw@users.sf.net>
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
    /// Stereochemistry specification for double bond stereochemistry. The data model defines the double
    /// atoms and two ligands attached to those two atoms, linearly connected with the double bond in the
    /// middle. The IBonds that define the stereo element are defined as an array where the bonds
    /// are sorted according to the linear connectivity.Thus, the first and third bonds are the two
    /// bonds attached on either side of the double bond, and the second bond is the double bond.
    /// The stereo annotation then indicates if the ligand atoms are in the cis position
    /// (Conformation.Together) or in the trans position (Conformation.Opposite), matching the
    /// orientation of the methyls in but-2-ene respectively as <i>Z</i> and <i>E</i>.
    /// </summary>
    public interface IDoubleBondStereochemistry
        : IStereoElement<IBond, IBond>
    {
        /// <summary>
        /// An array of ligand bonds around the double bond.
        /// </summary>
        /// <value>an array of two <see cref="IBond"/>s.</value>
        IReadOnlyList<IBond> Bonds { get; }

        /// <summary>
        /// <see cref="IBond"/> that is the stereo center.
        /// </summary>
        IBond StereoBond { get; }

        /// <summary>
        /// Defines the stereochemistry around the double bond.
        /// </summary>
        /// <value>The <see cref="DoubleBondConformation"/> for this stereo element.</value>
        DoubleBondConformation Stereo { get; }
    }
}
