/*
 * Copyright (c) 2014 European Bioinformatics Institute (EMBL-EBI)
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
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using NCDK.Common.Primitives;
using NCDK.Graphs;
using System;
using System.Collections.Generic;

namespace NCDK.ForceFields
{
    /// <summary>
    /// Assign MMFF aromatic atom types from the preliminary symbolic type. The assignment is described
    /// in the appendix of <token>cdk-cite-Halgren96a</token>:
    /// <para>
    /// For non-hydrogen atoms, the assignment of symbolic MMFF atom types takes place in two stages. In
    /// the first, a provisional atom type is assigned based on local connectivity. In the second,
    /// aromatic systems are perceived, and properly qualified aromatic atom types are assigned based on
    /// ring size and, for five-membered rings, on the position within the ring. Information in this file
    /// (MMFFAROM.PAR) is used to make the proper correspondence between provisional and final (aromatic)
    /// atom types. 
    /// </para>
    /// </summary>
    /// <remarks>
    /// The column labeled "L5" refers, in the case of 5-ring systems, to the position of the atom in
    /// question relative to the unique pi-lone-pair containing heteroatom (which itself occupies
    /// position "1"); a "4" is an artificial entry that is assigned when no such unique heteroatom
    /// exists, as for example occurs in imidazolium cations and in tetrazole anions. An entry of "1" in
    /// the "IM CAT" or "N5 ANION" column must also be matched for such ionic species to convert the
    /// "OLD" (preliminary) to "AROM" (aromatic) symbolic atom type. Note: in matching the "OLD" symbolic
    /// atom types, an "exact" match is first attempted. If this match fails, a wild-carded match, using
    /// for example "C*" is then employed. 
    /// <para>
    /// This class implements this in three stages. Firstly, the aromatic rings are found with <see cref="FindAromaticRings(int[][], int[], int[])"/>. 
    /// These rings are then parsed to <see cref="UpdateAromaticTypesInSixMemberRing(int[], string[])"/> 
    /// and <see cref="UpdateAromaticTypesInFiveMemberRing(int[], string[])"/>.
    /// The more complex of the two is the five member rings that normalises the ring to put
    /// the 'pi-lone-pair' hetroatom in position 1. The alpha and beta positions are then fixed and the
    /// <see cref="alphaTypes"/>  and <see cref="betaTypes"/>  mappings are used to obtain the correct assignment.
    /// </para>
    /// </remarks>
    // @author John May
    internal sealed class MmffAromaticTypeMapping
    {
        /// <summary>
        /// Create an instance to map from preliminary MMFF symbolic types to their aromatic equivalent.
        /// </summary>
        public MmffAromaticTypeMapping() { }

        /// <summary>
        /// Given the assigned preliminary MMFF atom types (symbs[]) update these to the aromatic types.
        /// To begin, all the 5 and 6 member aromatic cycles are discovered. The symbolic types of five
        /// and six member cycles are then update with <see cref="UpdateAromaticTypesInFiveMemberRing(int[], string[])"/>
        /// and <see cref="UpdateAromaticTypesInSixMemberRing(int[], string[])"/>.
        /// </summary>
        /// <param name="container">structure representation</param>
        /// <param name="symbs">vector of symbolic types for the whole structure</param>
        /// <param name="bonds">edge to bond map lookup</param>
        /// <param name="graph">adjacency list graph representation of structure</param>
        /// <param name="mmffArom">set of bonds that are aromatic</param>
        public void Assign(IAtomContainer container, string[] symbs, EdgeToBondMap bonds, int[][] graph, ISet<IBond> mmffArom)
        {
            var contribution = new int[graph.Length];
            var doubleBonds = new int[graph.Length];
            Arrays.Fill(doubleBonds, -1);
            SetupContributionAndDoubleBonds(container, bonds, graph, contribution, doubleBonds);

            var cycles = FindAromaticRings(CyclesOfSizeFiveOrSix(container, graph), contribution, doubleBonds);

            foreach (var cycle in cycles)
            {
                int len = cycle.Length - 1;
                if (len == 6)
                {
                    UpdateAromaticTypesInSixMemberRing(cycle, symbs);
                }
                if (len == 5 && NormaliseCycle(cycle, contribution))
                {
                    UpdateAromaticTypesInFiveMemberRing(cycle, symbs);
                }
                // mark aromatic bonds
                for (int i = 1; i < cycle.Length; i++)
                    mmffArom.Add(bonds[cycle[i], cycle[i - 1]]);
            }
        }

        /// <summary>
        /// From a provided set of cycles find the 5/6 member cycles that fit the MMFF aromaticity
        /// definition - <see cref="IsAromaticRing(int[], int[], int[], bool[])"/>. The cycles of size 6
        /// are listed first.
        /// </summary>
        /// <param name="cycles">initial set of cycles from</param>
        /// <param name="contribution">vector of p electron contributions from each vertex</param>
        /// <param name="dbs">vector of double-bond pairs, index stored double-bonded index</param>
        /// <returns>the cycles that are aromatic</returns>
        private static int[][] FindAromaticRings(int[][] cycles, int[] contribution, int[] dbs)
        {
            // loop control variables, the while loop continual checks all cycles
            // until no changes are found
            bool found;
            var checked_ = new bool[cycles.Length];

            // stores the aromatic atoms as a bit set and the aromatic bonds as
            // a hash set. the aromatic bonds are the result of this method but the
            // aromatic atoms are needed for checking each ring
            var aromaticAtoms = new bool[contribution.Length];

            var ringsOfSize6 = new List<int[]>();
            var ringsOfSize5 = new List<int[]>();

            do
            {
                found = false;
                for (int i = 0; i < cycles.Length; i++)
                {
                    // note paths are closed walks and repeat first/last vertex so
                    // the true length is one less
                    var cycle = cycles[i];
                    var len = cycle.Length - 1;

                    if (checked_[i])
                        continue;

                    if (IsAromaticRing(cycle, contribution, dbs, aromaticAtoms))
                    {
                        checked_[i] = true;
                        found |= true;
                        for (int j = 0; j < len; j++)
                        {
                            aromaticAtoms[cycle[j]] = true;
                        }
                        if (len == 6)
                            ringsOfSize6.Add(cycle);
                        else if (len == 5)
                            ringsOfSize5.Add(cycle);

                    }
                }
            } while (found);

            var rings = new List<int[]>();
            rings.AddRange(ringsOfSize6);
            rings.AddRange(ringsOfSize5);

            return rings.ToArray();
        }

        /// <summary>
        /// Check if a cycle/ring is aromatic. A cycle is aromatic if the sum of its p electrons is equal
        /// to 4n+2. Double bonds can only contribute if they are in the cycle being tested or are
        /// already delocalised.
        /// </summary>
        /// <param name="cycle">closed walk of vertices in the cycle</param>
        /// <param name="contribution">vector of p electron contributions from each vertex</param>
        /// <param name="dbs">vector of double-bond pairs, index stored double-bonded index</param>
        /// <param name="aromatic">binary set of aromatic atoms</param>
        /// <returns>whether the ring is aromatic</returns>
        public static bool IsAromaticRing(int[] cycle, int[] contribution, int[] dbs, bool[] aromatic)
        {
            int len = cycle.Length - 1;
            int sum = 0;

            int i = 0;
            int iPrev = len - 1;
            int iNext = 1;

            while (i < len)
            {
                var prev = cycle[iPrev];
                var curr = cycle[i];
                var next = cycle[iNext];

                var pElectrons = contribution[curr];

                if (pElectrons < 0)
                    return false;

                // single p electrons are only donated from double bonds, these are
                // only counted if the bonds are either in this ring or the bond
                // is aromatic
                if (pElectrons == 1)
                {
                    var other = dbs[curr];
                    if (other < 0)
                        return false;
                    if (other != prev && other != next && !aromatic[other])
                        return false;
                }

                iPrev = i;
                i = iNext;
                iNext = iNext + 1;
                sum += pElectrons;
            }

            // the sum of electrons 4n+2?
            return (sum - 2) % 4 == 0;
        }

        /// <summary>
        /// Update aromatic atom types in a six member ring. The aromatic types here are hard coded from
        /// the 'MMFFAROM.PAR' file.
        /// </summary>
        /// <param name="cycle">6-member aromatic cycle / ring</param>
        /// <param name="symbs">vector of symbolic types for the whole structure</param>
        public static void UpdateAromaticTypesInSixMemberRing(int[] cycle, string[] symbs)
        {
            foreach (var v in cycle)
            {
                switch (symbs[v])
                {
                    case NCN_PLUS:
                    case "N+=C":
                    case "N=+C":
                        symbs[v] = "NPD+";
                        break;
                    case "N2OX":
                        symbs[v] = "NPOX";
                        break;
                    case "N=C":
                    case "N=N":
                        symbs[v] = "NPYD";
                        break;
                    default:
                        if (symbs[v].StartsWithChar('C'))
                            symbs[v] = "CB";
                        break;
                }
            }
        }

        /// <summary>
        /// Update the symbolic for a 5-member cycle/ring. The cycle should first be normalised with
        /// <see cref="NormaliseCycle(int[], int[])"/> to put the unique 'pi-lone-pair' in position 1 (index
        /// 0). Using predefined mappings the symbolic atom types are updated in the 'symbs[]' vector.
        /// </summary>
        /// <param name="cycle">normalised 5-member cycle (6 indices)</param>
        /// <param name="symbs">vector of symbolic types for the whole structure</param>
        private void UpdateAromaticTypesInFiveMemberRing(int[] cycle, string[] symbs)
        {
            var hetro = symbs[cycle[0]];

            // simple conditions tell is the 'IM' and 'AN' flags
            var imidazolium = NCN_PLUS.Equals(hetro, StringComparison.Ordinal) || NGD_PLUS.Equals(hetro, StringComparison.Ordinal);
            var anion = "NM".Equals(hetro, StringComparison.Ordinal);

            symbs[cycle[0]] = hetroTypes[hetro];

            symbs[cycle[1]] = GetAlphaAromaticType(symbs[cycle[1]], imidazolium, anion);
            symbs[cycle[4]] = GetAlphaAromaticType(symbs[cycle[4]], imidazolium, anion);
            symbs[cycle[2]] = GetBetaAromaticType(symbs[cycle[2]], imidazolium, anion);
            symbs[cycle[3]] = GetBetaAromaticType(symbs[cycle[3]], imidazolium, anion);
        }

        /// <summary>
        /// Convenience method to obtain the aromatic type of a symbolic (SYMB) type in the alpha
        /// position of a 5-member ring. This method delegates to <see cref="GetAromaticType(IReadOnlyDictionary{string, string}, char, string, bool, bool)"/>
        /// setup for alpha atoms.
        /// </summary>
        /// <param name="symb">symbolic atom type</param>
        /// <param name="imidazolium">imidazolium flag (IM naming from MMFFAROM.PAR)</param>
        /// <param name="anion">anion flag (AN naming from MMFFAROM.PAR)</param>
        /// <returns>the aromatic type</returns>
        private string GetAlphaAromaticType(string symb, bool imidazolium, bool anion)
        {
            return GetAromaticType(alphaTypes, 'A', symb, imidazolium, anion);
        }

        /// <summary>
        /// Convenience method to obtain the aromatic type of a symbolic (SYMB) type in the beta position
        /// of a 5-member ring. This method delegates to <see cref="GetAromaticType(IReadOnlyDictionary{string, string}, char, string, bool, bool)"/>
        /// setup for beta atoms.
        /// </summary>
        /// <param name="symb">symbolic atom type</param>
        /// <param name="imidazolium">imidazolium flag (IM naming from MMFFAROM.PAR)</param>
        /// <param name="anion">anion flag (AN naming from MMFFAROM.PAR)</param>
        /// <returns>the aromatic type</returns>
        private string GetBetaAromaticType(string symb, bool imidazolium, bool anion)
        {
            return GetAromaticType(betaTypes, 'B', symb, imidazolium, anion);
        }

        /// <summary>
        /// Obtain the aromatic atom type for an atom in the alpha or beta position of a 5-member
        /// aromatic ring. The method primarily uses an HashMap to lookup up the aromatic type. The two
        /// maps are, <see cref="alphaTypes"/> and <see cref="betaTypes"/>. Depending on the position (alpha or
        /// beta), one map is passed to the method. The exceptions to using the HashMap directly are as
        /// follows: 1) if AN flag is raised and the symbolic type is a nitrogen, the type is 'N5M'. 2)
        /// If the IM or AN flag is raised, the atom is 'C5' or 'N5 instead of 'C5A', 'C5B', 'N5A', or
        /// 'N5B'. This is because the hetroatom in these rings can resonate and so the atom is both
        /// alpha and beta.
        /// </summary>
        /// <param name="map">mapping of alpha or beta types</param>
        /// <param name="suffix">'A' or 'B'</param>
        /// <param name="symb">input symbolic type</param>
        /// <param name="imidazolium">imidazolium flag (IM naming from MMFFAROM.PAR)</param>
        /// <param name="anion">anion flag (AN naming from MMFFAROM.PAR)</param>
        /// <returns>the aromatic type</returns>
        public static string GetAromaticType(IReadOnlyDictionary<string, string> map, char suffix, string symb, bool imidazolium, bool anion)
        {
            if (anion && symb.StartsWithChar('N'))
                symb = "N5M";
            if (map.ContainsKey(symb))
                symb = map[symb];
            if ((imidazolium || anion) && symb[symb.Length - 1] == suffix)
                symb = symb.Substring(0, symb.Length - 1);
            return symb;
        }

        /// <summary>
        /// Find the index of a hetroatom in a cycle. A hetroatom in MMFF is the unique atom that
        /// contributes a pi-lone-pair to the aromatic system.
        /// </summary>
        /// <param name="cycle">aromatic cycle, |C| = 5</param>
        /// <param name="contribution">vector of p electron contributions from each vertex</param>
        /// <returns>index of hetroatom, if none found index is &lt; 0.</returns>
        public static int IndexOfHetro(int[] cycle, int[] contribution)
        {
            int index = -1;
            for (int i = 0; i < cycle.Length - 1; i++)
            {
                if (contribution[cycle[i]] == 2)
                    index = index == -1 ? i : -2;
            }
            return index;
        }

        /// <summary>
        /// Normalises a 5-member 'cycle' such that the hetroatom contributing the lone-pair is in
        /// position 1 (index 0). The alpha atoms are then in index 1 and 4 whilst the beta atoms are in
        /// index 2 and 3. If the ring contains more than one hetroatom the cycle is not normalised
        /// (return=false).
        /// </summary>
        /// <param name="cycle">aromatic cycle to normalise, |C| = 5</param>
        /// <param name="contribution">vector of p electron contributions from each vertex (size |V|)</param>
        /// <returns>whether the cycle was normalised</returns>
        public static bool NormaliseCycle(int[] cycle, int[] contribution)
        {
            var offset = IndexOfHetro(cycle, contribution);
            if (offset < 0)
                return false;
            if (offset == 0)
                return true;
            var cpy = Arrays.CopyOf(cycle, cycle.Length);
            var len = cycle.Length - 1;
            for (int j = 0; j < len; j++)
            {
                cycle[j] = cpy[(offset + j) % len];
            }
            cycle[len] = cycle[0]; // make closed walk
            return true;
        }

        /// <summary>
        /// Electron contribution of an element with the specified connectivity and valence.
        /// </summary>
        /// <param name="elem">atomic number</param>
        /// <param name="x">connectivity</param>
        /// <param name="v">bonded valence</param>
        /// <returns>p electrons</returns>
        // high complexity but clean
        public static int Contribution(int elem, int x, int v)
        {
            switch (elem)
            {
                case 6:
                    if (x == 3 && v == 4) return 1; // pi bond *-C=*
                    break;
                case 7:
                    if (x == 2 && v == 3) return 1; // pi bond *-N=*
                    if (x == 3 && v == 4) return 1; // pi bond *-[N+H}=*
                    if (x == 3 && v == 3) return 2; // lone pair *-N-*
                    if (x == 2 && v == 2) return 2; // lone pair *-[N-]-*
                    break;
                case 8:
                case 16:
                    if (x == 2 && v == 2) return 2; // lone pair *-S-* and *-O-*
                    break;
            }
            return -1;
        }

        /// <summary>
        /// Locate all 5 and 6 member cycles (rings) in a structure representation.
        /// </summary>
        /// <param name="container">structure representation</param>
        /// <param name="graph">adjacency list graph representation of structure</param>
        /// <returns>closed walks (first = last vertex) of the cycles</returns>
        public static int[][] CyclesOfSizeFiveOrSix(IAtomContainer container, int[][] graph)
        {
            try
            {
                return Cycles.GetAllFinder(6).Find(container, graph, 6).GetPaths();
            }
            catch (IntractableException)
            {
                return Array.Empty<int[]>();
            }
        }

        /// <summary>
        /// Internal - sets up the 'contribution' and 'dbs' vectors. These define how many pi electrons
        /// an atom can contribute and provide a lookup of the double bonded neighbour.
        /// </summary>
        /// <param name="molecule">structure representation</param>
        /// <param name="bonds">edge to bond map lookup</param>
        /// <param name="graph">adjacency list graph representation of structure</param>
        /// <param name="contribution">vector of p electron contributions from each vertex</param>
        /// <param name="dbs">vector of double-bond pairs, index stored double-bonded index</param>
        private static void SetupContributionAndDoubleBonds(IAtomContainer molecule, EdgeToBondMap bonds, int[][] graph, int[] contribution, int[] dbs)
        {
            // fill the contribution and dbs vectors
            for (int v = 0; v < graph.Length; v++)
            {
                // hydrogens, valence, and connectivity
                var hyd = molecule.Atoms[v].ImplicitHydrogenCount.Value;
                var val = hyd;
                var con = hyd + graph[v].Length;

                foreach (var w in graph[v])
                {
                    var bond = bonds[v, w];
                    val += bond.Order.Numeric();
                    if (bond.Order == BondOrder.Double)
                    {
                        dbs[v] = dbs[v] == -1 ? w : -2;
                    }
                }

                contribution[v] = Contribution(molecule.Atoms[v].AtomicNumber, con, val);
            }
        }

        /// <summary>
        /// Mapping of preliminary atom MMFF symbolic types to aromatic types for atoms that contribute a
        /// lone pair.
        /// </summary>
        private readonly Dictionary<string, string> hetroTypes = 
            new Dictionary<string, string>()
            {
                { "S", STHI },
                { "-O-", OFUR },
                { "OC=C", OFUR },
                { "OC=N", OFUR },
                { NCN_PLUS, NIM_PLUS },
                { NGD_PLUS, NIM_PLUS },
                { "NM", N5M },
                { "NC=C", NPYL },
                { "NC=N", NPYL },
                { "NN=N", NPYL },
                { "NC=O", NPYL },
                { "NC=S", NPYL },
                { "NSO2", NPYL },
                { "NR", NPYL },
            };

        /// <summary>
        /// Mapping of preliminary atom MMFF symbolic types to aromatic types for atoms that contribute
        /// one electron and are alpha to an atom that contributes a lone pair.
        /// </summary>
        private readonly Dictionary<string, string> alphaTypes =
            new Dictionary<string, string>()
            {
                { "CNN+", CIM_PLUS },
                { "CGD+", CIM_PLUS },
                { "C=C", C5A },
                { "C=N", C5A },
                { "CGD", C5A },
                { "CB", C5A },
                { C5B, C5 },
                { "N2OX", N5AX },
                { NCN_PLUS, NIM_PLUS },
                { NGD_PLUS, NIM_PLUS },
                { "N+=C", N5A_PLUS },
                { "N+=N", N5A_PLUS },
                { "NPD+", N5A_PLUS },
                { "N=C", N5A },
                { "N=N", N5A },
            };

        /// <summary>
        /// Mapping of preliminary atom MMFF symbolic types to aromatic types for atoms that contribute
        /// one electron and are beta to an atom that contributes a lone pair.
        /// </summary>
        private readonly Dictionary<string, string> betaTypes =
            new Dictionary<string, string>()
            {
                { "CNN+", CIM_PLUS },
                { "CGD+", CIM_PLUS },
                { "C=C", C5B },
                { "C=N", C5B },
                { "CGD", C5B },
                { "CB", C5B },
                { C5A, C5 },
                { "N2OX", N5BX },
                { NCN_PLUS, NIM_PLUS },
                { NGD_PLUS, NIM_PLUS },
                { "N+=C", N5B_PLUS },
                { "N+=N", N5B_PLUS },
                { "NPD+", N5B_PLUS },
                { "N=C", N5B },
                { "N=N", N5B },
            };

        // C5 is intended
        private const string C5 = "C5";
        private const string C5A = "C5A";
        private const string C5B = "C5B";
        private const string N5A = "N5A";
        private const string N5B = "N5B";
        private const string NPYL = "NPYL";
        private const string NCN_PLUS = "NCN+";
        private const string NGD_PLUS = "NGD+";
        private const string NIM_PLUS = "NIM+";
        private const string N5A_PLUS = "N5A+";
        private const string N5B_PLUS = "N5B+";
        private const string N5M = "N5M";
        private const string N5AX = "N5AX";
        private const string N5BX = "N5BX";
        private const string CIM_PLUS = "CIM+";
        private const string OFUR = "OFUR";
        private const string STHI = "STHI";
    }
}
