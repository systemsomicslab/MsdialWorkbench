



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

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

using NCDK.Isomorphisms.Matchers;
using NCDK.Sgroups;
using NCDK.Stereo;
using NCDK.Tools.Manipulator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace NCDK.Default
{
    /// <summary>
    /// This class should not be used directly.
    /// </summary>
    // @author John Mayfield
    internal sealed class AtomContainer2 
        : ChemObject, IAtomContainer
    {
        private static readonly IChemObjectBuilder builder = new ChemObjectBuilder(false);

        /// <inheritdoc/>
        public override IChemObjectBuilder Builder => builder;

        internal List<BaseAtomRef> atoms;
        internal List<BaseBondRef> bonds;
        internal ObservableChemObjectCollection<ILonePair> lonepairs;
        internal ObservableChemObjectCollection<ISingleElectron> electrons;
        internal List<IStereoElement<IChemObject, IChemObject>> stereo;

        /// <inheritdoc/>
        // TODO: Implements ElectronContainers
        public IList<IAtom> Atoms { get; private set; }
        public IList<IBond> Bonds { get; private set; }
        public IList<ILonePair> LonePairs { get; private set; }
        public IList<ISingleElectron> SingleElectrons { get; private set; }
        // TODO: handle notification
        public ICollection<IStereoElement<IChemObject, IChemObject>> StereoElements { get; private set; }

        /// <summary>
        /// Create a new container with the specified capacities.
        /// </summary>
        /// <param name="numAtoms">expected number of atoms</param>
        /// <param name="numBonds">expected number of bonds</param>
        /// <param name="numLonePairs">expected number of lone pairs</param>
        /// <param name="numSingleElectrons">expected number of single electrons</param>
        internal AtomContainer2(
            int numAtoms,
            int numBonds,
            int numLonePairs,
            int numSingleElectrons)
        {
            this.atoms = new List<BaseAtomRef>(numAtoms);
            this.bonds = new List<BaseBondRef>(numBonds);
            this.lonepairs = new ObservableChemObjectCollection<ILonePair>(numLonePairs, this);
            this.electrons = new ObservableChemObjectCollection<ISingleElectron>(numSingleElectrons, this);
            this.stereo = new List<IStereoElement<IChemObject, IChemObject>>();

            this.Atoms = new BaseAtomRef_Collection(this);
            this.Bonds = new BaseBondRef_Collection(this);
            this.LonePairs = lonepairs;
            this.SingleElectrons = electrons;
            this.StereoElements = stereo;
        }

        /// <summary>
        /// Constructs an empty AtomContainer.
        /// </summary>
        public AtomContainer2()
            : this(0, 0, 0, 0)
        {
        }

        /// <summary>
        /// Constructs a shallow copy of the provided IAtomContainer with the same
        /// atoms, bonds, electron containers and stereochemistry of another
        /// AtomContainer. Removing atoms/bonds in this copy will not affect
        /// the original, however changing the properties will.
        /// </summary>
        /// <param name="src">the source atom container</param>
        public AtomContainer2(IAtomContainer src)
            : this(
                src.Atoms.Count,
                src.Bonds.Count,
                src.LonePairs.Count,
                src.SingleElectrons.Count)
        {
            foreach (var atom in src.Atoms)
                Atoms.Add(atom);
            foreach (var bond in src.Bonds)
                Bonds.Add(bond);
            foreach (var se in src.SingleElectrons)
                SingleElectrons.Add(se);
            foreach (var lp in src.LonePairs)
                LonePairs.Add(lp);
            foreach (var se in src.StereoElements)
                StereoElements.Add(se);
        }

        public AtomContainer2(IEnumerable<IAtom> atoms, IEnumerable<IBond> bonds)
            : this()
        {
            foreach (var atom in atoms)
                Atoms.Add(atom);
            foreach (var bond in bonds)
                Bonds.Add(bond);
        }

        /// <inheritdoc/>
        public  bool IsAromatic
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
        public  bool IsSingleOrDouble
        {
            get 
            { 
                return (flags & CDKConstants.IsSingleOrDoubleMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsSingleOrDoubleWithoutNotify(value);
                NotifyChanged();
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsSingleOrDoubleWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsSingleOrDoubleMask;
            else
                flags &= ~CDKConstants.IsSingleOrDoubleMask; 
        }

        internal class BaseAtomRef : AtomRef
        {
            private int index = -1;
            internal readonly IAtomContainer mol;
            internal readonly List<IBond> bonds = new List<IBond>(4);

            public BaseAtomRef(IAtomContainer mol, IAtom atom)
                : base(atom)
            {
                this.mol = mol;
            }

            public override int Index => index;
            public void SetIndex(int index) => this.index = index;
            public override IAtomContainer Container => mol;

            public override IReadOnlyList<IBond> Bonds => bonds;

            public override IBond GetBond(IAtom atom)
            {
                foreach (var bond in bonds)
                {
                    if (bond.GetOther(this).Equals(atom))
                        return bond;
                }
                return null;
            }

            public override int GetHashCode()
            {
                return Deref().GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is AtomRef)
                    return Deref().Equals(((AtomRef)obj).Deref());
                return Deref().Equals(obj);
            }

            public override string ToString()
            {
                return Deref().ToString();
            }
        }

        internal class BaseBondRef : BondRef
        {
            private int index;
            private readonly AtomContainer2 mol;
            private BaseAtomRef beg, end;

            public BaseBondRef(AtomContainer2 mol, IBond bond, BaseAtomRef beg, BaseAtomRef end)
                : base(bond)
            {
                this.mol = mol;
                this.beg = beg;
                this.end = end;
            }

            public override int Index => index;

            public void SetIndex(int index)
            {
                this.index = index;
            }

            public override IAtomContainer Container => mol;

            public override IAtom Begin => beg;
            public override IAtom End => end;

            internal BaseAtomRef GetBegin() => beg;
            internal BaseAtomRef GetEnd() => end;

            private IList<IAtom> internalAtoms;

            /// <inheritdoc/>
            public override IList<IAtom> Atoms
            {
                get
                {
                    if (internalAtoms == null)
                        internalAtoms = new InternalAtoms(this, base.Atoms);
                    return internalAtoms;
                }
            }

            internal class InternalAtoms : IList<IAtom>
            {
                BaseBondRef parent;
                IList<IAtom> baseList;

                public InternalAtoms(BaseBondRef parent, IList<IAtom> baseList)
                {
                    this.parent = parent;
                    this.baseList = baseList;
                }

                public IAtom this[int index]
                {
                    get
                    {
                        switch (index)
                        {
                            case 0:
                                return parent.Begin;
                            case 1:
                                return parent.End;
                            default:
                                return parent.mol.GetAtomRef(baseList[index]);
                        }
                    }

                    set
                    {
                        baseList[index] = value;
                        if (index == 0)
                        {
                            if (parent.beg != null)
                                parent.beg.bonds.Remove(parent);
                            parent.beg = parent.mol.GetAtomRef(value);
                            parent.beg.bonds.Add(parent);
                        }
                        else if (index == 1)
                        {
                            if (parent.end != null)
                                parent.end.bonds.Remove(parent);
                            parent.end = parent.mol.GetAtomRef(value);
                            parent.end.bonds.Add(parent);
                        }
                    }
                }

                public int Count => baseList.Count;
                public bool IsReadOnly => baseList.IsReadOnly;
                public void Add(IAtom item) => baseList.Add(item);
                public void Clear() => baseList.Clear();
                public bool Contains(IAtom item) => baseList.Contains(item);
                public void CopyTo(IAtom[] array, int arrayIndex) => baseList.CopyTo(array, arrayIndex);
                public IEnumerator<IAtom> GetEnumerator() => baseList.GetEnumerator();
                public int IndexOf(IAtom item) => baseList.IndexOf(item);
                public void Insert(int index, IAtom item) => baseList.Insert(index, item);
                public bool Remove(IAtom item) => baseList.Remove(item);
                public void RemoveAt(int index) => baseList.RemoveAt(index);
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public AtomRef GetOtherRef(IAtom atom)
            {
                if (atom == beg)
                    return end;
                else if (atom == end)
                    return beg;
                atom = AtomContainer2.Unbox(atom);
                if (atom == beg.Deref())
                    return end;
                else if (atom == end.Deref())
                    return beg;
                return null;
            }

            public override IAtom GetOther(IAtom atom) => GetOtherRef(atom);

            public override void SetAtoms(IEnumerable<IAtom> atoms_)
            {
                var atoms = atoms_.ToReadOnlyList();
                Trace.Assert(atoms.Count == 2);
                base.SetAtoms(atoms);
                // check for swap: intended ref check
                if (object.Equals(atoms[0], end) && object.Equals(atoms[1], beg))
                {
                    var tmp = beg;
                    beg = end;
                    end = tmp;
                    return;
                }
                if (beg != null)
                    beg.bonds.Remove(this);
                if (end != null)
                    end.bonds.Remove(this);
                beg = mol.GetAtomRef(atoms[0]);
                end = mol.GetAtomRef(atoms[1]);
                beg.bonds.Add(this);
                end.bonds.Add(this);
            }

            public override int GetHashCode()
            {
                return Deref().GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is BondRef)
                    return Deref().Equals(((BondRef)obj).Deref());
                return Deref().Equals(obj);
            }
        }

        internal sealed class PsuedoAtomRef : BaseAtomRef, IPseudoAtom
        {
            private readonly IPseudoAtom pseudo;

            public PsuedoAtomRef(IAtomContainer mol, IPseudoAtom atom)
                : base(mol, atom)
            {
                this.pseudo = atom;
            }

            public string Label
            {
                get => pseudo.Label;
                set => pseudo.Label = value;
            }

            public int AttachPointNum
            {
                get => pseudo.AttachPointNum;
                set => pseudo.AttachPointNum = value;
            }

            public override int GetHashCode()
            {
                return Deref().GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is BondRef)
                    return Deref().Equals(((BondRef)obj).Deref());
                return Deref().Equals(obj);
            }
        }

        internal sealed class QueryAtomRef : BaseAtomRef, IQueryAtom
        {
            private readonly IQueryAtom qatom;

            public QueryAtomRef(IAtomContainer mol, IQueryAtom atom)
                : base(mol, atom)
            {
                this.qatom = atom;
            }

            public bool Matches(IAtom atom)
            {
                return qatom.Matches(atom);
            }
        }

        internal sealed class PdbAtomRef : BaseAtomRef, IPDBAtom
        {
            private readonly IPDBAtom pdbAtom;

            public PdbAtomRef(IAtomContainer mol, IPDBAtom atom)
                : base(mol, atom)
            {
                this.pdbAtom = atom;
            }

            public string Record { get => pdbAtom.Record; set => pdbAtom.Record = value; }
            public double? TempFactor { get => pdbAtom.TempFactor; set => pdbAtom.TempFactor = value; }
            public string ResName { get => pdbAtom.ResName; set => pdbAtom.ResName = value; }
            public string ICode { get => pdbAtom.ICode; set => pdbAtom.ICode = value; }
            public string Name { get => pdbAtom.Name; set => pdbAtom.Name = value; }
            public string ChainID { get => pdbAtom.ChainID; set => pdbAtom.ChainID = value; }
            public string AltLoc { get => pdbAtom.AltLoc; set => pdbAtom.AltLoc = value; }
            public string SegID { get => pdbAtom.SegID; set => pdbAtom.SegID = value; }
            public int? Serial { get => pdbAtom.Serial; set => pdbAtom.Serial = value; }
            public string ResSeq { get => pdbAtom.ResSeq; set => pdbAtom.ResSeq = value; }
            public bool Oxt { get => pdbAtom.Oxt; set => pdbAtom.Oxt = value; }
            public bool? HetAtom { get => pdbAtom.HetAtom; set => pdbAtom.HetAtom = value; }
            public double? Occupancy { get => pdbAtom.Occupancy; set => pdbAtom.Occupancy = value; }
        }

        internal sealed class QueryBondRef 
            : BaseBondRef, IQueryBond
        {
            public QueryBondRef(AtomContainer2 mol,
                                IQueryBond bond,
                                BaseAtomRef beg,
                                BaseAtomRef end)
                : base(mol, bond, beg, end)
            {
            }

            public bool Matches(IBond bond)
            {
                return ((IQueryBond)Deref()).Matches(bond);
            }
        }

        internal abstract class A_Collection<TInterface, TRef> : IList<TInterface> where TRef : TInterface
        {
            List<TRef> list;

            public A_Collection(List<TRef> list)
            {
                this.list = list;
            }

            public abstract TInterface this[int index] { get; set; }
            public int Count => list.Count;
            public bool IsReadOnly => false;
            public abstract void Add(TInterface item);
            public abstract void Clear();
            public bool Contains(TInterface item) => list.Contains((TRef)item);
            public void CopyTo(TInterface[] array, int arrayIndex)
            {
                for (int i = 0; i < list.Count; i++)
                    array[arrayIndex + i] = list[i];
            }            
            public IEnumerator<TInterface> GetEnumerator() => list.Cast<TInterface>().GetEnumerator();
            public abstract int IndexOf(TInterface atom);
            public void Insert(int index, TInterface item)
            {
                list.Insert(index, default(TRef));
                this[index] = item;
            }
            public abstract bool Remove(TInterface item);
            public abstract void RemoveAt(int index);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal class BaseAtomRef_Collection : A_Collection<IAtom, BaseAtomRef>
        {
            AtomContainer2 parent;

            public BaseAtomRef_Collection(AtomContainer2 parent)
                : base(parent.atoms)
            {
                this.parent = parent;
            }

            public override IAtom this[int index]
            {
                get
                {
                    if (index >= this.parent.atoms.Count)
                        throw new ArgumentOutOfRangeException(nameof(index), $"No atom at index: {index}");
                    return parent.atoms[index];
                }

                set
                {
                    var atom = value;
                    if (atom == null)
                        throw new NullReferenceException("Null atom provided");
                    if (parent.atoms.Contains(atom))
                        throw new InvalidOperationException($"Atom already in container at index: {this.IndexOf(atom)}");
                    if (index < 0 || index >= parent.atoms.Count)
                        throw new ArgumentOutOfRangeException(nameof(index), $"No current atom at index: {index}");
                    var rep = parent.NewAtomRef(atom);
                    var org = parent.atoms[index];
                    parent.atoms[index] = rep;
                    parent.atoms[index].SetIndex(index);
                    foreach (var bond in org.Bonds.ToList())
                    {
                        if (bond.Begin.Equals(org))
                            bond.Atoms[0] = rep;
                        else if (bond.End.Equals(org))
                            bond.Atoms[1] = rep;
                    }

                    // update single electrons and lone pairs
                    foreach (var ec in parent.electrons)
                    {
                        if (org.Equals(ec.Atom))
                            ec.Atom = rep;
                    }
                    foreach (var lp in parent.lonepairs)
                    {
                        if (org.Equals(lp.Atom))
                            lp.Atom = rep;
                    }

                    // update stereo
                    for (int i = 0; i < parent.stereo.Count; i++)
                    {
                        var se = parent.stereo[i];
                        if (se.Contains(org))
                        {
                            var map = new CDKObjectMap();
                            map.Add(org, rep);
                            parent.stereo[i] = (IStereoElement<IChemObject, IChemObject>)se.Clone(map);
                        }
                    }
                    org.Listeners.Remove(parent);
                    rep.Listeners.Add(parent);
                    parent.NotifyChanged();
                }
            }

            public override int IndexOf(IAtom atom)
            {
                var aref = parent.GetAtomRefUnsafe(atom);
                return aref == null ? -1 : aref.Index;
            }

            public override void Add(IAtom atom)
            {
                if (parent.Contains(atom))
                    return;
                var aref = parent.NewAtomRef(atom);
                aref.SetIndex(parent.atoms.Count);
                parent.atoms.Add(aref);
                aref.Listeners.Add(parent);
                parent.NotifyChanged();
            }

            public override void RemoveAt(int index)
            {
                parent.atoms[index].Listeners.Remove(parent);
                parent.atoms[index].SetIndex(-1);
                for (int i = index; i < parent.atoms.Count - 1; i++)
                {
                    parent.atoms[i] = parent.atoms[i + 1];
                    parent.atoms[i].SetIndex(i);
                }
                parent.atoms.RemoveAt(parent.atoms.Count - 1);
                parent.NotifyChanged();
            }

            public override bool Remove(IAtom item)
            {
                var index = parent.Atoms.IndexOf(item);
                if (index != -1)
                {
                    RemoveAt(index);
                    return true;
                }
                return false;
            }

            public override void Clear()
            {
                foreach (var item in parent.atoms)
                    item.Listeners.Remove(parent);
                parent.atoms.Clear();
                parent.OnStateChanged(new ChemObjectChangeEventArgs(this));
            }
        }

        internal class BaseBondRef_Collection : A_Collection<IBond, BaseBondRef>
        {
            AtomContainer2 parent;

            public BaseBondRef_Collection(AtomContainer2 parent)
                : base(parent.bonds)
            {
                this.parent = parent;
            }

            public override IBond this[int index]
            {
                get
                {
                    if (index >= parent.bonds.Count)
                        throw new ArgumentOutOfRangeException(nameof(index), $"No bond at index: {index}");
                    return parent.bonds[index];
                }

                set
                {
                    throw new InvalidOperationException();
                }
            }

            public override int IndexOf(IBond bond)
            {
                var bref = parent.GetBondRefUnsafe(bond);
                return bref == null ? -1 : bref.Index;
            }

            public override void Add(IBond bond)
            {
                var bref = parent.NewBondRef(bond);
                bref.SetIndex(parent.bonds.Count);
                parent.AddToEndpoints(bref);
                parent.bonds.Add(bref);
                bref.Listeners.Add(parent);
                parent.NotifyChanged();
            }

            public override void RemoveAt(int index)
            {
                var bond = parent.bonds[index];
                parent.bonds[index].SetIndex(-1);
                for (int i = index; i < parent.bonds.Count - 1; i++)
                {
                    parent.bonds[i] = parent.bonds[i + 1];
                    parent.bonds[i].SetIndex(i);
                }
                parent.bonds.RemoveAt(parent.bonds.Count - 1);
                parent.DelFromEndpoints(bond);
                bond.Listeners.Remove(parent);
                parent.NotifyChanged();
            }

            public override bool Remove(IBond item)
            {
                var index = parent.Bonds.IndexOf(item);
                if (index != -1)
                {
                    RemoveAt(index);
                    return true;
                }
                return false;
            }

            public override void Clear()
            {
                foreach (var item in parent.bonds)
                    item.Listeners.Remove(parent);
                parent.bonds.Clear();
                parent.ClearAdjacency();
                parent.OnStateChanged(new ChemObjectChangeEventArgs(this));
            }
        }

        internal static IAtom Unbox(IAtom atom)
        {
            while (atom is AtomRef)
                atom = ((AtomRef)atom).Deref();
            return atom;
        }

        internal static IBond Unbox(IBond bond)
        {
            while (bond is BondRef)
                bond = ((BondRef)bond).Deref();
            return bond;
        }

        internal BaseAtomRef GetAtomRefUnsafe(IAtom atom)
        {
            if (atom.Container == this &&
                atom.Index != -1 &&
                atoms[atom.Index] == atom)
                return (BaseAtomRef)atom;
            atom = Unbox(atom);
            for (int i = 0; i < atoms.Count; i++)
                if (object.Equals(atoms[i], atom))
                    return atoms[i];
            return null;
        }

        internal BaseAtomRef GetAtomRef(IAtom atom)
        {
            var atomref = GetAtomRefUnsafe(atom);
            if (atomref == null)
                throw new NoSuchAtomException("Atom is not a member of this AtomContainer");
            return atomref;
        }

        internal BaseAtomRef NewAtomRef(IAtom atom)
        {
            // most common implementation we'll encounter..
            if (atom.GetType() == typeof(Atom))
                return new BaseAtomRef(this, atom);
            atom = Unbox(atom);
            // re-check the common case now we've un-boxed
            if (atom.GetType() == typeof(Atom))
                return new BaseAtomRef(this, atom);
            switch (atom)
            {
                case IPseudoAtom _atom:
                    return new PsuedoAtomRef(this, _atom);
                case IQueryAtom _atom:
                    return new QueryAtomRef(this, _atom);
                case IPDBAtom _atom:
                    return new PdbAtomRef(this, _atom);
            }
            return new BaseAtomRef(this, atom);
        }

        private BondRef GetBondRefUnsafe(IBond bond)
        {
            if (bond.Container == this 
             && bond.Index != -1 
             && bonds[bond.Index] == bond)
                return (BondRef)bond;
            bond = Unbox(bond);
            for (int i = 0; i < bonds.Count; i++)
                if (bonds[i].Deref() == bond)
                    return bonds[i];
            return null;
        }

        private BaseBondRef NewBondRef(IBond bond)
        {
            var beg = bond.Begin == null ? null : GetAtomRef(bond.Begin);
            var end = bond.End == null ? null : GetAtomRef(bond.End);
            if (bond.GetType() == typeof(Bond))
                return new BaseBondRef(this, bond, beg, end);
            bond = Unbox(bond);
            switch (bond)
            {
                case IQueryBond _bond:
                    return new QueryBondRef(this, _bond, beg, end);
            }
            return new BaseBondRef(this, bond, beg, end);
        }

        private void AddToEndpoints(BaseBondRef bondref)
        {
            if (bondref.Atoms.Count == 2)
            {
                var beg = GetAtomRef(bondref.Begin);
                var end = GetAtomRef(bondref.End);
                beg.bonds.Add(bondref);
                end.bonds.Add(bondref);
            }
            else
            {
                for (int i = 0; i < bondref.Atoms.Count; i++)
                {
                    GetAtomRef(bondref.Atoms[i]).bonds.Add(bondref);
                }
            }
        }

        private void DelFromEndpoints(BondRef bondref)
        {
            for (int i = 0; i < bondref.Atoms.Count; i++)
            {
                var aref = GetAtomRefUnsafe(bondref.Atoms[i]);
                // atom may have already been deleted, naughty!
                if (aref != null)
                    aref.bonds.Remove(bondref);
            }
        }

        private void ClearAdjacency()
        {
            for (int i = 0; i < atoms.Count; i++)
            {
                atoms[i].bonds.Clear();
            }
        }

        public void SetAtoms(IEnumerable<IAtom> newatoms)
        {
            var _newatoms = newatoms.ToList();
            bool reindexBonds = false;

            for (int i = 0; i < _newatoms.Count; i++)
            {
                if (i >= atoms.Count)
                    atoms.Add(null);

                if (_newatoms[i].Container == this)
                {
                    atoms[i] = (BaseAtomRef)_newatoms[i];
                    atoms[i].SetIndex(i);
                }
                else
                {
                    if (atoms[i] != null)
                        atoms[i].Listeners.Remove(this);
                    atoms[i] = NewAtomRef(_newatoms[i]);
                    atoms[i].SetIndex(i);
                    atoms[i].Listeners.Add(this);
                    reindexBonds = true;
                }
            }

            // delete rest of the array
            {
                for (int i = atoms.Count - 1; i >= _newatoms.Count; i--)
                {
                    atoms[i].Listeners.Remove(this);
                    atoms.RemoveAt(i);
                }
            }

            // ensure adjacency information is in sync, this code is only
            // called if the bonds are non-empty and 'external' atoms have been
            // added
            if (bonds.Count > 0 && reindexBonds)
            {
                ClearAdjacency();
                for (int i = 0; i < bonds.Count; i++)
                    AddToEndpoints(bonds[i]);
            }

            NotifyChanged();
        }

        public void SetBonds(IEnumerable<IBond> newbonds)
        {
            var _newbonds = newbonds.ToList();

            // replace existing bonds to clear their adjacency
            if (bonds.Count > 0) 
            {
                ClearAdjacency();
            }

            for (int i = 0; i < _newbonds.Count; i++) 
            {
                if (i >= bonds.Count)
                    bonds.Add(null);

                var bondRef = NewBondRef(_newbonds[i]);
                bondRef.SetIndex(i);
                AddToEndpoints(bondRef);
                if (bonds[i] != null)
                    bonds[i].Listeners.Remove(this);
                bonds[i] = bondRef;
                bondRef.Listeners.Add(this);
            }
            for (int i = bonds.Count - 1; i >= _newbonds.Count; i--)
                bonds.RemoveAt(i);

            NotifyChanged();
        }

        public IElectronContainer GetElectronContainer(int number)
        {
            if (number < bonds.Count) 
                return bonds[number];
            number -= bonds.Count;
            if (number < lonepairs.Count) 
                return lonepairs[number];
            number -= lonepairs.Count;
            if (number < electrons.Count) 
                return electrons[number];
            return null;
        }

        public IBond GetBond(IAtom beg, IAtom end)
        {
            var begref = GetAtomRefUnsafe(beg);
            return begref?.GetBond(end);
        }

        public int GetAtomCount()
        {
            return atoms.Count;
        }

        /// <inheritdoc/>
        public IEnumerable<IElectronContainer> GetElectronContainers()
        {
            return bonds.Cast<IElectronContainer>().Concat(LonePairs).Concat(SingleElectrons);
        }

        public IEnumerable<IAtom> GetConnectedAtoms(IAtom atom)
        {
            var aref = GetAtomRef(atom);
            var nbrs = new List<IAtom>(aref.Bonds.Count);
            foreach (var bond in aref.Bonds)
            {
                nbrs.Add(bond.GetOther(atom));
            }
            return nbrs;
        }

        public IEnumerable<IBond> GetConnectedBonds(IAtom atom)
        {
            var atomref = GetAtomRef(atom);
            return new List<IBond>(atomref.bonds);
        }

        public IEnumerable<ILonePair> GetConnectedLonePairs(IAtom atom)
        {
            GetAtomRef(atom);
            var lps = new List<ILonePair>();
            for (int i = 0; i < lonepairs.Count; i++)
            {
                if (lonepairs[i].Contains(atom)) 
                lps.Add(lonepairs[i]);
            }
            return lps;
        }

        public IEnumerable<ISingleElectron> GetConnectedSingleElectrons(IAtom atom)
        {
            GetAtomRef(atom);
            var ses = new List<ISingleElectron>();
            for (int i = 0; i < electrons.Count; i++)
            {
                if (electrons[i].Contains(atom))
                    ses.Add(electrons[i]);
            }
            return ses;
        }

        public IEnumerable<IElectronContainer> GetConnectedElectronContainers(IAtom atom)
        {
            var aref = GetAtomRef(atom);
            var ecs = new List<IElectronContainer>();
            
            foreach (var bond in aref.Bonds)
            {
                ecs.Add(bond);
            }
            for (int i = 0; i < lonepairs.Count; i++)
            {
                if (lonepairs[i].Contains(atom))
                    ecs.Add(lonepairs[i]);
            }
            for (int i = 0; i < electrons.Count; i++)
            {
                if (electrons[i].Contains(atom))
                    ecs.Add(electrons[i]);
            }
            return ecs;
        }

        public double GetBondOrderSum(IAtom atom)
        {
            double count = 0;
            foreach (var bond in GetAtomRef(atom).Bonds)
            {
                var order = bond.Order;
                if (!order.IsUnset())
                {
                    count += order.Numeric();
                }
            }
            return count;
        }

        public BondOrder GetMaximumBondOrder(IAtom atom)
        {
            var max = BondOrder.Unset;
            foreach (var bond in GetAtomRef(atom).Bonds)
                if (max == BondOrder.Unset || bond.Order.Numeric() > max.Numeric())
                    max = bond.Order;
            if (max == BondOrder.Unset)
                if (atom.ImplicitHydrogenCount != null 
                 && atom.ImplicitHydrogenCount > 0)
                    max = BondOrder.Single;
                else
                    max = BondOrder.Unset;
            return max;
        }

        public BondOrder GetMinimumBondOrder(IAtom atom)
        {
            var min = BondOrder.Unset;
            foreach (var bond in GetAtomRef(atom).Bonds)
                if (min == BondOrder.Unset || bond.Order.Numeric() < min.Numeric())
                    min = bond.Order;
            if (min == BondOrder.Unset)
                if (atom.ImplicitHydrogenCount != null 
                 && atom.ImplicitHydrogenCount > 0)
                    min = BondOrder.Single;
                else
                    min = BondOrder.Unset;
            return min;
        }

        public void Add(IAtomContainer that)
        {
            // mark visited
            foreach (IAtom atom in that.Atoms)
                atom.IsVisited = false;
            foreach (IBond bond in that.Bonds)
                bond.IsVisited = false;
            foreach (IAtom atom in this.Atoms)
                atom.IsVisited = true;
            foreach (IBond bond in this.Bonds)
                bond.IsVisited = true;

            // do stereo elements first
            foreach (var se in that.StereoElements)
            {
                if (se is ITetrahedralChirality 
                 && !((ITetrahedralChirality)se).ChiralAtom.IsVisited)
                    this.StereoElements.Add(se);
                else if (se is IDoubleBondStereochemistry 
                      && !((IDoubleBondStereochemistry)se).StereoBond.IsVisited)
                    this.StereoElements.Add(se);
                else if (se is ExtendedTetrahedral 
                      && !((ExtendedTetrahedral)se).Focus.IsVisited)
                    this.StereoElements.Add(se);
            }

            foreach (var atom in that.Atoms.Where(atom => !atom.IsVisited))
                Atoms.Add(atom);
            foreach (var bond in that.Bonds.Where(bond => !bond.IsVisited))
                Bonds.Add(bond);
            foreach (var lonePair in that.LonePairs.Where(lonePair => !lonePair.IsVisited))
                LonePairs.Add(lonePair);
            foreach (var singleElectron in that.SingleElectrons.Where(singleElectron => !Contains(singleElectron)))
                SingleElectrons.Add(singleElectron);

            NotifyChanged();
        }

        /// <inheritdoc/>
        public void Add(IAtom atom)
        {
            Atoms.Add(atom);
        }

        /// <inheritdoc/>
        public void Add(IElectronContainer electronContainer)
        {
            switch (electronContainer)
            {
                case IBond bond:
                    Bonds.Add(bond);
                    return;
                case ILonePair lonePair:
                    LonePairs.Add(lonePair);
                    return;
                case ISingleElectron singleElectron:
                    SingleElectrons.Add(singleElectron);
                    return;
            }
        }

        /// <inheritdoc/>
        public void Remove(IAtomContainer atomContainer)
        {
            foreach (var bond in atomContainer.Bonds)
                Bonds.Remove(bond);
            foreach (var lonePair in atomContainer.LonePairs)
                LonePairs.Remove(lonePair);
            foreach (var singleElectron in atomContainer.SingleElectrons)
                SingleElectrons.Remove(singleElectron);
            foreach (var atom in atomContainer.Atoms)
                Atoms.Remove(atom);

            NotifyChanged();
        }

        public IBond RemoveBond(IAtom beg, IAtom end)
        {
            var bond = GetBond(beg, end);
            if (bond != null)
            {
                RemoveBond(bond);
            }
            return bond;
        }

        public void RemoveBond(IBond bond)
        {
            if (bond != null)
            {
                var stereoToRemove = new List<IStereoElement<IChemObject, IChemObject>>();
                foreach (var stereoBond in stereo)
                {
                    if (stereoBond.Focus == bond
                     || stereoBond.Carriers.Contains(bond))
                    {
                        stereoToRemove.Add(stereoBond);
                        continue;
                    }
                }
                foreach (var remove in stereoToRemove)
                    stereo.Remove(remove);

                Bonds.Remove(bond);
            }
        }

        /// <inheritdoc/>
        public void Remove(IElectronContainer electronContainer)
        {
            switch (electronContainer)
            {
                case IBond bond:
                    Bonds.Remove(bond);
                    return;
                case ILonePair lonePair:
                    LonePairs.Remove(lonePair);
                    return;
                case ISingleElectron singleElectron:
                    SingleElectrons.Remove(singleElectron);
                    return;
            }
        }

        /// <inheritdoc/>
        [Obsolete]
        public void RemoveAtomAndConnectedElectronContainers(IAtom atom)
        {
            RemoveAtom(atom);
        }

        /// <inheritdoc/>
        public void RemoveAtom(IAtom atom)
        {
            var atomref = GetAtomRefUnsafe(atom);
            if (atomref != null)
            {
                if (atomref.Bonds.Count > 0)
                {
                    // update bonds
                    int newNumBonds = 0;
                    for (int i = 0; i < bonds.Count; i++)
                    {
                        if (!bonds[i].Contains(atom))
                        {
                            bonds[newNumBonds] = bonds[i];
                            bonds[newNumBonds].SetIndex(newNumBonds);
                            newNumBonds++;
                        }
                        else
                        {
                            bonds[i].Listeners.Remove(this);
                            DelFromEndpoints(bonds[i]);
                        }
                    }
                    for (int j = bonds.Count - 1; j >= newNumBonds; j--)
                        bonds.RemoveAt(j);
                }

                // update single electrons
                int newNumSingleElectrons = 0;
                for (int i = 0; i < electrons.Count; i++)
                {
                    if (!electrons[i].Contains(atom))
                    {
                        electrons[newNumSingleElectrons] = electrons[i];
                        newNumSingleElectrons++;
                    }
                    else
                    {
                        electrons[i].Listeners.Remove(this);
                    }
                }
                {
                    for (int j = electrons.Count - 1; j >= newNumSingleElectrons; j--)
                        electrons.RemoveAt(j);
                }

                // update lone pairs
                int newNumLonePairs = 0;
                for (int i = 0; i < lonepairs.Count; i++)
                {
                    if (!lonepairs[i].Contains(atom))
                    {
                        lonepairs[newNumLonePairs] = lonepairs[i];
                        newNumLonePairs++;
                    }
                    else
                    {
                        lonepairs[i].Listeners.Remove(this);
                    }
                }
                {
                    for (int j = lonepairs.Count - 1; j >= newNumLonePairs; j--)
                        lonepairs.RemoveAt(j);
                }

                var atomElements = new List<IStereoElement<IChemObject, IChemObject>>();
                foreach (var element in stereo)
                {
                    if (element.Contains(atom)) 
                        atomElements.Add(element);
                }
                foreach (var ae in atomElements)
                    stereo.Remove(ae);

                Atoms.RemoveAt(atomref.Index);
            }
        }

        /// <inheritdoc/>
        public void RemoveAtom(int pos)
        {
            RemoveAtom(Atoms[pos]);
        }

        /// <inheritdoc/>
        public void RemoveAllElements()
        {
            RemoveAllElectronContainers();
            foreach (var atom in atoms)
                atom.Listeners?.Remove(this);
            atoms.Clear();
            stereo.Clear();
            NotifyChanged();
        }

        public void RemoveAllElectronContainers()
        {
            Bonds.Clear();
            foreach (var e in lonepairs)
                e.Listeners?.Remove(this);
            foreach (var e in electrons)
                e.Listeners?.Remove(this);
            lonepairs.Clear();
            electrons.Clear();

            NotifyChanged();
        }

        public void RemoveAllBonds()
        {
            bonds.Clear();
            ClearAdjacency();
            NotifyChanged();
        }

        /// <inheritdoc/>
        public bool Contains(IAtom atom) => Atoms.Any(n => n.Equals(atom));

        /// <inheritdoc/>
        public bool Contains(IBond bond) => Bonds.Any(n => n.Equals(bond));

        /// <inheritdoc/>
        public bool Contains(ILonePair lonePair) => LonePairs.Any(n => n == lonePair);

        /// <inheritdoc/>
        public bool Contains(ISingleElectron singleElectron) => SingleElectrons.Any(n => n == singleElectron);

        /// <inheritdoc/>
        public bool Contains(IElectronContainer electronContainer)
        {
            if (electronContainer is IBond bond)
                return Contains(bond);
            if (electronContainer is ILonePair lonePair)
                return Contains(lonePair);
            if (electronContainer is ISingleElectron singleElectron)
                return Contains(singleElectron);
            return false;
        }

        /// <inheritdoc/>
        public string Title
        {
            get { return GetProperty<string>(CDKPropertyName.Title); }
            set { SetProperty(CDKPropertyName.Title, value); }
        }


        /// <inheritdoc/>
        public override string ToString()
        {
            return CDKStuff.ToString(this);   
        }

        /// <inheritdoc/>
        public override ICDKObject Clone(CDKObjectMap map)
        {
            CDKObjectMap refmap = new CDKObjectMap();

            // this is pretty wasteful as we need to delete most the data
            // we can't simply create an empty instance as the sub classes (e.g. AminoAcid)
            // would have a ClassCastException when they invoke clone
            AtomContainer2 clone = (AtomContainer2)base.Clone(map);

            // remove existing elements - we need to set the stereo elements list as list.clone() doesn't
            // work as expected and will also remove all elements from the original
            clone.atoms = new List<BaseAtomRef>();
            clone.bonds = new List<BaseBondRef>();
            clone.lonepairs = new ObservableChemObjectCollection<ILonePair>(clone);
            clone.electrons = new ObservableChemObjectCollection<ISingleElectron>(clone);
            clone.stereo = new List<IStereoElement<IChemObject, IChemObject>>();
            clone.Atoms = new BaseAtomRef_Collection(clone);
            clone.Bonds = new BaseBondRef_Collection(clone);
            clone.LonePairs = clone.lonepairs;
            clone.SingleElectrons = clone.electrons;
            clone.StereoElements = clone.stereo;
            clone.RemoveAllElectronContainers();

            // clone atoms
            IAtom[] c_atoms = new IAtom[this.atoms.Count];
            for (int i = 0; i < this.atoms.Count; i++)
            {
                c_atoms[i] = (IAtom)this.atoms[i].Deref().Clone(map);
            }
            clone.SetAtoms(c_atoms);
            for (int i = 0; i < c_atoms.Length; i++)
                refmap.Set(this.atoms[i], clone.Atoms[i]);

            // clone bonds using a the mappings from the original to the clone
            var bonds = new IBond[this.bonds.Count];
            for (int i = 0; i < this.bonds.Count; i++)
            {
                var original = this.bonds[i];
                var bond = (IBond)original.Deref().Clone(map);
                var n = bond.Atoms.Count;
                var members = new IAtom[n];
                for (int j = 0; j < n; j++)
                {
                    members[j] = map.Get(original.Atoms[j]);
                }

                bond.SetAtoms(members);
                bonds[i] = bond;
            }
            clone.SetBonds(bonds);
            for (int i = 0; i < bonds.Length; i++)
                refmap.Set(this.bonds[i], clone.Bonds[i]);

            // clone lone pairs (we can't use an array to buffer as there is no setLonePairs())
            for (int i = 0; i < this.lonepairs.Count; i++)
            {
                var original = this.lonepairs[i];
                var pair = (ILonePair)original.Clone();

                if (pair.Atom != null)
                    pair.Atom = refmap.Get(original.Atom);

                clone.LonePairs.Add(pair);
            }

            // clone single electrons (we can't use an array to buffer as there is no setSingleElectrons())
            for (int i = 0; i < electrons.Count; i++)
            {
                var original = this.electrons[i];
                var electron = (ISingleElectron)original.Clone();

                if (electron.Atom != null)
                    electron.Atom = refmap.Get(original.Atom);

                clone.SingleElectrons.Add(electron);
            }

            // map each stereo element to a new instance in the clone
            foreach (var element in stereo)
            {
                clone.StereoElements.Add((IStereoElement<IChemObject, IChemObject>)element.Clone(refmap));
            }

            // update sgroups
            var sgroups = this.GetCtabSgroups();
            if (sgroups != null)
            {
                clone.SetCtabSgroups(SgroupManipulator.Copy(sgroups, refmap));
            }

            return clone;
        }

        /// <inheritdoc/>
        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
            NotifyChanged(evt);
        }

        /// <inheritdoc/>
        public bool IsEmpty() => atoms.Count == 0;
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// This class should not be used directly.
    /// </summary>
    // @author John Mayfield
    internal sealed class AtomContainer2 
        : ChemObject, IAtomContainer
    {
        private static readonly IChemObjectBuilder builder = new ChemObjectBuilder(false);

        /// <inheritdoc/>
        public override IChemObjectBuilder Builder => builder;

        internal List<BaseAtomRef> atoms;
        internal List<BaseBondRef> bonds;
        internal List<ILonePair> lonepairs;
        internal List<ISingleElectron> electrons;
        internal List<IStereoElement<IChemObject, IChemObject>> stereo;

        /// <inheritdoc/>
        // TODO: Implements ElectronContainers
        public IList<IAtom> Atoms { get; private set; }
        public IList<IBond> Bonds { get; private set; }
        public IList<ILonePair> LonePairs { get; private set; }
        public IList<ISingleElectron> SingleElectrons { get; private set; }
        // TODO: handle notification
        public ICollection<IStereoElement<IChemObject, IChemObject>> StereoElements { get; private set; }

        /// <summary>
        /// Create a new container with the specified capacities.
        /// </summary>
        /// <param name="numAtoms">expected number of atoms</param>
        /// <param name="numBonds">expected number of bonds</param>
        /// <param name="numLonePairs">expected number of lone pairs</param>
        /// <param name="numSingleElectrons">expected number of single electrons</param>
        internal AtomContainer2(
            int numAtoms,
            int numBonds,
            int numLonePairs,
            int numSingleElectrons)
        {
            this.atoms = new List<BaseAtomRef>(numAtoms);
            this.bonds = new List<BaseBondRef>(numBonds);
            this.lonepairs = new List<ILonePair>();
            this.electrons = new List<ISingleElectron>();
            this.stereo = new List<IStereoElement<IChemObject, IChemObject>>();

            this.Atoms = new BaseAtomRef_Collection(this);
            this.Bonds = new BaseBondRef_Collection(this);
            this.LonePairs = lonepairs;
            this.SingleElectrons = electrons;
            this.StereoElements = stereo;
        }

        /// <summary>
        /// Constructs an empty AtomContainer.
        /// </summary>
        public AtomContainer2()
            : this(0, 0, 0, 0)
        {
        }

        /// <summary>
        /// Constructs a shallow copy of the provided IAtomContainer with the same
        /// atoms, bonds, electron containers and stereochemistry of another
        /// AtomContainer. Removing atoms/bonds in this copy will not affect
        /// the original, however changing the properties will.
        /// </summary>
        /// <param name="src">the source atom container</param>
        public AtomContainer2(IAtomContainer src)
            : this(
                src.Atoms.Count,
                src.Bonds.Count,
                src.LonePairs.Count,
                src.SingleElectrons.Count)
        {
            foreach (var atom in src.Atoms)
                Atoms.Add(atom);
            foreach (var bond in src.Bonds)
                Bonds.Add(bond);
            foreach (var se in src.SingleElectrons)
                SingleElectrons.Add(se);
            foreach (var lp in src.LonePairs)
                LonePairs.Add(lp);
            foreach (var se in src.StereoElements)
                StereoElements.Add(se);
        }

        public AtomContainer2(IEnumerable<IAtom> atoms, IEnumerable<IBond> bonds)
            : this()
        {
            foreach (var atom in atoms)
                Atoms.Add(atom);
            foreach (var bond in bonds)
                Bonds.Add(bond);
        }

        /// <inheritdoc/>
        public  bool IsAromatic
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
        public  bool IsSingleOrDouble
        {
            get 
            { 
                return (flags & CDKConstants.IsSingleOrDoubleMask) != 0;
            }
            
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            set
            {
                SetIsSingleOrDoubleWithoutNotify(value);
            }
        }

        /*private protected*/
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal void SetIsSingleOrDoubleWithoutNotify(bool value)
        {
            if (value)
                flags |= CDKConstants.IsSingleOrDoubleMask;
            else
                flags &= ~CDKConstants.IsSingleOrDoubleMask; 
        }

        internal class BaseAtomRef : AtomRef
        {
            private int index = -1;
            internal readonly IAtomContainer mol;
            internal readonly List<IBond> bonds = new List<IBond>(4);

            public BaseAtomRef(IAtomContainer mol, IAtom atom)
                : base(atom)
            {
                this.mol = mol;
            }

            public override int Index => index;
            public void SetIndex(int index) => this.index = index;
            public override IAtomContainer Container => mol;

            public override IReadOnlyList<IBond> Bonds => bonds;

            public override IBond GetBond(IAtom atom)
            {
                foreach (var bond in bonds)
                {
                    if (bond.GetOther(this).Equals(atom))
                        return bond;
                }
                return null;
            }

            public override int GetHashCode()
            {
                return Deref().GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is AtomRef)
                    return Deref().Equals(((AtomRef)obj).Deref());
                return Deref().Equals(obj);
            }

            public override string ToString()
            {
                return Deref().ToString();
            }
        }

        internal class BaseBondRef : BondRef
        {
            private int index;
            private readonly AtomContainer2 mol;
            private BaseAtomRef beg, end;

            public BaseBondRef(AtomContainer2 mol, IBond bond, BaseAtomRef beg, BaseAtomRef end)
                : base(bond)
            {
                this.mol = mol;
                this.beg = beg;
                this.end = end;
            }

            public override int Index => index;

            public void SetIndex(int index)
            {
                this.index = index;
            }

            public override IAtomContainer Container => mol;

            public override IAtom Begin => beg;
            public override IAtom End => end;

            internal BaseAtomRef GetBegin() => beg;
            internal BaseAtomRef GetEnd() => end;

            private IList<IAtom> internalAtoms;

            /// <inheritdoc/>
            public override IList<IAtom> Atoms
            {
                get
                {
                    if (internalAtoms == null)
                        internalAtoms = new InternalAtoms(this, base.Atoms);
                    return internalAtoms;
                }
            }

            internal class InternalAtoms : IList<IAtom>
            {
                BaseBondRef parent;
                IList<IAtom> baseList;

                public InternalAtoms(BaseBondRef parent, IList<IAtom> baseList)
                {
                    this.parent = parent;
                    this.baseList = baseList;
                }

                public IAtom this[int index]
                {
                    get
                    {
                        switch (index)
                        {
                            case 0:
                                return parent.Begin;
                            case 1:
                                return parent.End;
                            default:
                                return parent.mol.GetAtomRef(baseList[index]);
                        }
                    }

                    set
                    {
                        baseList[index] = value;
                        if (index == 0)
                        {
                            if (parent.beg != null)
                                parent.beg.bonds.Remove(parent);
                            parent.beg = parent.mol.GetAtomRef(value);
                            parent.beg.bonds.Add(parent);
                        }
                        else if (index == 1)
                        {
                            if (parent.end != null)
                                parent.end.bonds.Remove(parent);
                            parent.end = parent.mol.GetAtomRef(value);
                            parent.end.bonds.Add(parent);
                        }
                    }
                }

                public int Count => baseList.Count;
                public bool IsReadOnly => baseList.IsReadOnly;
                public void Add(IAtom item) => baseList.Add(item);
                public void Clear() => baseList.Clear();
                public bool Contains(IAtom item) => baseList.Contains(item);
                public void CopyTo(IAtom[] array, int arrayIndex) => baseList.CopyTo(array, arrayIndex);
                public IEnumerator<IAtom> GetEnumerator() => baseList.GetEnumerator();
                public int IndexOf(IAtom item) => baseList.IndexOf(item);
                public void Insert(int index, IAtom item) => baseList.Insert(index, item);
                public bool Remove(IAtom item) => baseList.Remove(item);
                public void RemoveAt(int index) => baseList.RemoveAt(index);
                IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            }

            public AtomRef GetOtherRef(IAtom atom)
            {
                if (atom == beg)
                    return end;
                else if (atom == end)
                    return beg;
                atom = AtomContainer2.Unbox(atom);
                if (atom == beg.Deref())
                    return end;
                else if (atom == end.Deref())
                    return beg;
                return null;
            }

            public override IAtom GetOther(IAtom atom) => GetOtherRef(atom);

            public override void SetAtoms(IEnumerable<IAtom> atoms_)
            {
                var atoms = atoms_.ToReadOnlyList();
                Trace.Assert(atoms.Count == 2);
                base.SetAtoms(atoms);
                // check for swap: intended ref check
                if (object.Equals(atoms[0], end) && object.Equals(atoms[1], beg))
                {
                    var tmp = beg;
                    beg = end;
                    end = tmp;
                    return;
                }
                if (beg != null)
                    beg.bonds.Remove(this);
                if (end != null)
                    end.bonds.Remove(this);
                beg = mol.GetAtomRef(atoms[0]);
                end = mol.GetAtomRef(atoms[1]);
                beg.bonds.Add(this);
                end.bonds.Add(this);
            }

            public override int GetHashCode()
            {
                return Deref().GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is BondRef)
                    return Deref().Equals(((BondRef)obj).Deref());
                return Deref().Equals(obj);
            }
        }

        internal sealed class PsuedoAtomRef : BaseAtomRef, IPseudoAtom
        {
            private readonly IPseudoAtom pseudo;

            public PsuedoAtomRef(IAtomContainer mol, IPseudoAtom atom)
                : base(mol, atom)
            {
                this.pseudo = atom;
            }

            public string Label
            {
                get => pseudo.Label;
                set => pseudo.Label = value;
            }

            public int AttachPointNum
            {
                get => pseudo.AttachPointNum;
                set => pseudo.AttachPointNum = value;
            }

            public override int GetHashCode()
            {
                return Deref().GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is BondRef)
                    return Deref().Equals(((BondRef)obj).Deref());
                return Deref().Equals(obj);
            }
        }

        internal sealed class QueryAtomRef : BaseAtomRef, IQueryAtom
        {
            private readonly IQueryAtom qatom;

            public QueryAtomRef(IAtomContainer mol, IQueryAtom atom)
                : base(mol, atom)
            {
                this.qatom = atom;
            }

            public bool Matches(IAtom atom)
            {
                return qatom.Matches(atom);
            }
        }

        internal sealed class PdbAtomRef : BaseAtomRef, IPDBAtom
        {
            private readonly IPDBAtom pdbAtom;

            public PdbAtomRef(IAtomContainer mol, IPDBAtom atom)
                : base(mol, atom)
            {
                this.pdbAtom = atom;
            }

            public string Record { get => pdbAtom.Record; set => pdbAtom.Record = value; }
            public double? TempFactor { get => pdbAtom.TempFactor; set => pdbAtom.TempFactor = value; }
            public string ResName { get => pdbAtom.ResName; set => pdbAtom.ResName = value; }
            public string ICode { get => pdbAtom.ICode; set => pdbAtom.ICode = value; }
            public string Name { get => pdbAtom.Name; set => pdbAtom.Name = value; }
            public string ChainID { get => pdbAtom.ChainID; set => pdbAtom.ChainID = value; }
            public string AltLoc { get => pdbAtom.AltLoc; set => pdbAtom.AltLoc = value; }
            public string SegID { get => pdbAtom.SegID; set => pdbAtom.SegID = value; }
            public int? Serial { get => pdbAtom.Serial; set => pdbAtom.Serial = value; }
            public string ResSeq { get => pdbAtom.ResSeq; set => pdbAtom.ResSeq = value; }
            public bool Oxt { get => pdbAtom.Oxt; set => pdbAtom.Oxt = value; }
            public bool? HetAtom { get => pdbAtom.HetAtom; set => pdbAtom.HetAtom = value; }
            public double? Occupancy { get => pdbAtom.Occupancy; set => pdbAtom.Occupancy = value; }
        }

        internal sealed class QueryBondRef 
            : BaseBondRef, IQueryBond
        {
            public QueryBondRef(AtomContainer2 mol,
                                IQueryBond bond,
                                BaseAtomRef beg,
                                BaseAtomRef end)
                : base(mol, bond, beg, end)
            {
            }

            public bool Matches(IBond bond)
            {
                return ((IQueryBond)Deref()).Matches(bond);
            }
        }

        internal abstract class A_Collection<TInterface, TRef> : IList<TInterface> where TRef : TInterface
        {
            List<TRef> list;

            public A_Collection(List<TRef> list)
            {
                this.list = list;
            }

            public abstract TInterface this[int index] { get; set; }
            public int Count => list.Count;
            public bool IsReadOnly => false;
            public abstract void Add(TInterface item);
            public abstract void Clear();
            public bool Contains(TInterface item) => list.Contains((TRef)item);
            public void CopyTo(TInterface[] array, int arrayIndex)
            {
                for (int i = 0; i < list.Count; i++)
                    array[arrayIndex + i] = list[i];
            }            
            public IEnumerator<TInterface> GetEnumerator() => list.Cast<TInterface>().GetEnumerator();
            public abstract int IndexOf(TInterface atom);
            public void Insert(int index, TInterface item)
            {
                list.Insert(index, default(TRef));
                this[index] = item;
            }
            public abstract bool Remove(TInterface item);
            public abstract void RemoveAt(int index);
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        internal class BaseAtomRef_Collection : A_Collection<IAtom, BaseAtomRef>
        {
            AtomContainer2 parent;

            public BaseAtomRef_Collection(AtomContainer2 parent)
                : base(parent.atoms)
            {
                this.parent = parent;
            }

            public override IAtom this[int index]
            {
                get
                {
                    if (index >= this.parent.atoms.Count)
                        throw new ArgumentOutOfRangeException(nameof(index), $"No atom at index: {index}");
                    return parent.atoms[index];
                }

                set
                {
                    var atom = value;
                    if (atom == null)
                        throw new NullReferenceException("Null atom provided");
                    if (parent.atoms.Contains(atom))
                        throw new InvalidOperationException($"Atom already in container at index: {this.IndexOf(atom)}");
                    if (index < 0 || index >= parent.atoms.Count)
                        throw new ArgumentOutOfRangeException(nameof(index), $"No current atom at index: {index}");
                    var rep = parent.NewAtomRef(atom);
                    var org = parent.atoms[index];
                    parent.atoms[index] = rep;
                    parent.atoms[index].SetIndex(index);
                    foreach (var bond in org.Bonds.ToList())
                    {
                        if (bond.Begin.Equals(org))
                            bond.Atoms[0] = rep;
                        else if (bond.End.Equals(org))
                            bond.Atoms[1] = rep;
                    }

                    // update single electrons and lone pairs
                    foreach (var ec in parent.electrons)
                    {
                        if (org.Equals(ec.Atom))
                            ec.Atom = rep;
                    }
                    foreach (var lp in parent.lonepairs)
                    {
                        if (org.Equals(lp.Atom))
                            lp.Atom = rep;
                    }

                    // update stereo
                    for (int i = 0; i < parent.stereo.Count; i++)
                    {
                        var se = parent.stereo[i];
                        if (se.Contains(org))
                        {
                            var map = new CDKObjectMap();
                            map.Add(org, rep);
                            parent.stereo[i] = (IStereoElement<IChemObject, IChemObject>)se.Clone(map);
                        }
                    }
                }
            }

            public override int IndexOf(IAtom atom)
            {
                var aref = parent.GetAtomRefUnsafe(atom);
                return aref == null ? -1 : aref.Index;
            }

            public override void Add(IAtom atom)
            {
                if (parent.Contains(atom))
                    return;
                var aref = parent.NewAtomRef(atom);
                aref.SetIndex(parent.atoms.Count);
                parent.atoms.Add(aref);
            }

            public override void RemoveAt(int index)
            {
                parent.atoms[index].SetIndex(-1);
                for (int i = index; i < parent.atoms.Count - 1; i++)
                {
                    parent.atoms[i] = parent.atoms[i + 1];
                    parent.atoms[i].SetIndex(i);
                }
                parent.atoms.RemoveAt(parent.atoms.Count - 1);
            }

            public override bool Remove(IAtom item)
            {
                var index = parent.Atoms.IndexOf(item);
                if (index != -1)
                {
                    RemoveAt(index);
                    return true;
                }
                return false;
            }

            public override void Clear()
            {
                parent.atoms.Clear();
            }
        }

        internal class BaseBondRef_Collection : A_Collection<IBond, BaseBondRef>
        {
            AtomContainer2 parent;

            public BaseBondRef_Collection(AtomContainer2 parent)
                : base(parent.bonds)
            {
                this.parent = parent;
            }

            public override IBond this[int index]
            {
                get
                {
                    if (index >= parent.bonds.Count)
                        throw new ArgumentOutOfRangeException(nameof(index), $"No bond at index: {index}");
                    return parent.bonds[index];
                }

                set
                {
                    throw new InvalidOperationException();
                }
            }

            public override int IndexOf(IBond bond)
            {
                var bref = parent.GetBondRefUnsafe(bond);
                return bref == null ? -1 : bref.Index;
            }

            public override void Add(IBond bond)
            {
                var bref = parent.NewBondRef(bond);
                bref.SetIndex(parent.bonds.Count);
                parent.AddToEndpoints(bref);
                parent.bonds.Add(bref);
                bref.Listeners.Add(parent);
            }

            public override void RemoveAt(int index)
            {
                var bond = parent.bonds[index];
                parent.bonds[index].SetIndex(-1);
                for (int i = index; i < parent.bonds.Count - 1; i++)
                {
                    parent.bonds[i] = parent.bonds[i + 1];
                    parent.bonds[i].SetIndex(i);
                }
                parent.bonds.RemoveAt(parent.bonds.Count - 1);
                parent.DelFromEndpoints(bond);
            }

            public override bool Remove(IBond item)
            {
                var index = parent.Bonds.IndexOf(item);
                if (index != -1)
                {
                    RemoveAt(index);
                    return true;
                }
                return false;
            }

            public override void Clear()
            {
                parent.bonds.Clear();
                parent.ClearAdjacency();
            }
        }

        internal static IAtom Unbox(IAtom atom)
        {
            while (atom is AtomRef)
                atom = ((AtomRef)atom).Deref();
            return atom;
        }

        internal static IBond Unbox(IBond bond)
        {
            while (bond is BondRef)
                bond = ((BondRef)bond).Deref();
            return bond;
        }

        internal BaseAtomRef GetAtomRefUnsafe(IAtom atom)
        {
            if (atom.Container == this &&
                atom.Index != -1 &&
                atoms[atom.Index] == atom)
                return (BaseAtomRef)atom;
            atom = Unbox(atom);
            for (int i = 0; i < atoms.Count; i++)
                if (object.Equals(atoms[i], atom))
                    return atoms[i];
            return null;
        }

        internal BaseAtomRef GetAtomRef(IAtom atom)
        {
            var atomref = GetAtomRefUnsafe(atom);
            if (atomref == null)
                throw new NoSuchAtomException("Atom is not a member of this AtomContainer");
            return atomref;
        }

        internal BaseAtomRef NewAtomRef(IAtom atom)
        {
            // most common implementation we'll encounter..
            if (atom.GetType() == typeof(Atom))
                return new BaseAtomRef(this, atom);
            atom = Unbox(atom);
            // re-check the common case now we've un-boxed
            if (atom.GetType() == typeof(Atom))
                return new BaseAtomRef(this, atom);
            switch (atom)
            {
                case IPseudoAtom _atom:
                    return new PsuedoAtomRef(this, _atom);
                case IQueryAtom _atom:
                    return new QueryAtomRef(this, _atom);
                case IPDBAtom _atom:
                    return new PdbAtomRef(this, _atom);
            }
            return new BaseAtomRef(this, atom);
        }

        private BondRef GetBondRefUnsafe(IBond bond)
        {
            if (bond.Container == this 
             && bond.Index != -1 
             && bonds[bond.Index] == bond)
                return (BondRef)bond;
            bond = Unbox(bond);
            for (int i = 0; i < bonds.Count; i++)
                if (bonds[i].Deref() == bond)
                    return bonds[i];
            return null;
        }

        private BaseBondRef NewBondRef(IBond bond)
        {
            var beg = bond.Begin == null ? null : GetAtomRef(bond.Begin);
            var end = bond.End == null ? null : GetAtomRef(bond.End);
            if (bond.GetType() == typeof(Bond))
                return new BaseBondRef(this, bond, beg, end);
            bond = Unbox(bond);
            switch (bond)
            {
                case IQueryBond _bond:
                    return new QueryBondRef(this, _bond, beg, end);
            }
            return new BaseBondRef(this, bond, beg, end);
        }

        private void AddToEndpoints(BaseBondRef bondref)
        {
            if (bondref.Atoms.Count == 2)
            {
                var beg = GetAtomRef(bondref.Begin);
                var end = GetAtomRef(bondref.End);
                beg.bonds.Add(bondref);
                end.bonds.Add(bondref);
            }
            else
            {
                for (int i = 0; i < bondref.Atoms.Count; i++)
                {
                    GetAtomRef(bondref.Atoms[i]).bonds.Add(bondref);
                }
            }
        }

        private void DelFromEndpoints(BondRef bondref)
        {
            for (int i = 0; i < bondref.Atoms.Count; i++)
            {
                var aref = GetAtomRefUnsafe(bondref.Atoms[i]);
                // atom may have already been deleted, naughty!
                if (aref != null)
                    aref.bonds.Remove(bondref);
            }
        }

        private void ClearAdjacency()
        {
            for (int i = 0; i < atoms.Count; i++)
            {
                atoms[i].bonds.Clear();
            }
        }

        public void SetAtoms(IEnumerable<IAtom> newatoms)
        {
            var _newatoms = newatoms.ToList();
            bool reindexBonds = false;

            for (int i = 0; i < _newatoms.Count; i++)
            {
                if (i >= atoms.Count)
                    atoms.Add(null);

                if (_newatoms[i].Container == this)
                {
                    atoms[i] = (BaseAtomRef)_newatoms[i];
                    atoms[i].SetIndex(i);
                }
                else
                {
                    atoms[i] = NewAtomRef(_newatoms[i]);
                    atoms[i].SetIndex(i);
                    reindexBonds = true;
                }
            }

            // delete rest of the array
            {
                for (int i = atoms.Count - 1; i >= _newatoms.Count; i--)
                {
                    atoms.RemoveAt(i);
                }
            }

            // ensure adjacency information is in sync, this code is only
            // called if the bonds are non-empty and 'external' atoms have been
            // added
            if (bonds.Count > 0 && reindexBonds)
            {
                ClearAdjacency();
                for (int i = 0; i < bonds.Count; i++)
                    AddToEndpoints(bonds[i]);
            }

        }

        public void SetBonds(IEnumerable<IBond> newbonds)
        {
            var _newbonds = newbonds.ToList();

            // replace existing bonds to clear their adjacency
            if (bonds.Count > 0) 
            {
                ClearAdjacency();
            }

            for (int i = 0; i < _newbonds.Count; i++) 
            {
                if (i >= bonds.Count)
                    bonds.Add(null);

                var bondRef = NewBondRef(_newbonds[i]);
                bondRef.SetIndex(i);
                AddToEndpoints(bondRef);
                bonds[i] = bondRef;
            }
            for (int i = bonds.Count - 1; i >= _newbonds.Count; i--)
                bonds.RemoveAt(i);

        }

        public IElectronContainer GetElectronContainer(int number)
        {
            if (number < bonds.Count) 
                return bonds[number];
            number -= bonds.Count;
            if (number < lonepairs.Count) 
                return lonepairs[number];
            number -= lonepairs.Count;
            if (number < electrons.Count) 
                return electrons[number];
            return null;
        }

        public IBond GetBond(IAtom beg, IAtom end)
        {
            var begref = GetAtomRefUnsafe(beg);
            return begref?.GetBond(end);
        }

        public int GetAtomCount()
        {
            return atoms.Count;
        }

        /// <inheritdoc/>
        public IEnumerable<IElectronContainer> GetElectronContainers()
        {
            return bonds.Cast<IElectronContainer>().Concat(LonePairs).Concat(SingleElectrons);
        }

        public IEnumerable<IAtom> GetConnectedAtoms(IAtom atom)
        {
            var aref = GetAtomRef(atom);
            var nbrs = new List<IAtom>(aref.Bonds.Count);
            foreach (var bond in aref.Bonds)
            {
                nbrs.Add(bond.GetOther(atom));
            }
            return nbrs;
        }

        public IEnumerable<IBond> GetConnectedBonds(IAtom atom)
        {
            var atomref = GetAtomRef(atom);
            return new List<IBond>(atomref.bonds);
        }

        public IEnumerable<ILonePair> GetConnectedLonePairs(IAtom atom)
        {
            GetAtomRef(atom);
            var lps = new List<ILonePair>();
            for (int i = 0; i < lonepairs.Count; i++)
            {
                if (lonepairs[i].Contains(atom)) 
                lps.Add(lonepairs[i]);
            }
            return lps;
        }

        public IEnumerable<ISingleElectron> GetConnectedSingleElectrons(IAtom atom)
        {
            GetAtomRef(atom);
            var ses = new List<ISingleElectron>();
            for (int i = 0; i < electrons.Count; i++)
            {
                if (electrons[i].Contains(atom))
                    ses.Add(electrons[i]);
            }
            return ses;
        }

        public IEnumerable<IElectronContainer> GetConnectedElectronContainers(IAtom atom)
        {
            var aref = GetAtomRef(atom);
            var ecs = new List<IElectronContainer>();
            
            foreach (var bond in aref.Bonds)
            {
                ecs.Add(bond);
            }
            for (int i = 0; i < lonepairs.Count; i++)
            {
                if (lonepairs[i].Contains(atom))
                    ecs.Add(lonepairs[i]);
            }
            for (int i = 0; i < electrons.Count; i++)
            {
                if (electrons[i].Contains(atom))
                    ecs.Add(electrons[i]);
            }
            return ecs;
        }

        public double GetBondOrderSum(IAtom atom)
        {
            double count = 0;
            foreach (var bond in GetAtomRef(atom).Bonds)
            {
                var order = bond.Order;
                if (!order.IsUnset())
                {
                    count += order.Numeric();
                }
            }
            return count;
        }

        public BondOrder GetMaximumBondOrder(IAtom atom)
        {
            var max = BondOrder.Unset;
            foreach (var bond in GetAtomRef(atom).Bonds)
                if (max == BondOrder.Unset || bond.Order.Numeric() > max.Numeric())
                    max = bond.Order;
            if (max == BondOrder.Unset)
                if (atom.ImplicitHydrogenCount != null 
                 && atom.ImplicitHydrogenCount > 0)
                    max = BondOrder.Single;
                else
                    max = BondOrder.Unset;
            return max;
        }

        public BondOrder GetMinimumBondOrder(IAtom atom)
        {
            var min = BondOrder.Unset;
            foreach (var bond in GetAtomRef(atom).Bonds)
                if (min == BondOrder.Unset || bond.Order.Numeric() < min.Numeric())
                    min = bond.Order;
            if (min == BondOrder.Unset)
                if (atom.ImplicitHydrogenCount != null 
                 && atom.ImplicitHydrogenCount > 0)
                    min = BondOrder.Single;
                else
                    min = BondOrder.Unset;
            return min;
        }

        public void Add(IAtomContainer that)
        {
            // mark visited
            foreach (IAtom atom in that.Atoms)
                atom.IsVisited = false;
            foreach (IBond bond in that.Bonds)
                bond.IsVisited = false;
            foreach (IAtom atom in this.Atoms)
                atom.IsVisited = true;
            foreach (IBond bond in this.Bonds)
                bond.IsVisited = true;

            // do stereo elements first
            foreach (var se in that.StereoElements)
            {
                if (se is ITetrahedralChirality 
                 && !((ITetrahedralChirality)se).ChiralAtom.IsVisited)
                    this.StereoElements.Add(se);
                else if (se is IDoubleBondStereochemistry 
                      && !((IDoubleBondStereochemistry)se).StereoBond.IsVisited)
                    this.StereoElements.Add(se);
                else if (se is ExtendedTetrahedral 
                      && !((ExtendedTetrahedral)se).Focus.IsVisited)
                    this.StereoElements.Add(se);
            }

            foreach (var atom in that.Atoms.Where(atom => !atom.IsVisited))
                Atoms.Add(atom);
            foreach (var bond in that.Bonds.Where(bond => !bond.IsVisited))
                Bonds.Add(bond);
            foreach (var lonePair in that.LonePairs.Where(lonePair => !lonePair.IsVisited))
                LonePairs.Add(lonePair);
            foreach (var singleElectron in that.SingleElectrons.Where(singleElectron => !Contains(singleElectron)))
                SingleElectrons.Add(singleElectron);

        }

        /// <inheritdoc/>
        public void Add(IAtom atom)
        {
            Atoms.Add(atom);
        }

        /// <inheritdoc/>
        public void Add(IElectronContainer electronContainer)
        {
            switch (electronContainer)
            {
                case IBond bond:
                    Bonds.Add(bond);
                    return;
                case ILonePair lonePair:
                    LonePairs.Add(lonePair);
                    return;
                case ISingleElectron singleElectron:
                    SingleElectrons.Add(singleElectron);
                    return;
            }
        }

        /// <inheritdoc/>
        public void Remove(IAtomContainer atomContainer)
        {
            foreach (var bond in atomContainer.Bonds)
                Bonds.Remove(bond);
            foreach (var lonePair in atomContainer.LonePairs)
                LonePairs.Remove(lonePair);
            foreach (var singleElectron in atomContainer.SingleElectrons)
                SingleElectrons.Remove(singleElectron);
            foreach (var atom in atomContainer.Atoms)
                Atoms.Remove(atom);

        }

        public IBond RemoveBond(IAtom beg, IAtom end)
        {
            var bond = GetBond(beg, end);
            if (bond != null)
            {
                RemoveBond(bond);
            }
            return bond;
        }

        public void RemoveBond(IBond bond)
        {
            if (bond != null)
            {
                var stereoToRemove = new List<IStereoElement<IChemObject, IChemObject>>();
                foreach (var stereoBond in stereo)
                {
                    if (stereoBond.Focus == bond
                     || stereoBond.Carriers.Contains(bond))
                    {
                        stereoToRemove.Add(stereoBond);
                        continue;
                    }
                }
                foreach (var remove in stereoToRemove)
                    stereo.Remove(remove);

                Bonds.Remove(bond);
            }
        }

        /// <inheritdoc/>
        public void Remove(IElectronContainer electronContainer)
        {
            switch (electronContainer)
            {
                case IBond bond:
                    Bonds.Remove(bond);
                    return;
                case ILonePair lonePair:
                    LonePairs.Remove(lonePair);
                    return;
                case ISingleElectron singleElectron:
                    SingleElectrons.Remove(singleElectron);
                    return;
            }
        }

        /// <inheritdoc/>
        [Obsolete]
        public void RemoveAtomAndConnectedElectronContainers(IAtom atom)
        {
            RemoveAtom(atom);
        }

        /// <inheritdoc/>
        public void RemoveAtom(IAtom atom)
        {
            var atomref = GetAtomRefUnsafe(atom);
            if (atomref != null)
            {
                if (atomref.Bonds.Count > 0)
                {
                    // update bonds
                    int newNumBonds = 0;
                    for (int i = 0; i < bonds.Count; i++)
                    {
                        if (!bonds[i].Contains(atom))
                        {
                            bonds[newNumBonds] = bonds[i];
                            bonds[newNumBonds].SetIndex(newNumBonds);
                            newNumBonds++;
                        }
                        else
                        {
                            bonds[i].Listeners.Remove(this);
                            DelFromEndpoints(bonds[i]);
                        }
                    }
                    for (int j = bonds.Count - 1; j >= newNumBonds; j--)
                        bonds.RemoveAt(j);
                }

                // update single electrons
                int newNumSingleElectrons = 0;
                for (int i = 0; i < electrons.Count; i++)
                {
                    if (!electrons[i].Contains(atom))
                    {
                        electrons[newNumSingleElectrons] = electrons[i];
                        newNumSingleElectrons++;
                    }
                    else
                    {
                        electrons[i].Listeners.Remove(this);
                    }
                }
                {
                    for (int j = electrons.Count - 1; j >= newNumSingleElectrons; j--)
                        electrons.RemoveAt(j);
                }

                // update lone pairs
                int newNumLonePairs = 0;
                for (int i = 0; i < lonepairs.Count; i++)
                {
                    if (!lonepairs[i].Contains(atom))
                    {
                        lonepairs[newNumLonePairs] = lonepairs[i];
                        newNumLonePairs++;
                    }
                    else
                    {
                        lonepairs[i].Listeners.Remove(this);
                    }
                }
                {
                    for (int j = lonepairs.Count - 1; j >= newNumLonePairs; j--)
                        lonepairs.RemoveAt(j);
                }

                var atomElements = new List<IStereoElement<IChemObject, IChemObject>>();
                foreach (var element in stereo)
                {
                    if (element.Contains(atom)) 
                        atomElements.Add(element);
                }
                foreach (var ae in atomElements)
                    stereo.Remove(ae);

                Atoms.RemoveAt(atomref.Index);
            }
        }

        /// <inheritdoc/>
        public void RemoveAtom(int pos)
        {
            RemoveAtom(Atoms[pos]);
        }

        /// <inheritdoc/>
        public void RemoveAllElements()
        {
            RemoveAllElectronContainers();
            atoms.Clear();
            stereo.Clear();
        }

        public void RemoveAllElectronContainers()
        {
            Bonds.Clear();
            lonepairs.Clear();
            electrons.Clear();

        }

        public void RemoveAllBonds()
        {
            bonds.Clear();
            ClearAdjacency();
        }

        /// <inheritdoc/>
        public bool Contains(IAtom atom) => Atoms.Any(n => n.Equals(atom));

        /// <inheritdoc/>
        public bool Contains(IBond bond) => Bonds.Any(n => n.Equals(bond));

        /// <inheritdoc/>
        public bool Contains(ILonePair lonePair) => LonePairs.Any(n => n == lonePair);

        /// <inheritdoc/>
        public bool Contains(ISingleElectron singleElectron) => SingleElectrons.Any(n => n == singleElectron);

        /// <inheritdoc/>
        public bool Contains(IElectronContainer electronContainer)
        {
            if (electronContainer is IBond bond)
                return Contains(bond);
            if (electronContainer is ILonePair lonePair)
                return Contains(lonePair);
            if (electronContainer is ISingleElectron singleElectron)
                return Contains(singleElectron);
            return false;
        }

        /// <inheritdoc/>
        public string Title
        {
            get { return GetProperty<string>(CDKPropertyName.Title); }
            set { SetProperty(CDKPropertyName.Title, value); }
        }


        /// <inheritdoc/>
        public override string ToString()
        {
            return CDKStuff.ToString(this);   
        }

        /// <inheritdoc/>
        public override ICDKObject Clone(CDKObjectMap map)
        {
            CDKObjectMap refmap = new CDKObjectMap();

            // this is pretty wasteful as we need to delete most the data
            // we can't simply create an empty instance as the sub classes (e.g. AminoAcid)
            // would have a ClassCastException when they invoke clone
            AtomContainer2 clone = (AtomContainer2)base.Clone(map);

            // remove existing elements - we need to set the stereo elements list as list.clone() doesn't
            // work as expected and will also remove all elements from the original
            clone.atoms = new List<BaseAtomRef>();
            clone.bonds = new List<BaseBondRef>();
            clone.lonepairs = new List<ILonePair>();
            clone.electrons = new List<ISingleElectron>();
            clone.stereo = new List<IStereoElement<IChemObject, IChemObject>>();
            clone.Atoms = new BaseAtomRef_Collection(clone);
            clone.Bonds = new BaseBondRef_Collection(clone);
            clone.LonePairs = clone.lonepairs;
            clone.SingleElectrons = clone.electrons;
            clone.StereoElements = clone.stereo;
            clone.RemoveAllElectronContainers();

            // clone atoms
            IAtom[] c_atoms = new IAtom[this.atoms.Count];
            for (int i = 0; i < this.atoms.Count; i++)
            {
                c_atoms[i] = (IAtom)this.atoms[i].Deref().Clone(map);
            }
            clone.SetAtoms(c_atoms);
            for (int i = 0; i < c_atoms.Length; i++)
                refmap.Set(this.atoms[i], clone.Atoms[i]);

            // clone bonds using a the mappings from the original to the clone
            var bonds = new IBond[this.bonds.Count];
            for (int i = 0; i < this.bonds.Count; i++)
            {
                var original = this.bonds[i];
                var bond = (IBond)original.Deref().Clone(map);
                var n = bond.Atoms.Count;
                var members = new IAtom[n];
                for (int j = 0; j < n; j++)
                {
                    members[j] = map.Get(original.Atoms[j]);
                }

                bond.SetAtoms(members);
                bonds[i] = bond;
            }
            clone.SetBonds(bonds);
            for (int i = 0; i < bonds.Length; i++)
                refmap.Set(this.bonds[i], clone.Bonds[i]);

            // clone lone pairs (we can't use an array to buffer as there is no setLonePairs())
            for (int i = 0; i < this.lonepairs.Count; i++)
            {
                var original = this.lonepairs[i];
                var pair = (ILonePair)original.Clone();

                if (pair.Atom != null)
                    pair.Atom = refmap.Get(original.Atom);

                clone.LonePairs.Add(pair);
            }

            // clone single electrons (we can't use an array to buffer as there is no setSingleElectrons())
            for (int i = 0; i < electrons.Count; i++)
            {
                var original = this.electrons[i];
                var electron = (ISingleElectron)original.Clone();

                if (electron.Atom != null)
                    electron.Atom = refmap.Get(original.Atom);

                clone.SingleElectrons.Add(electron);
            }

            // map each stereo element to a new instance in the clone
            foreach (var element in stereo)
            {
                clone.StereoElements.Add((IStereoElement<IChemObject, IChemObject>)element.Clone(refmap));
            }

            // update sgroups
            var sgroups = this.GetCtabSgroups();
            if (sgroups != null)
            {
                clone.SetCtabSgroups(SgroupManipulator.Copy(sgroups, refmap));
            }

            return clone;
        }

        /// <inheritdoc/>
        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
        }

        /// <inheritdoc/>
        public bool IsEmpty() => atoms.Count == 0;
    }
}


