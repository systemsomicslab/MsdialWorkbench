
using NCDK.Numerics;
using NCDK.Renderers.Colors;
using NCDK.Renderers.Generators;
using WPF = System.Windows;

namespace NCDK.Renderers
{

    public static partial class RendererModelTools
    {
        public static readonly WPF.Media.Color DefaultSelectionColor = WPF.Media.Color.FromRgb(0x49, 0xdf, 0xff);

        /// <summary>
        /// Get the color of a selection. Default value is WPF.Media.Color.FromRgb(0x49, 0xdf, 0xff).
        /// </summary>
        /// <returns>the color of a selection</returns>
        public static WPF.Media.Color GetSelectionColor(this RendererModel model)
        {
            const string key = "SelectionColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultSelectionColor;

            return value;
        }

        /// <summary>
        /// Set the color of a selection.
        /// </summary>
        public static void SetSelectionColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "SelectionColor";
            model.Parameters[key] = value;
        }

        public static bool HasSelectionColor(this RendererModel model)
        {
            const string key = "SelectionColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultExternalHighlightColor = WPF.Media.Colors.Gray;

        /// <summary>
        /// Get the color used to highlight external selections. Default value is WPF.Media.Colors.Gray.
        /// </summary>
        /// <returns>the color used to highlight external selections</returns>
        public static WPF.Media.Color GetExternalHighlightColor(this RendererModel model)
        {
            const string key = "ExternalHighlightColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultExternalHighlightColor;

            return value;
        }

        /// <summary>
        /// Set the color used to highlight external selections.
        /// </summary>
        public static void SetExternalHighlightColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "ExternalHighlightColor";
            model.Parameters[key] = value;
        }

