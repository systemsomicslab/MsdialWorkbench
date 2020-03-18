/* Copyright (C) 2015  The Chemistry Development Kit (CDK) project
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

using NCDK.Common.Collections;
using NCDK.Geometries;
using NCDK.Layout;
using NCDK.Numerics;
using NCDK.Renderers;
using NCDK.Renderers.Colors;
using NCDK.Renderers.Elements;
using NCDK.Renderers.Generators;
using NCDK.Renderers.Generators.Standards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using WPF = System.Windows;

namespace NCDK.Depict
{
    /// <summary>
    /// A high-level API for depicting molecules and reactions.
    /// </summary>
    /// <remarks>
    ///  <see cref="DepictionGenerator"/> in CDK is immutable but not in NCDK.
    /// </remarks>
    /// <example>
    /// <b>General Usage</b>
    /// <para>
    /// Create a generator and reuse it for multiple depictions. 
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Depict.DepictionGenerator_Example.cs+1"]/*' />
    /// </para>
    /// <b>One Line Quick Use</b>
    /// <para>
    /// For simplified use we can create a generator and use it once for a single depiction.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Depict.DepictionGenerator_Example.cs+2"]/*' />
    /// </para>
    /// <para>
    /// The intermediate <see cref="Depiction"/> object can write to many different formats
    /// through a variety of API calls.
    /// <include file='IncludeExamples.xml' path='Comments/Codes[@id="NCDK.Depict.DepictionGenerator_Example.cs+3"]/*' />
    /// </para>
    /// </example>
    // @author John may 
    public sealed class DepictionGenerator
    {
        /// <summary>
        /// Visually distinct colors for highlighting.
        /// http://stackoverflow.com/a/4382138
        /// Kenneth L. Kelly and Deanne B. Judd.
        /// "Color: Universal Language and Dictionary of Names",/:
        /// National Bureau of Standards,
        /// Spec. Publ. 440, Dec. 1976, 189 pages.
        /// </summary>
        public static readonly IReadOnlyList<Color> KellyMaxContrast = new Color[]
        {
            Color.FromRgb(0x00, 0x53, 0x8A), // Strong Blue (sub-optimal for defective color vision)
            Color.FromRgb(0x93, 0xAA, 0x00), // Vivid Yellowish Green (sub-optimal for defective color vision)
            Color.FromRgb(0xC1, 0x00, 0x20), // Vivid Red
            Color.FromRgb(0xFF, 0xB3, 0x00), // Vivid Yellow
            Color.FromRgb(0x00, 0x7D, 0x34), // Vivid Green (sub-optimal for defective color vision)
            Color.FromRgb(0xFF, 0x68, 0x00), // Vivid Orange
            Color.FromRgb(0xCE, 0xA2, 0x62), // Grayish Yellow
            Color.FromRgb(0x81, 0x70, 0x66), // Medium Gray
            Color.FromRgb(0xA6, 0xBD, 0xD7), // Very Light Blue
            Color.FromRgb(0x80, 0x3E, 0x75), // Strong Purple

            Color.FromRgb(0xF6, 0x76, 0x8E), // Strong Purplish Pink (sub-optimal for defective color vision)

            Color.FromRgb(0xFF, 0x7A, 0x5C), // Strong Yellowish Pink (sub-optimal for defective color vision)
            Color.FromRgb(0x53, 0x37, 0x7A), // Strong Violet (sub-optimal for defective color vision)
            Color.FromRgb(0xFF, 0x8E, 0x00), // Vivid Orange Yellow (sub-optimal for defective color vision)
            Color.FromRgb(0xB3, 0x28, 0x51), // Strong Purplish Red (sub-optimal for defective color vision)
            Color.FromRgb(0xF4, 0xC8, 0x00), // Vivid Greenish Yellow (sub-optimal for defective color vision)
            Color.FromRgb(0x7F, 0x18, 0x0D), // Strong Reddish Brown (sub-optimal for defective color vision)

            Color.FromRgb(0x59, 0x33, 0x15), // Deep Yellowish Brown (sub-optimal for defective color vision)
            Color.FromRgb(0xF1, 0x3A, 0x13), // Vivid Reddish Orange (sub-optimal for defective color vision)
            Color.FromRgb(0x23, 0x2C, 0x16), // Dark Olive Green (sub-optimal for defective color vision)
        };

        /// <summary>
        /// Magic value for indicating automatic parameters. These can
        /// be overridden by a caller.
        /// </summary>
        public const double Automatic = double.NegativeInfinity;

        /// <summary>
        /// Default margin for vector graphics formats.
        /// </summary>
        public static readonly double DefaultMillimeterMargin = 0.56;

        /// <summary>
        /// Default margin for raster graphics formats.
        /// </summary>
        public static readonly double DefaultPixelMargin = 4;

        /// <summary>
        /// The dimensions (width x height) of the depiction.
        /// </summary>
        private Dimensions dimensions = Dimensions.Automatic;

        /// <summary>
        /// Font used for depictions.
        /// </summary>
        private readonly Typeface font;

        private readonly double emSize;

        /// <summary>
        /// Diagram generators.
        /// </summary>
        private readonly List<IGenerator<IAtomContainer>> generators = new List<IGenerator<IAtomContainer>>();

        /// <summary>
        /// Flag to indicate atom numbers should be displayed.
        /// </summary>
        private bool annotateAtomNumbers = false;

        /// <summary>
        /// Flag to indicate atom values should be displayed.
        /// </summary>
        private bool annotateAtomValues = false;

        /// <summary>
        /// Flag to indicate atom maps should be displayed.
        /// </summary>
        private bool annotateAtomMapNumbers = false;

        /// <summary>
        /// Colors to use in atom-map highlighting. 
        /// </summary>
        private IReadOnlyList<Color> atomMapColors = null;

        /// <summary>
        /// Reactions are aligned such that mapped atoms have the same coordinates on the left/right.
        /// </summary>
        private bool alignMappedReactions = true;

        RendererModel templateModel = new RendererModel();

        /// <summary>
        /// Create a depiction generator using the standard sans-serif
        /// system font.
        /// </summary>
        public DepictionGenerator()
            : this(
                  new Typeface(
                      new FontFamily("Arial"),
                      WPF.FontStyles.Normal,
                      WPF.FontWeights.Normal,
                      WPF.FontStretches.Normal),
                  13)
        {
            templateModel.SetBondLength(26.1);
            templateModel.SetHashSpacing(26.0 / 8);
            templateModel.SetWaveSpacing(26.0 / 8);
        }

        /// <summary>
        /// Create a depiction generator that will render atom
        /// labels using the specified AWT font.
        /// </summary>
        /// <param name="font">the font to use to display</param>
        public DepictionGenerator(Typeface font, double emSize)
        {
            generators.Add(new BasicSceneGenerator());
            generators.Add(new StandardGenerator(this.font = font, this.emSize = emSize));

            // default margin and separation is automatic
            // since it depends on raster (px) vs vector (mm)

            templateModel.SetMargin(Automatic);
            templateModel.SetPadding(Automatic);
        }

        /// <summary>
        /// Internal copy constructor.
        /// </summary>
        public DepictionGenerator Clone()
        {
            var clone = (DepictionGenerator)this.MemberwiseClone();
            // dimensions is stable
            this.generators.AddRange(this.generators);

            return clone;
        }

        private RendererModel CreateModel()
        {
            var model = new RendererModel();
            foreach (var p in templateModel.Parameters)
                model.Parameters.Add(p);
            return model;
        }

        /// <summary>
        /// Depict a single molecule.
        /// </summary>
        /// <param name="mol">molecule</param>
        /// <param name="highlight">highlight the provided set of atoms and bonds in the depiction in the specified color</param>
        /// <returns>depiction instance</returns>
        /// <exception cref="CDKException">a depiction could not be generated</exception>
        public Depiction Depict(IAtomContainer mol, IReadOnlyDictionary<IChemObject, Color> highlight = null)
        {
            return Depict(new[] { mol }, 1, 1, highlight);
        }

        /// <summary>
        /// Depict a set of molecules, they will be depicted in a grid. The grid
        /// size (nrow x ncol) is determined automatically based on the number
        /// molecules.
        /// </summary>
        /// <param name="mols">molecules</param>
        /// <returns>depiction</returns>
        /// <exception cref="CDKException">a depiction could not be generated</exception>
        /// <seealso cref="Depict(IEnumerable{IAtomContainer}, int, int, IReadOnlyDictionary{IChemObject, Color})"/>
        public Depiction Depict(IEnumerable<IAtomContainer> mols, IReadOnlyDictionary<IChemObject, Color> highlight = null)
        {
            var molList = mols.ToList();
            var grid = Dimensions.DetermineGrid(molList.Count());
            return Depict(mols, grid.Height, grid.Width, highlight);
        }

        /// <summary>
        /// Depict a set of molecules, they will be depicted in a grid with the
        /// specified number of rows and columns. Rows are filled first and then
        /// columns.
        /// </summary>
        /// <param name="mols">molecules</param>
        /// <param name="nrow">number of rows</param>
        /// <param name="ncol">number of columns</param>
        /// <returns>depiction</returns>
        /// <exception cref="CDKException">a depiction could not be generated</exception>
        public Depiction Depict(IEnumerable<IAtomContainer> mols, int nrow, int ncol, IReadOnlyDictionary<IChemObject, Color> highlight = null)
        {
            if (highlight == null)
                highlight = Dictionaries.Empty<IChemObject, Color>();

            var layoutBackups = new List<LayoutBackup>();
            int molId = 0;
            foreach (var mol in mols)
            {
                if (mol == null)
                    throw new NullReferenceException("Null molecule provided!");
                SetIfMissing(mol, MarkedElement.IdKey, "mol" + ++molId);
                layoutBackups.Add(new LayoutBackup(mol));
            }

            // ensure we have coordinates, generate them if not
            // we also rescale the molecules such that all bond
            // lengths are the same.
            PrepareCoords(mols);

            // highlight parts
            foreach (var e in highlight)
                e.Key.SetProperty(StandardGenerator.HighlightColorKey, e.Value);

            // generate bound rendering elements
            var model = CreateModel();
            // setup the model scale
            var molList = mols.ToList();
            model.SetScale(CaclModelScale(molList));

            var molElems = Generate(molList, model, 1);

            // reset molecule coordinates
            foreach (LayoutBackup backup in layoutBackups)
                backup.Reset();

            // generate titles (if enabled)
            var titles = new List<Bounds>();
            if (model.GetShowMoleculeTitle())
            {
                foreach (var mol in mols)
                    titles.Add(GenerateTitle(model, mol, model.GetScale()));
            }

            // remove current highlight buffer
            foreach (var obj in highlight.Keys)
                obj.RemoveProperty(StandardGenerator.HighlightColorKey);

            return new MolGridDepiction(model, molElems, titles, dimensions, nrow, ncol);
        }

        /// <summary>
        /// Prepare a collection of molecules for rendering. If coordinates are not
        /// present they are generated, if coordinates exists they are scaled to
        /// be consistent (length=1.5).
        /// </summary>
        /// <param name="mols">molecules</param>
        /// <returns>coordinates</returns>
        /// <exception cref="CDKException"></exception>
        private static void PrepareCoords(IEnumerable<IAtomContainer> mols)
        {
            foreach (IAtomContainer mol in mols)
            {
                if (!Ensure2DLayout(mol) && mol.Bonds.Count > 0)
                {
                    double factor = GeometryUtil.GetScaleFactor(mol, 1.5);
                    GeometryUtil.ScaleMolecule(mol, factor);
                }
            }
        }

        /// <summary>
        /// Reset the coordinates to their position before rendering.
        /// </summary>
        /// <param name="mols">molecules</param>
        /// <param name="scales">how molecules were scaled</param>
        private static void ResetCoords(IEnumerable<IAtomContainer> mols, List<double> scales)
        {
            var it = scales.GetEnumerator();
            foreach (var mol in mols)
            {
                it.MoveNext();
                double factor = it.Current;
                if (!double.IsNaN(factor))
                {
                    GeometryUtil.ScaleMolecule(mol, 1 / factor);
                }
                else
                {
                    foreach (var atom in mol.Atoms)
                        atom.Point2D = null;
                }
            }
        }

        private static void SetIfMissing(IChemObject chemObject, string key, string val)
        {
            if (chemObject.GetProperty<string>(key) == null)
                chemObject.SetProperty(key, val);
        }

        /// <summary>
        /// Depict a reaction.
        /// </summary>
        /// <param name="rxn">reaction instance</param>
        /// <returns>depiction</returns>
        /// <exception cref="CDKException">a depiction could not be generated</exception>
        public Depiction Depict(IReaction rxn)
        {
            return Depict(rxn, null);
        }

        public Depiction Depict(IReaction rxn, IReadOnlyDictionary<IChemObject, Color> highlight)
        {
            if (highlight == null)
                highlight = Dictionaries.Empty<IChemObject, Color>();

            Ensure2DLayout(rxn); // can reorder components if align is enabled!

            var fgcol = templateModel.GetAtomColorer().GetAtomColor(rxn.Builder.NewAtom("C"));

            var reactants = rxn.Reactants.ToList();
            var products = rxn.Products.ToList();
            var agents = rxn.Agents.ToList();
            List<LayoutBackup> layoutBackups = new List<LayoutBackup>();

            // set ids for tagging elements
            int molId = 0;
            foreach (var mol in reactants)
            {
                SetIfMissing(mol, MarkedElement.IdKey, "mol" + ++molId);
                SetIfMissing(mol, MarkedElement.ClassKey, "reactant");
                layoutBackups.Add(new LayoutBackup(mol));
            }
            foreach (var mol in products)
            {
                SetIfMissing(mol, MarkedElement.IdKey, "mol" + ++molId);
                SetIfMissing(mol, MarkedElement.ClassKey, "product");
                layoutBackups.Add(new LayoutBackup(mol));
            }
            foreach (var mol in agents)
            {
                SetIfMissing(mol, MarkedElement.IdKey, "mol" + ++molId);
                SetIfMissing(mol, MarkedElement.ClassKey, "agent");
                layoutBackups.Add(new LayoutBackup(mol));
            }

            var myHighlight = new Dictionary<IChemObject, Color>();
            if (atomMapColors != null)
            {
                foreach (var e in MakeHighlightAtomMap(reactants, products))
                    myHighlight[e.Key] = e.Value;
            }
            // user highlight buffer pushes out the atom-map highlight if provided
            foreach (var e in highlight)
                myHighlight[e.Key] = e.Value;

            PrepareCoords(reactants);
            PrepareCoords(products);
            PrepareCoords(agents);

            // highlight parts
            foreach (var e in myHighlight)
                e.Key.SetProperty(StandardGenerator.HighlightColorKey, e.Value);

            // setup the model scale based on bond length
            var scale = this.CaclModelScale(rxn);
            var model = CreateModel();
            model.SetScale(scale);

            // reactant/product/agent element generation, we number the reactants, then products then agents
            var reactantBounds = Generate(reactants, model, 1);
            var productBounds = Generate(rxn.Products.ToList(), model, rxn.Reactants.Count);
            var agentBounds = Generate(rxn.Agents.ToList(), model, rxn.Reactants.Count + rxn.Products.Count);

            // remove current highlight buffer
            foreach (var obj in myHighlight.Keys)
                obj.RemoveProperty(StandardGenerator.HighlightColorKey);

            // generate a 'plus' element
            var plus = GeneratePlusSymbol(scale, fgcol);

            // reset the coordinates to how they were before we invoked depict
            foreach (LayoutBackup backup in layoutBackups)
                backup.Reset();

            var emptyBounds = new Bounds();
            var title = model.GetShowReactionTitle() ? GenerateTitle(model, rxn, scale) : emptyBounds;
            var reactantTitles = new List<Bounds>();
            var productTitles = new List<Bounds>();
            if (model.GetShowMoleculeTitle())
            {
                foreach (IAtomContainer reactant in reactants)
                    reactantTitles.Add(GenerateTitle(model, reactant, scale));
                foreach (IAtomContainer product in products)
                    productTitles.Add(GenerateTitle(model, product, scale));
            }

            Bounds conditions = GenerateReactionConditions(rxn, fgcol, model.GetScale());

            return new ReactionDepiction(model,
                                         reactantBounds, productBounds, agentBounds,
                                         plus, rxn.Direction, dimensions,
                                         reactantTitles,
                                         productTitles,
                                         title,
                                         conditions,
                                         fgcol);
        }

        /// <summary>
        /// Internal - makes a map of the highlights for reaction mapping.
        /// </summary>
        /// <param name="reactants">reaction reactants</param>
        /// <param name="products">reaction products</param>
        /// <returns>the highlight map</returns>
        private Dictionary<IChemObject, Color> MakeHighlightAtomMap(List<IAtomContainer> reactants,
                                                             List<IAtomContainer> products)
        {
            var colorMap = new Dictionary<IChemObject, Color>();
            var mapToColor = new Dictionary<int, Color>();
            var amap = new Dictionary<int, IAtom>();
            int colorIdx = -1;
            foreach (var mol in reactants)
            {
                int prevPalletIdx = colorIdx;
                foreach (var atom in mol.Atoms)
                {
                    int mapidx = AccessAtomMap(atom);
                    if (mapidx > 0)
                    {
                        if (prevPalletIdx == colorIdx)
                        {
                            colorIdx++; // select next color
                            if (colorIdx >= atomMapColors.Count)
                                throw new ArgumentException("Not enough colors to highlight atom mapping, please provide mode");
                        }
                        var color = atomMapColors[colorIdx];
                        colorMap[atom] = color;
                        mapToColor[mapidx] = color;
                        amap[mapidx] = atom;
                    }
                }
                if (colorIdx > prevPalletIdx)
                {
                    foreach (var bond in mol.Bonds)
                    {
                        var a1 = bond.Begin;
                        var a2 = bond.End;
                        var c1 = colorMap[a1];
                        var c2 = colorMap[a2];
                        if (c1 != null && c1 == c2)
                            colorMap[bond] = c1;
                    }
                }
            }

            foreach (var mol in products)
            {
                foreach (var atom in mol.Atoms)
                {
                    int mapidx = AccessAtomMap(atom);
                    if (mapidx > 0)
                    {
                        colorMap[atom] = mapToColor[mapidx];
                    }
                }
                foreach (var pBnd in mol.Bonds)
                {
                    var pBeg = pBnd.Begin;
                    var pEnd = pBnd.End;
                    if (colorMap.TryGetValue(pBeg, out Color c1)
                     && colorMap.TryGetValue(pEnd, out Color c2)
                     && c1 == c2)
                    {
                        if (amap.TryGetValue(AccessAtomMap(pBeg), out IAtom rBeg)
                         && amap.TryGetValue(AccessAtomMap(pEnd), out IAtom rEnd))
                        {
                            var rBnd = rBeg.GetBond(rEnd);
                            if (rBnd != null
                             && ((pBnd.IsAromatic && rBnd.IsAromatic) 
                              || rBnd.Order == pBnd.Order))
                            {
                                colorMap[pBnd] = c1;
                            }
                            else
                            {
                                colorMap.Remove(rBnd);
                            }
                        }
                    }
                }
            }

            return colorMap;
        }

        private static int AccessAtomMap(IAtom atom)
        {
            var mapidx = atom.GetProperty<int?>(CDKPropertyName.AtomAtomMapping);
            if (mapidx == null)
                return 0;
            return mapidx.Value;
        }

        private Bounds GeneratePlusSymbol(double scale, Color fgcol)
        {
            return new Bounds(StandardGenerator.EmbedText(font, emSize, "+", fgcol, 1 / scale));
        }

        private IRenderingElement Generate(IAtomContainer molecule, RendererModel model, int atomNum)
        {
            // tag the atom and bond ids
            string molId = molecule.GetProperty<string>(MarkedElement.IdKey);
            if (molId != null)
            {
                int atomId = 0, bondid = 0;
                foreach (var atom in molecule.Atoms)
                    SetIfMissing(atom, MarkedElement.IdKey, molId + "atm" + ++atomId);
                foreach (var bond in molecule.Bonds)
                    SetIfMissing(bond, MarkedElement.IdKey, molId + "bnd" + ++bondid);
            }

            if (annotateAtomNumbers)
            {
                foreach (var atom in molecule.Atoms)
                {
                    if (atom.GetProperty<string>(StandardGenerator.AnnotationLabelKey) != null)
                        throw new InvalidOperationException("Multiple annotation labels are not supported.");
                    atom.SetProperty(StandardGenerator.AnnotationLabelKey, (atomNum++).ToString());
                }
            }
            else if (annotateAtomValues)
            {
                foreach (var atom in molecule.Atoms)
                {
                    if (atom.GetProperty<string>(StandardGenerator.AnnotationLabelKey) != null)
                        throw new NotSupportedException("Multiple annotation labels are not supported.");
                    atom.SetProperty(StandardGenerator.AnnotationLabelKey,
                                     atom.GetProperty<string>(CDKPropertyName.Comment));
                }
            }
            else if (annotateAtomMapNumbers)
            {
                foreach (var atom in molecule.Atoms)
                {
                    if (atom.GetProperty<string>(StandardGenerator.AnnotationLabelKey) != null)
                        throw new InvalidOperationException("Multiple annotation labels are not supported.");
                    int mapidx = AccessAtomMap(atom);
                    if (mapidx > 0)
                    {
                        atom.SetProperty(StandardGenerator.AnnotationLabelKey, mapidx.ToString());
                    }
                }
            }

            var grp = new ElementGroup();
            foreach (var gen in generators)
                grp.Add(gen.Generate(molecule, model));

            // cleanup
            if (annotateAtomNumbers || annotateAtomMapNumbers)
            {
                foreach (var atom in molecule.Atoms)
                {
                    atom.RemoveProperty(StandardGenerator.AnnotationLabelKey);
                }
            }

            return grp;
        }

        private List<Bounds> Generate(IList<IAtomContainer> mols, RendererModel model, int atomNum)
        {
            var elems = new List<Bounds>();
            foreach (var mol in mols)
            {
                elems.Add(new Bounds(Generate(mol, model, atomNum)));
                atomNum += mol.Atoms.Count;
            }
            return elems;
        }

        /// <summary>
        /// Generate a bound element that is the title of the provided molecule. If title
        /// is not specified an empty bounds is returned.
        /// </summary>
        /// <param name="chemObj">molecule or reaction</param>
        /// <returns>bound element</returns>
        private Bounds GenerateTitle(RendererModel model, IChemObject chemObj, double scale)
        {
            var title = chemObj.GetProperty<string>(CDKPropertyName.Title);
            if (string.IsNullOrEmpty(title))
                return new Bounds();
            scale = 1 / scale * model.GetTitleFontScale();
            return new Bounds(MarkedElement.Markup(StandardGenerator.EmbedText(font, emSize, title, model.GetTitleColor(), scale), "title"));
        }

        private Bounds GenerateReactionConditions(IReaction chemObj, Color fg, double scale)
        {
            var title = chemObj.GetProperty<string>(CDKPropertyName.ReactionConditions);
            if (string.IsNullOrEmpty(title))
                return new Bounds();
            return new Bounds(MarkedElement.Markup(StandardGenerator.EmbedText(font, emSize, title, fg, 1 / scale), "conditions"));
        }

        /// <summary>
        /// Automatically generate coordinates if a user has provided a molecule without them.
        /// </summary>
        /// <param name="container">a molecule</param>
        /// <returns>if coordinates needed to be generated</returns>
        /// <exception cref="CDKException">coordinates could not be generated</exception>
        private static bool Ensure2DLayout(IAtomContainer container)
        {
            if (!GeometryUtil.Has2DCoordinates(container))
            {
                var sdg = new StructureDiagramGenerator();
                sdg.GenerateCoordinates(container);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Automatically generate coordinates if a user has provided reaction without them.
        /// </summary>
        /// <param name="rxn">reaction</param>
        /// <exception cref="CDKException">coordinates could not be generated</exception>
        private void Ensure2DLayout(IReaction rxn)
        {
            if (!GeometryUtil.Has2DCoordinates(rxn))
            {
                var sdg = new StructureDiagramGenerator { AlignMappedReaction = alignMappedReactions };
                sdg.GenerateCoordinates(rxn);
            }
        }

        /// <summary>
        /// Colorer for atom symbols.
        /// </summary>
        /// <seealso cref="HighlightStyle"/>
        /// <seealso cref="UniColor"/>
        public IAtomColorer AtomColorer
        {
            get => templateModel.GetAtomColorer();
            set
            {
                if (value == null)
                    value = RendererModelTools.DefaultAtomColorer;
                templateModel.SetAtomColorer(value);
            }
        }

        /// <summary>
        /// The background color.
        /// </summary>
        public Color BackgroundColor
        {
            get => templateModel.GetBackgroundColor();
            set => templateModel.SetBackgroundColor(value);
        }

        public HighlightStyle Highlighting
        {
            get => templateModel.GetHighlighting();
            set => templateModel.SetHighlighting(value);
        }

        public double OuterGlowWidth
        {
            get => templateModel.GetOuterGlowWidth();
            set => templateModel.SetOuterGlowWidth(value);
        }

        /// <summary>
        /// Display atom numbers on the molecule or reaction. The numbers are based on the
        /// ordering of atoms in the molecule data structure and not a systematic system
        /// such as IUPAC numbering.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// A depiction can not have both atom numbers and atom maps visible
        /// (but this can be achieved by manually setting the annotation).
        /// </note>
        /// </remarks>
        /// <seealso cref="AnnotateAtomMapNumbers"/>
        /// <seealso cref="StandardGenerator.AnnotationLabelKey"/>
        public bool AnnotateAtomNumbers
        {
            get => annotateAtomNumbers;
            set
            {
                if (value && (annotateAtomMapNumbers || annotateAtomValues))
                    throw new ArgumentException("Can not annotated atom numbers, atom values or maps are already annotated");
                annotateAtomNumbers = value;
            }
        }

        /// <summary>
        /// Display atom values on the molecule or reaction. The values need to be assigned by 
        /// <c>atom.SetProperty(CDKPropertyName.Comment, myValueToBeDisplayedNextToAtom);</c>
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// A depiction can not have both atom numbers and atom maps visible
        /// (but this can be achieved by manually setting the annotation).
        /// </note>
        /// </remarks>
        /// <seealso cref="AnnotateAtomMapNumbers"/>
        /// <seealso cref="StandardGenerator.AnnotationLabelKey"/>
        public bool AnnotateAtomValues
        {
            get => annotateAtomValues;

            set
            {
                if (value && (annotateAtomNumbers || annotateAtomMapNumbers))
                    throw new InvalidOperationException("Can not annotated atom values, atom numbers or maps are already annotated");
                annotateAtomValues = value;
            }
        }

        /// <summary>
        /// Display atom-atom mapping numbers on a reaction. Each atom map index
        /// is loaded from the property <see cref="CDKPropertyName.AtomAtomMapping"/>.
        /// </summary>
        /// <remarks>
        /// <note type="note">
        /// A depiction can not have both atom numbers and atom
        /// maps visible (but this can be achieved by manually setting
        /// the annotation).
        /// </note>
        /// </remarks>
        /// <seealso cref="AnnotateAtomNumbers"/>
        /// <seealso cref="CDKPropertyName.AtomAtomMapping"/>
        /// <seealso cref="StandardGenerator.AnnotationLabelKey"/>
        public bool AnnotateAtomMapNumbers
        {
            get => annotateAtomMapNumbers;
            set
            {
                if (value && annotateAtomNumbers)
                    throw new InvalidOperationException("Can not annotated atom maps, atom numbers or values are already annotated");
                annotateAtomMapNumbers = value;
            }
        }

        /// <summary>
        /// Adds to the highlight the coloring of reaction atom-maps. The
        /// optional color array is used as the pallet with which to
        /// highlight. If none is provided a set of high-contrast colors
        /// will be used.
        /// </summary>
        /// <seealso cref="AnnotateAtomMapNumbers"/>
        public IReadOnlyList<Color> AtomMapColors
        {
            get => atomMapColors;
            set => atomMapColors = value;
        }

        /// <summary>
        /// Display a molecule title with each depiction. The title
        /// is specified by setting the <see cref="CDKPropertyName.Title"/>
        /// property. For reactions only the main components have their
        /// title displayed.
        /// </summary>
        public bool ShowMoleculeTitle
        {
            get => templateModel.GetShowMoleculeTitle();
            set => templateModel.SetShowMoleculeTitle(value);
        }

        /// <summary>
        /// Display a reaction title with the depiction. The title
        /// is specified by setting the <see cref="CDKPropertyName.Title"/>
        /// property on the <see cref="IReaction"/> instance.
        /// </summary>
        public bool ShowReactionTitle
        {
            get => templateModel.GetShowReactionTitle();
            set => templateModel.SetShowReactionTitle(value);
        }

        /// <summary>
        /// Specifies that reactions with atom-atom mappings should have their reactants/product
        /// coordinates aligned. Default: true.
        /// </summary>
        public bool AlignMappedReaction
        {
            get => alignMappedReactions;
            set => alignMappedReactions = value;
        }

        /// <summary>
        /// The color annotations (e.g. atom-numbers) will appear in.
        /// </summary>
        public Color AnnotationColor
        {
            get => templateModel.GetAnnotationColor();
            set => templateModel.SetAnnotationColor(value);
        }

        /// <summary>
        /// The size of annotations relative to atom symbols.
        /// </summary>
        public double AnnotationFontScale
        {
            get => templateModel.GetAnnotationFontScale();
            set => templateModel.SetAnnotationFontScale(value);
        }

        /// <summary>
        /// The color titles will appear in.
        /// </summary>
        public Color TitleColor
        {
            get => templateModel.GetTitleColor();
            set => templateModel.SetTitleColor(value);
        }

        /// <summary>
        /// The size of titles compared to atom symbols.
        /// </summary>
        public double TitleFontScale
        {
            get => templateModel.GetTitleFontScale();
            set => templateModel.SetTitleFontScale(value);
        }

        /// <summary>
        /// Atom symbol visibility.
        /// </summary>
        public SymbolVisibility SymbolVisibility
        {
            get => templateModel.GetVisibility();
            set => templateModel.SetVisibility(value);
        }

        /// <summary>
        /// Specify a desired size of depiction. The units depend on the output format with
        /// raster images using pixels and vector graphics using millimeters. By default depictions
        /// are only ever made smaller if you would also like to make depictions fill all available
        /// space use the <see cref="FillToFit"/> option. 
        /// </summary>
        /// <remarks>
        /// Currently the size must either both be precisely specified (e.g. 256x256) or
        /// automatic (e.g. <see cref="Automatic"/>x<see cref="Automatic"/>) you cannot for example
        /// specify a fixed height and automatic width.
        /// </remarks>
        /// <seealso cref="FillToFit"/>
        public WPF.Size Size
        {
            get => new WPF.Size(dimensions.width, dimensions.height);

            set
            {
                dimensions = value.IsEmpty ? Dimensions.Automatic : new Dimensions(value.Width, value.Height);
            }
        }

        /// <summary>
        /// The desired size of margin. The units depend on the output format with
        /// raster images using pixels and vector graphics using millimeters.
        /// </summary>
        public double Margin
        {
            get => templateModel.GetMargin();
            set => templateModel.SetMargin(value);
        }

        /// <summary>
        /// The desired size of padding for molecule sets and reactions. The units
        /// depend on the output format with raster images using pixels and vector graphics
        /// using millimeters.
        /// </summary>
        public double Padding
        {
            get => templateModel.GetPadding();
            set => templateModel.SetPadding(value);
        }

        /// <summary>
        /// The desired zoom factor - this changes the base size of a
        /// depiction and is used for uniformly making depictions bigger. If
        /// you would like to simply fill all available space (not recommended)
        /// use <see cref="FillToFit"/>.
        /// </summary>
        /// <remarks>
        /// The zoom is a scaling factor, specifying a zoom of 2 is double size,
        /// 0.5 half size, etc.
        /// </remarks>
        public double Zoom
        {
            get => templateModel.GetZoomFactor();
            set => templateModel.SetZoomFactor(value);
        }

        /// <summary>
        /// Resize depictions to fill all available space (only if a size is specified).
        /// This generally isn't wanted as very small molecules (e.g. acetaldehyde) may
        /// become huge.
        /// </summary>
        public bool FillToFit
        {
            get => templateModel.GetFitToScreen();
            set => templateModel.SetFitToScreen(value);
        }

        /// <summary>
        /// When aromaticity is set on bonds, display this in the diagram. IUPAC
        /// recommends depicting kekulé structures to avoid ambiguity but it's common
        /// practice to render delocalised rings "donuts" or "life buoys". With fused
        /// rings this can be somewhat confusing as you end up with three lines at
        /// the fusion point.
        /// </summary>
        /// <remarks>
        /// By default small rings are renders as donuts with dashed bonds used
        /// otherwise. You can use dashed bonds always by setting <see langword="false"/>.
        /// </remarks>
        /// <seealso cref="RendererModelTools.GetForceDelocalisedBondDisplay(RendererModel)"/>
        /// <seealso cref="RendererModelTools.SetForceDelocalisedBondDisplay(RendererModel, bool)"/>
        public bool AromaticDisplay
        {
            get => templateModel.GetForceDelocalisedBondDisplay();
            set => templateModel.SetForceDelocalisedBondDisplay(value);
        }

        /// <summary>
        /// Low-level option method to set a rendering model parameter.
        /// </summary>
        /// <typeparam name="T">option value type</typeparam>
        /// <param name="key">option key</param>
        /// <param name="value">option value</param>
        public void SetParameter<T>(string key, T value)
        {
            templateModel.Parameters[key] = value;
        }

        private double CaclModelScale(IEnumerable<IAtomContainer> mols)
        {
            var bonds = new List<IBond>();
            foreach (var mol in mols)
            {
                foreach (var bond in mol.Bonds)
                {
                    bonds.Add(bond);
                }
            }
            return CalcModelScaleForBondLength(MedianBondLength(bonds));
        }

        private double CaclModelScale(IReaction rxn)
        {
            var mols = new List<IAtomContainer>();
            foreach (var mol in rxn.Reactants)
                mols.Add(mol);
            foreach (var mol in rxn.Products)
                mols.Add(mol);
            foreach (var mol in rxn.Agents)
                mols.Add(mol);
            return CaclModelScale(mols);
        }

        private static double MedianBondLength(ICollection<IBond> bonds)
        {
            if (!bonds.Any())
                return 1.5;
            int nBonds = 0;
            double[] lengths = new double[bonds.Count];
            foreach (var bond in bonds)
            {
                Vector2 p1 = bond.Begin.Point2D.Value;
                Vector2 p2 = bond.End.Point2D.Value;
                // watch out for overlaid atoms (occur in multiple group Sgroups)
                if (!p1.Equals(p2))
                    lengths[nBonds++] = Vector2.Distance(p1, p2);
            }
            Array.Sort(lengths, 0, nBonds);
            return lengths[nBonds / 2];
        }

        private double CalcModelScaleForBondLength(double bondLength)
        {
            return templateModel.GetBondLength() / bondLength;
        }

        private static FontFamily GetDefaultOsFont()
        {
            // TODO: Native Font Support - choose best for Win/Linux/OS X etc
            return WPF.SystemFonts.MessageFontFamily;
        }

        /// <summary>
        /// Utility class for storing coordinates and bond types and resetting them after use.
        /// </summary>
        private sealed class LayoutBackup
        {
            private readonly Vector2?[] coords;
            private readonly BondStereo[] btypes;
            private readonly IAtomContainer mol;

            public LayoutBackup(IAtomContainer mol)
            {
                var numAtoms = mol.Atoms.Count;
                var numBonds = mol.Bonds.Count;
                this.coords = new Vector2?[numAtoms];
                this.btypes = new BondStereo[numBonds];
                this.mol = mol;
                for (int i = 0; i < numAtoms; i++)
                {
                    var atom = mol.Atoms[i];
                    coords[i] = atom.Point2D;
                    if (coords[i] != null)
                        atom.Point2D = coords[i]; // copy
                }
                for (int i = 0; i < numBonds; i++)
                {
                    var bond = mol.Bonds[i];
                    btypes[i] = bond.Stereo;
                }
            }

            internal void Reset()
            {
                var numAtoms = mol.Atoms.Count;
                var numBonds = mol.Bonds.Count;
                for (int i = 0; i < numAtoms; i++)
                    mol.Atoms[i].Point2D = coords[i];
                for (int i = 0; i < numBonds; i++)
                    mol.Bonds[i].Stereo = btypes[i];
            }
        }
    }
}
