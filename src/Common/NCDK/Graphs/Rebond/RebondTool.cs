/* Copyright (C) 2003-2007  Miguel Howard <miguel@jmol.org>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the
 * beginning of your source code files, and to any copyright notice that
 * you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

namespace NCDK.Graphs.Rebond
{
    /// <summary>
    /// Provides tools to rebond a molecule from 3D coordinates only.
    /// The algorithm uses an efficient algorithm using a
    /// Binary Space Partitioning Tree (Bspt). It requires that the
    /// atom types are configured such that the covalent bond radii
    /// for all atoms are set. The AtomTypeFactory can be used for this.
    /// </summary>
    /// <seealso cref="Bspt{T}"/>
    // @cdk.keyword rebonding
    // @cdk.keyword bond, recalculation
    // @cdk.dictref blue-obelisk:rebondFrom3DCoordinates
    // @author      Miguel Howard
    // @cdk.created 2003-05-23
    // @cdk.module  standard
    public class RebondTool
    {
        private double maxCovalentRadius;
        private readonly double minBondDistance;
        private readonly double bondTolerance;

        private Bspt<ITupleAtom> bspt;

        public RebondTool(double maxCovalentRadius, double minBondDistance, double bondTolerance)
        {
            this.maxCovalentRadius = maxCovalentRadius;
            this.bondTolerance = bondTolerance;
            this.minBondDistance = minBondDistance;
            this.bspt = null;
        }

        /// <summary>
        /// Rebonding using a Binary Space Partition Tree. Note, that any bonds
        /// defined will be deleted first. It assumes the unit of 3D space to
        /// be 1 Å.
        /// </summary>
        public void Rebond(IAtomContainer container)
        {
            container.Bonds.Clear();
            maxCovalentRadius = 0.0;
            // construct a new binary space partition tree
            bspt = new Bspt<ITupleAtom>(3);
            foreach (var atom in container.Atoms)
            {
                double myCovalentRadius = atom.CovalentRadius.Value;
                if (myCovalentRadius == 0.0)
                {
                    throw new CDKException("Atom(s) does not have covalentRadius defined.");
                }
                if (myCovalentRadius > maxCovalentRadius) maxCovalentRadius = myCovalentRadius;
                TupleAtom tupleAtom = new TupleAtom(atom);
                bspt.AddTuple(tupleAtom);
            }
            // rebond all atoms
            foreach (var atom in container.Atoms)
            {
                BondAtom(container, atom);
            }
        }
        /// <summary>
        /// Rebonds one atom by looking up nearby atom using the binary space partition tree.
        /// </summary>
        private void BondAtom(IAtomContainer container, IAtom atom)
        {
            double myCovalentRadius = atom.CovalentRadius.Value;
            double searchRadius = myCovalentRadius + maxCovalentRadius + bondTolerance;
            var tupleAtom = new Point(atom.Point3D.Value.X, atom.Point3D.Value.Y, atom.Point3D.Value.Z);
            foreach (var hemiSphere in bspt.EnumerateHemiSphere(tupleAtom, searchRadius))
            {
                var atomNear = hemiSphere.Atom;
                if (atomNear != atom && container.GetBond(atom, atomNear) == null)
                {
                    bool bonded = IsBonded(myCovalentRadius, atomNear.CovalentRadius.Value, hemiSphere.Distance2);
                    if (bonded)
                    {
                        IBond bond = atom.Builder.NewBond(atom, atomNear, BondOrder.Single);
                        container.Bonds.Add(bond);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the bond order for the bond. At this moment, it only returns
        /// 0 or 1, but not 2 or 3, or aromatic bond order.
        /// </summary>
        private bool IsBonded(double covalentRadiusA, double covalentRadiusB, double distance2)
        {
            double maxAcceptable = covalentRadiusA + covalentRadiusB + bondTolerance;
            double maxAcceptable2 = maxAcceptable * maxAcceptable;
            double minBondDistance2 = this.minBondDistance * this.minBondDistance;
            if (distance2 < minBondDistance2) return false;
            return distance2 <= maxAcceptable2;
        }

        internal interface ITupleAtom : ITuple
        {
            IAtom Atom { get; }
        }

        private class TupleAtom : ITupleAtom
        {
            public TupleAtom(IAtom atom)
            {
                this.Atom = atom;
            }

            public virtual double GetDimValue(int dim)
            {
                if (dim == 0) return Atom.Point3D.Value.X;
                if (dim == 1) return Atom.Point3D.Value.Y;
                return Atom.Point3D.Value.Z;
            }

            public IAtom Atom { get; }
            public double Distance2 { get; set; }

            public override string ToString()
            {
                return ("<" + Atom.Point3D.Value.X + "," + Atom.Point3D.Value.Y + "," + Atom.Point3D.Value.Z + ">");
            }
        }
    }
}