        public static bool HasExternalHighlightColor(this RendererModel model)
        {
            const string key = "ExternalHighlightColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultPadding = 16;

        /// <summary>
        /// Get padding between molecules in a grid or row. Default value is 16.
        /// </summary>
        /// <returns>padding between molecules in a grid or row</returns>
        public static double GetPadding(this RendererModel model)
        {
            const string key = "Padding";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultPadding;

            return value;
        }

        /// <summary>
        /// Set padding between molecules in a grid or row.
        /// </summary>
        public static void SetPadding(this RendererModel model, double value)
        {
            const string key = "Padding";
            model.Parameters[key] = value;
        }

        public static bool HasPadding(this RendererModel model)
        {
            const string key = "Padding";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly System.Collections.Generic.IDictionary<IChemObject, WPF.Media.Color> DefaultColorHash = new System.Collections.Generic.Dictionary<IChemObject, WPF.Media.Color>();

        /// <summary>
        /// Get the color hash is used to color substructures. Default value is new System.Collections.Generic.Dictionary{IChemObject, WPF.Media.Color}().
        /// </summary>
        /// <returns>the color hash is used to color substructures</returns>
        public static System.Collections.Generic.IDictionary<IChemObject, WPF.Media.Color> GetColorHash(this RendererModel model)
        {
            const string key = "ColorHash";
            System.Collections.Generic.IDictionary<IChemObject, WPF.Media.Color> value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (System.Collections.Generic.IDictionary<IChemObject, WPF.Media.Color>)v;
            else
                model.Parameters[key] = value = DefaultColorHash;

            return value;
        }

        /// <summary>
        /// Set the color hash is used to color substructures.
        /// </summary>
        public static void SetColorHash(this RendererModel model, System.Collections.Generic.IDictionary<IChemObject, WPF.Media.Color> value)
        {
            const string key = "ColorHash";
            model.Parameters[key] = value;
        }

        public static bool HasColorHash(this RendererModel model)
        {
            const string key = "ColorHash";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultTitleFontScale = 0.8;

        /// <summary>
        /// Get size of title font relative compared to atom symbols. Default value is 0.8.
        /// </summary>
        /// <returns>size of title font relative compared to atom symbols</returns>
        public static double GetTitleFontScale(this RendererModel model)
        {
            const string key = "TitleFontScale";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultTitleFontScale;

            return value;
        }

        /// <summary>
        /// Set size of title font relative compared to atom symbols.
        /// </summary>
        public static void SetTitleFontScale(this RendererModel model, double value)
        {
            const string key = "TitleFontScale";
            model.Parameters[key] = value;
        }

        public static bool HasTitleFontScale(this RendererModel model)
        {
            const string key = "TitleFontScale";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultTitleColor = WPF.Media.Colors.Red;

        /// <summary>
        /// Get color of title text. Default value is WPF.Media.Colors.Red.
        /// </summary>
        /// <returns>color of title text</returns>
        public static WPF.Media.Color GetTitleColor(this RendererModel model)
        {
            const string key = "TitleColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultTitleColor;

            return value;
        }

        /// <summary>
        /// Set color of title text.
        /// </summary>
        public static void SetTitleColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "TitleColor";
            model.Parameters[key] = value;
        }

        public static bool HasTitleColor(this RendererModel model)
        {
            const string key = "TitleColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultMarkedOutput = true;

        /// <summary>
        /// Get if format supports it (e.g. SVG) should marked up elements (id and classes) be output.. Default value is true.
        /// </summary>
        /// <returns>if format supports it (e.g. SVG) should marked up elements (id and classes) be output.</returns>
        public static bool GetMarkedOutput(this RendererModel model)
        {
            const string key = "MarkedOutput";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultMarkedOutput;

            return value;
        }

        /// <summary>
        /// Set if format supports it (e.g. SVG) should marked up elements (id and classes) be output..
        /// </summary>
        public static void SetMarkedOutput(this RendererModel model, bool value)
        {
            const string key = "MarkedOutput";
            model.Parameters[key] = value;
        }

        public static bool HasMarkedOutput(this RendererModel model)
        {
            const string key = "MarkedOutput";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultAtomColor = WPF.Media.Colors.Black;

        /// <summary>
        /// Get the color by which atom labels are drawn. Default value is WPF.Media.Colors.Black.
        /// </summary>
        /// <returns>the color by which atom labels are drawn</returns>
        public static WPF.Media.Color GetAtomColor(this RendererModel model)
        {
            const string key = "AtomColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultAtomColor;

            return value;
        }

        /// <summary>
        /// Set the color by which atom labels are drawn.
        /// </summary>
        public static void SetAtomColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "AtomColor";
            model.Parameters[key] = value;
        }

        public static bool HasAtomColor(this RendererModel model)
        {
            const string key = "AtomColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly IAtomColorer DefaultAtomColorer = new UniColor(WPF.Media.Color.FromRgb(0x44, 0x44, 0x44));

        /// <summary>
        /// Get <see cref="IAtomColorer"/> used to draw elements. Default value is new UniColor(WPF.Media.Color.FromRgb(0x44, 0x44, 0x44)).
        /// </summary>
        /// <returns><see cref="IAtomColorer"/> used to draw elements</returns>
        public static IAtomColorer GetAtomColorer(this RendererModel model)
        {
            const string key = "AtomColorer";
            IAtomColorer value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (IAtomColorer)v;
            else
                model.Parameters[key] = value = DefaultAtomColorer;

            return value;
        }

        /// <summary>
        /// Set <see cref="IAtomColorer"/> used to draw elements.
        /// </summary>
        public static void SetAtomColorer(this RendererModel model, IAtomColorer value)
        {
            const string key = "AtomColorer";
            model.Parameters[key] = value;
        }

        public static bool HasAtomColorer(this RendererModel model)
        {
            const string key = "AtomColorer";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly SymbolVisibility DefaultVisibility = NCDK.Renderers.Generators.Standards.SelectionVisibility.Disconnected(SymbolVisibility.IupacRecommendationsWithoutTerminalCarbon);

        /// <summary>
        /// Get defines which atoms have their symbol displayed. Default value is NCDK.Renderers.Generators.Standards.SelectionVisibility.Disconnected(SymbolVisibility.IupacRecommendationsWithoutTerminalCarbon).
        /// </summary>
        /// <returns>defines which atoms have their symbol displayed</returns>
        public static SymbolVisibility GetVisibility(this RendererModel model)
        {
            const string key = "Visibility";
            SymbolVisibility value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (SymbolVisibility)v;
            else
                model.Parameters[key] = value = DefaultVisibility;

            return value;
        }

        /// <summary>
        /// Set defines which atoms have their symbol displayed.
        /// </summary>
        public static void SetVisibility(this RendererModel model, SymbolVisibility value)
        {
            const string key = "Visibility";
            model.Parameters[key] = value;
        }

        public static bool HasVisibility(this RendererModel model)
        {
            const string key = "Visibility";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultStrokeRatio = 1;

        /// <summary>
        /// Get defines the ratio of the stroke to the width of the stroke of the font used to depict atom symbols. Default value is 1.
        /// </summary>
        /// <returns>defines the ratio of the stroke to the width of the stroke of the font used to depict atom symbols</returns>
        public static double GetStrokeRatio(this RendererModel model)
        {
            const string key = "StrokeRatio";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultStrokeRatio;

            return value;
        }

        /// <summary>
        /// Set defines the ratio of the stroke to the width of the stroke of the font used to depict atom symbols.
        /// </summary>
        public static void SetStrokeRatio(this RendererModel model, double value)
        {
            const string key = "StrokeRatio";
            model.Parameters[key] = value;
        }

        public static bool HasStrokeRatio(this RendererModel model)
        {
            const string key = "StrokeRatio";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultBondSeparation = 0.16;

        /// <summary>
        /// Get defines the ratio of the separation between lines in double bonds as a percentage of length (<see cref="RendererModelTools.GetBondLength(RendererModel)"/>). Default value is 0.16.
        /// </summary>
        /// <returns>defines the ratio of the separation between lines in double bonds as a percentage of length (<see cref="RendererModelTools.GetBondLength(RendererModel)"/>)</returns>
        public static double GetBondSeparation(this RendererModel model)
        {
            const string key = "BondSeparation";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultBondSeparation;

            return value;
        }

        /// <summary>
        /// Set defines the ratio of the separation between lines in double bonds as a percentage of length (<see cref="RendererModelTools.GetBondLength(RendererModel)"/>).
        /// </summary>
        public static void SetBondSeparation(this RendererModel model, double value)
        {
            const string key = "BondSeparation";
            model.Parameters[key] = value;
        }

        public static bool HasBondSeparation(this RendererModel model)
        {
            const string key = "BondSeparation";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultSymbolMarginRatio = 2;

        /// <summary>
        /// Get defines the margin between an atom symbol and a connected bond based on the stroke width. Default value is 2.
        /// </summary>
        /// <returns>defines the margin between an atom symbol and a connected bond based on the stroke width</returns>
        public static double GetSymbolMarginRatio(this RendererModel model)
        {
            const string key = "SymbolMarginRatio";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultSymbolMarginRatio;

            return value;
        }

        /// <summary>
        /// Set defines the margin between an atom symbol and a connected bond based on the stroke width.
        /// </summary>
        public static void SetSymbolMarginRatio(this RendererModel model, double value)
        {
            const string key = "SymbolMarginRatio";
            model.Parameters[key] = value;
        }

        public static bool HasSymbolMarginRatio(this RendererModel model)
        {
            const string key = "SymbolMarginRatio";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultWedgeRatio = 6;

        /// <summary>
        /// Get ratio of the wide end of wedge compared to the narrow end (stroke width). Default value is 6.
        /// </summary>
        /// <returns>ratio of the wide end of wedge compared to the narrow end (stroke width)</returns>
        public static double GetWedgeRatio(this RendererModel model)
        {
            const string key = "WedgeRatio";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultWedgeRatio;

            return value;
        }

        /// <summary>
        /// Set ratio of the wide end of wedge compared to the narrow end (stroke width).
        /// </summary>
        public static void SetWedgeRatio(this RendererModel model, double value)
        {
            const string key = "WedgeRatio";
            model.Parameters[key] = value;
        }

        public static bool HasWedgeRatio(this RendererModel model)
        {
            const string key = "WedgeRatio";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultHashSpacing = 5;

        /// <summary>
        /// Get the preferred spacing between lines in hashed bonds. Default value is 5.
        /// </summary>
        /// <returns>the preferred spacing between lines in hashed bonds</returns>
        public static double GetHashSpacing(this RendererModel model)
        {
            const string key = "HashSpacing";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultHashSpacing;

            return value;
        }

        /// <summary>
        /// Set the preferred spacing between lines in hashed bonds.
        /// </summary>
        public static void SetHashSpacing(this RendererModel model, double value)
        {
            const string key = "HashSpacing";
            model.Parameters[key] = value;
        }

        public static bool HasHashSpacing(this RendererModel model)
        {
            const string key = "HashSpacing";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultWaveSpacing = 5;

        /// <summary>
        /// Get the spacing of waves (semi circles) drawn in wavy bonds with. Default value is 5.
        /// </summary>
        /// <returns>the spacing of waves (semi circles) drawn in wavy bonds with</returns>
        public static double GetWaveSpacing(this RendererModel model)
        {
            const string key = "WaveSpacing";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultWaveSpacing;

            return value;
        }

        /// <summary>
        /// Set the spacing of waves (semi circles) drawn in wavy bonds with.
        /// </summary>
        public static void SetWaveSpacing(this RendererModel model, double value)
        {
            const string key = "WaveSpacing";
            model.Parameters[key] = value;
        }

        public static bool HasWaveSpacing(this RendererModel model)
        {
            const string key = "WaveSpacing";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly int DefaultDashSection = 8;

        /// <summary>
        /// Get the number of sections to render in a dashed 'unknown' bond. Default value is 8.
        /// </summary>
        /// <returns>the number of sections to render in a dashed 'unknown' bond</returns>
        public static int GetDashSection(this RendererModel model)
        {
            const string key = "DashSection";
            int value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (int)v;
            else
                model.Parameters[key] = value = DefaultDashSection;

            return value;
        }

        /// <summary>
        /// Set the number of sections to render in a dashed 'unknown' bond.
        /// </summary>
        public static void SetDashSection(this RendererModel model, int value)
        {
            const string key = "DashSection";
            model.Parameters[key] = value;
        }

        public static bool HasDashSection(this RendererModel model)
        {
            const string key = "DashSection";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultFancyBoldWedges = true;

        /// <summary>
        /// Get modify bold wedges to be flush with adjacent bonds. Default value is true.
        /// </summary>
        /// <returns>modify bold wedges to be flush with adjacent bonds</returns>
        public static bool GetFancyBoldWedges(this RendererModel model)
        {
            const string key = "FancyBoldWedges";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultFancyBoldWedges;

            return value;
        }

        /// <summary>
        /// Set modify bold wedges to be flush with adjacent bonds.
        /// </summary>
        public static void SetFancyBoldWedges(this RendererModel model, bool value)
        {
            const string key = "FancyBoldWedges";
            model.Parameters[key] = value;
        }

        public static bool HasFancyBoldWedges(this RendererModel model)
        {
            const string key = "FancyBoldWedges";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultFancyHashedWedges = true;

        /// <summary>
        /// Get modify hashed wedges to be flush when there is a single adjacent bond. Default value is true.
        /// </summary>
        /// <returns>modify hashed wedges to be flush when there is a single adjacent bond</returns>
        public static bool GetFancyHashedWedges(this RendererModel model)
        {
            const string key = "FancyHashedWedges";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultFancyHashedWedges;

            return value;
        }

        /// <summary>
        /// Set modify hashed wedges to be flush when there is a single adjacent bond.
        /// </summary>
        public static void SetFancyHashedWedges(this RendererModel model, bool value)
        {
            const string key = "FancyHashedWedges";
            model.Parameters[key] = value;
        }

        public static bool HasFancyHashedWedges(this RendererModel model)
        {
            const string key = "FancyHashedWedges";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultOuterGlowWidth = 2;

        /// <summary>
        /// Get the width of outer glow as a percentage of stroke width. Default value is 2.
        /// </summary>
        /// <returns>the width of outer glow as a percentage of stroke width</returns>
        public static double GetOuterGlowWidth(this RendererModel model)
        {
            const string key = "OuterGlowWidth";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultOuterGlowWidth;

            return value;
        }

        /// <summary>
        /// Set the width of outer glow as a percentage of stroke width.
        /// </summary>
        public static void SetOuterGlowWidth(this RendererModel model, double value)
        {
            const string key = "OuterGlowWidth";
            model.Parameters[key] = value;
        }

        public static bool HasOuterGlowWidth(this RendererModel model)
        {
            const string key = "OuterGlowWidth";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly HighlightStyle DefaultHighlighting = HighlightStyle.None;

        /// <summary>
        /// Get the style of highlight used to emphasis atoms and bonds. Default value is HighlightStyle.None.
        /// </summary>
        /// <returns>the style of highlight used to emphasis atoms and bonds</returns>
        public static HighlightStyle GetHighlighting(this RendererModel model)
        {
            const string key = "Highlighting";
            HighlightStyle value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (HighlightStyle)v;
            else
                model.Parameters[key] = value = DefaultHighlighting;

            return value;
        }

        /// <summary>
        /// Set the style of highlight used to emphasis atoms and bonds.
        /// </summary>
        public static void SetHighlighting(this RendererModel model, HighlightStyle value)
        {
            const string key = "Highlighting";
            model.Parameters[key] = value;
        }

        public static bool HasHighlighting(this RendererModel model)
        {
            const string key = "Highlighting";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultAnnotationColor = WPF.Media.Colors.Red;

        /// <summary>
        /// Get the color of the atom numbers. Default value is WPF.Media.Colors.Red.
        /// </summary>
        /// <returns>the color of the atom numbers</returns>
        public static WPF.Media.Color GetAnnotationColor(this RendererModel model)
        {
            const string key = "AnnotationColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultAnnotationColor;

            return value;
        }

        /// <summary>
        /// Set the color of the atom numbers.
        /// </summary>
        public static void SetAnnotationColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "AnnotationColor";
            model.Parameters[key] = value;
        }

        public static bool HasAnnotationColor(this RendererModel model)
        {
            const string key = "AnnotationColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultAnnotationDistance = 0.25;

        /// <summary>
        /// Get the distance of atom numbers from their parent atom as a percentage of bond length. Default value is 0.25.
        /// </summary>
        /// <returns>the distance of atom numbers from their parent atom as a percentage of bond length</returns>
        public static double GetAnnotationDistance(this RendererModel model)
        {
            const string key = "AnnotationDistance";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultAnnotationDistance;

            return value;
        }

        /// <summary>
        /// Set the distance of atom numbers from their parent atom as a percentage of bond length.
        /// </summary>
        public static void SetAnnotationDistance(this RendererModel model, double value)
        {
            const string key = "AnnotationDistance";
            model.Parameters[key] = value;
        }

        public static bool HasAnnotationDistance(this RendererModel model)
        {
            const string key = "AnnotationDistance";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultAnnotationFontScale = 0.5;

        /// <summary>
        /// Get annotation font size relative to element symbols. Default value is 0.5.
        /// </summary>
        /// <returns>annotation font size relative to element symbols</returns>
        public static double GetAnnotationFontScale(this RendererModel model)
        {
            const string key = "AnnotationFontScale";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultAnnotationFontScale;

            return value;
        }

        /// <summary>
        /// Set annotation font size relative to element symbols.
        /// </summary>
        public static void SetAnnotationFontScale(this RendererModel model, double value)
        {
            const string key = "AnnotationFontScale";
            model.Parameters[key] = value;
        }

        public static bool HasAnnotationFontScale(this RendererModel model)
        {
            const string key = "AnnotationFontScale";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultSgroupBracketDepth = 0.18;

        /// <summary>
        /// Get relative to bond length how deep are brackets drawn. Default value is 0.18.
        /// </summary>
        /// <returns>relative to bond length how deep are brackets drawn</returns>
        public static double GetSgroupBracketDepth(this RendererModel model)
        {
            const string key = "SgroupBracketDepth";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultSgroupBracketDepth;

            return value;
        }

        /// <summary>
        /// Set relative to bond length how deep are brackets drawn.
        /// </summary>
        public static void SetSgroupBracketDepth(this RendererModel model, double value)
        {
            const string key = "SgroupBracketDepth";
            model.Parameters[key] = value;
        }

        public static bool HasSgroupBracketDepth(this RendererModel model)
        {
            const string key = "SgroupBracketDepth";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultSgroupFontScale = 0.6;

        /// <summary>
        /// Get scale Sgroup annotations relative to the normal font size (atom symbol). Default value is 0.6.
        /// </summary>
        /// <returns>scale Sgroup annotations relative to the normal font size (atom symbol)</returns>
        public static double GetSgroupFontScale(this RendererModel model)
        {
            const string key = "SgroupFontScale";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultSgroupFontScale;

            return value;
        }

        /// <summary>
        /// Set scale Sgroup annotations relative to the normal font size (atom symbol).
        /// </summary>
        public static void SetSgroupFontScale(this RendererModel model, double value)
        {
            const string key = "SgroupFontScale";
            model.Parameters[key] = value;
        }

        public static bool HasSgroupFontScale(this RendererModel model)
        {
            const string key = "SgroupFontScale";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultOmitMajorIsotopes = false;

        /// <summary>
        /// Get whether Major Isotopes e.g. 12C, 16O should be omitted. Default value is false.
        /// </summary>
        /// <returns>whether Major Isotopes e.g. 12C, 16O should be omitted</returns>
        public static bool GetOmitMajorIsotopes(this RendererModel model)
        {
            const string key = "OmitMajorIsotopes";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultOmitMajorIsotopes;

            return value;
        }

        /// <summary>
        /// Set whether Major Isotopes e.g. 12C, 16O should be omitted.
        /// </summary>
        public static void SetOmitMajorIsotopes(this RendererModel model, bool value)
        {
            const string key = "OmitMajorIsotopes";
            model.Parameters[key] = value;
        }

        public static bool HasOmitMajorIsotopes(this RendererModel model)
        {
            const string key = "OmitMajorIsotopes";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultForceDelocalisedBondDisplay = false;

        /// <summary>
        /// Get indicate delocalised/aromatic bonds should always be rendered, even when there is a valid Kekule structure. Delocalised bonds will either be rendered as a dashed bond to the side or as a circle/donut/life buoy inside small rings. This depiction is used by default when a bond does not have an order assigned (e.g. null/unset), for example: c1cccc1. Turning this option on means all delocalised bonds will be rendered this way even when they have bond orders correctly assigned: e.g. c1ccccc1, [cH-]1cccc1. <br/><b>As recommended by IUPAC, their usage is discouraged and the Kekule representation is more clear.</b>. Default value is false.
        /// </summary>
        /// <returns>indicate delocalised/aromatic bonds should always be rendered, even when there is a valid Kekule structure. Delocalised bonds will either be rendered as a dashed bond to the side or as a circle/donut/life buoy inside small rings. This depiction is used by default when a bond does not have an order assigned (e.g. null/unset), for example: c1cccc1. Turning this option on means all delocalised bonds will be rendered this way even when they have bond orders correctly assigned: e.g. c1ccccc1, [cH-]1cccc1. <br/><b>As recommended by IUPAC, their usage is discouraged and the Kekule representation is more clear.</b></returns>
        public static bool GetForceDelocalisedBondDisplay(this RendererModel model)
        {
            const string key = "ForceDelocalisedBondDisplay";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultForceDelocalisedBondDisplay;

            return value;
        }

        /// <summary>
        /// Set indicate delocalised/aromatic bonds should always be rendered, even when there is a valid Kekule structure. Delocalised bonds will either be rendered as a dashed bond to the side or as a circle/donut/life buoy inside small rings. This depiction is used by default when a bond does not have an order assigned (e.g. null/unset), for example: c1cccc1. Turning this option on means all delocalised bonds will be rendered this way even when they have bond orders correctly assigned: e.g. c1ccccc1, [cH-]1cccc1. <br/><b>As recommended by IUPAC, their usage is discouraged and the Kekule representation is more clear.</b>.
        /// </summary>
        public static void SetForceDelocalisedBondDisplay(this RendererModel model, bool value)
        {
            const string key = "ForceDelocalisedBondDisplay";
            model.Parameters[key] = value;
        }

        public static bool HasForceDelocalisedBondDisplay(this RendererModel model)
        {
            const string key = "ForceDelocalisedBondDisplay";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultDelocalisedDonutsBondDisplay = true;

        /// <summary>
        /// Get render small delocalised rings as bonds/life buoys? This can sometimes be misleading for fused rings but is commonly used.. Default value is true.
        /// </summary>
        /// <returns>render small delocalised rings as bonds/life buoys? This can sometimes be misleading for fused rings but is commonly used.</returns>
        public static bool GetDelocalisedDonutsBondDisplay(this RendererModel model)
        {
            const string key = "DelocalisedDonutsBondDisplay";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultDelocalisedDonutsBondDisplay;

            return value;
        }

        /// <summary>
        /// Set render small delocalised rings as bonds/life buoys? This can sometimes be misleading for fused rings but is commonly used..
        /// </summary>
        public static void SetDelocalisedDonutsBondDisplay(this RendererModel model, bool value)
        {
            const string key = "DelocalisedDonutsBondDisplay";
            model.Parameters[key] = value;
        }

        public static bool HasDelocalisedDonutsBondDisplay(this RendererModel model)
        {
            const string key = "DelocalisedDonutsBondDisplay";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultArrowHeadWidth = 10;

        /// <summary>
        /// Get the width of the head of arrows. Default value is 10.
        /// </summary>
        /// <returns>the width of the head of arrows</returns>
        public static double GetArrowHeadWidth(this RendererModel model)
        {
            const string key = "ArrowHeadWidth";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultArrowHeadWidth;

            return value;
        }

        /// <summary>
        /// Set the width of the head of arrows.
        /// </summary>
        public static void SetArrowHeadWidth(this RendererModel model, double value)
        {
            const string key = "ArrowHeadWidth";
            model.Parameters[key] = value;
        }

        public static bool HasArrowHeadWidth(this RendererModel model)
        {
            const string key = "ArrowHeadWidth";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowTooltip = false;

        /// <summary>
        /// Get determines if tooltips are to be shown. Default value is false.
        /// </summary>
        /// <returns>determines if tooltips are to be shown</returns>
        public static bool GetShowTooltip(this RendererModel model)
        {
            const string key = "ShowTooltip";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowTooltip;

            return value;
        }

        /// <summary>
        /// Set determines if tooltips are to be shown.
        /// </summary>
        public static void SetShowTooltip(this RendererModel model, bool value)
        {
            const string key = "ShowTooltip";
            model.Parameters[key] = value;
        }

        public static bool HasShowTooltip(this RendererModel model)
        {
            const string key = "ShowTooltip";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowMoleculeTitle = false;

        /// <summary>
        /// Get determines if the molecule's title is depicted. Default value is false.
        /// </summary>
        /// <returns>determines if the molecule's title is depicted</returns>
        public static bool GetShowMoleculeTitle(this RendererModel model)
        {
            const string key = "ShowMoleculeTitle";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowMoleculeTitle;

            return value;
        }

        /// <summary>
        /// Set determines if the molecule's title is depicted.
        /// </summary>
        public static void SetShowMoleculeTitle(this RendererModel model, bool value)
        {
            const string key = "ShowMoleculeTitle";
            model.Parameters[key] = value;
        }

        public static bool HasShowMoleculeTitle(this RendererModel model)
        {
            const string key = "ShowMoleculeTitle";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowReactionTitle = false;

        /// <summary>
        /// Get determines if the reaction's title is depicted. Default value is false.
        /// </summary>
        /// <returns>determines if the reaction's title is depicted</returns>
        public static bool GetShowReactionTitle(this RendererModel model)
        {
            const string key = "ShowReactionTitle";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowReactionTitle;

            return value;
        }

        /// <summary>
        /// Set determines if the reaction's title is depicted.
        /// </summary>
        public static void SetShowReactionTitle(this RendererModel model, bool value)
        {
            const string key = "ShowReactionTitle";
            model.Parameters[key] = value;
        }

        public static bool HasShowReactionTitle(this RendererModel model)
        {
            const string key = "ShowReactionTitle";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultFitToScreen = false;

        /// <summary>
        /// Get If <see langword="true"/>, the scale is set such that the diagram fills the whole screen. Default value is false.
        /// </summary>
        /// <returns>If <see langword="true"/>, the scale is set such that the diagram fills the whole screen</returns>
        public static bool GetFitToScreen(this RendererModel model)
        {
            const string key = "FitToScreen";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultFitToScreen;

            return value;
        }

        /// <summary>
        /// Set If <see langword="true"/>, the scale is set such that the diagram fills the whole screen.
        /// </summary>
        public static void SetFitToScreen(this RendererModel model, bool value)
        {
            const string key = "FitToScreen";
            model.Parameters[key] = value;
        }

        public static bool HasFitToScreen(this RendererModel model)
        {
            const string key = "FitToScreen";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultScale = 1;

        /// <summary>
        /// Get the scale is the factor to multiply model coordinates by to convert the coordinates to screen space coordinate, such that the entire structure fits the visible screen dimension. Default value is 1.
        /// </summary>
        /// <returns>the scale is the factor to multiply model coordinates by to convert the coordinates to screen space coordinate, such that the entire structure fits the visible screen dimension</returns>
        public static double GetScale(this RendererModel model)
        {
            const string key = "Scale";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultScale;

            return value;
        }

        /// <summary>
        /// Set the scale is the factor to multiply model coordinates by to convert the coordinates to screen space coordinate, such that the entire structure fits the visible screen dimension.
        /// </summary>
        public static void SetScale(this RendererModel model, double value)
        {
            const string key = "Scale";
            model.Parameters[key] = value;
        }

        public static bool HasScale(this RendererModel model)
        {
            const string key = "Scale";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultBackgroundColor = WPF.Media.Colors.White;

        /// <summary>
        /// Get the background color of the drawn image. Default value is WPF.Media.Colors.White.
        /// </summary>
        /// <returns>the background color of the drawn image</returns>
        public static WPF.Media.Color GetBackgroundColor(this RendererModel model)
        {
            const string key = "BackgroundColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultBackgroundColor;

            return value;
        }

        /// <summary>
        /// Set the background color of the drawn image.
        /// </summary>
        public static void SetBackgroundColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "BackgroundColor";
            model.Parameters[key] = value;
        }

        public static bool HasBackgroundColor(this RendererModel model)
        {
            const string key = "BackgroundColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultBondLength = 40;

        /// <summary>
        /// Get the length on the screen of a typical bond. Default value is 40.
        /// </summary>
        /// <returns>the length on the screen of a typical bond</returns>
        public static double GetBondLength(this RendererModel model)
        {
            const string key = "BondLength";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultBondLength;

            return value;
        }

        /// <summary>
        /// Set the length on the screen of a typical bond.
        /// </summary>
        public static void SetBondLength(this RendererModel model, double value)
        {
            const string key = "BondLength";
            model.Parameters[key] = value;
        }

        public static bool HasBondLength(this RendererModel model)
        {
            const string key = "BondLength";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultForegroundColor = WPF.Media.Colors.Black;

        /// <summary>
        /// Get the foreground color, with which objects are drawn. Default value is WPF.Media.Colors.Black.
        /// </summary>
        /// <returns>the foreground color, with which objects are drawn</returns>
        public static WPF.Media.Color GetForegroundColor(this RendererModel model)
        {
            const string key = "ForegroundColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultForegroundColor;

            return value;
        }

        /// <summary>
        /// Set the foreground color, with which objects are drawn.
        /// </summary>
        public static void SetForegroundColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "ForegroundColor";
            model.Parameters[key] = value;
        }

        public static bool HasForegroundColor(this RendererModel model)
        {
            const string key = "ForegroundColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultUseAntiAliasing = true;

        /// <summary>
        /// Get if set to true, uses anti-aliasing for drawing. Default value is true.
        /// </summary>
        /// <returns>if set to true, uses anti-aliasing for drawing</returns>
        public static bool GetUseAntiAliasing(this RendererModel model)
        {
            const string key = "UseAntiAliasing";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultUseAntiAliasing;

            return value;
        }

        /// <summary>
        /// Set if set to true, uses anti-aliasing for drawing.
        /// </summary>
        public static void SetUseAntiAliasing(this RendererModel model, bool value)
        {
            const string key = "UseAntiAliasing";
            model.Parameters[key] = value;
        }

        public static bool HasUseAntiAliasing(this RendererModel model)
        {
            const string key = "UseAntiAliasing";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultMargin = 10;

        /// <summary>
        /// Get area on each of the four margins to keep empty. Default value is 10.
        /// </summary>
        /// <returns>area on each of the four margins to keep empty</returns>
        public static double GetMargin(this RendererModel model)
        {
            const string key = "Margin";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultMargin;

            return value;
        }

        /// <summary>
        /// Set area on each of the four margins to keep empty.
        /// </summary>
        public static void SetMargin(this RendererModel model, double value)
        {
            const string key = "Margin";
            model.Parameters[key] = value;
        }

        public static bool HasMargin(this RendererModel model)
        {
            const string key = "Margin";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly Fonts.FontWeight DefaultUsedFontStyle = Fonts.FontWeight.Normal;

        /// <summary>
        /// Get the font style to use for text. Default value is Fonts.FontWeight.Normal.
        /// </summary>
        /// <returns>the font style to use for text</returns>
        public static Fonts.FontWeight GetUsedFontStyle(this RendererModel model)
        {
            const string key = "UsedFontStyle";
            Fonts.FontWeight value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (Fonts.FontWeight)v;
            else
                model.Parameters[key] = value = DefaultUsedFontStyle;

            return value;
        }

        /// <summary>
        /// Set the font style to use for text.
        /// </summary>
        public static void SetUsedFontStyle(this RendererModel model, Fonts.FontWeight value)
        {
            const string key = "UsedFontStyle";
            model.Parameters[key] = value;
        }

        public static bool HasUsedFontStyle(this RendererModel model)
        {
            const string key = "UsedFontStyle";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly string DefaultFontName = "Arial";

        /// <summary>
        /// Get font name to use for text. Default value is "Arial".
        /// </summary>
        /// <returns>font name to use for text</returns>
        public static string GetFontName(this RendererModel model)
        {
            const string key = "FontName";
            string value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (string)v;
            else
                model.Parameters[key] = value = DefaultFontName;

            return value;
        }

        /// <summary>
        /// Set font name to use for text.
        /// </summary>
        public static void SetFontName(this RendererModel model, string value)
        {
            const string key = "FontName";
            model.Parameters[key] = value;
        }

        public static bool HasFontName(this RendererModel model)
        {
            const string key = "FontName";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultZoomFactor = 1;

        /// <summary>
        /// Get the zoom factor which is a user oriented parameter allowing the user to zoom in on parts of the molecule. Default value is 1.
        /// </summary>
        /// <returns>the zoom factor which is a user oriented parameter allowing the user to zoom in on parts of the molecule</returns>
        public static double GetZoomFactor(this RendererModel model)
        {
            const string key = "ZoomFactor";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultZoomFactor;

            return value;
        }

        /// <summary>
        /// Set the zoom factor which is a user oriented parameter allowing the user to zoom in on parts of the molecule.
        /// </summary>
        public static void SetZoomFactor(this RendererModel model, double value)
        {
            const string key = "ZoomFactor";
            model.Parameters[key] = value;
        }

        public static bool HasZoomFactor(this RendererModel model)
        {
            const string key = "ZoomFactor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultAtomColorByType = false;

        /// <summary>
        /// Get triggers atoms to be colored by type when set to true. Default value is false.
        /// </summary>
        /// <returns>triggers atoms to be colored by type when set to true</returns>
        public static bool GetAtomColorByType(this RendererModel model)
        {
            const string key = "AtomColorByType";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultAtomColorByType;

            return value;
        }

        /// <summary>
        /// Set triggers atoms to be colored by type when set to true.
        /// </summary>
        public static void SetAtomColorByType(this RendererModel model, bool value)
        {
            const string key = "AtomColorByType";
            model.Parameters[key] = value;
        }

        public static bool HasAtomColorByType(this RendererModel model)
        {
            const string key = "AtomColorByType";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowExplicitHydrogens = true;

        /// <summary>
        /// Get boolean property that triggers explicit hydrogens to be drawn if set to true. Default value is true.
        /// </summary>
        /// <returns>boolean property that triggers explicit hydrogens to be drawn if set to true</returns>
        public static bool GetShowExplicitHydrogens(this RendererModel model)
        {
            const string key = "ShowExplicitHydrogens";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowExplicitHydrogens;

            return value;
        }

        /// <summary>
        /// Set boolean property that triggers explicit hydrogens to be drawn if set to true.
        /// </summary>
        public static void SetShowExplicitHydrogens(this RendererModel model, bool value)
        {
            const string key = "ShowExplicitHydrogens";
            model.Parameters[key] = value;
        }

        public static bool HasShowExplicitHydrogens(this RendererModel model)
        {
            const string key = "ShowExplicitHydrogens";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowImplicitHydrogens = false;

        /// <summary>
        /// Get indicates implicit hydrogens should be depicted. Default value is false.
        /// </summary>
        /// <returns>indicates implicit hydrogens should be depicted</returns>
        public static bool GetShowImplicitHydrogens(this RendererModel model)
        {
            const string key = "ShowImplicitHydrogens";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowImplicitHydrogens;

            return value;
        }

        /// <summary>
        /// Set indicates implicit hydrogens should be depicted.
        /// </summary>
        public static void SetShowImplicitHydrogens(this RendererModel model, bool value)
        {
            const string key = "ShowImplicitHydrogens";
            model.Parameters[key] = value;
        }

        public static bool HasShowImplicitHydrogens(this RendererModel model)
        {
            const string key = "ShowImplicitHydrogens";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultAtomRadius = 8;

        /// <summary>
        /// Get magic number with unknown units that defines the radius around an atom, e.g. used for highlighting atoms. Default value is 8.
        /// </summary>
        /// <returns>magic number with unknown units that defines the radius around an atom, e.g. used for highlighting atoms</returns>
        public static double GetAtomRadius(this RendererModel model)
        {
            const string key = "AtomRadius";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultAtomRadius;

            return value;
        }

        /// <summary>
        /// Set magic number with unknown units that defines the radius around an atom, e.g. used for highlighting atoms.
        /// </summary>
        public static void SetAtomRadius(this RendererModel model, double value)
        {
            const string key = "AtomRadius";
            model.Parameters[key] = value;
        }

        public static bool HasAtomRadius(this RendererModel model)
        {
            const string key = "AtomRadius";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultCompactAtom = false;

        /// <summary>
        /// Get atoms to be drawn as  filled shapes. Default value is false.
        /// </summary>
        /// <returns>atoms to be drawn as  filled shapes</returns>
        public static bool GetCompactAtom(this RendererModel model)
        {
            const string key = "CompactAtom";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultCompactAtom;

            return value;
        }

        /// <summary>
        /// Set atoms to be drawn as  filled shapes.
        /// </summary>
        public static void SetCompactAtom(this RendererModel model, bool value)
        {
            const string key = "CompactAtom";
            model.Parameters[key] = value;
        }

        public static bool HasCompactAtom(this RendererModel model)
        {
            const string key = "CompactAtom";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultKekuleStructure = false;

        /// <summary>
        /// Get whether structures should be drawn as Kekulé structures, thus giving each carbon element explicitly, instead of not displaying the element symbol. Example C-C-C instead of /". Default value is false.
        /// </summary>
        /// <returns>whether structures should be drawn as Kekulé structures, thus giving each carbon element explicitly, instead of not displaying the element symbol. Example C-C-C instead of /"</returns>
        public static bool GetKekuleStructure(this RendererModel model)
        {
            const string key = "KekuleStructure";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultKekuleStructure;

            return value;
        }

        /// <summary>
        /// Set whether structures should be drawn as Kekulé structures, thus giving each carbon element explicitly, instead of not displaying the element symbol. Example C-C-C instead of /".
        /// </summary>
        public static void SetKekuleStructure(this RendererModel model, bool value)
        {
            const string key = "KekuleStructure";
            model.Parameters[key] = value;
        }

        public static bool HasKekuleStructure(this RendererModel model)
        {
            const string key = "KekuleStructure";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly AtomShapeType DefaultCompactShape = AtomShapeType.Square;

        /// <summary>
        /// Get Shape to be used when drawing atoms in compact mode, as defined by the <see cref="GetCompactAtom"/> parameter. Default value is AtomShapeType.Square.
        /// </summary>
        /// <returns>Shape to be used when drawing atoms in compact mode, as defined by the <see cref="GetCompactAtom"/> parameter</returns>
        public static AtomShapeType GetCompactShape(this RendererModel model)
        {
            const string key = "CompactShape";
            AtomShapeType value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (AtomShapeType)v;
            else
                model.Parameters[key] = value = DefaultCompactShape;

            return value;
        }

        /// <summary>
        /// Set Shape to be used when drawing atoms in compact mode, as defined by the <see cref="GetCompactAtom"/> parameter.
        /// </summary>
        public static void SetCompactShape(this RendererModel model, AtomShapeType value)
        {
            const string key = "CompactShape";
            model.Parameters[key] = value;
        }

        public static bool HasCompactShape(this RendererModel model)
        {
            const string key = "CompactShape";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowEndCarbons = false;

        /// <summary>
        /// Get show carbons with only one (non-hydrogen) neighbor to be drawn with an element symbol. Default value is false.
        /// </summary>
        /// <returns>show carbons with only one (non-hydrogen) neighbor to be drawn with an element symbol</returns>
        public static bool GetShowEndCarbons(this RendererModel model)
        {
            const string key = "ShowEndCarbons";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowEndCarbons;

            return value;
        }

        /// <summary>
        /// Set show carbons with only one (non-hydrogen) neighbor to be drawn with an element symbol.
        /// </summary>
        public static void SetShowEndCarbons(this RendererModel model, bool value)
        {
            const string key = "ShowEndCarbons";
            model.Parameters[key] = value;
        }

        public static bool HasShowEndCarbons(this RendererModel model)
        {
            const string key = "ShowEndCarbons";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowAtomTypeNames = false;

        /// <summary>
        /// Get indicates atom type names should be given instead of element symbols. Default value is false.
        /// </summary>
        /// <returns>indicates atom type names should be given instead of element symbols</returns>
        public static bool GetShowAtomTypeNames(this RendererModel model)
        {
            const string key = "ShowAtomTypeNames";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowAtomTypeNames;

            return value;
        }

        /// <summary>
        /// Set indicates atom type names should be given instead of element symbols.
        /// </summary>
        public static void SetShowAtomTypeNames(this RendererModel model, bool value)
        {
            const string key = "ShowAtomTypeNames";
            model.Parameters[key] = value;
        }

        public static bool HasShowAtomTypeNames(this RendererModel model)
        {
            const string key = "ShowAtomTypeNames";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultAtomNumberTextColor = WPF.Media.Colors.Black;

        /// <summary>
        /// Get color to draw the atom numbers with. Default value is WPF.Media.Colors.Black.
        /// </summary>
        /// <returns>color to draw the atom numbers with</returns>
        public static WPF.Media.Color GetAtomNumberTextColor(this RendererModel model)
        {
            const string key = "AtomNumberTextColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultAtomNumberTextColor;

            return value;
        }

        /// <summary>
        /// Set color to draw the atom numbers with.
        /// </summary>
        public static void SetAtomNumberTextColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "AtomNumberTextColor";
            model.Parameters[key] = value;
        }

        public static bool HasAtomNumberTextColor(this RendererModel model)
        {
            const string key = "AtomNumberTextColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultWillDrawAtomNumbers = true;

        /// <summary>
        /// Get indicating if atom numbers should be drawn, allowing this feature to be disabled temporarily. Default value is true.
        /// </summary>
        /// <returns>indicating if atom numbers should be drawn, allowing this feature to be disabled temporarily</returns>
        public static bool GetWillDrawAtomNumbers(this RendererModel model)
        {
            const string key = "WillDrawAtomNumbers";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultWillDrawAtomNumbers;

            return value;
        }

        /// <summary>
        /// Set indicating if atom numbers should be drawn, allowing this feature to be disabled temporarily.
        /// </summary>
        public static void SetWillDrawAtomNumbers(this RendererModel model, bool value)
        {
            const string key = "WillDrawAtomNumbers";
            model.Parameters[key] = value;
        }

        public static bool HasWillDrawAtomNumbers(this RendererModel model)
        {
            const string key = "WillDrawAtomNumbers";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly IAtomColorer DefaultAtomNumberColorer = new CDK2DAtomColors();

        /// <summary>
        /// Get the color scheme by which to color the atom numbers. Default value is new CDK2DAtomColors().
        /// </summary>
        /// <returns>the color scheme by which to color the atom numbers</returns>
        public static IAtomColorer GetAtomNumberColorer(this RendererModel model)
        {
            const string key = "AtomNumberColorer";
            IAtomColorer value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (IAtomColorer)v;
            else
                model.Parameters[key] = value = DefaultAtomNumberColorer;

            return value;
        }

        /// <summary>
        /// Set the color scheme by which to color the atom numbers.
        /// </summary>
        public static void SetAtomNumberColorer(this RendererModel model, IAtomColorer value)
        {
            const string key = "AtomNumberColorer";
            model.Parameters[key] = value;
        }

        public static bool HasAtomNumberColorer(this RendererModel model)
        {
            const string key = "AtomNumberColorer";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultAtomNumberColorByType = false;

        /// <summary>
        /// Get indicate of the <see cref="GetAtomNumberColorer"/> scheme will be used. Default value is false.
        /// </summary>
        /// <returns>indicate of the <see cref="GetAtomNumberColorer"/> scheme will be used</returns>
        public static bool GetAtomNumberColorByType(this RendererModel model)
        {
            const string key = "AtomNumberColorByType";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultAtomNumberColorByType;

            return value;
        }

        /// <summary>
        /// Set indicate of the <see cref="GetAtomNumberColorer"/> scheme will be used.
        /// </summary>
        public static void SetAtomNumberColorByType(this RendererModel model, bool value)
        {
            const string key = "AtomNumberColorByType";
            model.Parameters[key] = value;
        }

        public static bool HasAtomNumberColorByType(this RendererModel model)
        {
            const string key = "AtomNumberColorByType";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly Vector2 DefaultAtomNumberOffset = Vector2.Zero;

        /// <summary>
        /// Get offset vector in screen space coordinates where the atom number label will be placed. Default value is Vector2.Zero.
        /// </summary>
        /// <returns>offset vector in screen space coordinates where the atom number label will be placed</returns>
        public static Vector2 GetAtomNumberOffset(this RendererModel model)
        {
            const string key = "AtomNumberOffset";
            Vector2 value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (Vector2)v;
            else
                model.Parameters[key] = value = DefaultAtomNumberOffset;

            return value;
        }

        /// <summary>
        /// Set offset vector in screen space coordinates where the atom number label will be placed.
        /// </summary>
        public static void SetAtomNumberOffset(this RendererModel model, Vector2 value)
        {
            const string key = "AtomNumberOffset";
            model.Parameters[key] = value;
        }

        public static bool HasAtomNumberOffset(this RendererModel model)
        {
            const string key = "AtomNumberOffset";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultBondWidth = 1;

        /// <summary>
        /// Get the width on screen of a bond. Default value is 1.
        /// </summary>
        /// <returns>the width on screen of a bond</returns>
        public static double GetBondWidth(this RendererModel model)
        {
            const string key = "BondWidth";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultBondWidth;

            return value;
        }

        /// <summary>
        /// Set the width on screen of a bond.
        /// </summary>
        public static void SetBondWidth(this RendererModel model, double value)
        {
            const string key = "BondWidth";
            model.Parameters[key] = value;
        }

        public static bool HasBondWidth(this RendererModel model)
        {
            const string key = "BondWidth";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultBondDistance = 2;

        /// <summary>
        /// Get the gap between double and triple bond lines on the screen. Default value is 2.
        /// </summary>
        /// <returns>the gap between double and triple bond lines on the screen</returns>
        public static double GetBondDistance(this RendererModel model)
        {
            const string key = "BondDistance";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultBondDistance;

            return value;
        }

        /// <summary>
        /// Set the gap between double and triple bond lines on the screen.
        /// </summary>
        public static void SetBondDistance(this RendererModel model, double value)
        {
            const string key = "BondDistance";
            model.Parameters[key] = value;
        }

        public static bool HasBondDistance(this RendererModel model)
        {
            const string key = "BondDistance";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultDefaultBondColor = WPF.Media.Colors.Black;

        /// <summary>
        /// Get the color to draw bonds if not other color is given. Default value is WPF.Media.Colors.Black.
        /// </summary>
        /// <returns>the color to draw bonds if not other color is given</returns>
        public static WPF.Media.Color GetDefaultBondColor(this RendererModel model)
        {
            const string key = "DefaultBondColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultDefaultBondColor;

            return value;
        }

        /// <summary>
        /// Set the color to draw bonds if not other color is given.
        /// </summary>
        public static void SetDefaultBondColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "DefaultBondColor";
            model.Parameters[key] = value;
        }

        public static bool HasDefaultBondColor(this RendererModel model)
        {
            const string key = "DefaultBondColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultWedgeWidth = 2;

        /// <summary>
        /// Get the width on screen of the fat end of a wedge bond. Default value is 2.
        /// </summary>
        /// <returns>the width on screen of the fat end of a wedge bond</returns>
        public static double GetWedgeWidth(this RendererModel model)
        {
            const string key = "WedgeWidth";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultWedgeWidth;

            return value;
        }

        /// <summary>
        /// Set the width on screen of the fat end of a wedge bond.
        /// </summary>
        public static void SetWedgeWidth(this RendererModel model, double value)
        {
            const string key = "WedgeWidth";
            model.Parameters[key] = value;
        }

        public static bool HasWedgeWidth(this RendererModel model)
        {
            const string key = "WedgeWidth";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultTowardsRingCenterProportion = 0.15;

        /// <summary>
        /// Get the proportion to move in towards the ring center. Default value is 0.15.
        /// </summary>
        /// <returns>the proportion to move in towards the ring center</returns>
        public static double GetTowardsRingCenterProportion(this RendererModel model)
        {
            const string key = "TowardsRingCenterProportion";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultTowardsRingCenterProportion;

            return value;
        }

        /// <summary>
        /// Set the proportion to move in towards the ring center.
        /// </summary>
        public static void SetTowardsRingCenterProportion(this RendererModel model, double value)
        {
            const string key = "TowardsRingCenterProportion";
            model.Parameters[key] = value;
        }

        public static bool HasTowardsRingCenterProportion(this RendererModel model)
        {
            const string key = "TowardsRingCenterProportion";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowAromaticity = true;

        /// <summary>
        /// Get whether rings should be drawn with a circle if they are aromatic. Default value is true.
        /// </summary>
        /// <returns>whether rings should be drawn with a circle if they are aromatic</returns>
        public static bool GetShowAromaticity(this RendererModel model)
        {
            const string key = "ShowAromaticity";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowAromaticity;

            return value;
        }

        /// <summary>
        /// Set whether rings should be drawn with a circle if they are aromatic.
        /// </summary>
        public static void SetShowAromaticity(this RendererModel model, bool value)
        {
            const string key = "ShowAromaticity";
            model.Parameters[key] = value;
        }

        public static bool HasShowAromaticity(this RendererModel model)
        {
            const string key = "ShowAromaticity";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultCDKStyleAromaticity = false;

        /// <summary>
        /// Get depicts aromaticity of rings in the original CDK style. Default value is false.
        /// </summary>
        /// <returns>depicts aromaticity of rings in the original CDK style</returns>
        public static bool GetCDKStyleAromaticity(this RendererModel model)
        {
            const string key = "CDKStyleAromaticity";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultCDKStyleAromaticity;

            return value;
        }

        /// <summary>
        /// Set depicts aromaticity of rings in the original CDK style.
        /// </summary>
        public static void SetCDKStyleAromaticity(this RendererModel model, bool value)
        {
            const string key = "CDKStyleAromaticity";
            model.Parameters[key] = value;
        }

        public static bool HasCDKStyleAromaticity(this RendererModel model)
        {
            const string key = "CDKStyleAromaticity";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly int DefaultMaxDrawableAromaticRing = 8;

        /// <summary>
        /// Get the maximum ring size for which an aromatic ring should be drawn. Default value is 8.
        /// </summary>
        /// <returns>the maximum ring size for which an aromatic ring should be drawn</returns>
        public static int GetMaxDrawableAromaticRing(this RendererModel model)
        {
            const string key = "MaxDrawableAromaticRing";
            int value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (int)v;
            else
                model.Parameters[key] = value = DefaultMaxDrawableAromaticRing;

            return value;
        }

        /// <summary>
        /// Set the maximum ring size for which an aromatic ring should be drawn.
        /// </summary>
        public static void SetMaxDrawableAromaticRing(this RendererModel model, int value)
        {
            const string key = "MaxDrawableAromaticRing";
            model.Parameters[key] = value;
        }

        public static bool HasMaxDrawableAromaticRing(this RendererModel model)
        {
            const string key = "MaxDrawableAromaticRing";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultRingProportion = 0.35;

        /// <summary>
        /// Get the proportion of a ring bounds to use to draw the ring. Default value is 0.35.
        /// </summary>
        /// <returns>the proportion of a ring bounds to use to draw the ring</returns>
        public static double GetRingProportion(this RendererModel model)
        {
            const string key = "RingProportion";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultRingProportion;

            return value;
        }

        /// <summary>
        /// Set the proportion of a ring bounds to use to draw the ring.
        /// </summary>
        public static void SetRingProportion(this RendererModel model, double value)
        {
            const string key = "RingProportion";
            model.Parameters[key] = value;
        }

        public static bool HasRingProportion(this RendererModel model)
        {
            const string key = "RingProportion";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowReactionBoxes = true;

        /// <summary>
        /// Get indicates if boxes are drawn around the reaction. Default value is true.
        /// </summary>
        /// <returns>indicates if boxes are drawn around the reaction</returns>
        public static bool GetShowReactionBoxes(this RendererModel model)
        {
            const string key = "ShowReactionBoxes";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowReactionBoxes;

            return value;
        }

        /// <summary>
        /// Set indicates if boxes are drawn around the reaction.
        /// </summary>
        public static void SetShowReactionBoxes(this RendererModel model, bool value)
        {
            const string key = "ShowReactionBoxes";
            model.Parameters[key] = value;
        }

        public static bool HasShowReactionBoxes(this RendererModel model)
        {
            const string key = "ShowReactionBoxes";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultAtomAtomMappingLineColor = WPF.Media.Colors.Gray;

        /// <summary>
        /// Get the color on screen of an atom-atom mapping line. Default value is WPF.Media.Colors.Gray.
        /// </summary>
        /// <returns>the color on screen of an atom-atom mapping line</returns>
        public static WPF.Media.Color GetAtomAtomMappingLineColor(this RendererModel model)
        {
            const string key = "AtomAtomMappingLineColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultAtomAtomMappingLineColor;

            return value;
        }

        /// <summary>
        /// Set the color on screen of an atom-atom mapping line.
        /// </summary>
        public static void SetAtomAtomMappingLineColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "AtomAtomMappingLineColor";
            model.Parameters[key] = value;
        }

        public static bool HasAtomAtomMappingLineColor(this RendererModel model)
        {
            const string key = "AtomAtomMappingLineColor";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultMappingLineWidth = 1;

        /// <summary>
        /// Get the width on screen of an atom-atom mapping line. Default value is 1.
        /// </summary>
        /// <returns>the width on screen of an atom-atom mapping line</returns>
        public static double GetMappingLineWidth(this RendererModel model)
        {
            const string key = "MappingLineWidth";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultMappingLineWidth;

            return value;
        }

        /// <summary>
        /// Set the width on screen of an atom-atom mapping line.
        /// </summary>
        public static void SetMappingLineWidth(this RendererModel model, double value)
        {
            const string key = "MappingLineWidth";
            model.Parameters[key] = value;
        }

        public static bool HasMappingLineWidth(this RendererModel model)
        {
            const string key = "MappingLineWidth";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly bool DefaultShowAtomAtomMapping = true;

        /// <summary>
        /// Get whether atom-atom mapping depiction can be temporarily disabled. Default value is true.
        /// </summary>
        /// <returns>whether atom-atom mapping depiction can be temporarily disabled</returns>
        public static bool GetShowAtomAtomMapping(this RendererModel model)
        {
            const string key = "ShowAtomAtomMapping";
            bool value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (bool)v;
            else
                model.Parameters[key] = value = DefaultShowAtomAtomMapping;

            return value;
        }

        /// <summary>
        /// Set whether atom-atom mapping depiction can be temporarily disabled.
        /// </summary>
        public static void SetShowAtomAtomMapping(this RendererModel model, bool value)
        {
            const string key = "ShowAtomAtomMapping";
            model.Parameters[key] = value;
        }

        public static bool HasShowAtomAtomMapping(this RendererModel model)
        {
            const string key = "ShowAtomAtomMapping";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly double DefaultHighlightRadius = 10;

        /// <summary>
        /// Get the atom radius on screen used to provide the highlight colors. Default value is 10.
        /// </summary>
        /// <returns>the atom radius on screen used to provide the highlight colors</returns>
        public static double GetHighlightRadius(this RendererModel model)
        {
            const string key = "HighlightRadius";
            double value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (double)v;
            else
                model.Parameters[key] = value = DefaultHighlightRadius;

            return value;
        }

        /// <summary>
        /// Set the atom radius on screen used to provide the highlight colors.
        /// </summary>
        public static void SetHighlightRadius(this RendererModel model, double value)
        {
            const string key = "HighlightRadius";
            model.Parameters[key] = value;
        }

        public static bool HasHighlightRadius(this RendererModel model)
        {
            const string key = "HighlightRadius";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly NCDK.Renderers.Generators.IPalette DefaultHighlightPalette = NCDK.Renderers.Generators.HighlightGenerator.DefaultPalette;

        /// <summary>
        /// Get color palette used to provide the highlight colors. Default value is NCDK.Renderers.Generators.HighlightGenerator.DefaultPalette.
        /// </summary>
        /// <returns>color palette used to provide the highlight colors</returns>
        public static NCDK.Renderers.Generators.IPalette GetHighlightPalette(this RendererModel model)
        {
            const string key = "HighlightPalette";
            NCDK.Renderers.Generators.IPalette value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (NCDK.Renderers.Generators.IPalette)v;
            else
                model.Parameters[key] = value = DefaultHighlightPalette;

            return value;
        }

        /// <summary>
        /// Set color palette used to provide the highlight colors.
        /// </summary>
        public static void SetHighlightPalette(this RendererModel model, NCDK.Renderers.Generators.IPalette value)
        {
            const string key = "HighlightPalette";
            model.Parameters[key] = value;
        }

        public static bool HasHighlightPalette(this RendererModel model)
        {
            const string key = "HighlightPalette";
            return model.Parameters.ContainsKey(key);
        }

        public static readonly WPF.Media.Color DefaultBoundsColor = WPF.Media.Colors.LightGray;

        /// <summary>
        /// Get the color of the box drawn at the bounds of a molecule, molecule set, or reaction. Default value is WPF.Media.Colors.LightGray.
        /// </summary>
        /// <returns>the color of the box drawn at the bounds of a molecule, molecule set, or reaction</returns>
        public static WPF.Media.Color GetBoundsColor(this RendererModel model)
        {
            const string key = "BoundsColor";
            WPF.Media.Color value;
            if (model.Parameters.TryGetValue(key, out object v))
                value = (WPF.Media.Color)v;
            else
                model.Parameters[key] = value = DefaultBoundsColor;

            return value;
        }

        /// <summary>
        /// Set the color of the box drawn at the bounds of a molecule, molecule set, or reaction.
        /// </summary>
        public static void SetBoundsColor(this RendererModel model, WPF.Media.Color value)
        {
            const string key = "BoundsColor";
            model.Parameters[key] = value;
        }

        public static bool HasBoundsColor(this RendererModel model)
        {
            const string key = "BoundsColor";
            return model.Parameters.ContainsKey(key);
        }

    }
}
