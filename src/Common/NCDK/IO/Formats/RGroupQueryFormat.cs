/*
 * Copyright (C) 2010  Mark Rijnbeek <mark_rynbeek@users.sf.net>
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

using NCDK.Tools;
using System.Collections.Generic;

namespace NCDK.IO.Formats
{
    /// <summary>
    /// Format for Symyx RGfiles (Rgroup query files).
    /// </summary>
    // @cdk.module ioformats
    public class RGroupQueryFormat : SimpleChemFormatMatcher, IChemFormatMatcher
    {
        private static IResourceFormat myself = null;

        public RGroupQueryFormat() { }

        public static IResourceFormat Instance
        {
            get
            {
                if (myself == null) myself = new RGroupQueryFormat();
                return myself;
            }
        }


        /// <inheritdoc/>
        public override string FormatName => "Symyx Rgroup query files";

        /// <inheritdoc/>
        public override string MIMEType => null;

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override IReadOnlyList<string> NameExtensions { get; } = new string[] { "mol", "rgp" };

        /// <inheritdoc/>
        public override string ReaderClassName { get; } = typeof(RGroupQueryReader).FullName;

        /// <inheritdoc/>
        public override string WriterClassName { get; } = typeof(RGroupQueryWriter).FullName;

        /// <inheritdoc/>
        public override bool Matches(int lineNumber, string line)
        {
            if (line.Contains("$RGP"))
                return true;
            else
                return false;
        }

        /// <inheritdoc/>
        public override bool IsXmlBased => false;

        /// <inheritdoc/>
        public override DataFeatures SupportedDataFeatures => RequiredDataFeatures | DataFeatures.Has2DCoordinates;

        /// <inheritdoc/>
        public override DataFeatures RequiredDataFeatures => DataFeatures.HasAtomElementSymbol;
    }
}
