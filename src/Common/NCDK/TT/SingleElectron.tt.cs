



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2004-2007  Egon Willighagen <egonw@users.sf.net>
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

using System;

namespace NCDK.Default
{
    /// <summary>
    /// A Single Electron is an orbital which is occupied by only one electron.
    /// </summary>
    /// <example>
    /// A radical in CDK is represented by an AtomContainer that contains an Atom
    /// and a SingleElectron type ElectronContainer:
    /// <code>
    /// AtomContainer radical = new AtomContainer();
    /// Atom carbon = new Atom("C");
    /// carbon.ImplicitHydrogenCount = 3;
    /// radical.SingleElectrons.Add(new SingleElectron(carbon));
    /// </code>
    /// </example>
    // @cdk.keyword radical
    // @cdk.keyword electron, unpaired
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class SingleElectron
        : ElectronContainer, ISingleElectron, ICloneable
    {
        private IAtom atom;

        /// <summary>
        /// Constructs an single electron orbital on an Atom.
        /// </summary>
        /// <param name="atom">The atom to which the single electron belongs.</param>
        public SingleElectron(IAtom atom)
        {
            this.atom = atom;
        }

        /// <summary>
        /// Constructs an single electron orbital with an associated Atom.
        /// </summary>
        public SingleElectron()
            : this(null)
        {
        }

        /// <summary>
        /// Number of electron for this class is defined as one.
        /// </summary>
        public override int? ElectronCount
        {
            get { return 1; }
            set {  }
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
        /// Returns true if the given atom participates in this SingleElectron.
        /// </summary>
        /// <param name="atom">The atom to be tested if it participates in this bond</param>
        /// <returns>true if this SingleElectron is associated with the atom</returns>
        public bool Contains(IAtom atom) => this.atom.Equals(atom);

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (SingleElectron)base.Clone(map);
            clone.atom = (IAtom)atom?.Clone(map);
            return clone;
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// A Single Electron is an orbital which is occupied by only one electron.
    /// </summary>
    /// <example>
    /// A radical in CDK is represented by an AtomContainer that contains an Atom
    /// and a SingleElectron type ElectronContainer:
    /// <code>
    /// AtomContainer radical = new AtomContainer();
    /// Atom carbon = new Atom("C");
    /// carbon.ImplicitHydrogenCount = 3;
    /// radical.SingleElectrons.Add(new SingleElectron(carbon));
    /// </code>
    /// </example>
    // @cdk.keyword radical
    // @cdk.keyword electron, unpaired
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class SingleElectron
        : ElectronContainer, ISingleElectron, ICloneable
    {
        private IAtom atom;

        /// <summary>
        /// Constructs an single electron orbital on an Atom.
        /// </summary>
        /// <param name="atom">The atom to which the single electron belongs.</param>
        public SingleElectron(IAtom atom)
        {
            this.atom = atom;
        }

        /// <summary>
        /// Constructs an single electron orbital with an associated Atom.
        /// </summary>
        public SingleElectron()
            : this(null)
        {
        }

        /// <summary>
        /// Number of electron for this class is defined as one.
        /// </summary>
        public override int? ElectronCount
        {
            get { return 1; }
            set {  }
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
        /// Returns true if the given atom participates in this SingleElectron.
        /// </summary>
        /// <param name="atom">The atom to be tested if it participates in this bond</param>
        /// <returns>true if this SingleElectron is associated with the atom</returns>
        public bool Contains(IAtom atom) => this.atom.Equals(atom);

        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (SingleElectron)base.Clone(map);
            clone.atom = (IAtom)atom?.Clone(map);
            return clone;
        }
    }
}
