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
using System.Collections.Generic;

namespace NCDK.IO.Formats
{
    // @cdk.module ioformats
    public class MDLV2000Format : SimpleChemFormatMatcher, IChemFormatMatcher
    {
        public MDLV2000Format() { }

        public static IResourceFormat Instance { get; } = new MDLV2000Format();

        /// <inheritdoc/>
        public override string FormatName => "MDL Molfile V2000";

        /// <inheritdoc/>
        public override string MIMEType => "chemical/x-mdl-molfile";

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override IReadOnlyList<string> NameExtensions => new string[] { "mol" };

        /// <inheritdoc/>
        public override string ReaderClassName => typeof(NCDK.IO.MDLV2000Reader).ToString();

        /// <inheritdoc/>
        public override string WriterClassName => typeof(NCDK.IO.MDLV2000Writer).ToString();

        /// <inheritdoc/>
        public override bool Matches(int lineNumber, string line)
        {
            if (lineNumber == 4 && (line.Contains("v2000") || line.Contains("V2000")))
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override bool IsXmlBased => false;

        /// <inheritdoc/>
        public override DataFeatures SupportedDataFeatures
            => RequiredDataFeatures | DataFeatures.Has2DCoordinates | DataFeatures.Has3DCoordinates
                    | DataFeatures.HasGraphRepresentation;

        /// <inheritdoc/>
        public override DataFeatures RequiredDataFeatures
            => DataFeatures.HasAtomElementSymbol;
    }
}
