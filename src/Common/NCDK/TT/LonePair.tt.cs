



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2002-2007  Egon Willighagen <egonw@users.sf.net>
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

using System;

namespace NCDK.Default
{
    /// <summary>
    /// A LonePair is an orbital primarily located with one Atom, containing
    /// two electrons.
    /// </summary>
    // @cdk.module data
    // @cdk.keyword orbital
    // @cdk.keyword lone-pair
    public class LonePair
        : ElectronContainer, ILonePair, ICloneable
    {
        /// <summary>Number of electrons in the lone pair.</summary>
        private readonly int electronCount = 2;

        /// <summary>The atom with which this lone pair is associated.</summary>
        private IAtom atom;

        /// <summary>
        /// Constructs an unconnected lone pair.
        /// </summary>
        public LonePair()
        {
            this.atom = null;
        }

        /// <summary>
        /// Constructs an lone pair on an Atom.
        /// </summary>
        /// <param name="atom">Atom to which this lone pair is connected</param>
        public LonePair(IAtom atom)
        {
            this.atom = atom;
        }

        /// <summary>
        /// The number of electrons in a LonePair.
        /// </summary>
        public override int? ElectronCount
        {
            get { return this.electronCount; }
        }

        /// <summary>
        /// The associated Atom.
        /// </summary>
        public IAtom Atom
        {
            get { return atom; }
            set
            {
                atom = value;
                 NotifyChanged();             }
        }

        /// <summary>
        /// Returns true if the given atom participates in this lone pair.
        /// </summary>
        /// <param name="atom">The atom to be tested if it participates in this bond</param>
        /// <returns>true if this lone pair is associated with the atom</returns>
        public bool Contains(IAtom atom)
        {
            return (this.atom.Equals(atom));
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (LonePair)base.Clone(map);
            clone.atom = (IAtom)atom?.Clone(map);
            return clone;
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// A LonePair is an orbital primarily located with one Atom, containing
    /// two electrons.
    /// </summary>
    // @cdk.module data
    // @cdk.keyword orbital
    // @cdk.keyword lone-pair
    public class LonePair
        : ElectronContainer, ILonePair, ICloneable
    {
        /// <summary>Number of electrons in the lone pair.</summary>
        private readonly int electronCount = 2;

        /// <summary>The atom with which this lone pair is associated.</summary>
        private IAtom atom;

        /// <summary>
        /// Constructs an unconnected lone pair.
        /// </summary>
        public LonePair()
        {
            this.atom = null;
        }

        /// <summary>
        /// Constructs an lone pair on an Atom.
        /// </summary>
        /// <param name="atom">Atom to which this lone pair is connected</param>
        public LonePair(IAtom atom)
        {
            this.atom = atom;
        }

        /// <summary>
        /// The number of electrons in a LonePair.
        /// </summary>
        public override int? ElectronCount
        {
            get { return this.electronCount; }
        }

        /// <summary>
        /// The associated Atom.
        /// </summary>
        public IAtom Atom
        {
            get { return atom; }
            set
            {
                atom = value;
                            }
        }

        /// <summary>
        /// Returns true if the given atom participates in this lone pair.
        /// </summary>
        /// <param name="atom">The atom to be tested if it participates in this bond</param>
        /// <returns>true if this lone pair is associated with the atom</returns>
        public bool Contains(IAtom atom)
        {
            return (this.atom.Equals(atom));
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (LonePair)base.Clone(map);
            clone.atom = (IAtom)atom?.Clone(map);
            return clone;
        }
    }
}
