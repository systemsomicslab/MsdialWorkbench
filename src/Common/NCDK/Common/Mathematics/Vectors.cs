/*
 * Copyright 1997-2006 Sun Microsystems, Inc.  All Rights Reserved.
 * DO NOT ALTER OR REMOVE COPYRIGHT NOTICES OR THIS FILE HEADER.
 *
 * This code is free software; you can redistribute it and/or modify it
 * under the terms of the GNU General Public License version 2 only, as
 * published by the Free Software Foundation.  Sun designates this
 * particular file as subject to the "Classpath" exception as provided
 * by Sun in the LICENSE file that accompanied this code.
 *
 * This code is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
 * version 2 for more details (a copy is included in the LICENSE file that
 * accompanied this code).
 *
 * You should have received a copy of the GNU General Public License version
 * 2 along with this work; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 *
 * Please contact Sun Microsystems, Inc., 4150 Network Circle, Santa Clara,
 * CA 95054 USA or visit www.sun.com if you need additional information or
 * have any questions.
 */

using System;
using NCDK.Numerics;
using System.Runtime.CompilerServices;

namespace NCDK.Common.Mathematics
{
    public static class Vectors
    {
        public static Vector2 Vector2MaxValue { get; } = new Vector2(double.MaxValue, double.MaxValue);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double DegreeToRadian(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double RadianToDegree(double angle)
        {
            return angle * (180 / Math.PI);
        }

        public static double Angle(Vector2 p1, Vector2 p2)
        {
            p1 = Vector2.Normalize(p1);
            p2 = Vector2.Normalize(p2);

            var ratio = Vector2.Dot(p1, p2);
            double theta;

            if (ratio < 0)
            {
                theta = Math.PI - 2 * Math.Asin((p1 + p2).Length() / 2);
            }
            else
            {
                theta = 2 * Math.Asin((p1 - p2).Length() / 2);
            }
            return theta;
        }

        public static double Angle(Vector3 p1, Vector3 p2)
        {
            p1 = Vector3.Normalize(p1);
            p2 = Vector3.Normalize(p2);

            var ratio = Vector3.Dot(p1, p2);
            double theta;

            if (ratio < 0)
            {
                theta = Math.PI - 2 * Math.Asin((p1 + p2).Length() / 2);
            }
            else
            {
                theta = 2 * Math.Asin((p1 - p2).Length() / 2);
            }
            return theta;
        }

        public static Quaternion NewQuaternionFromAxisAngle(Vector3 v, double angle)
        {
            var s = Math.Sin(angle / 2) / v.Length();
            return new Quaternion(v * s, Math.Cos(angle / 2));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion NewQuaternionFromAxisAngle(double x, double y, double z, double angle)
        {
            return NewQuaternionFromAxisAngle(new Vector3(x, y, z), angle);
        }

        public static bool LinesIntersect(
            double x1, double y1,
            double x2, double y2,
            double x3, double y3,
            double x4, double y4)
        {
            return ((RelativeCCW(x1, y1, x2, y2, x3, y3) *
                     RelativeCCW(x1, y1, x2, y2, x4, y4) <= 0)
                    && (RelativeCCW(x3, y3, x4, y4, x1, y1) *
                        RelativeCCW(x3, y3, x4, y4, x2, y2) <= 0));
        }

        public static int RelativeCCW(double x1, double y1,
                                     double x2, double y2,
                                     double px, double py)
        {
            x2 -= x1;
            y2 -= y1;
            px -= x1;
            py -= y1;
            double ccw = px * y2 - py * x2;
            if (ccw == 0.0)
            {
                // The point is colinear, classify based on which side of
                // the segment the point falls on.  We can calculate a
                // relative value using the projection of px,py onto the
                // segment - a negative value indicates the point projects
                // outside of the segment in the direction of the particular
                // endpoint used as the origin for the projection.
                ccw = px * x2 + py * y2;
                if (ccw > 0.0)
                {
                    // Reverse the projection to be relative to the original x2,y2
                    // x2 and y2 are simply negated.
                    // px and py need to have (x2 - x1) or (y2 - y1) subtracted
                    //    from them (based on the original values)
                    // Since we really want to get a positive answer when the
                    //    point is "beyond (x2,y2)", then we want to calculate
                    //    the inverse anyway - thus we leave x2 & y2 negated.
                    px -= x2;
                    py -= y2;
                    ccw = px * x2 + py * y2;
                    if (ccw < 0.0)
                    {
                        ccw = 0;
                    }
                }
            }
            return (ccw < 0.0) ? -1 : ((ccw > 0.0) ? 1 : 0);
        }
    }
}
