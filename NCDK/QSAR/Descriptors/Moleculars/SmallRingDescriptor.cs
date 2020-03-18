/* Copyright (c) 2014  Collaborative Drug Discovery, Inc. <alex@collaborativedrug.com>
 *               2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 * Implemented by Alex M. Clark, produced by Collaborative Drug Discovery, Inc.
 * Made available to the CDK community under the terms of the GNU LGPL.
 *
 *    http://collaborativedrug.com
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

using NCDK.Fingerprints;
using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Small ring descriptors: these are based on enumeration of all the small rings (sizes 3 to 9) in a molecule,
    /// which can be obtained quickly and deterministically.
    /// </summary>
    // @cdk.module qsarmolecular
    // @cdk.dictref qsar-descriptors:smallrings
    // @cdk.keyword smallrings
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#smallRings")]
    public class SmallRingDescriptor : AbstractDescriptor, IMolecularDescriptor
    { 
        public SmallRingDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(
                int nSmallRings,
                int nAromRings,
                int nRingBlocks,
                int nAromBlocks,
                int nRings3,
                int nRings4,
                int nRings5,
                int nRings6,
                int nRings7,
                int nRings8,
                int nRings9)
            {
                NSmallRings = nSmallRings;
                NAromRings = nAromRings;
                NRingBlocks = nRingBlocks;
                NAromBlocks = nAromBlocks;
                NRings3 = nRings3;
                NRings4 = nRings4;
                NRings5 = nRings5;
                NRings6 = nRings6;
                NRings7 = nRings7;
                NRings8 = nRings8;
                NRings9 = nRings9;
            }

            /// <summary>
            /// total number of small rings (of size 3 through 9)
            /// </summary>
            [DescriptorResultProperty("nSmallRings")]
            public int NSmallRings { get; private set; }

            /// <summary>
            /// total number of small aromatic rings
            /// </summary>
            [DescriptorResultProperty("nAromRings")]
            public int NAromRings { get; private set; }

            /// <summary>
            /// total number of distinct ring blocks
            /// </summary>
            [DescriptorResultProperty("nRingBlocks")]
            public int NRingBlocks { get; private set; }

            /// <summary>
            /// total number of aromatically connected components
            /// </summary>
            [DescriptorResultProperty("nAromBlocks")]
            public int NAromBlocks { get; private set; }

            /// <summary>
            /// individual breakdown of 3 membered rings
            /// </summary>
            [DescriptorResultProperty("nRings3")]
            public int NRings3 { get; private set; }

            /// <summary>
            /// individual breakdown of 4 membered rings
            /// </summary>
            [DescriptorResultProperty("nRings4")]
            public int NRings4 { get; private set; }

            /// <summary>
            /// individual breakdown of 5 membered rings
            /// </summary>
            [DescriptorResultProperty("nRings5")]
            public int NRings5 { get; private set; }

            /// <summary>
            /// individual breakdown of 6 membered rings
            /// </summary>
            [DescriptorResultProperty("nRings6")]
            public int NRings6 { get; private set; }

            /// <summary>
            /// individual breakdown of 7 membered rings
            /// </summary>
            [DescriptorResultProperty("nRings7")]
            public int NRings7 { get; private set; }

            /// <summary>
            /// individual breakdown of 8 membered rings
            /// </summary>
            [DescriptorResultProperty("nRings8")]
            public int NRings8 { get; private set; }

            /// <summary>
            /// individual breakdown of 9 membered rings
            /// </summary>
            [DescriptorResultProperty("nRings9")]
            public int NRings9 { get; private set; }
        }

        public Result Calculate(IAtomContainer container)
        {
            return new Calculator(container).Calculate();
        }

        class Calculator
        {
            IAtomContainer container;

            public Calculator(IAtomContainer container)
            {
                this.container = container;
            }

            private int[][] atomAdj, bondAdj; // precalculated adjacencies
            private int[] ringBlock;          // ring block identifier; 0=not in a ring
            private int[][] smallRings;       // all rings of size 3 through 7
            private int[] bondOrder;          // numeric bond order for easy reference
            private bool[] bondArom;          // aromaticity precalculated
            private bool[] piAtom;            // true for all atoms involved in a double bond
            private int[] implicitH;          // hydrogens in addition to those encoded

            public Result Calculate()
            {
                ExcavateMolecule();

                int nSmallRings = smallRings.Length;
                int nAromRings = 0;
                int nRingBlocks = 0;
                int nAromBlocks = CountAromaticComponents();
                int nRings3 = 0, nRings4 = 0, nRings5 = 0, nRings6 = 0, nRings7 = 0, nRings8 = 0, nRings9 = 0;

                // count up the rings individually
                foreach (var r in smallRings)
                {
                    int sz = r.Length;
                    if (sz == 3)
                        nRings3++;
                    else if (sz == 4)
                        nRings4++;
                    else if (sz == 5)
                        nRings5++;
                    else if (sz == 6)
                        nRings6++;
                    else if (sz == 7)
                        nRings7++;
                    else if (sz == 8)
                        nRings8++;
                    else if (sz == 9)
                        nRings9++;

                    bool aromatic = true;
                    for (int n = 0; n < r.Length; n++)
                        if (!bondArom[FindBond(r[n], r[n < sz - 1 ? n + 1 : 0])])
                        {
                            aromatic = false;
                            break;
                        }
                    if (aromatic)
                        nAromRings++;
                }

                // # of ring blocks: the highest identifier is the total number of ring systems (0=not in a ring block)
                for (int n = ringBlock.Length - 1; n >= 0; n--)
                    nRingBlocks = Math.Max(nRingBlocks, ringBlock[n]);

                return new Result(
                    nSmallRings,
                    nAromRings,
                    nRingBlocks,
                    nAromBlocks,
                    nRings3,
                    nRings4,
                    nRings5,
                    nRings6,
                    nRings7,
                    nRings8,
                    nRings9);
            }

            // analyze the molecule graph, and build up the desired properties
            private void ExcavateMolecule()
            {
                var na = container.Atoms.Count;
                var nb = container.Bonds.Count;

                // build up an index-based neighbour/edge graph
                atomAdj = new int[na][];
                bondAdj = new int[na][];
                bondOrder = new int[nb];
                for (int n = 0; n < container.Bonds.Count; n++)
                {
                    var bond = container.Bonds[n];
                    if (bond.Atoms.Count != 2)
                        continue; // biconnected bonds only
                    var a1 = container.Atoms.IndexOf(bond.Atoms[0]);
                    var a2 = container.Atoms.IndexOf(bond.Atoms[1]);

                    atomAdj[a1] = AppendInteger(atomAdj[a1], a2);
                    bondAdj[a1] = AppendInteger(bondAdj[a1], n);
                    atomAdj[a2] = AppendInteger(atomAdj[a2], a1);
                    bondAdj[a2] = AppendInteger(bondAdj[a2], n);
                    if (bond.Order == BondOrder.Single)
                        bondOrder[n] = 1;
                    else if (bond.Order == BondOrder.Double)
                        bondOrder[n] = 2;
                    else if (bond.Order == BondOrder.Triple)
                        bondOrder[n] = 3;
                    else if (bond.Order == BondOrder.Quadruple) bondOrder[n] = 4;
                    // (look for zero-order bonds later on)
                }
                for (int n = 0; n < na; n++)
                    if (atomAdj[n] == null)
                    {
                        atomAdj[n] = Array.Empty<int>();
                        bondAdj[n] = atomAdj[n];
                    }

                // calculate implicit hydrogens, using a very conservative formula
                implicitH = new int[na];
                for (int n = 0; n < na; n++)
                {
                    var atom = container.Atoms[n];
                    if (!CircularFingerprinter.HYVALENCES.TryGetValue(atom.AtomicNumber, out int hy))
                        continue;
                    var ch = atom.FormalCharge.Value;
                    if (atom.AtomicNumber.Equals(AtomicNumbers.C))
                        ch = -Math.Abs(ch);
                    int unpaired = 0; // (not current available, maybe introduce later)
                    hy += ch - unpaired;
                    foreach (var ba in bondAdj[n])
                        hy -= bondOrder[ba];
                    implicitH[n] = Math.Max(0, hy);
                }

                MarkRingBlocks();

                var rings = new List<int[]>();
                for (int rsz = 3; rsz <= 7; rsz++)
                {
                    var path = new int[rsz];
                    for (int n = 0; n < na; n++)
                        if (ringBlock[n] > 0)
                        {
                            path[0] = n;
                            RecursiveRingFind(path, 1, rsz, ringBlock[n], rings);
                        }
                }
                smallRings = rings.ToArray();

                DetectStrictAromaticity();
                DetectRelaxedAromaticity();
            }

            // assign a ring block ID to each atom (0=not in ring)
            private void MarkRingBlocks()
            {
                var na = container.Atoms.Count;
                ringBlock = new int[na];

                var visited = new bool[na];

                var path = new int[na + 1];
                int plen = 0;
                while (true)
                {
                    int last, current;

                    if (plen == 0) // find an element of a new component to visit
                    {
                        last = -1;
                        for (current = 0; current < na && visited[current]; current++)
                        {
                        }
                        if (current >= na)
                            break;
                    }
                    else
                    {
                        last = path[plen - 1];
                        current = -1;
                        for (int n = 0; n < atomAdj[last].Length; n++)
                            if (!visited[atomAdj[last][n]])
                            {
                                current = atomAdj[last][n];
                                break;
                            }
                    }

                    if (current >= 0 && plen >= 2) // path is at least 2 items long, so look for any not-previous visited neighbours
                    {
                        var back = path[plen - 1];
                        for (int n = 0; n < atomAdj[current].Length; n++)
                        {
                            var join = atomAdj[current][n];
                            if (join != back && visited[join])
                            {
                                path[plen] = current;
                                for (int i = plen; i == plen || path[i + 1] != join; i--)
                                {
                                    var id = ringBlock[path[i]];
                                    if (id == 0)
                                        ringBlock[path[i]] = last;
                                    else if (id != last)
                                    {
                                        for (int j = 0; j < na; j++)
                                            if (ringBlock[j] == id)
                                                ringBlock[j] = last;
                                    }
                                }
                            }
                        }
                    }
                    if (current >= 0) // can mark the new one as visited
                    {
                        visited[current] = true;
                        path[plen++] = current;
                    }
                    else // otherwise, found nothing and must rewind the path
                    {
                        plen--;
                    }
                }

                // the ring ID's are not necessarily consecutive, so reassign them to 0=none, 1..NBlocks
                int nextID = 0;
                for (int i = 0; i < na; i++)
                    if (ringBlock[i] > 0)
                    {
                        nextID--;
                        for (int j = na - 1; j >= i; j--)
                            if (ringBlock[j] == ringBlock[i])
                                ringBlock[j] = nextID;
                    }
                for (int i = 0; i < na; i++)
                    ringBlock[i] = -ringBlock[i];
            }

            // hunt for ring recursively: start with a partially defined path, and go exploring
            private void RecursiveRingFind(int[] path, int psize, int capacity, int rblk, List<int[]> rings)
            {
                // not enough atoms yet, so look for new possibilities
                if (psize < capacity)
                {
                    var last = path[psize - 1];
                    for (int n = 0; n < atomAdj[last].Length; n++)
                    {
                        var adj = atomAdj[last][n];
                        if (ringBlock[adj] != rblk)
                            continue;
                        bool fnd = false;
                        for (int i = 0; i < psize; i++)
                            if (path[i] == adj)
                            {
                                fnd = true;
                                break;
                            }
                        if (!fnd)
                        {
                            var newPath = new int[capacity];
                            if (psize >= 0)
                                Array.Copy(path, 0, newPath, 0, psize);
                            newPath[psize] = adj;
                            RecursiveRingFind(newPath, psize + 1, capacity, rblk, rings);
                        }
                    }
                    return;
                }

                {
                    // path is full, so make sure it eats its tail
                    var last = path[psize - 1];
                    bool fnd = false;
                    for (int n = 0; n < atomAdj[last].Length; n++)
                        if (atomAdj[last][n] == path[0])
                        {
                            fnd = true;
                            break;
                        }
                    if (!fnd)
                        return;
                }

                // make sure every element in the path has exactly 2 neighbours within the path; otherwise it is spanning a bridge, which
                // is an undesirable ring definition
                foreach (var aPath in path)
                {
                    int count = 0;
                    for (int i = 0; i < atomAdj[aPath].Length; i++)
                        foreach (var aPath1 in path)
                            if (atomAdj[aPath][i] == aPath1)
                            {
                                count++;
                                break;
                            }
                    if (count != 2)
                        return; // invalid
                }

                // reorder the array (there are 2N possible ordered permutations) then look for duplicates
                int first = 0;
                for (int n = 1; n < psize; n++)
                    if (path[n] < path[first])
                        first = n;
                var fm = (first - 1 + psize) % psize;
                var fp = (first + 1) % psize;
                bool flip = path[fm] < path[fp];
                if (first != 0 || flip)
                {
                    var newPath = new int[psize];
                    for (int n = 0; n < psize; n++)
                        newPath[n] = path[(first + (flip ? psize - n : n)) % psize];
                    path = newPath;
                }

                foreach (var look in rings)
                {
                    bool same = true;
                    for (int i = 0; i < psize; i++)
                        if (look[i] != path[i])
                        {
                            same = false;
                            break;
                        }
                    if (same)
                        return;
                }

                rings.Add(path);
            }

            // aromaticity detection: uses a very narrowly defined algorithm, which detects 6-membered rings with alternating double bonds;
            // rings that are chained together (e.g. anthracene) will also be detected by the extended followup; note that this will NOT mark
            // rings such as thiophene, imidazolium, porphyrins, etc.: these systems will be left in their original single/double bond form
            private void DetectStrictAromaticity()
            {
                var na = container.Atoms.Count;
                var nb = container.Bonds.Count;
                bondArom = new bool[nb];

                if (smallRings.Length == 0)
                    return;

                piAtom = new bool[na];
                for (int n = 0; n < nb; n++)
                    if (bondOrder[n] == 2)
                    {
                        var bond = container.Bonds[n];
                        piAtom[container.Atoms.IndexOf(bond.Atoms[0])] = true;
                        piAtom[container.Atoms.IndexOf(bond.Atoms[1])] = true;
                    }

                var maybe = new List<int[]>(); // rings which may yet be aromatic
                foreach (var r in smallRings)
                    if (r.Length == 6)
                    {
                        bool consider = true;
                        for (int n = 0; n < 6; n++)
                        {
                            int a = r[n];
                            if (!piAtom[a])
                            {
                                consider = false;
                                break;
                            }
                            var b = FindBond(a, r[n == 5 ? 0 : n + 1]);
                            if (bondOrder[b] != 1 && bondOrder[b] != 2)
                            {
                                consider = false;
                                break;
                            }
                        }
                        if (consider)
                            maybe.Add(r);
                    }

                // keep classifying rings as aromatic until no change; this needs to be done iteratively, for the benefit of highly
                // embedded ring systems, that can't be classified as aromatic until it is known that their neighbours obviously are
                while (true)
                {
                    bool anyChange = false;

                    for (int n = maybe.Count - 1; n >= 0; n--)
                    {
                        var r = maybe[n];
                        bool phase1 = true, phase2 = true; // has to go 121212 or 212121; already arom=either is OK
                        for (int i = 0; i < 6; i++)
                        {
                            var b = FindBond(r[i], r[i == 5 ? 0 : i + 1]);
                            if (bondArom[b])
                                continue; // valid for either phase
                            phase1 = phase1 && bondOrder[b] == (2 - (i & 1));
                            phase2 = phase2 && bondOrder[b] == (1 + (i & 1));
                        }
                        if (!phase1 && !phase2)
                            continue;

                        // the ring is deemed aromatic: mark the flags and remove from the maybe list
                        for (int i = 0; i < r.Length; i++)
                        {
                            bondArom[FindBond(r[i], r[i == 5 ? 0 : i + 1])] = true;
                        }
                        maybe.RemoveAt(n);
                        anyChange = true;
                    }

                    if (!anyChange)
                        break;
                }
            }

            // supplement the original 'strict' definition of aromaticity with a more inclusive kind, which includes lone pairs
            private void DetectRelaxedAromaticity()
            {
                int na = container.Atoms.Count, nb = container.Bonds.Count;

                int[] ELEMENT_BLOCKS = {0, 1, 2, 1, 1, 2, 2, 2, 2, 2, 2, 1, 1, 2, 2, 2, 2, 2, 2, 1, 1, 3, 3, 3, 3, 3, 3,
                3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 1, 1, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 1, 1, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 1, 1, 4, 4, 4, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3};
                int[] ELEMENT_VALENCE = {0, 1, 2, 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8,
                9, 10, 11, 12, 3, 4, 5, 6, 7, 8, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 3, 4, 5, 6, 7, 8, 1, 2, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 3, 4, 5, 6, 7, 8, 1, 1, 4, 4, 4,
                4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12};

                // figure out which atoms have a lone pair which is considered valid for aromaticity: if electrons[i]>=2, then it qualifies
                var electrons = new int[na];
                for (int n = 0; n < na; n++)
                {
                    var atom = container.Atoms[n];
                    var atno = atom.AtomicNumber;
                    electrons[n] = (ELEMENT_BLOCKS[atno] == 2 ? ELEMENT_VALENCE[atno] : 0) - atom.FormalCharge.Value - implicitH[n];
                }
                for (int n = 0; n < nb; n++)
                    if (bondOrder[n] > 0)
                    {
                        var bond = container.Bonds[n];
                        electrons[container.Atoms.IndexOf(bond.Atoms[0])] -= bondOrder[n];
                        electrons[container.Atoms.IndexOf(bond.Atoms[1])] -= bondOrder[n];
                    }

                // pull out all of the small rings that could be upgraded to aromatic
                var rings = new List<int[]>();
                foreach (var r in smallRings)
                    if (r.Length <= 7)
                    {
                        bool alreadyArom = true;
                        bool isInvalid = false;
                        for (int n = 0; n < r.Length; n++)
                        {
                            if (!piAtom[r[n]] && electrons[r[n]] < 2)
                            {
                                isInvalid = true;
                                break;
                            }
                            var b = FindBond(r[n], r[n < r.Length - 1 ? n + 1 : 0]);
                            var bo = bondOrder[b];
                            if (bo != 1 && bo != 2)
                            {
                                isInvalid = true;
                                break;
                            }
                            alreadyArom = alreadyArom && bondArom[b];
                        }
                        if (!alreadyArom && !isInvalid)
                            rings.Add(r);
                    }

                // keep processing rings, until no new ones are found
                while (rings.Count > 0)
                {
                    bool anyChange = false;

                    for (int n = 0; n < rings.Count; n++)
                    {
                        var r = rings[n];
                        int pairs = 0, maybe = 0;
                        for (int i = 0; i < r.Length; i++)
                        {
                            var a = r[i];
                            var b1 = FindBond(r[i], r[i < r.Length - 1 ? i + 1 : 0]);
                            var b2 = FindBond(r[i], r[i > 0 ? i - 1 : r.Length - 1]);
                            if (bondArom[b1])
                                maybe += 2;
                            else if (bondOrder[b1] == 2)
                                pairs += 2;
                            else if (electrons[a] >= 2 && bondOrder[b2] != 2)
                                pairs += 2;
                        }

                        // see if there's anything Hueckel (4N+2) buried in there
                        bool arom = false;
                        while (maybe >= 0)
                        {
                            if ((pairs + maybe - 2) % 4 == 0)
                            {
                                arom = true;
                                break;
                            }
                            maybe -= 2;
                        }
                        if (arom)
                        {
                            for (int i = 0; i < r.Length; i++)
                            {
                                var a = r[i];
                                var b = FindBond(r[i], r[i < r.Length - 1 ? i + 1 : 0]);
                                bondArom[b] = true;
                            }
                            rings.RemoveAt(n);
                            n--;
                            anyChange = true;
                        }
                    }

                    if (!anyChange) break;
                }
            }

            // rebuild the graph using only aromatic bonds, and count the number of non-singleton connected components
            private int CountAromaticComponents()
            {
                var na = container.Atoms.Count;
                var graph = new int[na][];
                for (int n = 0; n < na; n++)
                {
                    for (int i = 0; i < atomAdj[n].Length; i++)
                        if (bondArom[bondAdj[n][i]])
                            graph[n] = AppendInteger(graph[n], atomAdj[n][i]);
                }

                var cc = new int[na]; // -1=isolated, so ignore; 0=unassigned; >0=contained in a component
                int first = -1, high = 1;
                for (int n = 0; n < na; n++)
                {
                    if (graph[n] == null)
                        cc[n] = -1;
                    else if (first < 0)
                    {
                        first = n;
                        cc[n] = 1;
                    }
                }
                if (first < 0)
                    return 0; // all isolated

                while (true)
                {
                    while (first < na && (cc[first] > 0 || cc[first] < 0))
                    {
                        first++;
                    }
                    if (first >= na)
                        break;

                    bool anything = false;
                    for (int i = first; i < na; i++)
                        if (cc[i] == 0)
                        {
                            for (int j = 0; j < graph[i].Length; j++)
                            {
                                if (cc[graph[i][j]] != 0)
                                {
                                    cc[i] = cc[graph[i][j]];
                                    anything = true;
                                }
                            }
                        }
                    if (!anything)
                        cc[first] = ++high;
                }
                return high;
            }

            // convenience function for concatenating an integer
            private static int[] AppendInteger(int[] a, int v)
            {
                if (a == null || a.Length == 0)
                    return new int[] { v };
                var b = new int[a.Length + 1];
                Array.Copy(a, 0, b, 0, a.Length);
                b[a.Length] = v;
                return b;
            }

            // convenience: scans the atom adjacency to grab the bond index
            private int FindBond(int a1, int a2)
            {
                for (int n = atomAdj[a1].Length - 1; n >= 0; n--)
                    if (atomAdj[a1][n] == a2)
                        return bondAdj[a1][n];
                return -1;
            }
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
