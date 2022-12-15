using System;
using System.Windows;

namespace CompMs.Graphics.Helper
{
    internal static class GeometricCalculationHelper
    {
        private static readonly double _eps = 1e-10;

        public static double Dot(this Vector p, Vector q) {
            return p.X * q.X + p.Y * q.Y;
        }

        public static double Det(this Vector p, Vector q) {
            return p.X * q.Y - p.Y * q.X;
        }

        public static Point Interaction(Point p1, Point p2, Point q1, Point q2) {
            return p1 + (p2 - p1) * ((q2 - q1).Det(q1 - p1) / (q2 - q1).Det(p2 - p1));
        }

        public static bool OnSegment(Point p1, Point p2, Point q) {
            return Math.Abs((p1 - q).Det(p2 - q)) <= _eps && (p1 - q).Dot(p2 - q) < _eps;
        }
    }
}
