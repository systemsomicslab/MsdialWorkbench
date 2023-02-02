/* Copyright (C) 2010  Egon Willighagen <egonw@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
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

using NCDK.Aromaticities;
using NCDK.SMARTS;
using NCDK.Tools.Manipulator;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Returns the number of basic groups. The list of basic groups is defined
    /// by these SMARTS 
    /// "[$([NH2]-[CX4])]", 
    /// "[$([NH](-[CX4])-[CX4])]",
    /// "[$(N(-[CX4])(-[CX4])-[CX4])]",    
    /// "[$([*;+;!$(*~[*;-])])]",
    /// "[$(N=C-N)]", and "[$(N-C=N)]" 
    /// originally presented in
    /// JOELib <token>cdk-cite-WEGNER2006</token>.
    /// </summary>
    // @author      egonw
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:acidicGroupCount
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#basicGroupCount")]
    public class BasicGroupCountDescriptor  : AbstractDescriptor, IMolecularDescriptor
    {
        private static readonly SmartsPattern[] tools = new string[]
            {
                "[$([NH2]-[CX4])]",
                "[$([NH](-[CX4])-[CX4])]",
                "[$(N(-[CX4])(-[CX4])-[CX4])]",
                "[$([*;+;!$(*~[*;-])])]",
                "[$(N=C-N)]",
                "[$(N-C=N)]"
            }.Select(n => SmartsPattern.Create(n)).ToArray();

        private readonly bool checkAromaticity;

        public BasicGroupCountDescriptor(bool checkAromaticity = false)
        {
            this.checkAromaticity = checkAromaticity;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.NumberOfBase = value;
            }

            [DescriptorResultProperty("nBase")]
            public int NumberOfBase { get; private set; }

            public int Value => NumberOfBase;
        }

        public Result Calculate(IAtomContainer container)
        {
            container = (IAtomContainer)container.Clone(); // don't mod original

            // do aromaticity detection
            if (checkAromaticity)
            {
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                Aromaticity.CDKLegacy.Apply(container);
            }

            var count = tools.Select(n => n.MatchAll(container).Count()).Sum();
            return new Result(count);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
