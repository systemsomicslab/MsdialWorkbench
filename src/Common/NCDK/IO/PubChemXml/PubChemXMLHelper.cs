/* Copyright (C) 2008  Egon Willighagen <egonw@users.sf.net>
 *               2010  Brian Gilman <gilmanb@gmail.com>
 *
 * Contact: cdk-devel@lists.sourceforge.net
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
 * but WITHOUT Any WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using NCDK.Config;
using NCDK.Numerics;
using System.Collections.Generic;
using System.Globalization;
using System.Xml.Linq;

namespace NCDK.IO.PubChemXml
{
    /// <summary>
    /// Helper class to parse PubChem XML documents.
    /// </summary>
    // @cdk.module io
    // @author       Egon Willighagen <egonw@users.sf.net>
    // @cdk.created  2008-05-05
    internal class PubChemXMLHelper
    {
        private IChemObjectBuilder builder;
        private IsotopeFactory factory;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="builder"></param>
        /// <exception cref="System.IO.IOException">if there is error in getting the <see cref="IsotopeFactory"/></exception>
        public PubChemXMLHelper(IChemObjectBuilder builder)
        {
            this.builder = builder;
            factory = CDK.IsotopeFactory;
        }

        // general elements
        private const string EL_PCCOMPOUND = "PC-Compound";
        private const string EL_PCCOMPOUNDS = "PC-Compounds";
        private const string EL_PCSUBSTANCE = "PC-Substance";
        private const string EL_PCSUBSTANCE_SID = "PC-Substance_sid";
        private const string EL_PCCOMPOUND_ID = "PC-Compound_id";
        private const string EL_PCCOMPOUND_CID = "PC-CompoundType_id_cid";
        private const string EL_PCID_ID = "PC-ID_id";

        // atom block elements
        private const string EL_ATOMBLOCK = "PC-Atoms";
        private const string EL_ATOMSELEMENT = "PC-Atoms_element";
        private const string EL_ATOMSCHARGE = "PC-Atoms_charge";
        private const string EL_ATOMINT = "PC-AtomInt";
        private const string EL_ATOMINT_AID = "PC-AtomInt_aid";
        private const string EL_ATOMINT_VALUE = "PC-AtomInt_value";
        private const string EL_ELEMENT = "PC-Element";

        // coordinate block elements
        private const string EL_COORDINATESBLOCK = "PC-Compound_coords";
        private const string EL_COORDINATES_AID = "PC-Coordinates_aid";
        private const string EL_COORDINATES_AIDE = "PC-Coordinates_aid_E";
        private const string EL_ATOM_CONFORMER = "PC-Conformer";
        private const string EL_ATOM_CONFORMER_X = "PC-Conformer_x";
        private const string EL_ATOM_CONFORMER_XE = "PC-Conformer_x_E";
        private const string EL_ATOM_CONFORMER_Y = "PC-Conformer_y";
        private const string EL_ATOM_CONFORMER_YE = "PC-Conformer_y_E";
        private const string EL_ATOM_CONFORMER_Z = "PC-Conformer_z";
        private const string EL_ATOM_CONFORMER_ZE = "PC-Conformer_z_E";

        // bond block elements
        private const string EL_BONDBLOCK = "PC-Bonds";
        private const string EL_BONDID1 = "PC-Bonds_aid1";
        private const string EL_BONDID2 = "PC-Bonds_aid2";
        private const string EL_BONDORDER = "PC-Bonds_order";

        // property block elements
        private const string EL_PROPSBLOCK = "PC-Compound_props";
        private const string EL_PROPS_INFODATA = "PC-InfoData";
        private const string EL_PROPS_URNLABEL = "PC-Urn_label";
        private const string EL_PROPS_URNNAME = "PC-Urn_name";
        private const string EL_PROPS_SVAL = "PC-InfoData_value_sval";
        private const string EL_PROPS_FVAL = "PC-InfoData_value_fval";
        private const string EL_PROPS_BVAL = "PC-InfoData_value_binary";

        /// <summary>"http://www.ncbi.nlm.nih.gov"</summary>
        internal static readonly XNamespace PubChem_Namespace = XNamespace.Get("http://www.ncbi.nlm.nih.gov");
        /// <summary>PC-Compound</summary>
        internal static readonly XName Name_EL_PCCOMPOUND = PubChem_Namespace + "PC-Compound";
        /// <summary>PC-Compounds</summary>
        internal static readonly XName Name_EL_PCCOMPOUNDS = PubChem_Namespace + "PC-Compounds";
        /// <summary>PC-Substance</summary>
        internal static readonly XName Name_EL_PCSUBSTANCE = PubChem_Namespace + "PC-Substance";
        /// <summary>PC-Substance_sid</summary>
        internal static readonly XName Name_EL_PCSUBSTANCE_SID = PubChem_Namespace + "PC-Substance_sid";
        /// <summary>PC-Compound_id</summary>
        internal static readonly XName Name_EL_PCCOMPOUND_ID = PubChem_Namespace + "PC-Compound_id";
        /// <summary>PC-CompoundType_id_cid</summary>
        internal static readonly XName Name_EL_PCCOMPOUND_CID = PubChem_Namespace + "PC-CompoundType_id_cid";
        /// <summary>PC-ID_id</summary>
        internal static readonly XName Name_EL_PCID_ID = PubChem_Namespace + "PC-ID_id";

        // atom block elements
        internal static readonly XName Name_EL_ATOMBLOCK = PubChem_Namespace + EL_ATOMBLOCK;
        internal static readonly XName Name_EL_ATOMSELEMENT = PubChem_Namespace + EL_ATOMSELEMENT;
        internal static readonly XName Name_EL_ATOMSCHARGE = PubChem_Namespace + EL_ATOMSCHARGE;
        internal static readonly XName Name_EL_ATOMINT = PubChem_Namespace + EL_ATOMINT;
        internal static readonly XName Name_EL_ATOMINT_AID = PubChem_Namespace + EL_ATOMINT_AID;
        internal static readonly XName Name_EL_ATOMINT_VALUE = PubChem_Namespace + EL_ATOMINT_VALUE;
        internal static readonly XName Name_EL_ELEMENT = PubChem_Namespace + EL_ELEMENT;

        // coordinate block elements
        internal static readonly XName Name_EL_COORDINATESBLOCK = PubChem_Namespace + EL_COORDINATESBLOCK;
        internal static readonly XName Name_EL_COORDINATES_AID = PubChem_Namespace + EL_COORDINATES_AID;
        internal static readonly XName Name_EL_COORDINATES_AIDE = PubChem_Namespace + EL_COORDINATES_AIDE;
        internal static readonly XName Name_EL_ATOM_CONFORMER = PubChem_Namespace + EL_ATOM_CONFORMER;
        internal static readonly XName Name_EL_ATOM_CONFORMER_X = PubChem_Namespace + EL_ATOM_CONFORMER_X;
        internal static readonly XName Name_EL_ATOM_CONFORMER_XE = PubChem_Namespace + EL_ATOM_CONFORMER_XE;
        internal static readonly XName Name_EL_ATOM_CONFORMER_Y = PubChem_Namespace + EL_ATOM_CONFORMER_Y;
        internal static readonly XName Name_EL_ATOM_CONFORMER_YE = PubChem_Namespace + EL_ATOM_CONFORMER_YE;
        internal static readonly XName Name_EL_ATOM_CONFORMER_Z = PubChem_Namespace + EL_ATOM_CONFORMER_Z;
        internal static readonly XName Name_EL_ATOM_CONFORMER_ZE = PubChem_Namespace + EL_ATOM_CONFORMER_ZE;

        // bond block elements
        internal static readonly XName Name_EL_BONDBLOCK = PubChem_Namespace + EL_BONDBLOCK;
        internal static readonly XName Name_EL_BONDID1 = PubChem_Namespace + EL_BONDID1;
        internal static readonly XName Name_EL_BONDID2 = PubChem_Namespace + EL_BONDID2;
        internal static readonly XName Name_EL_BONDORDER = PubChem_Namespace + EL_BONDORDER;

        // property block elements
        internal static readonly XName Name_EL_PROPSBLOCK = PubChem_Namespace + EL_PROPSBLOCK;
        internal static readonly XName Name_EL_PROPS_INFODATA = PubChem_Namespace + EL_PROPS_INFODATA;
        internal static readonly XName Name_EL_PROPS_URNLABEL = PubChem_Namespace + EL_PROPS_URNLABEL;
        internal static readonly XName Name_EL_PROPS_URNNAME = PubChem_Namespace + EL_PROPS_URNNAME;
        internal static readonly XName Name_EL_PROPS_SVAL = PubChem_Namespace + EL_PROPS_SVAL;
        internal static readonly XName Name_EL_PROPS_FVAL = PubChem_Namespace + EL_PROPS_FVAL;
        internal static readonly XName Name_EL_PROPS_BVAL = PubChem_Namespace + EL_PROPS_BVAL;

        // field tags
        internal static readonly XName Name_EL_PCBonds_aid1_E = PubChem_Namespace + "PC-Bonds_aid1_E";
        internal static readonly XName Name_EL_PCBonds_aid2_E = PubChem_Namespace + "PC-Bonds_aid2_E";
        internal static readonly XName Name_EL_PCBondType = PubChem_Namespace + "PC-BondType";

        public IChemObjectSet<IAtomContainer> ParseCompoundsBlock(XElement parser)
        {
            var set = builder.NewAtomContainerSet();
            // assume the current element is PC-Compounds
            if (!parser.Name.Equals(Name_EL_PCCOMPOUNDS))
                return null;
            foreach (var elm in parser.Elements(Name_EL_PCCOMPOUND))
            {
                var molecule = ParseMolecule(elm, builder);
                if (molecule.Atoms.Count > 0)
                {
                    // skip empty PC-Compound's
                    set.Add(molecule);
                }
            }
            return set;
        }

        public IChemModel ParseSubstance(XElement parser)
        {
            IChemModel model = builder.NewChemModel();
            // assume the current element is PC-Compound
            if (!parser.Name.Equals(Name_EL_PCSUBSTANCE))
                return null;
            foreach (var elm in parser.Descendants(Name_EL_PCCOMPOUNDS))
            {
                var set = ParseCompoundsBlock(elm);
                model.MoleculeSet = set;
            }
            foreach (var elm in parser.Descendants(Name_EL_PCSUBSTANCE_SID))
            {
                string sid = GetSID(elm);
                model.SetProperty(CDKPropertyName.Title, sid);
            }
            return model;
        }

        public static string GetSID(XElement parser)
        {
            string sid = "unknown";
            if (!parser.Name.Equals(Name_EL_PCSUBSTANCE_SID))
                return null;
            foreach (var elm in parser.Descendants())
            {
                if (Name_EL_PCID_ID.Equals(elm.Name))
                {
                    sid = elm.Value;
                }
            }
            return sid;
        }

        public static string GetCID(XElement parser)
        {
            string cid = "unknown";
            if (!parser.Name.Equals(Name_EL_PCCOMPOUND_ID))
                return null;
            foreach (var elm in parser.Descendants(Name_EL_PCCOMPOUND_CID))
            {
                cid = elm.Value;
            }
            return cid;
        }

        public void ParseAtomElements(XElement parser, IAtomContainer molecule)
        {
            if (!Name_EL_ATOMSELEMENT.Equals(parser.Name))
                return;
            foreach (var elm in parser.Descendants())
            {
                if (Name_EL_ELEMENT.Equals(elm.Name))
                {
                    var atomicNumber = int.Parse(elm.Value, NumberFormatInfo.InvariantInfo);
                    var element = factory.GetElement(atomicNumber);
                    if (element == null)
                    {
                        var atom = molecule.Builder.NewPseudoAtom();
                        molecule.Atoms.Add(atom);
                    }
                    else
                    {
                        var atom = molecule.Builder.NewAtom(element.Symbol);
                        atom.AtomicNumber = element.AtomicNumber;
                        molecule.Atoms.Add(atom);
                    }
                }
            }
        }

        public void ParserAtomBlock(XElement parser, IAtomContainer molecule)
        {
            if (!Name_EL_ATOMBLOCK.Equals(parser.Name))
                return;
            foreach (var elm in parser.Descendants(Name_EL_ATOMSELEMENT))
                ParseAtomElements(elm, molecule);
            foreach (var elm in parser.Descendants(Name_EL_ATOMSCHARGE))
                ParseAtomCharges(elm, molecule);
        }

        public static void ParserCompoundInfoData(XElement parser, IAtomContainer molecule)
        {
            string urnLabel = null;
            string urnName = null;
            string sval = null;
            if (!Name_EL_PROPS_INFODATA.Equals(parser.Name))
                return;
            foreach (var elm in parser.Descendants())
            {
                if (Name_EL_PROPS_URNNAME.Equals(elm.Name))
                    urnName = elm.Value;
                else if (Name_EL_PROPS_URNLABEL.Equals(elm.Name))
                    urnLabel = elm.Value;
                else if (Name_EL_PROPS_SVAL.Equals(elm.Name))
                    sval = elm.Value;
                else if (Name_EL_PROPS_FVAL.Equals(elm.Name))
                    sval = elm.Value;
                else if (Name_EL_PROPS_BVAL.Equals(elm.Name))
                    sval = elm.Value;
            }
            if (urnLabel != null & sval != null)
            {
                string property = urnLabel + (urnName == null ? "" : " (" + urnName + ")");
                molecule.SetProperty(property, sval);
            }
        }

        public static void ParseAtomCharges(XElement parser, IAtomContainer molecule)
        {
            if (!Name_EL_ATOMSCHARGE.Equals(parser.Name))
                return;
            foreach (var elm in parser.Descendants())
            {
                if (Name_EL_ATOMINT.Equals(elm.Name))
                {
                    int aid = 0;
                    int charge = 0;
                    foreach (var aie in elm.Descendants())
                    {
                        if (Name_EL_ATOMINT_AID.Equals(aie.Name))
                            aid = int.Parse(aie.Value, NumberFormatInfo.InvariantInfo);
                        else if (Name_EL_ATOMINT_VALUE.Equals(aie.Name))
                            charge = int.Parse(aie.Value, NumberFormatInfo.InvariantInfo);
                    }
                    molecule.Atoms[aid - 1].FormalCharge = charge;
                }
            }
        }

        public IAtomContainer ParseMolecule(XElement parser, IChemObjectBuilder builder)
        {
            IAtomContainer molecule = builder.NewAtomContainer();
            // assume the current element is PC-Compound
            if (!parser.Name.Equals(Name_EL_PCCOMPOUND))
                return null;
            foreach (var elm in parser.Descendants(Name_EL_ATOMBLOCK))
                ParserAtomBlock(elm, molecule);
            foreach (var elm in parser.Descendants(Name_EL_BONDBLOCK))
                ParserBondBlock(elm, molecule);
            foreach (var elm in parser.Descendants(Name_EL_COORDINATESBLOCK))
                ParserCoordBlock(elm, molecule);
            foreach (var elm in parser.Descendants(Name_EL_PROPS_INFODATA))
                ParserCompoundInfoData(elm, molecule);
            foreach (var elm in parser.Descendants(Name_EL_PCCOMPOUND_ID))
            {
                string cid = GetCID(elm);
                molecule.SetProperty("PubChem CID", cid);
            }
            return molecule;
        }

        public static void ParserBondBlock(XElement parser, IAtomContainer molecule)
        {
            var id1s = new List<string>();
            var id2s = new List<string>();
            var orders = new List<string>();
            foreach (var elm in parser.Descendants())
            {
                if (Name_EL_BONDID1.Equals(elm.Name))
                    id1s = ParseValues(elm, Name_EL_PCBonds_aid1_E);
                else if (Name_EL_BONDID2.Equals(elm.Name))
                    id2s = ParseValues(elm, Name_EL_PCBonds_aid2_E);
                else if (Name_EL_BONDORDER.Equals(elm.Name))
                    orders = ParseValues(elm, Name_EL_PCBondType);
            }
            // aggregate information
            if (id1s.Count != id2s.Count)
                throw new CDKException("Inequal number of atom identifier in bond block.");
            if (id1s.Count != orders.Count)
                throw new CDKException("Number of bond orders does not match number of bonds in bond block.");
            for (int i = 0; i < id1s.Count; i++)
            {
                IAtom atom1 = molecule.Atoms[int.Parse(id1s[i], NumberFormatInfo.InvariantInfo) - 1];
                IAtom atom2 = molecule.Atoms[int.Parse(id2s[i], NumberFormatInfo.InvariantInfo) - 1];
                IBond bond = molecule.Builder.NewBond(atom1, atom2);
                int order = int.Parse(orders[i], NumberFormatInfo.InvariantInfo);
                if (order == 1)
                {
                    bond.Order = BondOrder.Single;
                    molecule.Bonds.Add(bond);
                }
                else if (order == 2)
                {
                    bond.Order = BondOrder.Double;
                    molecule.Bonds.Add(bond);
                }
                if (order == 3)
                {
                    bond.Order = BondOrder.Triple;
                    molecule.Bonds.Add(bond);
                }
                else
                {
                    // unknown bond order, skip
                }
            }
        }

        public static void ParserCoordBlock(XElement parser, IAtomContainer molecule)
        {
            var ids = new List<string>();
            var xs = new List<string>();
            var ys = new List<string>();
            var zs = new List<string>();
            foreach (var elm in parser.Descendants())
            {
                if (Name_EL_COORDINATES_AID.Equals(elm.Name))
                {
                    ids = ParseValues(elm, Name_EL_COORDINATES_AIDE);
                }
                else if (Name_EL_ATOM_CONFORMER.Equals(elm.Name))
                {
                    foreach (var cfi in elm.Descendants())
                    {
                        if (Name_EL_ATOM_CONFORMER_X.Equals(cfi.Name))
                            xs = ParseValues(cfi, Name_EL_ATOM_CONFORMER_XE);
                        else if (Name_EL_ATOM_CONFORMER_Y.Equals(cfi.Name))
                            ys = ParseValues(cfi, Name_EL_ATOM_CONFORMER_YE);
                        else if (Name_EL_ATOM_CONFORMER_Z.Equals(cfi.Name))
                            zs = ParseValues(cfi, Name_EL_ATOM_CONFORMER_ZE);
                    }
                }
            }
            // aggregate information
            bool has2dCoords = ids.Count == xs.Count && ids.Count == ys.Count;
            bool has3dCoords = has2dCoords && ids.Count == zs.Count;

            for (int i = 0; i < ids.Count; i++)
            {
                IAtom atom = molecule.Atoms[int.Parse(ids[i], NumberFormatInfo.InvariantInfo) - 1];
                if (has3dCoords)
                {
                    Vector3 coord = new Vector3(double.Parse(xs[i], NumberFormatInfo.InvariantInfo), double.Parse(ys[i], NumberFormatInfo.InvariantInfo), double.Parse(zs[i], NumberFormatInfo.InvariantInfo));
                    atom.Point3D = coord;
                }
                else if (has2dCoords)
                {
                    Vector2 coord = new Vector2(double.Parse(xs[i], NumberFormatInfo.InvariantInfo), double.Parse(ys[i], NumberFormatInfo.InvariantInfo));
                    atom.Point2D = coord;
                }
            }
        }

        private static List<string> ParseValues(XElement parser, XName fieldTag)
        {
            var values = new List<string>();
            foreach (var elm in parser.Descendants())
            {
                if (fieldTag.Equals(elm.Name))
                {
                    string value = elm.Value;
                    values.Add(value);
                }
            }
            return values;
        }
    }
}
