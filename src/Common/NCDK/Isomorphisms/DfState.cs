/*
 * Copyright (C) 2018 NextMove Software
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
 */

using NCDK.Common.Collections;
using NCDK.Isomorphisms.Matchers;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// Internals of the DF ("Depth-First" <token>cdk-cite-Jeliazkova18</token>) substructure
    /// matching algorithm. The algorithm is a simple but elegant backtracking search
    /// iterating over the bonds of a query. Like the popular VF2 the algorithm, it
    /// uses linear memory but unlike VF2 bonded atoms are selected from the
    /// neighbor lists of already mapped atoms.
    /// </summary>
    /// <remarks>
    /// In practice VF2 take O(N<sup>2</sup>) to match a linear chain against it's self
    /// whilst this algorithm is O(N).
    /// <list type="bullet">
    /// <listheader>References</listheader>
    /// <item><token>cdk-cite-Ray57</token></item>
    /// <item><token>cdk-cite-Ullmann76</token></item>
    /// <item><token>cdk-cite-Cordella04</token></item>
    /// <item><token>cdk-cite-Jeliazkova18</token></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.DfState_Example.cs"]/*' />
    /// </example>
    /// <seealso cref="DfState"/>
    // @author John Mayfield
    sealed class DfState : IEnumerable<int[]>
    {
        private const int UNMAPPED = -1;

        private readonly IAtomContainer query;
        private readonly IQueryBond[] qbonds;
        private readonly int numAtoms;
        private int numBonds;
        private int numMapped;
        private readonly int[] amap;

        private IAtomContainer mol;
        private bool[] avisit;

        // To make the algorithm re-entrant we need to
        // manage our own stack
        private sealed class StackFrame
        {
            internal int bidx;
            internal IAtom atom;
            internal IEnumerator<IChemObject> iter;

            internal StackFrame(StackFrame frame)
            {
                this.bidx = frame.bidx;
                this.atom = frame.atom;
                this.iter = frame.iter;
            }

            internal StackFrame() { }
        }

        private int sptr;
        private StackFrame[] stack;

        internal DfState(IQueryAtomContainer query)
        {
            var builder = Silent.ChemObjectBuilder.Instance;

            var tmp = builder.NewAtomContainer();
            tmp.Add(query);
            this.qbonds = new IQueryBond[tmp.Bonds.Count];
            this.amap = new int[query.Atoms.Count];

            int stackSize = 0;
            foreach (var atom in tmp.Atoms)
            {
                if (atom is IQueryAtom)
                {
                    if (amap[atom.Index] == 0)
                    {
                        stackSize += Prepare(atom, null) + 1;
                    }
                }
                else
                    throw new ArgumentException("All atoms must be IQueryAtoms!");
            }

            this.stack = new StackFrame[stackSize + 2];
            for (int i = 0; i < stack.Length; i++)
                this.stack[i] = new StackFrame();

            this.numAtoms = amap.Length;
            this.query = tmp;
        }

        /// <summary>
        /// Copy constructor, if a state has already been prepared the internals
        /// can be copied and the separate instance used in a thread-safe manner.
        /// </summary>
        /// <param name="state">the state</param>
        internal DfState(DfState state)
        {
            // only need shallow copy of the query bonds
            this.qbonds = (IQueryBond[])state.qbonds.Clone();
            this.query = state.query;
            this.numBonds = state.numBonds;
            this.numAtoms = state.numAtoms;
            this.numMapped = state.numMapped;
            this.amap = (int[])state.amap.Clone();
            this.avisit = state.avisit != null ? (bool[])state.avisit.Clone() : null;
            this.mol = state.mol;
            // deep copy of the stack-frame
            this.stack = new StackFrame[state.stack.Length];
            for (int i = 0; i < stack.Length; i++)
                this.stack[i] = new StackFrame(state.stack[i]);
            this.sptr = state.sptr;
        }

        // prepare the query, the required stack size is returned
        private int Prepare(IAtom atom, IBond prev)
        {
            int count = 0;
            amap[atom.Index] = 1;
            foreach (var bond in atom.Bonds)
            {
                if (bond == prev)
                    continue;
                var nbr = bond.GetOther(atom);
                if (amap[nbr.Index] == 0)
                {
                    qbonds[numBonds++] = (IQueryBond)bond;
                    count += Prepare(nbr, bond) + 1;
                }
                else if (nbr.Index < atom.Index)
                {
                    ++count; // ring closure
                    qbonds[numBonds++] = (IQueryBond)bond;
                }
            }
            return count;
        }

        /// <summary>
        /// Set the molecule to be matched.
        /// </summary>
        /// <param name="mol">the molecule</param>
        internal void SetMol(IAtomContainer mol)
        {
            this.mol = mol;
            Arrays.Fill(amap, -1);
            numMapped = 0;
            this.avisit = new bool[mol.Atoms.Count];
            sptr = 0;
            Store(0, null);
        }

        /// <summary>
        /// Set the molecule to be matched and the 'root' atom at which the match
        /// must start (e.g. query atom 0). It is presumed the root atom has already
        /// been tested against the query atom and matched.
        /// </summary>
        /// <param name="atom">the root atom.</param>
        internal void SetRoot(IAtom atom)
        {
            SetMol(atom.Container);
            numMapped = 1;
            var aidx = atom.Index;
            avisit[aidx] = true;
            amap[0] = aidx;
        }

        private int CurrBondIdx()
        {
            return stack[sptr].bidx;
        }

        private IEnumerator<IChemObject> GetAtomsEnumerator()
        {
            if (stack[sptr].iter == null)
                stack[sptr].iter = mol.Atoms.GetEnumerator();
            return stack[sptr].iter;
        }

        private IEnumerator<IChemObject> GetBondsEnumerator(IAtom atom)
        {
            if (stack[sptr].iter == null)
                stack[sptr].iter = atom.Bonds.GetEnumerator();
            return stack[sptr].iter;
        }

        /// <summary>
        /// Store the specified bond index and mapped query atom (optional)
        /// on the stack.
        /// </summary>
        /// <param name="bidx">bond index</param>
        /// <param name="queryatom">query atom - can be null</param>
        private void Store(int bidx, IQueryAtom queryatom)
        {
            ++sptr;
            stack[sptr].bidx = bidx;
            stack[sptr].iter = null;
            if (queryatom != null)
                stack[sptr].atom = queryatom;
            else
                stack[sptr].atom = null;
        }

        /// <summary>
        /// Pops a stack frame until a the query/mol atom pairing is unmapped
        /// or we reach the bottom of the stack
        /// </summary>
        private void Backtrack()
        {
            var qatom = stack[sptr].atom;
            --sptr;
            if (qatom != null)
            {
                --numMapped;
                avisit[amap[qatom.Index]] = false;
                amap[qatom.Index] = UNMAPPED;
            }
            else if (sptr != 0)
            {
                Backtrack();
            }
        }

        /// <summary>
        /// Determine if a atom from the molecule is unvisited and if it is matched
        /// by the query atom. If the match is feasible the provided query bond index
        /// stored on the stack.
        /// </summary>
        /// <param name="bidx">atom from the query</param>
        /// <param name="atom">atom from the molecule</param>
        /// <returns>the match was feasible and the state was stored</returns>
        private bool Feasible(int bidx, IQueryAtom qatom, IAtom atom)
        {
            var aidx = atom.Index;
            if (avisit[aidx] || !qatom.Matches(atom))
                return false;
            ++numMapped;
            amap[qatom.Index] = aidx;
            avisit[aidx] = true;
            Store(bidx, qatom);
            return true;
        }

        /// <summary>
        /// Determine if a bond from the molecule exists and if it is matched
        /// by the query bond. If the match is feasible the current query bond index
        /// is increment and stored on the stack.
        /// </summary>
        ///     /// <param name="qbond">bond from the query</param>
        /// <param name="bond">bond from the molecule</param>
        /// <returns>the match was feasible and the state was stored</returns>
        private bool Feasible(IQueryBond qbond, IBond bond)
        {
            if (bond == null || !qbond.Matches(bond))
                return false;
            Store(CurrBondIdx() + 1, null);
            return true;
        }

        /// <summary>
        /// Primary match function, if this function returns true the algorithm
        /// has found a match. Calling it again will backtrack and find the next
        /// match.
        /// </summary>
        /// <returns>a mapping was found</returns>
        bool MatchNext()
        {
            if (numAtoms == 0)
                return false;
            if (sptr > 1)
                Backtrack();
        main:
            while (sptr != 0)
            {
                int bidx = CurrBondIdx();

                if (bidx == numBonds)
                {
                    // done
                    if (numMapped == numAtoms)
                        return true;

                    // handle disconnected atoms
                    foreach (var qatom in query.Atoms)
                    {
                        if (amap[qatom.Index] == UNMAPPED)
                        {
                            var iter = GetAtomsEnumerator();
                            while (iter.MoveNext())
                            {
                                var atom = (IAtom)iter.Current;
                                if (Feasible(bidx, (IQueryAtom)qatom, atom))
                                    goto continue_main;
                            }
                            break;
                        }
                    }
                    Backtrack();
                    continue;
                }

                var qbond = qbonds[bidx];
                var qbeg = (IQueryAtom)qbond.Begin;
                var qend = (IQueryAtom)qbond.End;

                var begIdx = amap[qbeg.Index];
                var endIdx = amap[qend.Index];

                // both atoms matched, there must be a bond between them
                if (begIdx != UNMAPPED && endIdx != UNMAPPED)
                {
                    var bond = mol.Atoms[begIdx].GetBond(mol.Atoms[endIdx]);
                    if (Feasible(qbond, bond))
                        continue;
                }
                // 'beg' is mapped, find a feasible 'end' from it's neighbor list
                else if (begIdx != UNMAPPED)
                {
                    var beg = mol.Atoms[begIdx];
                    var iter = GetBondsEnumerator(beg);
                    while (iter.MoveNext())
                    {
                        var bond = (IBond)iter.Current;
                        var end = bond.GetOther(beg);
                        if (qbond.Matches(bond) && Feasible(bidx + 1, qend, end))
                            goto continue_main;
                    }
                }
                // 'end' is mapped, find a feasible 'beg' from it's neighbor list
                else if (endIdx != UNMAPPED)
                {
                    var end = mol.Atoms[endIdx];
                    var iter = GetBondsEnumerator(end);
                    while (iter.MoveNext())
                    {
                        var bond = (IBond)iter.Current;
                        var beg = bond.GetOther(end);
                        if (qbond.Matches(bond) && Feasible(bidx + 1, qbeg, beg))
                            goto continue_main;
                    }
                }
                // 'beg' nor 'end' matched, find a feasible mapping from
                // any atom in the molecule
                else
                {
                    var iter = GetAtomsEnumerator();
                    while (iter.MoveNext())
                    {
                        var atom = (IAtom)iter.Current;
                        if (Feasible(bidx, qbeg, atom))
                            goto continue_main;
                    }
                }
                Backtrack();
            continue_main:
                ;
            }
            return false;
        }

        /// <summary>
        /// Adapter to current CDK <see cref="Pattern"/> that takes and iterator of an
        /// int[] permutation from the query to the molecule.
        /// </summary>
        /// <returns>the enumerator</returns>
        public IEnumerator<int[]> GetEnumerator()
        {
            var lstate = new DfState(this);
            while (lstate.MatchNext())
                yield return lstate.amap;
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
