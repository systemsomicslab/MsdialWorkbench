/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Numerics;
using System;

namespace NCDK.Geometries
{
    /// <summary>
    /// A set of static utility classes for geometric calculations on Atoms.
    /// </summary>
    // @author Peter Murray-Rust
    // @cdk.created 2003-06-14
    public static class AtomTools
    {
        public readonly static double TypicalTetrahedralAngle = 2.0 * Math.Acos(1.0 / Math.Sqrt(3.0));

        /// <summary>
        /// Generate coordinates for all atoms which are singly bonded and have
        /// no coordinates. This is useful when hydrogens are present but have
        /// no coordinates. It knows about C, O, N, S only and will give tetrahedral or
        /// trigonal geometry elsewhere. Bond lengths are computed from covalent radii
        /// if available. Angles are tetrahedral or trigonal
        /// </summary>
        /// <param name="atomContainer">the set of atoms involved</param>
        // @cdk.keyword coordinate calculation
        // @cdk.keyword 3D model
        public static void Add3DCoordinates1(IAtomContainer atomContainer)
        {
            // atoms without coordinates
            var noCoords = atomContainer.Builder.NewAtomContainer();
            // get vector of possible referenceAtoms?
            var refAtoms = atomContainer.Builder.NewAtomContainer();
            foreach (var atom in atomContainer.Atoms)
            {
                // is this atom without 3D coords, and has only one ligand?
                if (atom.Point3D == null)
                {
                    var connectedAtoms = atomContainer.GetConnectedAtoms(atom).ToReadOnlyList();
                    if (connectedAtoms.Count == 1)
                    {
                        var refAtom = connectedAtoms[0];
                        if (refAtom.Point3D != null)
                        {
                            refAtoms.Atoms.Add(refAtom);
                            // store atoms with no coords and ref atoms in a
                            // single container
                            noCoords.Atoms.Add(atom);
                            noCoords.Atoms.Add(refAtom);
                            // bond is required to extract ligands
                            noCoords.Bonds.Add(atomContainer.Builder.NewBond(atom, refAtom, BondOrder.Single));
                        }
                    }
                }
            }
            // now add coordinates to ligands of reference atoms
            // use default length of 1.0, which can be adjusted later
            double length = 1;
            var angle = TypicalTetrahedralAngle;
            foreach (var refAtom in refAtoms.Atoms)
            {
                var noCoordLigands = noCoords.GetConnectedAtoms(refAtom).ToReadOnlyList();
                var nLigands = noCoordLigands.Count;
                var nwanted = nLigands;
                var elementType = refAtom.Symbol;
                // try to deal with lone pairs on small hetero
                switch (refAtom.AtomicNumber)
                {
                    case AtomicNumbers.N:
                    case AtomicNumbers.O:
                    case AtomicNumbers.S:
                        nwanted = 3;
                        break;
                }
                var newPoints = Calculate3DCoordinatesForLigands(atomContainer, refAtom, nwanted, length, angle);
                for (int j = 0; j < nLigands; j++)
                {
                    var ligand = noCoordLigands[j];
                    var newPoint = RescaleBondLength(refAtom, ligand, newPoints[j].Value);
                    ligand.Point3D = newPoint;
                }
            }
        }

        /// <summary>
        /// Rescales Point2 so that length 1-2 is sum of covalent radii.
        /// if covalent radii cannot be found, use bond length of 1.0
        /// </summary>
        /// <param name="atom1">stationary atom</param>
        /// <param name="atom2">movable atom</param>
        /// <param name="point2">coordinates for atom 2</param>
        /// <returns>new coords for atom 2</returns>
        public static Vector3 RescaleBondLength(IAtom atom1, IAtom atom2, Vector3 point2)
        {
            var point1 = atom1.Point3D.Value;
            var d1 = atom1.CovalentRadius.Value;
            var d2 = atom2.CovalentRadius.Value;
            // in case we have no covalent radii, set to 1.0
            var distance = (d1 < 0.1 || d2 < 0.1) ? 1.0 : atom1.CovalentRadius.Value + atom2.CovalentRadius.Value;
            var vect = Vector3.Normalize(point2 - point1) * distance;
            var newPoint = point1 + vect;
            return newPoint;
        }

