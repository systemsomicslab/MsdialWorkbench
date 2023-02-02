/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
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
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using System.Collections;
using System.Collections.Generic;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// Given a (subgraph-)isomorphism state this class can lazily iterate over the
    /// mappings in a non-recursive manner.
    /// </summary>
    // @author John May
    // @cdk.module isomorphism
    internal sealed class StateStream : IEnumerable<int[]>
    {
        /// <summary>A mapping state.</summary>
        private readonly State state;

        /// <summary>The stack replaces the call-stack in a recursive matcher.</summary>
        private readonly CandidateStack stack;

        /// <summary>Current candidates.</summary>
        private int n = 0, m = -1;

        /// <summary>
        /// Create a stream for the provided state.
        /// </summary>
        /// <param name="state">the state to stream over</param>
        public StateStream(State state)
        {
            this.state = state;
            this.stack = new CandidateStack(state.NMax());
        }

        public IEnumerator<int[]> GetEnumerator()
        {
            if (state.NMax() == 0 || state.MMax() == 0)
                yield break;
            int[] current;
            while ((current = FindNext()) != null)
                yield return current;
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Finds the next mapping from the current state.
        /// </summary>
        /// <returns>the next state (or null if none)</returns>
        private int[] FindNext()
        {
            while (Map()) ;
            if (state.Count == state.NMax()) return state.Mapping();
            return null;
        }

        /// <summary>
        /// Progress the state-machine - the function return false when a mapping is
        /// found on the mapping is done.
        /// </summary>
        /// <returns>the state is partial</returns>
        private bool Map()
        {
            // backtrack - we've tried all possible n or m, remove the last mapping
            if ((n == state.NMax() || m == state.MMax()) && !stack.IsEmpty())
                state.Remove(n = stack.PopN(), m = stack.PopM());

            while ((m = state.NextM(n, m)) < state.MMax())
            {
                if (state.Add(n, m))
                {
                    stack.Push(n, m);
                    n = state.NextN(-1);
                    m = -1;
                    return n < state.NMax();
                }
            }

            return state.Count > 0 || m < state.MMax();
        }


        /// <summary>
        /// A fixed size stack to keep track of which vertices are mapped. This stack
        /// allows us to turn the recursive algorithms it to lazy iterating mappers.
        /// A reclusive call is usually implemented as call-stack which stores the
        /// variable in each subroutine invocation. For the mapping we actually only
        /// need store the candidates.
        /// </summary>
        private sealed class CandidateStack
        {
            /// <summary>Candidate storage.</summary>
            private readonly int[] ns, ms;

            /// <summary>Size of each stack.</summary>
            private int nSize, mSize;

            public CandidateStack(int capacity)
            {
                ns = new int[capacity];
                ms = new int[capacity];
            }

            /// <summary>
            /// Push a candidate mapping on to the stack.
            /// </summary>
            /// <param name="n">query candidate</param>
            /// <param name="m">target candidate</param>
            public void Push(int n, int m)
            {
                ns[nSize++] = n;
                ms[mSize++] = m;
            }

            /// <summary>
            /// Pops the G1 candidate.
            /// </summary>
            /// <returns>the previous 'n' candidate</returns>
            public int PopN()
            {
                return ns[--nSize];
            }

            /// <summary>
            /// Pops the G2 candidate.
            /// </summary>
            /// <returns>the previous 'm' candidate</returns>
            public int PopM()
            {
                return ms[--mSize];
            }

            /// <summary>
            /// Is the stack empty - if so no candidates can be popped.
            /// </summary>
            /// <returns></returns>
            public bool IsEmpty() => nSize == 0 && mSize == 0;
        }
    }
}