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
    /// An atom ref, references a CDK <see cref="IAtom"/> indirectly. All
    /// methods are passed through to the referenced atom. The reference can
    /// be used to override the behaviour of the base atom.
    /// </summary>
    // @author John Mayfield 
    public class AtomRef 
        : ChemObjectRef, IAtom
    {
        private readonly IAtom atom;

        /// <summary>
        /// Create a pointer for the provided atom.
        /// </summary>
        /// <param name="atom">the atom to reference</param>
        public AtomRef(IAtom atom) : base(atom)
        {
            this.atom = atom;
        }

        /// <summary>
        /// Utility method to dereference an atom. If the atom is not
        /// an <see cref="AtomRef"/> it simply returns the input.
        /// </summary>
        /// <param name="atom">the atom</param>
        /// <returns>non-pointer atom</returns>
        public static IAtom Deref(IAtom atom)
        {
            while (atom is AtomRef)
                atom = ((AtomRef)atom).Deref();
            return atom;
        }

        /// <summary>
        /// Dereference the atom pointer once providing access to the base
        /// atom.
        /// </summary>
        /// <returns>the atom pointed to</returns>
        public IAtom Deref()
        {
            return atom;
        }

        public ChemicalElement Element => atom.Element;

        /// <inheritdoc/>
        public double? Charge
        {
            get { return atom.Charge; }
            set { atom.Charge = value; }
        }

        /// <inheritdoc/>
        public int AtomicNumber
        {
            get { return atom.AtomicNumber; }
            set { atom.AtomicNumber = value; }
        }

        /// <inheritdoc/>
        public double? Abundance
        {
            get { return atom.Abundance; }
            set { atom.Abundance = value; }
        }

        /// <inheritdoc/>
        public int? ImplicitHydrogenCount
        {
            get { return atom.ImplicitHydrogenCount; }
            set { atom.ImplicitHydrogenCount = value; }
        }

        /// <inheritdoc/>
        public double? ExactMass
        {
            get { return atom.ExactMass; }
            set { atom.ExactMass = value; }
        }

        /// <inheritdoc/>
        public string Symbol
        {
            get { return atom.Symbol; }
            set { atom.Symbol = value; }
        }

        /// <inheritdoc/>
        public int? MassNumber
        {
            get { return atom.MassNumber; }
            set { atom.MassNumber = value; }
        }

        /// <inheritdoc/>
        public string AtomTypeName
        {
            get { return atom.AtomTypeName; }
            set { atom.AtomTypeName = value; }
        }

        /// <inheritdoc/>
        public BondOrder MaxBondOrder
        {
            get { return atom.MaxBondOrder; }
            set { atom.MaxBondOrder = value; }
        }

        /// <inheritdoc/>
        public double? BondOrderSum
        {
            get { return atom.BondOrderSum; }
            set { atom.BondOrderSum = value; }
        }

        /// <inheritdoc/>
        public Vector2? Point2D
        {
            get { return atom.Point2D; }
            set { atom.Point2D = value; }
        }

        /// <inheritdoc/>
        public Vector3? Point3D
        {
            get { return atom.Point3D; }
            set { atom.Point3D = value; }
        }

        /// <inheritdoc/>
        public int? FormalCharge
        {
            get { return atom.FormalCharge; }
            set { atom.FormalCharge = value; }
        }

        /// <inheritdoc/>
        public Vector3? FractionalPoint3D
        {
            get { return atom.FractionalPoint3D; }
            set { atom.FractionalPoint3D = value; }
        }

        /// <inheritdoc/>
        public int? FormalNeighbourCount
        {
            get { return atom.FormalNeighbourCount; }
            set { atom.FormalNeighbourCount = value; }
        }

        /// <inheritdoc/>
        public int StereoParity
        {
            get { return atom.StereoParity; }
            set { atom.StereoParity = value; }
        }

        /// <inheritdoc/>
        public Hybridization Hybridization
        {
            get { return atom.Hybridization; }
            set { atom.Hybridization = value; }
        }

        /// <inheritdoc/>
        public double? CovalentRadius
        {
            get { return atom.CovalentRadius; }
            set { atom.CovalentRadius = value; }
        }

        /// <inheritdoc/>
        public virtual IAtomContainer Container => atom.Container;

        /// <inheritdoc/>
        public virtual int Index => atom.Index;

        /// <inheritdoc/>
        public int? Valency
        {
            get { return atom.Valency; }
            set { atom.Valency = value; }
        }

        /// <inheritdoc/>
        public virtual IReadOnlyList<IBond> Bonds => atom.Bonds;

        /// <inheritdoc/>
        public virtual IBond GetBond(IAtom atom)
        {
            return this.atom.GetBond(atom);
        }

        /// <inheritdoc/>
        public bool IsAliphatic
        {
            get { return atom.IsAliphatic; }
            set { atom.IsAliphatic = value; }
        }

        /// <inheritdoc/>
        public bool IsAromatic
        {
            get { return atom.IsAromatic; }
            set { atom.IsAromatic = value; }
        }

        /// <inheritdoc/>
        public bool IsInRing
        {
            get { return atom.IsInRing; }
            set { atom.IsInRing = value; }
        }

        /// <inheritdoc/>
        public bool IsSingleOrDouble
        {
            get { return atom.IsSingleOrDouble; }
            set { atom.IsSingleOrDouble = value; }
        }

        /// <inheritdoc/>
        public bool IsHydrogenBondDonor
        {
            get { return atom.IsHydrogenBondDonor; }
            set { atom.IsHydrogenBondDonor = value; }
        }

        /// <inheritdoc/>
        public bool IsHydrogenBondAcceptor
        {
            get { return atom.IsHydrogenBondAcceptor; }
            set { atom.IsHydrogenBondAcceptor = value; }
        }

        /// <inheritdoc/>
        public bool IsReactiveCenter
        {
            get { return atom.IsReactiveCenter; }
            set { atom.IsReactiveCenter = value; }
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return atom.GetHashCode();
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return atom.Equals(obj);
        }

        /// <inheritdoc/>
        public bool Equals(IAtom other)
        {
            return atom.Equals(other);
        }

        /// <inheritdoc/>
        public override ICDKObject Clone(CDKObjectMap map)
        {
            return atom.Clone(map);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{nameof(AtomRef)}({atom.ToString()})";
        }
    }
}
