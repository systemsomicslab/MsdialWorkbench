/*
 * Copyright (c) 2013 European Bioinformatics Institute (EMBL-EBI)
 *                    John May <jwmay@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation; either version 2.1 of the License, or (at
 * your option) any later version. All we ask is that proper credit is given
 * for our work, which includes - but is not limited to - adding the above
 * copyright notice to the beginning of your source code files, and to any
 * copyright notice that you may distribute with programs based on this work.
 *
 * This program is distributed in the hope that it will be useful, but WITHOUT
 * Any WARRANTY; without even the implied warranty of MERCHANTABILITY or
 * FITNESS FOR A PARTICULAR PURPOSE.  See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 U
 */

using NCDK.RingSearches;
using System.Collections.Generic;

namespace NCDK.Aromaticities
{
    /// <summary>
    /// Defines an electron donation model for perceiving aromatic systems. The model
    /// defines which atoms are allowed and how many electron it contributes. 
    /// </summary>
    /// <remarks>
    /// There are currently several models available.
    /// <list type="bullet">
    ///     <item><see cref="CDK"/>/<see cref="CDKAllowingExocyclicModel"/> - uses the information
    ///     form the preset CDK atom types to determine how many electrons each atom
    ///     should contribute. The model can either allow or exclude contributions
    ///     from exocyclic pi bonds. This model requires that atom types have be
    ///     perceived.
    /// </item>
    ///     <item><see cref="PiBondsModel"/> - a simple model only allowing cyclic pi bonds to contribute. This model only requires that bond orders are set.</item>
    ///     <item>
    ///      <see cref="DaylightModel"/> - a model similar to that used by Daylight for SMILES.
    ///      This model does not require atom types to be defined but every atom should
    ///      have it's hydrogen count set.
    ///     </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// To obtain an instance of the model simply invoke the named method.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Aromaticities.ElectronDonation_Example.cs"]/*' />
    /// </example>
    // @author John May
    // @cdk.module standard
    public abstract class ElectronDonation
    {
        /// <summary>
        /// Determine the number 'p' electron contributed by each atom in the
        /// provided <paramref name="container"/>. A value of '0' indicates the atom can
        /// contribute but that it contributes no electrons. A value of '-1'
        /// indicates the atom should not contribute at all.
        /// </summary>
        /// <param name="container">molecule</param>
        /// <param name="ringSearch">ring information</param>
        /// <returns>electron contribution of each atom (-1=none)</returns>
        public abstract IReadOnlyList<int> Contribution(IAtomContainer container, RingSearch ringSearch);

        /// <summary>
        /// Electron donation model to use for aromaticity perception.
        /// <para>
        /// Use the preset CDK atom types to determine the electron contribution of
        /// atoms. If an atom type has not been perceived or hybridisation is unset a
        /// runtime exception is thrown.</para>
        /// </summary>
        /// <remarks>
        /// The model accepts cyclic atoms which
        /// are <see cref="Hybridization.SP2"/> or
        /// <see cref="Hybridization.Planar3"/>
        /// hybridised. The <see cref="CDKPropertyName.PiBondCount"/> and
        /// <see cref="CDKPropertyName.LonePairCount"/> to determine how
        /// many electrons an atom type can contribute. Generally these values are
        /// not automatically configured and so several atom types are cached
        /// for lookup: <list type="bullet"> <item>N.planar3: 2 electrons </item>
        /// <item>N.minus.planar3: 2 electrons </item> <item>N.amide: 2 electrons </item>
        /// <item>S.2: 2 electrons </item> <item>S.planar3: 2 electrons </item>
        /// <item>C.minus.planar: 2 electrons </item> <item>O.planar3: 2 electrons </item>
        /// <item>N.sp2.3: 1 electron </item> <item>C.sp2: 1 electron </item> </list>
        /// Exocyclic pi bonds are not allowed to contribute.
        /// </remarks>
        /// <seealso cref="IAtomType.AtomTypeName"/>
        public static ElectronDonation CDKModel { get; } = new AtomTypeModel(false);

