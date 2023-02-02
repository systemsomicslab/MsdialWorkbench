/*  Copyright (C) 2004-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Aromaticities;
using NCDK.Tools.Manipulator;
using System;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// This Class contains a method that returns the number failures of the
    /// Lipinski's Rule Of 5.
    /// See <see href="http://en.wikipedia.org/wiki/Lipinski%27s_Rule_of_Five" />.
    /// </summary>
    // @author      mfe4
    // @cdk.created 2004-11-03
    // @cdk.module  qsarmolecular
    // @cdk.dictref qsar-descriptors:lipinskifailures
    // @cdk.keyword Lipinski
    // @cdk.keyword rule-of-five
    // @cdk.keyword descriptor
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#lipinskifailures")]
    public class RuleOfFiveDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private readonly bool checkAromaticity;

        public RuleOfFiveDescriptor(bool checkAromaticity = false)
        {
            this.checkAromaticity = checkAromaticity;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(int value)
            {
                this.LipinskiFailures = value;
            }

            /// <summary>
            /// The number of failures of the Lipinski rule
            /// </summary>
            [DescriptorResultProperty("LipinskiFailures")]
            public int LipinskiFailures { get; private set; }

            public int Value => LipinskiFailures;
        }

        public Result Calculate(IAtomContainer container,
            Func<IAtomContainer, double> calcLogP = null,
            Func<IAtomContainer, int> calcHBondAcceptorCount = null,
            Func<IAtomContainer, int> calcHBondDonorCount = null,
            Func<IAtomContainer, double> calcWeight = null,
            Func<IAtomContainer, int> calcRotatableBondsCount = null)
        {
            // do aromaticity detection
            if (checkAromaticity)
            {
                container = (IAtomContainer)container.Clone(); // don't mod original

                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(container);
                Aromaticity.CDKLegacy.Apply(container);
            }

            calcLogP = calcLogP ?? (mol => new XLogPDescriptor().Calculate(mol, correctSalicylFactor: true).Value);
            calcHBondAcceptorCount = calcHBondAcceptorCount ?? (mol => new HBondAcceptorCountDescriptor().Calculate(mol).Value);
            calcHBondDonorCount = calcHBondDonorCount ?? (mol => new HBondDonorCountDescriptor().Calculate(mol).Value);
            calcWeight = calcWeight ?? (mol => new WeightDescriptor().Calculate(mol).Value);
            calcRotatableBondsCount = calcRotatableBondsCount ?? (mol => new RotatableBondsCountDescriptor().Calculate(mol, includeTerminals: false, excludeAmides: true).Value);

            int lipinskifailures = 0;

            var xlogPvalue = calcLogP(container);
            var acceptors = calcHBondAcceptorCount(container);
            var donors = calcHBondDonorCount(container);
            var mwvalue = calcWeight(container);

            // exclude (heavy atom) terminal bonds
            // exclude amide C-N bonds because of their high rotational barrier
            // see Veber, D.F. et al., 2002, 45(12), pp.2615–23.
            var rotatablebonds = calcRotatableBondsCount(container);

            if (xlogPvalue > 5.0)
                lipinskifailures += 1;
            if (acceptors > 10)
                lipinskifailures += 1;
            if (donors > 5)
                lipinskifailures += 1;
            if (mwvalue > 500.0)
                lipinskifailures += 1;
            if (rotatablebonds > 10.0)
                lipinskifailures += 1;

            return new Result(lipinskifailures);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
