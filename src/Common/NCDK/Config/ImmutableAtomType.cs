/* Copyright (C) 2012-2016  Egon Willighagen <egonw@users.sf.net>
 *               2012-2014  John May <john.wilkinsonmay@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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
using System.Collections.Generic;
using System.Text;

namespace NCDK.Config
{
    /// <summary>
    /// An immutable <see cref="IAtomType"/> implementation to support the <see cref="AtomTypeFactory"/>.
    /// </summary>
    // @author egonw
    internal sealed class ImmutableAtomType
        : IAtomType
    {
        public IChemObjectBuilder Builder => CDK.Builder;
        
        internal IAtomType baseAtomType;

        internal readonly IReadOnlyDictionary<string, object> properties = NCDK.Common.Collections.Dictionaries.Empty<string, object>();

        internal ChemicalElement element;
        internal int atomicNumber;
        internal string symbol;

        internal double? abundance;
        internal double? exactMass;
        internal int? massNumber;

        internal string atomTypeName;
        internal BondOrder maxBondOrder;
        internal double? bondOrderSum;
        internal int? formalCharge;
        internal int? formalNeighbourCount;
        internal Hybridization hybridization;
        internal double? covalentRadius;
        internal int? valency;
        internal bool isHydrogenBondAcceptor;
        internal bool isHydrogenBondDonor;
        internal bool isAromatic;
        internal bool isAliphatic;
        internal bool isInRing;
        internal bool isReactiveCenter = false;

        public bool Compare(object obj) => this == obj;

        public void RemoveProperty(object description) { throw new InvalidOperationException("Immutable atom type cannot be modified"); }
        public T GetProperty<T>(object description) => baseAtomType.GetProperty<T>(description);
        public T GetProperty<T>(object description, T defaltValue) => baseAtomType.GetProperty<T>(description, defaltValue);
        public void SetProperty(object key, object value) { throw new InvalidOperationException("Immutable atom type cannot be modified"); }
        public void SetProperties(IEnumerable<KeyValuePair<object, object>> properties) { throw new InvalidOperationException("Immutable atom type cannot be modified"); }
        public void AddProperties(IEnumerable<KeyValuePair<object, object>> properties) { throw new InvalidOperationException("Immutable atom type cannot be modified"); }
        public IReadOnlyDictionary<object, object> GetProperties() => baseAtomType.GetProperties();

        private static void ThrowModifyException()
        {
            throw new InvalidOperationException("Immutable atom type cannot be modified");
        }

        public bool IsPlaced { get { return false; } set { } }
        public bool IsVisited { get { return false; } set { } }

        public string Id
        {
            get { return null; }
            set { ThrowModifyException(); }
        }

        public bool IsHydrogenBondAcceptor
        {
            get { return isHydrogenBondAcceptor; }
            set { ThrowModifyException(); }
        }

        public bool IsHydrogenBondDonor
        {
            get { return isHydrogenBondDonor; }
            set { ThrowModifyException(); }
        }

        public bool IsAliphatic
        {
            get { return isAliphatic; }
            set { ThrowModifyException(); }
        }

        public bool IsAromatic
        {
            get { return isAromatic; }
            set { ThrowModifyException(); }
        }

        public bool IsInRing
        {
            get { return isAromatic; }
            set { ThrowModifyException(); }
        }

        public bool IsReactiveCenter
        {
            get { return isReactiveCenter; }
            set { ThrowModifyException(); }
        }

        public int AtomicNumber
        {
            get { return atomicNumber; }
            set { ThrowModifyException(); }
        }

        public string Symbol
        {
            get { return symbol; }
            set { ThrowModifyException(); }
        }

        public double? Abundance
        {
            get { return abundance; }
            set { ThrowModifyException(); }
        }

        public double? ExactMass
        {
            get { return exactMass; }
            set { ThrowModifyException(); }
        }

        public int? MassNumber
        {
            get { return massNumber; }
            set { ThrowModifyException(); }
        }

        public string AtomTypeName
        {
            get { return atomTypeName; }
            set { ThrowModifyException(); }
        }

        public BondOrder MaxBondOrder
        {
            get { return maxBondOrder; }
            set { ThrowModifyException(); }
        }

        public double? BondOrderSum
        {
            get { return bondOrderSum; }
            set { ThrowModifyException(); }
        }

        public int? FormalCharge
        {
            get { return formalCharge; }
            set { ThrowModifyException(); }
        }

        public int? FormalNeighbourCount
        {
            get { return formalNeighbourCount; }
            set { ThrowModifyException(); }
        }

        public Hybridization Hybridization
        {
            get { return hybridization; }
            set { ThrowModifyException(); }
        }

        public double? CovalentRadius
        {
            get { return covalentRadius; }
            set { ThrowModifyException(); }
        }

        public int? Valency
        {
            get { return valency; }
            set { ThrowModifyException(); }
        }

        public ChemicalElement Element => element;

        internal ImmutableAtomType(IAtomType type)
        {
            this.element = type.Element;
            this.symbol = type.Symbol;
            this.atomicNumber = type.AtomicNumber;
            this.abundance = type.Abundance;
            this.exactMass = type.ExactMass;
            this.massNumber = type.MassNumber;
            this.formalCharge = type.FormalCharge;
            this.hybridization = type.Hybridization;
            this.formalNeighbourCount = type.FormalNeighbourCount;
            this.atomTypeName = type.AtomTypeName;
            this.maxBondOrder = type.MaxBondOrder;
            this.bondOrderSum = type.BondOrderSum;
            this.covalentRadius = type.CovalentRadius;
            this.isHydrogenBondAcceptor = type.IsHydrogenBondAcceptor;
            this.isHydrogenBondDonor = type.IsHydrogenBondDonor;
            this.isAromatic = type.IsAromatic;
            this.isAliphatic = type.IsAliphatic;
            this.isInRing = this.IsInRing;
            this.baseAtomType = type;
            if (type.Valency != null)
            {
                this.valency = type.Valency;
            }
            else
            {
                var piBondCount = type.GetProperty<int?>(CDKPropertyName.PiBondCount);
                if (piBondCount != null && formalNeighbourCount != null)
                {
                    this.valency = (int)piBondCount + (int)formalNeighbourCount;
                }
                else
                {
                    this.valency = null;
                }
            }
        }

        public ICollection<IChemObjectListener> Listeners { get; } = Array.Empty<IChemObjectListener>();
        public bool Notification { get { return false; } set { } }
        public void NotifyChanged() { }

        public object Clone()
        {
            return Clone(null); // no need to new CDKObjectMap
        }

        public ICDKObject Clone(CDKObjectMap map)
        {
            return (ImmutableAtomType)this.MemberwiseClone();
        }

        public override string ToString()
        {
            var resultString = new StringBuilder();
            resultString.Append("ImmutableAtomType(").Append(GetHashCode());
            if (AtomTypeName != null)
            {
                resultString.Append(", N:").Append(AtomTypeName);
            }
            if (MaxBondOrder != BondOrder.Unset)
            {
                resultString.Append(", MBO:").Append(MaxBondOrder);
            }
            if (BondOrderSum != null)
            {
                resultString.Append(", BOS:").Append(BondOrderSum);
            }
            if (FormalCharge != null)
            {
                resultString.Append(", FC:").Append(FormalCharge);
            }
            if (Hybridization != Hybridization.Unset)
            {
                resultString.Append(", H:").Append(Hybridization);
            }
            if (FormalNeighbourCount != null)
            {
                resultString.Append(", NC:").Append(FormalNeighbourCount);
            }
            if (CovalentRadius != null)
            {
                resultString.Append(", CR:").Append(CovalentRadius);
            }
            if (Valency != null)
            {
                resultString.Append(", EV:").Append(Valency);
            }
            resultString.Append(", ").Append(base.ToString());
            resultString.Append(')');
            return resultString.ToString();
        }
    }
}
    