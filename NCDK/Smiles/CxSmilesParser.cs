/*
 * Copyright (c) 2016 John May <jwmay@users.sf.net>
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
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using System;
using System.Collections.Generic;
using static NCDK.Smiles.CxSmilesState;
using static NCDK.Smiles.CxSmilesState.Radical;

namespace NCDK.Smiles
{
    /// <summary>
    /// Parse CXSMILES (ChemAxon Extended SMILES) layers. 
    /// </summary>
    /// <remarks>
    /// The layers are suffixed after the SMILES but before the title
    /// and encode a large number of the features. CXSMILES was not intended for outside consumption so has some quirks
    /// but does provide some useful features. This parser handles a subset of the grammar:
    /// <list type="bullet">
    /// <item>Atom Labels</item>
    /// <item>Atom Values</item>
    /// <item>Atom Coordinates</item>
    /// <item>Positional Variations</item>
    /// <item>Polymer Sgroups</item>
    /// <item>Atom Radicals</item>
    /// <item>Fragment grouping</item>
    /// </list> 
    /// The following properties are ignored
    /// <list type="bullet">
    /// <item>cis/trans specification</item>
    /// <item>relative stereochemistry</item>
    /// </list> 
    /// </remarks>
    internal sealed class CxSmilesParser
    {
        private const char CommaSeparatorChar = ',';
        private const char DotSeparatorChar = '.';

        private CxSmilesParser()
        {
        }

        /// <summary>
        /// Process atom labels from extended SMILES in a char iter.
        /// </summary>
        /// <param name="iter">char iteration</param>
        /// <param name="dest">destination of labels (atomidx->label)</param>
        /// <returns>parse success/failure</returns>
        private static bool ProcessAtomLabels(CharIter iter, SortedDictionary<int, string> dest)
        {
            int atomIdx = 0;
            while (iter.HasNext())
            {
                // fast forward through empty labels
                while (iter.NextIf(';'))
                    atomIdx++;

                char c = iter.Next();
                if (c == '$')
                {
                    iter.NextIf(','); // optional
                    // end of atom label
                    return true;
                }
                else
                {
                    iter.pos--; // push back
                    var beg = iter.pos;
                    var rollback = beg;
                    while (iter.HasNext())
                    {
                        if (iter.pos == beg && iter.Curr() == '_' && iter.Peek() == 'R')
                        {
                            ++beg;
                        }
                        // correct step over of escaped label
                        if (iter.Curr() == '&')
                        {
                            rollback = iter.pos;
                            if (iter.NextIf('&') && iter.NextIf('#') && iter.NextIfDigit())
                            {
                                while (iter.NextIfDigit()) { } // more digits
                                if (!iter.NextIf(';'))
                                {
                                    iter.pos = rollback;
                                }
                                else
                                {
                                }
                            }
                            else
                            {
                                iter.pos = rollback;
                            }
                        }
                        else if (iter.Curr() == ';')
                            break;
                        else if (iter.Curr() == '$')
                            break;
                        else
                            iter.Next();
                    }
                    dest.Add(atomIdx, Unescape(iter.Substr(beg, iter.pos)));
                    atomIdx++;
                    if (iter.NextIf('$'))
                    {
                        iter.NextIf(','); // optional
                        return true;
                    }
                    if (!iter.NextIf(';'))
                        return false;
                }
            }
            return false;
        }

        private static double ReadDouble(CharIter iter)
        {
            int sign = +1;
            if (iter.NextIf('-'))
                sign = -1;
            else if (iter.NextIf('+'))
                sign = +1;
            double intPart;
            double fracPart = 0;
            int divisor = 1;

            intPart = (double)ProcessUnsignedInt(iter);
            if (intPart < 0) intPart = 0;
            iter.NextIf('.');

            char c;
            while (iter.HasNext() && IsDigit(c = iter.Curr()))
            {
                fracPart *= 10;
                fracPart += c - '0';
                divisor *= 10;
                iter.Next();
            }

            return sign * (intPart + (fracPart / divisor));
        }

        /// <summary>
        /// Coordinates are written between parenthesis. The z-coord may be omitted '(0,1,),(2,3,)'.
        /// </summary>
        /// <param name="iter">input characters, iterator is progressed by this method</param>
        /// <param name="state">output CXSMILES state</param>
        /// <returns>parse was a success (or not)</returns>
        private static bool ProcessCoords(CharIter iter, CxSmilesState state)
        {
            if (state.atomCoords == null)
                state.atomCoords = new List<double[]>();
            while (iter.HasNext())
            {
                // end of coordinate list
                if (iter.Curr() == ')')
                {
                    iter.Next();
                    iter.NextIf(','); // optional
                    return true;
                }

                double x = ReadDouble(iter);
                if (!iter.NextIf(','))
                    return false;
                double y = ReadDouble(iter);
                if (!iter.NextIf(','))
                    return false;
                double z = ReadDouble(iter);
                iter.NextIf(';');

                state.coordFlag = state.coordFlag || z != 0;
                state.atomCoords.Add(new double[] { x, y, z });
            }
            return false;
        }

        /// <summary>
        /// Fragment grouping defines disconnected components that should be considered part of a single molecule (i.e.
        /// Salts). Examples include NaH, AlCl3, Cs2CO3, HATU, etc.
        /// </summary>
        /// <param name="iter">input characters, iterator is progressed by this method</param>
        /// <param name="state">output CXSMILES state</param>
        /// <returns>parse was a success (or not)</returns>
        private static bool ProcessFragmentGrouping(CharIter iter, CxSmilesState state)
        {
            if (state.fragGroups == null)
                state.fragGroups = new List<List<int>>();
            var dest = new List<int>();
            while (iter.HasNext())
            {
                dest.Clear();
                if (!ProcessIntList(iter, DotSeparatorChar, dest))
                    return false;
                iter.NextIf(CommaSeparatorChar);
                if (dest.Count == 0)
                    return true;
                state.fragGroups.Add(new List<int>(dest));
            }
            return false;
        }

        /// <summary>
        /// Sgroup polymers in CXSMILES can be variable length so may be terminated either with the next group
        /// or the end of the CXSMILES.
        /// </summary>
        /// <param name="c">character</param>
        /// <returns>character an delimit an Sgroup</returns>
        private static bool IsSgroupDelim(char c)
        {
            return c == ':' || c == ',' || c == '|';
        }

        private static bool ProcessDataSgroups(CharIter iter, CxSmilesState state)
        {
            if (state.dataSgroups == null)
                state.dataSgroups = new List<DataSgroup>(4);

            var atomset = new List<int>();
            if (!ProcessIntList(iter, CommaSeparatorChar, atomset))
                return false;

            if (!iter.NextIf(':'))
                return false;
            var beg = iter.pos;
            while (iter.HasNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            string field = Unescape(iter.Substr(beg, iter.pos));

            if (!iter.NextIf(':'))
                return false;
            beg = iter.pos;
            while (iter.HasNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            string value = Unescape(iter.Substr(beg, iter.pos));

            if (!iter.NextIf(':'))
            {
                state.dataSgroups.Add(new CxSmilesState.DataSgroup(atomset, field, value, "", "", ""));
                return true;
            }

            beg = iter.pos;
            while (iter.HasNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            var operator_ = Unescape(iter.Substr(beg, iter.pos));

            if (!iter.NextIf(':'))
            {
                state.dataSgroups.Add(new CxSmilesState.DataSgroup(atomset, field, value, operator_, "", ""));
                return true;
            }

            beg = iter.pos;
            while (iter.HasNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            var unit = Unescape(iter.Substr(beg, iter.pos));

            if (!iter.NextIf(':'))
            {
                state.dataSgroups.Add(new CxSmilesState.DataSgroup(atomset, field, value, operator_, unit, ""));
                return true;
            }

            beg = iter.pos;
            while (iter.HasNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            string tag = Unescape(iter.Substr(beg, iter.pos));

            state.dataSgroups.Add(new CxSmilesState.DataSgroup(atomset, field, value, operator_, unit, tag));

            return true;
        }

        /// <summary>
        /// Polymer Sgroups describe variations of repeating units. Only the atoms and not crossing bonds are written.
        /// </summary>
        /// <param name="iter">input characters, iterator is progressed by this method</param>
        /// <param name="state">output CXSMILES state</param>
        /// <returns>parse was a success (or not)</returns>
        private static bool ProcessPolymerSgroups(CharIter iter, CxSmilesState state)
        {
            if (state.sgroups == null)
                state.sgroups = new List<PolymerSgroup>();
            var beg = iter.pos;
            while (iter.HasNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            var keyword = iter.Substr(beg, iter.pos);
            if (!iter.NextIf(':'))
                return false;
            var atomset = new List<int>();
            if (!ProcessIntList(iter, CommaSeparatorChar, atomset))
                return false;


            string subscript;
            string supscript;

            if (!iter.NextIf(':'))
                return false;

            // "If the subscript equals the keyword of the Sgroup this field can be empty", ergo
            // if omitted it equals the keyword
            beg = iter.pos;
            while (iter.HasNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            subscript = Unescape(iter.Substr(beg, iter.pos));
            if (string.IsNullOrEmpty(subscript))
                subscript = keyword;

            // "In the superscript only connectivity and flip information is allowed.", default
            // appears to be "eu" either/unspecified
            if (!iter.NextIf(':'))
                return false;
            beg = iter.pos;
            while (iter.HasNext() && !IsSgroupDelim(iter.Curr()))
                iter.Next();
            supscript = Unescape(iter.Substr(beg, iter.pos));
            if (string.IsNullOrEmpty(supscript))
                supscript = "eu";

            if (iter.NextIf(',') || iter.Curr() == '|')
            {
                state.sgroups.Add(new CxSmilesState.PolymerSgroup(keyword, atomset, subscript, supscript));
                return true;
            }
            // not supported: crossing bond info (difficult to work out from doc) and bracket orientation

            return false;
        }

        /// <summary>
        /// Positional variation/multi centre bonding. Describe as a begin atom and one or more end points.
        /// </summary>
        /// <param name="iter">input characters, iterator is progressed by this method</param>
        /// <param name="state">output CXSMILES state</param>
        /// <returns>parse was a success (or not)</returns>
        private static bool ProcessPositionalVariation(CharIter iter, CxSmilesState state)
        {
            if (state.positionVar == null)
                state.positionVar = new SortedDictionary<int, IList<int>>();
            while (iter.HasNext())
            {
                if (IsDigit(iter.Curr()))
                {
                    int beg = ProcessUnsignedInt(iter);
                    if (!iter.NextIf(':'))
                        return false;
                    var endpoints = new List<int>(6);
                    if (!ProcessIntList(iter, DotSeparatorChar, endpoints))
                        return false;
                    iter.NextIf(',');
                    state.positionVar.Add(beg, endpoints);
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// CXSMILES radicals.
        /// </summary>
        /// <param name="iter">input characters, iterator is progressed by this method</param>
        /// <param name="state">output CXSMILES state</param>
        /// <returns>parse was a success (or not)</returns>
        private static bool ProcessRadicals(CharIter iter, CxSmilesState state)
        {
            if (state.atomRads == null)
                state.atomRads = new SortedDictionary<int, Radical>();
            CxSmilesState.Radical rad;
            switch (iter.Next())
            {
                case '1':
                    rad = Monovalent;
                    break;
                case '2':
                    rad = Divalent;
                    break;
                case '3':
                    rad = DivalentSinglet;
                    break;
                case '4':
                    rad = DivalentTriplet;
                    break;
                case '5':
                    rad = Trivalent;
                    break;
                case '6':
                    rad = TrivalentDoublet;
                    break;
                case '7':
                    rad = TrivalentQuartet;
                    break;
                default:
                    return false;
            }
            if (!iter.NextIf(':'))
                return false;
            var dest = new List<int>(4);
            if (!ProcessIntList(iter, CommaSeparatorChar, dest))
                return false;
            foreach (var atomidx in dest)
                state.atomRads.Add(atomidx, rad);
            return true;
        }

        /// <summary>
        /// Parse an string possibly containing CXSMILES into an intermediate state
        /// (<see cref="CxSmilesState"/>) representation.
        /// </summary>
        /// <param name="str">input character string (SMILES title field)</param>
        /// <param name="state">output CXSMILES state</param>
        /// <returns>position where CXSMILES ends (below 0 means no CXSMILES)</returns>
        public static int ProcessCx(string str, CxSmilesState state)
        {
            CharIter iter = new CharIter(str);

            if (!iter.NextIf('|'))
                return -1;

            while (iter.HasNext())
            {
                switch (iter.Next())
                {
                    case '$': // atom labels and values
                              // dest is atom labels by default
                        SortedDictionary<int, string> dest;
                        // check for atom values
                        if (iter.NextIf("_AV:"))
                            dest = state.atomValues = new SortedDictionary<int, string>();
                        else
                            dest = state.atomLabels = new SortedDictionary<int, string>();
                        if (!ProcessAtomLabels(iter, dest))
                            return -1;
                        break;
                    case '(': // coordinates
                        if (!ProcessCoords(iter, state))
                            return -1;
                        break;
                    case 'c': // cis/trans/unspec ignored
                    case 't':
                        // c/t:
                        if (iter.NextIf(':'))
                        {
                            if (!SkipIntList(iter, CommaSeparatorChar))
                                return -1;
                        }
                        // ctu:
                        else if (iter.NextIf("tu:"))
                        {
                            if (!SkipIntList(iter, CommaSeparatorChar))
                                return -1;
                        }
                        break;
                    case 'r': // relative stereochemistry ignored
                        if (iter.NextIf(':'))
                        {
                            if (!SkipIntList(iter, CommaSeparatorChar))
                                return -1;
                        }
                        else
                        {
                            if (!iter.NextIf(',') && iter.Curr() != '|')
                                return -1;
                        }
                        break;
                    case 'l': // lone pairs ignored
                        if (!iter.NextIf("p:"))
                            return -1;
                        if (!SkipIntMap(iter))
                            return -1;
                        break;
                    case 'f': // fragment grouping
                        if (!iter.NextIf(':'))
                            return -1;
                        if (!ProcessFragmentGrouping(iter, state))
                            return -1;
                        break;
                    case 'S': // Sgroup polymers
                        if (iter.NextIf("g:"))
                        {
                            if (!ProcessPolymerSgroups(iter, state))
                                return -1;
                        }
                        else if (iter.NextIf("gD:"))
                        {
                            if (!ProcessDataSgroups(iter, state))
                                return -1;
                        }
                        else
                        {
                            return -1;
                        }
                        break;
                    case 'm': // positional variation
                        if (!iter.NextIf(':'))
                            return -1;
                        if (!ProcessPositionalVariation(iter, state))
                            return -1;
                        break;
                    case '^': // Radicals
                        if (!ProcessRadicals(iter, state))
                            return -1;
                        break;
                    case 'C':
                    case 'H': // coordination and hydrogen bonding ignored
                        if (!iter.NextIf(':'))
                            return -1;
                        while (iter.HasNext() && IsDigit(iter.Curr()))
                        {
                            if (!SkipIntList(iter, DotSeparatorChar))
                                return -1;
                            iter.NextIf(',');
                        }
                        break;
                    case '|': // end of CX
                              // consume optional separators
                        if (!iter.NextIf(' ')) iter.NextIf('\t');
                        return iter.pos;
                    default:
                        return -1;
                }
            }

            return -1;
        }

        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        private static bool SkipIntList(CharIter iter, char sep)
        {
            while (iter.HasNext())
            {
                char c = iter.Curr();
                if (IsDigit(c) || c == sep)
                    iter.Next();
                else
                    return true;
            }
            // ran off end
            return false;
        }

        private static bool SkipIntMap(CharIter iter)
        {
            while (iter.HasNext())
            {
                char c = iter.Curr();
                if (Char.IsDigit(c) || c == ',' || c == ':')
                    iter.Next();
                else
                    return true;
            }
            // ran of end
            return false;
        }

        private static int ProcessUnsignedInt(CharIter iter)
        {
            if (!iter.HasNext())
                return -1;
            char c = iter.Curr();
            if (!IsDigit(c))
                return -1;
            int res = c - '0';
            iter.Next();
            while (iter.HasNext() && IsDigit(c = iter.Curr()))
            {
                res = res * 10 + c - '0';
                iter.Next();
            }
            return res;
        }

        /// <summary>
        /// Process a list of unsigned integers.
        /// </summary>
        /// <param name="iter">char iter</param>
        /// <param name="sep">the separator</param>
        /// <param name="dest">output</param>
        /// <returns>int-list was successfully processed</returns>
        private static bool ProcessIntList(CharIter iter, char sep, List<int> dest)
        {
            while (iter.HasNext())
            {
                char c = iter.Curr();
                if (IsDigit(c))
                {
                    int r = ProcessUnsignedInt(iter);
                    if (r < 0) return false;
                    iter.NextIf(sep);
                    dest.Add(r);
                }
                else
                {
                    return true;
                }
            }
            // ran of end
            return false;
        }

        public static string Unescape(string str)
        {
            int dst = 0;
            int src = 0;
            char[] chars = str.ToCharArray();
            int len = chars.Length;
            while (src < chars.Length)
            {
                // match the pattern &#[0-9][0-9]*;
                if (src + 3 < len && chars[src] == '&' && chars[src + 1] == '#' && IsDigit(chars[src + 2]))
                {
                    int tmp = src + 2;
                    int code = 0;
                    while (tmp < len && IsDigit(chars[tmp]))
                    {
                        code *= 10;
                        code += chars[tmp] - '0';
                        tmp++;
                    }
                    if (tmp < len && chars[tmp] == ';')
                    {
                        src = tmp + 1;
                        chars[dst++] = (char)code;
                        continue;
                    }
                }
                chars[dst++] = chars[src++];
            }
            return new string(chars, 0, dst);
        }

        /// <summary>
        /// Utility for parsing a sequence of characters. The char iter allows us to pull
        /// of one or more characters at a time and track where we are in the string.
        /// </summary>
        sealed class CharIter
        {
            private readonly string str;
            private readonly int len;
            public int pos = 0;

            internal CharIter(string str)
            {
                this.str = str;
                this.len = str.Length;
            }

            /// <summary>
            /// If the next character matches the provided query the iterator is progressed.
            /// </summary>
            /// <param name="c">query character</param>
            /// <returns>iterator was moved forwards</returns>
            internal bool NextIf(char c)
            {
                if (!HasNext() || str[pos] != c)
                    return false;
                pos++;
                return true;
            }

            internal bool NextIfDigit()
            {
                if (!HasNext() || !IsDigit(str[pos]))
                    return false;
                pos++;
                return true;
            }

            /// <summary>
            /// If the next sequence of characters matches the prefix the iterator
            /// is progressed to character following the prefix.
            /// </summary>
            /// <param name="prefix">prefix string</param>
            /// <returns>iterator was moved forwards</returns>
            internal bool NextIf(string prefix)
            {
                bool res;
                if (res = this.str.Substring(pos).StartsWith(prefix, StringComparison.Ordinal))
                    pos += prefix.Length;
                return res;
            }

            /// <summary>
            /// Is there more chracters to read?
            /// </summary>
            /// <returns>whether more characters are available</returns>
            internal bool HasNext()
            {
                return pos < len;
            }

            /// <summary>
            /// Access the current character of the iterator.
            /// </summary>
            /// <returns>charactor</returns>
            internal char Curr()
            {
                return str[pos];
            }

            /// <summary>
            /// Access the current character of the iterator and move
            /// to the next position.
            /// </summary>
            /// <returns>charactor</returns>
            internal char Next()
            {
                return str[pos++];
            }

            public char Peek()
            {
                return pos < str.Length ? str[pos + 1] : '\0';
            }

            /// <summary>
            /// Access a substring from the iterator.
            /// </summary>
            /// <param name="beg">begin position (inclusive)</param>
            /// <param name="end">end position (exclusive)</param>
            /// <returns>substring</returns>
            internal string Substr(int beg, int end)
            {
                return str.Substring(beg, end - beg);
            }
        }
    }
}
