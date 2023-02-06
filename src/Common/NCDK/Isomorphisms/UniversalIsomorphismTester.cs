/* Copyright (C) 2002-2007  Stephane Werner <mail@ixelis.net>
 *
 *  This code has been kindly provided by Stephane Werner
 *  and Thierry Hanser from IXELIS mail@ixelis.net
 *
 *  IXELIS sarl - Semantic Information Systems
 *  17 rue des C???res 67200 Strasbourg, France
 *  Tel/Fax : +33(0)3 88 27 81 39 Email: mail@ixelis.net
 *
 *  CDK Contact: cdk-devel@lists.sf.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Isomorphisms.Matchers;
using NCDK.Isomorphisms.MCSS;
using NCDK.Tools.Manipulator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Isomorphisms
{
    /// <summary>
    /// This class implements a multipurpose structure comparison tool.
    /// It allows to find maximal common substructure, find the
    /// mapping of a substructure in another structure, and the mapping of
    /// two isomorphic structures.
    /// </summary>
    /// <remarks>
    /// Structure comparison may be associated to bond constraints
    /// (mandatory bonds, e.g. scaffolds, reaction cores,...) on each source graph.
    /// The constraint flexibility allows a number of interesting queries.
    /// The substructure analysis relies on the RGraph generic class (see: RGraph)
    /// This class implements the link between the RGraph model and the
    /// the CDK model in this way the <see cref="RGraph"/> remains independent and may be used
    /// in other contexts.
    /// <para>
    /// This algorithm derives from the algorithm described in
    /// <token>cdk-cite-HAN90</token> and modified in the thesis of T. Hanser <token>cdk-cite-HAN93</token>.
    /// </para>
    /// <note type="warning">
    /// As a result of the adjacency perception used in this algorithm
    /// there is a single limitation: cyclopropane and isobutane are seen as isomorph.
    /// This is due to the fact that these two compounds are the only ones where
    /// each bond is connected two each other bond (bonds are fully connected)
    /// with the same number of bonds and still they have different structures
    /// The algorithm could be easily enhanced with a simple atom mapping manager
    /// to provide an atom level overlap definition that would reveal this case.
    /// We decided not to penalize the whole procedure because of one single
    /// exception query. Furthermore isomorphism may be discarded since the number of atoms are
    /// not the same (3 != 4) and in most case this will be already
    /// screened out by a fingerprint based filtering.
    /// It is possible to add a special treatment for this special query.
    /// Be reminded that this algorithm matches bonds only.
    /// </note>
    /// <para>
    /// <note type="note">
    /// While most isomorphism queries involve a multi-atom query structure
    /// there may be cases in which the query atom is a single atom. In such a case
    /// a mapping of target bonds to query bonds is not feasible. In such a case, the RMap objects
    /// correspond to atom indices rather than bond indices. In general, this will not affect user
    /// code and the same sequence of method calls for matching multi-atom query structures will
    /// work for single atom query structures as well.
    /// </note>
    /// </para>
    /// </remarks>
    /// <example>
    /// With the <see cref="IsSubgraph(IAtomContainer, IAtomContainer)"/> method,
    /// the second, and only the second argument <b>may</b> be a <see cref="IQueryAtomContainer"/>,
    /// which allows one to do SMARTS or MQL like queries.
    /// The first <see cref="IAtomContainer"/> must never be an <see cref="IQueryAtomContainer"/>.
    /// An example:
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Isomorphisms.UniversalIsomorphismTester_Example.cs"]/*' />
    /// </example>
    // @author      Stephane Werner from IXELIS mail@ixelis.net
    // @cdk.created 2002-07-17
    // @cdk.module  standard
    [Obsolete("Use the Pattern APIs from the " + nameof(Isomorphisms) + " namespace")]
    public class UniversalIsomorphismTester
    {
        private enum IdType
        {
            Id1 = 0,
            Id2 = 1,
        };

        /// <summary>
        /// Sets the time in milliseconds until the substructure search will be breaked.
        /// </summary>
        public long Timeout { get; set; } = -1;

        public UniversalIsomorphismTester()
        {
        }

        ///////////////////////////////////////////////////////////////////////////
        //                            Query Methods
        //
        // This methods are simple applications of the RGraph model on atom containers
        // using different constrains and search options. They give an example of the
        // most common queries but of course it is possible to define other type of
        // queries exploiting the constrain and option combinations
        //

        ////
        // Isomorphism search

        /// <summary>
        /// Tests if g1 and g2 are isomorph.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>true if the 2 molecule are isomorph</returns>
        /// <exception cref="CDKException">if the first molecule is an instance of IQueryAtomContainer</exception>
        public bool IsIsomorph(IAtomContainer g1, IAtomContainer g2)
        {
            if (g1 is IQueryAtomContainer)
                throw new CDKException("The first IAtomContainer must not be an IQueryAtomContainer");

            if (g2.Atoms.Count != g1.Atoms.Count)
                return false;
            // check single atom case
            if (g2.Atoms.Count == 1)
            {
                var atom = g1.Atoms[0];
                var atom2 = g2.Atoms[0];
                if (atom is IQueryAtom qAtom)
                    return qAtom.Matches(g2.Atoms[0]);
                else if (atom2 is IQueryAtom qAtom2)
                    return qAtom2.Matches(g1.Atoms[0]);
                else
                {
                    var atomSymbol = atom2.Symbol;
                    return g1.Atoms[0].Symbol.Equals(atomSymbol, StringComparison.Ordinal);
                }
            }
            return (GetIsomorphMap(g1, g2) != null);
        }

        /// <summary>
        /// Returns the first isomorph mapping found or <see langword="null"/>.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>
        /// the first isomorph mapping found projected of <paramref name="g1"/>. 
        /// This is a enumerable of <see cref="RMap"/> objects containing Ids of matching bonds.
        /// </returns>
        public IReadOnlyList<RMap> GetIsomorphMap(IAtomContainer g1, IAtomContainer g2)
        {
            if (g1 is IQueryAtomContainer)
                throw new CDKException("The first IAtomContainer must not be an IQueryAtomContainer");

            var rMapsList = Search(g1, g2, GetBitSet(g1), GetBitSet(g2), false, false);
            return rMapsList.FirstOrDefault();
        }

        /// <summary>
        /// Returns the first isomorph 'atom mapping' found for <paramref name="g2"/> in <paramref name="g1"/>.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>the first isomorph atom mapping found projected on g1. This is a List of RMap objects containing Ids of matching atoms.</returns>
        /// <exception cref="CDKException">if the first molecules is not an instance of <see cref="IQueryAtomContainer"/></exception>
        public IReadOnlyList<RMap> GetIsomorphAtomsMap(IAtomContainer g1, IAtomContainer g2)
        {
            if (g1 is IQueryAtomContainer)
                throw new CDKException("The first IAtomContainer must not be an IQueryAtomContainer");

            var list = CheckSingleAtomCases(g1, g2);
            if (list == null)
            {
                return MakeAtomsMapOfBondsMap(GetIsomorphMap(g1, g2), g1, g2);
            }
            else if (list.Count == 0)
            {
                return null;
            }
            else
            {
                return list;
            }
        }

        /// <summary>
        /// Returns all the isomorph 'mappings' found between two
        /// atom containers.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>the enumerable of all the 'mappings'</returns>
        public IEnumerable<IReadOnlyList<RMap>> GetIsomorphMaps(IAtomContainer g1, IAtomContainer g2)
        {
            return Search(g1, g2, GetBitSet(g1), GetBitSet(g2), true, true);
        }

        /////
        // Subgraph search

        /// <summary>
        /// Returns all the subgraph 'bond mappings' found for g2 in g1.
        /// This is an <see cref="IList{T}"/> of <see cref="IList{T}"/>s of <see cref="RMap"/> objects.
        /// </summary>
        /// <remarks>
        /// Note that if the query molecule is a single atom, then bond mappings
        /// cannot be defined. In such a case, the <see cref="RMap"/> object refers directly to
        /// atom - atom mappings. Thus RMap.id1 is the index of the target atom
        /// and RMap.id2 is the index of the matching query atom (in this case,
        /// it will always be 0). Note that in such a case, there is no need
        /// to call <see cref="MakeAtomsMapOfBondsMap(IReadOnlyList{RMap}, IAtomContainer, IAtomContainer)"/> ,
        /// though if it is called, then the
        /// return value is simply the same as the return value of this method.
        /// </remarks>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>the list of all the 'mappings' found projected of g1</returns>
        /// <seealso cref="MakeAtomsMapsOfBondsMaps(IEnumerable{IReadOnlyList{RMap}}, IAtomContainer, IAtomContainer)"/>
        public IEnumerable<IReadOnlyList<RMap>> GetSubgraphMaps(IAtomContainer g1, IAtomContainer g2)
        {
            return Search(g1, g2, new BitArray(g1.Bonds.Count), GetBitSet(g2), true, true);
        }

        /// <summary>
        /// Returns the first subgraph 'bond mapping' found for g2 in g1.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>the first subgraph bond mapping found projected on g1. This is a <see cref="IReadOnlyList{T}"/> of <see cref="RMap"/> objects containing Ids of matching bonds.</returns>
        public IReadOnlyList<RMap> GetSubgraphMap(IAtomContainer g1, IAtomContainer g2)
        {
            var rMapsList = Search(g1, g2, new BitArray(g1.Bonds.Count), GetBitSet(g2), false, false);
            return rMapsList.FirstOrDefault();
        }

        /// <summary>
        /// Returns all subgraph 'atom mappings' found for g2 in g1, where g2 must be a substructure
        /// of g1. If it is not a substructure, null will be returned.
        /// This is an <see cref="IList{T}"/> of <see cref="IList{T}"/>s of <see cref="RMap"/> objects.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">substructure to be mapped. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>all subgraph atom mappings found projected on g1. This is a <see cref="IList{T}"/> of <see cref="RMap"/> objects containing Ids of matching atoms.</returns>
        public IEnumerable<IReadOnlyList<RMap>> GetSubgraphAtomsMaps(IAtomContainer g1, IAtomContainer g2)
        {
            var list = CheckSingleAtomCases(g1, g2);
            if (list == null)
            {
                return MakeAtomsMapsOfBondsMaps(GetSubgraphMaps(g1, g2), g1, g2);
            }
            else
            {
                var atomsMap = new[] { list };
                return atomsMap;
            }
        }

        /// <summary>
        /// Returns the first subgraph 'atom mapping' found for g2 in g1, where g2 must be a substructure
        /// of g1. If it is not a substructure, null will be returned.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">substructure to be mapped. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>the first subgraph atom mapping found projected on g1. This is a <see cref="IList{T}"/> of <see cref="RMap"/> objects containing Ids of matching atoms.</returns>
        public IReadOnlyList<RMap> GetSubgraphAtomsMap(IAtomContainer g1, IAtomContainer g2)
        {
            var list = CheckSingleAtomCases(g1, g2);
            if (list == null)
            {
                return MakeAtomsMapOfBondsMap(GetSubgraphMap(g1, g2), g1, g2);
            }
            else if (list.Count == 0)
            {
                return null;
            }
            else
            {
                return list;
            }
        }

        /// <summary>
        /// Tests if g2 a subgraph of g1.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>true if g2 a subgraph on g1</returns>
        public bool IsSubgraph(IAtomContainer g1, IAtomContainer g2)
        {
            if (g1 is IQueryAtomContainer)
                throw new CDKException("The first IAtomContainer must not be an IQueryAtomContainer");

            if (g2.Atoms.Count > g1.Atoms.Count)
                return false;

            // test for single atom case
            if (g2.Atoms.Count == 1)
            {
                IAtom atom = g2.Atoms[0];
                for (int i = 0; i < g1.Atoms.Count; i++)
                {
                    IAtom atom2 = g1.Atoms[i];
                    if (atom is IQueryAtom qAtom)
                    {
                        if (qAtom.Matches(atom2))
                            return true;
                    }
                    else if (atom2 is IQueryAtom qAtom2)
                    {
                        if (qAtom2.Matches(atom))
                            return true;
                    }
                    else
                    {
                        if (atom2.Symbol.Equals(atom.Symbol, StringComparison.Ordinal))
                            return true;
                    }
                }
                return false;
            }
            if (!TestSubgraphHeuristics(g1, g2))
                return false;
            return (GetSubgraphMap(g1, g2) != null);
        }

        ////
        // Maximum common substructure search

        /// <summary>
        /// Returns all the maximal common substructure between two atom containers.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>the list of all the maximal common substructure found projected of g1 (list of <see cref="IAtomContainer"/>)</returns>
        public IReadOnlyList<IAtomContainer> GetOverlaps(IAtomContainer g1, IAtomContainer g2)
        {
            var rMapsList = Search(g1, g2, new BitArray(g1.Bonds.Count), new BitArray(g2.Bonds.Count), true, false);

            // projection on G1
            var graphList = ProjectList(rMapsList, g1, IdType.Id1);

            // reduction of set of solution (isomorphism and substructure
            // with different 'mappings'

            return GetMaximum(graphList);
        }

        /// <summary>
        /// Transforms an AtomContainer into a <see cref="BitArray"/> (which's size = number of bond
        /// in the atomContainer, all the bit are set to true).
        /// </summary>
        /// <param name="ac"><see cref="IAtomContainer"/> to transform</param>
        /// <returns>The bitSet</returns>
        public static BitArray GetBitSet(IAtomContainer ac)
        {
            BitArray bs;
            int n = ac.Bonds.Count;

            if (n != 0)
            {
                bs = new BitArray(n).Not(); // set all bit true
            }
            else
            {
                bs = new BitArray(0);
            }

            return bs;
        }

        //////////////////////////////////////////////////
        //          Internal methods

        /// <summary>
        /// Builds the <see cref="RGraph"/> ( resolution graph ), from two atomContainer
        /// (description of the two molecules to compare)
        /// This is the interface point between the CDK model and
        /// the generic MCSS algorithm based on the RGRaph.
        /// </summary>
        /// <param name="g1">Description of the first molecule</param>
        /// <param name="g2">Description of the second molecule</param>
        /// <returns>the rGraph</returns>
        public static RGraph BuildRGraph(IAtomContainer g1, IAtomContainer g2)
        {
            RGraph rGraph = new RGraph();
            NodeConstructor(rGraph, g1, g2);
            ArcConstructor(rGraph, g1, g2);
            return rGraph;
        }

        /// <summary>
        /// General <see cref="RGraph"/> parsing method (usually not used directly)
        /// This method is the entry point for the recursive search
        /// adapted to the atom container input.
        /// </summary>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="c1">initial condition ( bonds from g1 that must be contains in the solution )</param>
        /// <param name="c2">initial condition ( bonds from g2 that must be contains in the solution )</param>
        /// <param name="findAllStructure">if false stop at the first structure found</param>
        /// <param name="findAllMap">if true search all the 'mappings' for one same structure</param>
        /// <returns>a List of Lists of <see cref="RMap"/> objects that represent the search solutions</returns>
        public IEnumerable<IReadOnlyList<RMap>> Search(IAtomContainer g1, IAtomContainer g2, BitArray c1, BitArray c2, bool findAllStructure, bool findAllMap)
        {
            // remember start time
            var start = DateTime.Now.Ticks / 10000;

            // handle single query atom case separately
            if (g2.Atoms.Count == 1)
            {
                var queryAtom = g2.Atoms[0];

                // we can have a IQueryAtomContainer *or* an IAtomContainer
                if (queryAtom is IQueryAtom qAtom)
                {
                    foreach (var atom in g1.Atoms)
                    {
                        if (qAtom.Matches(atom))
                        {
                            var lmap = new[] { new RMap(g1.Atoms.IndexOf(atom), 0) };
                            yield return lmap;
                        }
                    }
                }
                else
                {
                    foreach (var atom in g1.Atoms)
                    {
                        if (queryAtom.Symbol.Equals(atom.Symbol, StringComparison.Ordinal))
                        {
                            var lmap = new[] { new RMap(g1.Atoms.IndexOf(atom), 0) };
                            yield return lmap;
                        }
                    }
                }
            }
            else
            {
                // build the RGraph corresponding to this problem
                var rGraph = BuildRGraph(g1, g2);
                // Set time data
                rGraph.Timeout = Timeout;
                rGraph.Start = start;
                // parse the RGraph with the given constrains and options
                rGraph.Parse(c1, c2, findAllStructure, findAllMap);
                var solutionList = rGraph.Solutions;

                // conversions of RGraph's internal solutions to G1/G2 mappings
                foreach (var set in solutionList)
                {
                    var rmap = rGraph.BitSetToRMap(set);
                    if (CheckQueryAtoms(rmap, g1, g2))
                        yield return rmap;
                }
            }
            yield break;
        }

        /// <summary>
        /// Checks that <see cref="IQueryAtom"/>'s correctly match consistently.
        /// </summary>
        /// <param name="bondmap">bond mapping</param>
        /// <param name="g1">target graph</param>
        /// <param name="g2">query graph</param>
        /// <returns>the atom matches are consistent</returns>
        private static bool CheckQueryAtoms(IReadOnlyList<RMap> bondmap, IAtomContainer g1, IAtomContainer g2)
        {
            if (!(g2 is IQueryAtomContainer))
                return true;
            var atommap = MakeAtomsMapOfBondsMap(bondmap, g1, g2);
            foreach (var rmap in atommap)
            {
                var a1 = g1.Atoms[rmap.Id1];
                var a2 = g2.Atoms[rmap.Id2];
                if (a2 is IQueryAtom qa)
                {
                    if (!qa.Matches(a1))
                        return false;
                }
            }
            return true;
        }

        //////////////////////////////////////
        //    Manipulation tools

        /// <summary>
        /// Projects a list of <see cref="RMap"/> on a molecule.
        /// </summary>
        /// <param name="rMapList">the list to project</param>
        /// <param name="g">the molecule on which project</param>
        /// <param name="id">the id in the <see cref="RMap"/> of the molecule <paramref name="g"/></param>
        /// <returns>an AtomContainer</returns>
        private static IAtomContainer Project(IReadOnlyList<RMap> rMapList, IAtomContainer g, IdType id)
        {
            var ac = g.Builder.NewAtomContainer();

            var table = new Dictionary<IAtom, IAtom>();
            IAtom a;
            IBond bond;

            foreach (var rMap in rMapList)
            {
                if (id == IdType.Id1)
                {
                    bond = g.Bonds[rMap.Id1];
                }
                else
                {
                    bond = g.Bonds[rMap.Id2];
                }

                a = bond.Begin;
                if (!table.TryGetValue(a, out IAtom a1))
                {
                    a1 = (IAtom)a.Clone();
                    ac.Atoms.Add(a1);
                    table.Add(a, a1);
                }

                a = bond.End;
                if (!table.TryGetValue(a, out IAtom a2))
                {
                    a2 = (IAtom)a.Clone();
                    ac.Atoms.Add(a2);
                    table.Add(a, a2);
                }
                var newBond = g.Builder.NewBond(a1, a2, bond.Order);
                newBond.IsAromatic = bond.IsAromatic;
                ac.Bonds.Add(newBond);
            }
            return ac;
        }

        /// <summary>
        /// Projects a list of RMapsList on a molecule.
        /// </summary>
        /// <param name="rMapsList">lists of <see cref="RMap"/> to project</param>
        /// <param name="g">the molecule on which project</param>
        /// <param name="id">the id in the <see cref="RMap"/> of the molecule <paramref name="g"/></param>
        /// <returns><see cref="IAtomContainer"/>s</returns>
        private static IReadOnlyList<IAtomContainer> ProjectList(IEnumerable<IReadOnlyList<RMap>> rMapsList, IAtomContainer g, IdType id)
        {
            var graphList = new List<IAtomContainer>();

            foreach (var rMapList in rMapsList)
            {
                var ac = Project(rMapList, g, id);
                graphList.Add(ac);
            }
            return graphList;
        }

        /// <summary>
        /// Removes all redundant solution.
        /// </summary>
        /// <param name="graphList">the list of structure to clean</param>
        /// <returns>the list cleaned</returns>
        /// <exception cref="CDKException">if there is a problem in obtaining subgraphs</exception>
        private List<IAtomContainer> GetMaximum(IReadOnlyList<IAtomContainer> graphList)
        {
            var reducedGraphList = new List<IAtomContainer>();
            reducedGraphList.AddRange(graphList);

            for (int i = 0; i < graphList.Count; i++)
            {
                var gi = graphList[i];

                for (int j = i + 1; j < graphList.Count; j++)
                {
                    var gj = graphList[j];

                    // Gi included in Gj or Gj included in Gi then
                    // reduce the irrelevant solution
                    if (IsSubgraph(gj, gi))
                    {
                        reducedGraphList.Remove(gi);
                    }
                    else if (IsSubgraph(gi, gj))
                    {
                        reducedGraphList.Remove(gj);
                    }
                }
            }
            return reducedGraphList;
        }

        /// <summary>
        ///  Checks for single atom cases before doing subgraph/isomorphism search.
        /// </summary>
        /// <param name="g1">AtomContainer to match on. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">AtomContainer as query. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns><see cref="IList{T}"/> of <see cref="IList{T}"/> of <see cref="RMap"/> objects for the Atoms (not Bonds!), null if no single atom case</returns>
        /// <exception cref="CDKException">if the first molecule is an instance of IQueryAtomContainer</exception>
        public static IReadOnlyList<RMap> CheckSingleAtomCases(IAtomContainer g1, IAtomContainer g2)
        {
            if (g1 is IQueryAtomContainer)
                throw new CDKException("The first IAtomContainer must not be an IQueryAtomContainer");

            if (g2.Atoms.Count == 1)
            {
                var arrayList = new List<RMap>();
                var atom = g2.Atoms[0];
                if (atom is IQueryAtom qAtom)
                {
                    for (int i = 0; i < g1.Atoms.Count; i++)
                    {
                        if (qAtom.Matches(g1.Atoms[i]))
                            arrayList.Add(new RMap(i, 0));
                    }
                }
                else
                {
                    var atomSymbol = atom.Symbol;
                    for (int i = 0; i < g1.Atoms.Count; i++)
                    {
                        if (g1.Atoms[i].Symbol.Equals(atomSymbol, StringComparison.Ordinal))
                            arrayList.Add(new RMap(i, 0));
                    }
                }
                return arrayList;
            }
            else if (g1.Atoms.Count == 1)
            {
                var arrayList = new List<RMap>();
                var atom = g1.Atoms[0];
                for (int i = 0; i < g2.Atoms.Count; i++)
                {
                    var atom2 = g2.Atoms[i];
                    if (atom2 is IQueryAtom qAtom)
                    {
                        if (qAtom.Matches(atom))
                        {
                            arrayList.Add(new RMap(0, i));
                        }
                    }
                    else
                    {
                        if (atom2.Symbol.Equals(atom.Symbol, StringComparison.Ordinal))
                            arrayList.Add(new RMap(0, i));
                    }
                }
                return arrayList;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        ///  This makes maps of matching atoms out of a maps of matching bonds as produced by the
        ///  Get(Subgraph|Ismorphism)Maps methods.
        /// </summary>
        /// <param name="l">The list produced by <see cref="GetSubgraphMaps"/> method.</param>
        /// <param name="g1">The first atom container. Must not be a <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">The second one (first and second as in getMap). May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>A List of <see cref="IList{T}"/>s of <see cref="RMap"/> objects of matching Atoms.</returns>
        public static IEnumerable<IReadOnlyList<RMap>> MakeAtomsMapsOfBondsMaps(IEnumerable<IReadOnlyList<RMap>> l, IAtomContainer g1, IAtomContainer g2)
        {
            foreach (var l2 in l)
            {
                if (g2.Atoms.Count == 1)
                    yield return l2; // since the RMap is already an atom-atom mapping
                else
                    yield return MakeAtomsMapOfBondsMap(l2, g1, g2);
            }
            yield break;
        }

        /// <summary>
        /// This makes a map of matching atoms out of a map of matching bonds as produced by the
        /// <see cref="GetSubgraphMap(IAtomContainer, IAtomContainer)"/>/<see cref="GetIsomorphMap(IAtomContainer, IAtomContainer)"/> methods.
        /// </summary>
        /// <param name="l">The list produced by the getMap method.</param>
        /// <param name="g1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="g2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>The mapping found projected on g1. This is a <see cref="List{T}"/> of <see cref="RMap"/> objects containing Ids of matching atoms.</returns>
        public static IReadOnlyList<RMap> MakeAtomsMapOfBondsMap(IReadOnlyList<RMap> l, IAtomContainer g1, IAtomContainer g2)
        {
            if (l == null)
                return l;
            var result = new List<RMap>();
            for (int i = 0; i < l.Count; i++)
            {
                var bond1 = g1.Bonds[l[i].Id1];
                var bond2 = g2.Bonds[l[i].Id2];
                var atom1 = BondManipulator.GetAtomArray(bond1);
                var atom2 = BondManipulator.GetAtomArray(bond2);
                for (int j = 0; j < 2; j++)
                {
                    var bondsConnectedToAtom1j = g1.GetConnectedBonds(atom1[j]);
                    foreach (var kbond in bondsConnectedToAtom1j)
                    {
                        if (kbond != bond1)
                        {
                            var testBond = kbond;
                            for (int m = 0; m < l.Count; m++)
                            {
                                if ((l[m]).Id1 == g1.Bonds.IndexOf(testBond))
                                {
                                    var testBond2 = g2.Bonds[(l[m]).Id2];
                                    for (int n = 0; n < 2; n++)
                                    {
                                        var bondsToTest = g2.GetConnectedBonds(atom2[n]);
                                        if (bondsToTest.Contains(testBond2))
                                        {
                                            var map = j == n 
                                                ? new RMap(g1.Atoms.IndexOf(atom1[0]), g2.Atoms.IndexOf(atom2[0]))
                                                : new RMap(g1.Atoms.IndexOf(atom1[1]), g2.Atoms.IndexOf(atom2[0]));
                                            if (!result.Contains(map))
                                            {
                                                result.Add(map);
                                            }
                                            var map2 = j == n
                                                ? new RMap(g1.Atoms.IndexOf(atom1[1]), g2.Atoms.IndexOf(atom2[1]))
                                                : new RMap(g1.Atoms.IndexOf(atom1[0]), g2.Atoms.IndexOf(atom2[1]));
                                            if (!result.Contains(map2))
                                            {
                                                result.Add(map2);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Builds the nodes of the <see cref="RGraph"/> ( resolution graph ), from
        /// two atom containers (description of the two molecules to compare)
        /// </summary>
        /// <param name="gr">the target RGraph</param>
        /// <param name="ac1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="ac2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <exception cref="CDKException">if it takes too long to identify overlaps</exception>
        private static void NodeConstructor(RGraph gr, IAtomContainer ac1, IAtomContainer ac2)
        {
            if (ac1 is IQueryAtomContainer)
                throw new CDKException("The first IAtomContainer must not be an IQueryAtomContainer");

            // resets the target graph.
            gr.Clear();

            // compares each bond of G1 to each bond of G2
            for (int i = 0; i < ac1.Bonds.Count; i++)
            {
                for (int j = 0; j < ac2.Bonds.Count; j++)
                {
                    var bondA2 = ac2.Bonds[j];
                    if (bondA2 is IQueryBond queryBond)
                    {
                        var atom1 = (IQueryAtom)(bondA2.Begin);
                        var atom2 = (IQueryAtom)(bondA2.End);
                        var bond = ac1.Bonds[i];
                        if (queryBond.Matches(bond))
                        {
                            var bondAtom0 = bond.Begin;
                            var bondAtom1 = bond.End;
                            // ok, bonds match
                            if (atom1.Matches(bondAtom0) && atom2.Matches(bondAtom1)
                             || atom1.Matches(bondAtom1) && atom2.Matches(bondAtom0))
                            {
                                // ok, atoms match in either order
                                gr.AddNode(new RNode(i, j));
                            }
                        }
                    }
                    else
                    {
                        // if both bonds are compatible then create an association node
                        // in the resolution graph
                        var ac1Bondi = ac1.Bonds[i];
                        var ac2Bondj = ac2.Bonds[j];
                        // bond type conditions
                        if (( // same bond order and same aromaticity flag (either both on or off)
                                ac1Bondi.Order == ac2Bondj.Order && ac1Bondi.IsAromatic == ac2Bondj.IsAromatic) 
                         || ( // both bond are aromatic
                                ac1Bondi.IsAromatic && ac2Bondj.IsAromatic))
                        {
                            var ac1Bondi0 = ac1Bondi.Begin;
                            var ac1Bondi1 = ac1Bondi.End;
                            var ac2Bondj0 = ac2Bondj.Begin;
                            var ac2Bondj1 = ac2Bondj.End;
                            // atom type conditions
                            if (
                                // a1 = a2 && b1 = b2
                                (ac1Bondi0.Symbol.Equals(ac2Bondj0.Symbol, StringComparison.Ordinal) && ac1Bondi1.Symbol.Equals(ac2Bondj1.Symbol, StringComparison.Ordinal)) 
                                // a1 = b2 && b1 = a2
                             || (ac1Bondi0.Symbol.Equals(ac2Bondj1.Symbol, StringComparison.Ordinal) && ac1Bondi1.Symbol.Equals(ac2Bondj0.Symbol, StringComparison.Ordinal)))
                            {
                                gr.AddNode(new RNode(i, j));
                            }
                        }
                    }
                }
            }
            foreach (var node in gr.Graph)
                node.EnsureNodeCount(gr.Graph.Count);
        }

        /// <summary>
        /// Build edges of the <see cref="RGraph"/>s.
        /// This method create the edge of the RGraph and
        /// calculates the incompatibility and neighborhood
        /// relationships between RGraph nodes.
        /// </summary>
        /// <param name="gr">the rGraph</param>
        /// <param name="ac1">first molecule. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="ac2">second molecule. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <exception cref="CDKException">if it takes too long to identify overlaps</exception>
        private static void ArcConstructor(RGraph gr, IAtomContainer ac1, IAtomContainer ac2)
        {
            // each node is incompatible with himself
            for (int i = 0; i < gr.Graph.Count; i++)
            {
                var x = gr.Graph[i];
                x.Forbidden.Set(i, true);
            }

            gr.FirstGraphSize = ac1.Bonds.Count;
            gr.SecondGraphSize = ac2.Bonds.Count;

            for (int i = 0; i < gr.Graph.Count; i++)
            {
                var x = gr.Graph[i];

                // two nodes are neighbors if their adjacency
                // relationship in are equivalent in G1 and G2
                // else they are incompatible.
                for (int j = i + 1; j < gr.Graph.Count; j++)
                {
                    var y = gr.Graph[j];

                    var a1 = ac1.Bonds[x.RMap.Id1];
                    var a2 = ac2.Bonds[x.RMap.Id2];

                    var b1 = ac1.Bonds[y.RMap.Id1];
                    var b2 = ac2.Bonds[y.RMap.Id2];

                    if (a2 is IQueryBond)
                    {
                        if (a1.Equals(b1) || a2.Equals(b2)
                         || !QueryAdjacencyAndOrder(a1, b1, a2, b2))
                        {
                            x.Forbidden.Set(j, true);
                            y.Forbidden.Set(i, true);
                        }
                        else if (HasCommonAtom(a1, b1))
                        {
                            x.Extension.Set(j, true);
                            y.Extension.Set(i, true);
                        }
                    }
                    else
                    {
                        if (a1.Equals(b1) || a2.Equals(b2) 
                         || (!GetCommonSymbol(a1, b1).Equals(GetCommonSymbol(a2, b2), StringComparison.Ordinal)))
                        {
                            x.Forbidden.Set(j, true);
                            y.Forbidden.Set(i, true);
                        }
                        else if (HasCommonAtom(a1, b1))
                        {
                            x.Extension.Set(j, true);
                            y.Extension.Set(i, true);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if two bonds have at least one atom in common.
        /// </summary>
        /// <param name="a">first bond</param>
        /// <param name="b">second bond</param>
        /// <returns> the symbol of the common atom or "" if the 2 bonds have no common atom</returns>
        private static bool HasCommonAtom(IBond a, IBond b)
        {
            return a.Contains(b.Begin) || a.Contains(b.End);
        }

        /// <summary>
        /// Determines if 2 bond have 1 atom in common and returns the common symbol.
        /// </summary>
        /// <param name="a">first bond</param>
        /// <param name="b">second bond</param>
        /// <returns>the symbol of the common atom or "" if the 2 bonds have no common atom</returns>
        private static string GetCommonSymbol(IBond a, IBond b)
        {
            string symbol = "";

            if (a.Contains(b.Begin))
            {
                symbol = b.Begin.Symbol;
            }
            else if (a.Contains(b.End))
            {
                symbol = b.End.Symbol;
            }

            return symbol;
        }

        /// <summary>
        /// Determines if 2 bond have 1 atom in common if second is a query AtomContainer.
        /// </summary>
        /// <param name="a1">first bond</param>
        /// <param name="b1">second bond</param>
        /// <param name="a2">first bond</param>
        /// <param name="b2">second bond</param>
        /// <returns>the symbol of the common atom or "" if the 2 bonds have no common atom</returns>
        private static bool QueryAdjacency(IBond a1, IBond b1, IBond a2, IBond b2)
        {
            IAtom atom1 = null;
            IAtom atom2 = null;

            if (a1.Contains(b1.Begin))
            {
                atom1 = b1.Begin;
            }
            else if (a1.Contains(b1.End))
            {
                atom1 = b1.End;
            }

            if (a2.Contains(b2.Begin))
            {
                atom2 = b2.Begin;
            }
            else if (a2.Contains(b2.End))
            {
                atom2 = b2.End;
            }

            if (atom1 != null && atom2 != null)
            {
                // well, this looks fishy: the atom2 is not always a IQueryAtom !
                return ((IQueryAtom)atom2).Matches(atom1);
            }
            else
                return atom1 == null && atom2 == null;
        }

        /// <summary>
        ///  Determines if 2 bond have 1 atom in common if second is a query AtomContainer
        ///  and whether the order of the atoms is correct (atoms match).
        /// </summary>
        /// <param name="bond1">first bond</param>
        /// <param name="bond2">second bond</param>
        /// <param name="queryBond1">first query bond</param>
        /// <param name="queryBond2">second query bond</param>
        /// <returns>the symbol of the common atom or "" if the 2 bonds have no common atom</returns>
        private static bool QueryAdjacencyAndOrder(IBond bond1, IBond bond2, IBond queryBond1, IBond queryBond2)
        {
            IAtom centralAtom = null;
            IAtom centralQueryAtom = null;

            if (bond1.Contains(bond2.Begin))
            {
                centralAtom = bond2.Begin;
            }
            else if (bond1.Contains(bond2.End))
            {
                centralAtom = bond2.End;
            }

            if (queryBond1.Contains(queryBond2.Begin))
            {
                centralQueryAtom = queryBond2.Begin;
            }
            else if (queryBond1.Contains(queryBond2.End))
            {
                centralQueryAtom = queryBond2.End;
            }

            if (centralAtom != null && centralQueryAtom != null 
             && ((IQueryAtom)centralQueryAtom).Matches(centralAtom))
            {
                var queryAtom1 = (IQueryAtom)queryBond1.GetConnectedAtom(centralQueryAtom);
                var queryAtom2 = (IQueryAtom)queryBond2.GetConnectedAtom(centralQueryAtom);
                var atom1 = bond1.GetConnectedAtom(centralAtom);
                var atom2 = bond2.GetConnectedAtom(centralAtom);
                if (queryAtom1.Matches(atom1) && queryAtom2.Matches(atom2)
                 || queryAtom1.Matches(atom2) && queryAtom2.Matches(atom1))
                {
                    return true;
                }
                else
                    return false;
            }
            else
                return centralAtom == null && centralQueryAtom == null;

        }

        /// <summary>
        ///  Checks some simple heuristics for whether the subgraph query can
        ///  realistically be a subgraph of the supergraph. If, for example, the
        ///  number of nitrogen atoms in the query is larger than that of the supergraph
        ///  it cannot be part of it.
        /// </summary>
        /// <param name="ac1">the supergraph to be checked. Must not be an <see cref="IQueryAtomContainer"/>.</param>
        /// <param name="ac2">the subgraph to be tested for. May be an <see cref="IQueryAtomContainer"/>.</param>
        /// <returns>true if the subgraph ac2 has a chance to be a subgraph of ac1</returns>
        /// <exception cref="CDKException">if the first molecule is an instance of <see cref="IQueryAtomContainer"/></exception>
        private static bool TestSubgraphHeuristics(IAtomContainer ac1, IAtomContainer ac2)
        {
            if (ac1 is IQueryAtomContainer)
                throw new CDKException("The first IAtomContainer must not be an IQueryAtomContainer");

            int ac1SingleBondCount = 0;
            int ac1DoubleBondCount = 0;
            int ac1TripleBondCount = 0;
            int ac1AromaticBondCount = 0;
            int ac2SingleBondCount = 0;
            int ac2DoubleBondCount = 0;
            int ac2TripleBondCount = 0;
            int ac2AromaticBondCount = 0;
            int ac1SCount = 0;
            int ac1OCount = 0;
            int ac1NCount = 0;
            int ac1FCount = 0;
            int ac1ClCount = 0;
            int ac1BrCount = 0;
            int ac1ICount = 0;
            int ac1CCount = 0;

            int ac2SCount = 0;
            int ac2OCount = 0;
            int ac2NCount = 0;
            int ac2FCount = 0;
            int ac2ClCount = 0;
            int ac2BrCount = 0;
            int ac2ICount = 0;
            int ac2CCount = 0;

            IBond bond;
            IAtom atom;
            for (int i = 0; i < ac1.Bonds.Count; i++)
            {
                bond = ac1.Bonds[i];
                if (bond.IsAromatic)
                    ac1AromaticBondCount++;
                else if (bond.Order == BondOrder.Single)
                    ac1SingleBondCount++;
                else if (bond.Order == BondOrder.Double)
                    ac1DoubleBondCount++;
                else if (bond.Order == BondOrder.Triple)
                    ac1TripleBondCount++;
            }
            for (int i = 0; i < ac2.Bonds.Count; i++)
            {
                bond = ac2.Bonds[i];
                if (bond is IQueryBond)
                    continue;
                if (bond.IsAromatic)
                    ac2AromaticBondCount++;
                else if (bond.Order == BondOrder.Single)
                    ac2SingleBondCount++;
                else if (bond.Order == BondOrder.Double)
                    ac2DoubleBondCount++;
                else if (bond.Order == BondOrder.Triple)
                    ac2TripleBondCount++;
            }

            if (ac2SingleBondCount > ac1SingleBondCount) return false;
            if (ac2AromaticBondCount > ac1AromaticBondCount) return false;
            if (ac2DoubleBondCount > ac1DoubleBondCount) return false;
            if (ac2TripleBondCount > ac1TripleBondCount) return false;

            for (int i = 0; i < ac1.Atoms.Count; i++)
            {
                atom = ac1.Atoms[i];
                switch (atom.AtomicNumber)
                {
                    case AtomicNumbers.S:  ac1SCount++;  break;
                    case AtomicNumbers.N:  ac1NCount++;  break;
                    case AtomicNumbers.O:  ac1OCount++;  break;
                    case AtomicNumbers.F:  ac1FCount++;  break;
                    case AtomicNumbers.Cl: ac1ClCount++; break;
                    case AtomicNumbers.Br: ac1BrCount++; break;
                    case AtomicNumbers.I:  ac1ICount++;  break;
                    case AtomicNumbers.C:  ac1CCount++;  break;
                }
            }
            for (int i = 0; i < ac2.Atoms.Count; i++)
            {
                atom = ac2.Atoms[i];
                if (atom is IQueryAtom)
                    continue;
                switch (atom.AtomicNumber)
                {
                    case AtomicNumbers.S:  ac2SCount++;  break;
                    case AtomicNumbers.N:  ac2NCount++;  break;
                    case AtomicNumbers.O:  ac2OCount++;  break;
                    case AtomicNumbers.F:  ac2FCount++;  break;
                    case AtomicNumbers.Cl: ac2ClCount++; break;
                    case AtomicNumbers.Br: ac2BrCount++; break;
                    case AtomicNumbers.I:  ac2ICount++;  break;
                    case AtomicNumbers.C:  ac2CCount++;  break;
                }
            }

            if (ac1SCount < ac2SCount) return false;
            if (ac1NCount < ac2NCount) return false;
            if (ac1OCount < ac2OCount) return false;
            if (ac1FCount < ac2FCount) return false;
            if (ac1ClCount < ac2ClCount) return false;
            if (ac1BrCount < ac2BrCount) return false;
            if (ac1ICount < ac2ICount) return false;
            return ac1CCount >= ac2CCount;
        }
    }
}
