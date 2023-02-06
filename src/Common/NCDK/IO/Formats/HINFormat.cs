/* Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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
using System;
using System.Collections.Generic;

namespace NCDK.IO.Formats
{
    /// <summary>
    /// See <see href="http://www.hyper.com/">here</see>.
    /// </summary>
    // @cdk.module ioformats
    public class HINFormat : SimpleChemFormatMatcher, IChemFormatMatcher
    {
        public HINFormat() { }

        public static IResourceFormat Instance { get; } = new HINFormat();

        /// <inheritdoc/>
        public override string FormatName => "HyperChem HIN";

        /// <inheritdoc/>
        public override string MIMEType => "chemical/x-hin";

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override IReadOnlyList<string> NameExtensions => new string[] { "hin" };

        /// <inheritdoc/>
        public override string ReaderClassName => typeof(HINReader).FullName;

        /// <inheritdoc/>
        public override string WriterClassName => typeof(HINWriter).FullName;

        /// <inheritdoc/>
        public override bool Matches(int lineNumber, string line)
        {
            if (line.StartsWith("atom ", StringComparison.Ordinal)
                    && (line.EndsWith(" s", StringComparison.Ordinal) || line.EndsWith(" d", StringComparison.Ordinal) || line.EndsWith(" t", StringComparison.Ordinal) || line.EndsWith(" a", StringComparison.Ordinal)))
            {
                var tokens = line.Split(' ');
                if ((tokens.Length % 2) == 0)
                {
                    // odd number of values found, typical for HIN
                    return true;
                }
            }
            return false;
        }

        /// <inheritdoc/>
        public override bool IsXmlBased => false;

        /// <inheritdoc/>
        public override DataFeatures SupportedDataFeatures => RequiredDataFeatures | DataFeatures.HasGraphRepresentation;

        /// <inheritdoc/>
        public override DataFeatures RequiredDataFeatures => DataFeatures.Has3DCoordinates | DataFeatures.HasAtomPartialCharges
                    | DataFeatures.HasAtomElementSymbol;
    }
}
