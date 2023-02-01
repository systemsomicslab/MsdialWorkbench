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
    /// <summary>
    /// See <see href="http://www.tripos.com/data/support/mol2.pdf">here</see>.
    /// </summary>
    // @cdk.module ioformats
    public class Mol2Format : SimpleChemFormatMatcher, IChemFormatMatcher
    {
        private static IResourceFormat myself = null;

        public Mol2Format() { }

        public static IResourceFormat Instance
        {
            get
            {
                if (myself == null) myself = new Mol2Format();
                return myself;
            }
        }

        /// <inheritdoc/>
        public override string FormatName => "Mol2 (Sybyl)";

        /// <inheritdoc/>
        public override string MIMEType => "chemical/x-mol2";

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override IReadOnlyList<string> NameExtensions { get; } = new string[] { "mol2" };

        /// <inheritdoc/>
        public override string ReaderClassName { get; } = typeof(Mol2Reader).FullName;

        /// <inheritdoc/>
        public override string WriterClassName { get; } = typeof(Mol2Writer).FullName;

        /// <inheritdoc/>
        public override bool Matches(int lineNumber, string line)
        {
            if (line.Contains("<TRIPOS>"))
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override bool IsXmlBased => false;

        /// <inheritdoc/>
        public override DataFeatures SupportedDataFeatures => RequiredDataFeatures | DataFeatures.Has2DCoordinates | DataFeatures.Has3DCoordinates
                    | DataFeatures.HasGraphRepresentation;

        /// <inheritdoc/>
        public override DataFeatures RequiredDataFeatures => DataFeatures.HasAtomElementSymbol;
    }
}
