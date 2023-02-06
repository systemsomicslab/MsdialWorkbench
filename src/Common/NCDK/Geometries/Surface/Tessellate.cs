/*
 * Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

namespace NCDK.Geometries.Surface
{
    /// <summary>
    /// Performs a tessellation of the unit sphere.
    /// <para>
    /// This class generates the coordinates of the triangles that will
    /// tessellate the unit sphere. The algorithm is recursive subdivision
    /// of an initial representation which can be tetrahedral, octahedral or
    /// icosahedral. The default is icosahedral. The number of points generated
    /// depends on the level of subdivision. The default is 4 levels and with the
    /// initial icosahedral representation this gives 1536 points.
    /// </para>
    /// <para>
    /// The constants for the tetrahedral and icosahedral representations were
    /// taken from http://eeg.sourceforge.net/eegdoc/eeg_toolbox/sphere_tri.html
    /// </para>
    /// </summary>
    // @author Rajarshi Guha
    // @cdk.created 2005-05-08
    // @cdk.module  qsarmolecular
    public class Tessellate
    {
        private Triangle[] Oldtess;
        private readonly int maxlevel;

        public Tessellate()
        {
            this.Oldtess = RepIco();
            this.maxlevel = 4;
        }

        public Tessellate(string type, int level)
        {
            if (string.Equals(type, "tet", StringComparison.Ordinal))
                this.Oldtess = RepTet();
            else if (string.Equals(type, "oct", StringComparison.Ordinal))
                this.Oldtess = RepOct();
            else if (string.Equals(type, "ico", StringComparison.Ordinal))
                this.Oldtess = RepIco();
            this.maxlevel = level;
        }

        private static Vector3 Midpoint(Vector3 p1, Vector3 p2)
        {
            double x, y, z;
            x = 0.5 * (p1.X + p2.X);
            y = 0.5 * (p1.Y + p2.Y);
            z = 0.5 * (p1.Z + p2.Z);
            return (new Vector3(x, y, z));
        }

        private static void Normalize(ref Vector3 p)
        {
            double mag = p.X * p.X + p.Y * p.Y + p.Z * p.Z;
            if (mag != 0.0)
            {
                mag = 1.0 / Math.Sqrt(mag);
                p.X = p.X * mag;
                p.Y = p.Y * mag;
                p.Z = p.Z * mag;
            }
        }

        public void DoTessellate()
        {
            for (int j = 1; j < maxlevel; j++)
            {
                int oldN = this.Oldtess.Length;
                int newN = oldN * 4;
                Triangle[] newtess = new Triangle[newN];

                for (int i = 0; i < oldN; i++)
                {
                    Triangle old = Oldtess[i];

                    Vector3 p1 = Midpoint(old.p1, old.p3);
                    Vector3 p2 = Midpoint(old.p1, old.p2);
                    Vector3 p3 = Midpoint(old.p2, old.p3);

                    Normalize(ref p1);
                    Normalize(ref p2);
                    Normalize(ref p3);

                    newtess[i * 4] = new Triangle(old.p1, p2, p1);
                    newtess[i * 4 + 1] = new Triangle(p2, old.p2, p3);
                    newtess[i * 4 + 2] = new Triangle(p1, p2, p3);
                    newtess[i * 4 + 3] = new Triangle(p1, p3, old.p3);
                }

                Oldtess = new Triangle[newN];
                for (int i = 0; i < newN; i++)
                    Oldtess[i] = newtess[i];
            }
        }

        public int GetNumberOfTriangles()
        {
            return Oldtess.Length;
        }

        public Triangle[] GetTessAsTriangles()
        {
            return Oldtess;
        }

        public Vector3[] GetTessAsPoint3ds()
        {
            Vector3[] ret = new Vector3[GetNumberOfTriangles() * 3];
            for (int i = 0; i < GetNumberOfTriangles(); i++)
            {
                ret[i * 3] = Oldtess[i].p1;
                ret[i * 3 + 1] = Oldtess[i].p2;
                ret[i * 3 + 2] = Oldtess[i].p3;
            }
            return ret;
        }

        private static Triangle[] RepTet()
        {
            double sqrt3 = 0.5773502692;
            Vector3[] v =
            {
                new Vector3(sqrt3, sqrt3, sqrt3),
                new Vector3(-sqrt3, -sqrt3, sqrt3),
                new Vector3(-sqrt3, sqrt3, -sqrt3),
                new Vector3(sqrt3, -sqrt3, -sqrt3),
            };
            Triangle[] rep = 
            {
                new Triangle(v[0], v[1], v[2]),
                new Triangle(v[0], v[3], v[1]),
                new Triangle(v[2], v[1], v[3]),
                new Triangle(v[3], v[0], v[2]),
            };
            return rep;
        }

        private static Triangle[] RepOct()
        {
            Vector3[] v = {new Vector3(1.0, 0.0, 0.0), new Vector3(-1.0, 0.0, 0.0), new Vector3(0.0, 1.0, 0.0),
                new Vector3(0.0, -1.0, 0.0), new Vector3(0.0, 0.0, 1.0), new Vector3(0.0, 0.0, -1.0)};
            Triangle[] rep = {new Triangle(v[0], v[4], v[2]), new Triangle(v[2], v[4], v[1]),
                new Triangle(v[1], v[4], v[3]), new Triangle(v[3], v[4], v[0]), new Triangle(v[0], v[2], v[5]),
                new Triangle(v[2], v[1], v[5]), new Triangle(v[1], v[3], v[5]), new Triangle(v[3], v[0], v[5])};
            return (rep);
        }

        private static Triangle[] RepIco()
        {
            double tau = 0.8506508084;
            double one = 0.5257311121;
            Vector3[] v = {new Vector3(tau, one, 0.0), new Vector3(-tau, one, 0.0), new Vector3(-tau, -one, 0.0),
                new Vector3(tau, -one, 0.0), new Vector3(one, 0.0, tau), new Vector3(one, 0.0, -tau),
                new Vector3(-one, 0.0, -tau), new Vector3(-one, 0.0, tau), new Vector3(0.0, tau, one),
                new Vector3(0.0, -tau, one), new Vector3(0.0, -tau, -one), new Vector3(0.0, tau, -one)};
            Triangle[] rep = {new Triangle(v[4], v[8], v[7]), new Triangle(v[4], v[7], v[9]),
                new Triangle(v[5], v[6], v[11]), new Triangle(v[5], v[10], v[6]), new Triangle(v[0], v[4], v[3]),
                new Triangle(v[0], v[3], v[5]), new Triangle(v[2], v[7], v[1]), new Triangle(v[2], v[1], v[6]),
                new Triangle(v[8], v[0], v[11]), new Triangle(v[8], v[11], v[1]), new Triangle(v[9], v[10], v[3]),
                new Triangle(v[9], v[2], v[10]), new Triangle(v[8], v[4], v[0]), new Triangle(v[11], v[0], v[5]),
                new Triangle(v[4], v[9], v[3]), new Triangle(v[5], v[3], v[10]), new Triangle(v[7], v[8], v[1]),
                new Triangle(v[6], v[1], v[11]), new Triangle(v[7], v[2], v[9]), new Triangle(v[6], v[10], v[2])};
            return rep;
        }
    }
}
