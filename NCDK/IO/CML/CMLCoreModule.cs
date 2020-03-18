/* Copyright (C) 1997-2007  Egon Willighagen <egonw@users.sf.net>
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

using NCDK.Common.Primitives;
using NCDK.Dict;
using NCDK.Geometries;
using NCDK.Numerics;
using NCDK.Stereo;
using NCDK.Tools;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace NCDK.IO.CML
{
    /// <summary>
    /// Core CML 1.x and 2.x elements are parsed by this class (see <token>cdk-cite-WIL01</token>).
    ///
    /// <para>Please file a bug report if this parser fails to parse
    /// a certain element or attribute value in a valid CML document.</para>
    /// </summary>
    // @cdk.module io
    // @author Egon Willighagen <egonw@sci.kun.nl>
    public class CMLCoreModule : ICMLModule
    {
        protected const string SYSTEMID = "CML-1999-05-15";

        // data model to store things into
        protected IChemFile CurrentChemFile { get; set; }

        protected IAtomContainer CurrentMolecule { get; set; }
        protected IChemObjectSet<IAtomContainer> CurrentMoleculeSet { get; set; }
        protected IChemModel CurrentChemModel { get; set; }
        protected IChemSequence CurrentChemSequence { get; set; }
        protected IReactionSet CurrentReactionSet { get; set; }
        protected IReaction CurrentReaction { get; set; }
        protected IAtom CurrentAtom { get; set; }
        protected IBond CurrentBond { get; set; }
        protected IStrand CurrentStrand { get; set; }
        protected IMonomer CurrentMonomer { get; set; }
        protected Dictionary<string, IAtom> AtomEnumeration { get; set; }
        protected IList<string> MoleculeCustomProperty { get; set; }

        // helper fields
        protected int FormulaCounter { get; set; }
        protected int AtomCounter { get; set; }
        protected List<string> ElSym { get; set; }
        protected List<string> ElTitles { get; set; }
        protected List<string> ElId { get; set; }
        protected List<string> Formula { get; set; }
        protected List<string> FormalCharges { get; set; }
        protected List<string> PartialCharges { get; set; }
        protected List<string> Isotope { get; set; }
        protected List<string> AtomicNumbers { get; set; }
        protected List<string> ExactMasses { get; set; }
        protected List<string> X3 { get; set; }
        protected List<string> Y3 { get; set; }
        protected List<string> Z3 { get; set; }
        protected List<string> X2 { get; set; }
        protected List<string> Y2 { get; set; }
        protected List<string> XFract { get; set; }
        protected List<string> YFract { get; set; }
        protected List<string> ZFract { get; set; }
        protected List<string> HCounts { get; set; }
        protected List<string> AtomParities { get; set; }
        protected List<string> ParityARef1 { get; set; }
        protected List<string> ParityARef2 { get; set; }
        protected List<string> ParityARef3 { get; set; }
        protected List<string> ParityARef4 { get; set; }
        protected List<string> AtomDictRefs { get; set; }
        protected List<string> AtomAromaticities { get; set; }
        protected List<string> SpinMultiplicities { get; set; }
        protected List<string> Occupancies { get; set; }
        protected Dictionary<int, List<string>> AtomCustomProperty { get; set; }
        protected bool ParityAtomsGiven { get; set; }
        protected bool ParityGiven { get; set; }

        protected int BondCounter { get; set; }
        protected List<string> BondId { get; set; }
        protected List<string> BondARef1 { get; set; }
        protected List<string> BondARef2 { get; set; }
        protected List<string> Order { get; set; }
        protected List<string> BondStereo { get; set; }
        protected List<string> BondDictRefs { get; set; }
        protected List<string> BondElId { get; set; }
        protected List<bool?> BondAromaticity { get; set; }
        protected Dictionary<string, Dictionary<string, string>> BondCustomProperty { get; set; }
        protected bool StereoGiven { get; set; }
        protected string InChIString { get; set; }
        protected int CurRef { get; set; }
        protected int CurrentElement { get; set; }
        protected string Builtin { get; set; }
        protected string DictRef { get; set; }
        protected string ElementTitle { get; set; }

        protected double[] UnitCellParams { get; set; }
        protected int CrystalScalar { get; set; }

        public CMLCoreModule(IChemFile chemFile)
        {
            this.CurrentChemFile = chemFile;
        }

        public CMLCoreModule(ICMLModule conv)
        {
            Inherit(conv);
        }

        public void Inherit(ICMLModule convention)
        {
            if (convention is CMLCoreModule conv)
            {
                // copy the data model
                this.CurrentChemFile = conv.CurrentChemFile;
                this.CurrentMolecule = conv.CurrentMolecule;
                this.CurrentMoleculeSet = conv.CurrentMoleculeSet;
                this.CurrentChemModel = conv.CurrentChemModel;
                this.CurrentChemSequence = conv.CurrentChemSequence;
                this.CurrentReactionSet = conv.CurrentReactionSet;
                this.CurrentReaction = conv.CurrentReaction;
                this.CurrentAtom = conv.CurrentAtom;
                this.CurrentStrand = conv.CurrentStrand;
                this.CurrentMonomer = conv.CurrentMonomer;
                this.AtomEnumeration = conv.AtomEnumeration;
                this.MoleculeCustomProperty = conv.MoleculeCustomProperty;

                // copy the intermediate fields
                this.Builtin = conv.Builtin;
                this.AtomCounter = conv.AtomCounter;
                this.FormulaCounter = conv.FormulaCounter;
                this.ElSym = conv.ElSym;
                this.ElTitles = conv.ElTitles;
                this.ElId = conv.ElId;
                this.FormalCharges = conv.FormalCharges;
                this.PartialCharges = conv.PartialCharges;
                this.Isotope = conv.Isotope;
                this.AtomicNumbers = conv.AtomicNumbers;
                this.ExactMasses = conv.ExactMasses;
                this.X3 = conv.X3;
                this.Y3 = conv.Y3;
                this.Z3 = conv.Z3;
                this.X2 = conv.X2;
                this.Y2 = conv.Y2;
                this.XFract = conv.XFract;
                this.YFract = conv.YFract;
                this.ZFract = conv.ZFract;
                this.HCounts = conv.HCounts;
                this.AtomParities = conv.AtomParities;
                this.ParityARef1 = conv.ParityARef1;
                this.ParityARef2 = conv.ParityARef2;
                this.ParityARef3 = conv.ParityARef3;
                this.ParityARef4 = conv.ParityARef4;
                this.AtomDictRefs = conv.AtomDictRefs;
                this.AtomAromaticities = conv.AtomAromaticities;
                this.SpinMultiplicities = conv.SpinMultiplicities;
                this.Occupancies = conv.Occupancies;
                this.BondCounter = conv.BondCounter;
                this.BondId = conv.BondId;
                this.BondARef1 = conv.BondARef1;
                this.BondARef2 = conv.BondARef2;
                this.Order = conv.Order;
                this.BondStereo = conv.BondStereo;
                this.BondCustomProperty = conv.BondCustomProperty;
                this.AtomCustomProperty = conv.AtomCustomProperty;
                this.BondDictRefs = conv.BondDictRefs;
                this.BondAromaticity = conv.BondAromaticity;
                this.CurRef = conv.CurRef;
                this.UnitCellParams = conv.UnitCellParams;
                this.InChIString = conv.InChIString;
            }
            else
            {
                Trace.TraceWarning("Cannot inherit information from module: ", convention.GetType().Name);
            }
        }

        public IChemFile ReturnChemFile()
        {
            return CurrentChemFile;
        }

        /// <summary>
        /// Clean all data about parsed data.
        /// </summary>
        protected void NewMolecule()
        {
            NewMoleculeData();
            NewAtomData();
            NewBondData();
            NewCrystalData();
            NewFormulaData();
        }

        /// <summary>
        /// Clean all data about the molecule itself.
        /// </summary>
        protected void NewMoleculeData()
        {
            this.InChIString = null;
        }

        /// <summary>
        /// Clean all data about read formulas.
        /// </summary>
        protected void NewFormulaData()
        {
            FormulaCounter = 0;
            Formula = new List<string>();
        }

        /// <summary>
        /// Clean all data about read atoms.
        /// </summary>
        protected void NewAtomData()
        {
            AtomCounter = 0;
            ElSym = new List<string>();
            ElId = new List<string>();
            ElTitles = new List<string>();
            FormalCharges = new List<string>();
            PartialCharges = new List<string>();
            Isotope = new List<string>();
            AtomicNumbers = new List<string>();
            ExactMasses = new List<string>();
            X3 = new List<string>();
            Y3 = new List<string>();
            Z3 = new List<string>();
            X2 = new List<string>();
            Y2 = new List<string>();
            XFract = new List<string>();
            YFract = new List<string>();
            ZFract = new List<string>();
            HCounts = new List<string>();
            AtomParities = new List<string>();
            ParityARef1 = new List<string>();
            ParityARef2 = new List<string>();
            ParityARef3 = new List<string>();
            ParityARef4 = new List<string>();
            AtomAromaticities = new List<string>();
            AtomDictRefs = new List<string>();
            SpinMultiplicities = new List<string>();
            Occupancies = new List<string>();
            AtomCustomProperty = new Dictionary<int, List<string>>();
        }

        /// <summary>
        /// Clean all data about read bonds.
        /// </summary>
        protected void NewBondData()
        {
            BondCounter = 0;
            BondId = new List<string>();
            BondARef1 = new List<string>();
            BondARef2 = new List<string>();
            Order = new List<string>();
            BondStereo = new List<string>();
            BondCustomProperty = new Dictionary<string, Dictionary<string, string>>();
            BondDictRefs = new List<string>();
            BondElId = new List<string>();
            BondAromaticity = new List<bool?>();
        }

        /// <summary>
        /// Clean all data about read bonds.
        /// </summary>
        protected void NewCrystalData()
        {
            UnitCellParams = new double[6];
            CrystalScalar = 0;
        }

        public virtual void StartDocument()
        {
            Trace.TraceInformation("Start XML Doc");
            CurrentChemSequence = CurrentChemFile.Builder.NewChemSequence();
            CurrentChemModel = CurrentChemFile.Builder.NewChemModel();
            CurrentMoleculeSet = CurrentChemFile.Builder.NewAtomContainerSet();
            CurrentMolecule = CurrentChemFile.Builder.NewAtomContainer();
            AtomEnumeration = new Dictionary<string, IAtom>();
            MoleculeCustomProperty = new List<string>();

            NewMolecule();
            Builtin = "";
            CurRef = 0;
        }

        public virtual void EndDocument()
        {
            if (CurrentReactionSet != null && CurrentReactionSet.Count == 0 && CurrentReaction != null)
            {
                Debug.WriteLine("Adding reaction to ReactionSet");
                CurrentReactionSet.Add(CurrentReaction);
            }
            if (CurrentReactionSet != null && CurrentChemModel.ReactionSet == null)
            {
                Debug.WriteLine("Adding SOR to ChemModel");
                CurrentChemModel.ReactionSet = CurrentReactionSet;
            }
            if (CurrentMoleculeSet != null && CurrentMoleculeSet.Count != 0)
            {
                Debug.WriteLine("Adding reaction to MoleculeSet");
                CurrentChemModel.MoleculeSet = CurrentMoleculeSet;
            }
            if (CurrentChemSequence.Count == 0)
            {
                Debug.WriteLine("Adding ChemModel to ChemSequence");
                CurrentChemSequence.Add(CurrentChemModel);
            }
            if (CurrentChemFile.Count == 0)
            {
                // assume there is one non-animation ChemSequence
                //            AddChemSequence(currentChemSequence);
                CurrentChemFile.Add(CurrentChemSequence);
            }

            Trace.TraceInformation("End XML Doc");
        }

        internal static string AttGetValue(IEnumerable<XAttribute> atts, string name)
        {
            var attribute = atts.Where(n => n.Name.LocalName == name).FirstOrDefault();
            return attribute?.Value;
        }

        public virtual void StartElement(CMLStack xpath, XElement element)
        {
            var name = element.Name.LocalName;
            Debug.WriteLine("StartElement");

            Builtin = "";
            DictRef = "";

            foreach (var att in element.Attributes())
            {
                var qname = att.Name.LocalName;
                switch (qname)
                {
                    case "builtin":
                        Builtin = att.Value;
                        Debug.WriteLine(name, "->BUILTIN found: ", att.Value);
                        break;
                    case "dictRef":
                        DictRef = att.Value;
                        Debug.WriteLine(name, "->DICTREF found: ", att.Value);
                        break;
                    case "title":
                        ElementTitle = att.Value;
                        Debug.WriteLine(name, "->TITLE found: ", att.Value);
                        break;
                    default:
                        Debug.WriteLine($"Qname: {qname}");
                        break;
                }
            }

            switch (name)
            {
                case "atom":
                    AtomCounter++;
                    foreach (var atti in element.Attributes())
                    {
                        var att = atti.Name.LocalName;
                        var value = atti.Value;
                        switch (att)
                        {
                            case "id":
                                // this is supported in CML 1.X
                                ElId.Add(value);
                                break;
                            case "elementType":
                                // this is supported in CML 2.0
                                ElSym.Add(value);
                                break;
                            case "title":
                                // this is supported in CML 2.0
                                ElTitles.Add(value);
                                break;
                            case "x2":
                                // this is supported in CML 2.0
                                X2.Add(value);
                                break;
                            case "xy2":
                                {
                                    // this is supported in CML 2.0
                                    var tokens = Strings.Tokenize(value);
                                    X2.Add(tokens[0]);
                                    Y2.Add(tokens[1]);
                                }
                                break;
                            case "xyzFract":
                                {
                                    // this is supported in CML 2.0
                                    var tokens = Strings.Tokenize(value);
                                    XFract.Add(tokens[0]);
                                    YFract.Add(tokens[1]);
                                    ZFract.Add(tokens[2]);
                                }
                                break;
                            case "xyz3":
                                {
                                    // this is supported in CML 2.0
                                    var tokens = Strings.Tokenize(value);
                                    X3.Add(tokens[0]);
                                    Y3.Add(tokens[1]);
                                    Z3.Add(tokens[2]);
                                }
                                break;
                            case "y2":
                                // this is supported in CML 2.0
                                Y2.Add(value);
                                break;
                            case "x3":
                                // this is supported in CML 2.0
                                X3.Add(value);
                                break;
                            case "y3":
                                // this is supported in CML 2.0
                                Y3.Add(value);
                                break;
                            case "z3":
                                // this is supported in CML 2.0
                                Z3.Add(value);
                                break;
                            case "xFract":
                                // this is supported in CML 2.0
                                XFract.Add(value);
                                break;
                            case "yFract":
                                // this is supported in CML 2.0
                                YFract.Add(value);
                                break;
                            case "zFract":
                                // this is supported in CML 2.0
                                ZFract.Add(value);
                                break;
                            case "formalCharge":
                                // this is supported in CML 2.0
                                FormalCharges.Add(value);
                                break;
                            case "hydrogenCount":
                                // this is supported in CML 2.0
                                HCounts.Add(value);
                                break;
                            case "isotopeNumber":
                                Isotope.Add(value);
                                break;
                            case "dictRef":
                                Debug.WriteLine($"occupancy: {value}");
                                AtomDictRefs.Add(value);
                                break;
                            case "spinMultiplicity":
                                SpinMultiplicities.Add(value);
                                break;
                            case "occupancy":
                                Occupancies.Add(value);
                                break;
                            default:
                                Trace.TraceWarning("Unparsed attribute: " + att);
                                break;
                        }

                        ParityAtomsGiven = false;
                        ParityGiven = false;
                    }
                    break;
                case "atomArray":
                    if (!xpath.EndsWith("formula", "atomArray"))
                    {
                        bool atomsCounted = false;
                        foreach (var attribute in element.Attributes())
                        {
                            var xname = attribute.Name;
                            int count = 0;
                            string att = xname.LocalName;
                            switch (att)
                            {
                                case "atomID":
                                    count = AddArrayElementsTo(ElId, attribute.Value);
                                    break;
                                case "elementType":
                                    count = AddArrayElementsTo(ElSym, attribute.Value);
                                    break;
                                case "x2":
                                    count = AddArrayElementsTo(X2, attribute.Value);
                                    break;
                                case "y2":
                                    count = AddArrayElementsTo(Y2, attribute.Value);
                                    break;
                                case "x3":
                                    count = AddArrayElementsTo(X3, attribute.Value);
                                    break;
                                case "y3":
                                    count = AddArrayElementsTo(Y3, attribute.Value);
                                    break;
                                case "z3":
                                    count = AddArrayElementsTo(Z3, attribute.Value);
                                    break;
                                case "xFract":
                                    count = AddArrayElementsTo(XFract, attribute.Value);
                                    break;
                                case "yFract":
                                    count = AddArrayElementsTo(YFract, attribute.Value);
                                    break;
                                case "zFract":
                                    count = AddArrayElementsTo(ZFract, attribute.Value);
                                    break;
                                default:
                                    Trace.TraceWarning("Unparsed attribute: " + att);
                                    break;
                            }
                            if (!atomsCounted)
                            {
                                AtomCounter += count;
                                atomsCounted = true;
                            }
                        }
                    }
                    break;
                case "atomParity":
                    foreach (var attribute in element.Attributes())
                    {
                        var xname = attribute.Name;
                        var att = xname.LocalName;
                        switch (att)
                        {
                            case "atomRefs4":
                                if (!ParityAtomsGiven)
                                {
                                    //Expect exactly four references
                                    try
                                    {
                                        var tokens = Strings.Tokenize(attribute.Value);
                                        ParityARef1.Add(tokens[0]);
                                        ParityARef2.Add(tokens[1]);
                                        ParityARef3.Add(tokens[2]);
                                        ParityARef4.Add(tokens[3]);
                                        ParityAtomsGiven = true;
                                    }
                                    catch (Exception e)
                                    {
                                        Trace.TraceError($"Error in CML file: {e.Message}");
                                        Debug.WriteLine(e);
                                    }
                                }
                                break;
                        }
                    }
                    break;
                case "bond":
                    BondCounter++;
                    foreach (var attribute in element.Attributes())
                    {
                        var xname = attribute.Name;
                        var att = xname.LocalName;
                        Debug.WriteLine("B2 ", att, "=", attribute.Value);

                        switch (att)
                        {
                            case "id":
                                BondId.Add(attribute.Value);
                                Debug.WriteLine($"B3 {BondId}");
                                break;
                            case "atomRefs": // this is CML 1.X support
                            case "atomRefs2": // this is CML 2.0 support
                                // expect exactly two references
                                try
                                {
                                    var tokens = Strings.Tokenize(attribute.Value, ' ');
                                    BondARef1.Add(tokens[0]);
                                    BondARef2.Add(tokens[1]);
                                }
                                catch (Exception e)
                                {
                                    Trace.TraceError($"Error in CML file: {e.Message}");
                                    Debug.WriteLine(e);
                                }
                                break;
                            case "order":
                                // this is CML 2.0 support
                                Order.Add(attribute.Value.Trim());
                                break;
                            case "dictRef":
                                BondDictRefs.Add(attribute.Value.Trim());
                                break;
                            default:
                                break;
                        }
                    }
                    StereoGiven = false;
                    CurRef = 0;
                    break;
                case "bondArray":
                    {
                        bool bondsCounted = false;
                        foreach (var attribute in element.Attributes())
                        {
                            var xname = attribute.Name;
                            int count = 0;
                            var att = xname.LocalName;
                            switch (att)
                            {
                                case "bondID":
                                    count = AddArrayElementsTo(BondId, attribute.Value);
                                    break;
                                case "atomRefs1":
                                    count = AddArrayElementsTo(BondARef1, attribute.Value);
                                    break;
                                case "atomRefs2":
                                    count = AddArrayElementsTo(BondARef2, attribute.Value);
                                    break;
                                case "atomRef1":
                                    count = AddArrayElementsTo(BondARef1, attribute.Value);
                                    break;
                                case "atomRef2":
                                    count = AddArrayElementsTo(BondARef2, attribute.Value);
                                    break;
                                case "order":
                                    count = AddArrayElementsTo(Order, attribute.Value);
                                    break;
                                default:
                                    Trace.TraceWarning("Unparsed attribute: " + att);
                                    break;
                            }
                            if (!bondsCounted)
                            {
                                BondCounter += count;
                                bondsCounted = true;
                            }
                        }
                        CurRef = 0;
                    }
                    break;
                case "bondStereo":
                    foreach (var attribute in element.Attributes())
                    {
                        var xname = attribute.Name;
                        var att = xname.LocalName;
                        switch (att)
                        {
                            case "dictRef":
                                var value = attribute.Value;
                                if (value.StartsWith("cml:", StringComparison.Ordinal) && value.Length > 4)
                                {
                                    BondStereo.Add(value.Substring(4));
                                    StereoGiven = true;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case "bondType":
                    foreach (var attribute in element.Attributes())
                    {
                        var xname = attribute.Name;
                        var att = xname.LocalName;
                        switch (att)
                        {
                            case "dictRef":
                                if (string.Equals(attribute.Value, "cdk:aromaticBond", StringComparison.Ordinal))
                                    BondAromaticity.Add(true);
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case "molecule":
                    {
                        NewMolecule();
                        Builtin = "";
                        if (CurrentChemModel == null)
                            CurrentChemModel = CurrentChemFile.Builder.NewChemModel();
                        if (CurrentMoleculeSet == null)
                            CurrentMoleculeSet = CurrentChemFile.Builder.NewAtomContainerSet();
                        CurrentMolecule = CurrentChemFile.Builder.NewAtomContainer();
                        foreach (var attribute in element.Attributes())
                        {
                            var xname = attribute.Name;
                            string att = xname.LocalName;
                            switch (att)
                            {
                                case "id":
                                    CurrentMolecule.Id = attribute.Value;
                                    break;
                                case "dictRef":
                                    CurrentMolecule.SetProperty(new DictRef(DictRef, attribute.Value), attribute.Value);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                    break;
                case "crystal":
                    NewCrystalData();
                    CurrentMolecule = CurrentChemFile.Builder.NewCrystal(CurrentMolecule);
                    foreach (var attribute in element.Attributes())
                    {
                        var xname = attribute.Name;
                        string att = xname.LocalName;
                        switch (att)
                        {
                            case "z":
                                ((ICrystal)CurrentMolecule).Z = int.Parse(attribute.Value, NumberFormatInfo.InvariantInfo);
                                break;
                            default:
                                break;
                        }
                    }
                    break;
                case "symmetry":
                    foreach (var attribute in element.Attributes())
                    {
                        var xname = attribute.Name;
                        var att = xname.LocalName;
                        if (string.Equals(att, "spaceGroup", StringComparison.Ordinal))
                        {
                            ((ICrystal)CurrentMolecule).SpaceGroup = attribute.Value;
                        }
                    }
                    break;
                case "identifier":
                    {
                        var convention_value = AttGetValue(element.Attributes(), "convention");
                        if (string.Equals("iupac:inchi", convention_value, StringComparison.Ordinal))
                        {
                            if (element.Attribute("value") != null)
                                CurrentMolecule.SetProperty(CDKPropertyName.InChI, element.Attribute("value").Value);
                        }
                    }
                    break;
                case "scalar":
                    if (xpath.EndsWith("crystal", "scalar"))
                        CrystalScalar++;
                    break;
                case "label":
                    if (xpath.EndsWith("atomType", "label"))
                    {
                        CurrentAtom.AtomTypeName = AttGetValue(element.Attributes(), "value");
                    }
                    break;
                case "list":
                    {
                        if (string.Equals(DictRef, "cdk:model", StringComparison.Ordinal))
                        {
                            CurrentChemModel = CurrentChemFile.Builder.NewChemModel();
                           foreach (var attribute in element.Attributes())
                            {
                                var xname = attribute.Name;
                                var att = xname.LocalName;
                                if (string.Equals(att, "id", StringComparison.Ordinal))
                                {
                                    CurrentChemModel.Id = attribute.Value;
                                }
                            }
                        }
                        else if (string.Equals(DictRef, "cdk:moleculeSet", StringComparison.Ordinal))
                        {
                            CurrentMoleculeSet = CurrentChemFile.Builder.NewAtomContainerSet();
                            // see if there is an ID attribute
                            foreach (var attribute in element.Attributes())
                            {
                                var xname = attribute.Name;
                                var att = xname.LocalName;
                                if (string.Equals(att, "id", StringComparison.Ordinal))
                                {
                                    CurrentMoleculeSet.Id = attribute.Value;
                                }
                            }
                            CurrentMolecule = CurrentChemFile.Builder.NewAtomContainer();
                        }
                        else
                        {
                            // the old default
                            CurrentMoleculeSet = CurrentChemFile.Builder.NewAtomContainerSet();
                            // see if there is an ID attribute
                            foreach (var attribute in element.Attributes())
                            {
                                var xname = attribute.Name;
                                var att = xname.LocalName;
                                if (string.Equals(att, "id", StringComparison.Ordinal))
                                {
                                    CurrentMoleculeSet.Id = attribute.Value;
                                }
                            }
                            CurrentMolecule = CurrentChemFile.Builder.NewAtomContainer();
                        }
                    }
                    break;
                case "formula":
                        FormulaCounter++;
                    foreach (var attribute in element.Attributes())
                    {
                        var xname = attribute.Name;
                        var att = xname.LocalName;
                        var value = attribute.Value;
                        if (string.Equals(att, "concise", StringComparison.Ordinal))
                        {
                            Formula.Add(value);
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        public virtual void EndElement(CMLStack xpath, XElement element)
        {
            var name = element.Name.LocalName;

            Debug.WriteLine($"EndElement: {name}");

            string cData = element.Value;
            switch (name)
            {
                case "bond":
                    if (!StereoGiven)
                        BondStereo.Add("");
                    if (BondCounter > BondDictRefs.Count)
                        BondDictRefs.Add(null);
                    if (BondCounter > BondAromaticity.Count)
                        BondAromaticity.Add(null);
                    break;
                case "atom":
                    if (AtomCounter > ElTitles.Count)
                        ElTitles.Add(null);
                    if (AtomCounter > HCounts.Count)
                        HCounts.Add(null);
                    if (AtomCounter > AtomDictRefs.Count)
                        AtomDictRefs.Add(null);
                    if (AtomCounter > AtomAromaticities.Count)
                        AtomAromaticities.Add(null);
                    if (AtomCounter > Isotope.Count)
                        Isotope.Add(null);
                    if (AtomCounter > AtomicNumbers.Count)
                        AtomicNumbers.Add(null);
                    if (AtomCounter > ExactMasses.Count)
                        ExactMasses.Add(null);
                    if (AtomCounter > SpinMultiplicities.Count)
                        SpinMultiplicities.Add(null);
                    if (AtomCounter > Occupancies.Count)
                        Occupancies.Add(null);
                    if (AtomCounter > FormalCharges.Count)
                    {
                        // while strictly undefined, assume zero formal charge when no
                        // number is given
                        FormalCharges.Add("0");
                    }
                    if (!ParityGiven)
                        AtomParities.Add("");
                    if (!ParityAtomsGiven)
                    {
                        ParityARef1.Add("");
                        ParityARef2.Add("");
                        ParityARef3.Add("");
                        ParityARef4.Add("");
                    }
                    // It may happen that not all atoms have associated 2D or 3D
                    // coordinates. accept that
                    if (AtomCounter > X2.Count && X2.Count != 0)
                    {
                        // apparently, the previous atoms had atomic coordinates, add
                        // 'null' for this atom
                        X2.Add(null);
                        Y2.Add(null);
                    }
                    if (AtomCounter > X3.Count && X3.Count != 0)
                    {
                        // apparently, the previous atoms had atomic coordinates, add
                        // 'null' for this atom
                        X3.Add(null);
                        Y3.Add(null);
                        Z3.Add(null);
                    }

                    if (AtomCounter > XFract.Count && XFract.Count != 0)
                    {
                        // apparently, the previous atoms had atomic coordinates, add
                        // 'null' for this atom
                        XFract.Add(null);
                        YFract.Add(null);
                        ZFract.Add(null);
                    }
                    break;
                case "molecule":
                    StoreData();
                    if (CurrentMolecule is ICrystal)
                    {
                        Debug.WriteLine("Adding crystal to chemModel");
                        CurrentChemModel.Crystal = (ICrystal)CurrentMolecule;
                        CurrentChemSequence.Add(CurrentChemModel);
                    }
                    else if (CurrentMolecule is IAtomContainer)
                    {
                        Debug.WriteLine("Adding molecule to set");
                        CurrentMoleculeSet.Add(CurrentMolecule);
                        Debug.WriteLine("#mols in set: " + CurrentMoleculeSet.Count());
                    }
                    break;
                case "crystal":
                    if (CrystalScalar > 0)
                    {
                        // convert unit cell parameters to cartesians
                        Vector3[] axes = CrystalGeometryTools.NotionalToCartesian(UnitCellParams[0], UnitCellParams[1],
                                UnitCellParams[2], UnitCellParams[3], UnitCellParams[4], UnitCellParams[5]);
                        ((ICrystal)CurrentMolecule).A = axes[0];
                        ((ICrystal)CurrentMolecule).B = axes[1];
                        ((ICrystal)CurrentMolecule).C = axes[2];
                    }
                    else
                    {
                        Trace.TraceError("Could not find crystal unit cell parameters");
                    }
                    break;
                case "list":
                    // FIXME: I really should check the DICTREF, but there is currently
                    // no mechanism for storing these for use with EndTag() :(
                    // So, instead, for now, just see if it already has done the setting
                    // to work around duplication
                    if (CurrentChemModel.MoleculeSet != CurrentMoleculeSet)
                    {
                        CurrentChemModel.MoleculeSet = CurrentMoleculeSet;
                        CurrentChemSequence.Add(CurrentChemModel);
                    }
                    break;
                case "coordinate3":
                    switch (Builtin)
                    {
                        case "xyz3":
                            Debug.WriteLine($"New coord3 xyz3 found: {element.Value}");
                            try
                            {
                                var tokens = Strings.Tokenize(element.Value);
                                X3.Add(tokens[0]);
                                Y3.Add(tokens[1]);
                                Z3.Add(tokens[2]);
                                Debug.WriteLine($"coord3 x3.Length: {X3.Count}");
                                Debug.WriteLine($"coord3 y3.Length: {Y3.Count}");
                                Debug.WriteLine($"coord3 z3.Length: {Z3.Count}");
                            }
                            catch (Exception exception)
                            {
                                Trace.TraceError("CMLParsing error while setting coordinate3!");
                                Debug.WriteLine(exception);
                            }
                            break;
                        default:
                            Trace.TraceWarning("Unknown coordinate3 BUILTIN: " + Builtin);
                            break;
                    }
                    break;
                case "string":
                    switch (Builtin)
                    {
                        case "elementType":
                            Debug.WriteLine($"Element: {cData.Trim()}");
                            ElSym.Add(cData);
                            break;
                        case "atomRef":
                            CurRef++;
                            Debug.WriteLine($"Bond: ref #{CurRef}");

                            if (CurRef == 1)
                            {
                                BondARef1.Add(cData.Trim());
                            }
                            else if (CurRef == 2)
                            {
                                BondARef2.Add(cData.Trim());
                            }
                            break;
                        case "order":
                            Debug.WriteLine($"Bond: order  {cData.Trim()}");
                            Order.Add(cData.Trim());
                            break;
                        case "formalCharge":
                            // NOTE: this combination is in violation of the CML DTD!!!
                            Trace.TraceWarning("formalCharge BUILTIN accepted but violating CML DTD");
                            Debug.WriteLine($"Charge:  {cData.Trim()}");
                            string charge = cData.Trim();
                            if (charge.StartsWithChar('+') && charge.Length > 1)
                            {
                                charge = charge.Substring(1);
                            }
                            FormalCharges.Add(charge);
                            break;
                        default:
                            break;
                    }
                    break;
                case "bondStereo":
                    if (!string.IsNullOrEmpty(element.Value) && !StereoGiven)
                    {
                        BondStereo.Add(element.Value);
                        StereoGiven = true;
                    }
                    break;
                case "atomParity":
                    {
                        if (!string.IsNullOrEmpty(element.Value) && !ParityGiven && ParityAtomsGiven)
                        {
                            AtomParities.Add(element.Value);
                            ParityGiven = true;
                        }
                    }
                    break;
                case "float":
                    switch (Builtin)
                    {
                        case "x3":
                            X3.Add(cData.Trim());
                            break;
                        case "y3":
                            Y3.Add(cData.Trim());
                            break;
                        case "z3":
                            Z3.Add(cData.Trim());
                            break;
                        case "x2":
                            X2.Add(cData.Trim());
                            break;
                        case "y2":
                            Y2.Add(cData.Trim());
                            break;
                        case "order":
                            // NOTE: this combination is in violation of the CML DTD!!!
                            Order.Add(cData.Trim());
                            break;
                        case "charge":
                        case "partialCharge":
                            PartialCharges.Add(cData.Trim());
                            break;
                        default:
                            break;
                    }
                    break;
                case "integer":
                    if (string.Equals(Builtin, "formalCharge", StringComparison.Ordinal))
                    {
                        FormalCharges.Add(cData.Trim());
                    }
                    break;
                case "coordinate2":
                    switch (Builtin)
                    {
                        case "xy2":
                            Debug.WriteLine($"New coord2 xy2 found.{cData}");
                            try
                            {
                                var tokens = Strings.Tokenize(cData);
                                X2.Add(tokens[0]);
                                Y2.Add(tokens[1]);
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 175, 1);
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case "stringArray":
                    switch (Builtin)
                    {
                        case "id":
                        case "atomId":
                        case "atomID":
                            // invalid according to CML1 DTD but found in OpenBabel 1.X output
                            try
                            {
                                var countAtoms = (AtomCounter == 0) ? true : false;
                                var tokens = Strings.Tokenize(cData);
                                foreach (var token in tokens)
                                {
                                    if (countAtoms)
                                    {
                                        AtomCounter++;
                                    }
                                    Debug.WriteLine($"StringArray (Token): {token}");
                                    ElId.Add(token);
                                }
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 186, 1);
                            }
                            break;
                        case "elementType":
                            try
                            {
                                var countAtoms = (AtomCounter == 0) ? true : false;
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                {
                                    if (countAtoms)
                                    {
                                        AtomCounter++;
                                    }
                                    ElSym.Add(token);
                                }
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 194, 1);
                            }
                            break;
                        case "atomRefs":
                            CurRef++;
                            Debug.WriteLine($"New atomRefs found: {CurRef}");

                            try
                            {
                                var countBonds = (BondCounter == 0) ? true : false;
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                {
                                    if (countBonds)
                                    {
                                        BondCounter++;
                                    }
                                    Debug.WriteLine($"Token: {token}");

                                    if (CurRef == 1)
                                    {
                                        BondARef1.Add(token);
                                    }
                                    else if (CurRef == 2)
                                    {
                                        BondARef2.Add(token);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 194, 1);
                            }
                            break;
                        case "atomRef":
                            CurRef++;
                            Debug.WriteLine($"New atomRef found: {CurRef}");
                            // this is CML1 stuff, we get things like:
                            // <bondArray> <stringArray builtin="atomRef">a2 a2 a2 a2 a3 a3
                            // a4 a4 a5 a6 a7 a9</stringArray> <stringArray
                            // builtin="atomRef">a9 a11 a12 a13 a5 a4 a6 a9 a7 a8 a8
                            // a10</stringArray> <stringArray builtin="order">1 1 1 1 2 1 2
                            // 1 1 1 2 2</stringArray> </bondArray>
                            try
                            {
                                var countBonds = (BondCounter == 0) ? true : false;
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                {
                                    if (countBonds)
                                    {
                                        BondCounter++;
                                    }
                                    Debug.WriteLine($"Token: {token}");

                                    if (CurRef == 1)
                                    {
                                        BondARef1.Add(token);
                                    }
                                    else if (CurRef == 2)
                                    {
                                        BondARef2.Add(token);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 194, 1);
                            }
                            break;
                        case "order":
                            Debug.WriteLine("New bond order found.");

                            try
                            {
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                {
                                    Debug.WriteLine($"Token: {token}");
                                    Order.Add(token);
                                }
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 194, 1);
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case "integerArray":
                    Debug.WriteLine($"IntegerArray: builtin = {Builtin}");

                    switch (Builtin)
                    {
                        case "formalCharge":
                            try
                            {
                                var tokens = Strings.Tokenize(cData);
                                foreach (var token in tokens)
                                {
                                    Debug.WriteLine($"Charge added: {token}");
                                    FormalCharges.Add(token);
                                }
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 205, 1);
                            }
                            break;
                        default:
                            break;
                    }            
                    break;
                case "scalar":
                    if (xpath.EndsWith("crystal", "scalar"))
                    {
                        Debug.WriteLine($"Going to set a crystal parameter: {CrystalScalar} to {cData}");
                        try
                        {
                            UnitCellParams[CrystalScalar - 1] = double.Parse(cData.Trim(), NumberFormatInfo.InvariantInfo);
                        }
                        catch (FormatException)
                        {
                            Trace.TraceError($"Content must a float: {cData}");
                        }
                    }
                    else if (xpath.EndsWith("bond", "scalar"))
                    {
                        switch (DictRef)
                        {
                            case "mdl:stereo":
                                BondStereo.Add(cData.Trim());
                                StereoGiven = true;
                                break;
                            default:
                                if (!BondCustomProperty.TryGetValue(BondId[BondId.Count - 1], out Dictionary<string, string> bp))
                                {
                                    bp = new Dictionary<string, string>();
                                    BondCustomProperty[BondId[BondId.Count - 1]] = bp;
                                }
                                bp[ElementTitle] = cData.Trim();
                                break;
                        }
                    }
                    else if (xpath.EndsWith("atom", "scalar"))
                    {
                        switch (DictRef)
                        {
                            case "cdk:partialCharge":
                                PartialCharges.Add(cData.Trim());
                                break;
                            case "cdk:atomicNumber":
                                AtomicNumbers.Add(cData.Trim());
                                break;
                            case "cdk:aromaticAtom":
                                AtomAromaticities.Add(cData.Trim());
                                break;
                            case "cdk:isotopicMass":
                                ExactMasses.Add(cData.Trim());
                                break;
                            default:
                                if (!AtomCustomProperty.ContainsKey(AtomCounter - 1))
                                    AtomCustomProperty[AtomCounter - 1] = new List<string>();
                                AtomCustomProperty[AtomCounter - 1].Add(ElementTitle);
                                AtomCustomProperty[AtomCounter - 1].Add(cData.Trim());
                                break;
                        }
                    }
                    else if (xpath.EndsWith("molecule", "scalar"))
                    {
                        switch (DictRef)
                        {
                            case "pdb:id":
                                CurrentMolecule.SetProperty(new DictRef(DictRef, cData), cData);
                                break;
                            case "cdk:molecularProperty":
                                CurrentMolecule.SetProperty(ElementTitle, cData);
                                break;
                            default:
                                MoleculeCustomProperty.Add(ElementTitle);
                                MoleculeCustomProperty.Add(cData.Trim());
                                break;
                        }
                    }
                    else if (xpath.EndsWith("reaction", "scalar"))
                    {
                        switch (DictRef)
                        {
                            case "cdk:reactionProperty":
                                CurrentReaction.SetProperty(ElementTitle, cData);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        Trace.TraceWarning("Ignoring scalar: " + xpath);
                    }
                    break;
                case "floatArray":
                    switch (Builtin)
                    {
                        case "x3":
                            try
                            {
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                    X3.Add(token);
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 205, 1);
                            }
                            break;
                        case "y3":
                            try
                            {
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                    Y3.Add(token);
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 213, 1);
                            }
                            break;
                        case "z3":
                            try
                            {
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                    Z3.Add(token);
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 221, 1);
                            }
                            break;
                        case "x2":
                            Debug.WriteLine("New floatArray found.");

                            try
                            {
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                    X2.Add(token);
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 205, 1);
                            }
                            break;
                        case "y2":
                            Debug.WriteLine("New floatArray found.");

                            try
                            {
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                    Y2.Add(token);
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 454, 1);
                            }
                            break;
                        case "partialCharge":
                            Debug.WriteLine("New floatArray with partial charges found.");

                            try
                            {
                                var tokens = Strings.Tokenize(cData);

                                foreach (var token in tokens)
                                    PartialCharges.Add(token);
                            }
                            catch (Exception e)
                            {
                                Notify($"CMLParsing error: {e}", SYSTEMID, 462, 1);
                            }
                            break;
                    }
                    break;
                case "basic":
                    // assuming this is the child element of <identifier>
                    this.InChIString = cData;
                    break;
                case "name":
                    if (xpath.EndsWith("molecule", "name"))
                    {
                        if (DictRef.Length > 0)
                        {
                            CurrentMolecule.SetProperty(new DictRef(DictRef, cData), cData);
                        }
                        else
                        {
                            CurrentMolecule.Title = cData;
                        }
                    }
                    break;
                case "formula":
                    CurrentMolecule.SetProperty(CDKPropertyName.Formula, cData);
                    break;
                default:
                    Debug.WriteLine($"Skipping element: {name}");
                    break;
            }

            Builtin = "";
            ElementTitle = "";
        }

        public virtual void CharacterData(CMLStack xpath, XElement element)
        {
            Debug.WriteLine($"CD: {element.Value}");
        }

        protected virtual void Notify(string message, string systemId, int line, int column)
        {
            Debug.WriteLine($"Message: {message}");
            Debug.WriteLine($"SystemId: {systemId}");
            Debug.WriteLine($"Line: {line}");
            Debug.WriteLine($"Column: {column}");
        }

        protected virtual void StoreData()
        {
            if (InChIString != null)
            {
                CurrentMolecule.SetProperty(CDKPropertyName.InChI, InChIString);
            }
            if (Formula != null && Formula.Count > 0)
            {
                CurrentMolecule.SetProperty(CDKPropertyName.Formula, Formula);
            }
            var customs = MoleculeCustomProperty.GetEnumerator();

            while (customs.MoveNext())
            {
                var x = customs.Current;
                customs.MoveNext();
                var y = customs.Current;
                CurrentMolecule.SetProperty(x, y);
            }
            StoreAtomData();
            NewAtomData();
            StoreBondData();
            NewBondData();
            ConvertCMLToCDKHydrogenCounts();
        }

        private void ConvertCMLToCDKHydrogenCounts()
        {
            foreach (var atom in CurrentMolecule.Atoms)
            {
                if (atom.ImplicitHydrogenCount != null)
                {
                    var explicitHCount = AtomContainerManipulator.CountExplicitHydrogens(CurrentMolecule, atom);
                    if (explicitHCount != 0)
                    {
                        atom.ImplicitHydrogenCount = atom.ImplicitHydrogenCount - explicitHCount;
                    }
                }
            }
        }

        protected virtual void StoreAtomData()
        {
            Debug.WriteLine($"No atoms: {AtomCounter}");
            if (AtomCounter == 0)
            {
                return;
            }

            bool hasID = false;
            bool has3D = false;
            bool has3Dfract = false;
            bool has2D = false;
            bool hasFormalCharge = false;
            bool hasAtomAromaticities = false;
            bool hasPartialCharge = false;
            bool hasHCounts = false;
            bool hasSymbols = false;
            bool hasTitles = false;
            bool hasIsotopes = false;
            bool hasAtomicNumbers = false;
            bool hasExactMasses = false;
            bool hasDictRefs = false;
            bool hasSpinMultiplicities = false;
            bool hasAtomParities = false;
            bool hasOccupancies = false;

            if (ElId.Count == AtomCounter)
            {
                hasID = true;
            }
            else
            {
                Debug.WriteLine($"No atom ids: {ElId.Count} != {AtomCounter}");
            }

            if (ElSym.Count == AtomCounter)
            {
                hasSymbols = true;
            }
            else
            {
                Debug.WriteLine($"No atom symbols: {ElSym.Count} != {AtomCounter}");
            }

            if (ElTitles.Count == AtomCounter)
            {
                hasTitles = true;
            }
            else
            {
                Debug.WriteLine($"No atom titles: {ElTitles.Count} != {AtomCounter}");
            }

            if ((X3.Count == AtomCounter) && (Y3.Count == AtomCounter) && (Z3.Count == AtomCounter))
            {
                has3D = true;
            }
            else
            {
                Debug.WriteLine($"No 3D info: {X3.Count} {Y3.Count} {Z3.Count} != {AtomCounter}");
            }

            if ((XFract.Count == AtomCounter) && (YFract.Count == AtomCounter) && (ZFract.Count == AtomCounter))
            {
                has3Dfract = true;
            }
            else
            {
                Debug.WriteLine($"No 3D fractional info: {XFract.Count} {YFract.Count} {ZFract.Count} != {AtomCounter}");
            }

            if ((X2.Count == AtomCounter) && (Y2.Count == AtomCounter))
            {
                has2D = true;
            }
            else
            {
                Debug.WriteLine($"No 2D info: {X2.Count} {Y2.Count} != {AtomCounter}");
            }

            if (FormalCharges.Count == AtomCounter)
            {
                hasFormalCharge = true;
            }
            else
            {
                Debug.WriteLine($"No formal Charge info: {FormalCharges.Count} != {AtomCounter}");
            }

            if (AtomAromaticities.Count == AtomCounter)
            {
                hasAtomAromaticities = true;
            }
            else
            {
                Debug.WriteLine($"No aromatic atom info: {AtomAromaticities.Count} != {AtomCounter}");
            }

            if (PartialCharges.Count == AtomCounter)
            {
                hasPartialCharge = true;
            }
            else
            {
                Debug.WriteLine($"No partial Charge info: {PartialCharges.Count} != {AtomCounter}");
            }

            if (HCounts.Count == AtomCounter)
            {
                hasHCounts = true;
            }
            else
            {
                Debug.WriteLine($"No hydrogen Count info: {HCounts.Count} != {AtomCounter}");
            }

            if (SpinMultiplicities.Count == AtomCounter)
            {
                hasSpinMultiplicities = true;
            }
            else
            {
                Debug.WriteLine($"No spinMultiplicity info: {SpinMultiplicities.Count} != {AtomCounter}");
            }

            if (AtomParities.Count == AtomCounter)
            {
                hasAtomParities = true;
            }
            else
            {
                Debug.WriteLine($"No atomParity info: {SpinMultiplicities.Count} != {AtomCounter}");
            }

            if (Occupancies.Count == AtomCounter)
            {
                hasOccupancies = true;
            }
            else
            {
                Debug.WriteLine($"No occupancy info: {Occupancies.Count} != {AtomCounter}");
            }

            if (AtomDictRefs.Count == AtomCounter)
            {
                hasDictRefs = true;
            }
            else
            {
                Debug.WriteLine($"No dictRef info: {AtomDictRefs.Count} != {AtomCounter}");
            }

            if (Isotope.Count == AtomCounter)
            {
                hasIsotopes = true;
            }
            else
            {
                Debug.WriteLine($"No isotope info: {Isotope.Count} != {AtomCounter}");
            }
            if (AtomicNumbers.Count == AtomCounter)
            {
                hasAtomicNumbers = true;
            }
            else
            {
                Debug.WriteLine($"No atomicNumbers info: {AtomicNumbers.Count} != {AtomCounter}");
            }
            if (ExactMasses.Count == AtomCounter)
            {
                hasExactMasses = true;
            }
            else
            {
                Debug.WriteLine($"No atomicNumbers info: {AtomicNumbers.Count} != {AtomCounter}");
            }

            for (int i = 0; i < AtomCounter; i++)
            {
                Trace.TraceInformation($"Storing atom: {i}");
                CurrentAtom = CurrentChemFile.Builder.NewAtom("H");
                Debug.WriteLine($"Atom # {AtomCounter}");
                if (hasID)
                {
                    Debug.WriteLine($"id: {ElId[i]}");
                    CurrentAtom.Id = ElId[i];
                    AtomEnumeration[ElId[i]] = CurrentAtom;
                }
                if (hasTitles)
                {
                    if (hasSymbols)
                    {
                        var symbol = ElSym[i];
                        switch (symbol)
                        {
                            case "Du":
                            case "Dummy":
                                if (!(CurrentAtom is IPseudoAtom))
                                {
                                    CurrentAtom = CurrentChemFile.Builder.NewPseudoAtom(CurrentAtom);
                                    if (hasID) AtomEnumeration[ElId[i]] = CurrentAtom;
                                }
                                ((IPseudoAtom)CurrentAtom).Label = ElTitles[i];
                                break;
                            default:
                                if (ElTitles[i] != null)
                                    CurrentAtom.SetProperty(CDKPropertyName.Title, ElTitles[i]);
                                break;
                        }
                    }
                    else
                    {
                        if (ElTitles[i] != null)
                            CurrentAtom.SetProperty(CDKPropertyName.Title, ElTitles[i]);
                    }
                }

                // store optional atom properties
                if (hasSymbols)
                {
                    string symbol = ElSym[i];
                    switch (symbol)
                    {
                        case "Du":
                        case "Dummy":
                            symbol = "R";
                            break;
                        case "R":
                            if (!(CurrentAtom is IPseudoAtom))
                            {
                                CurrentAtom = CurrentChemFile.Builder.NewPseudoAtom(CurrentAtom);
                                ((IPseudoAtom)CurrentAtom).Label = "R";
                                if (hasID)
                                    AtomEnumeration[ElId[i]] = CurrentAtom;
                            }
                            break;
                        default:
                            break;
                    }
                    CurrentAtom.Symbol = symbol;
                    if (!hasAtomicNumbers || AtomicNumbers[i] == null)
                        CurrentAtom.AtomicNumber = PeriodicTable.GetAtomicNumber(symbol);
                }

                if (has3D)
                {
                    if (X3[i] != null && Y3[i] != null && Z3[i] != null)
                    {
                        CurrentAtom.Point3D = new Vector3(
                            double.Parse(X3[i], NumberFormatInfo.InvariantInfo),
                            double.Parse(Y3[i], NumberFormatInfo.InvariantInfo),
                            double.Parse(Z3[i], NumberFormatInfo.InvariantInfo));
                    }
                }

                if (has3Dfract)
                {
                    CurrentAtom.FractionalPoint3D = new Vector3(
                        double.Parse(XFract[i], NumberFormatInfo.InvariantInfo),
                        double.Parse(YFract[i], NumberFormatInfo.InvariantInfo),
                        double.Parse(ZFract[i], NumberFormatInfo.InvariantInfo));
                }

                if (hasFormalCharge)
                {
                    CurrentAtom.FormalCharge = int.Parse(FormalCharges[i], NumberFormatInfo.InvariantInfo);
                }

                if (hasAtomAromaticities)
                {
                    if (AtomAromaticities[i] != null)
                        CurrentAtom.IsAromatic = true;
                }

                if (hasPartialCharge)
                {
                    Debug.WriteLine("Storing partial atomic charge...");
                    CurrentAtom.Charge = double.Parse(PartialCharges[i], NumberFormatInfo.InvariantInfo);
                }

                if (hasHCounts)
                {
                    string hCount = HCounts[i];
                    if (hCount != null)
                    {
                        CurrentAtom.ImplicitHydrogenCount = int.Parse(hCount, NumberFormatInfo.InvariantInfo);
                    }
                    else
                    {
                        CurrentAtom.ImplicitHydrogenCount = null;
                    }
                }

                if (has2D)
                {
                    if (X2[i] != null && Y2[i] != null)
                    {
                        CurrentAtom.Point2D = new Vector2(
                            double.Parse(X2[i], NumberFormatInfo.InvariantInfo),
                            double.Parse(Y2[i], NumberFormatInfo.InvariantInfo));
                    }
                }

                if (hasDictRefs)
                {
                    if (AtomDictRefs[i] != null)
                        CurrentAtom.SetProperty("org.openscience.cdk.dict", AtomDictRefs[i]);
                }

                if (hasSpinMultiplicities && SpinMultiplicities[i] != null)
                {
                    int unpairedElectrons = int.Parse(SpinMultiplicities[i], NumberFormatInfo.InvariantInfo) - 1;
                    for (int sm = 0; sm < unpairedElectrons; sm++)
                    {
                        CurrentMolecule.SingleElectrons.Add(CurrentChemFile.Builder.NewSingleElectron(CurrentAtom));
                    }
                }

                if (hasOccupancies && Occupancies[i] != null)
                {
                    // FIXME: this has no ChemFileCDO equivalent, not even if spelled correctly
                }

                if (hasIsotopes)
                {
                    if (Isotope[i] != null)
                        CurrentAtom.MassNumber = (int)double.Parse(Isotope[i], NumberFormatInfo.InvariantInfo);
                }

                if (hasAtomicNumbers)
                {
                    if (AtomicNumbers[i] != null)
                        CurrentAtom.AtomicNumber = int.Parse(AtomicNumbers[i], NumberFormatInfo.InvariantInfo);
                }

                if (hasExactMasses)
                {
                    if (ExactMasses[i] != null)
                        CurrentAtom.ExactMass = double.Parse(ExactMasses[i], NumberFormatInfo.InvariantInfo);
                }

                if (AtomCustomProperty.TryGetValue(i, out List<string> property))
                {
                    var it = property.GetEnumerator();
                    while (it.MoveNext())
                    {
                        var p1 = it.Current;
                        it.MoveNext();
                        var p2 = it.Current;
                        CurrentAtom.SetProperty(p1, p2);
                    }
                }
                CurrentMolecule.Atoms.Add(CurrentAtom);
            }

            for (int i = 0; i < AtomCounter; i++)
            {
                if (hasAtomParities && AtomParities[i] != null && AtomParities[i].Any())
                {
                    var ligandAtom1 = AtomEnumeration[ParityARef1[i]];
                    var ligandAtom2 = AtomEnumeration[ParityARef2[i]];
                    var ligandAtom3 = AtomEnumeration[ParityARef3[i]];
                    var ligandAtom4 = AtomEnumeration[ParityARef4[i]];
                    var ligandAtoms = new IAtom[] { ligandAtom1, ligandAtom2, ligandAtom3, ligandAtom4 };
                    TetrahedralStereo config;
                    int parity = 0;
                    try
                    {
                        parity = Math.Sign(double.Parse(AtomParities[i]));
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    if (parity > 0)
                        config = TetrahedralStereo.Clockwise;
                    else if (parity < 0)
                        config = TetrahedralStereo.AntiClockwise;
                    else
                    {
                        config = TetrahedralStereo.Unset;
                        Trace.TraceWarning($"Cannot interpret stereo information, invalid parity: '{AtomParities[i]}'");
                    }
                    if (!config.IsUnset())
                    {
                        TetrahedralChirality chirality = new TetrahedralChirality(CurrentMolecule.Atoms[i], ligandAtoms, config);
                        CurrentMolecule.StereoElements.Add(chirality);
                    }
                }
            }

            if (ElId.Count > 0)
            {
                // assume this is the current working list
                BondElId = ElId;
            }
        }

        protected virtual void StoreBondData()
        {
            Debug.WriteLine($"Testing a1,a2,stereo,order = count: {BondARef1.Count}, {BondARef2.Count}, {BondStereo.Count}, {Order.Count} = {BondCounter}");

            if ((BondARef1.Count == BondCounter) && (BondARef2.Count == BondCounter))
            {
                Debug.WriteLine("About to add bond info...");

                var orders = Order.GetEnumerator();
                var ids = BondId.GetEnumerator();
                var bar1s = BondARef1.GetEnumerator();
                var bar2s = BondARef2.GetEnumerator();
                var stereos = BondStereo.GetEnumerator();
                var aroms = BondAromaticity.GetEnumerator();

                while (bar1s.MoveNext())
                {
                    bar2s.MoveNext();
                    var a1 =AtomEnumeration[bar1s.Current];
                    var a2 =AtomEnumeration[bar2s.Current];
                    CurrentBond = CurrentChemFile.Builder.NewBond(a1, a2);
                    if (ids.MoveNext())
                    {
                        CurrentBond.Id = ids.Current;
                    }

                    if (orders.MoveNext())
                    {
                        var bondOrder = orders.Current;

                        switch (bondOrder)
                        {
                            case "S":
                                CurrentBond.Order = BondOrder.Single;
                                break;
                            case "D":
                                CurrentBond.Order = BondOrder.Double;
                                break;
                            case "T":
                                CurrentBond.Order = BondOrder.Triple;
                                break;
                            case "A":
                                CurrentBond.Order = BondOrder.Single;
                                CurrentBond.IsAromatic = true;
                                break;
                            default:
                                CurrentBond.Order = BondManipulator.CreateBondOrder(double.Parse(bondOrder, NumberFormatInfo.InvariantInfo));
                                break;
                        }
                    }

                    if (stereos.MoveNext())
                    {
                        var nextStereo = stereos.Current;
                        switch (nextStereo)
                        {
                            case "H":
                                CurrentBond.Stereo = NCDK.BondStereo.Down;
                                break;
                            case "W":
                                CurrentBond.Stereo = NCDK.BondStereo.Up;
                                break;
                            default:
                                if (nextStereo != null && nextStereo.Any())
                                {
                                    Trace.TraceWarning($"Cannot interpret bond display information: '{nextStereo}'");
                                }
                                break;
                        }
                    }

                    if (aroms.MoveNext())
                    {
                        var nextArom = aroms.Current;
                        if (nextArom != null && nextArom.Value)
                        {
                            CurrentBond.IsAromatic = true;
                        }
                    }

                    if (CurrentBond.Id != null)
                    {
                        if (BondCustomProperty.TryGetValue(CurrentBond.Id, out Dictionary<string, string> currentBondProperties))
                        {
                            foreach (var key in currentBondProperties.Keys)
                            {
                                CurrentBond.SetProperty(key, currentBondProperties[key]);
                            }
                            BondCustomProperty.Remove(CurrentBond.Id);
                        }
                    }

                    CurrentMolecule.Bonds.Add(CurrentBond);
                }
            }
        }

        protected virtual int AddArrayElementsTo(IList<string> toAddto, string array)
        {
            int i = 0;
            var tokens = Strings.Tokenize(array);
            foreach (var token in tokens)
            {
                toAddto.Add(token);
                i++;
            }
            return i;
        }
    }
}
