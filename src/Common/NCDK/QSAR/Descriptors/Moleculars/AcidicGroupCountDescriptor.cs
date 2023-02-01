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
    /// Returns the number of acidic groups. The list of acidic groups is defined
    /// by these SMARTS 
    /// "$([O;H1]-[C,S,P]=O)", 
    /// "$([*;-;!$(*~[*;+])])",
    /// "$([NH](S(=O)=O)C(F)(F)F)", and "$(n1nnnc1)" 
    /// originally presented in
    /// JOELib <token>cdk-cite-WEGNER2006</token>.
    /// </summary>
    // @author      egonw
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:acidicGroupCount  
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#acidicGroupCount")]
    public class AcidicGroupCountDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private static readonly SmartsPattern[] tools = new string[]
            {
                "[$([O;H1]-[C,S,P]=O)]",
                "[$([*;-;!$(*~[*;+])])]",
                "[$([NH](S(=O)=O)C(F)(F)F)]",
                "[$(n1nnnc1)]"
            }.Select(n => SmartsPattern.Create(n)).ToArray();

        private readonly bool checkAromaticity;

        public AcidicGroupCountDescriptor(bool checkAromaticity = false)
        {
            this.checkAromaticity = checkAromaticity;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.NumberOfAcids = value;
            }

            [DescriptorResultProperty("nAcid")]
            public int NumberOfAcids { get; private set; }

            public int Value => NumberOfAcids;
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
