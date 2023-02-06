/* Copyright (c) 2018 Kazuya Ujihara <ujihara.kazuya@gmail.com>
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

namespace NCDK.QSAR.Descriptors.Moleculars
{
    /// <summary>
    /// Calculates F<sub>sp</sub><sup>3</sup> (number of sp<sup>3</sup> hybridized carbons/total carbon count) <token>cdk-cite-Lovering2009</token>.
    /// </summary>
    [DescriptorSpecification(DescriptorTargets.AtomContainer, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#Fsp3")]
    public class FractionalCSP3Descriptor : AbstractDescriptor, IMolecularDescriptor
    {
        public FractionalCSP3Descriptor()
        {            
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(double value)
            {
                this.Fsp3 = value;
            }

            [DescriptorResultProperty("Fsp3")]
            public double Fsp3 { get; private set; }

            public double Value => Fsp3;
        }

        /// <summary>
        /// Calculates the fraction of C atoms that are SP3 hybridized.
        /// </summary>
        /// <returns>Fsp<sup>3</sup></returns>
        public Result Calculate(IAtomContainer container)
        {
            double v;
            int nC = 0;
            int nCSP3 = 0;
            var matcher = CDK.AtomTypeMatcher;
            foreach (var atom in container.Atoms)
            {
                if (atom.AtomicNumber == 6)
                {
                    nC++;
                    var matched = matcher.FindMatchingAtomType(container, atom);
                    if (matched != null && matched.Hybridization == Hybridization.SP3)
                    {
                        nCSP3++;
                    }
                }
            }
            v = nC == 0 ? 0 : (double)nCSP3 / nC;

            return new Result(v);
        }

        IDescriptorResult IMolecularDescriptor.Calculate(IAtomContainer mol) => Calculate(mol);
    }
}
