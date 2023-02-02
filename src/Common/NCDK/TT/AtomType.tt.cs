



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/*
 *  Copyright (C) 2001-2013  Christoph Steinbeck <steinbeck@users.sf.net>
 *                           John May <jwmay@users.sourceforge.net>
 *                           Egon Willighagen <egonw@users.sourceforge.net>
 *                           Rajarshi Guha <rajarshi@users.sourceforge.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

namespace NCDK.Default
{
    /// <summary>
    /// The base class for atom types. Atom types are typically used to describe the
    /// behaviour of an atom of a particular element in different environment like
    /// sp<sup>3</sup>
    /// hybridized carbon C3, etc., in some molecular modelling applications.
    /// </summary>
    // @author       steinbeck
    // @cdk.created  2001-08-08
    // @cdk.keyword  atom, type 
    public class AtomType
        : Isotope, IAtomType
    {
        /// <summary>
        /// The maximum bond order allowed for this atom type.
        /// </summary>
        internal BondOrder maxBondOrder;

        /// <summary>
        /// The maximum sum of all bond orders allowed for this atom type.
        /// </summary>
        internal double? bondOrderSum;

        /// <summary>
        /// The covalent radius of this atom type.
        /// </summary>
        internal double? covalentRadius;

        /// <summary>
        /// The formal charge of the atom with 0 as default.
        /// </summary>
        internal int? formalCharge;

        /// <summary>
        /// The hybridization state of this atom with <see cref="Hybridization.Unset"/>
        /// as default.
        /// </summary>
        internal Hybridization hybridization;

        /// <summary>
        /// The electron valency of this atom with <see langword="null"/> as default.
        /// </summary>
        internal int? valency;

        /// <summary>
        /// The formal number of neighbours this atom type can have with <see langword="null"/>
        /// as default. This includes explicitly and implicitly connected atoms, including
        /// implicit hydrogens.
        /// </summary>
        internal int? formalNeighbourCount;

        internal string atomTypeName;

        public AtomType(ChemicalElement element, int? formalCharge = 0)
            : base(element)
        {
            this.formalCharge = formalCharge;
        }

        public AtomType(int atomicNumber, int? formalCharge = 0)
            : this(ChemicalElement.Of(atomicNumber), formalCharge)
        {
        }

        /// <summary>
        /// Constructor for the AtomType object.
        /// </summary>
        /// <remarks>
        /// Defaults to a zero formal charge. All
        /// other fields are set to <see langword="null"/> or unset.
        /// </remarks>
        /// <param name="symbol">Symbol of the atom</param>
        public AtomType(string symbol, int? formalCharge = 0)
            : this(ChemicalElement.OfSymbol(symbol), formalCharge)
        {
        }

        /// <summary>
        /// Constructor for the AtomType object. Defaults to a zero formal charge.
        /// </summary>
        /// <param name="identifier">An id for this atom type, like C3 for sp3 carbon</param>
        /// <param name="symbol">The element symbol identifying the element to which this atom type applies</param>
        public AtomType(string identifier, string symbol)
            : this(ChemicalElement.OfSymbol(symbol))
        {
            this.atomTypeName = identifier;
        }

        /// <summary>
        /// Constructs an isotope by copying the symbol, atomic number,
        /// flags, identifier, exact mass, natural abundance and mass
        /// number from the given IIsotope. It does not copy the
        /// listeners and properties. If the element is an instanceof
        /// IAtomType, then the maximum bond order, bond order sum,
        /// van der Waals and covalent radii, formal charge, hybridization,
        /// electron valency, formal neighbour count and atom type name
        /// are copied too.
        /// </summary>
        /// <param name="element">IIsotope to copy information from</param>
        public AtomType(IElement element)
            : base(element)
        {
            if (element is IAtomType aa)
            {
                maxBondOrder = aa.MaxBondOrder;
                bondOrderSum = aa.BondOrderSum;
                covalentRadius = aa.CovalentRadius;
                formalCharge = aa.FormalCharge;
                hybridization = aa.Hybridization;
                valency = aa.Valency;
                formalNeighbourCount = aa.FormalNeighbourCount;
                atomTypeName = aa.AtomTypeName;
                SetIsHydrogenBondAcceptorWithoutNotify(aa.IsHydrogenBondAcceptor);
                SetIsHydrogenBondDonorWithoutNotify(aa.IsHydrogenBondDonor);
                SetIsAromaticWithoutNotify( aa.IsAromatic);
                SetIsInRingWithoutNotify(aa.IsInRing);
            }
        }

        /// <summary>
        /// The if attribute of the AtomType object.
        /// </summary>
        public virtual string AtomTypeName
        {
            get { return atomTypeName; }
            set 
            {
                atomTypeName = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The MaxBondOrder attribute of the AtomType object.
        /// </summary>
        public virtual BondOrder MaxBondOrder
        {
            get { return maxBondOrder; }
            set
            {
                maxBondOrder = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The the exact bond order sum attribute of the AtomType object.
        /// </summary>
        public virtual double? BondOrderSum
        {
            get { return bondOrderSum; }
            set 
            {
                bondOrderSum = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The formal charge of this atom.
        /// </summary>
        public virtual int? FormalCharge
        {
            get { return formalCharge; }
            set 
            { 
                formalCharge = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The formal neighbour count of this atom.
        /// </summary>
        public virtual int? FormalNeighbourCount
        {
            get { return formalNeighbourCount; }
            set
            { 
                formalNeighbourCount = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The hybridization of this atom.
        /// </summary>
        public virtual Hybridization Hybridization
        {
            get { return hybridization; }
            set { 
                hybridization = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// Compares a atom type with this atom type.
        /// </summary>
        /// <param name="obj">Object of type AtomType</param>
        /// <returns>true if the atom types are equal</returns>
        public override bool Compare(object obj)
        {
            return obj is IAtomType o && base.Compare(obj)
                && AtomTypeName == o.AtomTypeName
                && MaxBondOrder == o.MaxBondOrder
                && BondOrderSum == o.BondOrderSum;
        }

        /// <summary>
        /// The covalent radius for this AtomType.
        /// </summary>
        public virtual double? CovalentRadius
        {
            get { return covalentRadius; }
            set
            { 
                covalentRadius = value; 
                NotifyChanged();
            }
        }

        /// <summary>
        /// The the exact electron valency of the AtomType object.
        /// </summary>
        public virtual int? Valency
        {
            get { return valency; }
            set 
            {
                valency = value; 
                NotifyChanged();
            }
        }

        /// <inheritdoc/>
        public virtual bool IsHydrogenBondAcceptor
        {
            get 
            { 
                return (flags & CDKConstants.IsHydrogenBondAcceptorMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsHydrogenBondAcceptorWithoutNotify(value);
                NotifyChanged();
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsHydrogenBondAcceptorWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsHydrogenBondAcceptorMask;
            else
                flags &= ~CDKConstants.IsHydrogenBondAcceptorMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsHydrogenBondDonor
        {
            get 
            { 
                return (flags & CDKConstants.IsHydrogenBondDonorMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsHydrogenBondDonorWithoutNotify(value);
                NotifyChanged();
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsHydrogenBondDonorWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsHydrogenBondDonorMask;
            else
                flags &= ~CDKConstants.IsHydrogenBondDonorMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsAliphatic
        {
            get 
            { 
                return (flags & CDKConstants.IsAliphaticMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsAliphaticWithoutNotify(value);
                NotifyChanged();
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsAliphaticWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsAliphaticMask;
            else
                flags &= ~CDKConstants.IsAliphaticMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsAromatic
        {
            get 
            { 
                return (flags & CDKConstants.IsAromaticMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsAromaticWithoutNotify(value);
                NotifyChanged();
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsAromaticWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsAromaticMask;
            else
                flags &= ~CDKConstants.IsAromaticMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsInRing
        {
            get 
            { 
                return (flags & CDKConstants.IsInRingMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsInRingWithoutNotify(value);
                NotifyChanged();
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsInRingWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsInRingMask;
            else
                flags &= ~CDKConstants.IsInRingMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsReactiveCenter
        {
            get 
            { 
                return (flags & CDKConstants.IsReactiveCenterMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsReactiveCenterWithoutNotify(value);
                NotifyChanged();
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsReactiveCenterWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsReactiveCenterMask;
            else
                flags &= ~CDKConstants.IsReactiveCenterMask; 
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// The base class for atom types. Atom types are typically used to describe the
    /// behaviour of an atom of a particular element in different environment like
    /// sp<sup>3</sup>
    /// hybridized carbon C3, etc., in some molecular modelling applications.
    /// </summary>
    // @author       steinbeck
    // @cdk.created  2001-08-08
    // @cdk.keyword  atom, type 
    public class AtomType
        : Isotope, IAtomType
    {
        /// <summary>
        /// The maximum bond order allowed for this atom type.
        /// </summary>
        internal BondOrder maxBondOrder;

        /// <summary>
        /// The maximum sum of all bond orders allowed for this atom type.
        /// </summary>
        internal double? bondOrderSum;

        /// <summary>
        /// The covalent radius of this atom type.
        /// </summary>
        internal double? covalentRadius;

        /// <summary>
        /// The formal charge of the atom with 0 as default.
        /// </summary>
        internal int? formalCharge;

        /// <summary>
        /// The hybridization state of this atom with <see cref="Hybridization.Unset"/>
        /// as default.
        /// </summary>
        internal Hybridization hybridization;

        /// <summary>
        /// The electron valency of this atom with <see langword="null"/> as default.
        /// </summary>
        internal int? valency;

        /// <summary>
        /// The formal number of neighbours this atom type can have with <see langword="null"/>
        /// as default. This includes explicitly and implicitly connected atoms, including
        /// implicit hydrogens.
        /// </summary>
        internal int? formalNeighbourCount;

        internal string atomTypeName;

        public AtomType(ChemicalElement element, int? formalCharge = 0)
            : base(element)
        {
            this.formalCharge = formalCharge;
        }

        public AtomType(int atomicNumber, int? formalCharge = 0)
            : this(ChemicalElement.Of(atomicNumber), formalCharge)
        {
        }

        /// <summary>
        /// Constructor for the AtomType object.
        /// </summary>
        /// <remarks>
        /// Defaults to a zero formal charge. All
        /// other fields are set to <see langword="null"/> or unset.
        /// </remarks>
        /// <param name="symbol">Symbol of the atom</param>
        public AtomType(string symbol, int? formalCharge = 0)
            : this(ChemicalElement.OfSymbol(symbol), formalCharge)
        {
        }

        /// <summary>
        /// Constructor for the AtomType object. Defaults to a zero formal charge.
        /// </summary>
        /// <param name="identifier">An id for this atom type, like C3 for sp3 carbon</param>
        /// <param name="symbol">The element symbol identifying the element to which this atom type applies</param>
        public AtomType(string identifier, string symbol)
            : this(ChemicalElement.OfSymbol(symbol))
        {
            this.atomTypeName = identifier;
        }

        /// <summary>
        /// Constructs an isotope by copying the symbol, atomic number,
        /// flags, identifier, exact mass, natural abundance and mass
        /// number from the given IIsotope. It does not copy the
        /// listeners and properties. If the element is an instanceof
        /// IAtomType, then the maximum bond order, bond order sum,
        /// van der Waals and covalent radii, formal charge, hybridization,
        /// electron valency, formal neighbour count and atom type name
        /// are copied too.
        /// </summary>
        /// <param name="element">IIsotope to copy information from</param>
        public AtomType(IElement element)
            : base(element)
        {
            if (element is IAtomType aa)
            {
                maxBondOrder = aa.MaxBondOrder;
                bondOrderSum = aa.BondOrderSum;
                covalentRadius = aa.CovalentRadius;
                formalCharge = aa.FormalCharge;
                hybridization = aa.Hybridization;
                valency = aa.Valency;
                formalNeighbourCount = aa.FormalNeighbourCount;
                atomTypeName = aa.AtomTypeName;
                SetIsHydrogenBondAcceptorWithoutNotify(aa.IsHydrogenBondAcceptor);
                SetIsHydrogenBondDonorWithoutNotify(aa.IsHydrogenBondDonor);
                SetIsAromaticWithoutNotify( aa.IsAromatic);
                SetIsInRingWithoutNotify(aa.IsInRing);
            }
        }

        /// <summary>
        /// The if attribute of the AtomType object.
        /// </summary>
        public virtual string AtomTypeName
        {
            get { return atomTypeName; }
            set 
            {
                atomTypeName = value; 
            }
        }

        /// <summary>
        /// The MaxBondOrder attribute of the AtomType object.
        /// </summary>
        public virtual BondOrder MaxBondOrder
        {
            get { return maxBondOrder; }
            set
            {
                maxBondOrder = value; 
            }
        }

        /// <summary>
        /// The the exact bond order sum attribute of the AtomType object.
        /// </summary>
        public virtual double? BondOrderSum
        {
            get { return bondOrderSum; }
            set 
            {
                bondOrderSum = value; 
            }
        }

        /// <summary>
        /// The formal charge of this atom.
        /// </summary>
        public virtual int? FormalCharge
        {
            get { return formalCharge; }
            set 
            { 
                formalCharge = value; 
            }
        }

        /// <summary>
        /// The formal neighbour count of this atom.
        /// </summary>
        public virtual int? FormalNeighbourCount
        {
            get { return formalNeighbourCount; }
            set
            { 
                formalNeighbourCount = value; 
            }
        }

        /// <summary>
        /// The hybridization of this atom.
        /// </summary>
        public virtual Hybridization Hybridization
        {
            get { return hybridization; }
            set { 
                hybridization = value; 
            }
        }

        /// <summary>
        /// Compares a atom type with this atom type.
        /// </summary>
        /// <param name="obj">Object of type AtomType</param>
        /// <returns>true if the atom types are equal</returns>
        public override bool Compare(object obj)
        {
            return obj is IAtomType o && base.Compare(obj)
                && AtomTypeName == o.AtomTypeName
                && MaxBondOrder == o.MaxBondOrder
                && BondOrderSum == o.BondOrderSum;
        }

        /// <summary>
        /// The covalent radius for this AtomType.
        /// </summary>
        public virtual double? CovalentRadius
        {
            get { return covalentRadius; }
            set
            { 
                covalentRadius = value; 
            }
        }

        /// <summary>
        /// The the exact electron valency of the AtomType object.
        /// </summary>
        public virtual int? Valency
        {
            get { return valency; }
            set 
            {
                valency = value; 
            }
        }

        /// <inheritdoc/>
        public virtual bool IsHydrogenBondAcceptor
        {
            get 
            { 
                return (flags & CDKConstants.IsHydrogenBondAcceptorMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsHydrogenBondAcceptorWithoutNotify(value);
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsHydrogenBondAcceptorWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsHydrogenBondAcceptorMask;
            else
                flags &= ~CDKConstants.IsHydrogenBondAcceptorMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsHydrogenBondDonor
        {
            get 
            { 
                return (flags & CDKConstants.IsHydrogenBondDonorMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsHydrogenBondDonorWithoutNotify(value);
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsHydrogenBondDonorWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsHydrogenBondDonorMask;
            else
                flags &= ~CDKConstants.IsHydrogenBondDonorMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsAliphatic
        {
            get 
            { 
                return (flags & CDKConstants.IsAliphaticMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsAliphaticWithoutNotify(value);
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsAliphaticWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsAliphaticMask;
            else
                flags &= ~CDKConstants.IsAliphaticMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsAromatic
        {
            get 
            { 
                return (flags & CDKConstants.IsAromaticMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsAromaticWithoutNotify(value);
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsAromaticWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsAromaticMask;
            else
                flags &= ~CDKConstants.IsAromaticMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsInRing
        {
            get 
            { 
                return (flags & CDKConstants.IsInRingMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsInRingWithoutNotify(value);
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsInRingWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsInRingMask;
            else
                flags &= ~CDKConstants.IsInRingMask; 
        }
        /// <inheritdoc/>
        public virtual bool IsReactiveCenter
        {
            get 
            { 
                return (flags & CDKConstants.IsReactiveCenterMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsReactiveCenterWithoutNotify(value);
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsReactiveCenterWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsReactiveCenterMask;
            else
                flags &= ~CDKConstants.IsReactiveCenterMask; 
        }
    }
}
