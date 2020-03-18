/*  Copyright (C) 2004-2007  Rajarshi Guha <rajarshi@users.sourceforge.net>
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

using NCDK.Common.Base;
using NCDK.Config;
using NCDK.Isomorphisms;
using NCDK.Isomorphisms.Matchers;
using NCDK.Isomorphisms.MCSS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Utility methods for chi index calculations.
    /// </summary>
    /// <remarks>
    /// These methods are common to all the types of chi index calculations and can
    /// be used to evaluate path, path-cluster, cluster and chain chi indices.
    /// </remarks>
    // @author     Rajarshi Guha
    // @cdk.module qsarmolecular
    internal class ChiIndexUtils
    {
        /// <summary>
        /// Gets the fragments from a target matching a set of query fragments.
        /// </summary>
        /// <remarks>
        /// This method returns a list of lists. Each list contains the atoms of the target <see cref="IAtomContainer"/>
        /// that arise in the mapping of bonds in the target molecule to the bonds in the query fragment.
        /// The query fragments should be constructed
        /// using the <see cref="QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(IAtomContainer, bool)"/> method of the <see cref="QueryAtomContainerCreator"/>
        /// CDK class, since we are only interested in connectivity and not actual atom or bond type information.
        /// </remarks>
        /// <param name="atomContainer">The target <see cref="IAtomContainer"/></param>
        /// <param name="queries">An array of query fragments</param>
        /// <returns>A list of lists, each list being the atoms that match the query fragments</returns>
        public static List<List<int>> GetFragments(IAtomContainer atomContainer, QueryAtomContainer[] queries)
        {
            var universalIsomorphismTester = new UniversalIsomorphismTester();
            var uniqueSubgraphs = new List<List<int>>();
            foreach (var query in queries)
            {
                IReadOnlyList<IReadOnlyList<RMap>> subgraphMaps = null;
                try
                {
                    // we get the list of bond mappings
                    subgraphMaps = universalIsomorphismTester.GetSubgraphMaps(atomContainer, query).ToReadOnlyList();
                }
                catch (CDKException e)
                {
                    Console.Error.WriteLine(e.StackTrace);
                }
                if (subgraphMaps == null)
                    continue;
                if (!subgraphMaps.Any())
                    continue;

                // get the atom paths in the unique set of bond maps
                uniqueSubgraphs.AddRange(GetUniqueBondSubgraphs(subgraphMaps, atomContainer));
            }

            // lets run a check on the length of each returned fragment and delete
            // any that don't match the length of out query fragments. Note that since
            // sometimes a fragment might be a ring, it will have number of atoms
            // equal to the number of bonds, where as a fragment with no rings
            // will have number of atoms equal to the number of bonds+1. So we need to check
            // fragment size against all unique query sizes - I get lazy and don't check
            // unique query sizes, but the size of each query
            var ret = new List<List<int>>(uniqueSubgraphs.Count);
            foreach (var fragment in uniqueSubgraphs)
            {
                foreach (var query in queries)
                {
                    if (fragment.Count == query.Atoms.Count)
                    {
                        ret.Add(fragment);
                        break;
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Evaluates the simple chi index for a set of fragments.
        /// </summary>
        /// <param name="atomContainer">The target <see cref="IAtomContainer"/></param>
        /// <param name="fragLists">A list of fragments</param>
        /// <returns>The simple chi index</returns>
        public static double EvalSimpleIndex(IAtomContainer atomContainer, IEnumerable<IReadOnlyList<int>> fragLists)
        {
            double sum = 0;
            foreach (var fragList in fragLists)
            {
                double prod = 1.0;
                foreach (var atomSerial in fragList)
                {
                    var atom = atomContainer.Atoms[atomSerial];
                    var nconnected = atomContainer.GetConnectedAtoms(atom).Count();
                    prod = prod * nconnected;
                }
                if (prod != 0)
                    sum += 1.0 / Math.Sqrt(prod);
            }
            return sum;
        }

        /// <summary>
        /// Evaluates the valence corrected chi index for a set of fragments.
        /// </summary>
        /// <remarks>
        /// This method takes into account the S and P atom types described in
        /// Kier &amp; Hall (1986), page 20 for which empirical delta V values are used.
        /// </remarks>
        /// <param name="atomContainer">The target <see cref="IAtomContainer"/></param>
        /// <param name="fragList">A list of fragments</param>
        /// <returns>The valence corrected chi index</returns>
        /// <exception cref="CDKException"> if the <see cref="IsotopeFactory"/> cannot be created</exception>
        public static double EvalValenceIndex(IAtomContainer atomContainer, List<List<int>> fragList)
        {
            try
            {
                var ifac = CDK.IsotopeFactory;
                ifac.ConfigureAtoms(atomContainer);
            }
            catch (IOException e)
            {
                throw new CDKException($"IO problem occurred when using the CDK atom config\n{e.Message}", e);
            }
            double sum = 0;
            foreach (var aFragList in fragList)
            {
                var frag = aFragList;
                double prod = 1.0;
                foreach (var aFrag in frag)
                {
                    var atomSerial = aFrag;
                    var atom = atomContainer.Atoms[atomSerial];

                    string sym = atom.Symbol;
                    switch (atom.AtomicNumber)
                    {
                        case AtomicNumbers.S:
                            { // check for some special S environments
                                var tmp = DeltavSulphur(atom, atomContainer);
                                if (tmp != -1)
                                {
                                    prod = prod * tmp;
                                    continue;
                                }
                            }
                            break;
                        case AtomicNumbers.P:
                            { // check for some special P environments
                                var tmp = DeltavPhosphorous(atom, atomContainer);
                                if (tmp != -1)
                                {
                                    prod = prod * tmp;
                                    continue;
                                }
                            }
                            break;
                    }

                    var z = atom.AtomicNumber;

                    // TODO there should be a neater way to get the valence electron count
                    var zv = GetValenceElectronCount(atom);

                    var hsupp = atom.ImplicitHydrogenCount.Value;
                    var deltav = (double)(zv - hsupp) / (double)(z - zv - 1);

                    prod = prod * deltav;
                }
                if (prod != 0)
                    sum += 1.0 / Math.Sqrt(prod);
            }
            return sum;
        }

        private static int GetValenceElectronCount(IAtom atom)
        {
            var valency = AtomValenceTool.GetValence(atom);
            return valency - atom.FormalCharge.Value;
        }

        /// <summary>
        /// Evaluates the empirical delt V for some S environments.
        /// </summary>
        /// <remarks>
        /// The method checks to see whether a S atom is in a -S-S-,
        /// -SO-, -SO2- group and returns the empirical values noted
        /// in Kier &amp; Hall (1986), page 20.
        /// </remarks>
        /// <param name="atom">The S atom in question</param>
        /// <param name="atomContainer">The molecule containing the S</param>
        /// <returns>The empirical delta V if it is present in one of the above environments, -1 otherwise</returns>
        protected internal static double DeltavSulphur(IAtom atom, IAtomContainer atomContainer)
        {
            if (!atom.AtomicNumber.Equals(AtomicNumbers.S))
                return -1;

            // check whether it's a S in S-S
            var connected = atomContainer.GetConnectedAtoms(atom);
            foreach (var connectedAtom in connected)
            {
                if (connectedAtom.AtomicNumber.Equals(AtomicNumbers.S)
                 && atomContainer.GetBond(atom, connectedAtom).Order == BondOrder.Single)
                    return 0.89;
            }

            int count = 0;
            foreach (var connectedAtom in connected)
            {
                if (connectedAtom.AtomicNumber.Equals(AtomicNumbers.O)
                 && atomContainer.GetBond(atom, connectedAtom).Order == BondOrder.Double)
                    count++;
            }
            if (count == 1)
                return 1.33; // check whether it's a S in -SO-
            else if (count == 2)
                return 2.67; // check whether it's a S in -SO2-

            return -1;
        }

        /// <summary>
        /// Checks whether the P atom is in a PO environment.
        /// </summary>
        /// <remarks>
        /// This environment is noted in Kier &amp; Hall (1986), page 20
        /// </remarks>
        /// <param name="atom">The P atom in question</param>
        /// <param name="atomContainer">The molecule containing the P atom</param>
        /// <returns>The empirical delta V if present in the above environment, -1 otherwise</returns>
        private static double DeltavPhosphorous(IAtom atom, IAtomContainer atomContainer)
        {
            if (!atom.AtomicNumber.Equals(AtomicNumbers.P))
                return -1;

            var connected = atomContainer.GetConnectedAtoms(atom);
            int conditions = 0;

            if (connected.Count() == 4)
                conditions++;

            foreach (var connectedAtom in connected)
            {
                if (connectedAtom.AtomicNumber.Equals(AtomicNumbers.O)
                 && atomContainer.GetBond(atom, connectedAtom).Order == BondOrder.Double)
                    conditions++;
                if (atomContainer.GetBond(atom, connectedAtom).Order == BondOrder.Single)
                    conditions++;
            }
            if (conditions == 5)
                return 2.22;
            return -1;
        }

        /// <summary>
        /// Converts a set of bond mappings to a unique set of atom paths.
        /// </summary>
        /// <remarks>
        /// This method accepts a <see cref="IList{T}"/> of bond mappings. It first
        /// reduces the set to a unique set of bond maps and then for each bond map
        /// converts it to a series of atoms making up the bonds.
        /// </remarks>
        /// <param name="subgraphs">A <see cref="IList{T}"/> of bon mappings</param>
        /// <param name="ac">The molecule we are examining</param>
        /// <returns>A unique <see cref="IList{T}"/> of atom paths</returns>
        private static List<List<int>> GetUniqueBondSubgraphs(IEnumerable<IReadOnlyList<RMap>> subgraphs, IAtomContainer ac)
        {
            var bondList = new List<List<int>>();
            foreach (var subgraph in subgraphs)
            {
                var current = subgraph;
                var ids = new List<int>();
                foreach (var aCurrent in current)
                {
                    var rmap = aCurrent;
                    ids.Add(rmap.Id1);
                }
                ids.Sort();
                // set the unique set of bonds
                foreach (var bl in bondList)
                {
                    if (Compares.AreEqual(bl, ids))
                        goto GO_NO_REGISTER;
                }
                bondList.Add(ids);
            GO_NO_REGISTER:
                ;
            }

            var paths = new List<List<int>>();
            foreach (var aBondList1 in bondList)
            {
                var aBondList = aBondList1;
                var tmp = new List<int>();
                foreach (var bondNumber in aBondList)
                {
                    foreach (var atom in ac.Bonds[bondNumber].Atoms)
                    {
                        int atomInt = ac.Atoms.IndexOf(atom);
                        if (!tmp.Contains(atomInt))
                            tmp.Add(atomInt);
                    }
                }
                paths.Add(tmp);
            }
            return paths;
        }

        internal static QueryAtomContainer[] MakeQueries(params string[] smiles)
        {
            return smiles.Select(n => QueryAtomContainerCreator.CreateAnyAtomAnyBondContainer(CDK.SmilesParser.ParseSmiles(n), false)).ToArray();
        }
    }
}
