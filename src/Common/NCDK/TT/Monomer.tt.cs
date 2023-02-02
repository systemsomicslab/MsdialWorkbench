



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2001-2007  Edgar Luttmann <edgar@uni-paderborn.de>
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
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;

namespace NCDK.Default
{
    /// <summary>
    /// A Monomer is an AtomContainer which stores additional monomer specific
    /// informations for a group of Atoms.
    /// </summary>
    // @author     Edgar Luttmann <edgar@uni-paderborn.de>
    // @cdk.created    2001-08-06 
    // @cdk.keyword    monomer  
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class Monomer 
        : AtomContainer, IMonomer, ICloneable
    {
        /// <summary>The name of this monomer (e.g. Trp42).</summary>
        private string monomerName;

        /// <summary>The type of this monomer (e.g. TRP).</summary>
        private string monomerType;

        /// <summary>The name of this monomer (e.g. Trp42).</summary>
        public string MonomerName
        {
            get { return monomerName; }
            set
            {
                monomerName = value;
                 NotifyChanged();             }
        }

        /// <summary>The type of this monomer (e.g. TRP).</summary>
        public string MonomerType
        {
            get { return monomerType; }
            set
            {
                monomerType = value;
                 NotifyChanged();             }
        }

        public Monomer()
        {
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            return base.Clone(map);
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// A Monomer is an AtomContainer which stores additional monomer specific
    /// informations for a group of Atoms.
    /// </summary>
    // @author     Edgar Luttmann <edgar@uni-paderborn.de>
    // @cdk.created    2001-08-06 
    // @cdk.keyword    monomer  
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1710:IdentifiersShouldHaveCorrectSuffix", Justification = "Ignored")]
    public class Monomer 
        : AtomContainer, IMonomer, ICloneable
    {
        /// <summary>The name of this monomer (e.g. Trp42).</summary>
        private string monomerName;

        /// <summary>The type of this monomer (e.g. TRP).</summary>
        private string monomerType;

        /// <summary>The name of this monomer (e.g. Trp42).</summary>
        public string MonomerName
        {
            get { return monomerName; }
            set
            {
                monomerName = value;
                            }
        }

        /// <summary>The type of this monomer (e.g. TRP).</summary>
        public string MonomerType
        {
            get { return monomerType; }
            set
            {
                monomerType = value;
                            }
        }

        public Monomer()
        {
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            return base.Clone(map);
        }
    }
}
