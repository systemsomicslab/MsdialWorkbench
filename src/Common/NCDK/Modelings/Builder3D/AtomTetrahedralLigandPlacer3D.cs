/* Copyright (C) 2005-2007  Christian Hoppe <chhoppe@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All we ask is that proper credit is given for our work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
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

using NCDK.Common.Mathematics;
using NCDK.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCDK.Modelings.Builder3D
{
    /// <summary>
    /// A set of static utility classes for geometric calculations on Atoms.
    /// </summary>
    // @author         Peter Murray-Rust,chhoppe,egonw
    // @cdk.created    2003-??-??
    // @cdk.module    builder3d
    public class AtomTetrahedralLigandPlacer3D
    {
        private IReadOnlyDictionary<string, object> pSet = null;
        public double DefaultBondLengthH { get; set; } = 1.0;

        private readonly static double TETRAHEDRAL_ANGLE = 2.0 * Math.Acos(1.0 / Math.Sqrt(3.0));

        private const double SP2_ANGLE = 120 * Math.PI / 180;
        private const double SP_ANGLE = Math.PI;

        readonly static Vector3 XV = new Vector3(1, 0, 0);
        readonly static Vector3 YV = new Vector3(0, 1, 0);

        internal AtomTetrahedralLigandPlacer3D() { }

        public AtomTetrahedralLigandPlacer3D(IReadOnlyDictionary<string, object> moleculeParameter)
        {
            pSet = moleculeParameter;
        }

        /// <summary>
        /// Generate coordinates for all atoms which are singly bonded and have no
        /// coordinates. This is useful when hydrogens are present but have no coordinates.
        /// It knows about C, O, N, S only and will give tetrahedral or trigonal
        /// geometry elsewhere. Bond lengths are computed from covalent radii or taken
        /// out of a parameter set if available. Angles are tetrahedral or trigonal
        /// </summary>
        /// <param name="atomContainer">the set of atoms involved</param>
        // @cdk.keyword           coordinate calculation
        // @cdk.keyword           3D model
        public virtual void Add3DCoordinatesForSinglyBondedLigands(IAtomContainer atomContainer)
        {
            IAtom refAtom = null;
            IAtom atomC = null;
            int nwanted = 0;
            for (int i = 0; i < atomContainer.Atoms.Count; i++)
            {
                refAtom = atomContainer.Atoms[i];
                if (!refAtom.AtomicNumber.Equals(AtomicNumbers.H) && HasUnsetNeighbour(refAtom, atomContainer))
                {
                    IAtomContainer noCoords = GetUnsetAtomsInAtomContainer(refAtom, atomContainer);
                    IAtomContainer withCoords = GetPlacedAtomsInAtomContainer(refAtom, atomContainer);
                    if (withCoords.Atoms.Count > 0)
                    {
                        atomC = GetPlacedHeavyAtomInAtomContainer(withCoords.Atoms[0], refAtom, atomContainer);
                    }
                    if (refAtom.FormalNeighbourCount == 0 && refAtom.AtomicNumber.Equals(AtomicNumbers.C))
                    {
                        nwanted = noCoords.Atoms.Count;
                    }
                    else if (refAtom.FormalNeighbourCount == 0 && !refAtom.AtomicNumber.Equals(AtomicNumbers.C))
                    {
                        nwanted = 4;
                    }
                    else
                    {
                        nwanted = refAtom.FormalNeighbourCount.Value - withCoords.Atoms.Count;
                    }
                    Vector3[] newPoints = Get3DCoordinatesForLigands(refAtom, noCoords, withCoords, atomC, nwanted,
                            DefaultBondLengthH, -1);
                    for (int j = 0; j < noCoords.Atoms.Count; j++)
                    {
                        IAtom ligand = noCoords.Atoms[j];
                        Vector3 newPoint = RescaleBondLength(refAtom, ligand, newPoints[j]);
                        ligand.Point3D = newPoint;
                        ligand.IsPlaced = true;
                    }

                    noCoords.RemoveAllElements();
                    withCoords.RemoveAllElements();
                }
            }
        }

        /// <summary>
        /// Rescales Point2 so that length 1-2 is sum of covalent radii.
        /// If covalent radii cannot be found, use bond length of 1.0
        /// </summary>
        /// <param name="atom1">stationary atom</param>
        /// <param name="atom2">movable atom</param>
        /// <param name="point2">coordinates for atom 2</param>
        /// <returns>new coordinates for atom 2</returns>
        public virtual Vector3 RescaleBondLength(IAtom atom1, IAtom atom2, Vector3 point2)
        {
            Vector3 point1 = atom1.Point3D.Value;
            double? d1 = atom1.CovalentRadius;
            double? d2 = atom2.CovalentRadius;
            // in case we have no covalent radii, set to 1.0
            double distance = (d1 == null || d2 == null) ? 1 : d1.Value + d2.Value;
            if (pSet != null)
            {
                distance = GetDistanceValue(atom1.AtomTypeName, atom2.AtomTypeName);
            }
            var vect = point2 - point1;
            vect = Vector3.Normalize(vect);
            vect *= distance;
            Vector3 newPoint = point1 + vect;
            return newPoint;
        }

        /// <summary>
        /// Adds 3D coordinates for singly-bonded ligands of a reference atom (A).
        /// Initially designed for hydrogens. The ligands of refAtom are identified and
        /// those with 3D coordinates used to generate the new points. (This allows
        /// structures with partially known 3D coordinates to be used, as when groups
        /// are added.) "Bent" and "non-planar" groups can be formed by taking a subset
        /// of the calculated points. Thus R-NH2 could use 2 of the 3 points calculated
        /// from (1,iii) nomenclature: A is point to which new ones are "attached". A
        /// may have ligands B, C... B may have ligands J, K.. points X1, X2... are
        /// returned The cases (see individual routines, which use idealised geometry
        /// by default): (0) zero ligands of refAtom. The resultant points are randomly
        /// oriented: (i) 1 points required; +x,0,0 (ii) 2 points: use +x,0,0 and
        /// -x,0,0 (iii) 3 points: equilateral triangle in xy plane (iv) 4 points
        /// x,x,x, x,-x,-x, -x,x,-x, -x,-x,x (1a) 1 Ligand(B) of refAtom which itself
        /// has a ligand (J) (i) 1 points required; vector along AB vector (ii) 2
        /// points: 2 vectors in ABJ plane, staggered and eclipsed wrt J (iii) 3
        /// points: 1 staggered wrt J, the others +- gauche wrt J (1b) 1 Ligand(B) of
        /// refAtom which has no other ligands. A random J is generated and (1a)
        /// applied (2) 2 Ligands(B, C) of refAtom A (i) 1 points required; vector in
        /// ABC plane bisecting AB, AC. If ABC is linear, no points (ii) 2 points: 2
        /// vectors at angle ang, whose resultant is 2i (3) 3 Ligands(B, C, D) of
        /// refAtom A (i) 1 points required; if A, B, C, D coplanar, no points. else
        /// vector is resultant of BA, CA, DA fails if atom itself has no coordinates
        /// or &gt;4 ligands
        /// </summary>
        /// <param name="refAtom">(A) to which new ligands coordinates could be added</param>
        /// <param name="length">A-X length</param>
        /// <param name="angle">B-A-X angle (used in certain cases)</param>
        /// <param name="nwanted">Description of the Parameter</param>
        /// <param name="noCoords">Description of the Parameter</param>
        /// <param name="withCoords">Description of the Parameter</param>
        /// <param name="atomC">Description of the Parameter</param>
        /// <returns><see cref="Vector3"/>[] points calculated. If request could not be
        ///      fulfilled (e.g. too many atoms, or strange geometry, returns empty
        ///      array (zero length, not <see langword="null"/>)</returns>
        /// <exception cref="CDKException"></exception>
        // @cdk.keyword           coordinate generation
        public virtual Vector3[] Get3DCoordinatesForLigands(IAtom refAtom, IAtomContainer noCoords, IAtomContainer withCoords,
                IAtom atomC, int nwanted, double length, double angle)
        {
            Vector3[] newPoints = new Vector3[1];

            if (noCoords.Atoms.Count == 0 && withCoords.Atoms.Count == 0)
            {
                return newPoints;
            }

            // too many ligands at present
            if (withCoords.Atoms.Count > 3)
            {
                return newPoints;
            }

            BondOrder refMaxBondOrder = refAtom.MaxBondOrder;
            if (refAtom.FormalNeighbourCount == 1)
            {
                //            WTF???
            }
            else if (refAtom.FormalNeighbourCount == 2 || refMaxBondOrder == BondOrder.Triple)
            {
                //sp
                if (angle == -1)
                {
                    angle = SP_ANGLE;
                }
                newPoints[0] = Get3DCoordinatesForSPLigands(refAtom, withCoords, length, angle);
            }
            else if (refAtom.FormalNeighbourCount == 3 || (refMaxBondOrder == BondOrder.Double))
            {
                //sp2
                if (angle == -1)
                {
                    angle = SP2_ANGLE;
                }
                try
                {
                    newPoints = Get3DCoordinatesForSP2Ligands(refAtom, noCoords, withCoords, atomC, length, angle);
                }
                catch (Exception ex1)
                {
                    //                Debug.WriteLine("Get3DCoordinatesForLigandsERROR: Cannot place SP2 Ligands due to:" + ex1.ToString());
                    throw new CDKException("Cannot place sp2 substituents\n" + ex1.Message, ex1);
                }
            }
            else
            {
                //sp3
                try
                {
                    newPoints = Get3DCoordinatesForSP3Ligands(refAtom, noCoords, withCoords, atomC, nwanted, length, angle);
                }
                catch (Exception ex1)
                {
                    //                Debug.WriteLine("Get3DCoordinatesForLigandsERROR: Cannot place SP3 Ligands due to:" + ex1.ToString());
                    throw new CDKException("Cannot place sp3 substituents\n" + ex1.Message, ex1);
                }
            }
            //Debug.WriteLine("...Ready "+newPoints.Length+" "+newPoints[0].ToString());
            return newPoints;
        }

        public virtual Vector3 Get3DCoordinatesForSPLigands(IAtom refAtom, IAtomContainer withCoords, double length, double angle)
        {
            //Debug.WriteLine(" SP Ligands start "+refAtom.Point3D+" "+(withCoords.GetAtomAt(0)).Point3D);
            Vector3 ca = refAtom.Point3D.Value - (withCoords.Atoms[0]).Point3D.Value;
            ca = Vector3.Normalize(ca);
            ca *= length;
            Vector3 newPoint = refAtom.Point3D.Value + ca;
            return newPoint;
        }

        /// <summary>
        /// Main method for the calculation of the ligand coordinates for sp2 atoms.
        /// Decides if one or two coordinates should be created
        /// </summary>
        /// <param name="refAtom">central atom (Atom)</param>
        /// <param name="noCoords">Description of the Parameter</param>
        /// <param name="withCoords">Description of the Parameter</param>
        /// <param name="atomC">Description of the Parameter</param>
        /// <param name="length">Description of the Parameter</param>
        /// <param name="angle">Description of the Parameter</param>
        /// <returns>coordinates as Points3d []</returns>
        public virtual Vector3[] Get3DCoordinatesForSP2Ligands(IAtom refAtom, IAtomContainer noCoords, IAtomContainer withCoords,
                IAtom atomC, double length, double angle)
        {
            //Debug.WriteLine(" SP2 Ligands start");
            Vector3[] newPoints = new Vector3[1];
            if (angle < 0)
            {
                angle = SP2_ANGLE;
            }
            if (withCoords.Atoms.Count >= 2)
            {
                //Debug.WriteLine("Wanted:1 "+noCoords.Atoms.Count);
                newPoints[0] = Calculate3DCoordinatesSP2_1(refAtom.Point3D.Value, (withCoords.Atoms[0]).Point3D.Value,
                        (withCoords.Atoms[1]).Point3D.Value, length, -1 * angle);

            }
            else if (withCoords.Atoms.Count <= 1)
            {
                //Debug.WriteLine("NoCoords 2:"+noCoords.Atoms.Count);
                newPoints = Calculate3DCoordinatesSP2_2(refAtom.Point3D.Value, (withCoords.Atoms[0]).Point3D.Value,
                        atomC?.Point3D, length, angle);
            }
            //Debug.WriteLine("Ready SP2");
            return newPoints;
        }

        /// <summary>
        /// Main method for the calculation of the ligand coordinates for sp3 atoms.
        /// Decides how many coordinates should be created
        /// </summary>
        /// <param name="refAtom">central atom (Atom)</param>
        /// <param name="nwanted">how many ligands should be created</param>
        /// <param name="length">bond length</param>
        /// <param name="angle">angle in a B-A-(X) system; a=central atom;
        ///      x=ligand with unknown coordinates</param>
        /// <param name="noCoords">Description of the Parameter</param>
        /// <param name="withCoords">Description of the Parameter</param>
        /// <param name="atomC">Description of the Parameter</param>
        /// <returns>Description of the Return Value</returns>
        public virtual Vector3[] Get3DCoordinatesForSP3Ligands(IAtom refAtom, IAtomContainer noCoords, IAtomContainer withCoords,
                IAtom atomC, int nwanted, double length, double angle)
        {
            //Debug.WriteLine("SP3 Ligands start ");
            Vector3[] newPoints = Array.Empty<Vector3>();
            Vector3 aPoint = refAtom.Point3D.Value;
            int nwithCoords = withCoords.Atoms.Count;
            if (angle < 0)
            {
                angle = TETRAHEDRAL_ANGLE;
            }
            if (nwithCoords == 0)
            {
                newPoints = Calculate3DCoordinates0(refAtom.Point3D.Value, nwanted, length);
            }
            else if (nwithCoords == 1)
            {
                newPoints = Calculate3DCoordinates1(aPoint, (withCoords.Atoms[0]).Point3D.Value,
                        atomC?.Point3D, nwanted, length, angle);
            }
            else if (nwithCoords == 2)
            {
                Vector3 bPoint = withCoords.Atoms[0].Point3D.Value;
                Vector3 cPoint = withCoords.Atoms[1].Point3D.Value;
                newPoints = Calculate3DCoordinates2(aPoint, bPoint, cPoint, nwanted, length, angle);
            }
            else if (nwithCoords == 3)
            {
                Vector3 bPoint = withCoords.Atoms[0].Point3D.Value;
                Vector3 cPoint = withCoords.Atoms[1].Point3D.Value;
                newPoints = new Vector3[1];
                Vector3 dPoint = withCoords.Atoms[2].Point3D.Value;
                newPoints[0] = Calculate3DCoordinates3(aPoint, bPoint, cPoint, dPoint, length);
            }
            //Debug.WriteLine("...Ready");
            return newPoints;
        }

        /// <summary>
        /// Calculates substituent points. Calculate substituent points for (0) zero
        /// ligands of aPoint. The resultant points are randomly oriented: (i) 1 points
        /// required; +x,0,0 (ii) 2 points: use +x,0,0 and -x,0,0 (iii) 3 points:
        /// equilateral triangle in the xy plane (iv) 4 points x,x,x, x,-x,-x, -x,x,-x,
        /// -x,-x,x where 3x**2 = bond length
        /// </summary>
        /// <param name="aPoint">to which substituents are added</param>
        /// <param name="nwanted">number of points to calculate (1-4)</param>
        /// <param name="length">from aPoint</param>
        /// <returns>Vector3[] nwanted points (or zero if failed)</returns>
        public virtual Vector3[] Calculate3DCoordinates0(Vector3 aPoint, int nwanted, double length)
        {
            Vector3[] points = Array.Empty<Vector3>();
            if (nwanted == 1)
            {
                points = new Vector3[1];
                points[0] = aPoint + new Vector3(length, 0.0, 0.0);
            }
            else if (nwanted == 2)
            {
                points = new Vector3[2];
                points[0] = aPoint + new Vector3(length, 0.0, 0.0);
                points[1] = aPoint + new Vector3(-length, 0.0, 0.0);
            }
            else if (nwanted == 3)
            {
                points = new Vector3[3];
                points[0] = aPoint + new Vector3(length, 0.0, 0.0);
                points[1] = aPoint + new Vector3(-length * 0.5, -length * 0.5 * Math.Sqrt(3.0), 0.0f);
                points[2] = aPoint + new Vector3(-length * 0.5, length * 0.5 * Math.Sqrt(3.0), 0.0f);
            }
            else if (nwanted == 4)
            {
                points = new Vector3[4];
                double dx = length / Math.Sqrt(3.0);
                points[0] = aPoint + new Vector3(dx, dx, dx);
                points[1] = aPoint + new Vector3(dx, -dx, -dx);
                points[2] = aPoint + new Vector3(-dx, -dx, dx);
                points[3] = aPoint + new Vector3(-dx, dx, -dx);
            }
            return points;
        }

        /// <summary>
        /// Calculate new Point(s) X in a B-A system to form B-A-X. Use C as reference
        /// for * staggering about the B-A bond (1a) 1 Ligand(B) of refAtom (A) which
        /// itself has a ligand (C) (i) 1 points required; vector along AB vector (ii)
        /// 2 points: 2 vectors in ABC plane, staggered and eclipsed wrt C (iii) 3
        /// points: 1 staggered wrt C, the others +- gauche wrt C If C is null, a
        /// random non-colinear C is generated
        /// </summary>
        /// <param name="aPoint">to which substituents are added</param>
        /// <param name="nwanted">number of points to calculate (1-3)</param>
        /// <param name="length">A-X length</param>
        /// <param name="angle">B-A-X angle</param>
        /// <param name="bPoint">Description of the Parameter</param>
        /// <param name="cPoint">Description of the Parameter</param>
        /// <returns>Vector3[] nwanted points (or zero if failed)</returns>
        public virtual Vector3[] Calculate3DCoordinates1(Vector3 aPoint, Vector3 bPoint, Vector3? cPoint, int nwanted, double length, double angle)
        {
            Vector3[] points = new Vector3[nwanted];
            // BA vector
            Vector3 ba = aPoint - bPoint;
            ba = Vector3.Normalize(ba);
            // if no cPoint, generate a random reference
            if (cPoint == null)
            {
                Vector3 cVector = GetNonColinearVector(ba);
                cPoint = cVector;
            }
            // CB vector
            Vector3 cb = bPoint - cPoint.Value;
            cb = Vector3.Normalize(cb);
            // if A, B, C colinear, replace C by random point
            var cbdotba = Vector3.Dot(cb, ba);
            if (cbdotba > 0.999999)
            {
                Vector3 cVector = GetNonColinearVector(ba);
                cPoint = cVector;
                cb = bPoint - cPoint.Value;
            }
            // cbxba = c x b
            Vector3 cbxba = Vector3.Cross(cb, ba);
            cbxba = Vector3.Normalize(cbxba);
            // create three perp axes ba, cbxba, and ax
            Vector3 ax = Vector3.Cross(cbxba, ba);
            ax = Vector3.Normalize(ax);
            double drot = Math.PI * 2.0 / (double)nwanted;
            for (int i = 0; i < nwanted; i++)
            {
                double rot = (double)i * drot;
                points[i] = aPoint;
                Vector3 vx = ba;
                vx *= -Math.Cos(angle) * length;
                Vector3 vy = ax;
                vy *= (float)(Math.Cos(rot) * length);
                Vector3 vz = cbxba;
                vz *= (float)(Math.Sin(rot) * length);
                points[i] += vx;
                points[i] += vy;
                points[i] += vz;
            }
            
            // ax = null; cbxba = null; ba = null; cb = null;
            return points;
        }

        /// <summary>
        /// Calculate new Point(s) X in a B-A-C system, it forms a B-A(-C)-X
        /// system. (2) 2 Ligands(B, C) of refAtom A (i) 1 points required; vector in
        /// ABC plane bisecting AB, AC. If ABC is linear, no points (ii) 2 points: 2
        /// points X1, X2, X1-A-X2 = angle about 2i vector
        /// </summary>
        /// <param name="aPoint">to which substituents are added</param>
        /// <param name="bPoint">first ligand of A</param>
        /// <param name="cPoint">second ligand of A</param>
        /// <param name="nwanted">number of points to calculate (1-2)</param>
        /// <param name="length">A-X length</param>
        /// <param name="angle">B-A-X angle</param>
        /// <returns>Vector3[] nwanted points (or zero if failed)</returns>
        public virtual Vector3[] Calculate3DCoordinates2(Vector3 aPoint, Vector3 bPoint, Vector3 cPoint, int nwanted, double length, double angle)
        {
            //Debug.WriteLine("3DCoordinates2");
            Vector3[] newPoints = Array.Empty<Vector3>();
            double ang2 = angle / 2.0;

            Vector3 ba = aPoint - bPoint;
            Vector3 ca = aPoint - cPoint;
            Vector3 baxca = Vector3.Cross(ba, ca);
            if (baxca.Length() < 0.00000001)
            {
                ;
                // linear
            }
            else if (nwanted == 1)
            {
                newPoints = new Vector3[1];
                Vector3 ax = ba + ca;
                ax = Vector3.Normalize(ax);
                ax *= length;
                newPoints[0] = aPoint + ax;
            }
            else if (nwanted >= 2)
            {
                newPoints = new Vector3[2];
                Vector3 ax = ba + ca;
                ax = Vector3.Normalize(ax);
                baxca = Vector3.Normalize(baxca);
                baxca *= (float)(Math.Sin(ang2) * length);
                ax *= (float)(Math.Cos(ang2) * length);
                newPoints[0] = aPoint + ax + baxca;
                newPoints[1] = aPoint + ax - baxca;
            }
            return newPoints;
        }

        /// <summary>
        /// Calculate new point X in a B-A(-D)-C system. It forms a B-A(-D)(-C)-X
        /// system. (3) 3 Ligands(B, C, D) of refAtom A (i) 1 points required; if A, B,
        /// C, D coplanar, no points. else vector is resultant of BA, CA, DA
        /// </summary>
        /// <param name="aPoint">to which substituents are added</param>
        /// <param name="bPoint">first ligand of A</param>
        /// <param name="cPoint">second ligand of A</param>
        /// <param name="dPoint">third ligand of A</param>
        /// <param name="length">A-X length</param>
        /// <returns>Vector3 nwanted points (or null if failed (coplanar))</returns>
        public virtual Vector3 Calculate3DCoordinates3(Vector3 aPoint, Vector3 bPoint, Vector3 cPoint, Vector3 dPoint, double length)
        {
            //Debug.WriteLine("3DCoordinates3");
            Vector3 bc = bPoint - cPoint;
            Vector3 dc = dPoint - cPoint;
            Vector3 ca = cPoint - aPoint;

            Vector3 n1 = Vector3.Cross(bc, dc);
            n1 = Vector3.Normalize(n1);
            n1 *= length;

            Vector3 n2 = new Vector3();

            Vector3 ax = aPoint + n1 - aPoint;
            Vector3 ax2 = aPoint + n2 - aPoint;

            Vector3 point = aPoint;

            double dotProduct = Vector3.Dot(ca, ax);
            double angle = Math.Acos((dotProduct) / (ax.Length() * ca.Length()));

            if (angle < 1.5)
            {
                n2 = Vector3.Cross(dc, bc);
                n2 = Vector3.Normalize(n2);
                n2 *= length;
                point += n2;
            }
            else
            {
                point += n1;
            }
            return point;
        }

        /// <summary>
        /// Calculate new point in B-A-C system. It forms B-A(-X)-C system, where A is sp2
        /// </summary>
        /// <param name="aPoint">central point A (Vector3)</param>
        /// <param name="bPoint">B (Vector3)</param>
        /// <param name="cPoint">C (Vector3)</param>
        /// <param name="length">bond length</param>
        /// <param name="angle">angle between B(C)-A-X</param>
        /// <returns>new Point (Vector3)</returns>
        private static Vector3 Calculate3DCoordinatesSP2_1(Vector3 aPoint, Vector3 bPoint, Vector3 cPoint, double length, double angle)
        {
            //Debug.WriteLine("3DCoordinatesSP2_1");
            Vector3 ba = bPoint - aPoint;
            Vector3 ca = cPoint - aPoint;

            Vector3 n1 = Vector3.Cross(ba, ca);
            n1 = Vector3.Normalize(n1);

            Vector3 n2 = Rotate(ba, n1, angle);
            n2 = Vector3.Normalize(n2);

            n2 *= length;
            Vector3 point = aPoint;
            point += n2;
            return point;
        }

        /// <summary>
        /// Calculate two new points in B-A system. It forms B-A(-X)(-X) system, where A is sp2
        /// </summary>
        /// <param name="aPoint">central point A (Vector3)</param>
        /// <param name="bPoint">B (Vector3)</param>
        /// <param name="cPoint">C (Vector3)</param>
        /// <param name="length">bond length</param>
        /// <param name="angle">angle between B(C)-A-X</param>
        /// <returns>new Points (Vector3 [])</returns>
        private static Vector3[] Calculate3DCoordinatesSP2_2(Vector3 aPoint, Vector3 bPoint, Vector3? cPoint, double length, double angle)
        {
            //Debug.WriteLine("3DCoordinatesSP_2");
            Vector3 ca = new Vector3();
            Vector3[] newPoints = new Vector3[2];
            Vector3 ba = bPoint - aPoint;

            if (cPoint != null)
            {
                ca.X = cPoint.Value.X - aPoint.X;
                ca.Y = cPoint.Value.Y - aPoint.Y;
                ca.Z = cPoint.Value.Z - aPoint.Z;
            }
            else
            {
                ca.X = -1 * ba.X;
                ca.Y = -1 * ba.Y;
                ca.Z = -1.5f * ba.Z;
            }

            Vector3 crossProduct = Vector3.Cross(ba, ca);

            Vector3 n1 = Rotate(ba, crossProduct, 2 * angle);
            n1 = Vector3.Normalize(n1);
            n1 *= length;
            newPoints[0] = aPoint + n1;

            Vector3 n2 = Rotate(n1, ba, Math.PI);
            n2 = Vector3.Normalize(n2);
            n2 *= length;
            newPoints[1] = aPoint + n2;
            return newPoints;
        }

        /// <summary>
        /// Gets the non-colinear vector attribute of the AtomLigandPlacer3D class
        /// </summary>
        /// <param name="ab">Description of the Parameter</param>
        /// <returns>The non-colinear vector value</returns>
        private static Vector3 GetNonColinearVector(Vector3 ab)
        {
            Vector3 cr = Vector3.Cross(ab, XV);
            if (cr.Length() > 0.00001)
            {
                return XV;
            }
            else
            {
                return YV;
            }
        }

        /// <summary>
        /// Rotates a vector around an axis.
        /// </summary>
        /// <param name="vector">vector to be rotated around axis</param>
        /// <param name="axis">axis of rotation</param>
        /// <param name="angle">angle to vector rotate around</param>
        /// <returns>rotated vector</returns>
        // @author:         egonw
        public static Vector3 Rotate(Vector3 vector, Vector3 axis, double angle)
        {
            Quaternion rotate = Vectors.NewQuaternionFromAxisAngle(axis.X, axis.Y, axis.Z, angle);
            Vector3 result = Vector3.Transform(vector, rotate);
            return result;
        }

        /// <summary>
        /// Gets the distance between two atoms out of the parameter set.
        /// </summary>
        /// <param name="id1">id of the parameter set for atom1 (atom1.AtomTypeName)</param>
        /// <param name="id2">id of the parameter set for atom2</param>
        /// <returns>The distanceValue value</returns>
        /// <exception cref="Exception"> Description of the Exception</exception>
        private double GetDistanceValue(string id1, string id2)
        {
            string dkey = "";
            if (pSet.ContainsKey(("bond" + id1 + ";" + id2)))
            {
                dkey = "bond" + id1 + ";" + id2;
            }
            else if (pSet.ContainsKey(("bond" + id2 + ";" + id1)))
            {
                dkey = "bond" + id2 + ";" + id1;
            }
            else
            {
                //            Debug.WriteLine("DistanceKEYError:pSet has no key:" + id2 + " ; " + id1 + " take default bond length:" + DEFAULT_BOND_LENGTH_H);
                return DefaultBondLengthH;
            }
            return ((IList<double>)pSet[dkey])[0];
        }

        /// <summary>
        /// Gets the angleKey attribute of the AtomPlacer3D object.
        /// </summary>
        /// <param name="id1">Description of the Parameter</param>
        /// <param name="id2">Description of the Parameter</param>
        /// <param name="id3">Description of the Parameter</param>
        /// <returns>The angleKey value</returns>
        public double GetAngleValue(string id1, string id2, string id3)
        {
            string akey = "";
            if (pSet.ContainsKey(("angle" + id1 + ";" + id2 + ";" + id3)))
            {
                akey = "angle" + id1 + ";" + id2 + ";" + id3;
            }
            else if (pSet.ContainsKey(("angle" + id3 + ";" + id2 + ";" + id1)))
            {
                akey = "angle" + id3 + ";" + id2 + ";" + id1;
            }
            else if (pSet.ContainsKey(("angle" + id2 + ";" + id1 + ";" + id3)))
            {
                akey = "angle" + id2 + ";" + id1 + ";" + id3;
            }
            else if (pSet.ContainsKey(("angle" + id1 + ";" + id3 + ";" + id2)))
            {
                akey = "angle" + id1 + ";" + id3 + ";" + id2;
            }
            else if (pSet.ContainsKey(("angle" + id3 + ";" + id1 + ";" + id2)))
            {
                akey = "angle" + id3 + ";" + id1 + ";" + id2;
            }
            else if (pSet.ContainsKey(("angle" + id2 + ";" + id3 + ";" + id1)))
            {
                akey = "angle" + id2 + ";" + id3 + ";" + id1;
            }
            else
            {
                Trace.TraceInformation($"AngleKEYError:Unknown angle {id1} {id2} {id3} take default angle:{TETRAHEDRAL_ANGLE}");
                return TETRAHEDRAL_ANGLE;
            }
            return ((IList<double>)pSet[akey])[0];
        }

        /// <summary>
        /// Set Atoms in respect to stereoinformation.
        ///    take placed neighbours to stereocenter
        ///        create a x b
        ///         if right handed system (spatproduct &gt;0)
        ///            if unplaced is not up (relative to stereocenter)
        ///                n=b x a
        ///         Determine angle between n and possible ligand place points
        ///         if angle smaller than 90 degrees take this branch point
        /// </summary>
        /// <param name="atomA">placed Atom - stereocenter</param>
        /// <param name="ax">bond between stereocenter and unplaced atom</param>
        /// <param name="atomB">neighbour of atomA (in plane created by atomA, atomB and atomC)</param>
        /// <param name="atomC">neighbour of atomA</param>
        /// <param name="branchPoints">the two possible placement points for unplaced atom (up and down)</param>
        /// <returns>int value of branch point position</returns>
        public static int MakeStereocenter(Vector3 atomA, IBond ax, Vector3 atomB, Vector3 atomC, Vector3[] branchPoints)
        {
            Vector3 b = new Vector3((atomB.X - atomA.X), (atomB.Y - atomA.Y), (atomB.Z - atomA.Z));
            Vector3 c = new Vector3((atomC.X - atomA.X), (atomC.Y - atomA.Y), (atomC.Z - atomA.Z));
            Vector3 n1 = Vector3.Cross(b, c);
            n1 = Vector3.Normalize(n1);

            if (GetSpatproduct(b, c, n1) >= 0)
            {
                if (ax.Stereo != BondStereo.UpInverted)
                {
                    n1 = Vector3.Cross(c, b);
                    n1 = Vector3.Normalize(n1);
                }
            }
            double dotProduct = 0;
            for (int i = 0; i < branchPoints.Length; i++)
            {
                Vector3 n2 = new Vector3(branchPoints[0].X, branchPoints[0].Y, branchPoints[0].Z);
                dotProduct = Vector3.Dot(n1, n2);
                if (Math.Acos(dotProduct / (n1.Length() * n2.Length())) < 1.6)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Gets the spatproduct of three vectors.
        /// </summary>
        /// <param name="a">vector a</param>
        /// <param name="b">vector b</param>
        /// <param name="c">vector c</param>
        /// <returns>double value of the spatproduct</returns>
        private static double GetSpatproduct(Vector3 a, Vector3 b, Vector3 c)
        {
            return (c.X * (b.Y * a.Z - b.Z * a.Y) + c.Y * (b.Z * a.X - b.X * a.Z) + c.Z * (b.X * a.Y - b.Y * a.X));
        }

        /// <summary>
        /// Calculates the torsionAngle of a-b-c-d.
        /// </summary>
        /// <param name="a">Vector3</param>
        /// <param name="b">Vector3</param>
        /// <param name="c">Vector3</param>
        /// <param name="d">Vector3</param>
        /// <returns>The torsionAngle value</returns>
        public static double GetTorsionAngle(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Vector3 ab = new Vector3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
            Vector3 cb = new Vector3(c.X - b.X, c.Y - b.Y, c.Z - b.Z);
            Vector3 dc = new Vector3(d.X - c.X, d.Y - c.Y, d.Z - c.Z);
            Vector3 bc = new Vector3(b.X - c.X, b.Y - c.Y, b.Z - c.Z);
            Vector3 n1 = Vector3.Cross(ab, cb);

            if (GetSpatproduct(ab, cb, n1) > 0)
            {
                n1 = Vector3.Cross(cb, ab);
            }
            n1 = Vector3.Normalize(n1);
            Vector3 n2 = Vector3.Cross(dc, bc);
            if (GetSpatproduct(dc, bc, n2) < 0)
            {
                n2 = Vector3.Cross(bc, dc);
            }
            n2 = Vector3.Normalize(n2);
            return Vector3.Dot(n1, n2);
        }

        /// <summary>
        /// Gets all placed neighbouring atoms of a atom.
        /// </summary>
        /// <param name="atom">central atom (Atom)</param>
        /// <param name="ac">the molecule</param>
        /// <returns>all connected and placed atoms to the central atom (AtomContainer)</returns>
        internal static IAtomContainer GetPlacedAtomsInAtomContainer(IAtom atom, IAtomContainer ac)
        {
            var bonds = ac.GetConnectedBonds(atom);
            IAtomContainer connectedAtoms = atom.Builder.NewAtomContainer();
            IAtom connectedAtom = null;
            foreach (var bond in bonds)
            {
                connectedAtom = bond.GetOther(atom);
                if (connectedAtom.IsPlaced)
                {
                    connectedAtoms.Atoms.Add(connectedAtom);
                }
            }
            return connectedAtoms;
        }

        internal static IAtomContainer GetUnsetAtomsInAtomContainer(IAtom atom, IAtomContainer ac)
        {
            var atoms = ac.GetConnectedAtoms(atom);
            IAtomContainer connectedAtoms = atom.Builder.NewAtomContainer();
            foreach (var curAtom in atoms)
            {
                if (!curAtom.IsPlaced)
                {//&& atoms[i].Point3D == null) {
                    connectedAtoms.Atoms.Add(curAtom);
                }
            }
            return connectedAtoms;
        }

        private static bool HasUnsetNeighbour(IAtom atom, IAtomContainer ac)
        {
            var atoms = ac.GetConnectedAtoms(atom);
            foreach (var curAtom in atoms)
            {
                if (!curAtom.IsPlaced)
                {//&& atoms[i].Point3D == null) {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a placed neighbouring atom of a central atom atomA, which is not atomB.
        /// </summary>
        /// <param name="atomA">central atom (Atom)</param>
        /// <param name="atomB">atom connected to atomA (Atom)</param>
        /// <param name="ac">molecule</param>
        /// <returns>returns a connected atom (Atom)</returns>
        private static IAtom GetPlacedHeavyAtomInAtomContainer(IAtom atomA, IAtom atomB, IAtomContainer ac)
        {
            var atoms = ac.GetConnectedAtoms(atomA);
            IAtom atom = null;
            foreach (var curAtom in atoms)
            {
                if (curAtom.IsPlaced && !curAtom.AtomicNumber.Equals(AtomicNumbers.H) && curAtom != atomB)
                {
                    return curAtom;
                }
            }
            return atom;
        }
    }
}
