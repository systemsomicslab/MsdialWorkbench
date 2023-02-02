/*
 * Copyright (c) 2015 John May <jwmay@users.sf.net>
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
using NCDK.Common.Mathematics;
using NCDK.Graphs;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Layout
{
    /// <summary>
    /// An overlap resolver that tries to resolve overlaps by rotating (reflecting),
    /// bending, and stretching bonds. 
    /// </summary>
    /// <remarks>
    /// <para>
    /// The RBS (rotate, bend, stretch) algorithm is first described by <token>cdk-cite-Shelley83</token>,
    /// and later in more detail by <token>cdk-cite-HEL99</token>.
    /// </para>
    /// <para>
    /// Essentially we have a measure of <see cref="Congestion"/>. From that we find 
    /// un-bonded atoms that contribute significantly (i.e. overlap). To resolve
    /// that overlap we try resolving the overlap by changing (acyclic) bonds in the
    /// shortest path between the congested pair. Operations, from most to least 
    /// favourable, are:
    /// <list type="bullet">
    ///     <item>Rotation (or reflection), <see cref="Rotate(ICollection{AtomPair})"/></item>
    ///     <item>Inversion (not described in lit), <see cref="Invert(IEnumerable{AtomPair})"/></item>
    ///     <item>Stretch, <see cref="Stretch(AtomPair, IntStack, Vector2[], Dictionary{IBond, AtomPair})"/></item>
    ///     <item>Bend, <see cref="Bend(AtomPair, IntStack, Vector2[], Dictionary{IBond, AtomPair})"/></item>
    /// </list>
    /// </para>
    /// </remarks>
    sealed class LayoutRefiner
    {
        /// <summary>
        /// These value are constants but could be parametrised in future.
        /// </summary>

        // bond length should be changeable
        private const double BondLength = 1.5;

        // Min dist between un-bonded atoms, making the denominator smaller means
        // we want to spread atoms out more
        private const double MinDistance = BondLength / 2;

        // Min score is derived from the min distance
        private const double MinScore = 1 / (MinDistance * MinDistance);

        // How much do we add to a bond when making it longer.
        private const double StrechStep = 0.32 * BondLength;

        // How much we bend bonds by
        private readonly double BendStep = Vectors.DegreeToRadian(10);

        // Ensure we don't stretch bonds too long.
        private const double MaxBondLength = 2 * BondLength;

        // Only accept if improvement is >= 2%. I don't like this because
        // huge structures will have less improvement even though the overlap
        // was resolved.
        public const double ImprovementPrecThreshold = 0.02;

        // Rotation (reflection) is always good if it improves things
        // since we're not distorting the layout. Rather than use the
        // percentage based threshold we accept an modification if
        // the improvement is this much better.
        public const int RotateDeltaThreshold = 5;

        // Maximum number of iterations whilst improving
        private const int MaxIterations = 10;

        // fast lookup structures
        private readonly IAtomContainer mol;
        private readonly Dictionary<IAtom, int> idxs;
        private readonly int[][] adjList;
        private readonly EdgeToBondMap bondMap;
        private readonly IAtom[] atoms;

        // measuring and finding congestion
        private readonly Congestion congestion;
        private readonly AllPairsShortestPaths apsp;

        // buffers where we can store and restore different solutions
        private readonly Vector2[] buffer1, buffer2, backup;
        private readonly IntStack stackBackup;
        private readonly bool[] visited;

        // ring system index, allows us to quickly tell if two atoms are
        // in the same ring system
        private readonly int[] ringsystems;

        private readonly ISet<IAtom> afix;
        private readonly ISet<IBond> bfix;

        /// <summary>
        /// Create a new layout refiner for the provided molecule.
        /// </summary>
        /// <param name="mol">molecule to refine</param>
        internal LayoutRefiner(IAtomContainer mol, ISet<IAtom> afix, ISet<IBond> bfix)
        {
            this.mol = mol;
            this.afix = afix;
            this.bfix = bfix;
            this.bondMap = EdgeToBondMap.WithSpaceFor(mol);
            this.adjList = GraphUtil.ToAdjList(mol, bondMap);
            this.idxs = new Dictionary<IAtom, int>();
            foreach (var atom in mol.Atoms)
                idxs[atom] = idxs.Count;
            this.atoms = mol.Atoms.ToArray();
            
            // buffers for storing coordinates
            this.buffer1 = new Vector2[atoms.Length];
            this.buffer2 = new Vector2[atoms.Length];
            this.backup = new Vector2[atoms.Length];
            for (int i = 0; i < buffer1.Length; i++)
            {
                buffer1[i] = new Vector2();
                buffer2[i] = new Vector2();
                backup[i] = new Vector2();
            }
            this.stackBackup = new IntStack(atoms.Length);
            this.visited = new bool[atoms.Length];

            this.congestion = new Congestion(mol, adjList);

            // note, this is lazy so only does the shortest path when needed
            // and does |V| search at maximum
            this.apsp = new AllPairsShortestPaths(mol);

            // index ring systems, idx -> ring system number (rnum)
            int rnum = 1;
            this.ringsystems = new int[atoms.Length];
            for (int i = 0; i < atoms.Length; i++)
            {
                if (atoms[i].IsInRing && ringsystems[i] == 0)
                    TraverseRing(ringsystems, i, rnum++);
            }
        }

        /// <summary>
        /// Simple method for marking ring systems with a flood-fill.
        /// </summary>
        /// <param name="ringSystem">ring system vector</param>
        /// <param name="v">start atom</param>
        /// <param name="rnum">the number to mark atoms of this ring</param>
        private void TraverseRing(int[] ringSystem, int v, int rnum)
        {
            ringSystem[v] = rnum;
            foreach (var w in adjList[v])
            {
                if (ringSystem[w] == 0 && bondMap[v, w].IsInRing)
                    TraverseRing(ringSystem, w, rnum);
            }
        }

        /// <summary>
        /// Find all pairs of un-bonded atoms that are congested.
        /// </summary>
        /// <returns>pairs of congested atoms</returns>
        List<AtomPair> FindCongestedPairs()
        {
            var pairs = new List<AtomPair>();

            // only add a single pair between each ring system, otherwise we
            // may have many pairs that are actually all part of the same
            // congestion
            var ringpairs = new HashSet<IntTuple>();

            // score at which to check for crossing bonds
            double maybeCrossed = 1 / (2 * 2);

            var numAtoms = mol.Atoms.Count;
            for (int u = 0; u < numAtoms; u++)
            {
                for (int v = u + 1; v < numAtoms; v++)
                {
                    var contribution = congestion.Contribution(u, v);
                    // <0 = bonded
                    if (contribution <= 0)
                        continue;

                    // we don't modify ring bonds with the class to when the atoms
                    // same ring systems we can't reduce the congestion
                    if (ringsystems[u] > 0 && ringsystems[u] == ringsystems[v])
                        continue;

                    // an un-bonded atom pair is congested if they're and with a certain distance
                    // or any of their bonds are crossing
                    if (contribution >= MinScore || contribution >= maybeCrossed && HaveCrossingBonds(u, v))
                    {

                        int uWeight = mol.Atoms[u].GetProperty<int>(AtomPlacer.Priority);
                        int vWeight = mol.Atoms[v].GetProperty<int>(AtomPlacer.Priority);

                        int[] path = uWeight > vWeight ? apsp.From(u).GetPathTo(v)
                                                       : apsp.From(v).GetPathTo(u);

                        // something not right here if the len is < 3
                        int len = path.Length;
                        if (len < 3) continue;

                        // build the seqAt and bndAt lists from shortest path
                        int[] seqAt = new int[len - 2];
                        IBond[] bndAt = new IBond[len - 1];
                        MakeAtmBndQueues(path, seqAt, bndAt);

                        // we already know about this collision between these ring systems
                        // so dont add the pair
                        if (ringsystems[u] > 0 && ringsystems[v] > 0 &&
                                !ringpairs.Add(new IntTuple(ringsystems[u], ringsystems[v])))
                            continue;

                        // add to pairs to overlap
                        pairs.Add(new AtomPair(u, v, seqAt, bndAt));
                    }
                }
            }

            // sort the pairs to attempt consistent overlap resolution (order independent)
            pairs.Sort(new AtomPairComparer(this));

            return pairs;
        }

        class AtomPairComparer : IComparer<AtomPair>
        {
            LayoutRefiner parent;
            public AtomPairComparer(LayoutRefiner parent)
            {
                this.parent = parent;
            }

            public int Compare(AtomPair a, AtomPair b)
            {
                int a1 = parent.atoms[a.fst].GetProperty<int>(AtomPlacer.Priority);
                int a2 = parent.atoms[a.snd].GetProperty<int>(AtomPlacer.Priority);
                int b1 = parent.atoms[b.fst].GetProperty<int>(AtomPlacer.Priority);
                int b2 = parent.atoms[b.snd].GetProperty<int>(AtomPlacer.Priority);
                int amin, amax;
                int bmin, bmax;
                if (a1 < a2)
                {
                    amin = a1;
                    amax = a2;
                }
                else
                {
                    amin = a2;
                    amax = a1;
                }
                if (b1 < b2)
                {
                    bmin = a1;
                    bmax = a2;
                }
                else
                {
                    bmin = a2;
                    bmax = a1;
                }
                int cmp = amin.CompareTo(bmin);
                if (cmp != 0) return cmp;
                return amax.CompareTo(bmax);
            }
        }

        /// <summary>
        /// Check if two bonds are crossing.
        /// </summary>
        /// <param name="beg1">first atom of first bond</param>
        /// <param name="end1">second atom of first bond</param>
        /// <param name="beg2">first atom of second bond</param>
        /// <param name="end2">first atom of second bond</param>
        /// <returns>bond is crossing</returns>
        private static bool IsCrossed(Vector2 beg1, Vector2 end1, Vector2 beg2, Vector2 end2)
        {
            return Vectors.LinesIntersect(beg1.X, beg1.Y, end1.X, end1.Y, beg2.X, beg2.Y, end2.X, end2.Y);
        }

        /// <summary>
        /// Check if any of the bonds adjacent to <paramref name="u"/>, <paramref name="v"/> (not bonded) are crossing.
        /// </summary>
        /// <param name="u">an atom (index)</param>
        /// <param name="v">another atom (index)</param>
        /// <returns>there are crossing bonds</returns>
        private bool HaveCrossingBonds(int u, int v)
        {
            int[] us = adjList[u];
            int[] vs = adjList[v];
            foreach (var u1 in us)
            {
                foreach (var v1 in vs)
                {
                    if (u1 == v || v1 == u || u1 == v1)
                        continue;
                    if (IsCrossed(atoms[u].Point2D.Value, atoms[u1].Point2D.Value, atoms[v].Point2D.Value, atoms[v1].Point2D.Value))
                        return true;
                }
            }
            return false;
        }

        /// <summary>Set of rotatable bonds we've explored and found are probably symmetric.</summary>
        private readonly HashSet<IBond> probablySymmetric = new HashSet<IBond>();

        /// <summary>
        /// Attempt to reduce congestion through rotation of flippable bonds between
        /// congest pairs.
        /// </summary>
        /// <param name="pairs">congested pairs of atoms</param>
        void Rotate(ICollection<AtomPair> pairs)
        {
            // bond has already been tried in this phase so
            // don't need to test again
            var tried = new HashSet<IBond>();

            foreach (var pair in pairs)
            {
                foreach (var bond in pair.bndAt)
                {
                    // only try each bond once per phase and skip
                    if (!tried.Add(bond))
                        continue;
                    if (bfix.Contains(bond))
                        continue;

                    // those we have found to probably be symmetric
                    if (probablySymmetric.Contains(bond))
                        continue;

                    // can't rotate these
                    if (bond.Order != BondOrder.Single || bond.IsInRing)
                        continue;

                    IAtom beg = bond.Begin;
                    IAtom end = bond.End;
                    int begIdx = idxs[beg];
                    int endIdx = idxs[end];

                    // terminal
                    if (adjList[begIdx].Length == 1 || adjList[endIdx].Length == 1)
                        continue;

                    int begPriority = beg.GetProperty<int>(AtomPlacer.Priority);
                    int endPriority = end.GetProperty<int>(AtomPlacer.Priority);

                    Arrays.Fill(visited, false);
                    if (begPriority < endPriority)
                    {
                        stackBackup.len = VisitAdj(visited, stackBackup.xs, begIdx, endIdx);

                        // avoid moving fixed atoms
                        if (afix.Any())
                        {
                            int begCnt = NumFixedMoved(stackBackup.xs, stackBackup.len);
                            if (begCnt > 0)
                            {
                                Arrays.Fill(visited, false);
                                stackBackup.len = VisitAdj(visited, stackBackup.xs, endIdx, begIdx);
                                int endCnt = NumFixedMoved(stackBackup.xs, stackBackup.len);
                                if (endCnt > 0)
                                    continue;
                            }
                        }
                    }
                    else
                    {
                        stackBackup.len = VisitAdj(visited, stackBackup.xs, endIdx, begIdx);

                        // avoid moving fixed atoms
                        if (afix.Any())
                        {
                            int endCnt = NumFixedMoved(stackBackup.xs, stackBackup.len);
                            if (endCnt > 0)
                            {
                                Arrays.Fill(visited, false);
                                stackBackup.len = VisitAdj(visited, stackBackup.xs, begIdx, endIdx);
                                int begCnt = NumFixedMoved(stackBackup.xs, stackBackup.len);
                                if (begCnt > 0)
                                    continue;
                            }
                        }
                    }

                    double min = congestion.Score();

                    BackupCoords(backup, stackBackup);
                    Reflect(stackBackup, beg, end);
                    congestion.Update(visited, stackBackup.xs, stackBackup.len);

                    double delta = min - congestion.Score();

                    // keep if decent improvement or improvement and resolves this overlap
                    if (delta > RotateDeltaThreshold ||
                        (delta > 1 && congestion.Contribution(pair.fst, pair.snd) < MinScore))
                    {
                        goto continue_Pair;
                    }
                    else
                    {
                        // almost no difference from flipping... bond is probably symmetric
                        // mark to avoid in future iterations
                        if (Math.Abs(delta) < 0.1)
                            probablySymmetric.Add(bond);

                        // restore
                        RestoreCoords(stackBackup, backup);
                        congestion.Update(visited, stackBackup.xs, stackBackup.len);
                        congestion.score = min;
                    }
                }
            }
            continue_Pair:
            ;
        }

        private int NumFixedMoved(int[] xs, int len)
        {
            int cnt = 0;
            ISet<IAtom> amoved = new HashSet<IAtom>();
            for (int i = 0; i < len; i++)
            {
                amoved.Add(mol.Atoms[xs[i]]);
            }
            foreach (IBond bond in bfix)
            {
                if (amoved.Contains(bond.Begin) && amoved.Contains(bond.End))
                    cnt++;
            }
            return cnt;
        }

        /// <summary>
        /// Special case congestion minimisation, rotate terminals bonds around ring
        /// systems so they are inside the ring.
        /// </summary>
        /// <param name="pairs">congested atom pairs</param>
        void Invert(IEnumerable<AtomPair> pairs)
        {
            foreach (var pair in pairs)
            {
                if (congestion.Contribution(pair.fst, pair.snd) < MinScore)
                    continue;
                if (FusionPointInversion(pair))
                    continue;
                if (MacroCycleInversion(pair))
                    continue;
            }
        }

        // For substituents attached to macrocycles we may be able to point these in/out
        // of the ring
        private bool MacroCycleInversion(AtomPair pair)
        {
            foreach (var v in pair.seqAt)
            {
                IAtom atom = mol.Atoms[v];
                if (!atom.IsInRing || adjList[v].Length == 2)
                    continue;
                if (atom.GetProperty<object>(MacroCycleLayout.MACROCYCLE_ATOM_HINT) == null)
                    continue;
                var acyclic = new List<IBond>(2);
                var cyclic = new List<IBond>(2);
                foreach (var w in adjList[v])
                {
                    IBond bond = bondMap[v, w];
                    if (bond.IsInRing)
                        cyclic.Add(bond);
                    else
                        acyclic.Add(bond);
                }
                if (cyclic.Count > 2)
                    continue;

                foreach (var bond in acyclic)
                {
                    if (bfix.Contains(bond))
                        continue;
                    Arrays.Fill(visited, false);
                    stackBackup.len = Visit(visited, stackBackup.xs, v, idxs[bond.GetOther(atom)], 0);

                    Vector2 a = atom.Point2D.Value;
                    Vector2 b = bond.GetOther(atom).Point2D.Value;

                    Vector2 perp = new Vector2(b.X - a.X, b.Y - a.Y);
                    perp = Vector2.Normalize(perp);
                    double score = congestion.Score();
                    BackupCoords(backup, stackBackup);

                    Reflect(stackBackup, new Vector2(a.X - perp.Y, a.Y + perp.X), new Vector2(a.X + perp.Y, a.Y - perp.X));
                    congestion.Update(visited, stackBackup.xs, stackBackup.len);

                    if (PercDiff(score, congestion.Score()) >= ImprovementPrecThreshold)
                    {
                        return true;
                    }

                    RestoreCoords(stackBackup, backup);
                }
            }
            return false;
        }

        private bool FusionPointInversion(AtomPair pair)
        {
            // not candidates for inversion
            // > 3 bonds
            if (pair.bndAt.Count != 3)
                return false;
            // we want *!@*@*!@*
            if (!pair.bndAt[0].IsInRing || pair.bndAt[1].IsInRing || pair.bndAt[2].IsInRing)
                return false;
            // non-terminals
            if (adjList[pair.fst].Length > 1 || adjList[pair.snd].Length > 1)
                return false;

            IAtom fst = atoms[pair.fst];

            // choose which one to invert, preffering hydrogens
            stackBackup.Clear();
            if (fst.AtomicNumber == 1)
                stackBackup.Push(pair.fst);
            else
                stackBackup.Push(pair.snd);

            Reflect(stackBackup, pair.bndAt[0].Begin, pair.bndAt[0].End);
            congestion.Update(stackBackup.xs, stackBackup.len);
            return true;
        }

        /// <summary>
        /// Bend all bonds in the shortest path between a pair of atoms in an attempt
        /// to resolve the overlap. The bend that produces the minimum congestion is
        /// stored in the provided stack and coords with the congestion score
        /// returned.
        /// </summary>
        /// <param name="pair">congested atom pair</param>
        /// <param name="stack">best result vertices</param>
        /// <param name="coords">best result coords</param>
        /// <param name="firstVisit">visit map to avoid repeating work</param>
        /// <returns>congestion score of best result</returns>
        private double Bend(AtomPair pair, IntStack stack, Vector2[] coords, Dictionary<IBond, AtomPair> firstVisit)
        {
            stackBackup.Clear();

            Trace.Assert(stack.len == 0);
            double score = congestion.Score();
            double min = score;

            // special case: if we have an even length path where the two
            // most central bonds are cyclic but the next two aren't we bend away
            // from each other
            if (pair.bndAt.Count > 4 && (pair.bndAtCode & 0x1F) == 0x6)
            {
                var bndA = pair.bndAt[2];
                var bndB = pair.bndAt[3];

                if (bfix.Contains(bndA) || bfix.Contains(bndB))
                    return int.MaxValue;

                var pivotA = GetCommon(bndA, pair.bndAt[1]);
                var pivotB = GetCommon(bndB, pair.bndAt[0]);

                if (pivotA == null || pivotB == null)
                    return int.MaxValue;

                Arrays.Fill(visited, false);
                int split = Visit(visited, stack.xs, idxs[pivotA], idxs[bndA.GetOther(pivotA)], 0);
                stack.len = Visit(visited, stack.xs, idxs[pivotB], idxs[bndB.GetOther(pivotB)], split);

                // perform bend one way
                BackupCoords(backup, stack);
                Bend(stack.xs, 0, split, pivotA, BendStep);
                Bend(stack.xs, split, stack.len, pivotB, -BendStep);

                congestion.Update(stack.xs, stack.len);

                if (PercDiff(score, congestion.Score()) >= ImprovementPrecThreshold)
                {
                    BackupCoords(coords, stack);
                    stackBackup.CopyFrom(stack);
                    min = congestion.Score();
                }

                // now bend the other way
                RestoreCoords(stack, backup);
                Bend(stack.xs, 0, split, pivotA, -BendStep);
                Bend(stack.xs, split, stack.len, pivotB, BendStep);
                congestion.Update(stack.xs, stack.len);
                if (PercDiff(score, congestion.Score()) >= ImprovementPrecThreshold && congestion.Score() < min)
                {
                    BackupCoords(coords, stack);
                    stackBackup.CopyFrom(stack);
                    min = congestion.Score();
                }

                // restore original coordinates and reset score
                RestoreCoords(stack, backup);
                congestion.Update(stack.xs, stack.len);
                congestion.score = score;
            }
            // general case: try bending acyclic bonds in the shortest
            // path from inside out
            else
            {
                // try bending all bonds and accept the best one
                foreach (var bond in pair.bndAt)
                {
                    if (bond.IsInRing)
                        continue;
                    if (bfix.Contains(bond))
                        continue;

                    // has this bond already been tested as part of another pair
                    if (!firstVisit.TryGetValue(bond, out AtomPair first))
                        firstVisit[bond] = first = pair;
                    if (first != pair)
                        continue;

                    var beg = bond.Begin;
                    var end = bond.End;
                    var begPriority = beg.GetProperty<int>(AtomPlacer.Priority);
                    var endPriority = end.GetProperty<int>(AtomPlacer.Priority);

                    Arrays.Fill(visited, false);
                    if (begPriority < endPriority)
                        stack.len = Visit(visited, stack.xs, idxs[beg], idxs[end], 0);
                    else
                        stack.len = Visit(visited, stack.xs, idxs[end], idxs[beg], 0);

                    BackupCoords(backup, stack);

                    // bend one way
                    if (begPriority < endPriority)
                        Bend(stack.xs, 0, stack.len, beg, pair.attempt * BendStep);
                    else
                        Bend(stack.xs, 0, stack.len, end, pair.attempt * BendStep);
                    congestion.Update(visited, stack.xs, stack.len);

                    if (PercDiff(score, congestion.Score()) >= ImprovementPrecThreshold &&
                            congestion.Score() < min)
                    {
                        BackupCoords(coords, stack);
                        stackBackup.CopyFrom(stack);
                        min = congestion.Score();
                    }

                    // bend other way
                    if (begPriority < endPriority)
                        Bend(stack.xs, 0, stack.len, beg, pair.attempt * -BendStep);
                    else
                        Bend(stack.xs, 0, stack.len, end, pair.attempt * -BendStep);
                    congestion.Update(visited, stack.xs, stack.len);

                    if (PercDiff(score, congestion.Score()) >= ImprovementPrecThreshold && congestion.Score() < min)
                    {
                        BackupCoords(coords, stack);
                        stackBackup.CopyFrom(stack);
                        min = congestion.Score();
                    }

                    RestoreCoords(stack, backup);
                    congestion.Update(visited, stack.xs, stack.len);
                    congestion.score = score;
                }
            }

            stack.CopyFrom(stackBackup);

            return min;
        }

        /// <summary>
        /// Stretch all bonds in the shortest path between a pair of atoms in an
        /// attempt to resolve the overlap. The stretch that produces the minimum
        /// congestion is stored in the provided stack and coordinates with the congestion
        /// score returned.
        /// </summary>
        /// <param name="pair">congested atom pair</param>
        /// <param name="stack">best result vertices</param>
        /// <param name="coords">best result coordinates</param>
        /// <param name="firstVisit">visit map to avoid repeating work</param>
        /// <returns>congestion score of best result</returns>
        private double Stretch(AtomPair pair, IntStack stack, Vector2[] coords, Dictionary<IBond, AtomPair> firstVisit)
        {
            stackBackup.Clear();

            var score = congestion.Score();
            var min = score;

            foreach (var bond in pair.bndAt)
            {
                // don't stretch ring bonds
                if (bond.IsInRing)
                    continue;
                if (bfix.Contains(bond))
                    continue;

                // has this bond already been tested as part of another pair
                if (!firstVisit.TryGetValue(bond, out AtomPair first))
                    firstVisit[bond] = first = pair;
                if (first != pair)
                    continue;

                var beg = bond.Begin;
                var end = bond.End;
                var begIdx = idxs[beg];
                var endIdx = idxs[end];
                var begPriority = beg.GetProperty<int>(AtomPlacer.Priority);
                var endPriority = end.GetProperty<int>(AtomPlacer.Priority);

                Arrays.Fill(visited, false);
                if (begPriority < endPriority)
                    stack.len = Visit(visited, stack.xs, endIdx, begIdx, 0);
                else
                    stack.len = Visit(visited, stack.xs, begIdx, endIdx, 0);

                BackupCoords(backup, stack);
                if (begPriority < endPriority)
                    Stretch(stack, end, beg, pair.attempt * StrechStep);
                else
                    Stretch(stack, beg, end, pair.attempt * StrechStep);

                congestion.Update(visited, stack.xs, stack.len);

                if (PercDiff(score, congestion.Score()) >= ImprovementPrecThreshold && congestion.Score() < min)
                {
                    BackupCoords(coords, stack);
                    min = congestion.Score();
                    stackBackup.CopyFrom(stack);
                }

                RestoreCoords(stack, backup);
                congestion.Update(visited, stack.xs, stack.len);
                congestion.score = score;
            }

            stack.CopyFrom(stackBackup);

            return min;
        }

        /// <summary>
        /// Resolves conflicts either by bending bonds or stretching bonds in the
        /// shortest path between an overlapping pair. Bending and stretch are tried
        /// for each pair and the best resolution is used.
        /// </summary>
        /// <param name="pairs">pairs</param>
        private void BendOrStretch(IEnumerable<AtomPair> pairs)
        {
            // without checking which bonds have been bent/stretch already we
            // could end up repeating a lot of repeated work to no avail
            var bendVisit = new Dictionary<IBond, AtomPair>();
            var stretchVisit = new Dictionary<IBond, AtomPair>();

            var bendStack = new IntStack(atoms.Length);
            var stretchStack = new IntStack(atoms.Length);

            foreach (var pair in pairs)
            {
                double score = congestion.Score();

                // each attempt will be more aggressive/distorting
                for (pair.attempt = 1; pair.attempt <= 3; pair.attempt++)
                {
                    bendStack.Clear();
                    stretchStack.Clear();

                    // attempt both bending and stretching storing the
                    // best result in the provided buffer
                    var bendScore = Bend(pair, bendStack, buffer1, bendVisit);
                    var stretchScore = Stretch(pair, stretchStack, buffer2, stretchVisit);

                    // bending is better than stretching
                    if (bendScore < stretchScore && bendScore < score)
                    {
                        RestoreCoords(bendStack, buffer1);
                        congestion.Update(bendStack.xs, bendStack.len);
                        break;
                    }
                    // stretching is better than bending
                    else if (bendScore > stretchScore && stretchScore < score)
                    {
                        RestoreCoords(stretchStack, buffer2);
                        congestion.Update(stretchStack.xs, stretchStack.len);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Refine the 2D coordinates of a layout to reduce overlap and congestion.
        /// </summary>
        public void Refine()
        {
            for (int i = 1; i <= MaxIterations; i++)
            {
                var pairs = FindCongestedPairs();

                if (pairs.Count == 0)
                    break;

                var min = congestion.Score();

                // rotation: flipping around sigma bonds
                Rotate(pairs);

                // rotation improved, so try more rotation, we may have caused
                // new conflicts that can be resolved through more rotations
                if (congestion.Score() < min)
                    continue;

                // inversion: terminal atoms can be placed inside rings
                // which is preferable to bending or stretching
                Invert(pairs);

                if (congestion.Score() < min)
                    continue;

                // bending or stretching: least favourable but sometimes
                // the only way. We try either and use the best
                BendOrStretch(pairs);

                if (congestion.Score() < min)
                    continue;

                break;
            }
        }

        /// <summary>
        /// Backup the coordinates of atoms (indexes) in the stack to the provided
        /// destination.
        /// </summary>
        /// <param name="dest">destination</param>
        /// <param name="stack">atom indexes to backup</param>
        private void BackupCoords(Vector2[] dest, IntStack stack)
        {
            for (int i = 0; i < stack.len; i++)
            {
                var v = stack.xs[i];
                dest[v] = new Vector2(atoms[v].Point2D.Value.X, atoms[v].Point2D.Value.Y);
            }
        }

        /// <summary>
        /// Restore the coordinates of atoms (indexes) in the stack to the provided
        /// source.
        /// </summary>
        /// <param name="stack">atom indexes to backup</param>
        /// <param name="src">source of coordinates</param>
        private void RestoreCoords(IntStack stack, Vector2[] src)
        {
            for (int i = 0; i < stack.len; i++)
            {
                var v = stack.xs[i];
                atoms[v].Point2D = new Vector2(src[v].X, src[v].Y);
            }
        }

        /// <summary>
        /// Reflect all atoms (indexes) int he provided stack around the line formed
        /// of the beg and end atoms.
        /// </summary>
        /// <param name="stack">atom indexes to reflect</param>
        /// <param name="beg">beg atom of a bond</param>
        /// <param name="end">end atom of a bond</param>
        private void Reflect(IntStack stack, IAtom beg, IAtom end)
        {
            var begP = beg.Point2D.Value;
            var endP = end.Point2D.Value;
            Reflect(stack, begP, endP);
        }

        private void Reflect(IntStack stack, Vector2 begP, Vector2 endP)
        {
            double dx = endP.X - begP.X;
            double dy = endP.Y - begP.Y;

            double a = (dx * dx - dy * dy) / (dx * dx + dy * dy);
            double b = 2 * dx * dy / (dx * dx + dy * dy);

            for (int i = 0; i < stack.len; i++)
            {
                Reflect(atoms[stack.xs[i]], begP, a, b);
            }
        }

        /// <summary>
        /// Reflect a point (p) in a line formed of <paramref name="baseOfSource"/>, <paramref name="a"/>, and <paramref name="b"/>.
        /// </summary>
        /// <param name="ap">point to reflect</param>
        /// <param name="baseOfSource">base of the refection source</param>
        /// <param name="a">a reflection coefficient</param>
        /// <param name="b">b reflection coefficient</param>
        private static void Reflect(IAtom ap, Vector2 baseOfSource, double a, double b)
        {
            var x = a * (ap.Point2D.Value.X - baseOfSource.X) + b * (ap.Point2D.Value.Y - baseOfSource.Y) + baseOfSource.X;
            var y = b * (ap.Point2D.Value.X - baseOfSource.X) - a * (ap.Point2D.Value.Y - baseOfSource.Y) + baseOfSource.Y;
            ap.Point2D = new Vector2(x, y);
        }

        /// <summary>
        /// Bend select atoms around a provided pivot by the specified amount (r).
        /// </summary>
        /// <param name="indexes">array of atom indexes</param>
        /// <param name="from">start offset into the array (inclusive)</param>
        /// <param name="to">end offset into the array (exclusive)</param>
        /// <param name="pivotAtm">the point about which we are pivoting</param>
        /// <param name="r">radians to bend by</param>
        private void Bend(int[] indexes, int from, int to, IAtom pivotAtm, double r)
        {
            var s = Math.Sin(r);
            var c = Math.Cos(r);
            var pivot = pivotAtm.Point2D.Value;
            for (int i = from; i < to; i++)
            {
                var atom = mol.Atoms[indexes[i]];
                var p = atom.Point2D.Value;
                var x = p.X - pivot.X;
                var y = p.Y - pivot.Y;
                var nx = x * c + y * s;
                var ny = -x * s + y * c;
                atom.Point2D = new Vector2(nx + pivot.X, ny + pivot.Y);
            }
        }

        /// <summary>
        /// Stretch the distance between beg and end, moving all atoms provided in
        /// the stack.
        /// </summary>
        /// <param name="stack">atoms to be moved</param>
        /// <param name="beg">begin atom of a bond</param>
        /// <param name="end">end atom of a bond</param>
        /// <param name="amount">amount to try stretching by (absolute)</param>
        private void Stretch(IntStack stack, IAtom beg, IAtom end, double amount)
        {
            var begPoint = beg.Point2D.Value;
            var endPoint = end.Point2D.Value;

            if (Vector2.Distance(begPoint, endPoint) + amount > MaxBondLength)
                return;

            var vector = new Vector2(endPoint.X - begPoint.X, endPoint.Y - begPoint.Y);
            vector = Vector2.Normalize(vector);
            vector *= amount;

            for (int i = 0; i < stack.len; i++)
            {
                var atom = atoms[stack.xs[i]];
                atom.Point2D = atom.Point2D.Value + vector;
            }
        }

        /// <summary>
        /// Internal - makes atom (seq) and bond priority queues for resolving
        /// overlap. Only (acyclic - but not really) atoms and bonds in the shortest
        /// path between the two atoms can resolve an overlap. We create prioritised
        /// sequences of atoms/bonds where the more central in the shortest path.
        /// </summary>
        /// <param name="path">shortest path between atoms</param>
        /// <param name="seqAt">prioritised atoms, first atom is the middle of the path</param>
        /// <param name="bndAt">prioritised bonds, first bond is the middle of the path</param>
        private void MakeAtmBndQueues(int[] path, int[] seqAt, IBond[] bndAt)
        {
            var len = path.Length;
            var i = (len - 1) / 2;
            var j = i + 1;
            int nSeqAt = 0;
            int nBndAt = 0;
            if (IsOdd((path.Length)))
            {
                seqAt[nSeqAt++] = path[i--];
                bndAt[nBndAt++] = bondMap[path[j], path[j - 1]];
            }
            bndAt[nBndAt++] = bondMap[path[i], path[i + 1]];
            while (i > 0 && j < len - 1)
            {
                seqAt[nSeqAt++] = path[i--];
                seqAt[nSeqAt++] = path[j++];
                bndAt[nBndAt++] = bondMap[path[i], path[i + 1]];
                bndAt[nBndAt++] = bondMap[path[j], path[j - 1]];
            }
        }

        // is a number odd
        private static bool IsOdd(int len)
        {
            return (len & 0x1) != 0;
        }

        // percentage difference
        private static double PercDiff(double prev, double curr)
        {
            return (prev - curr) / prev;
        }

        /// <summary>
        /// Recursively visit <paramref name="v"/> and all vertices adjacent to it (excluding <paramref name="p"/>)
        /// adding all except <paramref name="v"/> to the result array.
        /// </summary>
        /// <param name="visited">visit flags array, should be cleared before search</param>
        /// <param name="result">visited vertices</param>
        /// <param name="p">previous vertex</param>
        /// <param name="v">start vertex</param>
        /// <returns>number of visited vertices</returns>
        private int VisitAdj(bool[] visited, int[] result, int p, int v)
        {
            int n = 0;
            Arrays.Fill(visited, false);
            visited[v] = true;
            foreach (var w in adjList[v])
            {
                if (w != p && !visited[w])
                {
                    n = Visit(visited, result, v, w, n);
                }
            }
            visited[v] = false;
            return n;
        }

        /// <summary>
        /// Recursively visit <paramref name="v"/> and all vertices adjacent to it (excluding <paramref name="p"/>)
        /// adding them to the result array.
        /// </summary>
        /// <param name="visited">visit flags array, should be cleared before search</param>
        /// <param name="result">visited vertices</param>
        /// <param name="p">previous vertex</param>
        /// <param name="v">start vertex</param>
        /// <param name="n">current number of visited vertices</param>
        /// <returns>new number of visited vertices</returns>
        private int Visit(bool[] visited, int[] result, int p, int v, int n)
        {
            visited[v] = true;
            result[n++] = v;
            foreach (var w in adjList[v])
            {
                if (w != p && !visited[w])
                {
                    n = Visit(visited, result, v, w, n);
                }
            }
            return n;
        }

        /// <summary>
        /// Access the common atom shared by two bonds.
        /// </summary>
        /// <param name="bndA">first bond</param>
        /// <param name="bndB">second bond</param>
        /// <returns>common atom or null if non exists</returns>
        private static IAtom GetCommon(IBond bndA, IBond bndB)
        {
            var beg = bndA.Begin;
            var end = bndA.End;
            if (bndB.Contains(beg))
                return beg;
            else if (bndB.Contains(end))
                return end;
            else
                return null;
        }

        /// <summary>
        /// Congested pair of un-bonded atoms, described by the index of the atoms
        /// (<see cref="fst"/>, <see cref="snd"/>). The atoms (<see cref="seqAt"/>) and bonds (<see cref="bndAt"/>) in the shortest path
        /// between the pair are stored as well as a bndAtCode for checking special
        /// case ring bond patterns.
        /// </summary>
        sealed class AtomPair
        {
            internal readonly int fst, snd;
            internal readonly int[] seqAt;
            internal readonly IList<IBond> bndAt;
            internal readonly int bndAtCode;

            /// <summary>
            /// Which attempt are we trying to resolve this overlap with.
            /// </summary>
            public int attempt = 1;

            public AtomPair(int fst, int snd, int[] seqAt, IList<IBond> bndAt)
            {
                this.fst = fst;
                this.snd = snd;
                this.seqAt = seqAt;
                this.bndAt = bndAt;
                this.bndAtCode = BondCode(bndAt);
            }

            public override bool Equals(object o)
            {
                if (this == o)
                    return true;
                if (o == null || GetType() != o.GetType())
                    return false;

                var pair = (AtomPair)o;

                return (fst == pair.fst && snd == pair.snd) || (fst == pair.snd && snd == pair.fst);
            }

            public override int GetHashCode()
            {
                return fst ^ snd;
            }

            /// <summary>
            /// Create the bond code bit mask, lowest bit is whether the path is
            /// odd/even then the other bits are whether the bonds are in a ring or
            /// not.
            /// </summary>
            /// <param name="enumBonds">bonds to encode</param>
            /// <returns>the bond code</returns>
            static int BondCode(IEnumerable<IBond> enumBonds)
            {
                var bonds = enumBonds.ToReadOnlyList();
                var code = bonds.Count & 0x1;
                for (int i = 0; i < bonds.Count; i++)
                {
                    if (bonds[i].IsInRing)
                    {
                        code |= 0x1 << (i + 1);
                    }
                }
                return code;
            }
        }

        /// <summary>
        /// Internal - fixed size integer stack.
        /// </summary>
        private sealed class IntStack
        {
            internal readonly int[] xs;
            internal int len;

            public IntStack(int cap)
            {
                this.xs = new int[cap];
            }

            public void Push(int x)
            {
                xs[len++] = x;
            }

            public void Clear()
            {
                this.len = 0;
            }

            public void CopyFrom(IntStack stack)
            {
                Array.Copy(stack.xs, 0, xs, 0, stack.len);
                this.len = stack.len;
            }

            public override string ToString()
            {
                return Arrays.ToJavaString(Arrays.CopyOf(xs, len));
            }
        }

        /// <summary>
        /// Internal - A hashable tuple of integers, allows to check for previously
        /// seen pairs.
        /// </summary>
        private sealed class IntTuple
        {
            private readonly int fst, snd;

            public IntTuple(int fst, int snd)
            {
                this.fst = fst;
                this.snd = snd;
            }

            public override bool Equals(Object o)
            {
                if (this == o)
                    return true;
                if (o == null || GetType() != o.GetType())
                    return false;

                var that = (IntTuple)o;

                return (this.fst == that.fst && this.snd == that.snd)
                    || (this.fst == that.snd && this.snd == that.fst);
            }

            public override int GetHashCode()
            {
                return fst ^ snd;
            }
        }
    }
}
