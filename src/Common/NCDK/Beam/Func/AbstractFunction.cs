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

namespace NCDK.Beam
{
    /// <summary>
    /// Provides ability to compose functions.
    /// </summary>
    /// <typeparam name="S"></typeparam>
    /// <typeparam name="T"></typeparam>
    // @author John May 
    internal abstract class AbstractFunction<S, T> : IFunction<S, T>
    {
        public abstract T Apply(S s);

        /// <inheritdoc/>
        public IFunction<S, U> With<U>(IFunction<T, U> g)
        {
            return new Composition<S, T, U>(this, g);
        }

        sealed class Composition<SS, TT, UU> : IFunction<SS, UU>
        {
            readonly IFunction<SS, TT> f;
            readonly IFunction<TT, UU> g;

            public Composition(IFunction<SS, TT> f, IFunction<TT, UU> g)
            {
                this.f = f;
                this.g = g;
            }

            public UU Apply(SS s)
            {
                return g.Apply(f.Apply(s));
            }

            public IFunction<SS, P> With<P>(IFunction<UU, P> g)
            {
                return new Composition<SS, UU, P>(this, g);
            }
        }
    }
}
