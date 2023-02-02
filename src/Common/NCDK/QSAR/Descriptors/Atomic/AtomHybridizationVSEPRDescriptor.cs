/* Copyright (C) 2004-2007  Miguel Rojas <miguel.rojas@uni-koeln.de>
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

namespace NCDK.QSAR.Descriptors.Atomic
{
    /// <summary>
    /// This class returns the hybridization of an atom.
    /// </summary>
    /// <remarks>
    ///  <para>This class try to find a SIMPLE WAY the molecular geometry for following from
    ///    Valence Shell Electron Pair Repulsion or VSEPR model and at the same time its
    ///    hybridization of atoms in a molecule.</para>
    ///
    ///  <para>The basic premise of the model is that the electrons are paired in a molecule
    ///    and that the molecule geometry is determined only by the repulsion between the pairs.
    ///    The geometry adopted by a molecule is then the one in which the repulsions are minimized.</para>
    ///
    ///  <para>It counts the number of electron pairs in the Lewis dot diagram which
    ///   are attached to an atom. Then uses the following table.</para>
    /// 
    /// <list type="table">
    /// <listheader>
    ///    <term>pairs on an atom</term>
    ///    <term>hybridization of the atom</term>
    ///    <term>geometry</term>
    ///    <term>number for CDK.Constants</term>
    /// </listheader>
    ///   <item><term>2</term><term>sp</term><term>linear</term><term>1</term></item>
    ///   <item><term>3</term><term>sp^2</term><term>trigonal planar</term><term>2</term></item>
    ///   <item><term>4</term><term>sp^3</term><term>tetrahedral</term><term>3</term></item>
    ///   <item><term>5</term><term>sp^3d</term><term>trigonal bipyramid</term><term>4</term></item>
    ///   <item><term>6</term><term>sp^3d^2</term><term>octahedral</term><term>5</term></item>
    ///   <item><term>7</term><term>sp^3d^3</term><term>pentagonal bipyramid</term><term>6</term></item>
    ///   <item><term>8</term><term>sp^3d^4</term><term>square antiprim</term><term>7</term></item>
    ///   <item><term>9</term><term>sp^3d^5</term><term>tricapped trigonal prism</term><term>8</term></item>
    /// </list> 
    ///
    ///  <para>This table only works if the central atom is a p-block element
    ///   (groups IIA through VIIIA), not a transition metal.</para>
    /// </remarks>
    // @author         Miguel Rojas
    // @cdk.created    2005-03-24
    // @cdk.module     qsaratomic
    // @cdk.dictref qsar-descriptors:atomHybridizationVSEPR
    [DescriptorSpecification(DescriptorTargets.Atom, "http://www.blueobelisk.org/ontologies/chemoinformatics-algorithms/#atomHybridizationVSEPR")]
    public partial class AtomHybridizationVSEPRDescriptor : AbstractDescriptor, IAtomicDescriptor
    {
        private IAtomContainer container;

        public AtomHybridizationVSEPRDescriptor(IAtomContainer container)
        {
            this.container = container;
        }

        [DescriptorResult]
        public class Result : AbstractDescriptorResult
        {
            public Result(Hybridization value)
            {
                this.AtomHybridization = value;
            }

            [DescriptorResultProperty("hybr")]
            public Hybridization AtomHybridization { get; private set; }

            public Hybridization Value => AtomHybridization;
        }

        /// <summary>
        /// This method calculates the hybridization of an atom.
        /// </summary>
        /// <param name="atom">The <see cref="IAtom"/> for which the <see cref="Result"/> is requested</param>
        /// <returns>The hybridization</returns>
        public Result Calculate(IAtom atom)
        {
            var atomType = CDK.AtomTypeMatcher.FindMatchingAtomType(container, atom);
            if (atomType == null)
                throw new CDKException("Atom type was null");
            if (atomType.Hybridization.IsUnset())
                throw new CDKException("Hybridization was null");
            return new Result(atomType.Hybridization);
        }

        IDescriptorResult IAtomicDescriptor.Calculate(IAtom atom) => Calculate(atom);
    }
}
