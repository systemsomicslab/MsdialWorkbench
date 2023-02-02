using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.Graphics.Helper
{
    internal static class ColorHelper
    {
        public static Color FromNormalizedRgb(double r, double g, double b) {
            return Color.FromRgb(ToByte(r), ToByte(g), ToByte(b));
        }

        public static Color FromNormalizedHsv(double h, double s, double v) {
            HSVtoRGB(h, s, v, out double r, out double g, out double b);
            return FromNormalizedRgb(r, g, b);
        }

        private static byte ToByte(double v) => (byte)(Math.Min(1, Math.Max(0, v)) * 255d);
        private static double ToDouble(byte v) => v / 255d;

        public static void RGBtoHSV(double r, double g, double b, out double h, out double s, out double v) {
            double min = Math.Min(Math.Min(r, g), b);
            double max = Math.Max(Math.Max(r, g), b);

            h = max - min;
            if (0.0f < h) {
                if (max == r) {
                    h = (g - b) / h;
                    if (h < 0.0f) {
                        h += 6.0f;
                    }
                }
                else if (max == g) {
                    h = 2.0f + (b - r) / h;
                }
                else {
                    h = 4.0f + (r - g) / h;
                }
            }
            h /= 6.0f;
            s = (max - min);
            if (0.0 < max) s /= max;
            v = max;
        }

        public static void HSVtoRGB(double h, double s, double v, out double r, out double g, out double b) {
            r = v;
            g = v;
            b = v;
            if (0.0f < s) {
                h *= 6.0f;
                int i = (int)h;
                double f = h - (double)i;
                switch (i) {
                    default:
                    case 0:
                        g *= 1 - s * (1 - f);
                        b *= 1 - s;
                        break;
                    case 1:
                        r *= 1 - s * f;
                        b *= 1 - s;
                        break;
                    case 2:
                        r *= 1 - s;
                        b *= 1 - s * (1 - f);
                        break;
                    case 3:
                        r *= 1 - s;
                        g *= 1 - s * f;
                        break;
                    case 4:
                        r *= 1 - s * (1 - f);
                        g *= 1 - s;
                        break;
                    case 5:
                        g *= 1 - s;
                        b *= 1 - s * f;
                        break;
                }
            }
        }
    }
}
