/* Copyright (C) 2006-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
 *                    2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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
using System.Diagnostics;
using System.Linq;

namespace NCDK.Tools
{
    /// <summary>
    /// Provides methods for checking whether an atoms lone pair electrons are saturated
    /// with respect to a particular atom type.
    /// </summary>
    public interface ILonePairElectronChecker
        : IValencyChecker, IDeduceBondOrderTool
    {
        /// <summary>
        /// Saturates <paramref name="atom"/> in <paramref name="container"/> by adding the appropriate number lone pairs.
        /// </summary>
        void Saturate(IAtom atom, IAtomContainer container);
    }

    /// <summary>
    /// Provides methods for checking whether an atoms lone pair electrons are saturated
    /// with respect to a particular atom type.
    /// </summary>
    // @author         Miguel Rojas
    // @cdk.created    2006-04-01
    // @cdk.keyword    saturation
    // @cdk.keyword    atom, valency
    // @cdk.module     standard
    public class LonePairElectronChecker
        : ILonePairElectronChecker
    {
        private static readonly AtomTypeFactory factory = CDK.CdkAtomTypeFactory;
        
        public LonePairElectronChecker()
        {
        }

        /// <inheritdoc/>
        public bool IsSaturated(IAtomContainer ac)
        {
            Debug.WriteLine("Are all atoms saturated?");
            foreach (var atom in ac.Atoms)
            {
                if (!IsSaturated(atom, ac))
                    return false;
            }
            return true;
        }

        /// <inheritdoc/>
        public bool IsSaturated(IAtom atom, IAtomContainer ac)
        {
            var atomType = factory.GetAtomType(atom.AtomTypeName);
            var lpCount = atomType.GetProperty<int>(CDKPropertyName.LonePairCount);
            var foundLPCount = ac.GetConnectedLonePairs(atom).Count();
            return foundLPCount >= lpCount;
        }

        /// <inheritdoc/>
        public void Saturate(IAtomContainer atomContainer)
        {
            Trace.TraceInformation("Saturating atomContainer by adjusting lone pair electrons...");
            var allSaturated = IsSaturated(atomContainer);
            if (!allSaturated)
            {
                for (int i = 0; i < atomContainer.Atoms.Count; i++)
                {
                    Saturate(atomContainer.Atoms[i], atomContainer);
                }
            }
        }

        /// <inheritdoc/>
        public void Saturate(IAtom atom, IAtomContainer ac)
        {
            Trace.TraceInformation("Saturating atom by adjusting lone pair electrons...");
            var atomType = factory.GetAtomType(atom.AtomTypeName);
            var lpCount = atomType.GetProperty<int>(CDKPropertyName.LonePairCount);
            var missingLPs = lpCount - ac.GetConnectedLonePairs(atom).Count();

            for (int j = 0; j < missingLPs; j++)
            {
                var lp = atom.Builder.NewLonePair(atom);
                ac.LonePairs.Add(lp);
            }
        }
    }
}
