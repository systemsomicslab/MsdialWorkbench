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
    /// Describes square planar configuration. 
    /// </summary>
    /// <remarks>
    /// The configuration around a square
    /// planar is described by 3 possible values (1:U, 2:4, or 3:Z) based on the
    /// ordering of the planar carries around the focus:
    /// <pre>
    /// Configurations:
    /// 
    ///     a                a                a
    ///     |                |                |
    ///  d--f--b = U      c--f--d = 4      b--f--c = Z
    ///     |                |                |
    ///     c                b                d
    /// 
    ///    SP1              SP2              SP3
    /// </pre>
    /// cis-platin can be represented as any of the following:
    /// <pre>
    /// [NH3][Pt@SP1]([NH3])(Cl)Cl
    /// [NH3][Pt@SP3]([NH3])(Cl)Cl
    /// [NH3][Pt@SP2](Cl)([NH3])Cl
    /// [NH3][Pt@SP1](Cl)(Cl)[NH3]
    /// </pre>
    /// trans-platin can be represented as any of the following:
    /// <pre>
    /// [NH3][Pt@SP2]([NH3])(Cl)Cl
    /// [NH3][Pt@SP1](Cl)([NH3])Cl
    /// [NH3][Pt@SP1](Cl)([NH3])Cl
    /// [NH3][Pt@SP3](Cl)(Cl)[NH3]
    /// </pre>
    /// </remarks>
    /// The normalize function (<see cref="Normalize()"/>) create a new
    /// <pre>IStereoElement</pre> where the carriers have been reorder such that the
    /// configuration is in a "U" shape (order=1).
    /// <seealso href="http://opensmiles.org/opensmiles.html#_octahedral_centers">Octahedral Centers, OpenSMILES</seealso>
    /// <seealso cref="TrigonalBipyramidal"/>
    /// <seealso cref="Octahedral"/>
    public sealed class SquarePlanar : AbstractStereo<IAtom, IAtom>
    {
        private static readonly int[][] PERMUTATIONS = new int[][]
        {
            new[]
            {A, B, C, D,  A, D, C, B,
             B, C, D, A,  B, A, D, C,
             C, D, A, B,  C, B, A, D,
             D, C, B, A,  D, A, B, C}, // SP1 (U)
            new[]
            {A, C, B, D,  A, D, B, C,
             B, D, A, C,  B, C, A, D,
             C, A, D, B,  C, B, D, A,
             D, B, C, A,  D, A, C, B}, // SP2 (4)
            new[]
            {A, B, D, C,  A, C, D, B,
             B, A, C, D,  B, D, C, A,
             C, D, B, A,  C, A, B, D,
             D, C, A, B,  D, B, A, C},  // SP3 (Z)
        };

        /// <summary>
        /// Create a square-planar configuration around a provided focus atom. The
        /// carriers are flat in the plane and their arrangement is either, U-shape,
        /// 4-shape, or Z-shape.
        /// </summary>
        /// <param name="focus">the focus</param>
        /// <param name="carriers">the carriers</param>
        /// <param name="order">the configuration order, 1-3</param>
        public SquarePlanar(IAtom focus, IAtom[] carriers, int order)
            : base(focus, carriers, new StereoElement(StereoClass.SquarePlanar, order))
        {
            if (Configure.Order() < 0 || Configure.Order() > 3)
                throw new ArgumentException("Invalid configuration order, should be between 1-3");
        }

        public SquarePlanar(IAtom focus, IAtom[] carriers, StereoElement stereo)
            : this(focus, carriers, stereo.Configuration.Order())
        {
        }

        /// <summary>
        /// Normalize the configuration to the lowest configuration order (1) -
        /// U-shaped.
        /// </summary>
        /// <returns>the normalized configuration</returns>
        public SquarePlanar Normalize()
        {
            int cfg = Configure.Order();
            if (cfg == 1)
                return this;
            IAtom[] carriers = InvApply(Carriers, PERMUTATIONS[cfg - 1]);
            return new SquarePlanar(Focus, carriers, StereoElement.SquarePlanarU);
        }

        /// <inheritdoc/>
        protected override IStereoElement<IAtom, IAtom> Create(IAtom focus, IReadOnlyList<IAtom> carriers, StereoElement stereo)
        {
            return new SquarePlanar(focus, carriers.ToArray(), stereo);
        }
    }
}
