/* Copyright (C) 2001-2007  Oliver Horlacher <oliver.horlacher@therastrat.com>
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

using NCDK.Smiles;
using NCDK.Tools;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace NCDK.Graphs.Invariant
{
    /// <summary>
    /// Canonically labels an atom container implementing
    /// the algorithm published in David Weininger et al. <token>cdk-cite-WEI89</token>.
    /// The Collections.Sort() method uses a merge sort which is
    /// stable and runs in n Log(n).
    /// </summary>
    // @cdk.module standard
    // @author   Oliver Horlacher <oliver.horlacher@therastrat.com>
    // @cdk.created  2002-02-26
    // @cdk.keyword canonicalization
    [Obsolete("this labeller uses slow data structures and has been replaced - " + nameof(Canon))]
    public class CanonicalLabeler
    {
        public CanonicalLabeler()
        {
        }

        /// <summary>
        /// Canonically label the fragment.  The labels are set as atom property InvPair.CANONICAL_LABEL of type int, indicating the canonical order.
        /// This is an implementation of the algorithm published in
        /// David Weininger et.al. <token>cdk-cite-WEI89</token>.
        /// <para>The Collections.Sort() method uses a merge sort which is
        /// stable and runs in n Log(n).
        /// </para>
        /// <para>It is assumed that a chemically valid AtomContainer is provided:
        /// this method does not check
        /// the correctness of the AtomContainer. Negative H counts will
        /// cause a FormatException to be thrown.
        /// </para>
        /// </summary>
        /// <param name="atomContainer">The molecule to label</param>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CanonLabel(IAtomContainer atomContainer)
        {
            if (atomContainer.Atoms.Count == 0)
                return;
            if (atomContainer.Atoms.Count == 1)
            {
                atomContainer.Atoms[0].SetProperty(InvPair.CanonicalLabelPropertyKey, 1);
            }

            var vect = CreateInvarLabel(atomContainer);
            Step3(vect, atomContainer);
        }

        /// <summary>
        /// </summary>
        /// <param name="v">the invariance pair vector</param>
        /// <param name="atoms"></param>
        private void Step2(List<InvPair> v, IAtomContainer atoms)
        {
            PrimeProduct(v, atoms);
            Step3(v, atoms);
        }

        /// <summary>
        /// </summary>
        /// <param name="v">the invariance pair vector</param>
        /// <param name="atoms"></param>
        private void Step3(List<InvPair> v, IAtomContainer atoms)
        {
            SortArrayList(v);
            RankArrayList(v);
            if (!IsInvPart(v))
            {
                Step2(v, atoms);
            }
            else
            {
                //On first pass save, partitioning as symmetry classes.
                if (((InvPair)v[v.Count - 1]).Curr < v.Count)
                {
                    BreakTies(v);
                    Step2(v, atoms);
                }
                // now apply the ranking
                foreach (var aV in v)
                {
                    ((InvPair)aV).Commit();
                }
            }
        }

        /// <summary>
        /// Create initial invariant labeling corresponds to step 1
        /// </summary>
        /// <returns>List containing the</returns>
        private static List<InvPair> CreateInvarLabel(IAtomContainer atomContainer)
        {
            var atoms = atomContainer.Atoms;
            StringBuilder inv;
            var vect = new List<InvPair>();
            foreach (var a in atoms)
            {
                inv = new StringBuilder();
                var connectedAtoms = atomContainer.GetConnectedAtoms(a).ToReadOnlyList();
                inv.Append(connectedAtoms.Count + (a.ImplicitHydrogenCount ?? 0)); //Num connections
                inv.Append(connectedAtoms.Count); //Num of non H bonds
                inv.Append(PeriodicTable.GetAtomicNumber(a.Symbol));

                double charge = a.Charge ?? 0;
                if (charge < 0) //Sign of charge
                    inv.Append(1);
                else
                    inv.Append(0); //Absolute charge
                inv.Append((int)Math.Abs((a.FormalCharge ?? 0))); //Hydrogen count
                inv.Append((a.ImplicitHydrogenCount ?? 0));
                vect.Add(new InvPair(long.Parse(inv.ToString(), NumberFormatInfo.InvariantInfo), a));
            }
            return vect;
        }

        /// <summary>
        /// Calculates the product of the neighbouring primes.
        /// </summary>
        /// <param name="v">the invariance pair vector</param>
        /// <param name="atomContainer"></param>
        private static void PrimeProduct(List<InvPair> v, IAtomContainer atomContainer)
        {
            long summ;
            foreach (var inv in v)
            {
                var neighbour = atomContainer.GetConnectedAtoms(inv.Atom);
                summ = 1;
                foreach (var a in neighbour)
                {
                    int next = a.GetProperty<InvPair>(InvPair.InvariancePairPropertyKey).Prime;
                    summ = summ * next;
                }
                inv.Last = inv.Curr;
                inv.Curr = summ;
            }
        }
        
        /// <summary>
        /// Sorts the vector according to the current invariance, corresponds to step 3
        /// </summary>
        /// <param name="v">the invariance pair vector</param>
        // @cdk.todo    can this be done in one loop?
        private static void SortArrayList(List<InvPair> v)
        {
            v.Sort(aSortArrayListCompareComparer);
            //v.Sort(SortArrayListCompareComparerCurr);
            //v.Sort(SortArrayListCompareComparerLast);
        }

        static readonly SortArrayListCompareComparer aSortArrayListCompareComparer = new SortArrayListCompareComparer();
        class SortArrayListCompareComparer : IComparer<InvPair>
        {
            public int Compare(InvPair o1, InvPair o2)
            {
                if (o1.Last > o2.Last) return +1;
                if (o1.Last < o2.Last) return -1;
                if (o1.Curr > o2.Curr) return +1;
                if (o1.Curr < o2.Curr) return -1;
                return 0;
            }
        }

        /// <summary>
        /// Rank atomic vector, corresponds to step 4.
        /// </summary>
        /// <param name="v">the invariance pair vector</param>
        private static void RankArrayList(List<InvPair> v)
        {
            int num = 1;
            var temp = new int[v.Count];
            InvPair last = (InvPair)v[0];
            int x;
            x = 0;
            foreach (var curr in v)
            {
                if (!last.Equals(curr))
                {
                    num++;
                }
                temp[x++] = num;
                last = curr;
            }
            x = 0;
            foreach (var curr in v)
            {
                curr.Curr = temp[x++];
                curr.SetPrime();
            }
        }

        /// <summary>
        /// Checks to see if the vector is invariantly partitioned
        /// </summary>
        /// <param name="v">the invariance pair vector</param>
        /// <returns>true if the vector is invariantly partitioned, false otherwise</returns>
        private static bool IsInvPart(List<InvPair> v)
        {
            if (v[v.Count - 1].Curr == v.Count) return true;
            foreach (var curr in v)
            {
                if (curr.Curr != curr.Last) return false;
            }
            return true;
        }

        /// <summary>
        /// Break ties. Corresponds to step 7
        /// </summary>
        /// <param name="v">the invariance pair vector</param>
        private static void BreakTies(List<InvPair> v)
        {
            InvPair last = null;
            int tie = 0;
            bool found = false;
            int x;
            x = 0;
            foreach (var curr in v)
            {
                curr.Curr = curr.Curr * 2;
                curr.SetPrime();
                if (x != 0 && !found && curr.Curr == last.Curr)
                {
                    tie = x - 1;
                    found = true;
                }
                last = curr;
                x++;
            }
            var v_tie = v[tie];
            v_tie.Curr = v_tie.Curr - 1;
            v_tie.SetPrime();
        }
    }
}
