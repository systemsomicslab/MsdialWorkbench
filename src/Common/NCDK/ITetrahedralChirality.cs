/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
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
    /// Stereochemistry specification for quadrivalent atoms. The data model defines the central, chiral <see cref="IAtom"/>,
    /// and its four ligand <see cref="IAtom"/>s, directly bonded to the chiral atom via an <see cref="IBond"/>. The ordering of the
    /// four ligands is important, and defines together with the <see cref="TetrahedralStereo"/> to spatial geometry around the chiral atom.
    /// The first ligand points towards to observer, and the three other ligands point away from the observer; the
    /// <see cref="TetrahedralStereo"/> then defines the order of the second, third, and fourth ligand to be clockwise or anti-clockwise.
    /// <para>
    /// If the tetrahedral centre has an implicit hydrogen or lone pair then the
    /// chiral atom is also stored as one of the ligands. This serves as a
    /// placeholder to indicate where the implicit hydrogen or lone pair would be.
    /// </para>
    /// </summary>
    // @cdk.module interfaces
    public interface ITetrahedralChirality
        : IStereoElement<IAtom, IAtom>
    {
        /// <summary>
        /// Returns an array of ligand atoms around the chiral atom. If the chiral
        /// centre has an implicit hydrogen or lone pair one of the ligands will be
        /// the chiral atom (<see cref="ChiralAtom"/>).
        /// </summary>
        /// <value>an array of four <see cref="IAtom"/>s.</value>
        IReadOnlyList<IAtom> Ligands { get; set; }
        IAtom ChiralAtom { get; }
        TetrahedralStereo Stereo { get; set; }
    }
}
