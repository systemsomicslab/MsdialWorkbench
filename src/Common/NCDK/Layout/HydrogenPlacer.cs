/* Copyright (C) 2003-2013 The Chemistry Development Kit (CDK) project
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

using NCDK.Geometries;
using NCDK.Numerics;
using System;
using System.Diagnostics;

namespace NCDK.Layout
{
    /// <summary>
    /// This is a wrapper class for some existing methods in AtomPlacer. It helps you
    /// to layout 2D and 3D coordinates for hydrogen atoms added to a molecule which
    /// already has coordinates for the rest of the atoms.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Layout.HydrogenPlacer_Example.cs"]/*' />
    /// </example>  
    /// <seealso cref="AtomPlacer"/>
    // @author Christoph Steinbeck
    // @cdk.created 2003-08-06
    // @cdk.module sdg
    public sealed class HydrogenPlacer
    {
        /// <summary>
        /// Place all hydrogens connected to atoms which have already been laid out.
        /// </summary>
        /// <param name="container">atom container</param>
        /// <param name="bondLength">bond length to user</param>
        public void PlaceHydrogens2D(IAtomContainer container, double bondLength)
        {
            Debug.WriteLine("placing hydrogens on all atoms");
            foreach (var atom in container.Atoms)
            {
                // only place hydrogens for atoms which have coordinates
                if (atom.Point2D != null)
                {
                    PlaceHydrogens2D(container, atom, bondLength);
                }
            }
            Debug.WriteLine("hydrogen placement complete");
        }

        /// <summary>
        /// Place hydrogens connected to the given atom using the average bond length
        /// in the container.
        /// </summary>
        /// <param name="container">atom container of which <paramref name="atom"/> is a member</param>
        /// <param name="atom">the atom of which to place connected hydrogens</param>
        /// <exception cref="ArgumentException">if the <paramref name="atom"/> does not have 2d coordinates</exception>
        /// <seealso cref="PlaceHydrogens2D(IAtomContainer, double)"/>
        public void PlaceHydrogens2D(IAtomContainer container, IAtom atom)
        {
            double bondLength = GeometryUtil.GetBondLengthAverage(container);
            PlaceHydrogens2D(container, atom, bondLength);
        }

        /// <summary>
        /// Place hydrogens connected to the provided atom <paramref name="atom"/> using the
        /// specified <paramref name="bondLength"/>.
        /// </summary>
        /// <param name="container">atom container</param>
        /// <param name="atom"></param>
        /// <param name="bondLength">bond length to user</param>
        /// <exception cref="ArgumentException">thrown if the <paramref name="atom"/> or 
        /// <i>container</i> was null or the atom has connected atoms which have not been placed.</exception>
        public void PlaceHydrogens2D(IAtomContainer container, IAtom atom, double bondLength)
        {
            if (atom.Point2D == null)
                throw new ArgumentException("cannot place hydrogens on atom without coordinates");

            Debug.WriteLine("placing hydrogens connected to atom ", atom.Symbol, ": ", atom.Point2D);
            Debug.WriteLine($"bond length{bondLength}");

            AtomPlacer atomPlacer = new AtomPlacer
            {
                Molecule = container ?? throw new ArgumentException("cannot place hydrogens, no container provided")
            };

            var connected = container.GetConnectedAtoms(atom);
            IAtomContainer placed = container.Builder.NewAtomContainer();
            IAtomContainer unplaced = container.Builder.NewAtomContainer();

            // divide connected atoms into those which are have and haven't been placed
            foreach (var conAtom in connected)
            {
                if (conAtom.Point2D == null)
                {
                    if (conAtom.AtomicNumber.Equals(AtomicNumbers.H))
                    {
                        unplaced.Atoms.Add(conAtom);
                    }
                    else
                    {
                        throw new ArgumentException("cannot place hydrogens, atom has connected non-hydrogens without coordinates");
                    }
                }
                else
                {
                    placed.Atoms.Add(conAtom);
                }
            }

            Debug.WriteLine("Atom placement before procedure:");
            Debug.WriteLine("Centre atom ", atom.Symbol, ": ", atom.Point2D);
            for (int i = 0; i < unplaced.Atoms.Count; i++)
            {
                Debug.WriteLine("H-" + i, ": ", unplaced.Atoms[i].Point2D);
            }

            Vector2 centerPlacedAtoms = GeometryUtil.Get2DCenter(placed);
            atomPlacer.DistributePartners(atom, placed, centerPlacedAtoms, unplaced, bondLength);

            Debug.WriteLine("Atom placement after procedure:");
            Debug.WriteLine($"Centre atom {atom.Symbol}: {atom.Point2D}");
            for (int i = 0; i < unplaced.Atoms.Count; i++)
            {
                Debug.WriteLine($"H-{i}: {unplaced.Atoms[i].Point2D}");
            }
        }
    }
}
