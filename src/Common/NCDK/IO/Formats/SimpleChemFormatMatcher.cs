/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
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

using NCDK.Tools;
using System.Collections.Generic;

namespace NCDK.IO.Formats
{
    /// <summary>
    /// A simple line matcher which delegates format matching to the previous
    /// per-line implementation.
    /// </summary>
    // @author John May
    // @cdk.module ioformats
    public abstract class SimpleChemFormatMatcher : AbstractResourceFormat, IChemFormatMatcher
    {
        public abstract string ReaderClassName { get; }
        public abstract string WriterClassName { get; }
        public abstract DataFeatures SupportedDataFeatures { get; }
        public abstract DataFeatures RequiredDataFeatures { get; }

        /// <summary>
        /// Check whether a given line at a specified position (line number) could
        /// belong to this format.
        /// </summary>
        /// <param name="lineNumber">the line number of <paramref name="line"/></param>
        /// <param name="line">the contents at the given <paramref name="lineNumber"/></param>
        /// <returns>this line in this position could indicate a format match</returns>
        public abstract bool Matches(int lineNumber, string line);

        /// <summary>
        /// Simple implementation, runs the lines one-by-one through <see cref="Matches(int, string)"/> and returns true if any line matches.
        /// </summary>
        /// <param name="lines">lines of the input to be checked</param>
        /// <returns>runs the lines</returns>
        public MatchResult Matches(IEnumerable<string> lines)
        {
            int i = 0;
            foreach (var line in lines)
            { 
                if (Matches(i + 1, line))
                    return new MatchResult(true, this, i);
                i++;
            }
            return MatchResult.NoMatch;
        }
    }
}
