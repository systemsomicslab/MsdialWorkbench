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
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
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
    /// Represents the idea of an chemical atom.
    /// </summary>
    // @author      egonw
    // @cdk.created 2005-08-24
    // @cdk.keyword atom
    public interface IAtom
        : IAtomType
    {
        /// <summary>
        /// The partial charge of this atom.
        /// </summary>
        double? Charge { get; set; }

        /// <summary>
        /// The implicit hydrogen count of this atom.
        /// </summary>
        int? ImplicitHydrogenCount { get; set; }

        /// <summary>
        /// A point specifying the location of this atom in a 2D space.
        /// </summary>
        Vector2? Point2D { get; set; }

        /// <summary>
        /// A point specifying the location of this atom in 3D space.
        /// </summary>
        Vector3? Point3D { get; set; }

        /// <summary>
        /// A point specifying the location of this atom in a Crystal unit cell.
        /// </summary>
        Vector3? FractionalPoint3D { get; set; }

        /// <summary>
        /// The stereo parity of this atom. It uses the predefined values found in CDKConstants.
        /// </summary>
        [Obsolete("Use " + nameof(IStereoElement<IChemObject, IChemObject>) + " for storing stereochemistry")]
        int StereoParity { get; set; }

        /// <summary>
        /// Access the <see cref="IAtomContainer"/> of which this atom is a member of. Because atoms
        /// can be in multiple molecules this method will only work if the atom has been accessed
        /// in the context of an <see cref="IAtomContainer"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// IAtomContainer mol  = new AtomContainer();
        /// IAtom atom = new Atom();
        /// atom.Container; // null
        /// mol.Add(atom);
        /// atom.Container; // still null
        /// mol.Atoms[0].Container; // not-null, returns 'mol'
        /// </code>
        /// </example>
        /// <value>
        /// the atom container or null if not accessed in the context of a container
        /// </value>
        IAtomContainer Container { get; }

        /// <summary>
        /// Acces the index of an atom in the context of an <see cref="IAtomContainer"/>. If the
        /// index is not known, &lt; 0 is returned.
        /// </summary>
        int Index { get; }

        /// <summary>
        /// Returns the bonds connected to this atom. If the bonds are not
        /// known an exception is thrown. This method will only throw an exception
        /// if <see cref="Index"/> returns &lt; 0 or <see cref="Container"/> returns <see langword="null"/>.
        /// </summary>
        /// <example>
        /// <code>
        /// IAtom atom = ...;
        /// if (atom.Index >= 0) 
        /// {
        ///   foreach (IBond bond in atom.Bonds) 
        ///   {
        /// 
        ///   }
        /// }
        /// 
        /// if (atom.Container != null) 
        /// {
        ///   foreach (IBond bond in atom.Bonds)
        /// {
        /// 
        ///   }
        /// }
        /// 
        /// IAtomContainer mol = ...;
        /// // guaranteed not throw an exception
        /// foreach (IBond bond in mol.Atoms[i].Bonds)
        /// {
        /// 
        /// }
        /// </code>
        /// </example>
        /// <exception cref="System.InvalidOperationException">thrown if the bonds are not known</exception>
        IReadOnlyList<IBond> Bonds { get; }

        /// <summary>
        /// A way for the Smiles parser to indicate that this atom was written with a lower case letter, e.g. 'c' rather than 'C'.
        /// </summary>
        bool IsSingleOrDouble { get; set; }

        /// <summary>
        /// Returns the bond connecting 'this' atom to the provided atom. If the
        /// atoms are not bonded, null is returned.
        /// </summary>
        /// <param name="atom">the other atom</param>
        /// <returns>the bond connecting the atoms</returns>
        /// <exception cref="InvalidOperationException">thrown if the bonds are not known</exception>
        IBond GetBond(IAtom atom);
    }
}
