/*
 * Copyright (C) 2018  Kazuya Ujihara <ujihara.kazuya@gmail.com>
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

using NCDK.Aromaticities;
using NCDK.Geometries;
using NCDK.Graphs.InChI;
using NCDK.Isomorphisms;
using NCDK.Isomorphisms.Matchers;
using NCDK.Layout;
using NCDK.Renderers;
using NCDK.Renderers.Colors;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using WPF = System.Windows;

namespace NCDK.MolViewer
{
    partial class AppearanceViewModel 
        : BindableBase
    {
        private static readonly IChemObjectBuilder builder = CDK.Builder;
        private static StructureDiagramGenerator sdg = new StructureDiagramGenerator();
        private static SmilesParser parser = new SmilesParser(builder);
        private static SmilesGenerator smilesGenerator = new SmilesGenerator(SmiFlavors.Default);

        private string _Smiles = null;
        private IChemObject _ChemObject = null;
        private ColoringStyle _Coloring = ColoringStyle.COW;
        private DelegateCommand _CleanStructureCommand;
        private DelegateCommand _PasteAsInChICommand;
        private DelegateCommand _SanitizeCommand;

        public string Smiles
        {
            get { return _Smiles; }

            set
            {
                if (this._Smiles != value)
                {
                    var text = value;
                    if (string.IsNullOrWhiteSpace(text))
                        return;

                    IChemObject o;

                    if (IsReactionSmilees(text))
                    {
                        IReaction rxn = null;
                        try
                        {
                            rxn = parser.ParseReactionSmiles(text);
                            ReactionManipulator.PerceiveDativeBonds(rxn);
                            ReactionManipulator.PerceiveRadicals(rxn);
                        }
                        catch (Exception)
                        {
                            // ignore
                        }

                        o = rxn;
                    }
                    else
                    {
                        IAtomContainer mol = null;
                        try
                        {
                            mol = parser.ParseSmiles(text);
                            AtomContainerManipulator.PerceiveDativeBonds(mol);
                            AtomContainerManipulator.PerceiveRadicals(mol);
                        }
                        catch (Exception)
                        {
                            // ignore
                        }

                        o = mol;
                    }

                    if (o != null)
                    {
                        _ChemObject = o;
                        this.RaisePropertyChanged(nameof(ChemObject));
                    }

                    this.SetProperty(ref this._Smiles, text);
                }
            }
        }

        public IChemObject ChemObject
        {
            get => _ChemObject;
            set
            {
                switch (value)
                {
                    case IAtomContainer mol:
                        if (!GeometryUtil.Has2DCoordinates(mol))
                        {
                            mol = (IAtomContainer)mol.Clone();
                            sdg.GenerateCoordinates(mol);
                            value = mol;
                        }
                        break;
                    case IReaction rxn:
                        if (!GeometryUtil.Has2DCoordinates(rxn))
                        {
                            rxn = (IReaction)rxn.Clone();
                            sdg.GenerateCoordinates(rxn);
                            value = rxn;
                        }
                        break;
                    default:
                        break;
                }

                this.SetProperty(ref this._ChemObject, value);
                OnChemObjectChanged();
            }
        }

        public ColoringStyle Coloring
        {
            get { return _Coloring; }
            set
            {
                this.SetProperty(ref this._Coloring, value);

                switch (value)
                {
                    case ColoringStyle.COW:
                        AtomColorer = new CDK2DAtomColors();
                        BackgroundColor = WPF.Media.Colors.White;
                        Highlighting = HighlightStyle.OuterGlow;
                        OuterGlowWidth = 4;
                        break;
                    case ColoringStyle.COT:
                        AtomColorer = new CDK2DAtomColors();
                        BackgroundColor = WPF.Media.Colors.Transparent;
                        Highlighting = HighlightStyle.OuterGlow;
                        OuterGlowWidth = 4;
                        break;
                    case ColoringStyle.BOW:
                        AtomColorer = new UniColor(WPF.Media.Colors.Black);
                        BackgroundColor = WPF.Media.Colors.White;
                        Highlighting = HighlightStyle.None;
                        OuterGlowWidth = RendererModelTools.DefaultOuterGlowWidth;
                        break;
                    case ColoringStyle.BOT:
                        AtomColorer = new UniColor(WPF.Media.Colors.Black);
                        BackgroundColor = WPF.Media.Colors.Transparent;
                        Highlighting = HighlightStyle.None;
                        OuterGlowWidth = RendererModelTools.DefaultOuterGlowWidth;
                        break;
                    case ColoringStyle.WOB:
                        AtomColorer = new UniColor(WPF.Media.Colors.White);
                        BackgroundColor = WPF.Media.Colors.Black;
                        Highlighting = HighlightStyle.None;
                        OuterGlowWidth = RendererModelTools.DefaultOuterGlowWidth;
                        break;
                    case ColoringStyle.COB:
                        AtomColorer = new CobColorer();
                        BackgroundColor = WPF.Media.Colors.Transparent;
                        Highlighting = HighlightStyle.OuterGlow;
                        OuterGlowWidth = 4;
                        break;
                    case ColoringStyle.NOB:
                        AtomColorer = new NobColorer();
                        BackgroundColor = WPF.Media.Colors.Black;
                        Highlighting = HighlightStyle.OuterGlow;
                        OuterGlowWidth = 4;
                        break;
                }
            }
        }

        public DelegateCommand CleanStructureCommand
        {
            get { return _CleanStructureCommand = _CleanStructureCommand ?? new DelegateCommand(CleanStructure); }
        }

        private void CleanStructure()
        {
            if (ChemObject == null)
                return;

            switch (ChemObject)
            {
                case IAtomContainer o:
                    o = (IAtomContainer)o.Clone();
                    sdg.GenerateCoordinates(o);
                    ChemObject = o;
                    break;
                case IReaction o:
                    o = (IReaction)o.Clone();
                    sdg.GenerateCoordinates(o);
                    ChemObject = o;
                    break;
                default:
                    Trace.TraceWarning($"'{ChemObject.GetType()}' is not supported.");
                    break;
            }
        }

        public DelegateCommand PasteAsInChICommand
        {
            get { return _PasteAsInChICommand = _PasteAsInChICommand ?? new DelegateCommand(PasteAsInChI); }
        }

        public DelegateCommand SanitizeCommand
        {
            get { return _SanitizeCommand = _SanitizeCommand ?? new DelegateCommand(Sanitize); }
        }

        static class AmideSanitizer
        {
            static readonly QueryAtom c1 = new QueryAtom(ExprType.AliphaticElement, AtomicNumbers.C);
            static readonly QueryAtom n1 = new QueryAtom(ExprType.AliphaticElement, AtomicNumbers.N);
            static readonly QueryAtom c2 = new QueryAtom(ExprType.AliphaticElement, AtomicNumbers.C);
            static readonly QueryAtom a1 = new QueryAtom(
                    new Expr(ExprType.AliphaticElement, AtomicNumbers.C)
                .Or(new Expr(ExprType.AliphaticElement, AtomicNumbers.N)));
            static readonly QueryAtom o1 = new QueryAtom(ExprType.AliphaticElement, AtomicNumbers.O);
            static readonly QueryBond b1 = new QueryBond(c1, n1, ExprType.AliphaticOrder, 1);
            static readonly QueryBond bcn = new QueryBond(n1, c2, new Expr(ExprType.AliphaticOrder, 2));
            static readonly QueryBond bco = new QueryBond(c2, o1, ExprType.AliphaticOrder, 1);
            static readonly QueryBond bca = new QueryBond(c2, a1, new Expr(ExprType.AliphaticOrder, 1));
            static readonly QueryAtomContainer query = new QueryAtomContainer(CDK.Builder.NewAtomContainer(new[] { c1, n1, c2, o1, a1 }, new[] { b1, bcn, bco, bca }));
            static readonly Pattern finder = Pattern.CreateSubstructureFinder(query);

            /// <summary>
            /// Sanitize amide bonds in <paramref name="mol"/>.
            /// </summary>
            /// <param name="mol">The molecule to sanitize.</param>
            /// <returns>The sanitized molecule </returns>
            public static IAtomContainer Sanitize(IAtomContainer mol)
            {
                mol = (IAtomContainer)mol.Clone();
                var arm = (IAtomContainer)mol.Clone();
                AtomContainerManipulator.PercieveAtomTypesAndConfigureAtoms(arm);
                Aromaticity.CDKLegacy.Apply(arm);

                for (int i = 0; i < mol.Atoms.Count; i++)
                {
                    mol.Atoms[i].IsVisited = false;

                    if (arm.Atoms[i].IsAromatic)
                        mol.Atoms[i].IsVisited = true;
                }

                for (int i = 0; i < mol.Bonds.Count; i++)
                    mol.Bonds[i].IsVisited = false;

                var ma = finder.MatchAll(mol);
                var atomMaps = ma.ToAtomMaps().ToList();
                var bondMaps = ma.ToBondMaps().ToList();

                for (int i = 0; i < atomMaps.Count; i++)
                {
                    var atomMap = atomMaps[i];
                    var bondMap = bondMaps[i];

                    if (!atomMap[n1].IsVisited && !atomMap[o1].IsVisited
                     && !bondMap[bcn].IsVisited && !bondMap[bco].IsVisited)
                    {
                        IAtom hydrogenAtomToRemove = null;
                        var to1 = atomMap[o1];
                        if (to1.ImplicitHydrogenCount != null && to1.ImplicitHydrogenCount > 0)
                        {
                            to1.ImplicitHydrogenCount = to1.ImplicitHydrogenCount.Value - 1;
                        }
                        else
                        {
                            foreach (var bond in to1.Bonds)
                            {
                                foreach (var atom in bond.Atoms)
                                {
                                    if (atom == to1)
                                        continue;
                                    if (atom.AtomicNumber == 1)
                                    {
                                        // found H
                                        hydrogenAtomToRemove = atom;
                                        goto HFound;
                                    }
                                }
                            }
                        HFound:
                            ;
                        }
                        if (hydrogenAtomToRemove != null)
                            mol.RemoveAtom(hydrogenAtomToRemove);

                        mol.RemoveBond(atomMap[c2], atomMap[n1]);
                        mol.RemoveBond(atomMap[c2], atomMap[o1]);
                        mol.AddBond(atomMap[c2], atomMap[n1], BondOrder.Single);
                        mol.AddBond(atomMap[c2], atomMap[o1], BondOrder.Double);
                        var bnc = mol.GetBond(atomMap[n1], atomMap[c1]);
                        bnc.Order = BondOrder.Single;
                        bnc.Stereo = BondStereo.None;
                        var bca = mol.GetBond(atomMap[c2], atomMap[a1]);
                        bca.Order = BondOrder.Single;
                        bca.Stereo = BondStereo.None;
                        atomMap[n1].ImplicitHydrogenCount = (atomMap[n1].ImplicitHydrogenCount ?? 0) + 1;
                        atomMap[n1].IsVisited = atomMap[o1].IsVisited = bondMap[bcn].IsVisited = bondMap[bco].IsVisited = true;
                    }
                }
                return mol;
            }

            public static IReaction Sanitize(IReaction reaction)
            {
                var rxnmol = ReactionManipulator.ToMolecule(reaction);
                rxnmol = Sanitize(rxnmol);
                return ReactionManipulator.ToReaction(rxnmol);
            }
        }

        private void Sanitize()
        {
            try
            {
                switch (ChemObject)
                {
                    case IAtomContainer mol:
                        ChemObject = AmideSanitizer.Sanitize(mol);
                        break;
                    case IReaction rxn:
                        ChemObject = AmideSanitizer.Sanitize(rxn);
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                Trace.TraceInformation(e.Message);
            }
        }

        private void PasteAsInChI()
        {
            IAtomContainer mol = null;
            if (Clipboard.ContainsText())
            {
                var text = Clipboard.GetText();
                try
                {
                    // Get InChIToStructure
                    var converter =  InChIToStructure.FromInChI(text, builder);
                    mol = converter.AtomContainer;
                }
                catch (Exception)
                {
                    // ignore
                }
            }
            if (mol == null)
                return;

            ChemObject = mol;
        }

        private void OnChemObjectChanged()
        {
            string smiles = null;

            try
            {
                switch (ChemObject)
                {
                    case IAtomContainer mol:
                        smiles = smilesGenerator.Create(mol);
                        break;
                    case IReaction rxn:
                        smiles = smilesGenerator.Create(rxn);
                        break;
                    default:
                        smiles = $"{ChemObject.GetType()} is not supported.";
                        break;
                }
            }
            catch (Exception e)
            {
                smiles = $"Failed to create SMILES: {e.Message}";
            }

            _Smiles = smiles; // not to change ChemObject
            base.RaisePropertyChanged(nameof(Smiles));
        }

        private static bool IsReactionSmilees(string smiles)
        {
            return smiles.Split(' ')[0].Contains(">");
        }
    }

    class CobColorer : IAtomColorer
    {
        private static readonly CDK2DAtomColors colors = new CDK2DAtomColors();

        public WPF.Media.Color GetAtomColor(IAtom atom)
        {
            var res = colors.GetAtomColor(atom);
            if (res.Equals(WPF.Media.Colors.Black))
                return WPF.Media.Colors.White;
            else
                return res;
        }

        public WPF.Media.Color GetAtomColor(IAtom atom, WPF.Media.Color color)
        {
            var res = colors.GetAtomColor(atom, color);
            if (res.Equals(WPF.Media.Colors.Black))
                return WPF.Media.Colors.White;
            else
                return res;
        }
    }

    class NobColorer : IAtomColorer
    {
        private static readonly CDK2DAtomColors colors = new CDK2DAtomColors();
        private static readonly WPF.Media.Color Neon = WPF.Media.Color.FromRgb(0x00, 0xFF, 0x0E);

        public WPF.Media.Color GetAtomColor(IAtom atom)
        {
            var res = colors.GetAtomColor(atom);
            if (res.Equals(WPF.Media.Colors.Black))
                return Neon;
            else
                return res;
        }

        public WPF.Media.Color GetAtomColor(IAtom atom, WPF.Media.Color color)
        {
            var res = colors.GetAtomColor(atom, color);
            if (res.Equals(WPF.Media.Colors.Black))
                return Neon;
            else
                return res;
        }
    }

    public enum ColoringStyle
    {
        COW, 
        COT,
        BOW,
        BOT,
        WOB,
        COB,
        NOB,
    }

    public class ColoringStyleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((ColoringStyle)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ToEnum(value);
        }

        private static ColoringStyle ToEnum(object parameter)
        {
            if (parameter is string p)
                return (ColoringStyle)Enum.Parse(typeof(ColoringStyle), p);
            return (ColoringStyle)0;
        }
    }

    public class HighlightingStyleToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ((HighlightStyle)value).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return ToEnum(value);
        }

        private static HighlightStyle ToEnum(object parameter)
        {
            if (parameter is string p)
                return (HighlightStyle)Enum.Parse(typeof(HighlightStyle), p);
            return (HighlightStyle)0;
        }
    }

    public class F2Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is double))
                throw new ApplicationException();
            return ((double)value).ToString("F2");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string))
                throw new ApplicationException();
            return double.Parse((string)value);
        }
    }

    public class Power10Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is double))
                throw new ApplicationException();
            return Math.Log10((double)value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is double))
                throw new ApplicationException();
            return Math.Pow(10, (double)value);
        }
    }
}
