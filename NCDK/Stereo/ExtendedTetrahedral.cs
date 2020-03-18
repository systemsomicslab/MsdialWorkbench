/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
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

using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Stereo
{
    /// <summary>
    /// Extended tetrahedral configuration. Defines the winding configuration in
    /// a system with an even number of cumulated pi bonds. Examples include,
    /// (R)-laballenic acid (CHEBI:38401) and (S)-laballenic acid (CHEBI:38402).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The extended tetrahedral stereochemistry can be represented and handled the
    /// same as normal tetrahedral stereochemistry. However the handling of the
    /// neighbours is subtly different. To assist in the description here are how
    /// atoms are referred to.
    /// </para>
    /// <pre>
    /// p0           p2     p&lt;i&gt;: periphals
    ///  \          /       t&lt;i&gt;: terminals
    ///   t0 = f = t1       f:    focus
    ///  /          \
    /// p1           p3
    /// </pre>
    /// <para>
    /// The data structure stores, the central 'focus' atom and the four peripheral
    /// atoms. The peripheral atoms are stored in a single array, "{p0, p1, p2, p3}", 
    /// the first two and last two entries should be attached to the same
    /// terminal atom (t0 or t1). For convenience the terminal atoms can be found with
    /// <see cref="FindTerminalAtoms(IAtomContainer)"/> .
    /// </para>
    /// <pre>
    /// p0           p2          p0   p2
    ///  \          /              \ /
    ///   t0 = f = t1       -->     c       c: t0/f/t1
    ///  /          \              / \
    /// p1           p3           p1  p3
    /// </pre>
    /// <para>
    /// The configuration treats the focus and terminal atoms as a single atom, the
    /// neighbours "{ p1, p2, p3 }" then proceeded either clockwise or
    /// anti-clockwise when the centre (t0/f/t1) is viewed from the first peripheral
    /// atom "p0".
    /// </para>
    /// <para>
    /// If any of the peripherals are implicit hydrogen atoms, then the terminal atom
    /// to which the hydrogen is attached can be used as a placeholder.
    /// </para>
    /// </remarks>
    // @author John May
    // @cdk.keyword extended tetrahedral
    // @cdk.keyword allene
    // @cdk.keyword axial chirality
    public sealed class ExtendedTetrahedral
        : AbstractStereo<IAtom, IAtom>
    {
        /// <summary>
        /// Create an extended tetrahedral stereo element for the provided 'focus'
        /// and 'peripherals' in the given 'winding'. See class documentation an
        /// annotated storage description.
        /// </summary>
        /// <param name="focus">the central cumulated atom</param>
        /// <param name="peripherals">atoms attached to the terminal atoms</param>
        /// <param name="winding">the configuration</param>
        public ExtendedTetrahedral(IAtom focus, IEnumerable<IAtom> peripherals, TetrahedralStereo winding)
            : this(focus, peripherals, winding.ToConfiguration())
        {
        }

        public ExtendedTetrahedral(IAtom focus, IEnumerable<IAtom> peripherals, StereoConfigurations configure)
            : base(focus, peripherals.ToReadOnlyList(), new StereoElement(StereoClass.Allenal, configure))
        {
        }

        public ExtendedTetrahedral(IAtom focus, IEnumerable<IAtom> peripherals, StereoElement stereo)
            : this(focus, peripherals, stereo.Configuration)
        {
        }

        /// <summary>
        /// The neighbouring peripherals atoms, these are attached to the terminal
        /// atoms in the cumulated system.
        /// </summary>
        /// <returns>the peripheral atoms</returns>
        public IReadOnlyList<IAtom> Peripherals => Carriers;

        /// <summary>
        /// The winding of the peripherals, when viewed from the first atom.
        /// </summary>
        /// <returns>winding configuration</returns>
        public TetrahedralStereo Winding => Configure.ToStereo();

        private static IAtom GetOtherNbr(IAtomContainer mol, IAtom atom, IAtom other)
        {
            IAtom res = null;
            foreach (var bond in mol.GetConnectedBonds(atom))
            {
                if (bond.Order != BondOrder.Double)
                    continue;
                var nbr = bond.GetOther(atom);
                if (!nbr.Equals(other))
                {
                    if (res != null)
                        return null;
                    res = nbr;
                }
            }
            return res;
        }

        /// <summary>
        /// Helper method to locate two terminal atoms in a container for a given
        /// focus.
        /// </summary>
        /// <param name="container">structure representation</param>
        /// <param name="focus">cumulated atom</param>
        /// <returns>the terminal atoms (unordered)</returns>
        public static IAtom[] FindTerminalAtoms(IAtomContainer container, IAtom focus)
        {
            var focusBonds = container.GetConnectedBonds(focus);

            if (focusBonds.Count() != 2)
                throw new ArgumentException("focus must have exactly 2 neighbors");

            var leftPrev = focus;
            var rightPrev = focus;
            var left = focusBonds.ElementAt(0).GetOther(focus);
            var right = focusBonds.ElementAt(1).GetOther(focus);

            IAtom tmp;
            while (left != null && right != null)
            {
                tmp = GetOtherNbr(container, left, leftPrev);
                leftPrev = left;
                left = tmp;
                tmp = GetOtherNbr(container, right, rightPrev);
                rightPrev = right;
                right = tmp;
            }
            return new IAtom[] { leftPrev, rightPrev };
        }

        /// <summary>
        /// Helper method to locate two terminal atoms in a container for this
        /// extended tetrahedral element. The atoms are ordered such that the first
        /// index is attached to the first two peripheral atoms and the second index
        /// is attached to the second two peripheral atoms.
        /// </summary>
        /// <param name="container">structure representation</param>
        /// <returns>the terminal atoms (ordered)</returns>
        public IAtom[] FindTerminalAtoms(IAtomContainer container)
        {
            var atoms = FindTerminalAtoms(container, Focus);
            var carriers = Carriers;
            if (container.GetBond(atoms[0], carriers[2]) != null
             || container.GetBond(atoms[0], carriers[3]) != null)
            {
                var tmp = atoms[0];
                atoms[0] = atoms[1];
                atoms[1] = tmp;
            }
            return atoms;
        }

        protected override IStereoElement<IAtom, IAtom> Create(IAtom focus, IReadOnlyList<IAtom> carriers, StereoElement stereo)
        {
            return new ExtendedTetrahedral(focus, carriers, stereo);
        }
    }
}
