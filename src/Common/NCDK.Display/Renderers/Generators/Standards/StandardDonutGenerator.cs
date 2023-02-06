/*
 * Copyright (C) 2019  The Chemistry Development Kit (CDK) project
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
 */

using NCDK.Geometries;
using NCDK.Graphs;
using NCDK.Renderers.Elements;
using System;
using System.Collections.Generic;
using System.Windows.Media;
using WPF = System.Windows;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// Generates aromatic donuts(or life buoys) as ovals in small(&lt;8) aromatic
    /// rings.If the ring is charged (and the charged is not shared with another
    /// ring, e.g.rbonds &gt; 2) it will be depicted in the middle of the ring.
    /// </summary>
    /// <seealso cref="RendererModelTools.GetForceDelocalisedBondDisplay(RendererModel)"/>
    /// <seealso cref="RendererModelTools.GetDelocalisedDonutsBondDisplay(RendererModel)"/>
    sealed class StandardDonutGenerator
    {
        // bonds involved in donuts!
        private readonly HashSet<IBond> bonds = new HashSet<IBond>();
        // atoms with delocalised charge
        private readonly HashSet<IAtom> atoms = new HashSet<IAtom>();
        // smallest rings through each edge
        IRingSet smallest;

        private readonly bool forceDelocalised;
        private readonly bool delocalisedDonuts;
        private readonly double dbSpacing;
        private readonly double scale;
        private readonly double stroke;
        private readonly Color fgColor;
        private readonly Typeface font;
        private readonly double emSize;
        private readonly IAtomContainer mol;

        /// <summary>
        /// Create a new generator for a molecule.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <param name="font">the font</param>
        /// <param name="emSize">the font size</param>
        /// <param name="model">the rendering parameters</param>
        public StandardDonutGenerator(IAtomContainer mol, Typeface font, double emSize, RendererModel model, double stroke)
        {
            this.mol = mol;
            this.font = font;
            this.emSize = emSize;
            this.forceDelocalised = model.GetForceDelocalisedBondDisplay();
            this.delocalisedDonuts = model.GetDelocalisedDonutsBondDisplay();
            this.dbSpacing = model.GetBondSeparation();
            this.scale = model.GetScale();
            this.stroke = stroke;
            this.fgColor = model.GetAtomColorer().GetAtomColor(CDK.Builder.NewAtom("C"));
        }

        public bool CanDelocalise(IAtomContainer ring)
        {
            var okay = ring.Bonds.Count < 8;
            if (!okay)
                return false;
            foreach (var bond in ring.Bonds)
            {
                if (!bond.IsAromatic)
                    okay = false;
                if (!bond.Order.IsUnset() && !forceDelocalised)
                    okay = false;
            }
            return okay;
        }

        public IRenderingElement Generate()
        {
            if (!delocalisedDonuts)
                return null;
            var group = new ElementGroup();
            smallest = Cycles.EdgeShort.Find(mol).ToRingSet();
            foreach (var ring in smallest)
            {
                if (!CanDelocalise(ring))
                    continue;
                foreach (var bond in ring.Bonds)
                {
                    bonds.Add(bond);
                }
                int charge = 0;
                int unpaired = 0;
                foreach (var atom in ring.Atoms)
                {
                    var q = atom.FormalCharge ?? 0;
                    if (q == 0)
                    {
                        continue;
                    }
                    int nCyclic = 0;
                    foreach (var bond in mol.GetConnectedBonds(atom))
                        if (bond.IsInRing)
                            nCyclic++;
                    if (nCyclic > 2)
                        continue;
                    atoms.Add(atom);
                    charge += q;
                }
                var p2 = GeometryUtil.Get2DCenter(ring);

                if (charge != 0)
                {
                    var qText = charge < 0 ? "–" : "+";
                    if (charge < -1)
                        qText = Math.Abs(charge) + qText;
                    else if (charge > +1)
                        qText = Math.Abs(charge) + qText;

                    TextOutline qSym = new TextOutline(qText, font, emSize);
                    qSym = qSym.Resize(1 / scale, -1 / scale);
                    qSym = qSym.Translate(p2.X - qSym.GetCenter().X,
                                          p2.Y - qSym.GetCenter().Y);
                    group.Add(GeneralPath.ShapeOf(qSym.GetOutline(), fgColor));
                }

                double s = GeometryUtil.GetBondLengthMedian(ring);
                double n = ring.Bonds.Count;
                double r = s / (2 * Math.Tan(Math.PI / n));
                group.Add(new OvalElement(new WPF::Point(p2.X, p2.Y), r - 1.5 * dbSpacing,
                                          stroke, false, fgColor));
            }
            return group;
        }

        public bool IsDelocalised(IBond bond)
        {
            return bonds.Contains(bond);
        }

        public bool IsChargeDelocalised(IAtom atom)
        {
            return atoms.Contains(atom);
        }
    }
}
