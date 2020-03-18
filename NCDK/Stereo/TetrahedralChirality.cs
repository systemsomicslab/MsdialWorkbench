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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System.Collections.Generic;
using System.Text;

namespace NCDK.Stereo
{
    /// <summary>
    /// Stereochemistry specification for tetravalent atoms. See <see cref="ITetrahedralChirality"/> for
    /// further details.
    /// </summary>
    /// <seealso cref="ITetrahedralChirality"/>
    // @cdk.module core
    public class TetrahedralChirality
        : AbstractStereo<IAtom, IAtom>, ITetrahedralChirality
    {
        public TetrahedralChirality(IAtom chiralAtom, IReadOnlyList<IAtom> ligands, TetrahedralStereo stereo)
            : this(chiralAtom, ligands, stereo.ToConfiguration())
        {
        }

        public TetrahedralChirality(IAtom chiralAtom, IReadOnlyList<IAtom> ligands, StereoConfigurations configure)
            : base(chiralAtom, ligands.ToReadOnlyList(), new StereoElement(StereoClass.Tetrahedral, configure))
        {
        }

        public TetrahedralChirality(IAtom chiralAtom, IReadOnlyList<IAtom> ligands, StereoElement stereo)
            : this(chiralAtom, ligands, stereo.Configuration)
        {
        }

        /// <summary>
        /// An array of ligand atoms around the chiral atom.
        /// </summary>
        public virtual IReadOnlyList<IAtom> Ligands
        {
            get => Carriers;
            set => Carriers = value;
        }

        /// <summary>
        /// Atom that is the chirality center.
        /// </summary>
        public virtual IAtom ChiralAtom => Focus;

        /// <summary>
        /// Defines the stereochemistry around the chiral atom. The value depends on the order of ligand atoms.
        /// </summary>
        public virtual TetrahedralStereo Stereo
        {
            get { return Configure.ToStereo(); }
            set { Configure = value.ToConfiguration(); }
        }

        protected override IStereoElement<IAtom, IAtom> Create(IAtom focus, IReadOnlyList<IAtom> carriers, StereoElement stereo)
        {
            return new TetrahedralChirality(focus, carriers, stereo);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("Tetrahedral{").Append(GetHashCode()).Append(", ");
            sb.Append(this.Stereo).Append(", ");
            sb.Append("c:").Append(this.ChiralAtom).Append(", ");
            var ligands = this.Ligands;
            for (int i = 0; i < ligands.Count; i++)
                sb.Append(i + 1).Append(':').Append(ligands[i]).Append(", ");
            sb.Append('}');
            return sb.ToString();
        }
    }
}
