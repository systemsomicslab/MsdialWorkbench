/*
 * Copyright (C) 2012 John May <jwmay@users.sf.net>
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

namespace NCDK.Graphs
{
    /// <summary>
    /// Utility to determine the shortest paths between all pairs of atoms in a molecule.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.AllPairsShortestPaths_Example.cs"]/*' />
    /// </example>
    /// <seealso cref="ShortestPaths"/>
    // @author John May
    // @cdk.module core
    public sealed class AllPairsShortestPaths
    {
        private readonly IAtomContainer container;
        private readonly ShortestPaths[] shortestPaths;

        /// <summary>
        /// Create a new all shortest paths utility for an <see cref="IAtomContainer"/>.
        /// </summary>
        /// <param name="container">the molecule of which to find the shortest paths</param>
        public AllPairsShortestPaths(IAtomContainer container)
        {
            // ToAdjList performs null check
            int[][] adjacent = GraphUtil.ToAdjList(container);
            int n = container.Atoms.Count;
            this.container = container;
            this.shortestPaths = new ShortestPaths[n];

            // for each atom construct the ShortestPaths object
            for (int i = 0; i < n; i++)
            {
                shortestPaths[i] = new ShortestPaths(adjacent, container, i);
            }
        }

        /// <summary>
        /// Access the shortest paths object for provided start vertex.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.AllPairsShortestPaths_Example.cs+From_int"]/*' />
        /// </example>
        /// <param name="start">the start vertex of the path</param>
        /// <returns>The shortest paths from the given state vertex</returns>
        /// <seealso cref="ShortestPaths"/>
        public ShortestPaths From(int start)
        {
            return (start < 0 || start >= shortestPaths.Length) ? EMPTY_SHORTEST_PATHS : shortestPaths[start];
        }

        /// <summary>
        /// Access the shortest paths object for provided start atom.
        /// </summary>
        /// <example>
        /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.AllPairsShortestPaths_Example.cs+From_IAtom"]/*' />
        /// </example>
        /// <param name="start">the start atom of the path</param>
        /// <returns>The shortest paths from the given state vertex</returns>
        /// <seealso cref="ShortestPaths"/>
        public ShortestPaths From(IAtom start)
        {
            // currently container.Atoms.IndexOf() return -1 when null
            return From(container.Atoms.IndexOf(start));
        }

        /// <summary>
        /// an empty atom container so we can handle invalid vertices/atoms better.
        /// Not very pretty but we can't access the domain model from cdk-core.
        /// </summary>
        private static readonly IAtomContainer EMPTY_CONTAINER = new EmptyAtomContainer();

        private class EmptyAtomContainer 
            : IAtomContainer
        {
            public IList<IAtom> Atoms => Array.Empty<IAtom>();
            public IList<IBond> Bonds => Array.Empty<IBond>();
            public IChemObjectBuilder Builder
            { get { throw new InvalidOperationException("not supported"); } }

            public ICollection<IChemObjectListener> Listeners { get; } = Array.Empty<IChemObjectListener>();
            public bool Notification { get { return false; } set { } }
            public void NotifyChanged() { }

            public string Id
            {
                get { throw new InvalidOperationException("not supported"); }
                set { }
            }

            public bool Compare(object obj) => this == obj;

            public bool IsEmpty() => true;

            public bool IsPlaced
            {
                get { throw new InvalidOperationException("not supported"); }
                set { }
            }

            public bool IsVisited
            {
                get { throw new InvalidOperationException("not supported"); }
                set { }
            }

            public bool IsSingleOrDouble
            {
                get { throw new InvalidOperationException("not supported"); }
                set { }
            }

            public bool IsAromatic
            {
                get { throw new InvalidOperationException("not supported"); }
                set { }
            }

            public IList<ILonePair> LonePairs => Array.Empty<ILonePair>();
            public IList<ISingleElectron> SingleElectrons => Array.Empty<ISingleElectron>();

            public void Add(IAtomContainer atomContainer)
            { throw new InvalidOperationException("not supported"); }

            public void Add(IAtom atom)
            { throw new InvalidOperationException("not supported"); }

            public void Add(IElectronContainer electronContainer)
            { throw new InvalidOperationException("not supported"); }

            public object Clone()
            { throw new InvalidOperationException("not supported"); }

            public ICDKObject Clone(CDKObjectMap map)
            { throw new InvalidOperationException("not supported"); }

            public bool Contains(ILonePair lonePair)
                => false;

            public bool Contains(IElectronContainer electronContainer)
                => false;

            public bool Contains(ISingleElectron singleElectron)
                => false;

            public bool Contains(IBond bond)
                => false;

            public bool Contains(IAtom atom)
                => false;

            public IBond GetBond(IAtom atom1, IAtom atom2)
            { throw new InvalidOperationException("not supported"); }

            public double GetBondOrderSum(IAtom atom)
            { throw new InvalidOperationException("not supported"); }

            public IEnumerable<IAtom> GetConnectedAtoms(IAtom atom)
            { throw new InvalidOperationException("not supported"); }

            public IEnumerable<IBond> GetConnectedBonds(IAtom atom)
            { throw new InvalidOperationException("not supported"); }

            public IEnumerable<IElectronContainer> GetConnectedElectronContainers(IAtom atom)
            { throw new InvalidOperationException("not supported"); }

            public IEnumerable<ILonePair> GetConnectedLonePairs(IAtom atom)
            { throw new InvalidOperationException("not supported"); }

            public IEnumerable<ISingleElectron> GetConnectedSingleElectrons(IAtom atom)
            { throw new InvalidOperationException("not supported"); }

            public IEnumerable<IElectronContainer> GetElectronContainers()
            { throw new InvalidOperationException("not supported"); }

            public BondOrder GetMaximumBondOrder(IAtom atom)
            { throw new InvalidOperationException("not supported"); }

            public BondOrder GetMinimumBondOrder(IAtom atom)
            { throw new InvalidOperationException("not supported"); }

            public void SetProperties(IEnumerable<KeyValuePair<object, object>> properties) { throw new InvalidOperationException("not supported"); }
            public void AddProperties(IEnumerable<KeyValuePair<object, object>> properties) { throw new InvalidOperationException("not supported"); }
            public IReadOnlyDictionary<object, object> GetProperties() { throw new InvalidOperationException("not supported"); }
            public T GetProperty<T>(object description, T defautValue) { throw new InvalidOperationException("not supported"); }
            public T GetProperty<T>(object description) => GetProperty(description, default(T));
            public void RemoveProperty(object description) { }
            public void SetProperty(object key, object value) { throw new InvalidOperationException("not supported"); }

            public ICollection<IStereoElement<IChemObject, IChemObject>> StereoElements => Array.Empty<IStereoElement<IChemObject, IChemObject>>();

            public void Remove(IElectronContainer electronContainer)
            { }

            public void RemoveBond(IBond bond)
            { }

            public IBond RemoveBond(IAtom atom0, IAtom atom1)
            {
                return null;
            }

            public void Remove(IAtomContainer atomContainer)
            { }

            public void RemoveAllElectronContainers()
            { }

            public void RemoveAllElements()
            { }

            [Obsolete]
            public void RemoveAtomAndConnectedElectronContainers(IAtom atom)
            { }

            public void RemoveAtom(IAtom atom)
            { }

            public void RemoveAtom(int pos)
            { }

            public void OnStateChanged(ChemObjectChangeEventArgs evt)
            {
                NotifyChanged();
            }

            public void SetAtoms(IEnumerable<IAtom> atoms)
            { throw new InvalidOperationException("not supported"); }

            public void SetBonds(IEnumerable<IBond> bonds)
            { throw new InvalidOperationException("not supported"); }

            public string Title
            {
                get { return null; }
                set { }
            }
        }

        /// <summary>
        /// pseudo shortest-paths - when an invalid atom is given. this will always
        /// return 0 .Length paths and distances.
        /// </summary>
        private static readonly ShortestPaths EMPTY_SHORTEST_PATHS = new ShortestPaths(Array.Empty<int[]>(), EMPTY_CONTAINER, 0);
    }
}
