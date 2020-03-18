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

using NCDK.Graphs;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NCDK.Numerics;

namespace NCDK.Layout
{
    /// <summary>
    /// A class for helping layout macrocycles.
    /// </summary>
    sealed class MacroCycleLayout
    {
        // Macrocycle templates
        private static IdentityTemplateLibrary TEMPLATES = IdentityTemplateLibrary.LoadFromResource("macro.smi");

        // Hint for placing substituents
        public static string MACROCYCLE_ATOM_HINT = "layout.macrocycle.atom.hint";

        // (counter)clockwise
        private const int CW = -1;
        private const int CCW = +1;

        // molecule representations
        private readonly IAtomContainer mol;
        private readonly int[][] adjList;
        private readonly Dictionary<IAtom, int> idxs = new Dictionary<IAtom, int>();

        /// <summary>
        /// Create a new helper for the provided molecule.
        /// </summary>
        /// <param name="mol">molecule</param>
        public MacroCycleLayout(IAtomContainer mol)
        {
            this.mol = mol;
            this.adjList = GraphUtil.ToAdjList(mol);
            foreach (var atom in mol.Atoms)
                idxs[atom] = idxs.Count;
        }

        /// <summary>
        /// Layout a macro cycle (the rest of the ring set is untouched).
        /// </summary>
        /// <param name="macrocycle">the macrocycle</param>
        /// <param name="ringset">the ring set the macrocycle belongs to (may only be it's self)</param>
        /// <returns>layout was successfully, if false caller fall-back to regular polygons</returns>
        public static bool Layout(IRing macrocycle, IRingSet ringset)
        {
            var anon = RoundUpIfNeeded(AtomContainerManipulator.Anonymise(macrocycle));
            var coords = TEMPLATES.GetCoordinates(anon);

            if (coords.Count == 0)
                return false;

            var best = new Vector2[anon.Atoms.Count];
            var bestOffset = SelectCoords(coords, best, macrocycle, ringset);

            for (int i = 0; i < macrocycle.Atoms.Count; i++)
            {
                macrocycle.Atoms[i].Point2D = best[(bestOffset + i) % macrocycle.Atoms.Count];
                macrocycle.Atoms[i].IsPlaced = true;
                macrocycle.Atoms[i].SetProperty(MACROCYCLE_ATOM_HINT, true);
            }
            macrocycle.IsPlaced = true;

            return true;
        }

