/* Copyright (C) 2009  Rajarshi Guha <rajarshi.guha@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using NCDK.Isomorphisms.Matchers;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NCDK.Pharmacophore
{
    /// <summary>
    /// Represents a colleciton of pharmacophore groups and constraints.
    /// <para>
    /// This : <see cref="QueryAtomContainer"/> since
    /// we need to be able to support things such as exclusion volumes, which cannot (easily)
    /// be represented as atom or bond analogs.
    /// </para>
    /// </summary>
    // @author Rajarshi Guha
    // @cdk.module pcore
    // @cdk.keyword pharmacophore
    // @cdk.keyword 3D isomorphism
    public class PharmacophoreQuery : QueryAtomContainer
    {
        private readonly List<object> exclusionVolumes;

        public PharmacophoreQuery()
            : base()
        {
            // builder should be injected but this is difficult as this class is create in static methods
            exclusionVolumes = new List<object>();
        }

        /// <summary>
        /// string representation of this query.
        /// </summary>
        /// <returns>string representation of this query</returns>
        public override string ToString()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append("PharmacophoreQuery(").Append(this.GetHashCode()).Append(", ");
            stringBuilder.Append("#A:").Append(Atoms.Count).Append(", ");
            stringBuilder.Append("#EC:").Append(GetElectronContainers().Count()).Append(", ");
            foreach (var atom in Atoms)
            {
                PharmacophoreQueryAtom qatom = (PharmacophoreQueryAtom)atom;
                stringBuilder.Append(qatom.Symbol).Append(", ");
            }
            foreach (var bond in Bonds)
            {
                stringBuilder.Append(bond.ToString()).Append(", ");
            }
            stringBuilder.Append(')');
            return stringBuilder.ToString();
        }
    }
}
