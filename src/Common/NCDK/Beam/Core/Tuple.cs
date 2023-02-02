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

using System;

namespace NCDK.Beam
{
    /// <summary>
    /// A simple utility class for storing two primitive integers.
    /// </summary>
    // @author John May
    internal sealed class Tuple
    {
        private readonly int fst, snd;

        public Tuple(int fst, int snd)
        {
            this.fst = fst;
            this.snd = snd;
        }

        /// <summary>
        /// Access the first value of the tuple.
        /// </summary>
        /// <returns>value</returns>
        public int First()
        {
            return fst;
        }

        /// <summary>
        /// Access the second value of the tuple.
        /// </summary>
        /// <returns>value</returns>
        public int Second()
        {
            return snd;
        }

        /// <inheritdoc/>
        public override bool Equals(object o)
        {
            var tuple = o as Tuple;
            if (tuple == null)
                return false;

            if (fst != tuple.fst) return false;
            if (snd != tuple.snd) return false;

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int result = fst;
            result = 31 * result + snd;
            return result;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return "{" + fst + ", " + snd + "}";
        }

        /// <summary>
        /// Create a new tuple for the provided values.
        /// </summary>
        /// <param name="fst">a value</param>
        /// <param name="snd">another value</param>
        /// <returns>a tuple of the two values</returns>
        public static Tuple Of(int fst, int snd)
        {
            return new Tuple(fst, snd);
        }
    }
}
