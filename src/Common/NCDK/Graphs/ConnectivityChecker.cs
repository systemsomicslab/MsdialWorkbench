/* Copyright (C) 1997-2007  The Chemistry Development Kit (CDK) project
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
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Graphs
{
    /// <summary>
    /// Tool class for checking whether the (sub)structure in an <see cref="IAtomContainer"/> is connected.
    /// </summary>
    /// <example>
    /// To check whether an <see cref="IAtomContainer"/> is connected this code
    /// can be used:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ConnectivityChecker_Example.cs+1"]/*' />
    /// A disconnected AtomContainer can be fragmented into connected
    /// fragments by using code like:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.ConnectivityChecker_Example.cs+2"]/*' />
    /// </example>
    // @cdk.module standard
    // @cdk.keyword connectivity
    public static class ConnectivityChecker
    {
        /// <summary>
        /// Check whether a set of atoms in an <see cref="IAtomContainer"/> is connected.
        /// </summary>
        /// <param name="atomContainer">The <see cref="IAtomContainer"/> to be check for connectedness</param>
        /// <returns>true if the <see cref="IAtomContainer"/> is connected</returns>
        public static bool IsConnected(IAtomContainer atomContainer)
        {
            // with one atom or less, we define it to be connected, as there is no
            // partitioning needed
            if (atomContainer.Atoms.Count < 2)
                return true;

            var cc = new ConnectedComponents(GraphUtil.ToAdjList(atomContainer));
            return cc.NumberOfComponents == 1;
        }

        /// <summary>
        /// Partitions the atoms in an <see cref="IAtomContainer"/> into covalently connected components.
        /// </summary>
        /// <param name="container">The <see cref="IAtomContainer"/> to be partitioned into connected components, i.e. molecules</param>
        /// <returns>A MoleculeSet.</returns>
        // @cdk.dictref   blue-obelisk:graphPartitioning
        public static IReadOnlyList<IAtomContainer> PartitionIntoMolecules(IAtomContainer container)
        {
            var cc = new ConnectedComponents(GraphUtil.ToAdjList(container));
            return PartitionIntoMolecules(container, cc.GetComponents());
        }

        public static IReadOnlyList<IAtomContainer> PartitionIntoMolecules(IAtomContainer container, int[] components)
        {
            int maxComponentIndex = 0;
            foreach (int component in components)
                if (component > maxComponentIndex)
                    maxComponentIndex = component;

            var containers = new IAtomContainer[maxComponentIndex + 1];
            var componentsMap = new Dictionary<IAtom, IAtomContainer>(2 * container.Atoms.Count);

            for (int i = 1; i < containers.Length; i++)
                containers[i] = container.Builder.NewAtomContainer();

            for (int i = 0; i < container.Atoms.Count; i++)
            {
                componentsMap.Add(container.Atoms[i], containers[components[i]]);
                containers[components[i]].Atoms.Add(container.Atoms[i]);
            }

            foreach (var bond in container.Bonds)
            {
                var begComp = componentsMap[bond.Begin];
                var endComp = componentsMap[bond.End];
                if (begComp == endComp)
                    begComp.Bonds.Add(bond);
            }

            foreach (var electron in container.SingleElectrons)
                componentsMap[electron.Atom].SingleElectrons.Add(electron);

            foreach (var lonePair in container.LonePairs)
                componentsMap[lonePair.Atom].LonePairs.Add(lonePair);

            foreach (var stereo in container.StereoElements)
            {
                var focus = stereo.Focus;
                switch (focus)
                {
                    case IAtom atom:
                        if (componentsMap.ContainsKey(atom))
                            componentsMap[atom].StereoElements.Add(stereo);
                        break;
                    case IBond bond:
                        if (componentsMap.ContainsKey(bond.Begin))
                            componentsMap[bond.Begin].StereoElements.Add(stereo);
                        break;
                    default:
                        throw new InvalidOperationException("New stereo element not using an atom/bond for focus?");
                }
            }

            // do not return IEnumerable, containers are modified above.
            var containerSet = new List<IAtomContainer>(containers.Skip(1));

            return containerSet;
        }
    }
}
        