        /// <summary>
        /// Select the best scoring template + offset for the given macrocycle.
        /// </summary>
        /// <param name="macrocycle">macrocycle</param>
        /// <param name="ringset">entire ring system</param>
        /// <param name="wind">winding of ring CW/CCW</param>
        /// <param name="winding">winding of each turn in the ring</param>
        /// <returns>the best scoring configuration</returns>
        private static MacroScore BestScore(IRing macrocycle, IRingSet ringset, int wind, int[] winding)
        {
            var numAtoms = macrocycle.Atoms.Count;

            var heteroIdxs = new List<int>();
            var ringAttachs = new List<IList<int>>();

            // hetero atoms
            for (int i = 0; i < numAtoms; i++)
            {
                if (macrocycle.Atoms[i].AtomicNumber != 6)
                    heteroIdxs.Add(i);
            }
            foreach (var other in ringset)
            {
                if (other == macrocycle)
                    continue;
                var shared = AtomContainerManipulator.GetIntersection(macrocycle, other);

                if (shared.Atoms.Count >= 2 && shared.Atoms.Count <= 4)
                    ringAttachs.Add(GetAttachedInOrder(macrocycle, shared));
            }

            // convex and concave are relative
            var convex = wind;
            var concave = -wind;

            MacroScore best = null;

            for (int i = 0; i < winding.Length; i++)
            {
                // score ring attachs
                int nRingClick = 0;
                foreach (var ringAttach in ringAttachs)
                {
                    int r1, r2, r3, r4;
                    switch (ringAttach.Count)
                    {
                        case 2:
                            r1 = (ringAttach[0] + i) % numAtoms;
                            r2 = (ringAttach[1] + i) % numAtoms;
                            if (winding[r1] == winding[r2])
                            {
                                if (winding[r1] == convex)
                                    nRingClick += 5;
                                else
                                    nRingClick++;
                            }
                            break;
                        case 3:
                            r1 = (ringAttach[0] + i) % numAtoms;
                            r2 = (ringAttach[1] + i) % numAtoms;
                            r3 = (ringAttach[2] + i) % numAtoms;
                            if (winding[r1] == convex &&
                                winding[r2] == concave &&
                                winding[r3] == convex)
                                nRingClick += 5;
                            else if (winding[r1] == concave &&
                                     winding[r2] == convex &&
                                     winding[r3] == concave)
                                nRingClick++;
                            break;
                        case 4:
                            r1 = (ringAttach[0] + i) % numAtoms;
                            r2 = (ringAttach[1] + i) % numAtoms;
                            r3 = (ringAttach[2] + i) % numAtoms;
                            r4 = (ringAttach[3] + i) % numAtoms;
                            if (winding[r1] == convex &&
                                winding[r2] == concave &&
                                winding[r3] == concave &&
                                winding[r4] == convex)
                                nRingClick++;
                            else if (winding[r1] == concave &&
                                     winding[r2] == convex &&
                                     winding[r3] == convex &&
                                     winding[r4] == concave)
                                nRingClick++;
                            break;
                    }
                }

                // score hetero atoms in concave positions
                int nConcaveHetero = 0;
                foreach (var heteroIdx in heteroIdxs)
                {
                    var k = (heteroIdx + i) % numAtoms;
                    if (winding[k] == concave)
                        nConcaveHetero++;
                }

                int nCorrectStereo = 0;
                int nIncorrectStereo = 0;
                foreach (var se in macrocycle.StereoElements)
                {
                    if (se.Class == StereoClass.CisTrans)
                    {
                        var bond = (IBond)se.Focus;
                        var beg = bond.Begin;
                        var end = bond.End;
                        StereoConfigurations cfg;
                        if (winding[(macrocycle.Atoms.IndexOf(beg) + i) % numAtoms] ==
                            winding[(macrocycle.Atoms.IndexOf(end) + i) % numAtoms])
                        {
                            cfg = StereoConfigurations.Together;
                        }
                        else
                        {
                            cfg = StereoConfigurations.Opposite;
                        }
                        if (cfg == se.Configure)
                        {
                            nCorrectStereo++;
                        }
                        else
                        {
                            nIncorrectStereo++;
                        }
                    }
                }

                var score = new MacroScore(i, nConcaveHetero, nCorrectStereo, nRingClick);
                if (score.CompareTo(best) < 0)
                {
                    best = score;
                }
            }

            return best;
        }

        /// <summary>
        /// Get the shared indices of a macrocycle and atoms shared with another ring.
        /// </summary>
        /// <param name="macrocycle">macrocycle ring</param>
        /// <param name="shared">shared atoms</param>
        /// <returns>the integers</returns>
        private static List<int> GetAttachedInOrder(IRing macrocycle, IAtomContainer shared)
        {
            var ringAttach = new List<int>();
            var visit = new HashSet<IAtom>();
            var atom = shared.Atoms[0];
            while (atom != null)
            {
                visit.Add(atom);
                ringAttach.Add(macrocycle.Atoms.IndexOf(atom));
                var connected = shared.GetConnectedAtoms(atom);
                atom = null;
                foreach (var neighbor in connected)
                {
                    if (!visit.Contains(neighbor))
                    {
                        atom = neighbor;
                        break;
                    }
                }
            }
            return ringAttach;
        }

