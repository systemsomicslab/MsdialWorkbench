/* Copyright (C) 2003-2007  The Chemistry Development Kit (CDK) project
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 * All we ask is that proper credit is given for our work, which includes
 * - but is not limited to - adding the above copyright notice to the beginning
 * of your source code files, and to any copyright notice that you may distribute
 * with programs based on this work.
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

using System.Collections.Generic;

namespace NCDK.Dict
{
    /// <summary>
    /// Entry in a Dictionary.
    /// </summary>
    /// <seealso cref="EntryDictionary"/>
    // @author       Egon Willighagen <egonw@users.sf.net>
    // @cdk.created  2003-08-23
    // @cdk.keyword  dictionary
    // @cdk.module   dict
    public class Entry
    {
        private string identifier;
        private readonly List<string> descriptorInfo;

        public Entry(string identifier, string term)
        {
            this.identifier = identifier.ToLowerInvariant();
            Label = term;
            this.descriptorInfo = new List<string>();
        }

        public Entry(string identifier)
            : this(identifier, "")
        { }

        public Entry()
            : this("", "")
        { }

        public string Label { get; set; }

        public string Id
        {
            set { identifier = value.ToLowerInvariant(); }
            get { return identifier; }
        }

        public string Definition { get; set; }

        public string Description { get; set; }

        public void AddDescriptorMetadata(string metadata)
        {
            this.descriptorInfo.Add(metadata);
        }

        public IReadOnlyList<string> DescriptorMetadata
        {
            get
            {
                return this.descriptorInfo;
            }
        }

        public override string ToString()
        {
            return "Entry[" + Id + "](" + Label + ")";
        }

        public object RawContent { get; set; }

        public string ClassName { get; set; }
    }
}
