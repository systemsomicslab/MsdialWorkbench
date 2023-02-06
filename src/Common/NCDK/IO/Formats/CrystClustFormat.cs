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
    // @cdk.module ioformats
    public class CrystClustFormat : SimpleChemFormatMatcher, IChemFormatMatcher
    {
        private static IResourceFormat myself = null;

        public CrystClustFormat() { }

        public static IResourceFormat Instance
        {
            get
            {
                if (myself == null) myself = new CrystClustFormat();
                return myself;
            }
        }

        /// <inheritdoc/>
        public override string FormatName => "CrystClust";

        /// <inheritdoc/>
        public override string MIMEType => null;

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override IReadOnlyList<string> NameExtensions { get; } = new string[] { "crystclust" };

        /// <inheritdoc/>
        public override string ReaderClassName => "NCDK.IO.CrystClustReader";

        /// <inheritdoc/>
        public override string WriterClassName => "NCDK.IO.CrystClustWriter";

        /// <inheritdoc/>
        public override bool Matches(int lineNumber, string line)
        {
            if (lineNumber == 1 && line.StartsWith("frame: ", StringComparison.Ordinal))
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override bool IsXmlBased => false;

        /// <inheritdoc/>
        public override DataFeatures SupportedDataFeatures => RequiredDataFeatures;

        /// <inheritdoc/>
        public override DataFeatures RequiredDataFeatures =>
                DataFeatures.Has3DCoordinates | DataFeatures.HasUnitcellParameters
                    | DataFeatures.HasAtomElementSymbol;
    }
}
