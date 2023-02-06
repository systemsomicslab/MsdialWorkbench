/* Copyright (C) 2007  Egon Willighagen
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

using NCDK.Config;

namespace NCDK.Tools
{
    /// <summary>
    /// Assumes CDK atom types to be detected and adds missing hydrogens based on the
    /// atom typing.
    /// </summary>
    // @author     egonw
    // @cdk.module valencycheck
    public class CDKValencyChecker
        : IValencyChecker
    {
        private static readonly AtomTypeFactory atomTypeList = CDK.CdkAtomTypeFactory;
        public static CDKValencyChecker Instance { get; } = new CDKValencyChecker();

        public bool IsSaturated(IAtomContainer atomContainer)
        {
            foreach (var atom in atomContainer.Atoms)
            {
                if (!IsSaturated(atom, atomContainer))
                    return false;
            }
            return true;
        }

        public bool IsSaturated(IAtom atom, IAtomContainer container)
        {
            var type = atomTypeList.GetAtomType(atom.AtomTypeName);
            if (type == null)
                throw new CDKException($"Atom type is not a recognized CDK atom type: {atom.AtomTypeName}");

            if (type.FormalNeighbourCount == null)
                throw new CDKException($"Atom type is too general; cannot decide the number of implicit hydrogen to add for: {atom.AtomTypeName}");

            if (type.GetProperty<object>(CDKPropertyName.PiBondCount) == null)
                throw new CDKException($"Atom type is too general; cannot determine the number of pi bonds for: {atom.AtomTypeName}");

            var bondOrderSum = container.GetBondOrderSum(atom);
            var maxBondOrder = container.GetMaximumBondOrder(atom);
            int? hcount = atom.ImplicitHydrogenCount == null ? 0 : atom.ImplicitHydrogenCount;

            int piBondCount = type.GetProperty<int?>(CDKPropertyName.PiBondCount).Value;
            int formalNeighborCount = type.FormalNeighbourCount.Value;

            int typeMaxBondOrder = piBondCount + 1;
            int typeBondOrderSum = formalNeighborCount + piBondCount;

            if (bondOrderSum + hcount == typeBondOrderSum && maxBondOrder.Numeric() <= typeMaxBondOrder)
            {
                return true;
            }
            return false;
        }
    }
}
