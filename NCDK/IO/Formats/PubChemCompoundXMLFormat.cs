/* Copyright (C) 2008  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 *  This library is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
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
    // @cdk.module ioformats
    public class PubChemCompoundXMLFormat : AbstractResourceFormat, IChemFormatMatcher
    {
        private static IResourceFormat myself = null;

        public PubChemCompoundXMLFormat() { }

        public static IResourceFormat Instance
        {
            get
            {
                if (myself == null) myself = new PubChemCompoundXMLFormat();
                return myself;
            }
        }

        /// <inheritdoc/>
        public override string FormatName => "PubChem Compound XML";

        /// <inheritdoc/>
        public override string MIMEType => null;

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override IReadOnlyList<string> NameExtensions { get; } = new string[] { "xml" };

        /// <inheritdoc/>
        public string ReaderClassName { get; } = typeof(PCCompoundXMLReader).FullName;

        /// <inheritdoc/>
        public string WriterClassName => null;

        /// <inheritdoc/>
        public override bool IsXmlBased => true;

        /// <inheritdoc/>
        public DataFeatures SupportedDataFeatures => DataFeatures.None;

        /// <inheritdoc/>
        public DataFeatures RequiredDataFeatures => DataFeatures.None;

        public MatchResult Matches(IEnumerable<string> lines)
        {
            MatchResult result = MatchResult.NoMatch;
            int i = 0;
            foreach (var line in lines)
            {
                if (line.Contains("<PC-Compound") && result == MatchResult.NoMatch)
                    result = new MatchResult(true, this, i);
                if (line.Contains("<PC-Compounds"))
                    return MatchResult.NoMatch;
                i++;
            }
            return result;
        }
    }
}
