/*
 * Copyright (C) 2010  Mark Rijnbeek <mark_rynbeek@users.sf.net>
 *               2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may
 * distribute with programs based on this work.
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
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NCDK.Isomorphisms.Matchers
{
    /// <summary>
    /// Represents a list of Rgroup substitutes to be associated with some
    /// <see cref="RGroupQuery"/>.
    /// </summary>
    // @cdk.module  isomorphism
    // @cdk.keyword Rgroup
    // @cdk.keyword R group
    // @cdk.keyword R-group
    // @author Mark Rijnbeek
    public class RGroupList
    {
        /// <summary>
        /// Default value for occurrence field.
        /// </summary>
        internal const string DefaultOccurence = ">0";

        /// <summary>
        /// Unique number to identify the Rgroup.
        /// </summary>
        private int rGroupNumber;

        /// <summary>
        /// Indicates that sites labeled with this Rgroup may only be
        /// substituted with a member of the Rgroup or with hydrogen.
        /// </summary>
        public bool IsRestH { get; set; }

        private string occurrence;

        /// <summary>
        /// List of substitute structures.
        /// </summary>
        public IList<RGroup> RGroups { get; set; }

        /// <summary>
        /// The rGroup (say B) that is required when this one (say A) exists.
        /// This captures the "LOG" information 'IF A (this) THEN B'.
        /// </summary>
        public int RequiredRGroupNumber { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="rGroupNumber"></param>
        public RGroupList(int rGroupNumber)
        {
            this.RGroupNumber = rGroupNumber;
            this.IsRestH = false;
            this.Occurrence = DefaultOccurence;
            this.RequiredRGroupNumber = 0;
        }

        /// <summary>
        /// Constructor with attributes given.
        /// </summary>
        /// <param name="rGroupNumber">R-Group number</param>
        /// <param name="restH">restH</param>
        /// <param name="occurrence">occurrence</param>
        /// <param name="requiredRGroupNumber">number of other R-Group required</param>
        public RGroupList(int rGroupNumber, bool restH, string occurrence, int requiredRGroupNumber)
        {
            this.RGroupNumber = rGroupNumber;
            this.IsRestH = restH;
            this.Occurrence = occurrence;
            this.RequiredRGroupNumber = requiredRGroupNumber;
        }

        /// <summary>
        /// R-Group number, checks for valid range.
        /// Spec: "value from 1 to 32 *, labels position of Rgroup on root."
        /// </summary>
        public int RGroupNumber
        {
            set
            {
                if (value < 1 || value > 32)
                {
                    throw new InvalidOperationException("Rgroup number must be between 1 and 32.");
                }
                this.rGroupNumber = value;
            }

            get
            {
                return rGroupNumber;
            }
        }

        /// <summary>
        /// The occurrence value. Validates user input to be conform the(Symyx) specification.
        /// <see cref="IsValidOccurrenceSyntax(string)"/>
        /// </summary>
        public string Occurrence
        {
            get
            {
                return occurrence;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    value = ">0"; //revert to default
                }
                else
                {
                    value = value.Trim().Replace(" ", "");
                    if (IsValidOccurrenceSyntax(value))
                    {
                        this.occurrence = value;
                    }
                    else
                        throw new CDKException("Invalid occurrence line: " + value);
                }
            }
        }

        /// <summary>
        /// Validates the occurrence value.
        /// <list type="bullet">
        /// <item>n : exactly n ;</item>
        /// <item>n - m : n through m ;</item>
        /// <item>&#62; n : greater than n ;</item>
        /// <item>&#60; n : fewer than n ;</item>
        /// <item>default (blank) is &gt; 0 ;</item>
        /// </list> 
        /// Any non-contradictory combination of the preceding values is also
        /// allowed; for example "1, 3-7, 9, &gt;11".
        /// </summary>
        /// <param name="occ">string to validate.</param>
        /// <returns><see langword="true"/> if valid string provided.</returns>
        public static bool IsValidOccurrenceSyntax(string occ)
        {
            var tokens = occ.Split(',');
            foreach (var token in tokens)
            {
                string cond = token.Trim().Replace(" ", "");
                do
                {
                    //Number: "n"
                    if (Match("^\\d+$", cond))
                    {
                        if (int.Parse(cond, NumberFormatInfo.InvariantInfo) < 0) // not allowed
                            return false;
                        break;
                    }
                    //Range: "n-m"
                    if (Match("^\\d+-\\d+$", cond))
                    {
                        var index_of_cond_m = cond.IndexOf('-');
                        int from = int.Parse(cond.Substring(0, index_of_cond_m), NumberFormatInfo.InvariantInfo);
                        int to = int.Parse(cond.Substring(index_of_cond_m + 1, cond.Length - (index_of_cond_m + 1)), NumberFormatInfo.InvariantInfo);
                        if (from < 0 || to < 0 || to < from) // not allowed
                            return false;
                        break;
                    }
                    //Smaller than: "<n"
                    if (Match("^<\\d+$", cond))
                    {
                        var index_of_cond_m = cond.IndexOf('<');
                        int n = int.Parse(cond.Substring(index_of_cond_m + 1, cond.Length - (index_of_cond_m + 1)), NumberFormatInfo.InvariantInfo);
                        if (n == 0) // not allowed
                            return false;
                        break;
                    }
                    //Greater than: ">n"
                    if (Match("^>\\d+$", cond))
                    {
                        break;
                    }

                    return false;
                } while (1 == 0);
            }

            return true;
        }

        /// <summary>
        /// Helper method for regular expression matching.
        /// </summary>
        /// <param name="regExp">regular expression string</param>
        /// <param name="userInput">user's input</param>
        /// <returns>the regular expression matched the user input</returns>
        private static bool Match(string regExp, string userInput)
        {
            return Regex.IsMatch(userInput, regExp);
        }

        /// <summary>
        /// Matches the 'occurrence' condition with a provided maximum number of
        /// RGroup attachments. Returns the valid occurrences (numeric) for these
        /// two combined. If none found, returns empty list.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Example: if R1 occurs 3 times attached to some root structure, then
        /// stating "&gt;5" as an occurrence for that RGoupList does not make
        /// sense: the example R1 can occur 0..3 times. Empty would be returned.
        /// </para>
        /// <para>
        /// If the occurence would be &gt;2, then 3 would be returned. Etcetera.
        /// </para>
        /// </remarks>
        /// <param name="maxAttachments">number of attachments</param>
        /// <returns>valid values by combining a max for R# with the occurrence cond.</returns>
        public IEnumerable<int> MatchOccurence(int maxAttachments)
        {
            for (int val = 0; val <= maxAttachments; val++)
            {
                bool addVal = false;

                var tokens = occurrence.Split(',');
                foreach (var token in tokens)
                {
                    if (addVal)
                        break;

                    string cond = token.Trim().Replace(" ", "");
                    if (Match("^\\d+$", cond))
                    { // n
                        if (int.Parse(cond, NumberFormatInfo.InvariantInfo) == val) addVal = true;
                    }
                    if (Match("^\\d+-\\d+$", cond))
                    { // n-m
                        var cond_index_of_m = cond.IndexOf('-');
                        int from = int.Parse(cond.Substring(0, cond_index_of_m), NumberFormatInfo.InvariantInfo);
                        int to = int.Parse(cond.Substring(cond_index_of_m + 1, cond.Length - (cond_index_of_m + 1)), NumberFormatInfo.InvariantInfo);
                        if (val >= from && val <= to)
                        {
                            addVal = true;
                        }
                    }
                    if (Match("^>\\d+$", cond))
                    { // <n
                        var cond_index_of_gt = cond.IndexOf('>');
                        int n = int.Parse(cond.Substring(cond_index_of_gt + 1, cond.Length - (cond_index_of_gt + 1)), NumberFormatInfo.InvariantInfo);
                        if (val > n)
                        {
                            addVal = true;
                        }
                    }
                    if (Match("^<\\d+$", cond))
                    { // >n
                        var cond_index_of_lt = cond.IndexOf('<');
                        int n = int.Parse(cond.Substring(cond_index_of_lt + 1, cond.Length - (cond_index_of_lt + 1)), NumberFormatInfo.InvariantInfo);
                        if (val < n)
                        {
                            addVal = true;
                        }
                    }
                    if (addVal)
                    {
                        yield return val;
                    }

                }
            }
            yield break;
        }

        public override bool Equals(object obj)
        {
            if (obj is RGroupList && this.rGroupNumber == ((RGroupList)obj).rGroupNumber)
                return true;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return this.rGroupNumber;
        }
    }
}
