/* Copyright (C) 2004-2018  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Tools;
using System.Collections.Generic;

namespace NCDK.IO.Formats
{
    /// <summary>
    /// See <see href="http://www.mdl.com/downloads/public/ctfile/ctfile.jsp">here</see>.
    /// </summary>
    // @cdk.module ioformats
    public class MDLRXNV2000Format : AbstractResourceFormat, IChemFormatMatcher
    {
        private static IResourceFormat myself = null;

        public MDLRXNV2000Format() { }

        public static IResourceFormat Instance
        {
            get
            {
                if (myself == null) myself = new MDLRXNV2000Format();
                return myself;
            }
        }

        /// <inheritdoc/>
        public override string FormatName => "MDL RXN V2000";

        /// <inheritdoc/>
        public override string MIMEType => "chemical/x-mdl-rxnfile";

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override IReadOnlyList<string> NameExtensions => new string[] { "rxn" };

        /// <inheritdoc/>
        public string ReaderClassName { get; } = typeof(MDLRXNV2000Reader).FullName;

        /// <inheritdoc/>
        public string WriterClassName => null;

        /// <inheritdoc/>
        public MatchResult Matches(IEnumerable<string> lines)
        {
            var _lines = lines.ToReadOnlyList();

            // if the first line doesn't have '$RXN' then it can't match
            if (_lines.Count < 1 || _lines[0].Trim() != "$RXN")
                return MatchResult.NoMatch;

            // check the header (fifth line)
            string header = _lines.Count > 4 ? _lines[4] : "";

            // atom count
            if (header.Length < 3 || !char.IsDigit(header[2]))
                return MatchResult.NoMatch;
            // bond count
            if (header.Length < 6 || !char.IsDigit(header[5]))
                return MatchResult.NoMatch;

            // check the rest of the header is only spaces and digits
            if (header.Length > 6)
            {
                string remainder = header.Substring(6).Trim();
                for (int i = 0; i < remainder.Length; ++i)
                {
                    char c = remainder[i];
                    if (!(char.IsDigit(c) || char.IsWhiteSpace(c)))
                    {
                        return MatchResult.NoMatch;
                    }
                }
            }

            return new MatchResult(true, this, 0);
        }

        /// <inheritdoc/>
        public override bool IsXmlBased => false;

        /// <inheritdoc/>
        public DataFeatures SupportedDataFeatures => DataFeatures.None;

        /// <inheritdoc/>
        public DataFeatures RequiredDataFeatures => DataFeatures.None;
    }
}
