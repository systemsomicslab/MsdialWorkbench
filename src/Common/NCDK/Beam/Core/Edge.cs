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
using System.Text;

namespace NCDK.Beam
{
    /// <summary>
    /// An edge defines two vertex end points and an associated <see cref="Bond"/> label.
    /// Edges are created from their <see cref="Bond"/> label as follows.
    /// </summary>
    /// <example>
    /// an edge between the vertices 1 and 2 the bond label is implicit
    /// <code>
    /// Edge e = Bond.Implicit.CreateEdge(1, 2);
    /// </code>
    /// an edge between the vertices 5 and 3 the bond label is double
    /// <code>
    /// Edge e = Bond.Double.CreateEdge(1, 2);
    /// </code></example>
    /// <seealso cref="Bond"/>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    sealed class Edge
    {
        /// <summary>Endpoints of the edge.</summary>
        private readonly int u, v, xor;

        /// <summary>Label on the edge.</summary>
        public Bond Bond { get; set; }

        internal Edge(int u, int v, Bond bond)
        {
            this.u = u;
            this.v = v;
            this.xor = u ^ v;
            this.Bond = bond;
        }

        internal Edge(Edge e)
            : this(e.u, e.v, e.Bond)
        {
        }

        /// <summary>
        /// Access either endpoint of the edge. For directional bonds, the endpoint
        /// can be considered as relative to this vertex.
        /// </summary>
        /// <returns>either endpoint</returns>
        public int Either()
        {
            return u;
        }

        /// <summary>
        /// Given one endpoint, access the other endpoint of the edge.
        /// </summary>
        /// <param name="x">an endpoint of the edge</param>
        /// <returns>the other endpoint</returns>
        public int Other(int x)
        {
            return x ^ xor;
        }

        /// <summary>
        /// Set the bond label.
        /// </summary>
        /// <param name="bond">the bond label</param>
        public void SetBond(Bond bond)
        {
            this.Bond = bond;
        }

        /// <summary>
        /// Access the bond label relative to a specified endpoint.
        /// </summary>
        /// <example><code>
        /// Edge e = Bond.Up.CreateEdge(2, 3);
        /// e.Bond(2); // Up
        /// e.Bond(3); // Down
        /// </code></example>
        /// <param name="x">endpoint to which the label is relative to</param>
        /// <returns>the bond label</returns>
        public Bond GetBond(int x)
        {
            if (x == u) return Bond;
            if (x == v) return Bond.Inverse();
            throw new ArgumentException(InvalidEndpointMessage(x));
        }

        /// <summary>
        /// Inverse of the edge label but keep the vertices the same.
        /// </summary>
        /// <returns>inverse edge</returns>
        public Edge Inverse()
        {
            return Bond.Inverse().CreateEdge(u, v);
        }

        /// <summary>Helper method to print error message.</summary>
        private string InvalidEndpointMessage(int x)
        {
            return "Vertex " + x + ", is not an endpoint of the edge " + ToString();
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return xor;
        }

        /// <inheritdoc/>
        public override bool Equals(object other)
        {
            var o = other as Edge;
            if (o == null)
                return false;
            return (u == o.u && v == o.v && Bond.Equals(o.Bond)) ||
                    (u == o.v && v == o.u && Bond.Equals(o.Bond.Inverse()));
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return new StringBuilder(20).Append('{')
                                        .Append(u)
                                        .Append(", ")
                                        .Append(v)
                                        .Append('}')
                                        .Append(": '")
                                        .Append(Bond)
                                        .Append("'")
                                        .ToString();
        }
    }
}
