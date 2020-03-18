/* 
 * Copyright (C) 1997-2007  Christoph Steinbeck <steinbeck@users.sf.net>
 *                    2014  Mark B Vine (orcid:0000-0002-7794-0426)
 *
 *  Contact: cdk-devel@lists.sourceforge.net
 *
 *  This program is free software; you can redistribute it and/or
 *  modify it under the terms of the GNU Lesser General Public License
 *  as published by the Free Software Foundation; either version 2.1
 *  of the License, or (at your option) any later version.
 *  All I ask is that proper credit is given for my work, which includes
 *  - but is not limited to - adding the above copyright notice to the beginning
 *  of your source code files, and to any copyright notice that you may distribute
 *  with programs based on this work.
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

namespace NCDK
{
    internal static class CDKConstants
    {
        internal const int IsPlacedMask = 0x0001;
        internal const int IsInRingMask = 0x0002;
        internal const int IsNotInIngMask = 0x0004;
        internal const int IsAliphaticMask = 0x0008;
        internal const int IsVisitedMask = 0x0010;
        internal const int IsAromaticMask = 0x0020;
        internal const int IsConjugatedMask = 0x0040;
        internal const int IsMappedMask = 0x0080;
        internal const int IsHydrogenBondDonorMask = 0x0100;
        internal const int IsHydrogenBondAcceptorMask = 0x0200;
        internal const int IsReactiveCenterMask = 0x0400;
        internal const int IsTypeableMask = 0x0800;
        internal const int IsSingleOrDoubleMask = 0x1000;
    }

    [Obsolete("Use " + nameof(IStereoElement<IChemObject, IChemObject>))]
    public static class StereoAtomParities
    {
        /// <summary>A positive atom parity.</summary>
        public const int Plus = 1;

        /// <summary>A negative atom parity.</summary>
        public const int Minus = -1;

        /// <summary>A undefined atom parity.</summary>
        public const int Undefined = 0;
    }

    public static class CDKPropertyName
    {
        /// <summary>
        /// Carbon NMR shift constant for use as a key in the
        /// <see cref="IChemObject"/>.physicalProperties hashtable.
        /// </summary>
        /// <seealso cref="NCDK.Default.ChemObject"/>
        public const string NMRShiftCarbon = "carbon nmr shift";

        /// <summary>
        /// Hydrogen NMR shift constant for use as a key in the
        /// <see cref="IChemObject"/>.physicalProperties hashtable.
        /// </summary>
        /// <seealso cref="NCDK.Default.ChemObject"/>
        public const string NMRShiftHydrogen = "hydrogen nmr shift";

        /// <summary>
        /// Nitrogen NMR shift constant for use as a key in the
        /// <see cref="IChemObject"/>.physicalProperties hashtable.
        /// </summary>
        /// <seealso cref="NCDK.Default.ChemObject"/>
        public const string NMRShiftNitrogen = "nitrogen nmr shift";

        /// <summary>Phosphorus NMR shift constant for use as a key in the
        /// <see cref="IChemObject"/>.physicalProperties hashtable.</summary>
        /// <seealso cref="NCDK.Default.ChemObject"/>
        public const string NMRShiftPhosphorus = "phosphorus nmr shift";

        /// <summary> Fluorine NMR shift constant for use as a key in the
        /// <see cref="IChemObject"/>.physicalProperties hashtable.</summary>
        /// <seealso cref="NCDK.Default.ChemObject"/>
        public const string NMRShiftFluorine = "fluorine nmr shift";

        /// <summary>Deuterium NMR shift constant for use as a key in the
        /// <see cref="IChemObject"/>.physicalProperties hashtable.</summary>
        /// <seealso cref="NCDK.Default.ChemObject"/>
        public const string NMRShiftDeuterium = "deuterium nmr shift";

        /// <summary>
        /// Sulfur NMR shift constant for use as a key in the 
        /// <see cref="IChemObject"/>'a physical properties hashtable.
        /// </summary>
        public const string NMRShiftSulfer = "sulfur nmr shift";

        /// <summary>
        /// NMR signal multiplicity constant for use as a key in the IChemObject.physicalProperties hashtable.
        /// </summary>
        public const string NMRSignalMultiplicity = "nmr signal multiplicity";

        /// <summary>
        /// NMR signal intensity constant for use as a key in the IChemObject.physicalProperties hashtable.
        /// </summary>
        public const string NMRSignalIntensity = "nmr signal intensity";

        /// <summary>
        /// NMR spectrum type constant for use as key for an arbitrary 1-dimensional
        /// NMR experiment.
        /// </summary>
        public const string NMRSpecType1D = "1D NMR spectrum";

        /// <summary>
        /// NMR spectrum type constant for use as key for a 1-dimensional DEPT90
        /// NMR experiment.
        /// </summary>
        public const string NMRSpecType1DDEPT90 = "1D NMR spectrum: DEPT90";

        /// <summary>
        /// NMR spectrum type constant for use as key for a 1-dimensional DEPT135
        /// NMR experiment.
        /// </summary>
        public const string NMRSpecType1DDEPT135 = "1D NMR spectrum: DEPT135";

        /// <summary>
        /// NMR spectrum type constant for use as key for a 2-dimensional HSQC
        /// NMR experiment.
        /// </summary>
        public const string NMRSpecType2DHSQC = "2D NMR spectrum: HSQC";

        /// <summary>
        /// NMR spectrum type constant for use as key for a 2-dimensional H,H-COSY
        /// NMR experiment.
        /// </summary>
        public const string NMRSpecType2DHHCOSY = "2D NMR spectrum: HHCOSY";

        /// <summary>
        /// NMR spectrum type constant for use as key for a 2-dimensional INADEQUATE
        /// NMR experiment.
        /// </summary>
        public const string NMRSpecType2DINADEQUATE = "2D NMR spectrum: INADEQUATE";

        /// <summary>
        /// NMR spectrum type constant for use as key for a 2-dimensional HMBC
        /// NMR experiment.
        /// </summary>
        public const string NMRSpecType2DHMBC = "2D NMR spectrum: HMBC";

        // **************************************
        // Some predefined property names for * ChemObjects *
        // **************************************

        /// <summary>The title for a <see cref="IChemObject"/>.</summary>
        public const string Title = "cdk:Title";

        /// <summary>A remark for a <see cref="IChemObject"/>.</summary>
        public const string Remark = "cdk:Remark";

        /// <summary>A string comment.</summary>
        public const string Comment = "cdk:Comment";

        /// <summary>A List of names.</summary>
        public const string Names = "cdk:Names";

        /// <summary>A List of annotation remarks.</summary>
        public const string Annotations = "cdk:Annotations";

        /// <summary>A description for a <see cref="IChemObject"/>.</summary>
        public const string Description = "cdk:Description";

        // **************************************
        // Some predefined property names for * Molecules *
        // **************************************

        /// <summary>The Daylight SMILES.</summary>
        public const string SMILES = "cdk:SMILES";

        /// <summary>The IUPAC International Chemical Identifier.</summary>
        public const string InChI = "cdk:InChI";

        /// <summary>The Molecular Formula Identifier.</summary>
        public const string Formula = "cdk:Formula";

        /// <summary>The IUPAC compatible name generated with AutoNom.</summary>
        public const string AutoNomName = "cdk:AutonomName";

        /// <summary>The Beilstein Registry Number.</summary>
        public const string BeilsteinRN = "cdk:BeilsteinRN";

        /// <summary>The CAS Registry Number.</summary>
        public const string CasRN = "cdk:CasRN";

        /// <summary>A set of all rings computed for this molecule.</summary>
        public const string AllRings = "cdk:AllRings";

        /// <summary>A smallest set of smallest rings computed for this molecule.</summary>
        public const string SmallestRings = "cdk:SmallestRings";

        /// <summary>The essential rings computed for this molecule.
        ///  The concept of Essential Rings is defined in
        ///  SSSRFinder
        /// </summary>
        public const string EssentialRings = "cdk:EssentialRings";

        /// <summary>
        /// The relevant rings computed for this molecule.
        /// The concept of relevant rings is defined in SSSRFinder.
        /// </summary>
        public const string RelevantRings = "cdk:RelevantRings";

        /// <summary>
        /// Property used for reactions when converted to/from molecules. It defines what role and atom
        /// has an a reaction.
        /// Used in <see cref="NCDK.Tools.Manipulator.ReactionManipulator.ToMolecule(IReaction)"/> and <see cref="NCDK.Tools.Manipulator.ReactionManipulator.ToReaction(IAtomContainer)"/>.
        /// </summary>
        public const string ReactionRole = "cdk:ReactionRole";

        /// <summary>
        /// Property used for reactions when converted to/from molecules. It defines fragment grouping, for example
        /// when handling ionic components.
        /// 
        /// Used in. ReactionManipulator.toMolecule and ReactionManipulator.toReaction.
        /// </summary>
        public const string ReactionGroup = "cdk:ReactionGroup";

        // **************************************
        // Some predefined property names for * Atoms *
        // **************************************

        /// <summary>
        /// This property will contain an List of Integers. Each
        /// element of the list indicates the size of the ring the given
        /// atom belongs to (if it is a ring atom at all).
        /// </summary>
        public const string RingSizes = "cdk:RingSizes";

        /// <summary>
        /// This property indicates how many ring bonds are connected to
        /// the given atom.
        /// </summary>
        public const string RingConnections = "cdk:RingConnections";

        /// <summary>
        /// This property indicate how many bond are present on the atom.
        /// </summary>
        public const string TotalConnections = "cdk:TotalConnections";

        /// <summary>
        /// Hydrogen count
        /// </summary>
        public const string TotalHCount = "cdk:TotalHydrogenCount";

        /// <summary>The Isotropic Shielding, usually calculated by
        /// a quantum chemistry program like Gaussian.
        /// This is a property used for calculating NMR chemical
        /// shifts by subtracting the value from the
        /// isotropic shielding value of a standard (e.g. TMS).
        /// </summary>
        public const string IsotropicShielding = "cdk:IsotropicShielding";

        /// <summary>
        /// A property to indicate IsRestH being true or false. IsRestH is a term
        /// used in RGroup queries: "if this property is applied ('on'), sites labelled
        /// with Rgroup rrr may only be substituted with a member of the Rgroup or with H"
        /// </summary>
        public const string RestH = "cdk:IsRestH";

        public const string AtomAtomMapping = "cdk:AtomAtomMapping";

        /// <summary>
        /// Atom number/label that can be applied using the Manual Numbering 
        /// Tool in ACD/ChemSketch.
        /// </summary>
        public const string ACDLabsAtomLabel = "cdk:ACDLabsAtomLabel";

        /// <summary>
        /// Property for reaction objects where the conditions of reactions can be placed.
        /// </summary>
        public const string ReactionConditions = "cdk:ReactionConditions";

        // **************************************
        // Some predefined property names for * AtomTypes *
        // **************************************

        /// <summary>Used as property key for indicating the ring size of a certain atom type.</summary>
        public const string PartOfRingOfSize = "cdk:Part of ring of size";

        /// <summary>Used as property key for indicating the chemical group of a certain atom type.</summary>
        public const string ChemicalGroupConstant = "cdk:Chemical Group";

        /// <summary>Used as property key for indicating the HOSE code for a certain atom type.</summary>
        public const string SphericalMatcher = "cdk:HOSE code spherical matcher";

        /// <summary>Used as property key for indicating the HOSE code for a certain atom type.</summary>
        public const string PiBondCount = "cdk:Pi Bond Count";

        /// <summary>Used as property key for indicating the HOSE code for a certain atom type.</summary>
        public const string LonePairCount = "cdk:Lone Pair Count";

        /// <summary>Used as property key for indicating the number of single electrons on the atom type.</summary>
        public const string SingleElectronCount = "cdk:Single Electron Count";

        /// <summary>pack the RGB color space components into a single int.</summary>
        public const string Color = "org.openscience.cdk.renderer.color";

        internal static int RGB2Int(int r, int g, int b) => (((r << 16) & 0xff0000) | ((g << 8) & 0x00ff00) | (b & 0x0000ff));
    }
}
