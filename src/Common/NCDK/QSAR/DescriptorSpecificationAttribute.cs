/*
 * Copyright (C) 2018  Kazuya Ujihara <ujihara.kazuya@gmail.com>
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

using System;

namespace NCDK.QSAR
{
    /// <summary>
    /// Attribute for specifying the type of descriptor.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public sealed class DescriptorSpecificationAttribute : Attribute
    {
        /// <summary>
        /// Reference to a formal definition in a
        /// dictionary (e.g. in STMML format) of the descriptor, preferably
        /// referring to the original article. The format of the content is
        /// expected to be &lt;dictionaryNameSpace&gt;:&lt;entryID&gt;.
        /// </summary>
        public string Reference { get; set; }

        /// <summary>
        /// Unique identifier for the actual
        /// implementation, preferably including the exact version number of
        /// the source code. E.g. $Id$ can be used when the source code is
        /// in a CVS repository.
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Name of the organisation/person/program/whatever who wrote/packaged the implementation.
        /// </summary>
        public string Vendor { get; set; }

        public DescriptorSpecificationAttribute(DescriptorTargets target)
            : this(target, null)
        {
        }

        public DescriptorSpecificationAttribute(DescriptorTargets target, string reference)
            : this(target, reference, CDK.Version, "The Chemistry Development Kit")
        {
        }

        public DescriptorSpecificationAttribute(DescriptorTargets target, string reference, string identifier, string vendor)
        {
            this.Reference = reference;
            this.Identifier = identifier;
            this.Vendor = vendor;
        }

        public override string ToString()
        {
            return "{"
                + "Reference=" + (this.Reference == null ? "null" : $"\"{this.Reference}\"") + ", "
                + "Identifier=" + (this.Identifier == null ? "null" : $"\"{this.Identifier}\"") + ", "
                + "Vendor=" + (this.Vendor == null ? "null" : $"\"{this.Vendor}\"") + "}";
        }

        public DescriptorTargets Target { get; set; }

        public DescriptorRequirements Requirements { get; set; }

    }

    [Flags]
    public enum DescriptorTargets
    {
        Atom = 1,
        Bond = 2,
        AtomContainer = 4,
        BioPolymer = 8,

        Enumerable = 0x10000,
        Pair = 0x20000,

        Substance = AtomContainer | Enumerable,
        AtomPair = Atom | Pair,
    }

    [Flags]
    public enum DescriptorRequirements
    {
        Geometry2D = 1,
        Geometry3D = 2,
    }
}
