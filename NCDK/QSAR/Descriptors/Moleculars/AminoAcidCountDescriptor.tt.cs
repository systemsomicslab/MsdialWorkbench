
/* Copyright (C) 2006-2007  The Chemistry Development Kit (CDK) project
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

using NCDK.Isomorphisms;
using NCDK.Templates;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Class that returns the number of each amino acid in an atom container.
    /// </summary>
    /// <remarks>
    /// Returns 20 values with names of the form <i>nX</i>, where <i>X</i> is the short version
    /// of the amino acid name
    /// </remarks>
    // @author      egonw
    // @cdk.created 2006-01-15
    // @cdk.module  qsarprotein
    // @cdk.dictref qsar-descriptors:aminoAcidsCount
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#aminoAcidsCount")]
    public class AminoAcidCountDescriptor : AbstractDescriptor, IMolecularDescriptor
    {
        private readonly IChemObjectSet<IAtomContainer> substructureSet = MakeSubstructureSet();

        static IChemObjectSet<IAtomContainer> MakeSubstructureSet()
        {
            var aas = AminoAcids.Proteinogenics;
            var substructureSet = aas[0].Builder.NewAtomContainerSet();
            foreach (var aa in aas)
            {
                substructureSet.Add(aa);
            }
            return substructureSet;
        }

        public AminoAcidCountDescriptor()
        {
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(IReadOnlyList<int> values)
            {
                if (AminoAcids.Proteinogenics.Count != values.Count)
                    throw new ArgumentException(nameof(values));
                this.Values = values;
            }

            public new IReadOnlyList<int> Values { get; private set; }

            [DescriptorResultProperty("nA")]
            public int NumberOfA => Values[0];
            [DescriptorResultProperty("nR")]
            public int NumberOfR => Values[1];
            [DescriptorResultProperty("nN")]
            public int NumberOfN => Values[2];
            [DescriptorResultProperty("nD")]
            public int NumberOfD => Values[3];
            [DescriptorResultProperty("nC")]
            public int NumberOfC => Values[4];
            [DescriptorResultProperty("nF")]
            public int NumberOfF => Values[5];
            [DescriptorResultProperty("nQ")]
            public int NumberOfQ => Values[6];
            [DescriptorResultProperty("nE")]
            public int NumberOfE => Values[7];
            [DescriptorResultProperty("nG")]
            public int NumberOfG => Values[8];
            [DescriptorResultProperty("nH")]
            public int NumberOfH => Values[9];
            [DescriptorResultProperty("nI")]
            public int NumberOfI => Values[10];
            [DescriptorResultProperty("nP")]
            public int NumberOfP => Values[11];
            [DescriptorResultProperty("nL")]
            public int NumberOfL => Values[12];
            [DescriptorResultProperty("nK")]
            public int NumberOfK => Values[13];
            [DescriptorResultProperty("nM")]
            public int NumberOfM => Values[14];
            [DescriptorResultProperty("nS")]
            public int NumberOfS => Values[15];
            [DescriptorResultProperty("nT")]
            public int NumberOfT => Values[16];
            [DescriptorResultProperty("nY")]
            public int NumberOfY => Values[17];
            [DescriptorResultProperty("nV")]
            public int NumberOfV => Values[18];
            [DescriptorResultProperty("nW")]
            public int NumberOfW => Values[19];
        }

        /// <summary>
        /// Determine the number of amino acids groups the supplied <see cref="IAtomContainer"/>.
        /// </summary>
        /// <returns>the number of aromatic atoms of this AtomContainer</returns>
        public Result Calculate(IAtomContainer container)
        {            
            container = (IAtomContainer)container.Clone();
            var results = new List<int>(substructureSet.Count);

            var universalIsomorphismTester = new UniversalIsomorphismTester();
            foreach (var substructure in substructureSet)
            {
                var maps = universalIsomorphismTester.GetSubgraphMaps(container, substructure);
                results.Add(maps.Count());
            }

            return new Result(results);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