        /// <summary>
        /// Adds 3D coordinates for singly-bonded ligands of a reference atom (A).
        /// Initially designed for hydrogens. The ligands of refAtom are identified
        /// and those with 3D coordinates used to generate the new points. (This
        /// allows structures with partially known 3D coordinates to be used, as when
        /// groups are added.)
        /// </summary>
        /// <remarks>
        /// "Bent" and "non-planar" groups can be formed by taking a subset of the
        /// calculated points. Thus R-NH2 could use 2 of the 3 points calculated
        /// from (1,iii)
        /// nomenclature: A is point to which new ones are "attached".
        ///     A may have ligands B, C...
        ///     B may have ligands J, K..
        ///     points X1, X2... are returned
        /// The cases (see individual routines, which use idealised geometry by default):
        /// (0) zero ligands of refAtom. The resultant points are randomly oriented:
        ///    (i) 1 points: required; +x,0,0
        ///    (ii) 2 points: use +x,0,0 and -x,0,0
        ///    (iii) 3 points: equilateral triangle in xy plane
        ///    (iv) 4 points x,x,x, x,-x,-x, -x,x,-x, -x,-x,x
        /// (1a) 1 Ligand(B) of refAtom which itself has a ligand (J)
        ///    (i) 1 points: required; vector along AB vector
        ///    (ii) 2 points: 2 vectors in ABJ plane, staggered and eclipsed wrt J
        ///    (iii) 3 points: 1 staggered wrt J, the others +- gauche wrt J
        /// (1b) 1 Ligand(B) of refAtom which has no other ligands. A random J is
        /// generated and (1a) applied
        /// (2) 2 Ligands(B, C) of refAtom A
        ///    (i) 1 points: required; vector in ABC plane bisecting AB, AC. If ABC is linear, no points
        ///    (ii) 2 points: 2 vectors at angle ang, whose resultant is 2i
        /// (3) 3 Ligands(B, C, D) of refAtom A
        ///    (i) 1 points: required; if A, B, C, D coplanar, no points.
        ///       else vector is resultant of BA, CA, DA
        /// fails if atom itself has no coordinates or &gt;4 ligands
        /// </remarks>
        /// <param name="atomContainer">describing the ligands of refAtom. It could be the whole molecule, or could be a selected subset of ligands</param>
        /// <param name="refAtom">(A) to which new ligands coordinates could be added</param>
        /// <param name="length">A-X length</param>
        /// <param name="angle">B-A-X angle (used in certain cases)</param>
        /// <param name="nwanted"></param>
        /// <returns>Point3D[] points calculated. If request could not be fulfilled (e.g. too many atoms, or strange geometry, returns empty array (zero length, not null)</returns>
        // @cdk.keyword coordinate generation
        public static Vector3?[] Calculate3DCoordinatesForLigands(IAtomContainer atomContainer, IAtom refAtom, int nwanted, double length, double angle)
        {
            var newPoints = Array.Empty<Vector3?>();
            var aPoint = refAtom.Point3D;
            // get ligands
            var connectedAtoms = atomContainer.GetConnectedAtoms(refAtom);
            if (connectedAtoms == null)
            {
                return newPoints;
            }
            var ligandsWithCoords = atomContainer.Builder.NewAtomContainer();
            foreach (var ligand in connectedAtoms)
            {
                if (ligand.Point3D != null)
                {
                    ligandsWithCoords.Atoms.Add(ligand);
                }
            }
            int nwithCoords = ligandsWithCoords.Atoms.Count;
            // too many ligands at present
            if (nwithCoords > 3)
            {
                return newPoints;
            }
            if (nwithCoords == 0)
            {
                newPoints = Calculate3DCoordinates0(refAtom.Point3D.Value, nwanted, length);
            }
            else if (nwithCoords == 1)
            {
                // ligand on A
                var bAtom = ligandsWithCoords.Atoms[0];
                connectedAtoms = ligandsWithCoords.GetConnectedAtoms(bAtom);
                // does B have a ligand (other than A)
                IAtom jAtom = null;
                foreach (var connectedAtom in connectedAtoms)
                {
                    if (!connectedAtom.Equals(refAtom))
                    {
                        jAtom = connectedAtom;
                        break;
                    }
                }
                newPoints = Calculate3DCoordinates1(aPoint, bAtom.Point3D, jAtom?.Point3D, nwanted, length, angle);
            }
            else if (nwithCoords == 2)
            {
                var bPoint = ligandsWithCoords.Atoms[0].Point3D;
                var cPoint = ligandsWithCoords.Atoms[1].Point3D;
                newPoints = Calculate3DCoordinates2(aPoint, bPoint, cPoint, nwanted, length, angle);
            }
            else if (nwithCoords == 3)
            {
                var bPoint = ligandsWithCoords.Atoms[0].Point3D;
                var cPoint = ligandsWithCoords.Atoms[1].Point3D;
                var dPoint = ligandsWithCoords.Atoms[2].Point3D;
                newPoints = new Vector3?[]
                {
                    Calculate3DCoordinates3(aPoint.Value, bPoint.Value, cPoint.Value, dPoint.Value, length),
                };
            }
            return newPoints;
        }

