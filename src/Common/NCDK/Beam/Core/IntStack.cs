/*
 * Copyright (c) 2013, European Bioinformatics Institute (EMBL-EBI)
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this
 *    list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
 * Any EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
 * Any DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON Any THEORY OF LIABILITY, WHETHER IN CONTRACT, Strict LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN Any WAY OUT OF THE USE OF THIS
 * SOFTWARE, Even IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *
 * The views and conclusions contained in the software and documentation are those
 * of the authors and should not be interpreted as representing official policies,
 * either expressed or implied, of the FreeBSD Project. 
 */

using NCDK.Common.Collections;

namespace NCDK.Beam
{
    /// <summary>
    /// A lightweight stack data structure for primitive 'int' types.
    /// </summary>
    // @author John May
    // @see java.util.ArrayDeque
    internal sealed class IntStack
    {
        /// <summary>Storage of values.</summary>
        private int[] xs;

        /// <summary>Number of items in the stack</summary>
        private int n;

        /// <summary>
        /// Create a new stack with specified initial capacity.
        /// </summary>
        /// <param name="n">capacity of the stack</param>
        public IntStack(int n)
        {
            this.xs = new int[n];
        }

        /// <summary>
        /// Push the value <paramref name="x"/> on to the stack.
        /// </summary>
        /// <param name="x">value to push</param>
        public void Push(int x)
        {
            if (n == xs.Length)
                xs = Arrays.CopyOf(xs, xs.Length * 2);
            xs[n++] = x;
        }

        /// <summary>
        /// Access and remove the value on the top of the stack. No check is made as
        /// to whether the stack is empty.
        /// </summary>
        /// <returns>value on top of the stack</returns>
        public int Pop()
        {
            return xs[--n];
        }

        /// <summary>
        /// Access the value on top of the stack without removing it. No check is
        /// made as to whether the stack is empty.
        /// </summary>
        /// <returns>the last value added</returns>
        public int Peek()
        {
            return xs[n - 1];
        }

        /// <summary>
        /// Determine if there are any items on the stack.
        /// </summary>
        /// <returns>whether the stack is empty</returns>
        public bool IsEmpty => n == 0;

        /// <summary>
        /// Number of items on the stack.
        /// </summary>
        /// <returns>size</returns>
        public int Count => n;

        /// <summary>Remove all values from the stack.</summary>
        public void Clear()
        {
            n = 0;
        }
    }
}
