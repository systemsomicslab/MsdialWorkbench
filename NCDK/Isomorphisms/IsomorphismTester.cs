/* Copyright (C) 2001-2007  The Chemistry Development Kit (CDK) project
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
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

using NCDK.Graphs.Invariant;
using System;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// A too simplistic implementation of an isomorphism test for chemical graphs.
    /// </summary>
    /// <remarks>
    /// <note type="important">As it uses the <see cref="MorganNumbersTools"/> it does not take bond order 
    /// into account. Alternatively, you can use the <see cref="UniversalIsomorphismTester"/>.</note>
    /// </remarks>
    /// <seealso cref="MorganNumbersTools"/>
    /// <seealso cref="UniversalIsomorphismTester"/>
    // @cdk.module standard
    // @author     steinbeck
    // @cdk.created    2001-09-10
    // @cdk.keyword    isomorphism
    public class IsomorphismTester
    {
        long[] baseTable;
        long[] sortedBaseTable;
        long[] compareTable;
        long[] sortedCompareTable;
        IAtomContainer base_ = null;
        IAtomContainer compare = null;

        public IsomorphismTester() { }

        public IsomorphismTester(IAtomContainer mol)
        {
            SetBaseTable(mol);
        }

        /// <summary>
        /// Checks whether a given molecule is isomorphic with the one
        /// that has been assigned to this IsomorphismTester at construction time.
        /// </summary>
        /// <param name="mol1">A first molecule to check against the second one</param>
        /// <param name="mol2">A second molecule to check against the first</param>
        /// <returns><see langword="true"/>, if the two molecules are isomorphic</returns>
        public bool IsIsomorphic(IAtomContainer mol1, IAtomContainer mol2)
        {
            SetBaseTable(mol1);
            return IsIsomorphic(mol2);
        }

        /// <summary>
        /// Checks whether a given molecule is isomorphic with the one
        /// that has been assigned to this IsomorphismTester at construction time.
        /// </summary>
        /// <param name="mol2">A molecule to check</param>
        /// <returns><see langword="true"/>, if the two molecules are isomorphic</returns>
        public bool IsIsomorphic(IAtomContainer mol2)
        {
            SetCompareTable(mol2);
            for (int f = 0; f < sortedBaseTable.Length; f++)
            {
                if (sortedBaseTable[f] != sortedCompareTable[f])
                {
                    return false;
                }
            }

            for (int f = 0; f < baseTable.Length; f++)
            {
                var found = false;
                for (int g = 0; g < compareTable.Length; g++)
                {
                    if (baseTable[f] == compareTable[g])
                    {
                        var atom1 = base_.Atoms[f];
                        var atom2 = compare.Atoms[g];
                        if (atom1.AtomicNumber != atom2.AtomicNumber
                         && atom1.ImplicitHydrogenCount == atom2.ImplicitHydrogenCount)
                        {
                            return false;
                        }
                        found = true;
                    }
                }
                if (!found)
                {
                    return false;
                }
            }
            return true;
        }

        private void SetBaseTable(IAtomContainer mol)
        {
            this.base_ = mol;
            this.baseTable = MorganNumbersTools.GetMorganNumbers(base_);
            sortedBaseTable = new long[baseTable.Length];
            Array.Copy(baseTable, 0, sortedBaseTable, 0, baseTable.Length);
            Array.Sort(sortedBaseTable);
        }

        private void SetCompareTable(IAtomContainer mol)
        {
            this.compare = mol;
            this.compareTable = MorganNumbersTools.GetMorganNumbers(compare);
            sortedCompareTable = new long[compareTable.Length];
            Array.Copy(compareTable, 0, sortedCompareTable, 0, compareTable.Length);
            Array.Sort(sortedCompareTable);
        }
    }
}
