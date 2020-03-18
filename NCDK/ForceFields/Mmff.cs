/*
 * Copyright (c) 2015 John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using NCDK.Graphs;
using System;
using System.Collections.Generic;

namespace NCDK.ForceFields
{
    /// <summary>
    /// Facade to access Merck Molecular Force Field (MMFF) functions.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    ///     <item><token>cdk-cite-Halgren96a</token></item>
    ///     <item><token>cdk-cite-Halgren96b</token></item>
    ///     <item><token>cdk-cite-Halgren96c</token></item>
    ///     <item><token>cdk-cite-Halgren96d</token></item>
    ///     <item><token>cdk-cite-Halgren96e</token></item>     
    /// </list>
    /// 
    /// <h4>Atom Types</h4>
    /// <para>
    /// Symbolic atom types are assigned with <see cref="Mmff.AssignAtomTypes(IAtomContainer)"/>.
    /// The atom type name can be accessed with <see cref="IAtomType.AtomTypeName"/>.
    /// </para>
    /// <h4>Partial Charges</h4>
    /// <para>
    /// Partial charges are assigned with <see cref="Mmff.PartialCharges(IAtomContainer)"/>.
    /// Atom types must be assigned before calling this function. Effective formal
    /// charges can also be obtained with <see cref="Mmff.EffectiveCharges(IAtomContainer)"/>
    /// both charge values are accessed with <see cref="IAtom.Charge"/>. Atoms of
    /// unknown type are assigned a neutral charge - to avoid this check the return
    /// value of <see cref="Mmff.AssignAtomTypes(IAtomContainer)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.ForceFields.Mmff.cs"]/*' />
    /// </example>
    // @author John May
    public class Mmff
    {
        private const string MMFF_ADJLIST_CACHE = "mmff.adjlist.cache";
        private const string MMFF_EDGEMAP_CACHE = "mmff.edgemap.cache";
        private const string MMFF_AROM = "mmff.arom";

        private readonly MmffAtomTypeMatcher mmffAtomTyper = new MmffAtomTypeMatcher();
        private readonly MmffParamSet mmffParamSet = MmffParamSet.Instance;

        /// <summary>
        /// Assign MMFF Symbolic atom types. The symbolic type can be accessed with
        /// <see cref="IAtomType.AtomTypeName"/>. An atom of unknown type is assigned the
        /// symbolic type <c>"UNK"</c>.
        /// All atoms, including hydrogens must be explicitly represented.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <returns>all atoms had a type assigned</returns>
        public virtual bool AssignAtomTypes(IAtomContainer mol)
        {
            // preconditions need explicit hydrogens
            foreach (var atom in mol.Atoms)
            {
                if (atom.ImplicitHydrogenCount == null || atom.ImplicitHydrogenCount > 0)
                    throw new ArgumentException("Hydrogens must be explicit nodes, each must have a zero (non-null) impl H count.");
            }

            // conversion to faster data structures
            var edgeMap = EdgeToBondMap.WithSpaceFor(mol);
            var adjList = GraphUtil.ToAdjList(mol, edgeMap);

            mol.SetProperty(MMFF_ADJLIST_CACHE, adjList);
            mol.SetProperty(MMFF_EDGEMAP_CACHE, edgeMap);

            var aromBonds = new HashSet<IBond>();
            var oldArom = GetAromatics(mol);

            // note: for MMFF we need to remove current aromatic flags for type
            // assignment (they are restored after)
            foreach (var chemObj in oldArom)
                chemObj.IsAromatic = false;
            var atomTypes = mmffAtomTyper.SymbolicTypes(mol, adjList, edgeMap, aromBonds);

            bool hasUnkType = false;
            for (int i = 0; i < mol.Atoms.Count; i++)
            {
                if (atomTypes[i] == null)
                {
                    mol.Atoms[i].AtomTypeName = "UNK";
                    hasUnkType = true;
                }
                else
                {
                    mol.Atoms[i].AtomTypeName = atomTypes[i];
                }
            }

            // restore aromatic flags and mark the MMFF aromatic bonds
            foreach (var chemObj in oldArom)
                chemObj.IsAromatic = true;
            foreach (var bond in aromBonds)
                bond.SetProperty(MMFF_AROM, true);

            return !hasUnkType;
        }

        /// <summary>
        /// Assign the effective formal charges used by MMFF in calculating the
        /// final partial charge values. Atom types must be assigned first. All 
        /// existing charges are cleared.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <returns>charges were assigned</returns>
        /// <seealso cref="PartialCharges(IAtomContainer)"/>
        /// <seealso cref="AssignAtomTypes(IAtomContainer)"/>
        public virtual bool EffectiveCharges(IAtomContainer mol)
        {
            var adjList = mol.GetProperty<int[][]>(MMFF_ADJLIST_CACHE);
            var edgeMap = mol.GetProperty<EdgeToBondMap>(MMFF_EDGEMAP_CACHE);

            if (adjList == null || edgeMap == null)
                throw new ArgumentException("Invoke assignAtomTypes first.");

            PrimaryCharges(mol, adjList, edgeMap);
            EffectiveCharges(mol, adjList);

            return true;
        }

        /// <summary>
        /// Assign the partial charges, all existing charges are cleared.
        /// Atom types must be assigned first. 
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <returns>charges were assigned</returns>
        /// <seealso cref="PartialCharges(IAtomContainer)"/>
        /// <seealso cref="AssignAtomTypes(IAtomContainer)"/>
        public virtual bool PartialCharges(IAtomContainer mol)
        {
            var adjList = mol.GetProperty<int[][]>(MMFF_ADJLIST_CACHE);
            var edgeMap = mol.GetProperty<EdgeToBondMap>(MMFF_EDGEMAP_CACHE);

            if (adjList == null || edgeMap == null)
                throw new ArgumentException("Invoke assignAtomTypes first.");

            EffectiveCharges(mol);

            for (int v = 0; v < mol.Atoms.Count; v++)
            {
                var atom = mol.Atoms[v];
                var symbType = atom.AtomTypeName;
                int thisType = mmffParamSet.IntType(symbType);

                // unknown
                if (thisType == 0)
                    continue;

                double pbci = (double)mmffParamSet.GetPartialBondChargeIncrement(thisType);

                foreach (var w in adjList[v])
                {
                    int otherType = mmffParamSet.IntType(mol.Atoms[w].AtomTypeName);

                    // unknown
                    if (otherType == 0)
                        continue;

                    var bond = edgeMap[v, w];
                    int bondCls = mmffParamSet.GetBondCls(thisType, otherType, bond.Order.Numeric(), bond.GetProperty(MMFF_AROM, false));
                    var bci = mmffParamSet.GetBondChargeIncrement(bondCls, thisType, otherType);
                    if (bci != null)
                    {
                        atom.Charge = atom.Charge.Value - (double)bci;
                    }
                    else
                    {
                        // empirical BCI
                        atom.Charge = atom.Charge + (pbci - (double)mmffParamSet.GetPartialBondChargeIncrement(otherType));
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Clear all transient properties assigned by this class. Assigned charges
        /// and atom type names remain set.
        /// </summary>
        /// <param name="mol">molecule</param>
        public virtual void ClearProps(IAtomContainer mol)
        {
            mol.RemoveProperty(MMFF_EDGEMAP_CACHE);
            mol.RemoveProperty(MMFF_ADJLIST_CACHE);
            foreach (var bond in mol.Bonds)
                bond.RemoveProperty(MMFF_AROM);
        }

        /// <summary>
        /// Internal method, MMFF primary charges. Tabulated (MMFFFORMCHG.PAR) and
        /// variable (assigned in code).
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <param name="adjList">adjacency list representation</param>
        /// <param name="edgeMap">edge to bond mapping</param>
        private void PrimaryCharges(IAtomContainer mol, int[][] adjList, EdgeToBondMap edgeMap)
        {
            for (int v = 0; v < mol.Atoms.Count; v++)
            {
                var atom = mol.Atoms[v];
                var symbType = atom.AtomTypeName;
                var fc = mmffParamSet.GetFormalCharge(symbType);

                atom.Charge = 0d;

                if (fc != null)
                {
                    atom.Charge = (double)fc;
                }
                else
                {
                    // charge sharing between equivalent terminal oxygens
                    switch (symbType)
                    {
                        case "O2S":
                        case "O3S":
                        case "O2P":
                        case "O3P":
                        case "O4P":
                            {
                                // already handled
                                if (atom.Charge != 0)
                                    continue;

                                // find the central atom (S or P)
                                int focus = -1;
                                foreach (var w in adjList[v])
                                {
                                    int elem = mol.Atoms[w].AtomicNumber;
                                    if (elem == AtomicNumbers.Sulfur || elem == AtomicNumbers.Phosphorus)
                                    {
                                        if (focus >= 0)
                                        {
                                            focus = -2;
                                            break;
                                        }
                                        focus = w;
                                    }
                                }

                                // log - multiple or unfound focus
                                if (focus < 0)
                                    continue;

                                // ensure [P+]-[O-] vs P=O are same by including the charge from
                                // the focus
                                double qSum = FCharge(mol.Atoms[focus]);
                                int nTerm = 0;

                                foreach (var w in adjList[focus])
                                {
                                    if (mol.Atoms[w].AtomTypeName.Equals(symbType, StringComparison.Ordinal))
                                    {
                                        qSum += FCharge(mol.Atoms[w]);
                                        nTerm++;
                                    }
                                }
                                double qSplt = qSum / nTerm;

                                foreach (var w in adjList[focus])
                                {
                                    if (mol.Atoms[w].AtomTypeName.Equals(symbType, StringComparison.Ordinal))
                                    {
                                        atom.Charge = qSplt;
                                    }
                                }
                            }
                            break;
                        case "N5M":
                            // charge sharing between nitrogen anions 
                            {
                                if (atom.Charge != 0)
                                    continue;

                                var eqiv = new HashSet<IAtom>();
                                var visit = new HashSet<int>();
                                var queue = new Deque<int> { v };

                                while (queue.Count != 0)
                                {
                                    int w = queue.Poll();
                                    visit.Add(w);

                                    if (string.Equals(mol.Atoms[w].AtomTypeName, "N5M", StringComparison.Ordinal))
                                        eqiv.Add(mol.Atoms[w]);

                                    foreach (var u in adjList[w])
                                    {
                                        var bond = edgeMap[w, u];
                                        if (bond.GetProperty(MMFF_AROM, false) && !visit.Contains(u))
                                        {
                                            queue.Add(u);
                                        }
                                    }
                                }

                                double q = 0;
                                foreach (var eqivAtom in eqiv)
                                {
                                    q += FCharge(eqivAtom);
                                }
                                q /= eqiv.Count;
                                foreach (var eqivAtom in eqiv)
                                {
                                    eqivAtom.Charge = q;
                                }
                            }
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Internal effective charges method.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <param name="adjList">adjacency list representation</param>
        /// <seealso cref="EffectiveCharges(IAtomContainer)"/>
        void EffectiveCharges(IAtomContainer mol, int[][] adjList)
        {
            double[] tmp = new double[mol.Atoms.Count];
            for (int v = 0; v < tmp.Length; v++)
            {
                IAtom atom = mol.Atoms[v];
                int intType = mmffParamSet.IntType(atom.AtomTypeName);

                // unknown
                if (intType == 0)
                {
                    continue;
                }

                int crd = mmffParamSet.GetCrd(intType);
                decimal fcAdj = mmffParamSet.GetFormalChargeAdjustment(intType);

                double adjust = (double)fcAdj;
                tmp[v] = atom.Charge.Value;

                // sharing when no formal charge adjustment - needed to match 
                // phosphate examples from paper V but documented?
                if (adjust == 0)
                {
                    foreach (var w in adjList[v])
                    {
                        if (mol.Atoms[w].Charge < 0)
                        {
                            tmp[v] += mol.Atoms[w].Charge.Value / (2.0 * adjList[w].Length);
                        }
                    }
                }

                // positive charge sharing - undocumented but inferred from validation suite
                if (string.Equals(atom.AtomTypeName, "NM", StringComparison.Ordinal))
                {
                    foreach (var w in adjList[v])
                    {
                        if (mol.Atoms[w].Charge > 0)
                        {
                            tmp[v] -= mol.Atoms[w].Charge.Value / 2;
                        }
                    }
                }

                // negative charge sharing
                if (adjust != 0)
                {
                    double q = 0;
                    foreach (var w in adjList[v])
                    {
                        q += mol.Atoms[w].Charge.Value;
                    }
                    tmp[v] = ((1 - (crd * adjust)) * tmp[v]) + (adjust * q);
                }
            }

            for (int v = 0; v < tmp.Length; v++)
            {
                mol.Atoms[v].Charge = tmp[v];
            }
        }

        /// <summary>
        /// Helper method to find all existing aromatic chem objects.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <returns>chem objects</returns>
        private static ISet<IMolecularEntity> GetAromatics(IAtomContainer mol)
        {
            var oldArom = new HashSet<IMolecularEntity>();
            foreach (var atom in mol.Atoms)
                if (atom.IsAromatic)
                    oldArom.Add(atom);
            foreach (var bond in mol.Bonds)
                if (bond.IsAromatic)
                    oldArom.Add(bond);
            return oldArom;
        }

        /// <summary>
        /// Access the formal charge - if the charge is null 0 is returned.
        /// <param name="atom">atom</param>
        /// <returns>formal charge</returns>
        /// </summary>
        static int FCharge(IAtom atom)
        {
            if (atom.FormalCharge == null)
                return 0;
            return atom.FormalCharge.Value;
        }
    }
}
