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
    /// Fixed size Union-Find/Disjoint-Set implementation.
    /// </summary>
    /// <example><code>
    /// UnionFind uf = new UnionFind(11);
    /// uf.Join(0, 1);
    /// uf.Join(1, 10);
    /// uf.Connected(0, 10); // are 0 and 10 joint?
    /// uf.Find(10);         // id for the set to which '10' belongs
    /// </code></example>
    // @author John May
    internal sealed class UnionFind
    {
        /// <summary>
        /// Each element is either a connected (negative), points to another element.
        /// The size of the set is indicated by the size of the negation on the
        /// connected.
        /// </summary>
        readonly int[] forest;

        /// <summary>
        /// Create a new UnionFind data structure with enough space for 'n'
        /// elements.
        /// </summary>
        /// <param name="n">number of elements</param>
        public UnionFind(int n)
        {
            this.forest = new int[n];
            Arrays.Fill(forest, -1);
        }

        /// <summary>
        /// Find the identifier of the set to which 'u' belongs.
        /// </summary>
        /// <param name="u">an element</param>
        /// <returns>the connected</returns>
        public int Find(int u)
        {
            return forest[u] < 0 ? u : (forest[u] = Find(forest[u]));
        }

        /// <summary>
        /// Join the sets containing 'u' and 'v'.
        /// </summary>
        /// <param name="u">an element</param>
        /// <param name="v">another element</param>
        public void Union(int u, int v)
        {
            int uRoot = Find(u);
            int vRoot = Find(v);

            if (uRoot == vRoot)
                return;

            if (forest[uRoot] < forest[vRoot])
                Join(vRoot, uRoot);
            else
                Join(uRoot, vRoot);
        }

        /// <summary>
        /// Join two disjoint sets. The larger set is appended onto the smaller set.
        /// </summary>
        /// <param name="sRoot">root of a set (small)</param>
        /// <param name="lRoot">root of another set (large)</param>
        private void Join(int sRoot, int lRoot)
        {
            forest[sRoot] = forest[sRoot] + forest[lRoot];
            forest[lRoot] = sRoot;
        }

        /// <summary>
        /// Are the elements 'u' and 'v' in the same set.
        /// </summary>
        /// <param name="u">an element</param>
        /// <param name="v">another element</param>
        /// <returns>the elements are in the same set.</returns>
        public bool Connected(int u, int v)
        {
            return Find(u) == Find(v);
        }

        /// <summary>
        /// Clear any joint sets - all items are once disjoint and are singletons.
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < forest.Length; i++)
                forest[i] = -1;
        }
    }
}
