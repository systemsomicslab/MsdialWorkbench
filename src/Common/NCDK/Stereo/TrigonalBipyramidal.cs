/*
 * Copyright (c) 2017 John Mayfield <jwmay@users.sf.net>
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Stereo
{
    /// <summary>
    /// Describes a trigonal-bipyramidal configuration. 
    /// </summary>
    /// <remarks>
    /// The configuration carriers
    /// are arranged with two co-linear on an axis and three equatorial. The
    /// configuration order is between 1 and 20 and follows the same meaning as
    /// SMILES.
    /// <pre>
    ///    d   c     TB1
    ///     \ /
    ///  a---x---e   where a: first carrier, b: second carrier, ... *
    ///      |             x: focus
    ///      b             'c' is in front of 'x', 'd' is behind
    /// </pre>
    /// 
    /// The configuration can be normalized to the lowest order (1) using the
    /// <see cref="Normalize()"/> function.
    /// </remarks>
    /// <seealso href="http://opensmiles.org/opensmiles.html#_octahedral_centers">Octahedral Centers, OpenSMILES</seealso>
    /// <seealso cref="Octahedral"/>
    /// <seealso cref="SquarePlanar"/>
    public sealed class TrigonalBipyramidal : AbstractStereo<IAtom, IAtom>
    {
        private static readonly int[][] PERMUTATIONS = new int[][]{
            new[]
            {A, B, C, D, E,  A, C, D, B, E,  A, D, B, C, E,
             E, D, C, B, A,  E, B, D, C, A,  E, C, B, D, A }, // TB1 a -> e @
            new[]
            {A, D, C, B, E,  A, C, B, D, E,  A, B, D, C, E,
             E, B, C, D, A,  E, D, B, C, A,  E, C, D, B, A }, // TB2 a -> e @@
            new[]
            {A, B, C, E, D,  A, C, E, B, D,  A, E, B, C, D,
             D, E, C, B, A,  D, B, E, C, A,  D, C, B, E, A }, // TB3 a -> d @
            new[]
            {A, E, C, B, D,  A, C, B, E, D,  A, B, E, C, D,
             D, B, C, E, A,  D, E, B, C, A,  D, C, E, B, A }, // TB4 a -> d @@
            new[]
            {A, B, D, E, C,  A, D, E, B, C,  A, E, B, D, C,
             C, E, D, B, A,  C, B, E, D, A,  C, D, B, E, A }, // TB5 a -> c @
            new[]
            {A, E, D, B, C,  A, D, B, E, C,  A, B, E, D, C,
             C, B, D, E, A,  C, E, B, D, A,  C, D, E, B, A }, // TB6 a -> c @@
            new[]
            {A, C, D, E, B,  A, D, E, C, B,  A, E, C, D, B,
             B, E, D, C, A,  B, C, E, D, A,  B, D, C, E, A }, // TB7 a -> b @
            new[]
            {A, E, D, C, B,  A, D, C, E, B,  A, C, E, D, B,
             B, C, D, E, A,  B, E, C, D, A,  B, D, E, C, A }, // TB8 a -> b @@
            new[]
            {B, A, C, D, E,  B, C, D, A, E,  B, D, A, C, E,
             E, D, C, A, B,  E, A, D, C, B,  E, C, A, D, B }, // TB9 b -> e @
            new[]
            {B, A, C, E, D,  B, C, E, A, D,  B, E, A, C, D,
             D, E, C, A, B,  D, A, E, C, B,  D, C, A, E, B }, // TB10 b -> d @
            new[]
            {B, D, C, A, E,  B, C, A, D, E,  B, A, D, C, E,
             E, A, C, D, B,  E, D, A, C, B,  E, C, D, A, B }, // TB11 b -> e @@
            new[]
            {B, E, C, A, D,  B, C, A, E, D,  B, A, E, C, D,
             D, A, C, E, B,  D, E, A, C, B,  D, C, E, A, B }, // TB12 b -> d @@
            new[]
            {B, A, D, E, C,  B, D, E, A, C,  B, E, A, D, C,
             C, E, D, A, B,  C, A, E, D, B,  C, D, A, E, B }, // TB13 b -> c @
            new[]
            {B, E, D, A, C,  B, D, A, E, C,  B, A, E, D, C,
             C, A, D, E, B,  C, E, A, D, B,  C, D, E, A, B }, // TB14 b -> c @@
            new[]
            {C, A, B, D, E,  C, B, D, A, E,  C, D, A, B, E,
             E, D, B, A, C,  E, A, D, B, C,  E, B, A, D, C }, // TB15 c -> e @
            new[]
            {C, A, B, E, D,  C, B, E, A, D,  C, E, A, B, D,
             D, E, B, A, C,  D, A, E, B, C,  D, B, A, E, C }, // TB16 c -> d @
            new[]
            {D, A, B, C, E,  D, B, C, A, E,  D, C, A, B, E,
             E, C, B, A, D,  E, A, C, B, D,  E, B, A, C, D }, // TB17 d -> e @
            new[]
            {D, C, B, A, E,  D, B, A, C, E,  D, A, C, B, E,
             E, A, B, C, D,  E, C, A, B, D,  E, B, C, A, D }, // TB18 d -> e @@
            new[]
            {C, E, B, A, D,  C, B, A, E, D,  C, A, E, B, D,
             D, A, B, E, C,  D, E, A, B, C,  D, B, E, A, C }, // TB19 c -> d @@
            new[]
            {C, D, B, A, E,  C, B, A, D, E,  C, A, D, B, E,
             E, A, B, D, C,  E, D, A, B, C,  E, B, D, A, C }, // TB20 c -> e @@
        };

        /// <summary>
        /// Create a new trigonal bipyramidal configuration.
        /// </summary>
        /// <param name="focus">the focus</param>
        /// <param name="carriers">the carriers</param>
        /// <param name="order">the order (1-20)</param>
        public TrigonalBipyramidal(IAtom focus, IReadOnlyList<IAtom> carriers, int order)
            : base(focus, carriers, new StereoElement(StereoClass.TrigonalBipyramidal, order))
        {
            if (order < 0 || order > 20)
                throw new ArgumentOutOfRangeException(nameof(order));
            if (Configure.Order() < 0 || Configure.Order() > 20)
                throw new InvalidOperationException("Invalid configuration order, should be between 1-20");
        }

        public TrigonalBipyramidal(IAtom focus, IReadOnlyList<IAtom> carriers, StereoElement stereo)
            : this(focus, carriers, stereo.Configuration.Order())
        {
        }

        /// <summary>
        /// Normalize the configuration to the lowest configuration order (1) -
        /// the axis goes from the first to last carrier, the three middle carriers
        /// are anti-clockwise looking from the first carrier.
        /// </summary>
        /// <returns>the normalized configuration</returns>
        public TrigonalBipyramidal Normalize()
        {
            int cfg = Configure.Order();
            if (cfg == 1)
                return this;
            var carriers = InvApply(Carriers, PERMUTATIONS[cfg - 1]);
            return new TrigonalBipyramidal(Focus, carriers, 1);
        }

        /// <inheritdoc/>
        protected override IStereoElement<IAtom, IAtom> Create(IAtom focus, IReadOnlyList<IAtom> carriers, StereoElement stereo)
        {
            return new TrigonalBipyramidal(focus, carriers.ToArray(), stereo);
        }
    }
}
