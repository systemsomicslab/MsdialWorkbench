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

using System.Globalization;
using System.Text.RegularExpressions;

namespace NCDK.Indexes
{
    /// <summary>
    /// Tools to work with CAS registry numbers.
    /// </summary>
    /// <remarks>
    /// References:
    /// <list type="bullet">
    ///     <item><see href="http://www.cas.org/EO/regsys.html">A CAS Registry Number</see></item>
    ///     <item><see href="http://www.cas.org/EO/checkdig.html">Check Digit Verification of CAS Registry Numbers</see></item>
    /// </list> 
    /// <see href="http://www.cas.org">CAS website</see>
    /// </remarks>
    // @author Egon Willighagen <egonw@sci.kun.nl>
    // @author Nathanaël "M.Le_maudit" Mazuir
    // @cdk.created 2003-06-30
    // @cdk.keyword CAS number
    public static class CASNumber
    {
        const string format = "^(\\d+)-(\\d\\d)-(\\d)$";
        static readonly Regex pattern = new Regex(format, RegexOptions.Compiled);

        /// <summary>
        /// Checks whether the registry number is valid.
        /// </summary>
        /// <param name="casNumber">the CAS number to validate</param>
        /// <returns>true if a valid CAS number, false otherwise</returns>
        // @cdk.keyword CAS number
        // @cdk.keyword validation
        public static bool IsValid(string casNumber)
        {
            bool overall = true;
            
            // check format
            var matcher = pattern.Match(casNumber);
            overall = overall && matcher.Success;

            if (matcher.Success)
            {
                // check number
                string part1 = matcher.Groups[1].Value;
                string part2 = matcher.Groups[2].Value;
                string part3 = matcher.Groups[3].Value;
                int part1value = int.Parse(part1, NumberFormatInfo.InvariantInfo);
                if (part1value < 50)
                {
                    overall = false;
                    // CAS numbers start at 50-00-0
                }
                else
                {
                    int digit = CASNumber.CalculateCheckDigit(part1, part2);
                    overall = overall && (digit == int.Parse(part3, NumberFormatInfo.InvariantInfo));
                }
            }

            return overall;
        }

        private static int CalculateCheckDigit(string part1, string part2)
        {
            int total = 0;
            total = total + 1 * int.Parse(part2.Substring(1, 1), NumberFormatInfo.InvariantInfo);
            total = total + 2 * int.Parse(part2.Substring(0, 1), NumberFormatInfo.InvariantInfo);
            int length = part1.Length;
            for (int i = 0; i < length; i++)
            {
                total = total + (3 + i) * int.Parse(part1.Substring(length - 1 - i, 1), NumberFormatInfo.InvariantInfo);
            }
            return total % 10;
        }
    }
}
