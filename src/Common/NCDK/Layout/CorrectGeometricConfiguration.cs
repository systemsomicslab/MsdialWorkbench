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
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using NCDK.Graphs;
using NCDK.Numerics;
using NCDK.RingSearches;
using NCDK.Stereo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Layout
{
    /// <summary>
    /// Correct double-bond configuration depiction in 2D to be correct for it's
    /// specified <see cref="IDoubleBondStereochemistry"/>. Ideally double-bond adjustment
    /// should be done in when generating a structure diagram (and consider
    /// overlaps). This method finds double bonds with incorrect depicted
    /// configuration and reflects one side to correct the configuration.
    /// </summary>
    /// <remarks>
    /// <note type="important">
    /// Should be invoked before labelling up/down bonds. Cyclic
    /// double-bonds with a configuration can not be corrected (error logged).
    /// </note>
    /// </remarks>
    // @author John May
    // @cdk.module sdg
    internal sealed class CorrectGeometricConfiguration
    {
        /// <summary>The structure we are assigning labels to.</summary>
        private readonly IAtomContainer container;

        /// <summary>Adjacency list graph representation of the structure.</summary>
        private readonly int[][] graph;

        /// <summary>Lookup atom index (avoid IAtomContainer).</summary>
        private readonly Dictionary<IAtom, int> atomToIndex;

        /// <summary>Test if a bond is cyclic.</summary>
        private readonly RingSearch ringSearch;

        /// <summary>Visited flags when atoms are being reflected.</summary>
        private readonly bool[] visited;

        /// <summary>
        /// Adjust all double bond elements in the provided structure. 
        /// </summary>
        /// <remarks>
        /// <note type="important">
        /// up/down labels should be adjusted before adjust double-bond
        /// configurations. coordinates are reflected by this method which can lead
        /// to incorrect tetrahedral specification.
        /// </note>
        /// </remarks>
        /// <param name="container">the structure to adjust</param>
        /// <exception cref="ArgumentException">an atom had unset coordinates</exception>
        public static IAtomContainer Correct(IAtomContainer container)
        {
            if (container.StereoElements.Any())
#pragma warning disable CA1806 // Do not ignore method results
                new CorrectGeometricConfiguration(container);
#pragma warning restore CA1806 // Do not ignore method results
            return container;
        }

        /// <summary>
        /// Adjust all double bond elements in the provided structure.
        /// </summary>
        /// <param name="container">the structure to adjust</param>
        /// <exception cref="ArgumentException">an atom had unset coordinates</exception>
        CorrectGeometricConfiguration(IAtomContainer container)
            : this(container, GraphUtil.ToAdjList(container))
        { }

        /// <summary>
        /// Adjust all double bond elements in the provided structure.
        /// </summary>
        /// <param name="container">the structure to adjust</param>
        /// <param name="graph">the adjacency list representation of the structure</param>
        /// <exception cref="ArgumentException">an atom had unset coordinates</exception>
        CorrectGeometricConfiguration(IAtomContainer container, int[][] graph)
        {
            this.container = container;
            this.graph = graph;
            this.visited = new bool[graph.Length];
            this.atomToIndex = new Dictionary<IAtom, int>();
            this.ringSearch = new RingSearch(container, graph);

            for (int i = 0; i < container.Atoms.Count; i++)
            {
                IAtom atom = container.Atoms[i];
                atomToIndex[atom] = i;
                if (atom.Point2D == null)
                    throw new ArgumentException("atom " + i + " had unset coordinates");
            }

            foreach (var element in container.StereoElements)
            {
                if (element is IDoubleBondStereochemistry)
                {
                    Adjust((IDoubleBondStereochemistry)element);
                }
                else if (element is ExtendedCisTrans) {
                    Adjust((ExtendedCisTrans)element);
                }
            }
        }

        /// <summary>
        /// Adjust the configuration of the <paramref name="dbs"/> element (if required).
        /// </summary>
        /// <param name="dbs">double-bond stereochemistry element</param>
        private void Adjust(IDoubleBondStereochemistry dbs)
        {
            var db = dbs.StereoBond;
            var bonds = dbs.Bonds;

            var left = db.Begin;
            var right = db.End;

            var p = Parity(dbs.Configure);
            var q = Parity(GetAtoms(left, bonds[0].GetOther(left), right)) 
                  * Parity(GetAtoms(right, bonds[1].GetOther(right), left));

            // configuration is unspecified? then we add an unspecified bond.
            // note: IDoubleBondStereochemistry doesn't indicate this yet
            if (p == 0)
            {
                foreach (var bond in container.GetConnectedBonds(left))
                    bond.Stereo = BondStereo.None;
                foreach (var bond in container.GetConnectedBonds(right))
                    bond.Stereo = BondStereo.None;
                bonds[0].Stereo = BondStereo.UpOrDown;
                return;
            }

            // configuration is already correct
            if (p == q)
                return;

            Arrays.Fill(visited, false);
            visited[atomToIndex[left]] = true;

            if (ringSearch.Cyclic(atomToIndex[left], atomToIndex[right]))
            {
                db.Stereo = BondStereo.EOrZ;
                return;
            }

            foreach (var w in graph[atomToIndex[right]])
            {
                if (!visited[w]) Reflect(w, db);
            }
        }

        /// <summary>
        /// Adjust the configuration of the cumulated double bonds to be
        /// either Cis or Trans.
        /// </summary>
        /// <param name="elem">the stereo element to adjust</param>
        private void Adjust(ExtendedCisTrans elem)
        {
            var middle = elem.Focus;
            var ends = ExtendedCisTrans.FindTerminalAtoms(container, middle);
            var bonds = elem.Carriers;
            var left = ends[0];
            var right = ends[1];
            var p = Parity(elem.Configure);
            var q = Parity(GetAtoms(left, bonds[0].GetOther(left), right))
                  * Parity(GetAtoms(right, bonds[1].GetOther(right), left));
            // configuration is unspecified? then we add an unspecified bond.
            // note: IDoubleBondStereochemistry doesn't indicate this yet
            if (p == 0)
            {
                foreach (var bond in container.GetConnectedBonds(left))
                    bond.Stereo = BondStereo.None;
                foreach (var bond in container.GetConnectedBonds(right))
                    bond.Stereo = BondStereo.None;
                bonds[0].Stereo = BondStereo.UpOrDown;
                return;
            }
            // configuration is already correct
            if (p == q)
                return;
            Arrays.Fill(visited, false);
            visited[atomToIndex[left]] = true;
            if (ringSearch.Cyclic(atomToIndex[middle.Begin], atomToIndex[middle.End]))
            {
                return;
            }
            foreach (var w in graph[atomToIndex[right]])
            {
                if (!visited[w])
                    Reflect(w, middle);
            }
        }

        /// <summary>
        /// Create an array of three atoms for a side of the double bond. This is
        /// used to determine the 'winding' of one side of the double bond.
        /// </summary>
        /// <param name="focus">a double bonded atom</param>
        /// <param name="substituent">the substituent we know the configuration of</param>
        /// <param name="otherFocus">the other focus (i.e. the atom focus is double bonded to)</param>
        /// <returns>3 atoms arranged as, substituent, other substituent and other
        ///         focus. if the focus atom has an implicit hydrogen the other
        ///         substituent is the focus.</returns>
        private IAtom[] GetAtoms(IAtom focus, IAtom substituent, IAtom otherFocus)
        {
            var otherSubstituent = focus;
            foreach (var w in graph[atomToIndex[focus]])
            {
                var atom = container.Atoms[w];
                if (atom != substituent && atom != otherFocus)
                    otherSubstituent = atom;
            }
            return new IAtom[] { substituent, otherSubstituent, otherFocus };
        }

        /// <summary>
        /// Access the parity (odd/even) parity of the double bond configuration (together/opposite).
        /// </summary>
        /// <param name="config">double bond element</param>
        /// <returns>together = -1, opposite = +1</returns>
        private static int Parity(StereoConfigurations config)
        {
            switch (config)
            {
                case StereoConfigurations.Together:
                    return -1;
                case StereoConfigurations.Opposite:
                    return +1;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Determine the parity (odd/even) of the triangle formed by the 3 atoms.
        /// </summary>
        /// <param name="atoms">array of 3 atoms</param>
        /// <returns>the parity of the triangle formed by 3 points, odd = -1, even = +1</returns>
        private static int Parity(IAtom[] atoms)
        {
            return Parity(atoms[0].Point2D.Value, atoms[1].Point2D.Value, atoms[2].Point2D.Value);
        }

        /// <summary>
        /// Determine the parity of the triangle formed by the 3 coordinates a, b and c.
        /// </summary>
        /// <param name="a">point 1</param>
        /// <param name="b">point 2</param>
        /// <param name="c">point 3</param>
        /// <returns>the parity of the triangle formed by 3 points</returns>
        private static int Parity(Vector2 a, Vector2 b, Vector2 c)
        {
            double det = (a.X - c.X) * (b.Y - c.Y) - (a.Y - c.Y) * (b.X - c.X);
            return Math.Sign(det);
        }

        /// <summary>
        /// Reflect the atom at index <paramref name="v"/> and any then reflect any unvisited neighbors.
        /// </summary>
        /// <param name="v">index of the atom to reflect</param>
        /// <param name="bond">bond</param>
        private void Reflect(int v, IBond bond)
        {
            visited[v] = true;
            IAtom atom = container.Atoms[v];
            atom.Point2D = Reflect(atom.Point2D.Value, bond);
            foreach (var w in graph[v])
            {
                if (!visited[w])
                    Reflect(w, bond);
            }
        }

        /// <summary>
        /// Reflect the point <paramref name="p"/> over the <paramref name="bond"/>.
        /// </summary>
        /// <param name="p">the point to reflect</param>
        /// <param name="bond">bond</param>
        /// <returns>the reflected point</returns>
        private static Vector2 Reflect(Vector2 p, IBond bond)
        {
            var a = bond.Begin;
            var b = bond.End;
            return Reflect(p, a.Point2D.Value.X, a.Point2D.Value.Y, b.Point2D.Value.X, b.Point2D.Value.Y);
        }

        /// <summary>
        /// Reflect the point <paramref name="p"/> 
        /// in the line (<paramref name="x0"/>, <paramref name="y0"/> - <paramref name="x1"/>, <paramref name="y1"/>).
        /// </summary>
        /// <param name="p">the point to reflect</param>
        /// <param name="x0">plane x start</param>
        /// <param name="y0">plane y end</param>
        /// <param name="x1">plane x start</param>
        /// <param name="y1">plane y end</param>
        /// <returns>the reflected point</returns>
        private static Vector2 Reflect(Vector2 p, double x0, double y0, double x1, double y1)
        {
            double dx, dy, a, b;

            dx = (x1 - x0);
            dy = (y1 - y0);

            a = (dx * dx - dy * dy) / (dx * dx + dy * dy);
            b = 2 * dx * dy / (dx * dx + dy * dy);

            return new Vector2(a * (p.X - x0) + b * (p.Y - y0) + x0, b * (p.X - x0) - a * (p.Y - y0) + y0);
        }
    }
}
