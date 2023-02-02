/* Copyright (C) 2006-2007  Sam Adams <sea36@users.sf.net>
 *
 * Contact: cdk-devel@lists.sourceforge.net
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public License
 * as published by the Free Software Foundation; either version 2.1
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Config;
using NCDK.Stereo;
using NCDK.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// This class generates a CDK IAtomContainer from an InChI string.  It places
    /// calls to a JNI wrapper for the InChI C++ library.
    /// <para>
    /// The generated IAtomContainer will have all 2D and 3D coordinates set to 0.0,
    /// but may have atom parities set.  Double bond and allene stereochemistry are
    /// not currently recorded.
    /// </para>
    /// </summary>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.InChI.InChIToStructure_Example.cs"]/*' />
    /// </example>
    // @author Sam Adams
    // @cdk.module inchi
    public class InChIToStructure
    {
        internal NInchiInputInchi input;
        internal NInchiOutputStructure output;

        // magic number - indicates isotope mass is relative
        private const int ISOTOPIC_SHIFT_FLAG = 10000;

        /// <summary>
        /// Constructor. Generates CDK AtomContainer from InChI.
        /// </summary>
        /// <param name="inchi"></param>
        /// <param name="builder"></param>
        internal InChIToStructure(string inchi, IChemObjectBuilder builder)
        {
            try
            {
                input = new NInchiInputInchi(inchi, "");
            }
            catch (NInchiException jie)
            {
                throw new CDKException("Failed to convert InChI to molecule: " + jie.Message, jie);
            }
            GenerateAtomContainerFromInChI(builder);
        }

        /// <summary>
        /// Constructor. Generates CMLMolecule from InChI.
        /// </summary>
        /// <param name="inchi"></param>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        internal InChIToStructure(string inchi, IChemObjectBuilder builder, string options)
        {
            try
            {
                input = new NInchiInputInchi(inchi, options);
            }
            catch (NInchiException jie)
            {
                throw new CDKException("Failed to convert InChI to molecule: " + jie.Message, jie);
            }
            GenerateAtomContainerFromInChI(builder);
        }

        /// <summary>
        /// Constructor. Generates CMLMolecule from InChI.
        /// </summary>
        /// <param name="inchi"></param>
        /// <param name="builder"></param>
        /// <param name="options"></param>
        internal InChIToStructure(string inchi, IChemObjectBuilder builder, IEnumerable<string> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            try
            {
                input = new NInchiInputInchi(inchi, options.Select(n => InChIOption.ValueOfIgnoreCase(n)));
            }
            catch (NInchiException jie)
            {
                throw new CDKException("Failed to convert InChI to molecule: " + jie.Message);
            }
            GenerateAtomContainerFromInChI(builder);
        }

        /// <summary>
        /// Gets structure from InChI, and converts InChI library data structure into an IAtomContainer.
        /// </summary>
        /// <param name="builder"></param>
        private void GenerateAtomContainerFromInChI(IChemObjectBuilder builder)
        {
            try
            {
                output = NInchiWrapper.GetStructureFromInchi(input);
            }
            catch (NInchiException jie)
            {
                throw new CDKException("Failed to convert InChI to molecule: " + jie.Message, jie);
            }

            //molecule = new AtomContainer();
            AtomContainer = builder.NewAtomContainer();

            var inchiCdkAtomMap = new Dictionary<NInchiAtom, IAtom>();

            for (int i = 0; i < output.Atoms.Count; i++)
            {
                var iAt = output.Atoms[i];
                var cAt = builder.NewAtom();

                inchiCdkAtomMap[iAt] = cAt;

                cAt.Id = "a" + i;
                cAt.Symbol = iAt.ElementType;
                cAt.AtomicNumber = PeriodicTable.GetAtomicNumber(cAt.Symbol);

                // Ignore coordinates - all zero - unless aux info was given... but
                // the CDK doesn't have an API to provide that

                // InChI does not have unset properties so we set charge,
                // hydrogen count (implicit) and isotopic mass
                cAt.FormalCharge = iAt.Charge;
                cAt.ImplicitHydrogenCount = iAt.ImplicitH;

                var isotopicMass = iAt.IsotopicMass;

                if (isotopicMass != 0)
                {
                    if (ISOTOPIC_SHIFT_FLAG == (isotopicMass & ISOTOPIC_SHIFT_FLAG))
                    {
                        try
                        {
                            var massNumber = CDK.IsotopeFactory.GetMajorIsotope(cAt.AtomicNumber).MassNumber.Value;
                            cAt.MassNumber = massNumber + (isotopicMass - ISOTOPIC_SHIFT_FLAG);
                        }
                        catch (IOException e)
                        {
                            throw new CDKException("Could not load Isotopes data", e);
                        }
                    }
                    else
                    {
                        cAt.MassNumber = isotopicMass;
                    }
                }

                AtomContainer.Atoms.Add(cAt);

                cAt = AtomContainer.Atoms[AtomContainer.Atoms.Count - 1];
                for (int j = 0; j < iAt.ImplicitDeuterium; j++)
                {
                    var deut = builder.NewAtom();
                    deut.AtomicNumber = 1;
                    deut.Symbol = "H";
                    deut.MassNumber = 2;
                    deut.ImplicitHydrogenCount = 0;
                    AtomContainer.Atoms.Add(deut);
                    deut = AtomContainer.Atoms[AtomContainer.Atoms.Count - 1];
                    var bond = builder.NewBond(cAt, deut, BondOrder.Single);
                    AtomContainer.Bonds.Add(bond);
                }
                for (int j = 0; j < iAt.ImplicitTritium; j++)
                {
                    var trit = builder.NewAtom();
                    trit.AtomicNumber = 1;
                    trit.Symbol = "H";
                    trit.MassNumber = 3;
                    trit.ImplicitHydrogenCount = 0;
                    AtomContainer.Atoms.Add(trit);
                    trit = AtomContainer.Atoms[AtomContainer.Atoms.Count - 1];
                    var bond = builder.NewBond(cAt, trit, BondOrder.Single);
                    AtomContainer.Bonds.Add(bond);
                }
            }

            for (int i = 0; i < output.Bonds.Count; i++)
            {
                var iBo = output.Bonds[i];

                var atO = inchiCdkAtomMap[iBo.OriginAtom];
                var atT = inchiCdkAtomMap[iBo.TargetAtom];
                var cBo = builder.NewBond(atO, atT);

                var type = iBo.BondType;
                switch (type)
                {
                    case INCHI_BOND_TYPE.Single:
                        cBo.Order = BondOrder.Single;
                        break;
                    case INCHI_BOND_TYPE.Double:
                        cBo.Order = BondOrder.Double;
                        break;
                    case INCHI_BOND_TYPE.Triple:
                        cBo.Order = BondOrder.Triple;
                        break;
                    case INCHI_BOND_TYPE.Altern:
                        cBo.IsAromatic = true;
                        break;
                    default:
                        throw new CDKException("Unknown bond type: " + type);
                }

                var stereo = iBo.BondStereo;
                switch (stereo)
                {
                    // No stereo definition
                    case INCHI_BOND_STEREO.None:
                        cBo.Stereo = BondStereo.None;
                        break;
                    // Bond ending (fat end of wedge) below the plane
                    case INCHI_BOND_STEREO.Single1Down:
                        cBo.Stereo = BondStereo.Down;
                        break;
                    // Bond ending (fat end of wedge) above the plane
                    case INCHI_BOND_STEREO.Single1Up:
                        cBo.Stereo = BondStereo.Up;
                        break;
                    // Bond starting (pointy end of wedge) below the plane
                    case INCHI_BOND_STEREO.Single2Down:
                        cBo.Stereo = BondStereo.DownInverted;
                        break;
                    // Bond starting (pointy end of wedge) above the plane
                    case INCHI_BOND_STEREO.Single2Up:
                        cBo.Stereo = BondStereo.UpInverted;
                        break;
                    // Bond with undefined stereochemistry
                    case INCHI_BOND_STEREO.Single1Either:
                    case INCHI_BOND_STEREO.DoubleEither:
                        cBo.Stereo = BondStereo.None;
                        break;
                }
                AtomContainer.Bonds.Add(cBo);
            }

            for (int i = 0; i < output.Stereos.Count; i++)
            {
                var stereo0d = output.Stereos[i];
                if (stereo0d.StereoType == INCHI_STEREOTYPE.Tetrahedral
                 || stereo0d.StereoType == INCHI_STEREOTYPE.Allene)
                {
                    var central = stereo0d.CentralAtom;
                    var neighbours = stereo0d.Neighbors;

                    var focus = inchiCdkAtomMap[central];
                    var neighbors = new IAtom[]
                    {
                        inchiCdkAtomMap[neighbours[0]],
                        inchiCdkAtomMap[neighbours[1]],
                        inchiCdkAtomMap[neighbours[2]],
                        inchiCdkAtomMap[neighbours[3]]
                    };
                    TetrahedralStereo stereo;

                    // as per JNI InChI doc even is clockwise and odd is
                    // anti-clockwise
                    switch (stereo0d.Parity)
                    {
                        case INCHI_PARITY.Odd:
                            stereo = TetrahedralStereo.AntiClockwise;
                            break;
                        case INCHI_PARITY.Even:
                            stereo = TetrahedralStereo.Clockwise;
                            break;
                        default:
                            // CDK Only supports parities of + or -
                            continue;
                    }

                    IStereoElement<IChemObject, IChemObject> stereoElement = null;

                    switch (stereo0d.StereoType)
                    {
                        case INCHI_STEREOTYPE.Tetrahedral:
                            stereoElement = builder.NewTetrahedralChirality(focus, neighbors, stereo);
                            break;
                        case INCHI_STEREOTYPE.Allene:
                            {
                                // The periphals (p<i>) and terminals (t<i>) are refering to
                                // the following atoms. The focus (f) is also shown.
                                //
                                //   p0          p2
                                //    \          /
                                //     t0 = f = t1
                                //    /         \
                                //   p1         p3
                                var peripherals = neighbors;
                                var terminals = ExtendedTetrahedral.FindTerminalAtoms(AtomContainer, focus);

                                // InChI always provides the terminal atoms t0 and t1 as
                                // periphals, here we find where they are and then add in
                                // the other explicit atom. As the InChI create hydrogens
                                // for stereo elements, there will always we an explicit
                                // atom that can be found - it may be optionally suppressed
                                // later.

                                // not much documentation on this (at all) but they appear
                                // to always be the middle two atoms (index 1, 2) we therefore
                                // test these first - but handle the other indices just in
                                // case
                                foreach (var terminal in terminals)
                                {
                                    if (peripherals[1].Equals(terminal))
                                    {
                                        peripherals[1] = FindOtherSinglyBonded(AtomContainer, terminal, peripherals[0]);
                                    }
                                    else if (peripherals[2].Equals(terminal))
                                    {
                                        peripherals[2] = FindOtherSinglyBonded(AtomContainer, terminal, peripherals[3]);
                                    }
                                    else if (peripherals[0].Equals(terminal))
                                    {
                                        peripherals[0] = FindOtherSinglyBonded(AtomContainer, terminal, peripherals[1]);
                                    }
                                    else if (peripherals[3].Equals(terminal))
                                    {
                                        peripherals[3] = FindOtherSinglyBonded(AtomContainer, terminal, peripherals[2]);
                                    }
                                }

                                stereoElement = new ExtendedTetrahedral(focus, peripherals, stereo);
                            }
                            break;
                    }

                    Trace.Assert(stereoElement != null);
                    AtomContainer.StereoElements.Add(stereoElement);
                }
                else if (stereo0d.StereoType == INCHI_STEREOTYPE.DoubleBond)
                {
                    NInchiAtom[] neighbors = stereo0d.Neighbors;

                    // from JNI InChI doc
                    //                            neighbor[4]  : {#X,#A,#B,#Y} in this order
                    // X                          central_atom : NO_ATOM
                    //  \            X        Y   type         : INCHI_StereoType_DoubleBond
                    //   A == B       \      /
                    //         \       A == B
                    //          Y
                    var x = inchiCdkAtomMap[neighbors[0]];
                    var a = inchiCdkAtomMap[neighbors[1]];
                    var b = inchiCdkAtomMap[neighbors[2]];
                    var y = inchiCdkAtomMap[neighbors[3]];

                    // XXX: AtomContainer is doing slow lookup
                    var stereoBond = AtomContainer.GetBond(a, b);
                    stereoBond.SetAtoms(new[] { a, b }); // ensure a is first atom

                    var conformation = DoubleBondConformation.Unset;

                    switch (stereo0d.Parity)
                    {
                        case INCHI_PARITY.Odd:
                            conformation = DoubleBondConformation.Together;
                            break;
                        case INCHI_PARITY.Even:
                            conformation = DoubleBondConformation.Opposite;
                            break;
                    }

                    // unspecified not stored
                    if (conformation.IsUnset()) continue;

                    AtomContainer.StereoElements.Add(new DoubleBondStereochemistry(stereoBond, new IBond[]{AtomContainer.GetBond(x, a),
                            AtomContainer.GetBond(b, y)}, conformation));
                }
                else
                {
                    // TODO - other types of atom parity - double bond, etc
                }
            }
        }

        /// <summary>
        /// Finds a neighbor attached to 'atom' that is singley bonded and isn't 'exclude'. If no such atom exists, the 'atom' is returned.
        /// </summary>
        /// <param name="container">a molecule container</param>
        /// <param name="atom">the atom to find the neighbor or</param>
        /// <param name="exclude">don't find this atom</param>
        /// <returns>the other atom (or 'atom')</returns>
        private static IAtom FindOtherSinglyBonded(IAtomContainer container, IAtom atom, IAtom exclude)
        {
            foreach (var bond in container.GetConnectedBonds(atom))
            {
                if (!BondOrder.Single.Equals(bond.Order) || bond.Contains(exclude))
                    continue;
                return bond.GetOther(atom);
            }
            return atom;
        }

        /// <summary>
        /// Generated molecule.
        /// </summary>
        public IAtomContainer AtomContainer { get; private set; }

        /// <summary>
        /// Return status from InChI process.  OKAY and WARNING indicate
        /// InChI has been generated, in all other cases InChI generation has failed.
        /// </summary>
        public InChIReturnCode ReturnStatus => output.ReturnStatus;

        /// <summary>
        /// Generated (error/warning) messages.
        /// </summary>
        public string Message => output.Message;

        /// <summary>
        /// Generated log.
        /// </summary>
        public string Log => output.Log;

        /// <summary>
        /// Returns warning flags, see INCHIDIFF in inchicmp.h.
        /// </summary>
        /// <remarks>
        /// [x * 2 + y]:
        /// <list type="bullet">
        /// <item>x=0 =&gt; Reconnected if present in InChI otherwise Disconnected/Normal</item>
        /// <item>x=1 =&gt; Disconnected layer if Reconnected layer is present</item>
        /// <item>y=1 =&gt; Main layer or Mobile-H</item>
        /// <item>y=0 =&gt; Fixed-H layer</item>
        /// </list>
        /// </remarks>
        public IReadOnlyList<ulong> WarningFlags => output.WarningFlags;

        public static InChIToStructure FromInChI(string inchi)
        {
            return FromInChI(inchi, CDK.Builder);
        }

        /// <summary>
        /// Gets structure generator for an InChI string.
        /// </summary>
        /// <param name="inchi">InChI to generate structure from.</param>
        /// <param name="builder">the builder to use</param>
        /// <returns>the InChI structure generator object</returns>
        /// <exception cref="CDKException">if the generator cannot be instantiated</exception>
        public static InChIToStructure FromInChI(string inchi, IChemObjectBuilder builder)
        {
            return new InChIToStructure(inchi, builder);
        }

        /// <summary>
        /// Gets structure generator for an InChI string.
        /// </summary>
        /// <param name="inchi">InChI to generate structure from.</param>
        /// <param name="builder">the builder to employ</param>
        /// <param name="options">string of options for structure generation.</param>
        /// <returns>the InChI structure generator object</returns>
        /// <exception cref="CDKException">if the generator cannot be instantiated</exception>
        public static InChIToStructure FromInChI(string inchi, IChemObjectBuilder builder, string options)
        {
            return new InChIToStructure(inchi, builder, options);
        }

        /// <summary>
        /// Gets structure generator for an InChI string.
        /// </summary>
        /// <param name="inchi">InChI to generate structure from.</param>
        /// <param name="options">List of options (net.sf.jniinchi.INCHI_OPTION) for structure generation.</param>
        /// <param name="builder">the builder to employ</param>
        /// <returns>the InChI structure generator object</returns>
        /// <exception cref="CDKException">if the generator cannot be instantiated</exception>
        public static InChIToStructure FromInChI(string inchi, IChemObjectBuilder builder, IEnumerable<string> options)
        {
            return new InChIToStructure(inchi, builder, options);
        }
    }
}