        /// <summary>
        /// Select the best coordinates
        /// </summary>
        /// <param name="ps">template points</param>
        /// <param name="coords">best coordinates (updated by this method)</param>
        /// <param name="macrocycle">the macrocycle</param>
        /// <param name="ringset">rest of the ring system</param>
        /// <returns>offset into the coordinates</returns>
        private static int SelectCoords(IEnumerable<Vector2[]> ps, Vector2[] coords, IRing macrocycle, IRingSet ringset)
        {
            Debug.Assert(ps.Any());
            var winding = new int[coords.Length];

            MacroScore best = null;
            foreach (var p in ps)
            {
                var wind = Winding(p, winding);
                var score = BestScore(macrocycle, ringset, wind, winding);
                if (score.CompareTo(best) < 0)
                {
                    best = score;
                    Array.Copy(p, 0, coords, 0, p.Length);
                }
            }

            // never null
            return best != null ? best.offset : 0;
        }

        /// <summary>
        /// Determine the overall winding and the vertex of a ring template.
        /// </summary>
        /// <param name="coords">ring coordinates</param>
        /// <param name="winding">winding result for each atom (cw/ccw)</param>
        /// <returns>global winding</returns>
        private static int Winding(Vector2[] coords, int[] winding)
        {
            int cw = 0, ccw = 0;

            var prev = coords[coords.Length - 1];
            for (int i = 0; i < coords.Length; i++)
            {
                var curr = coords[i];
                var next = coords[(i + 1) % coords.Length];
                winding[i] = Winding(prev, curr, next);

                if (winding[i] < 0)
                    cw++;
                else if (winding[i] > 0)
                    ccw++;
                else
                    return 0;

                prev = curr;
            }

            if (cw == ccw)
                return 0;

            return cw > ccw ? CW : CCW;
        }

        /// <summary>
        /// Determine the winding of three points using the determinant.
        /// </summary>
        /// <param name="a">first point</param>
        /// <param name="b">second point</param>
        /// <param name="c">third point</param>
        /// <returns>&lt;0 =&gt; clockwise, 0 =&gt; linear, &gt;0 anti-clockwise</returns>
        private static int Winding(Vector2 a, Vector2 b, Vector2 c)
        {
            return (int)Math.Sign((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X));
        }

        /// <summary>
        /// Helper class for storing/ranking macrocycle templates.
        /// </summary>
        private sealed class MacroScore : IComparable<MacroScore>
        {
            public readonly int offset;
            readonly int nConcaveHetero;
            readonly int nRingClick;
            readonly int nCorrectStereo;

            public MacroScore(int offset, int nConcaveHetero, int nCorrectStereo, int nRingClick)
            {
                this.offset = offset;
                this.nConcaveHetero = nConcaveHetero;
                this.nRingClick = nRingClick;
                this.nCorrectStereo = nCorrectStereo;
            }

            public int CompareTo(MacroScore o)
            {
                if (o == null)
                    return -1;
                int cmp = 0;
                cmp = -this.nRingClick.CompareTo(o.nRingClick);
                if (cmp != 0)
                    return cmp;
                cmp = -this.nCorrectStereo.CompareTo(o.nCorrectStereo);
                if (cmp != 0)
                    return cmp;
                cmp = -this.nConcaveHetero.CompareTo(o.nConcaveHetero);
                return cmp;
            }
        }

        /// <summary>
        /// Make a ring one atom bigger if it's of an odd size.
        /// </summary>
        /// <param name="anon">ring</param>
        /// <returns>'anon' returned of chaining convenience</returns>
        private static IAtomContainer RoundUpIfNeeded(IAtomContainer anon)
        {
            var bldr = anon.Builder;
            if ((anon.Atoms.Count & 0x1) != 0)
            {
                var bond = anon.Bonds[anon.Bonds.Count - 1];
                anon.Bonds.RemoveAt(anon.Bonds.Count - 1);
                var dummy = bldr.NewAtom("C");
                anon.Atoms.Add(dummy);
                anon.Bonds.Add(bldr.NewBond(bond.Begin, dummy, BondOrder.Single));
                anon.Bonds.Add(bldr.NewBond(dummy, bond.End, BondOrder.Single));
            }
            return anon;
        }
    }
}
