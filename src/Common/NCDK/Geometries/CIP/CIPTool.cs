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

using NCDK.Geometries.CIP.Rules;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Geometries.CIP
{
    /// <summary>
    /// Tool to help determine the R,S and stereochemistry definitions of a subset of the
    /// CIP rules <token>cdk-cite-Cahn1966</token>. The used set up sub rules are specified in the
    /// <see cref="CIPLigandRule"/> class.
    /// </summary>
    /// <example>
    /// Basic use starts from a <see cref="ITetrahedralChirality"/> and therefore
    /// assumes atoms with four neighbours:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Geometries.CIP.CIPTool_Example.cs"]/*' />
    /// The <see cref="BondStereo"/> value can be
    /// reconstructed from 3D coordinates with the <see cref="StereoElementTools"/>.
    /// </example>
    // @cdk.module cip
    public static class CIPTool
    {
        /// <summary>
        /// <see cref="IAtom"/> index to indicate an implicit hydrogen, not present in the chemical graph.
        /// </summary>
        public const int Hydrogen = -1;

        private static CIPLigandRule cipRule = new CIPLigandRule();

        /// <summary>
        /// Enumeration with the two tetrahedral chiralities defined by the CIP schema.
        /// </summary>
       // @author egonw
        public enum CIPChirality
        {
            None = 0,
            R, S, E, Z, 
        }

        /// <summary>
        /// Returns the R or S chirality according to the CIP rules, based on the given
        /// chirality information.
        /// </summary>
        /// <param name="stereoCenter">Chiral center for which the CIP chirality is to be
        ///                     determined as <see cref="LigancyFourChirality"/> object.</param>
        /// <returns>A <see cref="CIPChirality"/> value.</returns>
        public static CIPChirality GetCIPChirality(LigancyFourChirality stereoCenter)
        {
            var ligands = Order(stereoCenter.Ligands);
            var rsChirality = stereoCenter.Project(ligands);

            bool allAreDifferent = CheckIfAllLigandsAreDifferent(ligands);
            if (!allAreDifferent)
                return CIPChirality.None;

            if (rsChirality.Stereo == TetrahedralStereo.Clockwise)
                return CIPChirality.R;

            return CIPChirality.S;
        }

        /// <summary> 
        /// Property key to store the CIP descriptor label for an atom / bond. The
        /// label is a string.
        /// </summary>
        public const string CIPDescriptorPropertyKey = "cip.label";

        public static string GetCIPDescriptor(this IAtom o)
            => o.GetProperty<string>(CIPDescriptorPropertyKey);

        public static void SetCIPDescriptor(this IAtom o, string chirality)
            => o.SetProperty(CIPDescriptorPropertyKey, chirality);

        public static string GetCIPDescriptor(this IBond o)
            => o.GetProperty<string>(CIPDescriptorPropertyKey);

        public static void SetCIPDescriptor(this IBond o, string chirality)
            => o.SetProperty(CIPDescriptorPropertyKey, chirality);

        /// <summary>
        /// Convenience method for labelling all stereo elements. The <see cref="CIPChirality"/> is determined for each element and stored as as <see cref="string"/> 
        /// on the <see cref="CIPDescriptorPropertyKey"/> property key.
        /// Atoms/bonds that are not stereocenters have no label assigned and the
        /// property will be null.
        /// </summary>
        /// <param name="container">structure to label</param>
        public static void Label(IAtomContainer container)
        {
            foreach (var stereoElement in container.StereoElements)
            {
                if (stereoElement is ITetrahedralChirality tc)
                {
                    tc.ChiralAtom.SetCIPDescriptor(GetCIPChirality(container, tc).ToString());
                }
                else if (stereoElement is IDoubleBondStereochemistry dbs)
                {
                    dbs.StereoBond.SetCIPDescriptor(GetCIPChirality(container, dbs).ToString());
                }
            }
        }

        /// <summary>
        /// Returns the R or S chirality according to the CIP rules, based on the given
        /// chirality information.
        /// </summary>
        /// <param name="container"><see cref="IAtomContainer"/> to which the <paramref name="stereoCenter"/> belongs.</param>              
        /// <param name="stereoCenter">Chiral center for which the CIP chirality is to be determined as <see cref="ITetrahedralChirality"/> object.</param>
        /// <returns>A <see cref="CIPChirality"/> value.</returns>
        public static CIPChirality GetCIPChirality(IAtomContainer container, ITetrahedralChirality stereoCenter)
        {
            // the LigancyFourChirality is kind of redundant but we keep for an
            // easy way to get the ILigands array
            var tmp = new LigancyFourChirality(container, stereoCenter).ligands;
            var stereo = stereoCenter.Stereo;

            int parity = PermParity(tmp);

            if (parity == 0)
                return CIPChirality.None;
            if (parity < 0)
                stereo = stereo.Invert();

            if (stereo == TetrahedralStereo.Clockwise)
                return CIPChirality.R;
            if (stereo == TetrahedralStereo.AntiClockwise)
                return CIPChirality.S;

            return CIPChirality.None;
        }

        public static CIPChirality GetCIPChirality(IAtomContainer container, IDoubleBondStereochemistry stereoCenter)
        {
            IBond stereoBond = stereoCenter.StereoBond;
            IBond leftBond = stereoCenter.Bonds[0];
            IBond rightBond = stereoCenter.Bonds[1];

            // the following variables are usd to label the atoms - makes things
            // a little more concise
            //
            // x       y       x
            //  \     /         \
            //   u = v    or     u = v
            //                        \
            //                         y
            //
            var u = stereoBond.Begin;
            var v = stereoBond.End;
            var x = leftBond.GetOther(u);
            var y = rightBond.GetOther(v);

            var conformation = stereoCenter.Stereo;

            var leftLigands = GetLigands(u, container, v).ToList();
            var rightLigands = GetLigands(v, container, u).ToList();

            if (leftLigands.Count > 2 || rightLigands.Count > 2)
                return CIPChirality.None;

            // invert if x/y aren't in the first position
            if (leftLigands[0].LigandAtom != x)
                conformation = conformation.Invert();
            if (rightLigands[0].LigandAtom != y)
                conformation = conformation.Invert();

            int p = PermParity(leftLigands) * PermParity(rightLigands);

            if (p == 0)
                return CIPChirality.None;
            else if (p < 0)
                conformation = conformation.Invert();

            switch (conformation)
            {
                case DoubleBondConformation.Together:
                    return CIPChirality.Z;
                case DoubleBondConformation.Opposite:
                    return CIPChirality.E;
                default:
                    return CIPChirality.None;
            }
        }

        /// <summary>
        /// Obtain the ligands connected to the 'atom' excluding 'exclude'. This is
        /// mainly meant as a utility for double-bond labelling.
        /// </summary>
        /// <param name="atom">an atom</param>
        /// <param name="container">a structure to which 'atom' belongs</param>
        /// <param name="exclude">exclude this atom - can not be null</param>
        /// <returns>the ligands</returns>
        private static IEnumerable<ILigand> GetLigands(IAtom atom, IAtomContainer container, IAtom exclude)
        {
            var neighbors = container.GetConnectedAtoms(atom);

            var ligands = neighbors
                .Where(neighbor => neighbor != exclude)
                .Select(neighbor => new Ligand(container, new VisitedAtoms(), atom, neighbor));

            return ligands;
        }

        /// <summary>
        /// Checks if each next <see cref="ILigand"/> is different from the previous
        /// one according to the <see cref="CIPLigandRule"/>. It assumes that the input
        /// is sorted based on that rule.
        /// </summary>
        /// <param name="ligands">array of <see cref="ILigand"/> to check</param>
        /// <returns>true, if all ligands are different</returns>
        internal static bool CheckIfAllLigandsAreDifferent(IReadOnlyList<ILigand> ligands)
        {
            for (int i = 0; i < (ligands.Count - 1); i++)
            {
                if (cipRule.Compare(ligands[i], ligands[i + 1]) == 0)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Reorders the <see cref="ILigand"/> objects in the array according to the CIP rules.
        /// </summary>
        /// <param name="ligands">Array of <see cref="ILigand"/>s to be reordered.</param>
        /// <returns>Reordered array of <see cref="ILigand"/>s.</returns>
        internal static IReadOnlyList<ILigand> Order(IReadOnlyList<ILigand> ligands)
        {
            var newLigands = ligands.ToArray();
            Array.Sort(newLigands, cipRule);
            return newLigands;
        }

        /// <summary>
        /// Obtain the permutation parity (-1,0,+1) to put the ligands in descending
        /// order (highest first). A parity of 0 indicates two or more ligands were
        /// equivalent.
        /// </summary>
        /// <param name="ligands">the ligands to sort</param>
        /// <returns>parity, odd (-1), even (+1) or none (0)</returns>
        private static int PermParity(List<ILigand> ligands)
        {
            // count the number of swaps made by insertion sort - if duplicates
            // are fount the parity is 0
            int swaps = 0;

            for (int j = 1, hi = ligands.Count; j < hi; j++)
            {
                var ligand = ligands[j];
                int i = j - 1;
                int cmp = 0;
                while ((i >= 0) && (cmp = cipRule.Compare(ligand, ligands[i])) > 0)
                {
                    ligands[i + 1] = ligands[i--];
                    swaps++;
                }
                if (cmp == 0) // identical entries
                    return 0;
                ligands[i + 1] = ligand;
            }

            // odd (-1) or even (+1)
            return (swaps & 0x1) == 0x1 ? -1 : +1;
        }

        /// <summary>
        /// Creates a ligancy for chirality around a single chiral atom, where the involved
        /// atoms are identified by there index in the <see cref="IAtomContainer"/>. For the four ligand
        /// atoms, <see cref="Hydrogen"/> can be passed as index, which will indicate the presence of
        /// an implicit hydrogen, not explicitly present in the chemical graph of the
        /// given <paramref name="container"/>.
        /// </summary>
        /// <param name="container"><see cref="IAtomContainer"/> for which the returned <see cref="ILigand"/>s are defined</param>
        /// <param name="chiralAtom">int pointing to the <see cref="IAtom"/> index of the chiral atom</param>
        /// <param name="ligand1">int pointing to the <see cref="IAtom"/> index of the first <see cref="ILigand"/></param>
        /// <param name="ligand2">int pointing to the <see cref="IAtom"/> index of the second <see cref="ILigand"/></param>
        /// <param name="ligand3">int pointing to the <see cref="IAtom"/> index of the third <see cref="ILigand"/></param>
        /// <param name="ligand4">int pointing to the <see cref="IAtom"/> index of the fourth <see cref="ILigand"/></param>
        /// <param name="stereo"><see cref="TetrahedralStereo"/> for the chirality</param>
        /// <returns>the created <see cref="LigancyFourChirality"/></returns>
        public static LigancyFourChirality DefineLigancyFourChirality(IAtomContainer container, int chiralAtom, int ligand1, int ligand2, int ligand3, int ligand4, TetrahedralStereo stereo)
        {
            var atomIndices = new int[] { ligand1, ligand2, ligand3, ligand4 };
            var visitedAtoms = new VisitedAtoms();
            var ligands = new ILigand[4];
            for (int i = 0; i < 4; i++)
            {
                ligands[i] = DefineLigand(container, visitedAtoms, chiralAtom, atomIndices[i]);
            }
            return new LigancyFourChirality(container.Atoms[chiralAtom], ligands, stereo);
        }

        /// <summary>
        /// Creates a ligand attached to a single chiral atom, where the involved
        /// atoms are identified by there index in the <see cref="IAtomContainer"/>. For ligand
        /// atom, <see cref="Hydrogen"/> can be passed as index, which will indicate the presence of
        /// an implicit hydrogen, not explicitly present in the chemical graph of the
        /// given <paramref name="container"/>.
        /// </summary>
        /// <param name="container"><see cref="IAtomContainer"/> for which the returned <see cref="ILigand"/>s are defined</param>
        /// <param name="visitedAtoms">a list of atoms already visited in the analysis</param>
        /// <param name="chiralAtom">an integer pointing to the <see cref="IAtom"/> index of the chiral atom</param>
        /// <param name="ligandAtom">an integer pointing to the <see cref="IAtom"/> index of the <see cref="ILigand"/></param>
        /// <returns>the created <see cref="ILigand"/></returns>
        public static ILigand DefineLigand(IAtomContainer container, VisitedAtoms visitedAtoms, int chiralAtom, int ligandAtom)
        {
            if (ligandAtom == Hydrogen)
            {
                return new ImplicitHydrogenLigand(container, visitedAtoms, container.Atoms[chiralAtom]);
            }
            else
            {
                return new Ligand(container, visitedAtoms, container.Atoms[chiralAtom], container.Atoms[ligandAtom]);
            }
        }

        /// <summary>
        /// Returns a CIP-expanded array of side chains of a ligand. If the ligand atom is only connected to
        /// the chiral atom, the method will return an empty list. The expansion involves the CIP rules,
        /// so that a double bonded oxygen will be represented twice in the list.
        /// </summary>
        /// <param name="ligand">the <see cref="ILigand"/> for which to return the ILigands</param>
        /// <returns>a <see cref="ILigand"/> array with the side chains of the ligand atom</returns>
        public static IReadOnlyList<ILigand> GetLigandLigands(ILigand ligand)
        {
            if (ligand is TerminalLigand)
                return Array.Empty<ILigand>();

            var container = ligand.AtomContainer;
            var ligandAtom = ligand.LigandAtom;
            var centralAtom = ligand.CentralAtom;
            var visitedAtoms = ligand.VisitedAtoms;
            var bonds = container.GetConnectedBonds(ligandAtom);
            // duplicate ligands according to bond order, following the CIP rules
            var ligands = new List<ILigand>();
            foreach (var bond in bonds)
            {
                if (bond.Contains(centralAtom))
                {
                    if (BondOrder.Single == bond.Order)
                        continue;
                    int duplication = GetDuplication(bond.Order) - 1;
                    if (duplication > 0)
                    {
                        for (int i = 1; i <= duplication; i++)
                        {
                            ligands.Add(new TerminalLigand(container, visitedAtoms, ligandAtom, centralAtom));
                        }
                    }
                }
                else
                {
                    var duplication = GetDuplication(bond.Order);
                    var connectedAtom = bond.GetOther(ligandAtom);
                    if (visitedAtoms.IsVisited(connectedAtom))
                    {
                        ligands.Add(new TerminalLigand(container, visitedAtoms, ligandAtom, connectedAtom));
                    }
                    else
                    {
                        ligands.Add(new Ligand(container, visitedAtoms, ligandAtom, connectedAtom));
                    }
                    for (int i = 2; i <= duplication; i++)
                    {
                        ligands.Add(new TerminalLigand(container, visitedAtoms, ligandAtom, connectedAtom));
                    }
                }
            }
            return ligands;
        }

        /// <summary>
        /// Returns the number of times the side chain should end up as the CIP-expanded ligand list. The CIP
        /// rules prescribe that a double bonded oxygen should be represented twice in the list.
        /// </summary>
        /// <param name="order"><see cref="BondOrder"/> of the bond</param>
        /// <returns>int reflecting the duplication number</returns>
        private static int GetDuplication(BondOrder order)
        {
            return order.Numeric();
        }
    }
}
