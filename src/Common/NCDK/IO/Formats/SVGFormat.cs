/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
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

using System.Collections.Generic;

namespace NCDK.IO.Formats
{
    // @cdk.module ioformats
    public class SVGFormat : AbstractResourceFormat, IResourceFormat
    {
        private static IResourceFormat myself = null;

        public SVGFormat() { }

        public static IResourceFormat Instance
        {
            get
            {
                if (myself == null) myself = new SVGFormat();
                return myself;
            }
        }

        /// <inheritdoc/>
        public override string FormatName => "Scalable Vector Graphics";

        /// <inheritdoc/>
        public override string MIMEType => "image/svg+xml";

        /// <inheritdoc/>
        public override string PreferredNameExtension => NameExtensions[0];

        /// <inheritdoc/>
        public override IReadOnlyList<string> NameExtensions { get; } = new string[] { "svg" };

        /// <inheritdoc/>
        public override bool IsXmlBased => true;
    }
}
