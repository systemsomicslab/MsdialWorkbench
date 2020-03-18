/* Copyright (C) 2006-2007  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Numerics;
using System;
using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// Implements the concept of a covalent bond between two or more atoms. A bond is
    /// considered to be a number of electrons connecting two ore more atoms.
    /// type filter text
    /// </summary>
    // @author      egonw
    // @cdk.module interfaces
    // @cdk.created 2005-08-24
    // @cdk.keyword bond
    // @cdk.keyword atom
    // @cdk.keyword electron
    public interface IBond
        : IElectronContainer, IMolecularEntity
    {
        /// <summary>
        /// Atoms making up this bond.
        /// </summary>
        IList<IAtom> Atoms { get; }

        /// <summary>
        /// Sets atoms making up this bond.
        /// </summary>
        /// <param name="atoms">Atoms that forms this bond</param>
        void SetAtoms(IEnumerable<IAtom> atoms);

        /// <summary>
        /// The begin (or first) atom of the bond.
        /// </summary>
        /// <returns>the begin atom</returns>
        IAtom Begin { get; }

        /// <summary>
        /// The end (or second) atom of the bond.
        /// </summary>
        /// <returns>the end atom</returns>
#pragma warning disable CA1716 // Identifiers should not match keywords
        IAtom End { get; }
#pragma warning restore CA1716 // Identifiers should not match keywords

        int Index { get; }

        IAtomContainer Container { get; }

        /// <summary>
        /// Returns the other atom in the bond, the atom is connected to the given atom. This
        /// method is only correct for two-centre bonds, for n-centre bonds the behaviour is undefined
        /// and the more correct <see cref="GetConnectedAtoms(IAtom)"/> should be used.
        /// </summary>
        /// <example>
        /// <code>IAtom beg = bond.getBegin();
        /// IAtom end = bond.getEnd();
        /// // bond.getConnectedAtom(beg) == end
        /// // bond.getConnectedAtom(end) == beg
        /// </code>
        /// </example>
        /// <param name="atom">The atom the bond partner is searched of</param>
        /// <returns>the connected atom or null if the given atom is not part of the bond</returns>
        [Obsolete("use the method " + nameof(GetOther))]
        IAtom GetConnectedAtom(IAtom atom);

        /// <summary>
        /// Returns the other atom in the bond, the atom is connected to the given atom.This
        /// method is only correct for two-centre bonds, for n-centre bonds the behaviour is undefined
        /// and the more correct <see cref="GetConnectedAtoms(IAtom)"/> should be used.
        /// </summary>
        /// <example>
        /// <code>IAtom beg = bond.getBegin();
        /// IAtom end = bond.getEnd();
        /// // bond.getOther(beg) == end
        /// // bond.getOther(end) == beg
        /// </code>
        /// </example>
        /// <param name="atom">The atom the bond partner is searched of</param>
        /// <returns>the connected atom or null if the given atom is not part of the bond</returns>
        IAtom GetOther(IAtom atom);

        /// <summary>
        /// Returns all the atoms in the bond connected to the given atom.
        /// </summary>
        /// <param name="atom">The atoms the bond partner is searched of</param>
        /// <returns>the connected atoms or <see langword="null"/> if the given atom is not part of the bond</returns>
        IEnumerable<IAtom> GetConnectedAtoms(IAtom atom);

        /// <summary>
        /// Returns true if the given atom participates in this bond.
        /// </summary>
        /// <param name="atom">The atom to be tested if it participates in this bond</param>
        /// <returns>true if the atom participates in this bond</returns>
        bool Contains(IAtom atom);

        /// <summary>
        /// The bond order of this bond.
        /// </summary>
        BondOrder Order { get; set; }

        /// <summary>
        /// The stereo descriptor for this bond.
        /// </summary>
        /// <remarks>
        /// <note type="note">This function will also modify the bond display style.</note>
        /// </remarks>
        /// <seealso cref="Display"/>
        BondStereo Stereo { get; set; }

        /// <summary>
        /// The bond display style.
        /// </summary>
        BondDisplay Display { get; set; }

        /// <summary>
        /// Calculate the geometric 2D center of the bond.
        /// </summary>
        Vector2 GetGeometric2DCenter();

        /// <summary>
        /// Calculate the geometric 3D center of the bond.
        /// </summary>
        Vector3 GetGeometric3DCenter();

        /// <summary>
        /// Checks whether a bond is connected to another one. This can only be true if the bonds have an Atom in common.
        /// </summary>
        /// <param name="bond">The bond which is checked to be connect with this one</param>
        /// <returns><see langword="true"/>, if the bonds share an atom, otherwise <see langword="false"/></returns>
        bool IsConnectedTo(IBond bond);

        /// <summary>
        /// It's unclear whether the bond is a single or double bond.
        /// </summary>
        bool IsSingleOrDouble { get; set; }

        /// <summary>
        /// It has reactive center. It is used for example in reaction.
        /// </summary>
        bool IsReactiveCenter { get; set; }
    }
}
