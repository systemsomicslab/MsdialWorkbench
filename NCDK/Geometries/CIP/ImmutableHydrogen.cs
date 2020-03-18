/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
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
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;
using NCDK.Numerics;
using System;
using System.Collections;
using System.Collections.Generic;

namespace NCDK.Geometries.CIP
{
    /// <summary>
    /// Helper class to represent a immutable hydrogen. All set methods are void, and only
    /// <see cref="Symbol"/>, <see cref="AtomicNumber"/>, and <see cref="MassNumber"/> are
    /// implemented.
    /// </summary>
    // @author egonw
    // @cdk.module cip
    class ImmutableHydrogen : IAtom, ICloneable
    {
        private const string SymbolH = "H";

        public ImmutableHydrogen()
        {
        }

        /// <summary>
        /// This field is not used by this immutable hydrogen.
        /// Any setting will have no effect what so ever.
        /// </summary>
        public ChemicalElement Element => ChemicalElement.H;
        public double? Charge { get { return null; } set { } }
        public Vector3? FractionalPoint3D { get { return null; } set { } }
        public int? ImplicitHydrogenCount { get { return null; } set { } }
        public Vector2? Point2D { get { return null; } set { } }
        public Vector3? Point3D { get { return null; } set { } }
        public int StereoParity { get { return StereoAtomParities.Undefined; } set { } }
        public string AtomTypeName { get { return null; } set { } }
        public double? BondOrderSum { get { return null; } set { } }
        public int? FormalCharge { get { return null; } set { } }
        public int? FormalNeighbourCount { get { return null; } set { } }
        public Hybridization Hybridization { get { return Hybridization.Unset; } set { } }
        public BondOrder MaxBondOrder { get { return BondOrder.Unset; } set { } }
        public int? Valency { get { return null; } set { } }
        public double? ExactMass { get { return null; } set { } }
        public int? MassNumber { get { return 1; } set { } }
        public double? Abundance { get { return null; } set { } }
        public double? CovalentRadius { get { return null; } set { } }
        public int AtomicNumber { get { return 1; } set { } }
        public string Symbol { get { return SymbolH; } set { } }
        public bool IsHydrogenBondAcceptor { get { return false; } set { } }
        public bool IsHydrogenBondDonor { get { return false; } set { } }
        public bool IsAliphatic { get { return false; } set { } }
        public bool IsAromatic { get { return false; } set { } }
        public bool IsInRing { get { return false; } set { } }
        public bool IsReactiveCenter { get { return false; } set { } }
        public bool IsSingleOrDouble { get { return false; } set { } }
        public bool IsPlaced { get { return false; } set { } }
        public bool IsVisited { get { return false; } set { } }
        public string Id { get { return null; } set { } }
        public bool Notification { get { return false; } set { } }

        public IReadOnlyDictionary<object, object> GetProperties() => null;
        public T GetProperty<T>(object description) => default(T);
        public T GetProperty<T>(object description, T defaltValue) => default(T);
        public void RemoveProperty(object description) { }
        public void SetProperty(object key, object value) { }
        public void SetProperties(IEnumerable<KeyValuePair<object, object>> properties) { }
        public void AddProperties(IEnumerable<KeyValuePair<object, object>> properties) { }

        class ImmutableCollection : ICollection<IChemObjectListener>
        {
            private static EmptyEnumerable<IChemObjectListener> empty = new EmptyEnumerable<IChemObjectListener>();

            public int Count => 0;
            public bool IsReadOnly => false;
            public void Add(IChemObjectListener item) { }
            public void Clear() { }
            public bool Contains(IChemObjectListener item) => false;
            public void CopyTo(IChemObjectListener[] array, int arrayIndex) { }
            public IEnumerator<IChemObjectListener> GetEnumerator() => empty.GetEnumerator();
            public bool Remove(IChemObjectListener item) => false;
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        /// <summary>
        /// List for listener administration.
        /// </summary>
        public ICollection<IChemObjectListener> Listeners { get; } = new ImmutableCollection();

        public void NotifyChanged() { }

        public bool Compare(object obj)
        {
            return this == obj;
        }

        public object Clone() => this;
        public ICDKObject Clone(CDKObjectMap map) => (ICDKObject)Clone();
        public IChemObjectBuilder Builder => null;
        public IAtomContainer Container => null;
        public int Index => 0;

        private class DummyBonds : IReadOnlyList<IBond>
        {
            public IBond this[int index] => throw new InvalidOperationException();
            public int Count => 1;
            public IEnumerator<IBond> GetEnumerator() => throw new InvalidOperationException();
            IEnumerator IEnumerable.GetEnumerator() => throw new InvalidOperationException();
        }

        public IReadOnlyList<IBond> Bonds { get; } = new DummyBonds();

        public IBond GetBond(IAtom atom)
        {
            throw new InvalidOperationException();
        }

        public bool Equals(IAtom other)
        {
            return this == other;
        }
    }
}
