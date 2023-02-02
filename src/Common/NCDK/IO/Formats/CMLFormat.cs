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
    /// See <see href="http://en.wikipedia.org/wiki/Chemical_Markup_Language">here</see>.
    /// </summary>
    // @cdk.module ioformats
    public class CMLFormat : SimpleChemFormatMatcher, IChemFormatMatcher
    {
        private static IResourceFormat myself = null;

        public CMLFormat() { }

        public static IResourceFormat Instance
        {
            get
            {
                if (myself == null) myself = new CMLFormat();
                return myself;
            }
        }

        /// <inheritdoc/>
        public override string FormatName => "Chemical Markup Language";

        /// <inheritdoc/>
        public override string MIMEType => "chemical/x-cml";

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override IReadOnlyList<string> NameExtensions { get; } = new string[] { "cml", "xml" };

        /// <inheritdoc/>
        public override string ReaderClassName { get; } = typeof(CMLReader).FullName;

        /// <inheritdoc/>
        public override string WriterClassName { get; } = typeof(CMLWriter).FullName;

        /// <inheritdoc/>
        public override bool Matches(int lineNumber, string line)
        {
            if (line.Contains("http://www.xml-cml.org/schema")
             || line.Contains("<atom")
             || line.Contains("<molecule")
             || line.Contains("<reaction") 
             || line.Contains("<cml")
             || line.Contains("<bond") )
            {
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override bool IsXmlBased => true;

        /// <inheritdoc/>
        public override DataFeatures SupportedDataFeatures => DataFeatures.Has2DCoordinates | DataFeatures.Has3DCoordinates
                    | DataFeatures.HasAtomPartialCharges | DataFeatures.HasAtomFormalCharges
                    | DataFeatures.HasAtomMassNumbers | DataFeatures.HasAtomIsotopeNumbers
                    | DataFeatures.HasGraphRepresentation | DataFeatures.HasAtomElementSymbol;

        /// <inheritdoc/>
        public override DataFeatures RequiredDataFeatures => DataFeatures.None;
    }
}
