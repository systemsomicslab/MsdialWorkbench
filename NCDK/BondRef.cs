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

using NCDK.Numerics;
using System.Collections.Generic;

namespace NCDK
{
    /// <summary>
    /// A bond ref, references a CDK <see cref="IBond"/> indirectly. All
    /// methods are passed through to the referenced bond. The reference can
    /// be used to override the behaviour of the base bond.
    /// </summary>
    // @author John Mayfield 
    public class BondRef
        : ChemObjectRef, IBond
    {
        private readonly IBond bond;

        /// <summary>
        /// Create a pointer for the provided bond.
        /// </summary>
        /// <param name="bond">the bond to reference</param>
        public BondRef(IBond bond) : base(bond)
        {
            this.bond = bond;
        }

        /// <summary>
        /// Utility method to dereference an bond pointer. If the bond is not
        /// an <see cref="BondRef"/> it simply returns the input.
        /// </summary>
        /// <param name="bond">the bond</param>
        /// <returns>non-pointer bond</returns>
        public static IBond Deref(IBond bond)
        {
            while (bond is BondRef)
                bond = ((BondRef)bond).Deref();
            return bond;
        }

        /// <summary>
        /// Dereference the bond pointer once providing access to the base
        /// bond.
        /// </summary>
        /// <returns>the bond pointed to</returns>
        public IBond Deref()
        {
            return bond;
        }

        /// <inheritdoc/>
        public int? ElectronCount
        {
            get { return bond.ElectronCount; }
            set { bond.ElectronCount = value; }
        }

        /// <inheritdoc/>
        public virtual IList<IAtom> Atoms
        {
            get { return bond.Atoms; }
        }

        public virtual void SetAtoms(IEnumerable<IAtom> atoms)
        {
            bond.SetAtoms(atoms);
        }

        /// <inheritdoc/>
        public virtual IAtom Begin => bond.Begin;

        /// <inheritdoc/>
        public virtual IAtom End => bond.End;

        /// <inheritdoc/>
        public virtual int Index => bond.Index;

        /// <inheritdoc/>
        public virtual IAtomContainer Container => bond.Container;

        /// <inheritdoc/>
        public IAtom GetConnectedAtom(IAtom atom)
        {
            return bond.GetConnectedAtom(atom);
        }

        /// <inheritdoc/>
        public virtual IAtom GetOther(IAtom atom)
        {
            return bond.GetOther(atom);
        }

        /// <inheritdoc/>
        public IEnumerable<IAtom> GetConnectedAtoms(IAtom atom)
        {
            return bond.GetConnectedAtoms(atom);
        }

        /// <inheritdoc/>
        public bool Contains(IAtom atom)
        {
            return bond.Contains(atom);
        }

        /// <inheritdoc/>
        public BondOrder Order
        {
            get { return bond.Order; }
            set { bond.Order = value; }
        }

        /// <inheritdoc/>
        public BondStereo Stereo
        {
            get { return bond.Stereo; }
            set { bond.Stereo = value; }
        }

        /// <inheritdoc/>
        public BondDisplay Display
        {
            get { return bond.Display; }
            set { bond.Display = value; }
        }

        /// <inheritdoc/>
        public Vector2 GetGeometric2DCenter() => bond.GetGeometric2DCenter();

        /// <inheritdoc/>
        public Vector3 GetGeometric3DCenter() => bond.GetGeometric3DCenter();

        /// <inheritdoc/>
        public override bool Compare(object obj)
        {
            return bond.Compare(obj);
        }

        /// <inheritdoc/>
        public bool IsConnectedTo(IBond bond)
        {
            return this.bond.IsConnectedTo(bond);
        }

        /// <inheritdoc/>
        public bool IsAromatic
        {
            get { return bond.IsAromatic; }
            set { bond.IsAromatic = value; }
        }

        /// <inheritdoc/>
        public bool IsAliphatic
        {
            get { return bond.IsAliphatic; }
            set { bond.IsAliphatic = value; }
        }

        /// <inheritdoc/>
        public bool IsInRing
        {
            get { return bond.IsInRing; }
            set { bond.IsInRing = value; }
        }

        /// <inheritdoc/>
        public bool IsSingleOrDouble
        {
            get { return bond.IsSingleOrDouble; }
            set { bond.IsSingleOrDouble = value; }
        }

        /// <inheritdoc/>
        public bool IsReactiveCenter
        {
            get { return bond.IsReactiveCenter; }
            set { bond.IsReactiveCenter = value; }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return bond.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return bond.Equals(obj);
        }

        /// <inheritdoc/>
        public bool Equals(IBond other)
        {
            return bond.Equals(other);
        }

        /// <inheritdoc/>
        public override ICDKObject Clone(CDKObjectMap map)
        {
            return bond.Clone(map);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(BondRef)}({bond.ToString()})";
        }
    }
}