        /// <summary>
        /// Calculates substituent points.
        /// Calculate substituent points for
        /// </summary>
        /// <remarks>
        /// (0) zero ligands of aPoint. The resultant points are randomly oriented:
        ///    (i) 1 points: required; +x,0,0
        ///    (ii) 2 points: use +x,0,0 and -x,0,0
        ///    (iii) 3 points: equilateral triangle in xy plane
        ///    (iv) 4 points x,x,x, x,-x,-x, -x,x,-x, -x,-x,x where 3x**2 = bond length
        /// </remarks>
        /// <param name="aPoint">to which substituents are added</param>
        /// <param name="nwanted">number of points to calculate (1-4)</param>
        /// <param name="length">from aPoint</param>
        /// <returns>Vector3[] nwanted points (or zero if failed)</returns>
        public static Vector3?[] Calculate3DCoordinates0(Vector3 aPoint, int nwanted, double length)
        {
            Vector3?[] points;
            switch (nwanted)
            {
                case 1:
                    points = new Vector3?[]
                    {
                        aPoint + new Vector3(length, 0.0, 0.0),
                    };
                    break;
                case 2:
                    points = new Vector3?[]
                    {
                        aPoint + new Vector3(length, 0.0, 0.0),
                        aPoint + new Vector3(-length, 0.0, 0.0),
                    };
                    break;
                case 3:
                    points = new Vector3?[]
                    {
                        aPoint + new Vector3(length, 0.0, 0.0),
                        aPoint + new Vector3(-length * 0.5, -length * 0.5 * Math.Sqrt(3.0), 0),
                        aPoint + new Vector3(-length * 0.5, length * 0.5 * Math.Sqrt(3.0), 0),
                    };
                    break;
                case 4:
                    double dx = length / Math.Sqrt(3.0);
                    points = new Vector3?[]
                    {
                        aPoint + new Vector3(dx, dx, dx),
                        aPoint + new Vector3(dx, -dx, -dx),
                        aPoint + new Vector3(-dx, -dx, dx),
                        aPoint + new Vector3(-dx, dx, -dx),
                    };
                    break;
                default:
                    points = Array.Empty<Vector3?>();
                    break;
            }
            return points;
        }

        /// <summary>
        /// Calculate new Point(s) X in a B-A system to form B-A-X.
        /// Use C as reference for * staggering about the B-A bond
        /// </summary>
        /// <remarks>
        /// (1a) 1 Ligand(B) of refAtom (A) which itself has a ligand (C)
        ///    (i) 1 points: required; vector along AB vector
        ///    (ii) 2 points: 2 vectors in ABC plane, staggered and eclipsed wrt C
        ///    (iii) 3 points: 1 staggered wrt C, the others +- gauche wrt C
        /// If C is null, a random non-colinear C is generated
        /// </remarks>
        /// <param name="aPoint">to which substituents are added</param>
        /// <param name="bPoint">first ligand of A</param>
        /// <param name="cPoint">second ligand of A</param>
        /// <param name="nwanted">number of points to calculate (1-3)</param>
        /// <param name="length">A-X length</param>
        /// <param name="angle">B-A-X angle</param>
        /// <returns>Vector3[] nwanted points (or zero if failed)</returns>
        public static Vector3?[] Calculate3DCoordinates1(Vector3? aPoint, Vector3? bPoint, Vector3? cPoint, int nwanted, double length, double angle)
        {
            var points = new Vector3?[nwanted];
            // BA vector
            var ba = Vector3.Normalize(aPoint.Value - bPoint.Value);
            // if no cPoint, generate a random reference
            if (cPoint == null)
            {
                var cVector = GetNonColinearVector(ba);
                cPoint = cVector;
            }
            // CB vector
            var cb = Vector3.Normalize(bPoint.Value - cPoint.Value);
            // if A, B, C colinear, replace C by random point
            var cbdotba = Vector3.Dot(cb, ba);
            if (cbdotba > 0.999999)
            {
                var cVector = GetNonColinearVector(ba);
                cPoint = cVector;
                cb = bPoint.Value - cPoint.Value;
            }
            // cbxba = c x b
            var cbxba = Vector3.Normalize(Vector3.Cross(cb, ba));
            // create three perp axes ba, cbxba, and ax
            var ax = Vector3.Normalize(Vector3.Cross(cbxba, ba));
            var drot = Math.PI * 2.0 / nwanted;
            for (int i = 0; i < nwanted; i++)
            {
                var rot = i * drot;
                points[i] = aPoint.Value;
                var vx = ba * (-Math.Cos(angle) * length); ;
                var vy = ax * (Math.Cos(rot) * length);
                var vz = cbxba * (Math.Sin(rot) * length);
                points[i] += vx;
                points[i] += vy;
                points[i] += vz;
            }
            return points;
        }

