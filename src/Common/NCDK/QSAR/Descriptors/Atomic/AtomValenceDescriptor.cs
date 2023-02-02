/* Copyright (C) 2004-2007  Matteo Floris <mfe4@users.sf.net>
 *                     2008  Egon Willighagen <egonw@users.sf.net>
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Collections.Generic;

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class returns the valence of an atom.
    /// This descriptor does not have any parameters.
    /// </summary>
    /// <seealso cref="AtomValenceTool"/>
    // @author      mfe4
    // @cdk.created 2004-11-13
    // @cdk.module  qsaratomic
    // @cdk.dictref qsar-descriptors:atomValence
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#atomValence")]
    public partial class AtomValenceDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        IAtomContainer container;

        public AtomValenceDescriptor(IAtomContainer container)
        {
            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.AtomValence = value;
            }

            [DescriptorResultProperty("val")]
            public int AtomValence { get; private set; }

            public int Value => AtomValence;
        }
        
        /// <summary>
        /// This method calculates the valence of an atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>The valence of an atom</returns>
        public Result Calculate(IAtom atom)
        {
            int atomValence = AtomValenceTool.GetValence(atom);
            return new Result(atomValence);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
