/* Copyright (C) 2001-2007  Bradley A. Smith <bradley@baysmith.com>
 *               2003-2009  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Common.Primitives;
using NCDK.IO.Formats;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace NCDK.IO
{
    /// <summary>
    /// A factory for recognizing chemical file formats. Formats
    /// of GZiped files can be detected too.
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.IO.FormatFactory_Example.cs"]/*' />
    /// </example>
    // @cdk.module  ioformats
    // @author  Egon Willighagen <egonw@sci.kun.nl>
    // @author  Bradley A. Smith <bradley@baysmith.com>
    public class FormatFactory
    {
        private readonly int headerLength;

        private List<IChemFormatMatcher> formats = new List<IChemFormatMatcher>(100);

        /// <summary>
        /// Constructs a ReaderFactory which tries to detect the format in the
        /// first 65536 chars.
        /// </summary>
        public FormatFactory()
            : this(65536)
        { }

        /// <summary>
        /// Constructs a ReaderFactory which tries to detect the format in the
        /// first given number of chars.
        /// </summary>
        /// <param name="headerLength">length of the header in number of chars</param>
        public FormatFactory(int headerLength)
        {
            this.headerLength = headerLength;
            LoadFormats();
        }

        private void LoadFormats()
        {
            foreach (var format in ServiceLoader<IChemFormatMatcher>.Load())
                formats.Add(format);
        }

        /// <summary>
        /// Registers a format for detection.
        /// </summary>
        public void RegisterFormat(IChemFormatMatcher format)
        {
            formats.Add(format);
        }

        /// <summary>
        /// The list of recognizable formats.
        /// </summary>
        public IReadOnlyList<IChemFormatMatcher> Formats => formats;

        /// <summary>
        /// Creates a string of the Class name of the <see cref="IChemObject"/> reader
        /// for this file format. The input is read line-by-line
        /// until a line containing an identifying string is
        /// found.
        /// <para>The ReaderFactory detects more formats than the CDK
        /// has Readers for.</para>
        /// <para>This method is not able to detect the format of gziped files.
        /// Use <see cref="GuessFormat(Stream)"/> instead for such files.</para>
        /// </summary>
        /// <returns>The guessed <see cref="IChemFormat"/> or <see langword="null"/> if the file format is not recognized.</returns>
        /// <exception cref="ArgumentNullException">if the input is null</exception>
        /// <seealso cref="GuessFormat(Stream)"/>
        internal IChemFormat GuessFormat(TextReader input)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            // make a copy of the header
            /* Search file for a line containing an identifying keyword */
            var lines = new List<string>();
            {
                long nRead = 0;
                string line;
                while ((line = input.ReadLine()) != null)
                {
                    nRead += line.Length;
                    if (nRead > this.headerLength)
                        break;
                    lines.Add(line);
                }
            }

            ICollection<MatchResult> results = new SortedSet<MatchResult>();

            foreach (var format in formats)
            {
                results.Add(format.Matches(lines));
            }

            // best result is first element (sorted set)
            if (results.Count > 1)
            {
                MatchResult best = results.First();
                if (best.IsMatched) return best.Format;
            }

            {
                string line = lines.FirstOrDefault();
                if (line == null)
                    return null;

                // is it a XYZ file?
                var tokens = Strings.Tokenize(line.Trim());
                try
                {
                    int tokenCount = tokens.Count;
                    if (tokenCount == 1)
                    {
                        int.Parse(tokens[0], NumberFormatInfo.InvariantInfo);
                        // if not failed, then it is a XYZ file
                        return (IChemFormat)XYZFormat.Instance;
                    }
                    else if (tokenCount == 2)
                    {
                        int.Parse(tokens[0], NumberFormatInfo.InvariantInfo);
                        if (string.Equals("BOHR", tokens[1].ToUpperInvariant(), StringComparison.Ordinal))
                        {
                            return (IChemFormat)XYZFormat.Instance;
                        }
                    }
                }
                catch (FormatException)
                {
                }
            }

            return null;
        }

        public IChemFormat GuessFormat(Stream input)
        {
            if (input == null)
            {
                throw new ArgumentException("input cannot be null");
            }

            if (!input.CanSeek)
            {
                throw new ArgumentException("input must support mark");
            }
            var position = input.Position;

            var reader = new StreamReader(input);
            var format = GuessFormat(reader);

            input.Seek(position, SeekOrigin.Begin);

            return format;
        }
    }
}