        /// <summary>
        /// Calculate new Point(s) X in a B-A-C system. It forms form a B-A(-C)-X system.
        /// </summary>
        /// <remarks>
        /// (2) 2 Ligands(B, C) of refAtom A
        ///    (i) 1 points: required; vector in ABC plane bisecting AB, AC. If ABC is
        ///        linear, no points
        ///    (ii) 2 points: 2 points X1, X2, X1-A-X2 = angle about 2i vector
        /// </remarks>
        /// <param name="aPoint">to which substituents are added</param>
        /// <param name="bPoint">first ligand of A</param>
        /// <param name="cPoint">second ligand of A</param>
        /// <param name="nwanted">number of points to calculate (1-2)</param>
        /// <param name="length">A-X length</param>
        /// <param name="angle">B-A-X angle</param>
        /// <returns>Vector3[] nwanted points (or zero if failed)</returns>
        public static Vector3?[] Calculate3DCoordinates2(Vector3? aPoint, Vector3? bPoint, Vector3? cPoint, int nwanted, double length, double angle)
        {
            var newPoints = Array.Empty<Vector3?>();
            var ang2 = angle / 2.0;

            var ba = aPoint.Value - bPoint.Value;
            var ca = aPoint.Value - cPoint.Value;
            var baxca = Vector3.Cross(ba, ca);
            if (baxca.Length() < 0.00000001)
            {
                ; // linear
            }
            else if (nwanted == 1)
            {
                newPoints = new Vector3?[1];
                var ax = Vector3.Normalize(ba - ca) * length;
                newPoints[0] = aPoint.Value + ax;
            }
            else if (nwanted == 2)
            {
                newPoints = new Vector3?[2];
                var ax = Vector3.Normalize(ba + ca) * (Math.Cos(ang2) * length);
                baxca = Vector3.Normalize(baxca) * (Math.Sin(ang2) * length);
                newPoints[0] = aPoint.Value + ax + baxca;
                newPoints[1] = aPoint.Value + ax - baxca;
            }
            return newPoints;
        }

        /// <summary>
        /// Calculate new point X in a B-A(-D)-C system. It forms a B-A(-D)(-C)-X system.
        /// </summary>
        /// <remarks>
        /// (3) 3 Ligands(B, C, D) of refAtom A
        ///    (i) 1 points: required; if A, B, C, D coplanar, no points.
        ///       else vector is resultant of BA, CA, DA
        /// </remarks>
        /// <param name="aPoint">to which substituents are added</param>
        /// <param name="bPoint">first ligand of A</param>
        /// <param name="cPoint">second ligand of A</param>
        /// <param name="dPoint">third ligand of A</param>
        /// <param name="length">A-X length</param>
        /// <returns>Vector3 nwanted points (or null if failed (coplanar))</returns>
        public static Vector3? Calculate3DCoordinates3(Vector3 aPoint, Vector3 bPoint, Vector3 cPoint, Vector3 dPoint, double length)
        {
            var v1 = aPoint - bPoint;
            var v2 = aPoint - cPoint;
            var v3 = aPoint - dPoint;
            var v = bPoint + cPoint + dPoint;
            if (v.Length() < 0.00001)
            {
                return null;
            }
            v = Vector3.Normalize(v) * length;
            var point = aPoint + v;
            return point;
        }

        /// gets a point not on vector a...b; this can be used to define a plan or cross products
        private static Vector3 GetNonColinearVector(Vector3 ab)
        {
            var cr = Vector3.Cross(ab, Vector3.UnitX);
            if (cr.Length() > 0.00001)
            {
                return Vector3.UnitX;
            }
            else
            {
                return Vector3.UnitY;
            }
        }
    }
}
