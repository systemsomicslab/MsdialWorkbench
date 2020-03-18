/* Copyright (C) 1998-2007  Dan Gezelter
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

using System;

namespace NCDK.Maths
{
    /// <summary>
    /// Converts a string representation of a Fortran double to a double.
    /// </summary>
    /// <remarks>
    /// A modified version of the "atof" method provided in the Core Java
    /// books by Cay S. Horstmann &amp; Gary Cornell.  The main difference
    /// here is that we scan for Fortran double precision characters
    /// ('D' and 'd') which often cause the C versions of atof to
    /// barf.
    /// </remarks>
    // @author Dan Gezelter
    // @cdk.module standard
    internal static class FortranFormat
    {
        /// <summary>
        /// Converts a string of digits to an double.
        /// </summary>
        /// <param name="s">a string denoting a double</param>
        public static double Atof(string s)
        {
            int i = 0;
            int sign = 1;
            double r = 0; // integer part
            double p = 1; // exponent of fractional part
            int state = 0; // 0 = int part, 1 = frac part

            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
            if (i < s.Length && s[i] == '-')
            {
                sign = -1;
                i++;
            }
            else if (i < s.Length && s[i] == '+')
            {
                i++;
            }
            while (i < s.Length)
            {
                char ch = s[i];
                if ('0' <= ch && ch <= '9')
                {
                    if (state == 0)
                        r = r * 10 + ch - '0';
                    else if (state == 1)
                    {
                        p = p / 10;
                        r = r + p * (ch - '0');
                    }
                }
                else if (ch == '.')
                {
                    if (state == 0)
                        state = 1;
                    else
                        return sign * r;
                }
                else if (ch == 'e' || ch == 'E' || ch == 'd' || ch == 'D')
                {
                    long e = (int)ParseLong(s.Substring(i + 1), 10);
                    return sign * r * Math.Pow(10, e);
                }
                else
                    return sign * r;
                i++;
            }
            return sign * r;
        }

        private static long ParseLong(string s, int base_)
        {
            int i = 0;
            int sign = 1;
            long r = 0;

            while (i < s.Length && char.IsWhiteSpace(s[i]))
                i++;
            if (i < s.Length && s[i] == '-')
            {
                sign = -1;
                i++;
            }
            else if (i < s.Length && s[i] == '+')
            {
                i++;
            }
            while (i < s.Length)
            {
                char ch = s[i];
                if ('0' <= ch && ch < '0' + base_)
                    r = r * base_ + ch - '0';
                else if ('A' <= ch && ch < 'A' + base_ - 10)
                    r = r * base_ + ch - 'A' + 10;
                else if ('a' <= ch && ch < 'a' + base_ - 10)
                    r = r * base_ + ch - 'a' + 10;
                else
                    return r * sign;
                i++;
            }
            return r * sign;
        }
    }
}
