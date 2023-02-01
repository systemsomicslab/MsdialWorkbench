/* Copyright (C) 2005-2009  The Jmol Development Team
 * Copyright (C) 2010 Gilleain Torrance <gilleain.torrance@gmail.com>
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Numerics;

namespace NCDK.Stereo
{
    /// <summary>
    /// Methods to determine or check the stereo class of a set of atoms.
    /// </summary>
    /// <remarks>
    /// Some of these methods were adapted from Jmol's smiles search package.
    /// </remarks>
    // @author maclean
    // @cdk.module standard
    public static class StereoTool
    {
        /// <summary>
        /// The handedness of a tetrahedron, in terms of the point-plane distance
        /// of three of the corners, compared to the fourth.
        /// </summary>
        public enum TetrahedralSign
        {
            /// <summary>
            /// Indices a positive point-plane distance
            /// </summary>
            Plus,
            /// <summary>
            /// A negative point-plane distance
            /// </summary>
            Minus
        }

        /// <summary>
        /// The shape that four atoms take in a plane.
        /// </summary>
        public enum SquarePlanarShape
        {
            UShape, FourShape, ZShape
        }

        /// <summary>
        /// The maximum angle in radians for two lines to be 'diaxial'.
        /// Where 0.95 is about 172 degrees.
        /// </summary>
        public const double MaxAxisAngle = 0.95;

        /// <summary>
        /// The maximum tolerance for the normal calculated during colinearity.
        /// </summary>
        public const double MinColinarNormal = 0.05;

        public const double PlaneTolerance = 0.05;

        /// <summary>
        /// Checks these four atoms for square planarity.
        /// </summary>
        /// <param name="atomA">an atom in the plane</param>
        /// <param name="atomB">an atom in the plane</param>
        /// <param name="atomC">an atom in the plane</param>
        /// <param name="atomD">an atom in the plane</param>
        /// <returns>true if all the atoms are in the same plane</returns>
        public static bool IsSquarePlanar(IAtom atomA, IAtom atomB, IAtom atomC, IAtom atomD)
        {
            Vector3 pointA = atomA.Point3D.Value;
            Vector3 pointB = atomB.Point3D.Value;
            Vector3 pointC = atomC.Point3D.Value;
            Vector3 pointD = atomD.Point3D.Value;

            return IsSquarePlanar(pointA, pointB, pointC, pointD);
        }

        private static bool IsSquarePlanar(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 pointD)
        {
            var normal = new Vector3();
            return IsSquarePlanar(pointA, pointB, pointC, pointD, out normal);
        }

        private static bool IsSquarePlanar(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 pointD, out Vector3 normal)
        {
            // define a plane using ABC, also checking that the are not colinear
            Vector3 vectorAB = new Vector3();
            Vector3 vectorAC = new Vector3();
            GetRawNormal(pointA, pointB, pointC, out normal, out vectorAB, out vectorAC);
            if (StereoTool.IsColinear(normal)) return false;

            // check that F is in the same plane as CDE
            return StereoTool.AllCoplanar(normal, pointC, pointD);
        }

        /// <summary>
        /// <para>Given four atoms (assumed to be in the same plane), returns the
        /// arrangement of those atoms in that plane.</para>
        ///
        /// <para>The 'shapes' returned represent arrangements that look a little like
        /// the characters 'U', '4', and 'Z'.</para>
        /// </summary>
        /// <param name="atomA">an atom in the plane</param>
        /// <param name="atomB">an atom in the plane</param>
        /// <param name="atomC">an atom in the plane</param>
        /// <param name="atomD">an atom in the plane</param>
        /// <returns>the shape (U/4/Z)</returns>
        public static SquarePlanarShape GetSquarePlanarShape(IAtom atomA, IAtom atomB, IAtom atomC, IAtom atomD)
        {
            Vector3 pointA = atomA.Point3D.Value;
            Vector3 pointB = atomB.Point3D.Value;
            Vector3 pointC = atomC.Point3D.Value;
            Vector3 pointD = atomD.Point3D.Value;

            // normalA normalB normalC are right-hand normals for the given
            // triangles
            // A-B-C, B-C-D, C-D-A
            Vector3 normalA = new Vector3();
            Vector3 normalB = new Vector3();
            Vector3 normalC = new Vector3();

            // these are temporary vectors that are re-used in the calculations
            Vector3 tmpX = new Vector3();
            Vector3 tmpY = new Vector3();

            // the normals (normalA, normalB, normalC) are calculated
            StereoTool.GetRawNormal(pointA, pointB, pointC, out normalA, out tmpX, out tmpY);
            StereoTool.GetRawNormal(pointB, pointC, pointD, out normalB, out tmpX, out tmpY);
            StereoTool.GetRawNormal(pointC, pointD, pointA, out normalC, out tmpX, out tmpY);

            // normalize the normals
            Vector3.Normalize(normalA);
            Vector3.Normalize(normalB);
            Vector3.Normalize(normalC);

            // sp1 up up up U-shaped
            // sp2 up up Down 4-shaped
            // sp3 up Down Down Z-shaped
            var aDotB = Vector3.Dot(normalA, normalB);
            double aDotC = Vector3.Dot(normalA, normalC);
            double bDotC = Vector3.Dot(normalB, normalC);
            if (aDotB > 0 && aDotC > 0 && bDotC > 0)
            { // UUU or DDD
                return SquarePlanarShape.UShape;
            }
            else if (aDotB > 0 && aDotC < 0 && bDotC < 0)
            { // UUD or DDU
                return SquarePlanarShape.FourShape;
            }
            else
            { // UDD or DUU
                return SquarePlanarShape.ZShape;
            }
        }

        /// <summary>
        /// Check that all the points in the list are coplanar (in the same plane)
        /// as the plane defined by the planeNormal and the pointInPlane.
        /// </summary>
        /// <param name="planeNormal">the normal to the plane</param>
        /// <param name="pointInPlane">any point know to be in the plane</param>
        /// <param name="points">an array of points to test</param>
        /// <returns>false if any of the points is not in the plane</returns>
        public static bool AllCoplanar(Vector3 planeNormal, Vector3 pointInPlane, params Vector3[] points)
        {
            foreach (var point in points)
            {
                double distance = StereoTool.SignedDistanceToPlane(planeNormal, pointInPlane, point);
                if (distance < PlaneTolerance)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks these 7 atoms to see if they are at the points of an octahedron.
        /// </summary>
        /// <param name="atomA">one of the axial atoms</param>
        /// <param name="atomB">the central atom</param>
        /// <param name="atomC">one of the equatorial atoms</param>
        /// <param name="atomD">one of the equatorial atoms</param>
        /// <param name="atomE">one of the equatorial atoms</param>
        /// <param name="atomF">one of the equatorial atoms</param>
        /// <param name="atomG">the other axial atom</param>
        /// <returns>true if the geometry is octahedral</returns>
        public static bool IsOctahedral(IAtom atomA, IAtom atomB, IAtom atomC, IAtom atomD, IAtom atomE, IAtom atomF, IAtom atomG)
        {
            Vector3 pointA = atomA.Point3D.Value;
            Vector3 pointB = atomB.Point3D.Value;
            Vector3 pointC = atomC.Point3D.Value;
            Vector3 pointD = atomD.Point3D.Value;
            Vector3 pointE = atomE.Point3D.Value;
            Vector3 pointF = atomF.Point3D.Value;
            Vector3 pointG = atomG.Point3D.Value;

            // the points on the axis should be in a line
            bool isColinearABG = IsColinear(pointA, pointB, pointG);
            if (!isColinearABG) return false;

            // check that CDEF are in a plane
            Vector3 normal = new Vector3();
            IsSquarePlanar(pointC, pointD, pointE, pointF, out normal);

            // now check rotation in relation to the first atom
            Vector3 vectorAB = Vector3.Subtract(pointA, pointB);

            // that is, they point in opposite directions
            return Vector3.Dot(normal, vectorAB) < 0;
        }

        /// <summary>
        /// Checks these 6 atoms to see if they form a trigonal-bipyramidal shape.
        /// </summary>
        /// <param name="atomA">one of the axial atoms</param>
        /// <param name="atomB">the central atom</param>
        /// <param name="atomC">one of the equatorial atoms</param>
        /// <param name="atomD">one of the equatorial atoms</param>
        /// <param name="atomE">one of the equatorial atoms</param>
        /// <param name="atomF">the other axial atom</param>
        /// <returns>true if the geometry is trigonal-bipyramidal</returns>
        public static bool IsTrigonalBipyramidal(IAtom atomA, IAtom atomB, IAtom atomC, IAtom atomD, IAtom atomE, IAtom atomF)
        {
            Vector3 pointA = atomA.Point3D.Value;
            Vector3 pointB = atomB.Point3D.Value;
            Vector3 pointC = atomC.Point3D.Value;
            Vector3 pointD = atomD.Point3D.Value;
            Vector3 pointE = atomE.Point3D.Value;
            Vector3 pointF = atomF.Point3D.Value;

            bool isColinearABF = StereoTool.IsColinear(pointA, pointB, pointF);
            if (isColinearABF)
            {
                // the normal to the equatorial plane
                Vector3 normal = StereoTool.GetNormal(pointC, pointD, pointE);

                // get the side of the plane that axis point A is
                TetrahedralSign handednessCDEA = StereoTool.GetHandedness(normal, pointC, pointF);

                // get the side of the plane that axis point F is
                TetrahedralSign handednessCDEF = StereoTool.GetHandedness(normal, pointC, pointA);

                // in other words, the two axial points (A,F) are on opposite sides
                // of the equatorial plane CDE
                return handednessCDEA != handednessCDEF;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Take four atoms, and return <see cref="TetrahedralStereo.Clockwise"/> or <see cref="TetrahedralStereo.AntiClockwise"/>.
        /// The first atom is the one pointing towards the observer.
        /// </summary>
        /// <param name="atom1">the atom pointing towards the observer</param>
        /// <param name="atom2">the second atom (points away)</param>
        /// <param name="atom3">the third atom (points away)</param>
        /// <param name="atom4">the fourth atom (points away)</param>
        /// <returns>clockwise or anticlockwise</returns>
        public static TetrahedralStereo GetStereo(IAtom atom1, IAtom atom2, IAtom atom3, IAtom atom4)
        {
            // a normal is calculated for the base atoms (2, 3, 4) and compared to
            // the first atom. PLUS indicates ACW.
            TetrahedralSign sign = StereoTool.GetHandedness(atom2, atom3, atom4, atom1);

            if (sign == TetrahedralSign.Plus)
            {
                return TetrahedralStereo.AntiClockwise;
            }
            else
            {
                return TetrahedralStereo.Clockwise;
            }
        }

        /// <summary>
        /// Gets the tetrahedral handedness of four atoms - three of which form the
        /// 'base' of the tetrahedron, and the other the apex. Note that it assumes
        /// a right-handed coordinate system, and that the points {A,B,C} are in
        /// a counter-clockwise order in the plane they share.
        /// </summary>
        /// <param name="baseAtomA">the first atom in the base of the tetrahedron</param>
        /// <param name="baseAtomB">the second atom in the base of the tetrahedron</param>
        /// <param name="baseAtomC">the third atom in the base of the tetrahedron</param>
        /// <param name="apexAtom">the atom in the point of the tetrahedron</param>
        /// <returns>the sign of the tetrahedron</returns>
        public static TetrahedralSign GetHandedness(IAtom baseAtomA, IAtom baseAtomB, IAtom baseAtomC, IAtom apexAtom)
        {
            Vector3 pointA = baseAtomA.Point3D.Value;
            Vector3 pointB = baseAtomB.Point3D.Value;
            Vector3 pointC = baseAtomC.Point3D.Value;
            Vector3 pointD = apexAtom.Point3D.Value;
            return StereoTool.GetHandedness(pointA, pointB, pointC, pointD);
        }

        private static TetrahedralSign GetHandedness(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 pointD)
        {
            // assumes anti-clockwise for a right-handed system
            Vector3 normal = StereoTool.GetNormal(pointA, pointB, pointC);

            // it doesn't matter which of points {A,B,C} is used
            return StereoTool.GetHandedness(normal, pointA, pointD);
        }

        private static TetrahedralSign GetHandedness(Vector3 planeNormal, Vector3 pointInPlane, Vector3 testPoint)
        {
            double distance = SignedDistanceToPlane(planeNormal, pointInPlane, testPoint);

            // The point-plane distance is the absolute value,
            // the sign of the distance gives the side of the plane the point is on
            // relative to the plane normal.
            if (distance > 0)
            {
                return TetrahedralSign.Plus;
            }
            else
            {
                return TetrahedralSign.Minus;
            }
        }

        /// <summary>
        /// Checks the three supplied points to see if they fall on the same line.
        /// It does this by finding the normal to an arbitrary pair of lines between
        /// the points (in fact, A-B and A-C) and checking that its length is 0.
        /// </summary>
        /// <param name="ptA"></param>
        /// <param name="ptB"></param>
        /// <param name="ptC"></param>
        /// <returns>true if the tree points are on a straight line</returns>
        public static bool IsColinear(Vector3 ptA, Vector3 ptB, Vector3 ptC)
        {
            Vector3 vectorAB = new Vector3();
            Vector3 vectorAC = new Vector3();
            Vector3 normal = new Vector3();

            StereoTool.GetRawNormal(ptA, ptB, ptC, out normal, out vectorAB, out vectorAC);
            return IsColinear(normal);
        }

        private static bool IsColinear(Vector3 normal)
        {
            double baCrossACLen = normal.Length();
            return baCrossACLen < StereoTool.MinColinarNormal;
        }

        /// <summary>
        /// Given a normalized normal for a plane, any point in that plane, and
        /// a point, will return the distance between the plane and that point.
        /// </summary>
        /// <param name="planeNormal">the normalized plane normal</param>
        /// <param name="pointInPlane">an arbitrary point in that plane</param>
        /// <param name="point">the point to measure</param>
        /// <returns>the signed distance to the plane</returns>
        public static double SignedDistanceToPlane(Vector3 planeNormal, Vector3 pointInPlane, Vector3 point)
        {
            if (planeNormal == null) return double.NaN;

            Vector3 pointPointDiff = new Vector3();
            pointPointDiff = Vector3.Subtract(point, pointInPlane);
            return Vector3.Dot(planeNormal, pointPointDiff);
        }

        /// <summary>
        /// Given three points (A, B, C), makes the vectors A-B and A-C, and makes
        /// the cross product of these two vectors; this has the effect of making a
        /// third vector at right angles to AB and AC.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// The returned normal is normalized; that is, it has been divided by its length.</note></remarks>
        /// <param name="ptA">the 'middle' point</param>
        /// <param name="ptB">one of the end points</param>
        /// <param name="ptC">one of the end points</param>
        /// <returns>the vector at right angles to AB and AC</returns>
        public static Vector3 GetNormal(Vector3 ptA, Vector3 ptB, Vector3 ptC)
        {
            Vector3 vectorAB = new Vector3();
            Vector3 vectorAC = new Vector3();
            Vector3 normal = new Vector3();
            StereoTool.GetRawNormal(ptA, ptB, ptC, out normal, out vectorAB, out vectorAC);
            Vector3.Normalize(normal);
            return normal;
        }

        private static void GetRawNormal(Vector3 ptA, Vector3 ptB, Vector3 ptC, out Vector3 normal, out Vector3 vcAB, out Vector3 vcAC)
        {
            // make A->B and A->C
            vcAB = Vector3.Subtract(ptB, ptA);
            vcAC = Vector3.Subtract(ptC, ptA);

            // make the normal to this
            normal = Vector3.Cross(vcAB, vcAC);
        }
    }
}
