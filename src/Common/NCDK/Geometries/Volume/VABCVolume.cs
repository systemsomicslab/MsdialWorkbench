/* Copyright (C) 2011  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Aromaticities;
using NCDK.Config;
using NCDK.Graphs;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Geometries.Volume
{
    /// <summary>
    /// Calculates the Van der Waals volume using the method proposed
    /// in <token>cdk-cite-Zhao2003</token>. The method is limited to molecules
    /// with the following elements: H, C, N, O, F, Cl, Br, I,
    /// P, S, As, B, Si, Se, and Te.
    /// </summary>
    // @cdk.module   standard
    // @cdk.keyword  volume, molecular
    public static class VABCVolume
    {
        /// <summary>
        /// Values are taken from the spreadsheet where possible. The values in the
        /// paper are imprecise.
        /// </summary>
        private static readonly Dictionary<string, double> bondiiVolumes = new Dictionary<string, double>()
            {
                { "H", 7.2382293504 },
                { "C", 20.5795259250667 },
                { "N", 15.5985308577667 },
                { "O", 14.7102267005611 },
                { "Cl", 22.4492971208333 },
                { "Br", 26.5218483279667 },
                { "F", 13.3057882007064 },
                { "I", 32.5150310206656 },
                { "S", 24.4290240576 },
                { "P", 24.4290240576 },
                { "As", 26.5218483279667 },
                { "B", 40.48 },  // value missing from spreadsheet; taken from paper
                { "Se", 28.7309115245333 },
                { "Si", 38.7923854248 },
            };

        private static readonly AtomTypeFactory atomTypeList = CDK.CdkAtomTypeFactory;

        /// <summary>
        /// Calculates the volume for the given <see cref="IAtomContainer"/>. This methods assumes
        /// that atom types have been perceived.
        /// </summary>
        /// <param name="molecule"><see cref="IAtomContainer"/> to calculate the volume of.</param>
        /// <returns>the volume in cubic Ångström.</returns>
        public static double Calculate(IAtomContainer molecule)
        {
            double sum = 0.0;
            int totalHCount = 0;
            foreach (var atom in molecule.Atoms)
            {
                if (!bondiiVolumes.TryGetValue(atom.Symbol, out double bondiiVolume))
                    throw new CDKException("Unsupported element.");

                sum += bondiiVolume;

                // add volumes of implicit hydrogens?
                var type = atomTypeList.GetAtomType(atom.AtomTypeName);
                if (type == null)
                    throw new CDKException($"Unknown atom type for atom: {atom.Symbol}");
                if (type.FormalNeighbourCount == null)
                    throw new CDKException($"Formal neighbor count not given for : {type.AtomTypeName}");
                int hCount = type.FormalNeighbourCount.Value - molecule.GetConnectedBonds(atom).Count();
                sum += hCount * bondiiVolumes["H"];
                totalHCount += hCount;
            }
            sum -= 5.92 * (molecule.Bonds.Count + totalHCount);

            Aromaticity.CDKLegacy.Apply(molecule);
            var ringSet = Cycles.FindSSSR(molecule).ToRingSet();
            if (ringSet.Count() > 0)
            {
                int aromRingCount = 0;
                int nonAromRingCount = 0;
                foreach (var ring in ringSet)
                {
                    if (RingIsAromatic(ring))
                    {
                        aromRingCount++;
                    }
                    else
                    {
                        nonAromRingCount++;
                    }
                }

                sum -= 14.7 * aromRingCount;
                sum -= 3.8 * nonAromRingCount;
            }

            return sum;
        }

        private static bool RingIsAromatic(IAtomContainer ring)
        {
            foreach (var atom in ring.Atoms)
            {
                if (!atom.IsAromatic)
                    return false;
            }
            return true;
        }
    }
}
