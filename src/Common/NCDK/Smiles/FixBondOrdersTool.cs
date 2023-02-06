/*  Copyright (C)  2012  Kevin Lawson <kevin.lawson@syngenta.com>
 *                       Lucy Entwistle <lucy.entwistle@syngenta.com>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All I ask is that proper credit is given for my work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Graphs;
using System;
using System.Collections.Generic;

namespace NCDK.Smiles
{
    /// <summary>
    /// Class to Fix bond orders at present for aromatic rings only.
    /// </summary>
    /// <remarks>
    /// Contains one public function: KekuliseAromaticRings(IAtomContainer molecule)
    /// <list type="bullet">
    /// <item>Analyses which rings are marked <see cref="IMolecularEntity.IsAromatic"/>/<see cref="Hybridization.SP2"/>/<see cref="Hybridization.Planar3"/></item>
    /// <item>Splits rings into groups containing independent sets of single/fused rings</item>
    /// <item>Loops over each ring group</item>
    /// <item>Uses an adjacency matrix of bonds (rows) and atoms (columns) to represent
    /// each fused ring system</item>
    /// <item>Scans the adjacency matrix for bonds for which there
    /// is no order choice (eg - both bonds to the NH of pyrrole must be single)</item>
    /// <item>All choices made to match valency against bonds used (including implicit H atoms)</item>
    /// <item>Solves other bonds as possible - dependent on previous choices - makes free
    /// (random) choices only where necessary and possible</item>
    /// <item>Makes assumption that where there is a choice in bond order
    /// (not forced by previous choices) - either choice is consistent with correct solution</item>
    ///
    /// <item>Requires molecule with all rings to be solved being marked aromatic
    /// (<see cref="Hybridization.SP2"/>/<see cref="Hybridization.Planar3"/> atoms). All bonds to non-ring atoms need to be fully defined
    /// (including implicit H atoms)</item>
    /// </list>
    /// </remarks>
    // @author Kevin Lawson
    // @author Lucy Entwistle
    // @cdk.module smiles
    [Obsolete("Use " + nameof(Aromaticities.Kekulization))]
    public static class FixBondOrdersTool
    {
        private class Matrix
        {
            private readonly int[] mArray;
            private readonly int rowCount;
            private readonly int columnCount;

            public Matrix(int rows, int cols)
            {
                //Single array of size rows * cols in matrix
                mArray = new int[rows * cols];

                //Record no of rows and number of columns
                rowCount = rows;
                columnCount = cols;
            }

            public int this[int rIndex, int cIndex]
            {
                set
                {
                    mArray[rIndex * columnCount + cIndex] = value;
                }
                get
                {
                    return mArray[rIndex * columnCount + cIndex];
                }
            }

            public int ColIndexOf(int colIndex, int val)
            {
                for (int i = 0; i < rowCount; i++)
                {
                    if (mArray[i * columnCount + colIndex] == val)
                    {
                        return i;
                    }
                }
                return -1;
            }

            public int RowIndexOf(int rowIndex, int val)
            {
                for (int i = 0; i < columnCount; i++)
                {
                    if (mArray[rowIndex * GetCols() + i] == val)
                    {
                        return i;
                    }
                }
                return -1;
            }

            public int SumOfRow(int rowIndex)
            {
                int sumOfRow = 0;
                for (int i = 0; i < columnCount; i++)
                {
                    sumOfRow += mArray[rowIndex * columnCount + i];
                }
                return sumOfRow;
            }

            public int GetRows()
            {
                return rowCount;
            }

            public int GetCols()
            {
                return columnCount;
            }
        }

        /// <summary>
        /// Function to add double/single bond order information for molecules having rings containing all atoms marked <see cref="Hybridization.SP2"/> or <see cref="Hybridization.Planar3"/> hybridisation.
        /// </summary>
        /// <param name="molecule">The <see cref="IAtomContainer"/> to kekulise</param>
        /// <returns>The <see cref="IAtomContainer"/> with Kekulé structure</returns>
        public static IAtomContainer KekuliseAromaticRings(IAtomContainer molecule)
        {
            IAtomContainer mNew = null;
            mNew = (IAtomContainer)molecule.Clone();

            IRingSet ringSet;

            try
            {
                ringSet = RemoveExtraRings(mNew);
            }
            catch (CDKException)
            {
                throw;
            }
            catch (Exception x)
            {
                throw new CDKException("failure in SSSRFinder.findAllRings", x);
            }

            if (ringSet == null)
            {
                throw new CDKException("failure in SSSRFinder.findAllRings");
            }

            //We need to establish which rings share bonds and set up sets of such interdependant rings
            List<int[]> rBondsArray = null;
            List<List<int>> ringGroups = null;

            //Start by getting a list (same dimensions and ordering as ring set) of all the ring bond numbers in the reduced ring set
            rBondsArray = GetRingSystem(mNew, ringSet);
            //Now find out which share a bond and assign them accordingly to groups
            ringGroups = AssignRingGroups(rBondsArray);

            //Loop through each group of rings checking all choices of double bond combis and seeing if you can get a
            //proper molecule.
            for (int i = 0; i < ringGroups.Count; i++)
            {
                //Set all ring bonds with single order to allow Matrix solving to work
                SetAllRingBondsSingleOrder(ringGroups[i], ringSet);

                //Set up  lists of atoms, bonds and atom pairs for this ringGroup
                List<int> atomNos = null;
                atomNos = GetAtomNosForRingGroup(mNew, ringGroups[i], ringSet);

                List<int> bondNos = null;
                bondNos = GetBondNosForRingGroup(mNew, ringGroups[i], ringSet);

                //Array of same dimensions as bondNos (cols in Matrix)
                List<int[]> atomNoPairs = null;
                atomNoPairs = GetAtomNoPairsForRingGroup(mNew, bondNos);

                //Set up adjacency Matrix
                Matrix M = new Matrix(atomNos.Count, bondNos.Count);
                for (int x = 0; x < M.GetRows(); x++)
                {
                    for (int y = 0; y < M.GetCols(); y++)
                    {
                        if (atomNos[x] == atomNoPairs[y][0])
                        {
                            M[x, y] = 1;
                        }
                        else
                        {
                            if (atomNos[x] == atomNoPairs[y][1])
                            {
                                M[x, y] = 1;
                            }
                            else
                            {
                                M[x, y] = 0;
                            }
                        }
                    }
                }

                //Array of same dimensions as atomNos (rows in Matrix)
                List<int> freeValencies = null;
                freeValencies = GetFreeValenciesForRingGroup(mNew, atomNos, M, ringSet);

                //Array of "answers"
                List<int> bondOrders = new List<int>();
                for (int j = 0; j < bondNos.Count; j++)
                {
                    bondOrders.Add(0);
                }

                if (SolveMatrix(M, atomNos, bondNos, freeValencies, atomNoPairs, bondOrders))
                {
                    for (int j = 0; j < bondOrders.Count; j++)
                    {
                        mNew.Bonds[bondNos[j]].Order =
                                bondOrders[j] == 1 ? BondOrder.Single : BondOrder.Double;
                    }
                }
                else
                {
                    //                TODO Put any failure code here
                }
            }
            return mNew;
        }

        /// <summary>
        /// Removes rings which do not have all sp2/planar3 aromatic atoms.
        /// and also gets rid of rings that have more than 8 atoms in them.
        /// </summary>
        /// <param name="m">The <see cref="IAtomContainer"/> from which we want to remove rings</param>
        /// <returns>The set of reduced rings</returns>
        private static IRingSet RemoveExtraRings(IAtomContainer m)
        {
            IRingSet rs = Cycles.FindSSSR(m).ToRingSet();

            //remove rings which don't have all aromatic atoms (according to hybridization set by lower case symbols in smiles):
            var rToRemove = new List<int>();
            for (int i = 0; i < rs.Count; i++)
            {
                if (rs[i].Atoms.Count > 8)
                {
                    rToRemove.Add(i);
                }
                else
                {
                    foreach (var a in rs[i].Atoms)
                    {
                        Hybridization h = a.Hybridization;
                        if (h.IsUnset() || !(h == Hybridization.SP2 || h == Hybridization.Planar3))
                        {
                            rToRemove.Add(i);
                            break;
                        }
                    }
                }
            }
            rToRemove.Reverse();
            foreach (var ri in rToRemove)
                rs.RemoveAt(ri);
            return rs;
        }

        /// <summary>
        /// Stores an <see cref="IRingSet"/> corresponding to a molecule using the bond numbers.
        /// </summary>
        /// <param name="mol">The IAtomContainer for which to store the IRingSet.</param>
        /// <param name="ringSet">The IRingSet to store</param>
        /// <returns>The List of int arrays for the bond numbers of each ringSet</returns>
        private static List<int[]> GetRingSystem(IAtomContainer mol, IRingSet ringSet)
        {
            List<int[]> bondsArray;
            bondsArray = new List<int[]>();
            for (int r = 0; r < ringSet.Count; ++r)
            {
                IRing ring = (IRing)ringSet[r];
                int[] bondNumbers = new int[ring.Bonds.Count];
                for (int i = 0; i < ring.Bonds.Count; ++i)
                {
                    bondNumbers[i] = mol.Bonds.IndexOf(ring.Bonds[i]);
                }
                bondsArray.Add(bondNumbers);
            }
            return bondsArray;
        }

        /// <summary>
        /// Assigns a set of rings to groups each sharing a bond.
        /// </summary>
        /// <param name="rBondsArray"></param>
        /// <returns>A List of Lists each containing the ring indices of a set of fused rings</returns>
        private static List<List<int>> AssignRingGroups(List<int[]> rBondsArray)
        {
            List<List<int>> ringGroups;
            ringGroups = new List<List<int>>();
            for (int i = 0; i < rBondsArray.Count - 1; i++)
            { //for each ring except the last in rBondsArray
                for (int j = 0; j < rBondsArray[i].Length; j++)
                { //for each bond in each ring
                    //check there's no shared bond with any other ring already in ringGroups
                    for (int k = i + 1; k < rBondsArray.Count; k++)
                    {
                        for (int l = 0; l < rBondsArray[k].Length; l++)
                        { //for each ring in each ring
                            //Is there a bond in common? Then add both rings
                            if (rBondsArray[i][j] == rBondsArray[k][l])
                            {
                                if (i != k)
                                {
                                    ringGroups.Add(new List<int>());
                                    ringGroups[ringGroups.Count - 1].Add(i);
                                    ringGroups[ringGroups.Count - 1].Add(k);
                                }
                            }
                        }
                    }
                }
            }
            while (CombineGroups(ringGroups)) ;

            //Anything not added yet is a singleton
            for (int i = 0; i < rBondsArray.Count; i++)
            {
                bool found = false;
                for (int j = 0; j < ringGroups.Count; j++)
                {
                    if (ringGroups[j].Contains(i))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    ringGroups.Add(new List<int>());
                    ringGroups[ringGroups.Count - 1].Add(i);
                }
            }
            return ringGroups;
        }

        private static bool CombineGroups(List<List<int>> ringGroups)
        {
            for (int i = 0; i < ringGroups.Count - 1; i++)
            {
                //Look for another group to combine with it
                for (int j = i + 1; j < ringGroups.Count; j++)
                {
                    for (int k = 0; k < ringGroups[j].Count; k++)
                    {
                        if (ringGroups[i].Contains(ringGroups[j][k]))
                        {
                            //Add all the new elements
                            for (int l = 0; l < ringGroups[j].Count; l++)
                            {
                                if (!ringGroups[i].Contains(ringGroups[j][l]))
                                {
                                    ringGroups[i].Add(ringGroups[j][l]);
                                }
                            }
                            ringGroups.RemoveAt(j);
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Sets all bonds in an <see cref="IRingSet"/> to single order.
        /// </summary>
        /// <returns>True for success</returns>
        private static bool SetAllRingBondsSingleOrder(List<int> ringGroup, IRingSet ringSet)
        {
            foreach (var i in ringGroup)
            {
                foreach (var bond in ringSet[i].Bonds)
                {
                    bond.Order = BondOrder.Single;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets the List of atom nos corresponding to a particular set of fused rings.
        /// </summary>
        /// <returns>List of atom numbers for each set</returns>
        private static List<int> GetAtomNosForRingGroup(IAtomContainer molecule, List<int> ringGroup, IRingSet ringSet)
        {
            List<int> atc = new List<int>();
            foreach (var i in ringGroup)
            {
                foreach (var atom in ringSet[i].Atoms)
                {
                    if (atc.Count > 0)
                    {
                        if (!atc.Contains(molecule.Atoms.IndexOf(atom)))
                        {
                            atc.Add(molecule.Atoms.IndexOf(atom));
                        }
                    }
                    else
                    {
                        atc.Add(molecule.Atoms.IndexOf(atom));
                    }
                }
            }
            return atc;
        }

        /// <summary>
        /// Gets the List of bond nos corresponding to a particular set of fused rings.
        /// </summary>
        /// <returns>List of bond numbers for each set</returns>
        private static List<int> GetBondNosForRingGroup(IAtomContainer molecule, List<int> ringGroup, IRingSet ringSet)
        {
            List<int> btc = new List<int>();
            foreach (var i in ringGroup)
            {
                foreach (var bond in ringSet[i].Bonds)
                {
                    if (btc.Count > 0)
                    {
                        if (!btc.Contains(molecule.Bonds.IndexOf(bond)))
                        {
                            btc.Add(molecule.Bonds.IndexOf(bond));
                        }
                    }
                    else
                    {
                        btc.Add(molecule.Bonds.IndexOf(bond));
                    }
                }
            }
            return btc;
        }

        /// <summary>
        /// Gets List of atom number pairs for each bond in a list of bonds for the molecule.
        /// </summary>
        /// <returns>List of atom pairs</returns>
        private static List<int[]> GetAtomNoPairsForRingGroup(IAtomContainer molecule, List<int> bondsToCheck)
        {
            List<int[]> aptc = new List<int[]>();
            foreach (var i in bondsToCheck)
            {
                int[] aps = new int[2];
                aps[0] = molecule.Atoms.IndexOf(molecule.Bonds[i].Atoms[0]);
                aps[1] = molecule.Atoms.IndexOf(molecule.Bonds[i].Atoms[1]);
                aptc.Add(aps);
            }
            return aptc;
        }

        /// <summary>
        /// Function to set up an array of integers corresponding to indicate how many free valencies need fulfilling for each atom through ring bonds.
        /// </summary>
        /// <returns>The List of free valencies available for extra ring bonding</returns>
        private static List<int> GetFreeValenciesForRingGroup(IAtomContainer molecule, List<int> atomsToCheck, Matrix M, IRingSet rs)
        {
            List<int> fvtc = new List<int>();
            for (int i = 0; i < atomsToCheck.Count; i++)
            {
                int j = atomsToCheck[i];

                //Put in an implicit hydrogen atom for Planar3 C- atoms in 5-membered rings (it doesn't get put in by the Smiles parser)
                if (string.Equals("C", molecule.Atoms[j].Symbol, StringComparison.Ordinal)
                        && (molecule.Atoms[j].Hybridization == Hybridization.Planar3))
                {
                    //Check that ring containing the atom is five-membered
                    foreach (var ac in rs)
                    {
                        if (ac.Contains(molecule.Atoms[j]))
                        {
                            if ((int)molecule.GetBondOrderSum(molecule.Atoms[j]) == 2 && ac.Atoms.Count == 5)
                            {
                                molecule.Atoms[j].ImplicitHydrogenCount = 1;
                                break;
                            }
                        }
                    }
                }
                int implicitH = 0;
                if (!molecule.Atoms[j].ImplicitHydrogenCount.HasValue)
                {
                    var ha = CDK.HydrogenAdder;
                    try
                    {
                        ha.AddImplicitHydrogens(molecule, molecule.Atoms[j]);
                        implicitH = molecule.Atoms[j].ImplicitHydrogenCount.Value;
                    }
                    catch (CDKException)
                    {
                        //No need to do anything because implicitH already set to 0
                    }
                }
                else
                {
                    implicitH = molecule.Atoms[j].ImplicitHydrogenCount.Value;
                }
                fvtc.Add(molecule.Atoms[j].Valency.Value - (implicitH + (int)molecule.GetBondOrderSum(molecule.Atoms[j])) + M.SumOfRow(i));
            }
            return fvtc;
        }

        /// <summary>
        /// Function to solve the adjacency Matrix.
        /// Returns true/false on success/failure.
        /// Passed a reference to an array of bond orders to be filled in.
        /// Passed a setup Matrix M indicating the atoms that are part of each bond.
        /// The system v = Mb represents the set of equations: valence[atomA] = SUM
        /// OF ( M[A][B]*bondOrder[bondB] ) where M[A][B] = 1 if atom A is part of
        /// bond B, and M[A][B] = 0 otherwise. Use the system to solve bondOrder. For
        /// example if atom 1 has free valence 2, and is part of bonds 5 and 6, we
        /// know that B5 = 1, B6 = 1 if then also, atom 2 has free valence 3, and is
        /// part of bond 5 and bond 9, we know, from the solved equation above that
        /// B9 = 2. And so forth.
        ///
        /// If nothing can be deduced from previously solved equations, the code
        /// assigns a 1 to the first unknown bond it finds in the bondOrder array and
        /// continues.
        /// </summary>
        /// <returns>True or false for success or failure</returns>
        private static bool SolveMatrix(Matrix M, List<int> atomNos, List<int> bondNos, List<int> freeValencies, List<int[]> atomNoPairs, List<int> bondOrder)
        {
            // Look for bonds that need to be a certain order
            List<int> solved = new List<int>();
            List<int> solvedRow = new List<int>();
            for (int j = 0; j < atomNos.Count; j++)
            {
                // Count no.of bonds for this atom
                int sumOfRow = M.SumOfRow(j);

                // Atom with no of bonds equal to its valence - all must be single bonds.
                if (sumOfRow == freeValencies[j])
                {
                    for (int k = 0; k < bondNos.Count; k++)
                    {
                        if (M[j, k] == 1)
                        {
                            bondOrder[k] = 1;
                            solved.Add(k);
                        }
                    }
                    solvedRow.Add(j);
                } // Atom with only one bond - bond must be equal to atom valence.
                else if (sumOfRow == 1)
                {
                    for (int k = 0; k < bondNos.Count; k++)
                    {
                        if (M[j, k] == 1)
                        {
                            bondOrder[k] = freeValencies[j];
                            solved.Add(k);
                        }
                    }
                    solvedRow.Add(j);
                }
            }

            // thisRun indicates whether any bonds have been solved on this run
            // through the Matrix. Loop continues until all bonds have been solved
            // or there is a run where no bonds were solved, showing that the
            // structure is unsolvable.
            int thisRun = 1;
            while (solvedRow.Count != M.GetRows() && thisRun == 1)
            {
                thisRun = 0;
                if (solved.Count > 0)
                {
                    for (int j = 0; j < M.GetRows(); j++)
                    {
                        if (solvedRow.Contains(j) == false)
                        {
                            int unknownBonds = 0;
                            int knownBondTotal = 0;
                            for (int k = 0; k < bondNos.Count; k++)
                            {
                                if (M[j, k] == 1)
                                {
                                    if (solved.Contains(k))
                                    {
                                        knownBondTotal += bondOrder[k];
                                    }
                                    else
                                    {
                                        unknownBonds++;
                                    }
                                }
                            }

                            // have any bonds for this atom been solved?
                            if (unknownBonds == 0)
                            {
                                solvedRow.Add(j);
                                thisRun = 1;
                            }
                            else
                            {
                                if (knownBondTotal != 0)
                                {
                                    if (unknownBonds == freeValencies[j] - knownBondTotal)
                                    {
                                        // all remaining bonds must be single
                                        for (int k = 0; k < bondNos.Count; k++)
                                        {
                                            if (M[j, k] == 1 && solved.Contains(k) == false)
                                            {
                                                bondOrder[k] = 1;
                                                solved.Add(k);
                                            }
                                        }
                                        solvedRow.Add(j);
                                        thisRun = 1;
                                    }
                                    else if (unknownBonds == 1)
                                    {
                                        // only one unsolved bond, so must equal remaining free valence
                                        for (int k = 0; k < bondNos.Count; k++)
                                        {
                                            if (M[j, k] == 1 && solved.Contains(k) == false)
                                            {
                                                bondOrder[k] = freeValencies[j] - knownBondTotal;
                                                solved.Add(k);
                                            }
                                        }
                                        solvedRow.Add(j);
                                        thisRun = 1;
                                    }
                                }
                            }
                        }
                    }
                }

                // If we can't solve any bonds from the information we have so far, there must be a choice to make.
                // Pick a bond that is yet to be solved and set it as a single bond.
                if (thisRun == 0)
                {
                    int ring = 1;
                    int j = 0;
                    while (ring == 1 && j < bondNos.Count)
                    {
                        int badChoice = 0;
                        if (solvedRow.Contains(atomNos.IndexOf(atomNoPairs[j][0])))
                        {
                            badChoice = 1;
                        }
                        if (solvedRow.Contains(atomNos.IndexOf(atomNoPairs[j][1])))
                        {
                            badChoice = 1;
                        }
                        if (bondOrder[j] == 0 && badChoice == 0)
                        {
                            //                            javax.swing.JOptionPane.ShowMessageDialog(null, j);
                            bondOrder[j] = 1;
                            ring = 0;
                            thisRun = 1;
                            solved.Add(j);
                        }
                        j++;
                    }
                }
            }
            if (solvedRow.Count != M.GetRows())
            {
                return false;
            }
            else
            {
                int errorFound = 0;
                for (int j = 0; j < atomNos.Count; j++)
                {
                    int checker = 0;
                    for (int k = 0; k < bondNos.Count; k++)
                    {
                        checker += M[j, k] * bondOrder[k];
                    }
                    if (checker != freeValencies[j])
                    {
                        errorFound = 1;
                    }
                }
                if (errorFound == 1)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