        /// <summary>
        /// Electron donation model to use for aromaticity perception.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use the preset CDK atom types to determine the electron contribution of
        /// atoms. If an atom type has not been perceived or hybridisation is unset a
        /// runtime exception is thrown.
        /// </para>
        /// <para>
        /// The model accepts cyclic atoms which
        /// are <see cref="Hybridization.SP2"/> or
        /// <see cref="Hybridization.Planar3"/>
        /// hybridised. The <see cref="CDKPropertyName.PiBondCount"/> and
        /// <see cref="CDKPropertyName.LonePairCount"/> to determine how
        /// many electrons an atom type can contribute. Generally these values are
        /// not automatically configured and so several atom types are cached
        /// for lookup: <list type="bullet"> <item>N.planar3: 2 electrons </item>
        /// <item>N.minus.planar3: 2 electrons </item> <item>N.amide: 2 electrons </item>
        /// <item>S.2: 2 electrons </item> <item>S.planar3: 2 electrons </item>
        /// <item>C.minus.planar: 2 electrons </item> <item>O.planar3: 2 electrons </item>
        /// <item>N.sp2.3: 1 electron </item> <item>C.sp2: 1 electron </item> </list>
        /// Exocyclic pi bonds are not allowed to contribute.
        /// </para>
        /// </remarks>
        /// <seealso cref="IAtomType.AtomTypeName"/>
        public static ElectronDonation CDKAllowingExocyclicModel { get; } = new AtomTypeModel(true);

        /// <summary>
        /// Electron donation model to use for aromaticity perception.
        /// </summary>
        /// <remarks>
        /// <para>
        /// A very simple aromaticity model which only allows atoms adjacent to
        /// cyclic pi bonds. Lone pairs are not consider and as such molecules like
        /// furan and pyrrole are non-aromatic. The model is useful for storing
        /// aromaticity in MDL and Mol2 file formats where aromatic systems involving
        /// a lone pair can not be properly represented.</para>
        /// </remarks>
        public static ElectronDonation PiBondsModel { get; } = new PiBondModel();

        /// <summary>
        /// Electron donation model closely mirroring the Daylight model for use in
        /// generating SMILES. The model was interpreted from various resources and
        /// as such may not match exactly. If you find an inconsistency please add a
        /// request for enhancement to the patch tracker. One known limitation is
        /// that this model does not currently consider unknown/pseudo atoms '*'.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The model makes a couple of assumptions which it will not correct for.
        /// Checked assumptions cause the model to throw a runtime exception. <list type="bullet">
        /// <item>there should be no valence errors (unchecked)</item> <item>every atom has
        /// a set implicit hydrogen count (checked)</item> <item>every bond has defined
        /// order, single, double etc (checked)</item> <item>atomic number of non-pseudo
        /// atoms is set (checked)</item> </list> 
        /// </para>
        /// <para>
        /// The aromaticity model in SMILES was designed to simplify canonicalisation
        /// and express symmetry in a molecule. The contributed electrons can be
        /// summarised as follows (refer to code for exact specification): <list type="bullet">
        /// <item>carbon, nitrogen, oxygen, phosphorus, sulphur, arsenic and selenium
        /// are allow to be aromatic</item> <item>atoms should be Sp2 hybridised - not
        /// actually computed</item> <item>atoms adjacent to a single cyclic pi bond
        /// contribute 1 electron</item> <item>neutral or negatively charged atoms with a
        /// lone pair contribute 2 electrons</item> <item>exocyclic pi bonds are allowed
        /// but if the exocyclic atom is more electronegative it consumes an
        /// electron. As an example ketone groups contribute '0'
        /// electrons.</item></list>
        /// </para>
        /// </remarks>
        public static ElectronDonation DaylightModel { get; } = new DaylightModel();
    }
}
