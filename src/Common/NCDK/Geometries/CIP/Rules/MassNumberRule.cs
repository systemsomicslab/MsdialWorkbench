/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
 *               2014  Mark B Vine (orcid:0000-0002-7794-0426)
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

using NCDK.Config;

namespace NCDK.Geometries.CIP.Rules
{
    /// <summary>
    /// Compares to <see cref="ILigand"/>s based on mass numbers.
    /// </summary>
    // @cdk.module cip
    internal class MassNumberRule : ISequenceSubRule<ILigand>
    {
        IsotopeFactory factory = CDK.IsotopeFactory;

        public int Compare(ILigand ligand1, ILigand ligand2)
        {
            return GetMassNumber(ligand1).CompareTo(GetMassNumber(ligand2));
        }

        private int GetMassNumber(ILigand ligand)
        {
            var massNumber = ligand.LigandAtom.MassNumber;
            if (massNumber != null)
                return massNumber.Value;
            return factory.GetMajorIsotope(ligand.LigandAtom.Symbol).MassNumber.Value;
        }
    }
}
