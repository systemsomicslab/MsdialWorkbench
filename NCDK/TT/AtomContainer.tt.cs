



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 1997-2007  Christoph Steinbeck
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
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

using NCDK.Sgroups;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable CA1710 // Identifiers should have correct suffix

namespace NCDK.Default
{
    /// <summary>
    /// Base class for all chemical objects that maintain a list of <see cref="IAtom"/>s and <see cref="IElectronContainer"/>s.
    /// </summary>
    /// <example>
    /// Looping over all Bonds in the AtomContainer is typically done like: 
    /// <code>
    /// foreach (IBond aBond in atomContainer.Bonds)
    /// {
    ///     // do something
    /// }
    /// </code>
    /// </example>
    // @author steinbeck
    // @cdk.created 2000-10-02 
    public partial class AtomContainer
        : ChemObject, IAtomContainer, IChemObjectListener
    {
        private static readonly IChemObjectBuilder builder = new ChemObjectBuilder(true);

        /// <summary>
        /// Atoms contained by this object.
        /// </summary>
        internal ObservableChemObjectCollection_IAtom atoms;

        /// <summary>
        /// Bonds contained by this object.
        /// </summary>
        internal ObservableChemObjectCollection<IBond> bonds;

        /// <summary>
        /// Lone pairs contained by this object.
        /// </summary>
        internal ObservableChemObjectCollection<ILonePair> lonePairs;

        /// <summary>
        /// Single electrons contained by this object.
        /// </summary>
        internal ObservableChemObjectCollection<ISingleElectron> singleElectrons;

        /// <summary>
        /// Stereo elements contained by this object.
        /// </summary>
        internal List<IStereoElement<IChemObject, IChemObject>> stereo;

        /// <inheritdoc/>
        public override IChemObjectBuilder Builder => builder;

        internal class ObservableChemObjectCollection_IAtom
            : ObservableChemObjectCollection<IAtom>
        {
            AtomContainer parent;

            public ObservableChemObjectCollection_IAtom(AtomContainer parent, IEnumerable<IAtom> atoms)
                : base(parent, atoms)
            {
                this.parent = parent;
                AllowDuplicate = false;
            }

            public override IAtom this[int index]
            {
                get => base[index];

                set
                {
                    if (index >= base.Count)
                        throw new ArgumentOutOfRangeException($"No atom at index:{index}");
                    int aidx = base.IndexOf(value);
                    if (aidx >= 0)
                        throw new InvalidOperationException($"Atom already in container at index: {index}");
                    IAtom oldAtom = base[index];
                    base[index] = value;

         
                    value.Listeners.Add(parent);
                    oldAtom.Listeners.Remove(parent);
         

                    // replace in electron containers
                    foreach (var bond in parent.bonds)
                    {
                        for (int i = 0; i < bond.Atoms.Count; i++)
                        {
                            if (oldAtom.Equals(bond.Atoms[i]))
                            {
                                bond.Atoms[i] = value;
                            }
                        }
                    }
                    foreach (var ec in parent.singleElectrons)
                    {
                        if (oldAtom.Equals(ec.Atom))
                            ec.Atom = value;
                    }
                    foreach (var lp in parent.lonePairs)
                    {
                        if (oldAtom.Equals(lp.Atom))
                            lp.Atom = value;
                    }

                    // update stereo
                    CDKObjectMap map = null;
                    List<IStereoElement<IChemObject, IChemObject>> oldStereo = null;
                    List<IStereoElement<IChemObject, IChemObject>> newStereo = null;

                    foreach (var se in parent.stereo)
                    {
                        if (se.Contains(oldAtom))
                        {
                            if (oldStereo == null)
                            {
                                oldStereo = new List<IStereoElement<IChemObject, IChemObject>>();
                                newStereo = new List<IStereoElement<IChemObject, IChemObject>>();
                                map = new CDKObjectMap();
                                foreach (var a in list)
                                    map.Add(a, a);
                                map.Set(oldAtom, value);
                            }
                            oldStereo.Add(se);
                            newStereo.Add((IStereoElement<IChemObject, IChemObject>)se.Clone(map));
                        }
                    }
                    if (oldStereo != null)
                    {
                        foreach (var stereo in oldStereo)
                            parent.stereo.Remove(stereo);
                        foreach (var stereo in newStereo)
                            parent.stereo.Add(stereo);
                    }

         
                    parent.NotifyChanged();
         
                }
            }
        }

        private void Init(
            ObservableChemObjectCollection_IAtom atoms,
            ObservableChemObjectCollection<IBond> bonds,
            ObservableChemObjectCollection<ILonePair> lonePairs,
            ObservableChemObjectCollection<ISingleElectron> singleElectrons,
            List<IStereoElement<IChemObject, IChemObject>> stereo)
        {
            this.atoms = atoms;
            this.bonds = bonds;
            this.lonePairs = lonePairs;
            this.singleElectrons = singleElectrons;
            this.stereo = stereo;
        }

        public AtomContainer(
            IEnumerable<IAtom> atoms,
            IEnumerable<IBond> bonds,
            IEnumerable<ILonePair> lonePairs,
            IEnumerable<ISingleElectron> singleElectrons,
            IEnumerable<IStereoElement<IChemObject, IChemObject>> stereo)
        {
            Init(
                new ObservableChemObjectCollection_IAtom(this, atoms ?? Array.Empty<IAtom>()),
                CreateObservableChemObjectCollection(bonds ?? Array.Empty<IBond>(), true),
                CreateObservableChemObjectCollection(lonePairs ?? Array.Empty<ILonePair>(), true),
                CreateObservableChemObjectCollection(singleElectrons ?? Array.Empty<ISingleElectron>(), true),
                new List<IStereoElement<IChemObject, IChemObject>>(stereo ?? Array.Empty<IStereoElement<IChemObject, IChemObject>>())
            );
        }

        private ObservableChemObjectCollection<T> CreateObservableChemObjectCollection<T>(IEnumerable<T> objs, bool allowDup) where T : IChemObject
        {
 
            var list = new ObservableChemObjectCollection<T>(this, objs)
            {
                AllowDuplicate = allowDup
            };
            return list;
        }

        public AtomContainer(
           IEnumerable<IAtom> atoms,
           IEnumerable<IBond> bonds)
             : this(
                  atoms,
                  bonds,
                  Array.Empty<ILonePair>(),
                  Array.Empty<ISingleElectron>(),
                  Array.Empty<IStereoElement<IChemObject, IChemObject>>())
        { }

        /// <summary>
        ///  Constructs an empty AtomContainer.
        /// </summary>
        public AtomContainer()
            : this(
                      Array.Empty<IAtom>(), 
                      Array.Empty<IBond>(), 
                      Array.Empty<ILonePair>(),
                      Array.Empty<ISingleElectron>(),
                      Array.Empty<IStereoElement<IChemObject, IChemObject>>())
        { }

        /// <summary>
        /// Constructs an AtomContainer with a copy of the atoms and electronContainers
        /// of another AtomContainer (A shallow copy, i.e., with the same objects as in
        /// the original AtomContainer).
        /// </summary>
        /// <param name="container">An AtomContainer to copy the atoms and electronContainers from</param>
        public AtomContainer(IAtomContainer container)
            : this(
                  container.Atoms,
                  container.Bonds,
                  container.LonePairs,
                  container.SingleElectrons,
                  container.StereoElements)
        { }

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
        public virtual bool IsSingleOrDouble
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

        /// <inheritdoc/>
        public virtual IList<IAtom> Atoms => atoms;

        /// <inheritdoc/>
        public virtual IList<IBond> Bonds => bonds;

        /// <inheritdoc/>
        public virtual IList<ILonePair> LonePairs => lonePairs;

        /// <inheritdoc/>
        public virtual IList<ISingleElectron> SingleElectrons => singleElectrons;

        /// <inheritdoc/>
        public virtual ICollection<IStereoElement<IChemObject, IChemObject>> StereoElements => stereo;

        /// <summary>
        /// Returns the bond that connects the two given atoms.
        /// </summary>
        /// <param name="atom1">The first atom</param>
        /// <param name="atom2">The second atom</param>
        /// <returns>The bond that connects the two atoms</returns>
        public virtual IBond GetBond(IAtom atom1, IAtom atom2)
        {
            return bonds.Where(bond => bond.Contains(atom1) && bond.GetOther(atom1).Equals(atom2)).FirstOrDefault();
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IAtom> GetConnectedAtoms(IAtom atom)
        {
            if (!atoms.Contains(atom))
                throw new NoSuchAtomException("Atom does not belong to the container!");
            return Bonds.Where(n => n.Contains(atom)).Select(n => n.GetOther(atom));
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IBond> GetConnectedBonds(IAtom atom)
        {
            if (!atoms.Contains(atom))
                throw new NoSuchAtomException("Atom does not belong to the container!");
            return bonds.Where(bond => bond.Contains(atom));
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ILonePair> GetConnectedLonePairs(IAtom atom)
        {
            if (!atoms.Contains(atom))
                throw new NoSuchAtomException("Atom does not belong to the container!");
            return LonePairs.Where(lonePair => lonePair.Contains(atom));
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ISingleElectron> GetConnectedSingleElectrons(IAtom atom)
        {
            if (!atoms.Contains(atom))
                throw new NoSuchAtomException("Atom does not belong to the container!");
            return SingleElectrons.Where(singleElectron => singleElectron.Contains(atom));
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IElectronContainer> GetConnectedElectronContainers(IAtom atom)
        {
            foreach (var e in GetConnectedBonds(atom))
                yield return e;
            foreach (var e in GetConnectedLonePairs(atom))
                yield return e;
            foreach (var e in GetConnectedSingleElectrons(atom))
                yield return e;
            yield break;
        }

        private IEnumerable<BondOrder> GetBondOrders(IAtom atom)
        {
            return bonds.Where(bond => bond.Contains(atom))
                .Select(bond => bond.Order)
                .Where(order => !order.IsUnset());
        }

        /// <inheritdoc/>
        public virtual double GetBondOrderSum(IAtom atom)
        {
            return GetBondOrders(atom).Select(order => order.Numeric()).Sum();
        }

        /// <inheritdoc/>
        public virtual BondOrder GetMaximumBondOrder(IAtom atom)
        {
            BondOrder max = BondOrder.Unset;
            foreach (IBond bond in Bonds)
            {
                if (!bond.Contains(atom))
                    continue;
                if (max == BondOrder.Unset || bond.Order.Numeric() > max.Numeric()) 
                {
                    max = bond.Order;
                }
            }
            if (max == BondOrder.Unset)
            {
                if (!Contains(atom))
                    throw new NoSuchAtomException("Atom does not belong to this container!");
                if (atom.ImplicitHydrogenCount != null &&
                    atom.ImplicitHydrogenCount > 0)
                    max = BondOrder.Single;
                else
                    max = BondOrder.Unset;
            }
            return max;
        }

        /// <inheritdoc/>
        public virtual BondOrder GetMinimumBondOrder(IAtom atom)
        {
            BondOrder min = BondOrder.Unset;
            foreach (IBond bond in Bonds) 
            {
                if (!bond.Contains(atom))
                    continue;
                if (min == BondOrder.Unset || bond.Order.Numeric() < min.Numeric()) 
                {
                    min = bond.Order;
                }
            }
            if (min == BondOrder.Unset) 
            {
                if (!Contains(atom))
                    throw new NoSuchAtomException("Atom does not belong to this container!");
                if (atom.ImplicitHydrogenCount != null &&
                    atom.ImplicitHydrogenCount > 0)
                    min = BondOrder.Single;
                else
                    min = BondOrder.Unset;
            }
            return min;
        }

        /// <inheritdoc/>
        public virtual void Add(IAtomContainer that)
        {
            foreach (IAtom atom in that.Atoms)
                atom.IsVisited = false;
            foreach (IBond bond in that.Bonds)
                bond.IsVisited = false;
            foreach (IAtom atom in this.Atoms)
                atom.IsVisited = true;
            foreach (IBond bond in this.Bonds)
                bond.IsVisited = true;

            foreach (var atom in that.Atoms.Where(atom => !atom.IsVisited))
                Atoms.Add(atom);
            foreach (var bond in that.Bonds.Where(bond => !bond.IsVisited))
                Bonds.Add(bond);
            foreach (var lonePair in that.LonePairs.Where(lonePair => !lonePair.IsVisited))
                LonePairs.Add(lonePair);
            foreach (var singleElectron in that.SingleElectrons.Where(singleElectron => !Contains(singleElectron)))
                SingleElectrons.Add(singleElectron);
            foreach (var se in that.StereoElements)
                stereo.Add(se);

             NotifyChanged();         }

        /// <inheritdoc/>
        public virtual void Add(IAtom atom)
        {
            Atoms.Add(atom);
        }

        /// <inheritdoc/>
        public virtual void Add(IElectronContainer electronContainer)
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
        public virtual void Remove(IAtomContainer atomContainer)
        {
            foreach (var atom in atomContainer.Atoms)
                Atoms.Remove(atom);
            foreach (var bond in atomContainer.Bonds)
                Bonds.Remove(bond);
            foreach (var lonePair in atomContainer.LonePairs)
                LonePairs.Remove(lonePair);
            foreach (var singleElectron in atomContainer.SingleElectrons)
                SingleElectrons.Remove(singleElectron);
        }

        /// <inheritdoc/>
        public virtual void Remove(IElectronContainer electronContainer)
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
        [Obsolete("Use " + nameof(RemoveAtom))]
        public virtual void RemoveAtomAndConnectedElectronContainers(IAtom atom)
        {
            RemoveAtom(atom);
        }

        /// <inheritdoc/>
        public virtual void RemoveAtom(IAtom atom)
        {
            {
                var toRemove = bonds.Where(bond => bond.Contains(atom)).ToList();
                foreach (var bond in toRemove)
                    bonds.Remove(bond);
            }
            {
                var toRemove = lonePairs.Where(lonePair => lonePair.Contains(atom)).ToList();
                foreach (var lonePair in toRemove)
                    lonePairs.Remove(lonePair);
            }
            {
                var toRemove = singleElectrons.Where(singleElectron => singleElectron.Contains(atom)).ToList();
                foreach (var singleElectron in toRemove)
                    singleElectrons.Remove(singleElectron);
            }
            {
                var toRemove = stereo.Where(stereoElement => stereoElement.Contains(atom)).ToList();
                foreach (var stereoElement in toRemove)
                    stereo.Remove(stereoElement);
            }

            Atoms.Remove(atom);

             NotifyChanged();         }

        /// <inheritdoc/>
        public virtual void RemoveAtom(int pos) 
        {
            RemoveAtom(Atoms[pos]);
        }

        /// <inheritdoc/>
        public virtual void RemoveAllElements()
        {
            RemoveAllElectronContainers();
            foreach (var atom in atoms)
                atom.Listeners?.Remove(this);
            atoms.Clear();
            stereo.Clear();

             NotifyChanged();         }

        /// <inheritdoc/>
        public virtual void RemoveAllElectronContainers()
        {
            Bonds.Clear();
            foreach (var e in lonePairs)
                e.Listeners?.Remove(this);
            foreach (var e in singleElectrons)
                e.Listeners?.Remove(this);
            lonePairs.Clear();
            singleElectrons.Clear();

             NotifyChanged();         }

        /// <summary>
        /// Removes the bond that connects the two given atoms.
        /// </summary>
        /// <param name="atom1">The first atom</param>
        /// <param name="atom2">The second atom</param>
        /// <returns>The bond that connects the two atoms</returns>
        public virtual IBond RemoveBond(IAtom atom1, IAtom atom2)
        {
            var bond = GetBond(atom1, atom2);
            RemoveBond(bond);
            return bond;
        }

        public virtual void RemoveBond(IBond bond)
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
        public virtual bool Contains(IAtom atom) => atoms.Any(n => n.Equals(atom));

        /// <inheritdoc/>
        public virtual bool Contains(IBond bond) => bonds.Any(n => n.Equals(bond));

        /// <inheritdoc/>
        public virtual bool Contains(ILonePair lonePair) => lonePairs.Any(n => n == lonePair);

        /// <inheritdoc/>
        public virtual bool Contains(ISingleElectron singleElectron) => singleElectrons.Any(n => n == singleElectron);

        /// <inheritdoc/>
        public virtual bool Contains(IElectronContainer electronContainer)
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
        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (AtomContainer)base.Clone(map);            
            clone.atoms = new ObservableChemObjectCollection_IAtom(this, atoms.Where(n => n != null).Select(n => (IAtom)n.Clone(map)));
            clone.bonds = CreateObservableChemObjectCollection(bonds.Where(n => n != null).Select(n => (IBond)n.Clone(map)), true);
            clone.lonePairs = CreateObservableChemObjectCollection(lonePairs.Where(n => n != null).Select(n => (ILonePair)n.Clone(map)), true);
            clone.singleElectrons = CreateObservableChemObjectCollection(singleElectrons.Where(n => n != null).Select(n => (ISingleElectron)n.Clone(map)), true);
            clone.stereo = new List<IStereoElement<IChemObject, IChemObject>>(stereo.Select(n => (IStereoElement<IChemObject, IChemObject>)n.Clone(map)));

            // update sgroups
            var sgroups = this.GetCtabSgroups();
            if (sgroups != null)
            {
                clone.SetCtabSgroups(SgroupManipulator.Copy(sgroups, map));
            }

            return clone;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IElectronContainer> GetElectronContainers()
        {
            return bonds.Cast<IElectronContainer>().Concat(LonePairs).Concat(SingleElectrons);
        }

        /// <inheritdoc/>
        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
             NotifyChanged(evt);         }

        /// <inheritdoc/>
        public void SetAtoms(IEnumerable<IAtom> atoms)
        {
            this.atoms.Clear();
            this.atoms.AddRange(atoms);
        }

        /// <inheritdoc/>
        public void SetBonds(IEnumerable<IBond> bonds)
        {
            this.bonds.Clear();
            this.bonds.AddRange(bonds);
        }

        /// <inheritdoc/>
        public virtual bool IsEmpty() => atoms.Count == 0;

        /// <inheritdoc/>
        public virtual string Title 
        { 
            get { return  GetProperty<string>(CDKPropertyName.Title); }
            set { SetProperty(CDKPropertyName.Title, value); }
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// Base class for all chemical objects that maintain a list of <see cref="IAtom"/>s and <see cref="IElectronContainer"/>s.
    /// </summary>
    /// <example>
    /// Looping over all Bonds in the AtomContainer is typically done like: 
    /// <code>
    /// foreach (IBond aBond in atomContainer.Bonds)
    /// {
    ///     // do something
    /// }
    /// </code>
    /// </example>
    // @author steinbeck
    // @cdk.created 2000-10-02 
    public partial class AtomContainer
        : ChemObject, IAtomContainer, IChemObjectListener
    {
        private static readonly IChemObjectBuilder builder = new ChemObjectBuilder(true);

        /// <summary>
        /// Atoms contained by this object.
        /// </summary>
        internal ObservableChemObjectCollection_IAtom atoms;

        /// <summary>
        /// Bonds contained by this object.
        /// </summary>
        internal ObservableChemObjectCollection<IBond> bonds;

        /// <summary>
        /// Lone pairs contained by this object.
        /// </summary>
        internal ObservableChemObjectCollection<ILonePair> lonePairs;

        /// <summary>
        /// Single electrons contained by this object.
        /// </summary>
        internal ObservableChemObjectCollection<ISingleElectron> singleElectrons;

        /// <summary>
        /// Stereo elements contained by this object.
        /// </summary>
        internal List<IStereoElement<IChemObject, IChemObject>> stereo;

        /// <inheritdoc/>
        public override IChemObjectBuilder Builder => builder;

        internal class ObservableChemObjectCollection_IAtom
            : ObservableChemObjectCollection<IAtom>
        {
            AtomContainer parent;

            public ObservableChemObjectCollection_IAtom(AtomContainer parent, IEnumerable<IAtom> atoms)
                : base(parent, atoms)
            {
                this.parent = parent;
                AllowDuplicate = false;
            }

            public override IAtom this[int index]
            {
                get => base[index];

                set
                {
                    if (index >= base.Count)
                        throw new ArgumentOutOfRangeException($"No atom at index:{index}");
                    int aidx = base.IndexOf(value);
                    if (aidx >= 0)
                        throw new InvalidOperationException($"Atom already in container at index: {index}");
                    IAtom oldAtom = base[index];
                    base[index] = value;

         

                    // replace in electron containers
                    foreach (var bond in parent.bonds)
                    {
                        for (int i = 0; i < bond.Atoms.Count; i++)
                        {
                            if (oldAtom.Equals(bond.Atoms[i]))
                            {
                                bond.Atoms[i] = value;
                            }
                        }
                    }
                    foreach (var ec in parent.singleElectrons)
                    {
                        if (oldAtom.Equals(ec.Atom))
                            ec.Atom = value;
                    }
                    foreach (var lp in parent.lonePairs)
                    {
                        if (oldAtom.Equals(lp.Atom))
                            lp.Atom = value;
                    }

                    // update stereo
                    CDKObjectMap map = null;
                    List<IStereoElement<IChemObject, IChemObject>> oldStereo = null;
                    List<IStereoElement<IChemObject, IChemObject>> newStereo = null;

                    foreach (var se in parent.stereo)
                    {
                        if (se.Contains(oldAtom))
                        {
                            if (oldStereo == null)
                            {
                                oldStereo = new List<IStereoElement<IChemObject, IChemObject>>();
                                newStereo = new List<IStereoElement<IChemObject, IChemObject>>();
                                map = new CDKObjectMap();
                                foreach (var a in list)
                                    map.Add(a, a);
                                map.Set(oldAtom, value);
                            }
                            oldStereo.Add(se);
                            newStereo.Add((IStereoElement<IChemObject, IChemObject>)se.Clone(map));
                        }
                    }
                    if (oldStereo != null)
                    {
                        foreach (var stereo in oldStereo)
                            parent.stereo.Remove(stereo);
                        foreach (var stereo in newStereo)
                            parent.stereo.Add(stereo);
                    }

         
                }
            }
        }

        private void Init(
            ObservableChemObjectCollection_IAtom atoms,
            ObservableChemObjectCollection<IBond> bonds,
            ObservableChemObjectCollection<ILonePair> lonePairs,
            ObservableChemObjectCollection<ISingleElectron> singleElectrons,
            List<IStereoElement<IChemObject, IChemObject>> stereo)
        {
            this.atoms = atoms;
            this.bonds = bonds;
            this.lonePairs = lonePairs;
            this.singleElectrons = singleElectrons;
            this.stereo = stereo;
        }

        public AtomContainer(
            IEnumerable<IAtom> atoms,
            IEnumerable<IBond> bonds,
            IEnumerable<ILonePair> lonePairs,
            IEnumerable<ISingleElectron> singleElectrons,
            IEnumerable<IStereoElement<IChemObject, IChemObject>> stereo)
        {
            Init(
                new ObservableChemObjectCollection_IAtom(this, atoms ?? Array.Empty<IAtom>()),
                CreateObservableChemObjectCollection(bonds ?? Array.Empty<IBond>(), true),
                CreateObservableChemObjectCollection(lonePairs ?? Array.Empty<ILonePair>(), true),
                CreateObservableChemObjectCollection(singleElectrons ?? Array.Empty<ISingleElectron>(), true),
                new List<IStereoElement<IChemObject, IChemObject>>(stereo ?? Array.Empty<IStereoElement<IChemObject, IChemObject>>())
            );
        }

        private ObservableChemObjectCollection<T> CreateObservableChemObjectCollection<T>(IEnumerable<T> objs, bool allowDup) where T : IChemObject
        {
 
            var list = new ObservableChemObjectCollection<T>(null, objs)
            {
                AllowDuplicate = allowDup
            };
            return list;
        }

        public AtomContainer(
           IEnumerable<IAtom> atoms,
           IEnumerable<IBond> bonds)
             : this(
                  atoms,
                  bonds,
                  Array.Empty<ILonePair>(),
                  Array.Empty<ISingleElectron>(),
                  Array.Empty<IStereoElement<IChemObject, IChemObject>>())
        { }

        /// <summary>
        ///  Constructs an empty AtomContainer.
        /// </summary>
        public AtomContainer()
            : this(
                      Array.Empty<IAtom>(), 
                      Array.Empty<IBond>(), 
                      Array.Empty<ILonePair>(),
                      Array.Empty<ISingleElectron>(),
                      Array.Empty<IStereoElement<IChemObject, IChemObject>>())
        { }

        /// <summary>
        /// Constructs an AtomContainer with a copy of the atoms and electronContainers
        /// of another AtomContainer (A shallow copy, i.e., with the same objects as in
        /// the original AtomContainer).
        /// </summary>
        /// <param name="container">An AtomContainer to copy the atoms and electronContainers from</param>
        public AtomContainer(IAtomContainer container)
            : this(
                  container.Atoms,
                  container.Bonds,
                  container.LonePairs,
                  container.SingleElectrons,
                  container.StereoElements)
        { }

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
        public virtual bool IsSingleOrDouble
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

        /// <inheritdoc/>
        public virtual IList<IAtom> Atoms => atoms;

        /// <inheritdoc/>
        public virtual IList<IBond> Bonds => bonds;

        /// <inheritdoc/>
        public virtual IList<ILonePair> LonePairs => lonePairs;

        /// <inheritdoc/>
        public virtual IList<ISingleElectron> SingleElectrons => singleElectrons;

        /// <inheritdoc/>
        public virtual ICollection<IStereoElement<IChemObject, IChemObject>> StereoElements => stereo;

        /// <summary>
        /// Returns the bond that connects the two given atoms.
        /// </summary>
        /// <param name="atom1">The first atom</param>
        /// <param name="atom2">The second atom</param>
        /// <returns>The bond that connects the two atoms</returns>
        public virtual IBond GetBond(IAtom atom1, IAtom atom2)
        {
            return bonds.Where(bond => bond.Contains(atom1) && bond.GetOther(atom1).Equals(atom2)).FirstOrDefault();
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IAtom> GetConnectedAtoms(IAtom atom)
        {
            if (!atoms.Contains(atom))
                throw new NoSuchAtomException("Atom does not belong to the container!");
            return Bonds.Where(n => n.Contains(atom)).Select(n => n.GetOther(atom));
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IBond> GetConnectedBonds(IAtom atom)
        {
            if (!atoms.Contains(atom))
                throw new NoSuchAtomException("Atom does not belong to the container!");
            return bonds.Where(bond => bond.Contains(atom));
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ILonePair> GetConnectedLonePairs(IAtom atom)
        {
            if (!atoms.Contains(atom))
                throw new NoSuchAtomException("Atom does not belong to the container!");
            return LonePairs.Where(lonePair => lonePair.Contains(atom));
        }

        /// <inheritdoc/>
        public virtual IEnumerable<ISingleElectron> GetConnectedSingleElectrons(IAtom atom)
        {
            if (!atoms.Contains(atom))
                throw new NoSuchAtomException("Atom does not belong to the container!");
            return SingleElectrons.Where(singleElectron => singleElectron.Contains(atom));
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IElectronContainer> GetConnectedElectronContainers(IAtom atom)
        {
            foreach (var e in GetConnectedBonds(atom))
                yield return e;
            foreach (var e in GetConnectedLonePairs(atom))
                yield return e;
            foreach (var e in GetConnectedSingleElectrons(atom))
                yield return e;
            yield break;
        }

        private IEnumerable<BondOrder> GetBondOrders(IAtom atom)
        {
            return bonds.Where(bond => bond.Contains(atom))
                .Select(bond => bond.Order)
                .Where(order => !order.IsUnset());
        }

        /// <inheritdoc/>
        public virtual double GetBondOrderSum(IAtom atom)
        {
            return GetBondOrders(atom).Select(order => order.Numeric()).Sum();
        }

        /// <inheritdoc/>
        public virtual BondOrder GetMaximumBondOrder(IAtom atom)
        {
            BondOrder max = BondOrder.Unset;
            foreach (IBond bond in Bonds)
            {
                if (!bond.Contains(atom))
                    continue;
                if (max == BondOrder.Unset || bond.Order.Numeric() > max.Numeric()) 
                {
                    max = bond.Order;
                }
            }
            if (max == BondOrder.Unset)
            {
                if (!Contains(atom))
                    throw new NoSuchAtomException("Atom does not belong to this container!");
                if (atom.ImplicitHydrogenCount != null &&
                    atom.ImplicitHydrogenCount > 0)
                    max = BondOrder.Single;
                else
                    max = BondOrder.Unset;
            }
            return max;
        }

        /// <inheritdoc/>
        public virtual BondOrder GetMinimumBondOrder(IAtom atom)
        {
            BondOrder min = BondOrder.Unset;
            foreach (IBond bond in Bonds) 
            {
                if (!bond.Contains(atom))
                    continue;
                if (min == BondOrder.Unset || bond.Order.Numeric() < min.Numeric()) 
                {
                    min = bond.Order;
                }
            }
            if (min == BondOrder.Unset) 
            {
                if (!Contains(atom))
                    throw new NoSuchAtomException("Atom does not belong to this container!");
                if (atom.ImplicitHydrogenCount != null &&
                    atom.ImplicitHydrogenCount > 0)
                    min = BondOrder.Single;
                else
                    min = BondOrder.Unset;
            }
            return min;
        }

        /// <inheritdoc/>
        public virtual void Add(IAtomContainer that)
        {
            foreach (IAtom atom in that.Atoms)
                atom.IsVisited = false;
            foreach (IBond bond in that.Bonds)
                bond.IsVisited = false;
            foreach (IAtom atom in this.Atoms)
                atom.IsVisited = true;
            foreach (IBond bond in this.Bonds)
                bond.IsVisited = true;

            foreach (var atom in that.Atoms.Where(atom => !atom.IsVisited))
                Atoms.Add(atom);
            foreach (var bond in that.Bonds.Where(bond => !bond.IsVisited))
                Bonds.Add(bond);
            foreach (var lonePair in that.LonePairs.Where(lonePair => !lonePair.IsVisited))
                LonePairs.Add(lonePair);
            foreach (var singleElectron in that.SingleElectrons.Where(singleElectron => !Contains(singleElectron)))
                SingleElectrons.Add(singleElectron);
            foreach (var se in that.StereoElements)
                stereo.Add(se);

                    }

        /// <inheritdoc/>
        public virtual void Add(IAtom atom)
        {
            Atoms.Add(atom);
        }

        /// <inheritdoc/>
        public virtual void Add(IElectronContainer electronContainer)
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
        public virtual void Remove(IAtomContainer atomContainer)
        {
            foreach (var atom in atomContainer.Atoms)
                Atoms.Remove(atom);
            foreach (var bond in atomContainer.Bonds)
                Bonds.Remove(bond);
            foreach (var lonePair in atomContainer.LonePairs)
                LonePairs.Remove(lonePair);
            foreach (var singleElectron in atomContainer.SingleElectrons)
                SingleElectrons.Remove(singleElectron);
        }

        /// <inheritdoc/>
        public virtual void Remove(IElectronContainer electronContainer)
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
        [Obsolete("Use " + nameof(RemoveAtom))]
        public virtual void RemoveAtomAndConnectedElectronContainers(IAtom atom)
        {
            RemoveAtom(atom);
        }

        /// <inheritdoc/>
        public virtual void RemoveAtom(IAtom atom)
        {
            {
                var toRemove = bonds.Where(bond => bond.Contains(atom)).ToList();
                foreach (var bond in toRemove)
                    bonds.Remove(bond);
            }
            {
                var toRemove = lonePairs.Where(lonePair => lonePair.Contains(atom)).ToList();
                foreach (var lonePair in toRemove)
                    lonePairs.Remove(lonePair);
            }
            {
                var toRemove = singleElectrons.Where(singleElectron => singleElectron.Contains(atom)).ToList();
                foreach (var singleElectron in toRemove)
                    singleElectrons.Remove(singleElectron);
            }
            {
                var toRemove = stereo.Where(stereoElement => stereoElement.Contains(atom)).ToList();
                foreach (var stereoElement in toRemove)
                    stereo.Remove(stereoElement);
            }

            Atoms.Remove(atom);

                    }

        /// <inheritdoc/>
        public virtual void RemoveAtom(int pos) 
        {
            RemoveAtom(Atoms[pos]);
        }

        /// <inheritdoc/>
        public virtual void RemoveAllElements()
        {
            RemoveAllElectronContainers();
            foreach (var atom in atoms)
                atom.Listeners?.Remove(this);
            atoms.Clear();
            stereo.Clear();

                    }

        /// <inheritdoc/>
        public virtual void RemoveAllElectronContainers()
        {
            Bonds.Clear();
            foreach (var e in lonePairs)
                e.Listeners?.Remove(this);
            foreach (var e in singleElectrons)
                e.Listeners?.Remove(this);
            lonePairs.Clear();
            singleElectrons.Clear();

                    }

        /// <summary>
        /// Removes the bond that connects the two given atoms.
        /// </summary>
        /// <param name="atom1">The first atom</param>
        /// <param name="atom2">The second atom</param>
        /// <returns>The bond that connects the two atoms</returns>
        public virtual IBond RemoveBond(IAtom atom1, IAtom atom2)
        {
            var bond = GetBond(atom1, atom2);
            RemoveBond(bond);
            return bond;
        }

        public virtual void RemoveBond(IBond bond)
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
        public virtual bool Contains(IAtom atom) => atoms.Any(n => n.Equals(atom));

        /// <inheritdoc/>
        public virtual bool Contains(IBond bond) => bonds.Any(n => n.Equals(bond));

        /// <inheritdoc/>
        public virtual bool Contains(ILonePair lonePair) => lonePairs.Any(n => n == lonePair);

        /// <inheritdoc/>
        public virtual bool Contains(ISingleElectron singleElectron) => singleElectrons.Any(n => n == singleElectron);

        /// <inheritdoc/>
        public virtual bool Contains(IElectronContainer electronContainer)
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
        public override ICDKObject Clone(CDKObjectMap map)
        {
            var clone = (AtomContainer)base.Clone(map);            
            clone.atoms = new ObservableChemObjectCollection_IAtom(this, atoms.Where(n => n != null).Select(n => (IAtom)n.Clone(map)));
            clone.bonds = CreateObservableChemObjectCollection(bonds.Where(n => n != null).Select(n => (IBond)n.Clone(map)), true);
            clone.lonePairs = CreateObservableChemObjectCollection(lonePairs.Where(n => n != null).Select(n => (ILonePair)n.Clone(map)), true);
            clone.singleElectrons = CreateObservableChemObjectCollection(singleElectrons.Where(n => n != null).Select(n => (ISingleElectron)n.Clone(map)), true);
            clone.stereo = new List<IStereoElement<IChemObject, IChemObject>>(stereo.Select(n => (IStereoElement<IChemObject, IChemObject>)n.Clone(map)));

            // update sgroups
            var sgroups = this.GetCtabSgroups();
            if (sgroups != null)
            {
                clone.SetCtabSgroups(SgroupManipulator.Copy(sgroups, map));
            }

            return clone;
        }

        /// <inheritdoc/>
        public virtual IEnumerable<IElectronContainer> GetElectronContainers()
        {
            return bonds.Cast<IElectronContainer>().Concat(LonePairs).Concat(SingleElectrons);
        }

        /// <inheritdoc/>
        public void OnStateChanged(ChemObjectChangeEventArgs evt)
        {
                    }

        /// <inheritdoc/>
        public void SetAtoms(IEnumerable<IAtom> atoms)
        {
            this.atoms.Clear();
            this.atoms.AddRange(atoms);
        }

        /// <inheritdoc/>
        public void SetBonds(IEnumerable<IBond> bonds)
        {
            this.bonds.Clear();
            this.bonds.AddRange(bonds);
        }

        /// <inheritdoc/>
        public virtual bool IsEmpty() => atoms.Count == 0;

        /// <inheritdoc/>
        public virtual string Title 
        { 
            get { return  GetProperty<string>(CDKPropertyName.Title); }
            set { SetProperty(CDKPropertyName.Title, value); }
        }
    }
}
