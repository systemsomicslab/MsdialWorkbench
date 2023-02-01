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

using NCDK.Stereo;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace NCDK.Graphs.InChI
{
    /// <summary>
    /// This class generates the IUPAC International Chemical Identifier (InChI) for
    /// a CDK IAtomContainer. It places calls to a JNI wrapper for the InChI C++ library.
    /// </summary>
    /// <remarks><para>If the atom container has 3D coordinates for all of its atoms then they
    /// will be used, otherwise 2D coordinates will be used if available.</para>
    /// <para>Spin multiplicities and some aspects of stereochemistry are not
    /// currently handled completely.</para>
    /// </remarks>
    /// <example>
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Graphs.InChI.InChIGenerator_Example.cs"]/*' />
    /// </example>
    /// TODO: distinguish between singlet and undefined spin multiplicity<br/>
    /// TODO: double bond and allene parities<br/>
    /// TODO: problem recognising bond stereochemistry<br/>
    // @author Sam Adams
    // @cdk.module inchi
    public class InChIGenerator
    {
        internal NInchiInput Input { get; set; }
        internal NInchiOutput Output { get; set; }
        private readonly bool auxNone;

        /// <summary>
        /// AtomContainer instance refers to.
        /// </summary>
        protected IAtomContainer ReferringAtomContainer { get; set; }

        /// <summary>
        /// Constructor. Generates InChI from CDK AtomContainer.
        /// <para>Reads atoms, bonds etc from atom container and converts to format
        /// InChI library requires, then calls the library.</para>
        /// </summary>
        /// <param name="atomContainer">AtomContainer to generate InChI for.</param>
        /// <param name="ignoreAromaticBonds">if aromatic bonds should be treated as bonds of type single and double</param>
        /// <exception cref="CDKException">if there is an error during InChI generation</exception>
        protected internal InChIGenerator(IAtomContainer atomContainer, bool ignoreAromaticBonds)
            : this(atomContainer, new[] { InChIOption.AuxNone }, ignoreAromaticBonds)
        { }

        /// <summary>
        /// Constructor. Generates InChI from CDK AtomContainer.
        /// <para>Reads atoms, bonds etc from atom container and converts to format
        /// InChI library requires, then calls the library.</para>
        /// </summary>
        /// <param name="atomContainer">AtomContainer to generate InChI for.</param>
        /// <param name="options">Space delimited string of options to pass to InChI library.
        ///     Each option may optionally be preceded by a command line switch (/ or -).</param>
        /// <param name="ignoreAromaticBonds">if aromatic bonds should be treated as bonds of type single and double</param>
        protected internal InChIGenerator(IAtomContainer atomContainer, string options, bool ignoreAromaticBonds)
        {
            try
            {
                Input = new NInChIInputAdapter(options);
                GenerateInChIFromCDKAtomContainer(atomContainer, ignoreAromaticBonds);
                auxNone = Input.Options != null && Input.Options.Contains("AuxNone");
            }
            catch (NInchiException jie)
            {
                throw new CDKException("InChI generation failed: " + jie.Message, jie);
            }
        }

        /// <summary>
        /// Constructor. Generates InChI from CDK AtomContainer.
        /// <para>Reads atoms, bonds etc from atom container and converts to format
        /// InChI library requires, then calls the library.</para>
        /// </summary>
        /// <param name="atomContainer">AtomContainer to generate InChI for.</param>
        /// <param name="options">List of INCHI_OPTION.</param>
        /// <param name="ignoreAromaticBonds">if aromatic bonds should be treated as bonds of type single and double</param>
        internal InChIGenerator(IAtomContainer atomContainer, IEnumerable<InChIOption> options, bool ignoreAromaticBonds)
        {
            try
            {
                Input = new NInChIInputAdapter(new List<InChIOption>(options));
                GenerateInChIFromCDKAtomContainer(atomContainer, ignoreAromaticBonds);
                auxNone = Input.Options != null && Input.Options.Contains("AuxNone");
            }
            catch (NInchiException jie)
            {
                throw new CDKException("InChI generation failed: " + jie.Message, jie);
            }
        }

        /// <summary>
        /// Reads atoms, bonds etc from atom container and converts to format
        /// InChI library requires, then places call for the library to generate
        /// the InChI.
        /// </summary>
        /// <param name="atomContainer">AtomContainer to generate InChI for.</param>
        /// <param name="ignore"></param>
        private void GenerateInChIFromCDKAtomContainer(IAtomContainer atomContainer, bool ignore)
        {
            this.ReferringAtomContainer = atomContainer;

            // Check for 3d coordinates
            bool all3d = true;
            bool all2d = true;
            foreach (var atom in atomContainer.Atoms)
            {
                if (all3d && atom.Point3D == null)
                {
                    all3d = false;
                }
                if (all2d && atom.Point2D == null)
                {
                    all2d = false;
                }
            }

            var atomMap = new Dictionary<IAtom, NInchiAtom>();
            foreach (var atom in atomContainer.Atoms)
            {
                // Get coordinates
                // Use 3d if possible, otherwise 2d or none
                double x, y, z;
                if (all3d)
                {
                    var p = atom.Point3D.Value;
                    x = p.X;
                    y = p.Y;
                    z = p.Z;
                }
                else if (all2d)
                {
                    var p = atom.Point2D.Value;
                    x = p.X;
                    y = p.Y;
                    z = 0.0;
                }
                else
                {
                    x = 0.0;
                    y = 0.0;
                    z = 0.0;
                }

                // Chemical element symbol
                var el = atom.Symbol;

                // Generate InChI atom
                var iatom = Input.Add(new NInchiAtom(x, y, z, el));
                atomMap[atom] = iatom;

                // Check if charged
                var charge = atom.FormalCharge.Value;
                if (charge != 0)
                {
                    iatom.Charge = charge;
                }

                // Check whether isotopic
                var isotopeNumber = atom.MassNumber;
                if (isotopeNumber != null)
                {
                    iatom.IsotopicMass = isotopeNumber.Value;
                }

                // Check for implicit hydrogens
                // atom.HydrogenCount returns number of implicit hydrogens, not
                // total number
                // Ref: Posting to cdk-devel list by Egon Willighagen 2005-09-17
                int? implicitH = atom.ImplicitHydrogenCount;

                // set implicit hydrogen count, -1 tells the inchi to determine it
                iatom.ImplicitH = implicitH ?? -1;

                // Check if radical
                int count = atomContainer.GetConnectedSingleElectrons(atom).Count();
                if (count == 0)
                {
                    // TODO - how to check whether singlet or undefined multiplicity
                }
                else if (count == 1)
                {
                    iatom.Radical = INCHI_RADICAL.Doublet;
                }
                else if (count == 2)
                {
                    iatom.Radical = INCHI_RADICAL.Triplet;
                }
                else
                {
                    throw new CDKException("Unrecognised radical type");
                }
            }

            // Process bonds
            var bondMap = new Dictionary<IBond, NInchiBond>();
            foreach (var bond in atomContainer.Bonds)
            {
                // Assumes 2 centre bond
                var at0 = atomMap[bond.Begin];
                var at1 = atomMap[bond.End];

                // Get bond order
                INCHI_BOND_TYPE order;
                var bo = bond.Order;
                if (!ignore && bond.IsAromatic)
                {
                    order = INCHI_BOND_TYPE.Altern;
                }
                else if (bo == BondOrder.Single)
                {
                    order = INCHI_BOND_TYPE.Single;
                }
                else if (bo == BondOrder.Double)
                {
                    order = INCHI_BOND_TYPE.Double;
                }
                else if (bo == BondOrder.Triple)
                {
                    order = INCHI_BOND_TYPE.Triple;
                }
                else
                {
                    throw new CDKException("Failed to generate InChI: Unsupported bond type");
                }

                // Create InChI bond
                var ibond = new NInchiBond(at0, at1, order);
                Input.Add(ibond);

                // Check for bond stereo definitions
                var stereo = bond.Stereo;
                // No stereo definition
                if (stereo == BondStereo.None)
                {
                    ibond.BondStereo = INCHI_BOND_STEREO.None;
                }
                // Bond ending (fat end of wedge) below the plane
                else if (stereo == BondStereo.Down)
                {
                    ibond.BondStereo = INCHI_BOND_STEREO.Single1Down;
                }
                // Bond ending (fat end of wedge) above the plane
                else if (stereo == BondStereo.Up)
                {
                    ibond.BondStereo = INCHI_BOND_STEREO.Single1Up;
                }
                // Bond starting (pointy end of wedge) below the plane
                else if (stereo == BondStereo.DownInverted)
                {
                    ibond.BondStereo = INCHI_BOND_STEREO.Single2Down;
                }
                // Bond starting (pointy end of wedge) above the plane
                else if (stereo == BondStereo.UpInverted)
                {
                    ibond.BondStereo = INCHI_BOND_STEREO.Single2Up;
                }
                else if (stereo == BondStereo.EOrZ)
                {
                    ibond.BondStereo = INCHI_BOND_STEREO.DoubleEither;
                }
                else if (stereo == BondStereo.UpOrDown)
                {
                    ibond.BondStereo = INCHI_BOND_STEREO.Single1Either;
                }
                else if (stereo == BondStereo.UpOrDownInverted)
                {
                    ibond.BondStereo = INCHI_BOND_STEREO.Single2Either;
                }
                // Bond with undefined stereochemistry
                else if (stereo == BondStereo.None)
                {
                    if (order == INCHI_BOND_TYPE.Single)
                    {
                        ibond.BondStereo = INCHI_BOND_STEREO.Single1Either;
                    }
                    else if (order == INCHI_BOND_TYPE.Double)
                    {
                        ibond.BondStereo = INCHI_BOND_STEREO.DoubleEither;
                    }
                }
            }

            // Process tetrahedral stereo elements
            foreach (var stereoElem in atomContainer.StereoElements)
            {
                if (stereoElem is ITetrahedralChirality chirality)
                {
                    var stereoType = chirality.Stereo;

                    var atC = atomMap[chirality.ChiralAtom];
                    var at0 = atomMap[chirality.Ligands[0]];
                    var at1 = atomMap[chirality.Ligands[1]];
                    var at2 = atomMap[chirality.Ligands[2]];
                    var at3 = atomMap[chirality.Ligands[3]];
                    var p = INCHI_PARITY.Unknown;
                    if (stereoType == TetrahedralStereo.AntiClockwise)
                    {
                        p = INCHI_PARITY.Odd;
                    }
                    else if (stereoType == TetrahedralStereo.Clockwise)
                    {
                        p = INCHI_PARITY.Even;
                    }
                    else
                    {
                        throw new CDKException("Unknown tetrahedral chirality");
                    }

                    var jniStereo = new NInchiStereo0D(atC, at0, at1, at2, at3, INCHI_STEREOTYPE.Tetrahedral, p);
                    Input.Stereos.Add(jniStereo);
                }
                else if (stereoElem is IDoubleBondStereochemistry dbStereo)
                {
                    var surroundingBonds = dbStereo.Bonds;
                    if (surroundingBonds[0] == null || surroundingBonds[1] == null)
                        throw new CDKException("Cannot generate an InChI with incomplete double bond info");
                    var stereoType = dbStereo.Stereo;

                    IBond stereoBond = dbStereo.StereoBond;
                    NInchiAtom at0 = null;
                    NInchiAtom at1 = null;
                    NInchiAtom at2 = null;
                    NInchiAtom at3 = null;
                    // TODO: I should check for two atom bonds... or maybe that should happen when you
                    //    create a double bond stereochemistry
                    if (stereoBond.Contains(surroundingBonds[0].Begin))
                    {
                        // first atom is A
                        at1 = atomMap[surroundingBonds[0].Begin];
                        at0 = atomMap[surroundingBonds[0].End];
                    }
                    else
                    {
                        // first atom is X
                        at0 = atomMap[surroundingBonds[0].Begin];
                        at1 = atomMap[surroundingBonds[0].End];
                    }
                    if (stereoBond.Contains(surroundingBonds[1].Begin))
                    {
                        // first atom is B
                        at2 = atomMap[surroundingBonds[1].Begin];
                        at3 = atomMap[surroundingBonds[1].End];
                    }
                    else
                    {
                        // first atom is Y
                        at2 = atomMap[surroundingBonds[1].End];
                        at3 = atomMap[surroundingBonds[1].Begin];
                    }
                    var p = INCHI_PARITY.Unknown;
                    if (stereoType == DoubleBondConformation.Together)
                    {
                        p = INCHI_PARITY.Odd;
                    }
                    else if (stereoType == DoubleBondConformation.Opposite)
                    {
                        p = INCHI_PARITY.Even;
                    }
                    else
                    {
                        throw new CDKException("Unknown double bond stereochemistry");
                    }

                    var jniStereo = new NInchiStereo0D(null, at0, at1, at2, at3, INCHI_STEREOTYPE.DoubleBond, p);
                    Input.Stereos.Add(jniStereo);
                }
                else if (stereoElem is ExtendedTetrahedral extendedTetrahedral)
                {
                    TetrahedralStereo winding = extendedTetrahedral.Winding;

                    // The peripherals (p<i>) and terminals (t<i>) are referring to
                    // the following atoms. The focus (f) is also shown.
                    //
                    //   p0          p2
                    //    \          /
                    //     t0 = f = t1
                    //    /         \
                    //   p1         p3
                    var terminals = extendedTetrahedral.FindTerminalAtoms(atomContainer);
                    var peripherals = extendedTetrahedral.Peripherals.ToArray();

                    // InChI API is particular about the input, each terminal atom
                    // needs to be present in the list of neighbors and they must
                    // be at index 1 and 2 (i.e. in the middle). This is true even
                    // of explicit atoms. For the implicit atoms, the terminals may
                    // be in the peripherals already and so we correct the winding
                    // and reposition as needed.

                    var t0Bonds = OnlySingleBonded(atomContainer.GetConnectedBonds(terminals[0]));
                    var t1Bonds = OnlySingleBonded(atomContainer.GetConnectedBonds(terminals[1]));

                    // first if there are two explicit atoms we need to replace one
                    // with the terminal atom - the configuration does not change
                    if (t0Bonds.Count == 2)
                    {
                        var orgBond = t0Bonds[0];
                        t0Bonds.RemoveAt(0);
                        var replace = orgBond.GetOther(terminals[0]);
                        for (int i = 0; i < peripherals.Length; i++)
                            if (replace == peripherals[i])
                                peripherals[i] = terminals[0];
                    }

                    if (t1Bonds.Count == 2)
                    {
                        var orgBond = t0Bonds[0];
                        t1Bonds.RemoveAt(0);
                        var replace = orgBond.GetOther(terminals[1]);
                        for (int i = 0; i < peripherals.Length; i++)
                            if (replace == peripherals[i])
                                peripherals[i] = terminals[1];
                    }

                    // the neighbor attached to each terminal atom that we will
                    // define the configuration of
                    var t0Neighbor = t0Bonds[0].GetOther(terminals[0]);
                    var t1Neighbor = t1Bonds[0].GetOther(terminals[1]);

                    // we now need to move all the atoms into the correct positions
                    // everytime we exchange atoms the configuration inverts
                    for (int i = 0; i < peripherals.Length; i++)
                    {
                        if (i != 0 && t0Neighbor == peripherals[i])
                        {
                            Swap(peripherals, i, 0);
                            winding = winding.Invert();
                        }
                        else if (i != 1 && terminals[0] == peripherals[i])
                        {
                            Swap(peripherals, i, 1);
                            winding = winding.Invert();
                        }
                        else if (i != 2 && terminals[1] == peripherals[i])
                        {
                            Swap(peripherals, i, 2);
                            winding = winding.Invert();
                        }
                        else if (i != 3 && t1Neighbor == peripherals[i])
                        {
                            Swap(peripherals, i, 3);
                            winding = winding.Invert();
                        }
                    }

                    var parity = INCHI_PARITY.Unknown;
                    if (winding == TetrahedralStereo.AntiClockwise)
                        parity = INCHI_PARITY.Odd;
                    else if (winding == TetrahedralStereo.Clockwise)
                        parity = INCHI_PARITY.Even;
                    else
                        throw new CDKException("Unknown extended tetrahedral chirality");

                    NInchiStereo0D jniStereo = new NInchiStereo0D(atomMap[extendedTetrahedral.Focus],
                            atomMap[peripherals[0]], atomMap[peripherals[1]], atomMap[peripherals[2]],
                            atomMap[peripherals[3]], INCHI_STEREOTYPE.Allene, parity);
                    Input.Stereos.Add(jniStereo);
                }
            }

            try
            {
                Output = NInchiWrapper.GetInchi(Input);
            }
            catch (NInchiException jie)
            {
                throw new CDKException("Failed to generate InChI: " + jie.Message, jie);
            }
        }

        private static List<IBond> OnlySingleBonded(IEnumerable<IBond> bonds)
        {
            var filtered = new List<IBond>();
            foreach (var bond in bonds)
            {
                if (bond.Order == BondOrder.Single)
                    filtered.Add(bond);
            }
            return filtered;
        }

        private static void Swap(object[] objs, int i, int j)
        {
            var tmp = objs[i];
            objs[i] = objs[j];
            objs[j] = tmp;
        }

        /// <summary>
        /// Gets return status from InChI process. <see cref="InChIReturnCode.Ok"/> and <see cref="InChIReturnCode.Warning"/> indicate
        /// InChI has been generated, in all other cases InChI generation
        /// has failed.
        /// </summary>
        public InChIReturnCode ReturnStatus => Output.ReturnStatus;

        /// <summary>
        /// Gets generated InChI string.
        /// </summary>
        public string InChI => Output.InChI;

        /// <summary>
        /// Gets generated InChIKey string.
        /// </summary>
        public string GetInChIKey()
        {
            try
            {
                var key = NInchiWrapper.GetInchiKey(Output.InChI);
                if (key.ReturnStatus == INCHI_KEY.OK)
                {
                    return key.Key;
                }
                else
                {
                    throw new CDKException("Error while creating InChIKey: " + key.ReturnStatus);
                }
            }
            catch (NInchiException exception)
            {
                throw new CDKException("Error while creating InChIKey: " + exception.Message, exception);
            }
        }

        /// <summary>
        /// Gets auxiliary information.
        /// </summary>
        public string AuxInfo
        {
            get
            {
                if (auxNone)
                {
                    Trace.TraceWarning("AuxInfo requested but AuxNone option is set (default).");
                }
                return (Output.AuxInfo);
            }
        }

        /// <summary>
        /// Gets generated (error/warning) messages.
        /// </summary>
        public string Message => Output.Message;

        /// <summary>
        /// Gets generated log.
        /// </summary>
        public string Log => Output.Log;
    }
}
