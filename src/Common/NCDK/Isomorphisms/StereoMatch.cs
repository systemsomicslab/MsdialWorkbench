/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
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
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using System.Collections.Generic;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// Filters out (sub)graph-isomorphism matches that have invalid stereochemistry
    /// configuration. The class is not currently set up to handle partial mappings
    /// (MCS) but could easily be extended to handle such cases. <p/> The class
    /// implements the Guava predicate and can be used easily filter the mappings.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.StereoMatch_Example.cs"]/*' />
    /// </example>
    // @author John May
    // @cdk.module isomorphism
    internal sealed class StereoMatch 
    {
        /// <summary>Query and target contains.</summary>
        private readonly IAtomContainer query, target;

        /// <summary>Atom to atom index lookup.</summary>
        private readonly Dictionary<IAtom, int> queryMap;

        /// <summary>Atom to atom index lookup.</summary>
        private readonly Dictionary<IAtom, int> targetMap;

        /// <summary>Indexed array of stereo elements.</summary>
        private readonly IStereoElement<IChemObject, IChemObject>[] queryElements, targetElements;

        /// <summary>Indexed array of stereo element types.</summary>
        private readonly Types[] queryTypes, targetTypes;

        /// <summary>Indices of focus atoms of stereo elements.</summary>
        private readonly int[] queryStereoIndices, targetStereoIndices;

        /// <summary>
        /// Create a predicate for checking mappings between a provided
        /// <paramref name="query"/> and <paramref name="target"/>.
        /// </summary>
        /// <param name="query">query container</param>
        /// <param name="target">target container</param>
        internal StereoMatch(IAtomContainer query, IAtomContainer target)
        {
            this.query = query;
            this.target = target;
            this.queryMap = IndexAtoms(query);
            this.targetMap = IndexAtoms(target);
            this.queryElements = new IStereoElement<IChemObject, IChemObject>[query.Atoms.Count];
            this.targetElements = new IStereoElement<IChemObject, IChemObject>[target.Atoms.Count];
            this.queryTypes = new Types[query.Atoms.Count];
            this.targetTypes = new Types[target.Atoms.Count];

            queryStereoIndices = IndexElements(queryMap, queryElements, queryTypes, query);
            targetStereoIndices = IndexElements(targetMap, targetElements, targetTypes, target);
        }

        /// <summary>
        /// Is the <paramref name="mapping"/> of the stereochemistry in the query preserved in
        /// the target.
        /// </summary>
        /// <param name="mapping">permutation of the query vertices</param>
        /// <returns>the stereo chemistry is value</returns>
        public bool Apply(int[] mapping)
        {
            // n.b. not true for unspecified queries e.g. [C@?H](*)(*)*
            if (queryStereoIndices.Length > targetStereoIndices.Length) return false;

            foreach (var u in queryStereoIndices)
            {
                switch (queryTypes[u])
                {
                    case Types.Tetrahedral:
                        if (!CheckTetrahedral(u, mapping)) return false;
                        break;
                    case Types.Geometric:
                        if (!CheckGeometric(u, OtherIndex(u), mapping)) return false;
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Verify the tetrahedral stereochemistry (clockwise/anticlockwise) of atom
        /// <paramref name="u"/> is preserved in the target when the <paramref name="mapping"/> is used.
        /// </summary>
        /// <param name="u">tetrahedral index in the target</param>
        /// <param name="mapping">mapping of vertices</param>
        /// <returns>the tetrahedral configuration is preserved</returns>
        private bool CheckTetrahedral(int u, int[] mapping)
        {
            int v = mapping[u];
            if (targetTypes[v] != Types.Tetrahedral) return false;

            ITetrahedralChirality queryElement = (ITetrahedralChirality)queryElements[u];
            ITetrahedralChirality targetElement = (ITetrahedralChirality)targetElements[v];

            // access neighbors of each element, then map the query to the target
            int[] us = Neighbors(queryElement, queryMap);
            int[] vs = Neighbors(targetElement, targetMap);
            us = Map(u, v, us, mapping);

            if (us == null) return false;

            int p = PermutationParity(us) * Parity(queryElement.Stereo);
            int q = PermutationParity(vs) * Parity(targetElement.Stereo);

            return p == q;
        }

        /// <summary>
        /// Transforms the neighbors <paramref name="us"/> adjacent to <paramref name="u"/> into the target
        /// indices using the mapping <paramref name="mapping"/>. The transformation accounts
        /// for an implicit hydrogen in the query being an explicit hydrogen in the
        /// target.
        /// </summary>
        /// <param name="u">central atom of tetrahedral element</param>
        /// <param name="v">mapped central atom of the tetrahedral element</param>
        /// <param name="us">neighboring vertices of u (u plural)</param>
        /// <param name="mapping">mapping from the query to the target</param>
        /// <returns>the neighbors us, transformed into the neighbors around v</returns>
        private int[] Map(int u, int v, int[] us, int[] mapping)
        {
            // implicit hydrogen in query but explicit in target, modify the mapping
            // such that the central atom, u, mapps to the hydrogen
            if (query.Atoms[u].ImplicitHydrogenCount == 1 && target.Atoms[v].ImplicitHydrogenCount == 0)
            {
                IAtom explicitHydrogen = FindHydrogen(((ITetrahedralChirality)targetElements[v]).Ligands);
                // the substructure had a hydrogen but the superstructure did not
                // the matching is not possible - if we allowed the mapping then
                // we would have different results for implicit/explicit hydrogens
                if (explicitHydrogen == null) return null;
                mapping[u] = targetMap[explicitHydrogen];
            }

            for (int i = 0; i < us.Length; i++)
                us[i] = mapping[us[i]];

            mapping[u] = v; // remove temporary mapping to hydrogen
            return us;
        }

        /// <summary>
        /// Verify the geometric stereochemistry (cis/trans) of the double bond
        /// <c><paramref name="u1"/>=<paramref name="u2"/></c> is preserved in the target when the <paramref name="mapping"/> is
        /// used.
        /// </summary>
        /// <param name="u1">one index of the double bond</param>
        /// <param name="u2">other index of the double bond</param>
        /// <param name="mapping">mapping of vertices</param>
        /// <returns>the geometric configuration is preserved</returns>
        private bool CheckGeometric(int u1, int u2, int[] mapping)
        {
            int v1 = mapping[u1];
            int v2 = mapping[u2];

            // no configuration in target
            if (targetTypes[v1] != Types.Geometric || targetTypes[v2] != Types.Geometric) return false;

            IDoubleBondStereochemistry queryElement = (IDoubleBondStereochemistry)queryElements[u1];
            IDoubleBondStereochemistry targetElement = (IDoubleBondStereochemistry)targetElements[v1];

            // although the atoms were mapped and 'v1' and 'v2' are bond in double-bond
            // elements they are not in the same element
            if (!targetElement.StereoBond.Contains(target.Atoms[v1])
                    || !targetElement.StereoBond.Contains(target.Atoms[v2])) return false;

            // bond is undirected so we need to ensure v1 is the first atom in the bond
            // we also need to to swap the substituents later
            bool swap = false;
            if (targetElement.StereoBond.Begin != target.Atoms[v1])
            {
                int tmp = v1;
                v1 = v2;
                v2 = tmp;
                swap = true;
            }

            var queryBonds = queryElement.Bonds;
            var targetBonds = targetElement.Bonds;

            int p = Parity(queryElement.Stereo);
            int q = Parity(targetElement.Stereo);

            int uLeft = queryMap[queryBonds[0].GetOther(query.Atoms[u1])];
            int uRight = queryMap[queryBonds[1].GetOther(query.Atoms[u2])];

            int vLeft = targetMap[targetBonds[0].GetOther(target.Atoms[v1])];
            int vRight = targetMap[targetBonds[1].GetOther(target.Atoms[v2])];

            if (swap)
            {
                int tmp = vLeft;
                vLeft = vRight;
                vRight = tmp;
            }

            if (mapping[uLeft] != vLeft) p *= -1;
            if (mapping[uRight] != vRight) p *= -1;

            return p == q;
        }

        /// <summary>
        /// Access the neighbors of <paramref name="element"/> as their indices.
        /// </summary>
        /// <param name="element">tetrahedral element</param>
        /// <param name="map">atom index lookup</param>
        /// <returns>the neighbors</returns>
        private static int[] Neighbors(ITetrahedralChirality element, IReadOnlyDictionary<IAtom, int> map)
        {
            var atoms = element.Ligands;
            int[] vs = new int[atoms.Count];
            for (int i = 0; i < atoms.Count; i++)
                vs[i] = map[atoms[i]];
            return vs;
        }

        /// <summary>
        /// Given an array of atoms, find the first hydrogen atom.
        /// </summary>
        /// <param name="atoms">array of non-null atoms.</param>
        /// <returns>a hydrogen atom</returns>
        private static IAtom FindHydrogen(IEnumerable<IAtom> atoms)
        {
            foreach (var a in atoms)
            {
                if (1 == a.AtomicNumber) return a;
            }
            return null;
        }

        /// <summary>
        /// Compute the permutation parity of the values <paramref name="vs"/>. The parity is
        /// whether we need to do an odd or even number of swaps to put the values in
        /// sorted order.
        /// </summary>
        /// <param name="vs">values</param>
        /// <returns>parity of the permutation (odd = -1, even = +1)</returns>
        private static int PermutationParity(int[] vs)
        {
            int n = 0;
            for (int i = 0; i < vs.Length; i++)
                for (int j = i + 1; j < vs.Length; j++)
                    if (vs[i] > vs[j]) n++;
            return (n & 0x1) == 1 ? -1 : 1;
        }

        /// <summary>
        /// Given an index of an atom in the query get the index of the other atom in
        /// the double bond.
        /// </summary>
        /// <param name="i">query atom index</param>
        /// <returns>the other atom index involved in a double bond</returns>
        private int OtherIndex(int i)
        {
            IDoubleBondStereochemistry element = (IDoubleBondStereochemistry)queryElements[i];
            return queryMap[element.StereoBond.GetOther(query.Atoms[i])];
        }

        /// <summary>
        /// Create an index of atoms for the provided <paramref name="container"/>.
        /// </summary>
        /// <param name="container">the container to index the atoms of</param>
        /// <returns>the index/lookup of atoms to the index they appear</returns>
        private static Dictionary<IAtom, int> IndexAtoms(IAtomContainer container)
        {
            var map = new Dictionary<IAtom, int>(container.Atoms.Count);
            for (int i = 0; i < container.Atoms.Count; i++)
                map.Add(container.Atoms[i], i);
            return map;
        }

        /// <summary>
        /// Index the stereo elements of the <paramref name="container"/> into the the 
        /// <paramref name="elements"/> and <paramref name="types"/> arrays. The <paramref name="map"/> is used for looking
        /// up the index of atoms.
        /// </summary>
        /// <param name="map">index of atoms</param>
        /// <param name="elements">array to fill with stereo elements</param>
        /// <param name="types">type of stereo element indexed</param>
        /// <param name="container">the container to index the elements of</param>
        /// <returns>indices of atoms involved in stereo configurations</returns>
        private static int[] IndexElements(Dictionary<IAtom, int> map, IStereoElement<IChemObject, IChemObject>[] elements, Types[] types, IAtomContainer container)
        {
            int[] indices = new int[container.Atoms.Count];
            int nElements = 0;
            foreach (var element in container.StereoElements)
            {
                if (element is ITetrahedralChirality tc)
                {
                    int idx = map[tc.ChiralAtom];
                    elements[idx] = element;
                    types[idx] = Types.Tetrahedral;
                    indices[nElements++] = idx;
                }
                else if (element is IDoubleBondStereochemistry dbs)
                {
                    int idx1 = map[dbs.StereoBond.Begin];
                    int idx2 = map[dbs.StereoBond.End];
                    elements[idx2] = elements[idx1] = element;
                    types[idx1] = types[idx2] = Types.Geometric;
                    indices[nElements++] = idx1; // only visit the first atom
                }
            }
            return Arrays.CopyOf(indices, nElements);
        }

        /// <summary>
        /// Get the parity (-1,+1) of the tetrahedral configuration.
        /// </summary>
        /// <param name="stereo">configuration</param>
        /// <returns>the parity</returns>
        private static int Parity(TetrahedralStereo stereo)
        {
            return stereo == TetrahedralStereo.Clockwise ? 1 : -1;
        }

        /// <summary>
        /// Get the parity (-1,+1) of the geometric (double bond) configuration.
        /// </summary>
        /// <param name="conformation">configuration</param>
        /// <returns>the parity</returns>
        private static int Parity(DoubleBondConformation conformation)
        {
            return conformation == DoubleBondConformation.Together ? 1 : -1;
        }

        // could be moved into the StereoElement to allow faster introspection
        private enum Types
        {
            Tetrahedral, Geometric
        }
    }
}

