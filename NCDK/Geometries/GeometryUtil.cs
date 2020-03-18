/*  Copyright (C) 1997-2014  The Chemistry Development Kit (CDK) project
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
 *  but WITHOUT Any WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Collections;
using NCDK.Common.Mathematics;
using NCDK.Numerics;
using NCDK.Sgroups;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Geometries
{
    /// <summary>
    /// A set of static utility classes for geometric calculations and operations. This class is
    /// extensively used, for example, by JChemPaint to edit molecule. All methods in this class change
    /// the coordinates of the atoms. Use GeometryTools if you use an external set of coordinates (e. g.
    /// renderingCoordinates from RendererModel)
    /// </summary>
    // @author seb
    // @author Stefan Kuhn
    // @author Egon Willighagen
    // @author Ludovic Petain
    // @author Christian Hoppe
    // @author Niels Out
    // @author John May
    public static class GeometryUtil
    {
        /// <summary>
        /// Provides the coverage of coordinates for this molecule.
        /// </summary>
        /// <seealso cref="Get2DCoordinateCoverage(IAtomContainer)"/>
        /// <seealso cref="Get3DCoordinateCoverage(IAtomContainer)"/>
        public enum CoordinateCoverage
        {
            /// <summary>
            /// All atoms have coordinates.
            /// </summary>
            Full,

            /// <summary>
            /// At least one atom has coordinates but not all.
            /// </summary>
            Partial,

            /// <summary>
            /// No atoms have coordinates.
            /// </summary>
            None
        }

        /// <summary>
        /// Adds an automatically calculated offset to the coordinates of all atoms such that all
        /// coordinates are positive and the smallest x or y coordinate is exactly zero. 
        /// </summary>
        /// <param name="atomCon">AtomContainer for which all the atoms are translated to positive coordinates</param>
        public static void TranslateAllPositive(IAtomContainer atomCon)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            foreach (var atom in atomCon.Atoms)
            {
                if (atom.Point2D != null)
                {
                    if (atom.Point2D.Value.X < minX)
                    {
                        minX = atom.Point2D.Value.X;
                    }
                    if (atom.Point2D.Value.Y < minY)
                    {
                        minY = atom.Point2D.Value.Y;
                    }
                }
            }
            Debug.WriteLine($"Translating: minx={minX}, minY={minY}");
            Translate2D(atomCon, minX * -1, minY * -1);
        }

        /// <summary>
        /// Translates the given molecule by the given Vector.
        /// </summary>
        /// <param name="atomCon">The molecule to be translated</param>
        /// <param name="transX">translation in x direction</param>
        /// <param name="transY">translation in y direction</param>
        public static void Translate2D(IAtomContainer atomCon, double transX, double transY)
        {
            Translate2D(atomCon, new Vector2(transX, transY));
        }

        /// <summary>
        /// Scales a molecule such that it fills a given percentage of a given dimension. 
        /// </summary>
        /// <param name="atomCon">The molecule to be scaled {width, height}</param>
        /// <param name="areaDim">The dimension to be filled {width, height}</param>
        /// <param name="fillFactor">The percentage of the dimension to be filled</param>
        public static void ScaleMolecule(IAtomContainer atomCon, double[] areaDim, double fillFactor)
        {
            var molDim = Get2DDimension(atomCon);
            var widthFactor = (double)areaDim[0] / (double)molDim[0];
            var heightFactor = (double)areaDim[1] / (double)molDim[1];
            var scaleFactor = Math.Min(widthFactor, heightFactor) * fillFactor;
            ScaleMolecule(atomCon, scaleFactor);
        }

        /// <summary>
        /// Multiplies all the coordinates of the atoms of the given molecule with the <paramref name="scaleFactor"/>. 
        /// </summary>
        /// <param name="atomCon">The molecule to be scaled</param>
        /// <param name="scaleFactor">scale factor</param>
        public static void ScaleMolecule(IAtomContainer atomCon, double scaleFactor)
        {
            for (int i = 0; i < atomCon.Atoms.Count; i++)
            {
                if (atomCon.Atoms[i].Point2D != null)
                {
                    atomCon.Atoms[i].Point2D = atomCon.Atoms[i].Point2D.Value * scaleFactor;
                }
            }
            // scale Sgroup brackets
            if (atomCon.GetCtabSgroups() != null)
            {
                var sgroups = atomCon.GetCtabSgroups();
                foreach (var sgroup in sgroups)
                {
                    var brackets = (IList<SgroupBracket>)sgroup.GetValue(SgroupKey.CtabBracket);
                    if (brackets != null)
                    {
                        foreach (var bracket in brackets)
                        {
                            bracket.FirstPoint = bracket.FirstPoint * scaleFactor;
                            bracket.SecondPoint = bracket.SecondPoint * scaleFactor;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Centers the molecule in the given area. 
        /// </summary>
        /// <param name="atomCon">molecule to be centered</param>
        /// <param name="areaDim">dimension in which the molecule is to be centered, array containing {width, height}</param>
        public static void Center(IAtomContainer atomCon, double[] areaDim)
        {
            var molDim = Get2DDimension(atomCon);
            var transX = (areaDim[0] - molDim[0]) / 2;
            var transY = (areaDim[1] - molDim[1]) / 2;
            TranslateAllPositive(atomCon);
            Translate2D(atomCon, new Vector2(transX, transY));
        }

        /// <summary>
        /// Translates a molecule from the origin to a new point denoted by a vector. See comment for
        /// Center(IAtomContainer atomCon, Dimension areaDim, Dictionary renderingCoordinates) for details
        /// on coordinate sets
        /// </summary>
        /// <param name="atomCon">molecule to be translated</param>
        /// <param name="vector">dimension that represents the translation vector</param>
        public static void Translate2D(IAtomContainer atomCon, Vector2 vector)
        {
            foreach (var atom in atomCon.Atoms)
            {
                if (atom.Point2D != null)
                {
                    atom.Point2D = atom.Point2D.Value + vector;
                }
                else
                {
                    Trace.TraceWarning("Could not translate atom in 2D space");
                }
            }
            // translate Sgroup brackets
            if (atomCon.GetCtabSgroups() != null)
            {
                var sgroups = atomCon.GetCtabSgroups();
                foreach (var sgroup in sgroups)
                {
                    IList<SgroupBracket> brackets = (IList<SgroupBracket>)sgroup.GetValue(SgroupKey.CtabBracket);
                    if (brackets != null)
                    {
                        foreach (var bracket in brackets)
                        {
                            bracket.FirstPoint = bracket.FirstPoint + vector;
                            bracket.SecondPoint = bracket.SecondPoint + vector;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Rotates a molecule around a given center by a given angle.
        /// </summary>
        /// <param name="atomCon">The molecule to be rotated</param>
        /// <param name="center">A point giving the rotation center</param>
        /// <param name="angle">The angle by which to rotate the molecule, in radians</param>
        public static void Rotate(IAtomContainer atomCon, Vector2 center, double angle)
        {
            var costheta = Math.Cos(angle);
            var sintheta = Math.Sin(angle);
            for (int i = 0; i < atomCon.Atoms.Count; i++)
            {
                var atom = atomCon.Atoms[i];
                var point = atom.Point2D.Value;
                var relativex = point.X - center.X;
                var relativey = point.Y - center.Y;
                point.X = (relativex * costheta - relativey * sintheta + center.X);
                point.Y = (relativex * sintheta + relativey * costheta + center.Y);
                atom.Point2D = point;
            }
        }

        /// <summary>
        /// Rotates a 3D point about a specified line segment by a specified angle.
        /// </summary>
        /// <remarks>
        /// The code is based on code available <see href="http://astronomy.swin.edu.au/~pbourke/geometry/rotate/source.c">here</see>.
        /// Positive angles are anticlockwise looking down the axis towards the origin. Assume right hand
        /// coordinate system.</remarks>
        /// <param name="atom">The atom to rotate</param>
        /// <param name="p1">The first point of the line segment</param>
        /// <param name="p2">The second point of the line segment</param>
        /// <param name="angle">The angle to rotate by (in degrees)</param>
        public static void Rotate(IAtom atom, Vector3 p1, Vector3 p2, double angle)
        {
            double costheta, sintheta;

            var r = new Vector3
            {
                X = p2.X - p1.X,
                Y = p2.Y - p1.Y,
                Z = p2.Z - p1.Z
            };
            r = Vector3.Normalize(r);

            angle = angle * Math.PI / 180.0;
            costheta = Math.Cos(angle);
            sintheta = Math.Sin(angle);

            var p = atom.Point3D.Value;
            p.X -= p1.X;
            p.Y -= p1.Y;
            p.Z -= p1.Z;

            var q = Vector3.Zero;
            q.X += ((costheta + (1 - costheta) * r.X * r.X) * p.X);
            q.X += (((1 - costheta) * r.X * r.Y - r.Z * sintheta) * p.Y);
            q.X += (((1 - costheta) * r.X * r.Z + r.Y * sintheta) * p.Z);

            q.Y += (((1 - costheta) * r.X * r.Y + r.Z * sintheta) * p.X);
            q.Y += ((costheta + (1 - costheta) * r.Y * r.Y) * p.Y);
            q.Y += (((1 - costheta) * r.Y * r.Z - r.X * sintheta) * p.Z);

            q.Z += (((1 - costheta) * r.X * r.Z - r.Y * sintheta) * p.X);
            q.Z += (((1 - costheta) * r.Y * r.Z + r.X * sintheta) * p.Y);
            q.Z += ((costheta + (1 - costheta) * r.Z * r.Z) * p.Z);

            q.X += p1.X;
            q.Y += p1.Y;
            q.Z += p1.Z;

            atom.Point3D = q;
        }

        /// <summary>
        /// Returns the dimension of a molecule (width/height).
        /// </summary>
        /// <param name="atomCon">of which the dimension should be returned</param>
        /// <returns>array containing {width, height}</returns>
        public static double[] Get2DDimension(IAtomContainer atomCon)
        {
            var minmax = GetMinMax(atomCon);
            var maxX = minmax[2];
            var maxY = minmax[3];
            var minX = minmax[0];
            var minY = minmax[1];
            return new double[] { maxX - minX, maxY - minY };
        }

        /// <summary>
        /// Returns the minimum and maximum X and Y coordinates of the atoms.
        /// The output is returned as: 
        /// <c>double[] { minX, minY, maxX, maxY }</c>.
        /// </summary>
        /// <param name="atoms"></param>
        /// <returns>An four int array as defined above.</returns>
        public static double[] GetMinMax(IEnumerable<IAtom> atoms)
        {
            double maxX = -double.MaxValue;
            double maxY = -double.MaxValue;
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            foreach (var atom in atoms)
            {
                if (atom.Point2D != null)
                {
                    if (atom.Point2D.Value.X > maxX)
                    {
                        maxX = atom.Point2D.Value.X;
                    }
                    if (atom.Point2D.Value.X < minX)
                    {
                        minX = atom.Point2D.Value.X;
                    }
                    if (atom.Point2D.Value.Y > maxY)
                    {
                        maxY = atom.Point2D.Value.Y;
                    }
                    if (atom.Point2D.Value.Y < minY)
                    {
                        minY = atom.Point2D.Value.Y;
                    }
                }
            }
            var minmax = new double[4];
            minmax[0] = minX;
            minmax[1] = minY;
            minmax[2] = maxX;
            minmax[3] = maxY;
            return minmax;
        }

        /// <summary>
        /// Returns the minimum and maximum X and Y coordinates of the atoms in the
        /// <see cref="IAtomContainer"/>. The output is returned as: 
        /// <c>double[] { minX, minY, maxX, maxY }</c>.
        /// </summary>
        /// <param name="container"></param>
        /// <returns>An four int array as defined above.</returns>
        public static double[] GetMinMax(IAtomContainer container)
        {
            return GetMinMax(container.Atoms);
        }

        /// <summary>
        /// Translates a molecule from the origin to a new point denoted by a vector. 
        /// </summary>
        /// <param name="atomCon">molecule to be translated</param>
        /// <param name="p">Description of the Parameter</param>
        public static void Translate2DCentreOfMassTo(IAtomContainer atomCon, Vector2 p)
        {
            var com = Get2DCentreOfMass(atomCon).Value;
            var translation = new Vector2(p.X - com.X, p.Y - com.Y);
            foreach (var atom in atomCon.Atoms)
            {
                if (atom.Point2D != null)
                {
                    atom.Point2D = atom.Point2D.Value + translation;
                }
            }
        }

        /// <summary>
        /// Calculates the center of the given atoms and returns it as a Vector2. 
        /// </summary>
        /// <param name="atoms">The vector of the given atoms</param>
        /// <returns>The center of the given atoms as Vector2</returns>
        public static Vector2 Get2DCenter(IEnumerable<IAtom> atoms)
        {
            double xsum = 0;
            double ysum = 0;
            int length = 0;
            foreach (var atom in atoms)
            {
                if (atom.Point2D != null)
                {
                    xsum += atom.Point2D.Value.X;
                    ysum += atom.Point2D.Value.Y;
                    length++;
                }
            }
            if (length == 0)
                return new Vector2(double.NaN, double.NaN);
            return new Vector2(xsum / length, ysum / length);
        }

        /// <summary>
        /// Returns the geometric center of all the rings in this ringset. 
        /// </summary>
        /// <param name="ringSet">Description of the Parameter</param>
        /// <returns>the geometric center of the rings in this ringset</returns>
        public static Vector2 Get2DCenter(IEnumerable<IRing> ringSet)
        {
            double centerX = 0;
            double centerY = 0;
            int count = 0;
            foreach (var ring in ringSet)
            {
                var centerPoint = Get2DCenter(ring);
                if (centerPoint != null)
                {
                    centerX += centerPoint.X;
                    centerY += centerPoint.Y;
                    count++;
                }
            }
            if (count == 0)
                return new Vector2(double.NaN, double.NaN);
            return new Vector2(centerX / count, centerY / count);
        }

        /// <summary>
        /// Calculates the center of mass for the <see cref="IAtom"/>s in the AtomContainer for the 2D
        /// coordinates. 
        /// </summary>
        /// <param name="ac">AtomContainer for which the center of mass is calculated</param>
        /// <returns>Null, if any of the atomcontainer <see cref="IAtom"/>'s</returns>
        /// masses are null
        // @cdk.keyword center of mass
        public static Vector2? Get2DCentreOfMass(IAtomContainer ac)
        {
            double xsum = 0.0;
            double ysum = 0.0;

            double totalmass = 0.0;

            foreach (var a in ac.Atoms)
            {
                var mass = a.ExactMass;
                if (mass == null)
                    return null;
                totalmass += mass.Value;
                xsum += mass.Value * a.Point2D.Value.X;
                ysum += mass.Value * a.Point2D.Value.Y;
            }

            return new Vector2((xsum / totalmass), (ysum / totalmass));
        }

        /// <summary>
        /// Returns the geometric center of all the atoms in the atomContainer.
        /// </summary>
        /// <param name="container">Description of the Parameter</param>
        /// <returns>the geometric center of the atoms in this atomContainer</returns>
        public static Vector2 Get2DCenter(IAtomContainer container)
        {
            double centerX = 0;
            double centerY = 0;
            double counter = 0;
            foreach (var atom in container.Atoms)
            {
                if (atom.Point2D != null)
                {
                    centerX += atom.Point2D.Value.X;
                    centerY += atom.Point2D.Value.Y;
                    counter++;
                }
            }
            if (counter == 0)
                return new Vector2(double.NaN, double.NaN);
            return new Vector2((centerX / counter), (centerY / counter));
        }

        /// <summary>
        /// Translates the geometric 2DCenter of the given AtomContainer container to the specified
        /// Vector2 p.
        /// </summary>
        /// <param name="container">AtomContainer which should be translated.</param>
        /// <param name="p">New Location of the geometric 2D Center.</param>
        /// <seealso cref="Get2DCenter(IAtomContainer)"/>
        /// <seealso cref="Translate2DCentreOfMassTo"/>
        public static void Translate2DCenterTo(IAtomContainer container, Vector2 p)
        {
            var com = Get2DCenter(container);
            if (com == null)
                return;
            var translation = new Vector2(p.X - com.X, p.Y - com.Y);
            foreach (var atom in container.Atoms)
            {
                if (atom.Point2D != null)
                {
                    atom.Point2D = atom.Point2D.Value + translation;
                }
            }
        }

        /// <summary>
        /// Calculates the center of mass for the <see cref="IAtom"/>s in the AtomContainer.
        /// </summary>
        /// <param name="ac">AtomContainer for which the center of mass is calculated</param>
        /// <returns>The center of mass of the molecule, or <see langword="null"/> if the molecule does not have 3D coordinates or if any of the atoms do not have a valid atomic mass</returns>
        // @cdk.keyword center of mass
        // @cdk.dictref blue-obelisk:calculate3DCenterOfMass
        public static Vector3? Get3DCentreOfMass(IAtomContainer ac)
        {
            double xsum = 0.0;
            double ysum = 0.0;
            double zsum = 0.0;

            double totalmass = 0.0;

            var isotopes = CDK.IsotopeFactory;

            foreach (var a in ac.Atoms)
            {
                var mass = a.ExactMass;
                // some sanity checking
                if (a.Point3D == null)
                    return null;
                if (mass == null)
                   mass = isotopes.GetNaturalMass(a.Element);

                totalmass += mass.Value;
                xsum += mass.Value * a.Point3D.Value.X;
                ysum += mass.Value * a.Point3D.Value.Y;
                zsum += mass.Value * a.Point3D.Value.Z;
            }

            return new Vector3((xsum / totalmass), (ysum / totalmass), (zsum / totalmass));
        }

        /// <summary>
        /// Returns the geometric center of all the atoms in this atomContainer. 
        /// </summary>
        /// <param name="ac">Description of the Parameter</param>
        /// <returns>the geometric center of the atoms in this atomContainer</returns>
        public static Vector3 Get3DCenter(IAtomContainer ac)
        {
            double centerX = 0;
            double centerY = 0;
            double centerZ = 0;
            double counter = 0;
            foreach (var atom in ac.Atoms)
            {
                if (atom.Point3D != null)
                {
                    centerX += atom.Point3D.Value.X;
                    centerY += atom.Point3D.Value.Y;
                    centerZ += atom.Point3D.Value.Z;
                    counter++;
                }
            }
            if (counter == 0)
                return new Vector3(double.NaN, double.NaN, double.NaN);
            return new Vector3((centerX / counter), (centerY / counter), (centerZ / counter));
        }

        /// <summary>
        /// Gets the angle attribute of the GeometryTools class.
        /// </summary>
        /// <param name="xDiff">Description of the Parameter</param>
        /// <param name="yDiff">Description of the Parameter</param>
        /// <returns>The angle value</returns>
        public static double GetAngle(double xDiff, double yDiff)
        {
            double angle = 0;
            if (xDiff >= 0 && yDiff >= 0)
            {
                angle = Math.Atan(yDiff / xDiff);
            }
            else if (xDiff < 0 && yDiff >= 0)
            {
                angle = Math.PI + Math.Atan(yDiff / xDiff);
            }
            else if (xDiff < 0 && yDiff < 0)
            {
                angle = Math.PI + Math.Atan(yDiff / xDiff);
            }
            else if (xDiff >= 0 && yDiff < 0)
            {
                angle = 2 * Math.PI + Math.Atan(yDiff / xDiff);
            }
            return angle;
        }

        /// <summary>
        /// Gets the coordinates of two points (that represent a bond) and calculates for each the
        /// coordinates of two new points that have the given distance vertical to the bond.
        /// </summary>
        /// <param name="coords">The coordinates of the two given points of the bond like this [point1x,  point1y, point2x, point2y]</param>
        /// <param name="dist">The vertical distance between the given points and those to be calculated</param>
        /// <returns>The coordinates of the calculated four points</returns>
        public static int[] DistanceCalculator(int[] coords, double dist)
        {
            double angle;
            if ((coords[2] - coords[0]) == 0)
            {
                angle = Math.PI / 2;
            }
            else
            {
                angle = Math.Atan(((double)coords[3] - (double)coords[1]) / ((double)coords[2] - (double)coords[0]));
            }
            int begin1X = (int)(Math.Cos(angle + Math.PI / 2) * dist + coords[0]);
            int begin1Y = (int)(Math.Sin(angle + Math.PI / 2) * dist + coords[1]);
            int begin2X = (int)(Math.Cos(angle - Math.PI / 2) * dist + coords[0]);
            int begin2Y = (int)(Math.Sin(angle - Math.PI / 2) * dist + coords[1]);
            int end1X = (int)(Math.Cos(angle - Math.PI / 2) * dist + coords[2]);
            int end1Y = (int)(Math.Sin(angle - Math.PI / 2) * dist + coords[3]);
            int end2X = (int)(Math.Cos(angle + Math.PI / 2) * dist + coords[2]);
            int end2Y = (int)(Math.Sin(angle + Math.PI / 2) * dist + coords[3]);

            return new int[] { begin1X, begin1Y, begin2X, begin2Y, end1X, end1Y, end2X, end2Y };
        }

        public static double[] DistanceCalculator(double[] coords, double dist)
        {
            double angle;
            if ((coords[2] - coords[0]) == 0)
            {
                angle = Math.PI / 2;
            }
            else
            {
                angle = Math.Atan((coords[3] - coords[1]) / (coords[2] - coords[0]));
            }
            double begin1X = (Math.Cos(angle + Math.PI / 2) * dist + coords[0]);
            double begin1Y = (Math.Sin(angle + Math.PI / 2) * dist + coords[1]);
            double begin2X = (Math.Cos(angle - Math.PI / 2) * dist + coords[0]);
            double begin2Y = (Math.Sin(angle - Math.PI / 2) * dist + coords[1]);
            double end1X = (Math.Cos(angle - Math.PI / 2) * dist + coords[2]);
            double end1Y = (Math.Sin(angle - Math.PI / 2) * dist + coords[3]);
            double end2X = (Math.Cos(angle + Math.PI / 2) * dist + coords[2]);
            double end2Y = (Math.Sin(angle + Math.PI / 2) * dist + coords[3]);

            return new double[] { begin1X, begin1Y, begin2X, begin2Y, end1X, end1Y, end2X, end2Y };
        }

        /// <summary>
        /// Writes the coordinates of the atoms participating the given bond into an array. 
        /// </summary>
        /// <param name="bond">The given bond</param>
        /// <returns>The array with the coordinates</returns>
        public static int[] GetBondCoordinates(IBond bond)
        {
            if (bond.Begin.Point2D == null || bond.End.Point2D == null)
            {
                Trace.TraceError("GetBondCoordinates() called on Bond without 2D coordinates!");
                return Array.Empty<int>();
            }
            var beginX = (int)bond.Begin.Point2D.Value.X;
            var endX = (int)bond.End.Point2D.Value.X;
            var beginY = (int)bond.Begin.Point2D.Value.Y;
            var endY = (int)bond.End.Point2D.Value.Y;
            return new int[] { beginX, beginY, endX, endY };
        }

        /// <summary>
        /// Returns the atom of the given molecule that is closest to the given coordinates. 
        /// </summary>
        /// <param name="xPosition">The x coordinate</param>
        /// <param name="yPosition">The y coordinate</param>
        /// <param name="atomCon">The molecule that is searched for the closest atom</param>
        /// <returns>The atom that is closest to the given coordinates</returns>
        public static IAtom GetClosestAtom(int xPosition, int yPosition, IAtomContainer atomCon)
        {
            IAtom closestAtom = null;
            double smallestMouseDistance = -1;
            for (int i = 0; i < atomCon.Atoms.Count; i++)
            {
                var currentAtom = atomCon.Atoms[i];
                var atomX = currentAtom.Point2D.Value.X;
                var atomY = currentAtom.Point2D.Value.Y;
                var mouseDistance = Math.Sqrt(Math.Pow(atomX - xPosition, 2) + Math.Pow(atomY - yPosition, 2));
                if (mouseDistance < smallestMouseDistance || smallestMouseDistance == -1)
                {
                    smallestMouseDistance = mouseDistance;
                    closestAtom = currentAtom;
                }
            }
            return closestAtom;
        }

        /// <summary>
        /// Returns the atom of the given molecule that is closest to the given atom (excluding itself).
        /// </summary>
        /// <param name="atomCon">The molecule that is searched for the closest atom</param>
        /// <param name="atom">The atom to search around</param>
        /// <returns>The atom that is closest to the given coordinates</returns>
        public static IAtom GetClosestAtom(IAtomContainer atomCon, IAtom atom)
        {
            IAtom closestAtom = null;
            double min = double.MaxValue;
            var atomPosition = atom.Point2D.Value;
            for (int i = 0; i < atomCon.Atoms.Count; i++)
            {
                var currentAtom = atomCon.Atoms[i];
                if (!currentAtom.Equals(atom))
                {
                    var d = Vector2.Distance(atomPosition, currentAtom.Point2D.Value);
                    if (d < min)
                    {
                        min = d;
                        closestAtom = currentAtom;
                    }
                }
            }
            return closestAtom;
        }

        /// <summary>
        /// Returns the atom of the given molecule that is closest to the given coordinates and is not
        /// the atom.
        /// </summary>
        /// <param name="xPosition">The x coordinate</param>
        /// <param name="yPosition">The y coordinate</param>
        /// <param name="atomCon">The molecule that is searched for the closest atom</param>
        /// <param name="toignore">This molecule will not be returned.</param>
        /// <returns>The atom that is closest to the given coordinates</returns>
        public static IAtom GetClosestAtom(double xPosition, double yPosition, IAtomContainer atomCon, IAtom toignore)
        {
            IAtom closestAtom = null;
            IAtom currentAtom;
            // we compare squared distances, allowing us to do one Sqrt()
            // calculation less
            double smallestSquaredMouseDistance = -1;
            for (int i = 0; i < atomCon.Atoms.Count; i++)
            {
                currentAtom = atomCon.Atoms[i];
                if (!currentAtom.Equals(toignore))
                {
                    var atomX = currentAtom.Point2D.Value.X;
                    var atomY = currentAtom.Point2D.Value.Y;
                    var mouseSquaredDistance = Math.Pow(atomX - xPosition, 2) + Math.Pow(atomY - yPosition, 2);
                    if (mouseSquaredDistance < smallestSquaredMouseDistance || smallestSquaredMouseDistance == -1)
                    {
                        smallestSquaredMouseDistance = mouseSquaredDistance;
                        closestAtom = currentAtom;
                    }
                }
            }
            return closestAtom;
        }

        /// <summary>
        /// Returns the atom of the given molecule that is closest to the given coordinates. 
        /// </summary>
        /// <param name="xPosition">The x coordinate</param>
        /// <param name="yPosition">The y coordinate</param>
        /// <param name="atomCon">The molecule that is searched for the closest atom</param>
        /// <returns>The atom that is closest to the given coordinates</returns>
        public static IAtom GetClosestAtom(double xPosition, double yPosition, IAtomContainer atomCon)
        {
            IAtom closestAtom = null;
            double smallestMouseDistance = -1;
            for (int i = 0; i < atomCon.Atoms.Count; i++)
            {
                var currentAtom = atomCon.Atoms[i];
                var atomX = currentAtom.Point2D.Value.X;
                var atomY = currentAtom.Point2D.Value.Y;
                var mouseDistance = Math.Sqrt(Math.Pow(atomX - xPosition, 2) + Math.Pow(atomY - yPosition, 2));
                if (mouseDistance < smallestMouseDistance || smallestMouseDistance == -1)
                {
                    smallestMouseDistance = mouseDistance;
                    closestAtom = currentAtom;
                }
            }
            return closestAtom;
        }

        /// <summary>
        /// Returns the bond of the given molecule that is closest to the given coordinates.
        /// </summary>
        /// <param name="xPosition">The x coordinate</param>
        /// <param name="yPosition">The y coordinate</param>
        /// <param name="atomCon">The molecule that is searched for the closest bond</param>
        /// <returns>The bond that is closest to the given coordinates</returns>
        public static IBond GetClosestBond(int xPosition, int yPosition, IAtomContainer atomCon)
        {
            IBond closestBond = null;
            double smallestMouseDistance = -1;
            foreach (var currentBond in atomCon.Bonds)
            {
                var bondCenter = Get2DCenter(currentBond.Atoms);
                if (bondCenter == null)
                    continue;
                var mouseDistance = Math.Sqrt(Math.Pow(bondCenter.X - xPosition, 2) + Math.Pow(bondCenter.Y - yPosition, 2));
                if (mouseDistance < smallestMouseDistance || smallestMouseDistance == -1)
                {
                    smallestMouseDistance = mouseDistance;
                    closestBond = currentBond;
                }
            }
            return closestBond;
        }

        /// <summary>
        /// Returns the bond of the given molecule that is closest to the given coordinates.
        /// </summary>
        /// <param name="xPosition">The x coordinate</param>
        /// <param name="yPosition">The y coordinate</param>
        /// <param name="atomCon">The molecule that is searched for the closest bond</param>
        /// <returns>The bond that is closest to the given coordinates</returns>
        public static IBond GetClosestBond(double xPosition, double yPosition, IAtomContainer atomCon)
        {
            IBond closestBond = null;
            double smallestMouseDistance = -1;
            foreach (var currentBond in atomCon.Bonds)
            {
                var bondCenter = Get2DCenter(currentBond.Atoms);
                if (bondCenter == null)
                    continue;
                var mouseDistance = Math.Sqrt(Math.Pow(bondCenter.X - xPosition, 2) + Math.Pow(bondCenter.Y - yPosition, 2));
                if (mouseDistance < smallestMouseDistance || smallestMouseDistance == -1)
                {
                    smallestMouseDistance = mouseDistance;
                    closestBond = currentBond;
                }
            }
            return closestBond;
        }

        /// <summary>
        /// Sorts a Vector of atoms such that the 2D distances of the atom locations from a given point
        /// are smallest for the first atoms in the vector. 
        /// </summary>
        /// <param name="point">The point from which the distances to the atoms are measured</param>
        /// <param name="atoms">The atoms for which the distances to point are measured</param>
        public static void SortBy2DDistance(IAtom[] atoms, Vector2 point)
        {
            bool doneSomething;
            do
            {
                doneSomething = false;
                for (int f = 0; f < atoms.Length - 1; f++)
                {
                    var atom1 = atoms[f];
                    var atom2 = atoms[f + 1];
                    var distance1 = Vector2.Distance(point, atom1.Point2D.Value);
                    var distance2 = Vector2.Distance(point, atom2.Point2D.Value);
                    if (distance2 < distance1)
                    {
                        atoms[f] = atom2;
                        atoms[f + 1] = atom1;
                        doneSomething = true;
                    }
                }
            } while (doneSomething);
        }

        /// <summary>
        /// Determines the scale factor for displaying a structure loaded from disk in a frame. An
        /// average of all bond length values is produced and a scale factor is determined which would
        /// scale the given molecule.
        /// </summary>
        /// <param name="container">The AtomContainer for which the ScaleFactor is to be calculated</param>
        /// <param name="bondLength">The target bond length</param>
        /// <returns>The ScaleFactor with which the AtomContainer must be scaled to have the target bond length</returns>
        public static double GetScaleFactor(IAtomContainer container, double bondLength)
        {
            var currentAverageBondLength = GetBondLengthMedian(container);
            if (currentAverageBondLength == 0 || double.IsNaN(currentAverageBondLength))
                return 1;
            return bondLength / currentAverageBondLength;
        }

        /// <summary>
        /// An average of all 2D bond length values is produced. Bonds which have Atom's with no
        /// coordinates are disregarded. See comment for Center(IAtomContainer atomCon, Dimension
        /// areaDim, Dictionary renderingCoordinates) for details on coordinate sets
        /// </summary>
        /// <param name="container">The AtomContainer for which the average bond length is to be calculated</param>
        /// <returns>the average bond length</returns>
        public static double GetBondLengthAverage(IAtomContainer container)
        {
            double bondLengthSum = 0;
            int bondCounter = 0;
            foreach (var bond in container.Bonds)
            {
                var atom1 = bond.Begin;
                var atom2 = bond.End;
                if (atom1.Point2D != null && atom2.Point2D != null)
                {
                    bondCounter++;
                    bondLengthSum += GetLength2D(bond);
                }
            }
            return bondLengthSum / bondCounter;
        }

        /// <summary>
        /// Returns the geometric length of this bond in 2D space. See comment for Center(IAtomContainer
        /// atomCon, Dimension areaDim, Dictionary renderingCoordinates) for details on coordinate sets
        /// </summary>
        /// <param name="bond">Description of the Parameter</param>
        /// <returns>The geometric length of this bond</returns>
        public static double GetLength2D(IBond bond)
        {
            if (bond.Begin == null || bond.End == null)
            {
                return 0.0;
            }
            var point1 = bond.Begin.Point2D.Value;
            var point2 = bond.End.Point2D.Value;
            if (point1 == null || point2 == null)
            {
                return 0.0;
            }
            return Vector2.Distance(point1, point2);
        }

        /// <summary>
        /// Determines if all this <see cref="IAtomContainer"/>'s atoms contain
        /// 2D coordinates. If any atom is null or has unset 2D coordinates this method will return
        /// false.
        /// </summary>
        /// <param name="container">the atom container to examine</param>
        /// <returns>indication that all 2D coordinates are available</returns>
        /// <seealso cref="IAtom.Point2D"/>
        public static bool Has2DCoordinates(IAtomContainer container)
        {
            if (container == null || container.Atoms.Count == 0)
                return false;

            foreach (var atom in container.Atoms)
            {
                if (atom == null || atom.Point2D == null)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Determine if all parts of a reaction have coodinates
        /// </summary>
        /// <param name="reaction">a reaction</param>
        /// <returns>the reaction has coordinates</returns>
        public static bool Has2DCoordinates(IReaction reaction)
        {
            foreach (IAtomContainer mol in reaction.Reactants)
                if (!Has2DCoordinates(mol))
                    return false;
            foreach (IAtomContainer mol in reaction.Products)
                if (!Has2DCoordinates(mol))
                    return false;
            foreach (IAtomContainer mol in reaction.Agents)
                if (!Has2DCoordinates(mol))
                    return false;
            return true;
        }
        
        /// <summary>
        /// Determines the coverage of this <see cref="IAtomContainer"/>'s 2D
        /// coordinates. If all atoms are non-null and have 2D coordinates this method will return 
        /// <see cref="CoordinateCoverage.Full"/>. If one or more atoms does have 2D coordinates and any others atoms
        /// are null or are missing 2D coordinates this method will return
        /// <see cref="CoordinateCoverage.Partial"/>. If all atoms are null or are all missing 2D coordinates this
        /// method will return <see cref="CoordinateCoverage.None"/>. If the provided container is null 
        /// <see cref="CoordinateCoverage.None"/> is also returned.
        /// </summary>
        /// <param name="container">the container to inspect</param>
        /// <returns><see cref="CoordinateCoverage.Full"/>, <see cref="CoordinateCoverage.Partial"/> or <see cref="CoordinateCoverage.None"/> depending on the number of 3D coordinates present</returns>
        /// <seealso cref="CoordinateCoverage"/>
        /// <seealso cref="Has2DCoordinates(IAtomContainer)"/>
        /// <seealso cref="Get3DCoordinateCoverage(IAtomContainer)"/>
        /// <seealso cref="IAtom.Point2D"/>
        public static CoordinateCoverage Get2DCoordinateCoverage(IAtomContainer container)
        {
            if (container == null || container.Atoms.Count == 0)
                return CoordinateCoverage.None;

            int count = 0;

            foreach (var atom in container.Atoms)
            {
                count += atom != null && atom.Point2D != null ? 1 : 0;
            }

            return count == 0 
                ? CoordinateCoverage.None 
                : count == container.Atoms.Count 
                ? CoordinateCoverage.Full
                : CoordinateCoverage.Partial;
        }

        /// <summary>
        /// Determines if this AtomContainer contains 2D coordinates for some or all molecules.
        /// </summary>
        /// <param name="container">the molecule to be considered</param>
        /// <returns>0 no 2d, 1=some, 2= for each atom</returns>
        /// <seealso cref="Get2DCoordinateCoverage(IAtomContainer)"/>
        [Obsolete("Use " + nameof(Get2DCoordinateCoverage) + "(" + nameof(IAtomContainer) + ") for determining partial coordinates")] 
        public static int Has2DCoordinatesNew(IAtomContainer container)
        {
            if (container == null)
                return 0;

            bool no2d = false;
            bool with2d = false;
            foreach (var atom in container.Atoms)
            {
                if (atom.Point2D == null)
                {
                    no2d = true;
                }
                else
                {
                    with2d = true;
                }
            }
            if (!no2d && with2d)
            {
                return 2;
            }
            else if (no2d && with2d)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Determines if this Atom contains 2D coordinates. 
        /// </summary>
        /// <param name="atom">Description of the Parameter</param>
        /// <returns>bool indication that 2D coordinates are available</returns>
        public static bool Has2DCoordinates(IAtom atom)
        {
            return atom.Point2D != null;
        }

        /// <summary>
        /// Determines if this Bond contains 2D coordinates.
        /// </summary>
        /// <param name="bond">Description of the Parameter</param>
        /// <returns>bool indication that 2D coordinates are available</returns>
        public static bool Has2DCoordinates(IBond bond)
        {
            foreach (var iAtom in bond.Atoms)
            {
                if (iAtom.Point2D == null)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Determines if all this <see cref="IAtomContainer"/>'s atoms contain
        /// 3D coordinates. If any atom is null or has unset 3D coordinates this method will return
        /// false. If the provided container is null false is returned.
        /// </summary>
        /// <param name="container">the atom container to examine</param>
        /// <returns>indication that all 3D coordinates are available</returns>
        /// <seealso cref="IAtom.Point3D"/>
        public static bool Has3DCoordinates(IAtomContainer container)
        {
            if (container == null || container.Atoms.Count == 0)
                return false;

            foreach (var atom in container.Atoms)
                if (atom == null || atom.Point3D == null)
                    return false;

            return true;
        }

        /// <summary>
        /// Determines the coverage of this <see cref="IAtomContainer"/>'s 3D
        /// coordinates. If all atoms are non-null and have 3D coordinates this method will return <see cref="CoordinateCoverage.Full"/>. If one or more atoms does have 3D coordinates and any others atoms
        /// are null or are missing 3D coordinates this method will return <see cref="CoordinateCoverage.Partial"/>. If all atoms are null or are all missing 3D coordinates this
        /// method will return <see cref="CoordinateCoverage.None"/>. If the provided container is null <see cref="CoordinateCoverage.None"/> is also returned.
        /// </summary>
        /// <param name="container">the container to inspect</param>
        /// <returns><see cref="CoordinateCoverage.Full"/>, <see cref="CoordinateCoverage.Partial"/> or <see cref="CoordinateCoverage.None"/> depending on the number of 3D coordinates present</returns>
        /// <seealso cref="CoordinateCoverage"/>
        /// <seealso cref="Has3DCoordinates(IAtomContainer)"/>
        /// <seealso cref="Get2DCoordinateCoverage(IAtomContainer)"/>
        /// <seealso cref="IAtom.Point3D"/>
        public static CoordinateCoverage Get3DCoordinateCoverage(IAtomContainer container)
        {
            if (container == null || container.Atoms.Count == 0)
                return CoordinateCoverage.None;

            int count = 0;
            foreach (var atom in container.Atoms)
                count += atom != null && atom.Point3D != null ? 1 : 0;

            return count == 0 
                ? CoordinateCoverage.None 
                : count == container.Atoms.Count 
                ? CoordinateCoverage.Full
                : CoordinateCoverage.Partial;
        }

        /// <summary>
        /// Determines the normalized vector orthogonal on the vector p1-&gt;p2.
        /// </summary>
        /// <param name="point1">Description of the Parameter</param>
        /// <param name="point2">Description of the Parameter</param>
        /// <returns>Description of the Return Value</returns>
        public static Vector2 CalculatePerpendicularUnitVector(Vector2 point1, Vector2 point2)
        {
            var vector = point2 - point1;
            vector = Vector2.Normalize(vector);

            // Return the perpendicular vector
            return new Vector2(-1 * vector.Y, vector.X);
        }

        /// <summary>
        /// Calculates the normalization factor in order to get an average bond length of 1.5. It takes
        /// only into account Bond's with two atoms.
        /// </summary>
        /// <param name="container">Description of the Parameter</param>
        /// <returns>The normalizationFactor value</returns>
        public static double GetNormalizationFactor(IAtomContainer container)
        {
            double bondlength = 0.0;
            double ratio;
            // Desired bond length for storing structures in MDL mol files This
            // should probably be set externally (from system wide settings)
            double desiredBondLength = 1.5;
            // loop over all bonds and determine the mean bond distance
            int counter = 0;
            foreach (var bond in container.Bonds)
            {
                // only consider two atom bonds into account
                if (bond.Atoms.Count == 2)
                {
                    counter++;
                    var atom1 = bond.Begin;
                    var atom2 = bond.End;
                    bondlength += 
                        Math.Sqrt(Math.Pow(atom1.Point2D.Value.X - atom2.Point2D.Value.X, 2)
                      + Math.Pow(atom1.Point2D.Value.Y - atom2.Point2D.Value.Y, 2));
                }
            }
            bondlength = bondlength / counter;
            ratio = desiredBondLength / bondlength;
            return ratio;
        }

        /// <summary>
        /// Determines the best alignment for the label of an atom in 2D space. It returns 1 if left
        /// aligned, and -1 if right aligned. 
        /// </summary>
        /// <param name="container">Description of the Parameter</param>
        /// <param name="atom">Description of the Parameter</param>
        /// <returns>The bestAlignmentForLabel value</returns>
        public static int GetBestAlignmentForLabel(IAtomContainer container, IAtom atom)
        {
            var overallDiffX = container.GetConnectedAtoms(atom).Select(n => n.Point2D.Value.X - atom.Point2D.Value.X).Sum();
            return overallDiffX <= 0 ? 1 : -1;
        }

        /// <summary>
        /// Determines the best alignment for the label of an atom in 2D space. It returns 1 if right
        /// (=default) aligned, and -1 if left aligned. returns 2 if top aligned, and -2 if H is aligned
        /// below the atom.
        /// </summary>
        /// <param name="container">Description of the Parameter</param>
        /// <param name="atom">Description of the Parameter</param>
        /// <returns>The bestAlignmentForLabel value</returns>
        public static int GetBestAlignmentForLabelXY(IAtomContainer container, IAtom atom)
        {
            double overallDiffX = 0;
            double overallDiffY = 0;
            foreach (var connectedAtom in container.GetConnectedAtoms(atom))
            {
                overallDiffX += connectedAtom.Point2D.Value.X - atom.Point2D.Value.X;
                overallDiffY += connectedAtom.Point2D.Value.Y - atom.Point2D.Value.Y;
            }

            return Math.Abs(overallDiffY) > Math.Abs(overallDiffX)
                ? (overallDiffY < 0 ? 2 : -2)
                : (overallDiffX <= 0 ? 1 : -1);
        }

        /// <summary>
        /// Returns the atoms which are closes to an atom in an AtomContainer by distance in 3d.
        /// </summary>
        /// <param name="container">The AtomContainer to examine</param>
        /// <param name="startAtom">the atom to start from</param>
        /// <param name="max">the number of neighbours to return</param>
        /// <returns>the average bond length</returns>
        public static IReadOnlyList<IAtom> FindClosestInSpace(IAtomContainer container, IAtom startAtom, int max)
        {
            if (startAtom.Point3D == null)
                throw new CDKException("No point3d, but FindClosestInSpace is working on point3ds");

            var originalPoint = startAtom.Point3D.Value;
            var atomsByDistance = new SortedDictionary<double, IAtom>();
            foreach (var atom in container.Atoms)
            {
                if (!atom.Equals(startAtom))
                {
                    if (atom.Point3D == null)
                        throw new CDKException("No point3d, but FindClosestInSpace is working on point3ds");

                    var distance = Vector3.Distance(atom.Point3D.Value, originalPoint);
                    atomsByDistance.Add(distance, atom);
                }
            }
            // FIXME: should there not be some sort here??
            var returnValue = new List<IAtom>();
            int i = 0;
            foreach (var key in atomsByDistance.Keys)
            {
                if (!(i < max))
                    break;
                returnValue.Add(atomsByDistance[key]);
                i++;
            }
            return returnValue;
        }

        /// <summary>
        /// Returns a IDictionary with the AtomNumbers, the first number corresponds to the first (or the largest
        /// AtomContainer) atom container. It is recommend to sort the atomContainer due to their number
        /// of atoms before calling this function.
        /// </summary>
        /// <remarks>
        /// The molecules needs to be aligned before! (coordinates are needed)
        /// </remarks>
        /// <param name="firstAtomContainer">the (largest) first aligned AtomContainer which is the reference</param>
        /// <param name="secondAtomContainer">the second aligned AtomContainer</param>
        /// <param name="searchRadius">the radius of space search from each atom</param>
        /// <param name="mappedAtoms"></param>
        public static void MapAtomsOfAlignedStructures(
            IAtomContainer firstAtomContainer,
            IAtomContainer secondAtomContainer,
            double searchRadius, 
            IDictionary<int, int> mappedAtoms)
        {
            var distanceMatrix = Arrays.CreateJagged<double>(firstAtomContainer.Atoms.Count, secondAtomContainer.Atoms.Count);
            for (int i = 0; i < firstAtomContainer.Atoms.Count; i++)
            {
                var firstAtomPoint = firstAtomContainer.Atoms[i].Point3D.Value;
                for (int j = 0; j < secondAtomContainer.Atoms.Count; j++)
                {
                    distanceMatrix[i][j] = Vector3.Distance(firstAtomPoint, secondAtomContainer.Atoms[j].Point3D.Value);
                }
            }

            for (int i = 0; i < firstAtomContainer.Atoms.Count; i++)
            {
                var minimumDistance = searchRadius;
                for (int j = 0; j < secondAtomContainer.Atoms.Count; j++)
                {
                    if (distanceMatrix[i][j] < searchRadius && distanceMatrix[i][j] < minimumDistance)
                    {
                        //check atom properties
                        if (CheckAtomMapping(firstAtomContainer, secondAtomContainer, i, j))
                        {
                            minimumDistance = distanceMatrix[i][j];
                            mappedAtoms.Add(
                                firstAtomContainer.Atoms.IndexOf(firstAtomContainer.Atoms[i]),
                                secondAtomContainer.Atoms.IndexOf(secondAtomContainer.Atoms[j]));
                        }
                    }
                }
            }
        }

        private static bool CheckAtomMapping(IAtomContainer firstAC, IAtomContainer secondAC, int posFirstAtom, int posSecondAtom)
        {
            var firstAtom = firstAC.Atoms[posFirstAtom];
            var secondAtom = secondAC.Atoms[posSecondAtom];
            // XXX: floating point comparision!
            return string.Equals(firstAtom.Symbol, secondAtom.Symbol, StringComparison.Ordinal)
                && firstAC.GetConnectedAtoms(firstAtom).Count() == secondAC.GetConnectedAtoms(secondAtom).Count()
                && firstAtom.BondOrderSum.Equals(secondAtom.BondOrderSum)
                && firstAtom.MaxBondOrder == secondAtom.MaxBondOrder;
        }

        private static IAtomContainer SetVisitedFlagsToFalse(IAtomContainer atomContainer)
        {
            for (int i = 0; i < atomContainer.Atoms.Count; i++)
                atomContainer.Atoms[i].IsVisited = false;

            return atomContainer;
        }

        /// <summary>
        /// Return the RMSD of bonds length between the 2 aligned molecules.
        /// </summary>
        /// <param name="firstAtomContainer">the (largest) first aligned AtomContainer which is the reference</param>
        /// <param name="secondAtomContainer">the second aligned AtomContainer</param>
        /// <param name="mappedAtoms">IDictionary: a IDictionary of the mapped atoms</param>
        /// <param name="Coords3d">bool: true if moecules has 3D coords, false if molecules has 2D coords</param>
        /// <returns>double: all the RMSD of bonds length</returns>
        public static double GetBondLengthRMSD(IAtomContainer firstAtomContainer, IAtomContainer secondAtomContainer, IReadOnlyDictionary<int, int> mappedAtoms, bool Coords3d)
        {
            var firstAtoms = mappedAtoms.Keys;
            double sum = 0;
            double n = 0;
            SetVisitedFlagsToFalse(firstAtomContainer);
            SetVisitedFlagsToFalse(secondAtomContainer);
            foreach (var firstAtom in firstAtoms)
            {
                var centerAtomFirstMolecule = firstAtomContainer.Atoms[firstAtom];
                centerAtomFirstMolecule.IsVisited = true;
                var centerAtomSecondMolecule = secondAtomContainer.Atoms[mappedAtoms[firstAtomContainer.Atoms.IndexOf(centerAtomFirstMolecule)]];
                var connectedAtoms = firstAtomContainer.GetConnectedAtoms(centerAtomFirstMolecule);
                foreach (var conAtom in connectedAtoms)
                {
                    //this step is built to know if the program has already calculate a bond length (so as not to have duplicate values)
                    if (!conAtom.IsVisited)
                    {
                        if (Coords3d)
                        {
                            var distance1 = Vector3.Distance(centerAtomFirstMolecule.Point3D.Value, conAtom.Point3D.Value);
                            var distance2 = Vector3.Distance(centerAtomSecondMolecule.Point3D.Value, secondAtomContainer.Atoms[mappedAtoms[firstAtomContainer.Atoms.IndexOf(conAtom)]].Point3D.Value);
                            sum = sum + Math.Pow((distance1 - distance2), 2);
                            n++;
                        }
                        else
                        {
                            var distance1 = Vector2.Distance(centerAtomFirstMolecule.Point2D.Value, conAtom.Point2D.Value);
                            var distance2 = Vector2.Distance(centerAtomSecondMolecule.Point2D.Value, secondAtomContainer.Atoms[mappedAtoms[firstAtomContainer.Atoms.IndexOf(conAtom)]].Point2D.Value);
                            sum = sum + Math.Pow(distance1 - distance2, 2);
                            n++;
                        }
                    }
                }
            }
            SetVisitedFlagsToFalse(firstAtomContainer);
            SetVisitedFlagsToFalse(secondAtomContainer);
            return Math.Sqrt(sum / n);
        }

        /// <summary>
        /// Return the variation of each angle value between the 2 aligned molecules.
        /// </summary>
        /// <param name="firstAtomContainer">the (largest) first aligned AtomContainer which is the reference</param>
        /// <param name="secondAtomContainer">the second aligned AtomContainer</param>
        /// <param name="mappedAtoms">IDictionary: a IDictionary of the mapped atoms</param>
        /// <returns>double: the value of the RMSD</returns>
        public static double GetAngleRMSD(IAtomContainer firstAtomContainer, IAtomContainer secondAtomContainer, IReadOnlyDictionary<int, int> mappedAtoms)
        {
            //Debug.WriteLine("**** GT GetAngleRMSD ****");
            IEnumerable<int> firstAtoms = mappedAtoms.Keys;
            //Debug.WriteLine("mappedAtoms:"+mappedAtoms.ToString());
            IAtom firstAtomfirstAC;
            IAtom centerAtomfirstAC;
            IAtom firstAtomsecondAC;
            IAtom secondAtomsecondAC;
            IAtom centerAtomsecondAC;
            double angleFirstMolecule;
            double angleSecondMolecule;
            double sum = 0;
            double n = 0;
            foreach (var firstAtomNumber in firstAtoms)
            {
                centerAtomfirstAC = firstAtomContainer.Atoms[firstAtomNumber];
                var connectedAtoms = firstAtomContainer.GetConnectedAtoms(centerAtomfirstAC).ToReadOnlyList();
                if (connectedAtoms.Count > 1)
                {
                    for (int i = 0; i < connectedAtoms.Count - 1; i++)
                    {
                        firstAtomfirstAC = connectedAtoms[i];
                        for (int j = i + 1; j < connectedAtoms.Count; j++)
                        {
                            angleFirstMolecule = GetAngle(centerAtomfirstAC, firstAtomfirstAC, connectedAtoms[j]);
                            centerAtomsecondAC = secondAtomContainer.Atoms[mappedAtoms[firstAtomContainer
                                    .Atoms.IndexOf(centerAtomfirstAC)]];
                            firstAtomsecondAC = secondAtomContainer.Atoms[mappedAtoms[firstAtomContainer
                                    .Atoms.IndexOf(firstAtomfirstAC)]];
                            secondAtomsecondAC = secondAtomContainer.Atoms[mappedAtoms[firstAtomContainer
                                    .Atoms.IndexOf(connectedAtoms[j])]];
                            angleSecondMolecule = GetAngle(centerAtomsecondAC, firstAtomsecondAC, secondAtomsecondAC);
                            sum = sum + Math.Pow(angleFirstMolecule - angleSecondMolecule, 2);
                            n++;
                        }
                    }
                }//if
            }
            return Math.Sqrt(sum / n);
        }

        private static double GetAngle(IAtom atom1, IAtom atom2, IAtom atom3)
        {
            Vector3 centerAtom = new Vector3
            {
                X = atom1.Point3D.Value.X,
                Y = atom1.Point3D.Value.Y,
                Z = atom1.Point3D.Value.Z
            };
            Vector3 firstAtom = new Vector3();
            Vector3 secondAtom = new Vector3();

            firstAtom.X = atom2.Point3D.Value.X;
            firstAtom.Y = atom2.Point3D.Value.Y;
            firstAtom.Z = atom2.Point3D.Value.Z;

            secondAtom.X = atom3.Point3D.Value.X;
            secondAtom.Y = atom3.Point3D.Value.Y;
            secondAtom.Z = atom3.Point3D.Value.Z;

            firstAtom = firstAtom - centerAtom;
            secondAtom = secondAtom - centerAtom;

            return Vectors.Angle(firstAtom, secondAtom);
        }

        /// <summary>
        /// Return the RMSD between the 2 aligned molecules.
        /// </summary>
        /// <param name="firstAtomContainer">The (largest) first aligned <see cref="IAtomContainer"/> which is the reference</param>
        /// <param name="secondAtomContainer">The second aligned <see cref="IAtomContainer"/></param>
        /// <param name="mappedAtoms">A dictionary of the mapped atoms</param>
        /// <param name="Coords3d"><see langword="true"/> if molecules has 3D coordinations, <see langword="false"/> if molecules has 2D coordinations</param>
        /// <returns>The value of the RMSD</returns>
        /// <exception cref="CDKException">if there is an error in getting mapped atoms</exception>
        public static double GetAllAtomRMSD(IAtomContainer firstAtomContainer, IAtomContainer secondAtomContainer,
                IReadOnlyDictionary<int, int> mappedAtoms, bool Coords3d)
        {
            double sum = 0;
            double RMSD;
            var firstAtoms = mappedAtoms.Keys;
            int secondAtomNumber;
            int n = 0;
            foreach (var firstAtomNumber in firstAtoms)
            {
                try
                {
                    secondAtomNumber = mappedAtoms[firstAtomNumber];
                    var firstAtom = firstAtomContainer.Atoms[firstAtomNumber];
                    if (Coords3d)
                    {
                        sum = sum + Math.Pow(Vector3.Distance(firstAtom.Point3D.Value, secondAtomContainer.Atoms[secondAtomNumber].Point3D.Value), 2);
                        n++;
                    }
                    else
                    {
                        sum = sum + Math.Pow(Vector2.Distance(firstAtom.Point2D.Value, secondAtomContainer.Atoms[secondAtomNumber].Point2D.Value), 2);
                        n++;
                    }
                }
                catch (Exception ex)
                {
                    throw new CDKException(ex.Message, ex);
                }
            }
            RMSD = Math.Sqrt(sum / n);
            return RMSD;
        }

        /// <summary>
        /// Return the RMSD of the heavy atoms between the 2 aligned molecules.
        /// </summary>
        /// <param name="firstAtomContainer">The (largest) first aligned AtomContainer which is the reference</param>
        /// <param name="secondAtomContainer">The second aligned AtomContainer</param>
        /// <param name="mappedAtoms">A dictionary of the mapped atoms</param>
        /// <param name="hetAtomOnly"><see langword="true"/> if only hetero atoms should be considered</param>
        /// <param name="Coords3d"><see langword="true"/> if molecules has 3D coordinates, <see langword="false"/> if molecules has 2D coordinates</param>
        /// <returns>double: the value of the RMSD</returns>
        public static double GetHeavyAtomRMSD(IAtomContainer firstAtomContainer, IAtomContainer secondAtomContainer, IReadOnlyDictionary<int, int> mappedAtoms, bool hetAtomOnly, bool Coords3d)
        {
            double sum = 0;
            double RMSD;
            var firstAtoms = mappedAtoms.Keys;
            int secondAtomNumber;
            int n = 0;
            foreach (var firstAtomNumber in firstAtoms)
            {
                secondAtomNumber = mappedAtoms[firstAtomNumber];
                var firstAtom = firstAtomContainer.Atoms[firstAtomNumber];
                if (hetAtomOnly)
                {
                    switch (firstAtom.AtomicNumber)
                    {
                        case AtomicNumbers.H:
                        case AtomicNumbers.C:
                            break;
                        default:
                            if (Coords3d)
                            {
                                sum = sum + Math.Pow(Vector3.Distance(firstAtom.Point3D.Value, secondAtomContainer.Atoms[secondAtomNumber].Point3D.Value), 2);
                                n++;
                            }
                            else
                            {
                                sum = sum + Math.Pow(Vector2.Distance(firstAtom.Point2D.Value, secondAtomContainer.Atoms[secondAtomNumber].Point2D.Value), 2);
                                n++;
                            }
                            break;
                    }
                }
                else
                {
                    switch (firstAtom.AtomicNumber)
                    {
                        case AtomicNumbers.H:
                            break;
                        default:
                            if (Coords3d)
                            {
                                sum = sum + Math.Pow(Vector3.Distance(firstAtom.Point3D.Value, secondAtomContainer.Atoms[secondAtomNumber].Point3D.Value), 2);
                                n++;
                            }
                            else
                            {
                                sum = sum + Math.Pow(Vector2.Distance(firstAtom.Point2D.Value, secondAtomContainer.Atoms[secondAtomNumber].Point2D.Value), 2);
                                n++;
                            }
                            break;
                    }
                }
            }
            RMSD = Math.Sqrt(sum / n);
            return RMSD;
        }

        /// <summary>
        /// An average of all 3D bond length values is produced, using point3ds in atoms. Atom's with no
        /// coordinates are disregarded.
        /// </summary>
        /// <param name="container">The AtomContainer for which the average bond length is to be calculated</param>
        /// <returns>the average bond length</returns>
        public static double GetBondLengthAverage3D(IAtomContainer container)
        {
            double bondLengthSum = 0;
            int bondCounter = 0;
            foreach (var bond in container.Bonds)
            {
                var atom1 = bond.Begin;
                var atom2 = bond.End;
                if (atom1.Point3D != null && atom2.Point3D != null)
                {
                    bondCounter++;
                    bondLengthSum += Vector3.Distance(atom1.Point3D.Value, atom2.Point3D.Value);
                }
            }
            return bondLengthSum / bondCounter;
        }

        /// <summary>
        /// Shift the container horizontally to the right to make its bounds not overlap with the other
        /// bounds. To avoid dependence on Java AWT, rectangles are described by arrays of double. Each
        /// rectangle is specified by {minX, minY, maxX, maxY}.
        /// </summary>
        /// <param name="container">the <see cref="IAtomContainer"/> to shift to the right</param>
        /// <param name="bounds">the bounds of the <see cref="IAtomContainer"/> to shift</param>
        /// <param name="last">the bounds that is used as reference</param>
        /// <param name="gap">the gap between the two rectangles</param>
        /// <returns>the rectangle of the <see cref="IAtomContainer"/> after the shift</returns>
        public static double[] ShiftContainer(IAtomContainer container, double[] bounds, double[] last, double gap)
        {
            Trace.Assert(bounds.Length == 4);
            Trace.Assert(last.Length == 4);

            var boundsMinX = bounds[0];
            var boundsMinY = bounds[1];
            var boundsMaxX = bounds[2];
            var boundsMaxY = bounds[3];

            var lastMaxX = last[2];

            // determine if the containers are overlapping
            if (lastMaxX + gap >= boundsMinX)
            {
                var xShift = lastMaxX + gap - boundsMinX;
                var shift = new Vector2(xShift, 0);
                GeometryUtil.Translate2D(container, shift);
                return new double[] { boundsMinX + xShift, boundsMinY, boundsMaxX + xShift, boundsMaxY };
            }
            else
            {
                // the containers are not overlapping
                return bounds;
            }
        }

        /// <summary>
        /// Returns the average 2D bond length values of all products and reactants
        /// of the given reaction. The method uses  <see cref="GetBondLengthAverage(IAtomContainer)"/> internally.
        /// </summary>
        /// <param name="reaction">The IReaction for which the average 2D bond length is calculated</param>
        /// <returns>the average 2D bond length</returns>
        /// <seealso cref="GetBondLengthAverage(IAtomContainer)"/>
        public static double GetBondLengthAverage(IReaction reaction)
        {
            double bondlenghtsum = 0.0;
            int containercount = 0;
            var containers = ReactionManipulator.GetAllAtomContainers(reaction);
            foreach (var container in containers)
            {
                containercount++;
                bondlenghtsum += GetBondLengthAverage(container);
            }
            return bondlenghtsum / containercount;
        }

        /// <summary>
        /// Calculate the median bond length of an atom container.
        /// </summary>
        /// <param name="container">structure representation</param>
        /// <returns>median bond length</returns>
        /// <exception cref="ArgumentException">unset coordinates or no bonds</exception>
        public static double GetBondLengthMedian(IAtomContainer container)
        {
            if (container.Bonds.Count == 0)
                throw new ArgumentException("Container has no bonds.");
            int nBonds = 0;
            var lengths = new double[container.Bonds.Count];
            for (int i = 0; i < container.Bonds.Count; i++)
            {
                var bond = container.Bonds[i];
                var atom1 = bond.Begin;
                var atom2 = bond.End;
                if (atom1.Point2D == null || atom2.Point2D == null)
                    throw new ArgumentException("An atom has no 2D coordinates.");
                var p1 = atom1.Point2D.Value;
                var p2 = atom2.Point2D.Value;
                if (p1.X != p2.X || p1.Y != p2.Y)
                    lengths[nBonds++] = Vector2.Distance(p1, p2);
            }
            Array.Sort(lengths, 0, nBonds);
            return lengths[nBonds / 2];
        }

        /// <summary>
        /// Determines if this model contains 3D coordinates for all atoms.
        /// </summary>
        /// <param name="chemModel">the ChemModel to consider</param>
        /// <returns>bool indication that 3D coordinates are available for all atoms.</returns>
        public static bool Has3DCoordinates(IChemModel chemModel)
        {
            var acs = ChemModelManipulator.GetAllAtomContainers(chemModel);
            return !acs.Any(ac => !Has3DCoordinates(ac));
        }

        /// <summary>
        /// Shift the containers in a reaction vertically upwards to not overlap with the reference
        /// rectangle. The shift is such that the given gap is realized, but only if the reactions are
        /// actually overlapping. To avoid dependence on Java AWT, rectangles are described by
        /// arrays of double. Each rectangle is specified by {minX, minY, maxX, maxY}.
        /// </summary>
        /// <param name="reaction">the reaction to shift</param>
        /// <param name="bounds">the bounds of the reaction to shift</param>
        /// <param name="last">the bounds of the last reaction</param>
        /// <param name="gap"></param>
        /// <returns>the rectangle of the shifted reaction</returns>
        public static double[] ShiftReactionVertical(IReaction reaction, double[] bounds, double[] last, double gap)
        {
            Trace.Assert(bounds.Length == 4);
            Trace.Assert(last.Length == 4);

            var boundsMinX = bounds[0];
            var boundsMinY = bounds[1];
            var boundsMaxX = bounds[2];
            var boundsMaxY = bounds[3];

            var lastMinY = last[1];
            var lastMaxY = last[3];

            var boundsHeight = boundsMaxY - boundsMinY;
            var lastHeight = lastMaxY - lastMinY;

            // determine if the reactions are overlapping
            if (lastMaxY + gap >= boundsMinY)
            {
                double yShift = boundsHeight + lastHeight + gap;
                Vector2 shift = new Vector2(0, yShift);
                var containers = ReactionManipulator.GetAllAtomContainers(reaction);
                foreach (var container in containers)
                    Translate2D(container, shift);

                return new double[] { boundsMinX, boundsMinY + yShift, boundsMaxX, boundsMaxY + yShift };
            }
            else
            {
                // the reactions were not overlapping
                return bounds;
            }
        }
    }
}
