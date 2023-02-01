/*
 * Copyright (c) 2015 John May <jwmay@users.sf.net>
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
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.Common.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.Renderers.Generators.Standards
{
    /// <summary>
    /// Utility class for handling/formatting abbreviation (superatom) labels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Depending on orientation a label may need to be reversed. For example
    /// consider '-OAc', if the bond exits from the right it is preferable to
    /// write it 'AcO-'. Other labels don't need reversing at all (e.g. tBu).
    /// We reverse labels by spiting them up into 'tokens', reversing token order, * and then joining them back together.
    /// </para>
    /// <para>
    /// Abbreviation labels that are formulas benefit from sub and subscripting
    /// certain parts. For example OPO3H2 looks better with the digits 3 and 2
    /// rendered in subscript.
    /// </para>
    /// </remarks>
    internal sealed class AbbreviationLabel
    {
        /// <summary>
        /// Better rendering of negative charge by using minus and not
        /// an ASCII hyphen.
        /// </summary>
        private const string MINUS_STRING = "\u2212";

        // chemical symbol prefixes
        private static readonly string[] PREFIX_LIST = new string[]
        {
            "n", "norm", "n-", "c", "cy", "cyc", "cyclo", "c-", "cy-", "cyc-", "i", "iso", "i-", "t", "tert", "t-", "s",
            "sec", "s-", "o", "ortho", "o-", "m", "meta", "m-", "p", "para", "p-", "1-", "2-", "3-", "4-", "5-", "6-",
            "7-", "8-", "9-",
        };

        // see https://en.wikipedia.org/wiki/Wikipedia:Naming_conventions_(chemistry)#Prefixes_in_titles
        private readonly static string[] ITAL_PREFIX = new string[]
        {
            "n", "norm", "sec", "s", "tert", "t",
            "ortho", "o", "meta", "m", "para", "p"
        };

        // chemical symbols excluding periodic symbols which are loaded separately
        // Some of these are derived from https://github.com/openbabel/superatoms that
        // has the following license:
        //    This is free and unencumbered software released into the public domain.
        //
        //    Anyone is free to copy, modify, publish, use, compile, sell, or
        //    distribute this software, either in source code form or as a compiled
        //    binary, for any purpose, commercial or non-commercial, and by any
        //    means.
        //
        //    In jurisdictions that recognize copyright laws, the author or authors
        //    of this software dedicate any and all copyright interest in the
        //    software to the public domain. We make this dedication for the benefit
        //    of the public at large and to the detriment of our heirs and
        //    successors. We intend this dedication to be an overt act of
        //    relinquishment in perpetuity of all present and future rights to this
        //    software under copyright law.
        //
        //    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
        //    EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
        //    MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
        //    IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY CLAIM, DAMAGES OR
        //    OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
        //    ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
        //    OTHER DEALINGS IN THE SOFTWARE.
        //
        //    For more information, please refer to <http://unlicense.org/>
        private readonly static string[] SYMBOL_LIST = new string[]
        {
            "acac", "Ace", "Acetyl", "Acyl", "Ad", "All", "Alloc", "Allyl", "Amyl", "AOC",
            "BDMS", "Benzoyl", "Benzyl", "Bn", "BOC", "Boc", "BOM", "bpy", "Bromo", "Bs", "Bu", "But", "Butyl", "Bz", "Bzl",
            "Car", "Cbz", "Chloro", "CoA", "Cy",
            "dppf", "dppp", "dba", "D", "Dan", "Dansyl", "DEIPS", "DEM", "Dip", "Dmb", "DPA", "DTBMS",
            "EE", "EOM", "Et", "Ethyl",
            "Fluoro", "FMOC", "Fmoc", "Formyl",
            "Heptyl", "Hexyl",
            "Iodo", "IPDMS",
            "Me", "MEM", "Mesityl", "Mesyl", "Methoxy", "Methyl", "MOM", "Ms",
            "Nitro",
            "Oct", "Octyl",
            "PAB", "Pentyl", "Ph", "Phenyl", "Pivaloyl", "PMB", "Pro", "Propargyl", "Propyl", "Pv",
            "R", "SEM",
            "T", "TBS", "TBDMS", "Trt", "TBDPS", "TES", "Tf", "THP", "THPO", "TIPS", "TMS", "Tos", "Tol", "Tosyl", "Tr", "Troc",
            "Vinyl", "Voc", "Z",
        };

        private static readonly Trie PREFIX_TRIE = new Trie();
        private static readonly Trie ITAL_PREFIX_TRIE = new Trie();
        private static readonly Trie SYMBOL_TRIE = new Trie();

        // build the tries on class init
        static AbbreviationLabel()
        {
            foreach (var str in PREFIX_LIST)
                Insert(PREFIX_TRIE, str, 0);
            foreach (var str in ITAL_PREFIX)
                Insert(ITAL_PREFIX_TRIE, str, 0);
            foreach (var elem in ChemicalElement.Values)
                if (!string.IsNullOrEmpty(elem.Symbol))
                    Insert(SYMBOL_TRIE, elem.Symbol, 0);
            foreach (var str in SYMBOL_LIST)
                Insert(SYMBOL_TRIE, str, 0);
        }

        public const int STYLE_NORMAL = 0;
        public const int STYLE_SUBSCRIPT = -1;
        public const int STYLE_SUPSCRIPT = +1;
        public const int STYLE_ITALIC = 2;

        /// <summary>
        /// A small class to help describe which parts of a string
        /// are super and subscript (style field).
        /// </summary>
        public sealed class FormattedText
        {
            public string Text { get; set; }
            public int Style { get; set; }

            public FormattedText(string text, int style)
            {
                Text = text;
                Style = style;
            }
        }

        /// <summary>
        /// Split a label it to recognised tokens for reversing, the
        /// validity of the label is not checked! The method is intended
        /// for zero/single attachments only and linkers are not supported.
        /// </summary>
        /// <example>
        /// Example: 
        /// <code>NHCH2Ph -> N,H,C,H2,Ph -> reverse/join -> PhH2CHN</code>
        /// The method return value signals whether formula
        /// formatting (sub- and super- script) can be applied.
        /// </example>
        /// <param name="label">abbreviation label</param>
        /// <param name="tokens">the list of tokens from the input (n>0)</param>
        /// <returns>whether the label parsed okay (i.e. apply formatting)</returns>
        public static bool Parse(string label, IList<string> tokens)
        {
            int i = 0;
            int len = label.Length;

            while (i < len)
            {
                int st = i;
                int last;

                char c = label[i];

                // BRACKETS we treat as separate
                switch (c)
                {
                    case '(':
                    case ')':
                        tokens.Add(c.ToString());
                        i++;
                        // digits following closing brackets
                        if (c == ')')
                        {
                            st = i;
                            while (i < len && IsDigit(c = label[i]))
                            {
                                i++;
                            }
                            if (i > st)
                                tokens.Add(label.Substring(st, i - st));
                        }
                        continue;
                    // separators
                    case '/':
                    case '·':
                    case '.':
                    case '•':
                    case '=':
                        tokens.Add(c.ToString());
                        i++;
                        continue;
                }

                // SYMBOL Tokens
                // optional prefix o- m- p- etc.
                if ((last = FindPrefix(PREFIX_TRIE, label, i, -1)) > 0)
                {
                    i += (last - i);
                }
                int symSt = i;

                // a valid symbol token
                if ((last = FindPrefix(SYMBOL_TRIE, label, i, -1)) > 0)
                {
                    i += (last - i);
                    // an optional number suffix e.g. O2 F3 Ph3 etc.
                    while (i < len && IsDigit(label[i]))
                    {
                        i++;
                    }
                }
                // a charge token, only if it's after some other parts
                else if (i == st && st > 0)
                {
                    c = Norm(label[i]);
                    switch (c)
                    {
                        case '-':
                        case '+':
                            i++;
                            while (i < len && IsDigit(label[i]))
                            {
                                i++;
                            }
                            // we expect charge at the end of the string.. if there is
                            // still more it's not good input
                            if (i < len)
                            {
                                return FailParse(label, tokens);
                            }
                            break;
                    }
                }

                if (i == st || i == symSt)
                {
                    return FailParse(label, tokens);
                }

                tokens.Add(label.Substring(st, i - st));
            }

            return true;
        }

        /// <summary>
        /// Abort call when a label could not be parsed. The tokens are cleared
        /// and replaced with the original label.
        /// </summary>
        /// <param name="label">the original label</param>
        /// <param name="tokens">the current tokens</param>
        /// <returns>always returns false</returns>
        private static bool FailParse(string label, IList<string> tokens)
        {
            tokens.Clear();
            tokens.Add(label);
            return false;
        }

        private static bool IsNumber(string str)
        {
            for (int i = 0; i < str.Length; i++)
                if (!IsDigit(str[i]))
                    return false;
            return true;
        }

        /// <summary>
        /// Reverse a list of tokens for display, flipping
        /// brackets as needed.
        /// </summary>
        /// <param name="tokens">list of tokens</param>
        public static void Reverse(List<string> tokens)
        {
            tokens.Reverse();
            // now flip brackets and move numbers
            var numbers = new Deque<string>();
            for (int i = 0; i < tokens.Count; i++)
            {
                string token = tokens[i];
                switch (token)
                {
                    case "(":
                        tokens[i] = ")";
                        string num = numbers.Pop();
                        if (num.Any())
                        {
                            tokens.Insert(i + 1, num);
                            i++;
                        }
                        break;
                    case ")":
                        tokens[i] = "(";
                        if (i > 0 && IsNumber(tokens[i - 1]))
                        {
                            var last = tokens[i - 1];
                            tokens.RemoveAt(i - 1);
                            numbers.Push(last);
                            i--;
                        }
                        else
                        {
                            numbers.Push("");
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Format and optimise the tokens for rendering (e.g. "OAc" or "AcO"
        /// can be done in one go) and mark tokens that are subscript (-1)
        /// or superscript (+1).
        /// </summary>
        /// <param name="tokens">tokenized label</param>
        public static List<FormattedText> Format(IList<string> tokens)
        {
            var texts = new List<FormattedText>(2 + tokens.Count);
            foreach (var token in tokens)
            {
                // charges
                if (IsChargeToken(token))
                {
                    // charges are superscript
                    var sign = Norm(token[0]).ToString();
                    string coef = token.Length > 1 ? token.Substring(1) : "";
                    if (sign.Equals("-"))
                        sign = MINUS_STRING;
                    texts.Add(new FormattedText(coef + sign, STYLE_SUPSCRIPT));
                }
                // subscript number after brackets
                else if (token.Length == 1 && IsDigit(token[0]) && texts.Any() && texts[texts.Count - 1].Text.Equals(")"))
                {
                    texts.Add(new FormattedText(token, STYLE_SUBSCRIPT));
                }
                else
                {
                    // optional prefix
                    int i = FindPrefix(ITAL_PREFIX_TRIE, token, 0, 0);
                    // find a numeric suffix to subscript
                    int j = token.Length;
                    while (j > 0 && IsDigit(token[j - 1]))
                        j--;
                    // check if we have numeric suffix
                    if (j > 0 && j < token.Length)
                    {
                        if (i > j)
                            i = 0; // prefix overlaps with suffix so don't use it
                        if (i > 0)
                            texts.Add(new FormattedText(token.Substring(0, i), STYLE_ITALIC));
                        texts.Add(new FormattedText(token.Substring(i, j), STYLE_NORMAL));
                        texts.Add(new FormattedText(token.Substring(j), STYLE_SUBSCRIPT));
                    }
                    else
                    {
                        if (i > 0)
                            texts.Add(new FormattedText(token.Substring(0, i), STYLE_ITALIC));
                        texts.Add(new FormattedText(token.Substring(i), STYLE_NORMAL));
                    }
                }
            }

            // merge adjacent text together if it is of the same style
            var res = new List<FormattedText>(texts.Count);
            FormattedText prev = null;
            foreach (var curr in texts)
            {
                if (prev == null || prev.Style != curr.Style)
                {
                    res.Add(prev = curr);
                }
                else
                {
                    prev.Text += curr.Text;
                }
            }

            return res;
        }

        internal static void Reduce(List<FormattedText> texts, int from, int to)
        {
            var tmp = new List<FormattedText>(texts.Count);
            FormattedText prev = null;
            tmp.AddRange(texts.GetRange(0, from));
            foreach (var curr in texts.GetRange(from, to - from))
            {
                if (prev == null || prev.Style != curr.Style)
                {
                    tmp.Add(prev = curr);
                }
                else
                {
                    prev.Text += curr.Text;
                }
            }
            tmp.AddRange(texts.GetRange(to, texts.Count - to));
            texts.Clear();
            texts.AddRange(tmp);
        }

        /// <summary>
        /// Determines if the token is representing a charge.
        /// </summary>
        /// <param name="token">string token</param>
        /// <returns>the token is a charge label (+2, -, +, -2)</returns>
        private static bool IsChargeToken(string token)
        {
            return token.Length > 0 && Norm(token[0]) == '-' || token[0] == '+';
        }

        /// <summary>
        /// Basic method to check if a character is a digit.
        /// </summary>
        /// <param name="c">character</param>
        /// <returns>the character is a digit</returns>
        private static bool IsDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        /// <summary>
        /// Normalise dashes in-case a user has entered one by accident.
        /// </summary>
        /// <param name="c">character</param>
        /// <returns>normalised character</returns>
        private static char Norm(char c)
        {
            switch (c)
            {
                case '\u002d': // hyphen
                case '\u2012': // figure dash
                case '\u2013': // en-dash
                case '\u2014': // em-dash
                case '\u2212': // minus
                    return '-'; // 002d
                default:
                    return c;
            }
        }

        /// <summary>
        /// Find the longest prefix from position (i) in this string that
        /// is present in the trie symbol table.
        /// </summary>
        /// <param name="trie">trie node (start with root)</param>
        /// <param name="str">string to find a prefix of</param>
        /// <param name="i">the position in the string</param>
        /// <param name="best">best score so far (-1 to start)</param>
        /// <returns>the length of the prefix</returns>
        private static int FindPrefix(Trie trie, string str, int i, int best)
        {
            if (trie == null)
                return best;
            if (trie.Token != null)
                best = i;
            if (i == str.Length)
                return best;
            var c = Norm(str[i]);
            if (c > 128)
                return best;
            return FindPrefix(trie.Children[c], str, i + 1, best);
        }

        /// <summary>
        /// Insert a string (<paramref name="str"/>) into the <paramref name="trie"/>.
        /// </summary>
        /// <param name="trie"><paramref name="trie"/> node</param>
        /// <param name="str">the string to insert</param>
        /// <param name="i">index in the string</param>
        /// <returns>a created child node or null</returns>
        private static Trie Insert(Trie trie, string str, int i)
        {
            if (trie == null)
                trie = new Trie();
            if (i == str.Length)
            {
                trie.Token = str;
            }
            else
            {
                char c = str[i];
                trie.Children[c] = Insert(trie.Children[c], str, i + 1);
            }
            return trie;
        }

        /// <summary>
        /// A trie symbol table node.
        /// </summary>
        private sealed class Trie
        {
            public string Token { get; set; }
            public Trie[] Children { get; } = new Trie[128];
        }
    }
}
