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
using System.Collections.Generic;

namespace NCDK.Beam
{
    /// <summary>
    /// Enumeration of valid <see cref="Edge"/> labels. The connections include all the
    /// valid undirected and directed bond types and <see cref="Dot"/>. Opposed to the
    /// other types, <see cref="Dot"/> indicates that two atoms are not connected.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <item>
    /// <term><see cref="Bond"/></term>
    /// <term><see cref="Token"/></term>
    /// <term><see cref="Order"/></term>
    /// <term><see cref="Inverse()"/></term>
    /// </item>
    /// <item><term><see cref="Dot"/></term><term>.</term><term>0</term><term></term></item>
    /// <item><term><see cref="Implicit"/></term><term></term><term>undefined (2 or 3)</term><term></term></item>
    /// <item><term><see cref="Single"/></term><term>-</term><term>2</term><term></term></item>
    /// <item><term><see cref="Aromatic"/></term><term>:</term><term>3</term><term></term></item>
    /// <item><term><see cref="Double"/></term><term>=</term><term>4</term><term></term></item>
    /// <item><term><see cref="Triple"/></term><term>#</term><term>6</term><term></term></item>
    /// <item><term><see cref="Quadruple"/></term><term>$</term><term>8</term><term></term></item>
    /// <item><term><see cref="Up"/></term><term>/</term><term>2</term><term><see cref="Down"/></term></item>
    /// <item><term><see cref="Down"/></term><term>\</term><term>2</term><term><see cref="Up"/></term></item>
    /// </list>
    /// </remarks>
    /// <seealso href="http://www.opensmiles.org/opensmiles.html#bonds">Bonds, OpenSMILES Specification</seealso>
    // @author John May
#if PUBLIC_BEAM
    public
#else
    internal
#endif
    class Bond
    {
        /// <summary>Atoms are not bonded. </summary>
        public static readonly Bond Dot = new Bond(O.Dot, ".", 0);

        /// <summary>Atoms are bonded by either a single or aromatic bond. </summary>
        public static readonly Bond Implicit = new Bond(O.Implicit, "", 1);

        /// <summary>An implicit bond which is delocalised. </summary>
        public static readonly Bond ImplicitAromatic = new Bond(O.ImplicitAromatic, "", 1);

        /// <summary>Atoms are bonded by a single pair of electrons. </summary>
        public static readonly Bond Single = new Bond(O.Single, "-", 1);

        /// <summary>Atoms are bonded by two pairs of electrons. </summary>
        public static readonly Bond Double = new Bond(O.Double, "=", 2);

        /// <summary>A double bond which is delocalised. </summary>
        public static readonly Bond DoubleAromatic = new Bond(O.DoubleAromatic, "=", 2);

        /// <summary>Atoms are bonded by three pairs of electrons. </summary>
        public static readonly Bond Triple = new Bond(O.Triple, "#", 3);

        /// <summary>Atoms are bonded by four pairs of electrons. </summary>
        public static readonly Bond Quadruple = new Bond(O.Quadruple, "$", 4);

        /// <summary>Atoms are bonded by a delocalized bond of an aromatic system. </summary>
        public static readonly Bond Aromatic = new Bond(O.Aromatic, ":", 1);

        /// <summary>
        /// Directional, single or aromatic bond (currently always single). The bond
        /// is relative to each endpoint such that the second endpoint is
        /// <i>above</i> the first or the first end point is <i>below</i> the
        /// second.
        /// </summary>
        public static readonly Bond Up = new Bond_Up(O.Up, "/", 1);

        public static readonly Bond Down = new Bond_Down(O.Down, "\\", 1);

        public static IEnumerable<Bond> Values = new[]
        {
            Dot, Implicit, ImplicitAromatic, Single, Double, DoubleAromatic, Triple, Quadruple, Aromatic, Up, Down, 
        };

        public static class O
        {
            public const int Dot = 0;
            public const int Implicit = 1;
            public const int ImplicitAromatic = 2;
            public const int Single = 3;
            public const int Double = 4;
            public const int DoubleAromatic = 5;
            public const int Triple = 6;
            public const int Quadruple = 7;
            public const int Aromatic = 8;
            public const int Up = 9;
            public const int Down = 10;
        }

        class Bond_Up : Bond
        {
            public Bond_Up(int ordinal, string token, int Order)
                : base(ordinal, token, Order)
            {
            }

            public override Bond Inverse()
            {
                return Down;
            }

            public override bool IsDirectional => true;
        }

        /// <summary>
        /// Directional, single or aromatic bond (currently always single). The bond
        /// is relative to each endpoint such that the second endpoint is
        /// <i>below</i> the first or the first end point is <i>above</i> the
        /// second.
        /// </summary>

        class Bond_Down : Bond
        {
            public Bond_Down(int ordinal, string token, int Order)
                : base(ordinal, token, Order)
            {
            }

            public override Bond Inverse()
            {
                return Up;
            }

            public override bool IsDirectional => true;
        }

        private readonly int ordinal;

        /// <summary>The token for the bond in the SMILES grammar. </summary>
        private readonly string token;

        private readonly int order;

        public Bond(int ordinal, string token, int Order)
        {
            this.ordinal = ordinal;
            this.token = token;
            this.order = Order;
        }

        public int Ordinal => ordinal;

        /// <summary>
        /// The token of the bond in the SMILES grammar.
        /// </summary>
        public string Token => token;

        /// <summary>
        /// The Order of the bond.
        /// </summary>
        public int Order => order;

        /// <summary>
        /// Access the inverse of a directional bond <see cref="Up"/>, <see cref="Down"/>). If
        /// a bond is non-directional the same bond is returned.
        /// </summary>
        /// <returns>inverse of the bond</returns>
        public virtual Bond Inverse()
        {
            return this;
        }

        /// <summary>
        /// Create an edge between the vertices <paramref name="u"/> and <paramref name="v"/> with this
        /// label.
        /// </summary>
        /// <example><code>Edge e = Bond.Implicit.CreateEdge(2, 3);</code></example>
        /// <param name="u">an end point of the edge</param>
        /// <param name="v">the other endpoint of the edge</param>
        /// <returns>a new edge labeled with this value</returns>
        /// <seealso cref="Edge"/>
        public Edge CreateEdge(int u, int v)
        {
            return new Edge(u, v, this);
        }

        public virtual bool IsDirectional => false;

        /// <inheritdoc/>
        public override string ToString() => token;
    }
}
