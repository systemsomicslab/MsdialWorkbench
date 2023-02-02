/*
 * Copyright (c) 2017 John Mayfield <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NCDK.IO
{
    /// <summary>
    /// Tool to help process INChI 1.12beta content.
    /// </summary>
    // @cdk.module extra
    public static class InChIContentProcessorTool
    {
        private static readonly Regex pattern1 = new Regex("([A-Z][a-z]?)(\\d+)?(.*)", RegexOptions.Compiled);
        private static readonly Regex pattern2 = new Regex("^(\\d+)-?(.*)", RegexOptions.Compiled);

        /// <summary>
        /// Processes the content from the formula field of the INChI.
        /// Typical values look like C6H6, from INChI=1.12Beta/C6H6/c1-2-4-6-5-3-1/h1-6H.
        /// </summary>
        public static IAtomContainer ProcessFormula(IAtomContainer parsedContent, string atomsEncoding)
        {
            Debug.WriteLine($"Parsing atom data: {atomsEncoding}");

            string remainder = atomsEncoding;
            while (remainder.Length > 0)
            {
                Debug.WriteLine($"Remaining: {remainder}");
                var match = pattern1.Match(remainder);
                if (match.Success)
                {
                    string symbol = match.Groups[1].Value;
                    Debug.WriteLine($"Atom symbol: {symbol}");
                    switch (symbol)
                    {
                        case "H":
                            // don't add explicit hydrogens
                            break;
                        default:
                            string occurenceStr = match.Groups[2].Value;
                            int occurence = 1;
                            if (!string.IsNullOrEmpty(occurenceStr))
                            {
                                occurence = int.Parse(occurenceStr, NumberFormatInfo.InvariantInfo);
                            }
                            Debug.WriteLine($"  occurence: {occurence}");
                            for (int i = 1; i <= occurence; i++)
                            {
                                parsedContent.Atoms.Add(parsedContent.Builder.NewAtom(symbol));
                            }
                            break;
                    }
                    remainder = match.Groups[3].Value;
                    if (remainder == null)
                        remainder = "";
                    Debug.WriteLine($"  Remaining: {remainder}");
                }
                else
                {
                    Trace.TraceError("No match found!");
                    remainder = "";
                }
                Debug.WriteLine($"NO atoms: {parsedContent.Atoms.Count}");
            }
            return parsedContent;
        }

        /// <summary>
        /// Processes the content from the connections field of the INChI.
        /// Typical values look like 1-2-4-6-5-3-1, from INChI=1.12Beta/C6H6/c1-2-4-6-5-3-1/h1-6H.
        /// </summary>
        /// <param name="bondsEncoding">the content of the INChI connections field</param>
        /// <param name="container">the atomContainer parsed from the formula field</param>
        /// <param name="source">the atom to build the path upon. If -1, then start new path</param>
        /// <seealso cref="ProcessFormula(IAtomContainer, string)"/>
        public static void ProcessConnections(string bondsEncoding, IAtomContainer container, int source)
        {
            Debug.WriteLine($"Parsing bond data: {bondsEncoding}");

            IBond bondToAdd = null;
            /* Fixme: treatment of branching is too limited! */
            string remainder = bondsEncoding;
            while (remainder.Length > 0)
            {
                Debug.WriteLine($"Bond part: {remainder}");
                if (remainder[0] == '(')
                {
                    string branch = ChopBranch(remainder);
                    ProcessConnections(branch, container, source);
                    if (branch.Length + 2 <= remainder.Length)
                    {
                        remainder = remainder.Substring(branch.Length + 2);
                    }
                    else
                    {
                        remainder = "";
                    }
                }
                else
                {
                    var matcher = pattern2.Match(remainder);
                    if (matcher.Success)
                    {
                        string targetStr = matcher.Groups[1].Value;
                        int target = int.Parse(targetStr, NumberFormatInfo.InvariantInfo);
                        Debug.WriteLine($"Source atom: {source}");
                        Debug.WriteLine($"Target atom: {targetStr}");
                        IAtom targetAtom = container.Atoms[target - 1];
                        if (source != -1)
                        {
                            IAtom sourceAtom = container.Atoms[source - 1];
                            bondToAdd = container.Builder.NewBond(sourceAtom, targetAtom, BondOrder.Single);
                            container.Bonds.Add(bondToAdd);
                        }
                        remainder = matcher.Groups[2].Value;
                        source = target;
                        Debug.WriteLine($"  remainder: {remainder}");
                    }
                    else
                    {
                        Trace.TraceError("Could not get next bond info part");
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the first full branch. It extracts everything between the first
        /// '(' and the corresponding ')' char.
        /// </summary>
        private static string ChopBranch(string remainder)
        {
            bool doChop = false;
            int branchLevel = 0;
            var choppedString = new StringBuilder();
            for (int i = 0; i < remainder.Length; i++)
            {
                char currentChar = remainder[i];
                if (currentChar == '(')
                {
                    if (doChop) choppedString.Append(currentChar);
                    doChop = true;
                    branchLevel++;
                }
                else if (currentChar == ')')
                {
                    branchLevel--;
                    if (branchLevel == 0)
                    {
                        doChop = false;
                        break;
                    }
                    if (doChop) choppedString.Append(currentChar);
                }
                else if (doChop)
                {
                    choppedString.Append(currentChar);
                }
            }
            return choppedString.ToString();
        }
    }
}
