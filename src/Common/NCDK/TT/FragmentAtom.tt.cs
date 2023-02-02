



// .NET Framework port by Kazuya Ujihara
// Copyright (C) 2016-2017  Kazuya Ujihara <ujihara.kazuya@gmail.com>

/* Copyright (C) 2006-2007  Egon Willighagen <ewilligh@uni-koeln.de>
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
using System.Linq;

namespace NCDK.Default
{
    /// <summary>
    /// Class to represent an IPseudoAtom which embeds an IAtomContainer. Very much
    /// like the MDL Molfile "Group" concept.
    /// </summary>
    // @author egonw 
    public class FragmentAtom 
        : PseudoAtom, IFragmentAtom
    {
        public FragmentAtom()
        {
            Fragment = Builder.NewAtomContainer();
        }

        public virtual bool IsExpanded { get; set; }
        public virtual IAtomContainer Fragment { get; set; }

        /// <summary>
        /// The exact mass of an FragmentAtom is defined as the sum of exact masses
        /// of the IAtom's in the fragment.
        /// </summary>
        public override double? ExactMass
        {
            get { return Fragment.Atoms.Select(atom => atom.ExactMass.Value).Sum(); }
            set { throw new InvalidOperationException($"Cannot set the mass of a {nameof(IFragmentAtom)}."); }
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            FragmentAtom clone = (FragmentAtom)base.Clone(map);
            clone.Fragment = (IAtomContainer)Fragment.Clone(map);
            clone.IsExpanded = IsExpanded;
            return clone;
        }
    }
}
namespace NCDK.Silent
{
    /// <summary>
    /// Class to represent an IPseudoAtom which embeds an IAtomContainer. Very much
    /// like the MDL Molfile "Group" concept.
    /// </summary>
    // @author egonw 
    public class FragmentAtom 
        : PseudoAtom, IFragmentAtom
    {
        public FragmentAtom()
        {
            Fragment = Builder.NewAtomContainer();
        }

        public virtual bool IsExpanded { get; set; }
        public virtual IAtomContainer Fragment { get; set; }

        /// <summary>
        /// The exact mass of an FragmentAtom is defined as the sum of exact masses
        /// of the IAtom's in the fragment.
        /// </summary>
        public override double? ExactMass
        {
            get { return Fragment.Atoms.Select(atom => atom.ExactMass.Value).Sum(); }
            set { throw new InvalidOperationException($"Cannot set the mass of a {nameof(IFragmentAtom)}."); }
        }

        public override ICDKObject Clone(CDKObjectMap map)
        {
            FragmentAtom clone = (FragmentAtom)base.Clone(map);
            clone.Fragment = (IAtomContainer)Fragment.Clone(map);
            clone.IsExpanded = IsExpanded;
            return clone;
        }
    }
}
