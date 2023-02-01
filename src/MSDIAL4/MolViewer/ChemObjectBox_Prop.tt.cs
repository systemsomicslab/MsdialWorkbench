

using NCDK.Depict;
using NCDK.Renderers;
using NCDK.Renderers.Colors;
using System.Collections.Generic;
using System.Windows;
using WPF = System.Windows;

namespace NCDK.MolViewer
{
    public partial class ChemObjectBox : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty ChemObjectProperty =
            DependencyProperty.Register(
                "ChemObject",
                typeof(IChemObject),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (IChemObject)(null),
                    new PropertyChangedCallback(OnChemObjectPropertyChanged)));

        public IChemObject ChemObject
        {
            get { return (IChemObject)GetValue(ChemObjectProperty); }
            set { SetValue(ChemObjectProperty, value); }
        }

        private static void OnChemObjectPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                var value = (IChemObject)e.NewValue;var old = o._ChemObject;if (old != value) {    o._ChemObject = value;    o.ChemObjectChanged?.Invoke(d, new ChemObjectChangedEventArgs(old, value));}
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty HighlightingObjectsProperty =
            DependencyProperty.Register(
                "HighlightingObjects",
                typeof(string),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (string)(""),
                    new PropertyChangedCallback(OnHighlightingObjectsPropertyChanged)));

        public string HighlightingObjects
        {
            get { return (string)GetValue(HighlightingObjectsProperty); }
            set { SetValue(HighlightingObjectsProperty, value); }
        }

        private static void OnHighlightingObjectsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o._HighlightObjects = (string)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty AtomColorerProperty =
            DependencyProperty.Register(
                "AtomColorer",
                typeof(IAtomColorer),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (IAtomColorer)(new UniColor(WPF.Media.Colors.Black)),
                    new PropertyChangedCallback(OnAtomColorerPropertyChanged)));

        public IAtomColorer AtomColorer
        {
            get { return (IAtomColorer)GetValue(AtomColorerProperty); }
            set { SetValue(AtomColorerProperty, value); }
        }

        private static void OnAtomColorerPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.AtomColorer = (IAtomColorer)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty BackgroundColorProperty =
            DependencyProperty.Register(
                "BackgroundColor",
                typeof(WPF.Media.Color),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (WPF.Media.Color)(WPF.Media.Colors.Transparent),
                    new PropertyChangedCallback(OnBackgroundColorPropertyChanged)));

        public WPF.Media.Color BackgroundColor
        {
            get { return (WPF.Media.Color)GetValue(BackgroundColorProperty); }
            set { SetValue(BackgroundColorProperty, value); }
        }

        private static void OnBackgroundColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.BackgroundColor = (WPF.Media.Color)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty HighlightingProperty =
            DependencyProperty.Register(
                "Highlighting",
                typeof(HighlightStyle),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (HighlightStyle)(HighlightStyle.None),
                    new PropertyChangedCallback(OnHighlightingPropertyChanged)));

        public HighlightStyle Highlighting
        {
            get { return (HighlightStyle)GetValue(HighlightingProperty); }
            set { SetValue(HighlightingProperty, value); }
        }

        private static void OnHighlightingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.Highlighting = (HighlightStyle)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty OuterGlowWidthProperty =
            DependencyProperty.Register(
                "OuterGlowWidth",
                typeof(double),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (double)(RendererModelTools.DefaultOuterGlowWidth),
                    new PropertyChangedCallback(OnOuterGlowWidthPropertyChanged)));

        public double OuterGlowWidth
        {
            get { return (double)GetValue(OuterGlowWidthProperty); }
            set { SetValue(OuterGlowWidthProperty, value); }
        }

        private static void OnOuterGlowWidthPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.OuterGlowWidth = (double)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty AlignMappedReactionProperty =
            DependencyProperty.Register(
                "AlignMappedReaction",
                typeof(bool),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (bool)(true),
                    new PropertyChangedCallback(OnAlignMappedReactionPropertyChanged)));

        public bool AlignMappedReaction
        {
            get { return (bool)GetValue(AlignMappedReactionProperty); }
            set { SetValue(AlignMappedReactionProperty, value); }
        }

        private static void OnAlignMappedReactionPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.AlignMappedReaction = (bool)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty AnnotateAtomNumbersProperty =
            DependencyProperty.Register(
                "AnnotateAtomNumbers",
                typeof(bool),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (bool)(false),
                    new PropertyChangedCallback(OnAnnotateAtomNumbersPropertyChanged)));

        public bool AnnotateAtomNumbers
        {
            get { return (bool)GetValue(AnnotateAtomNumbersProperty); }
            set { SetValue(AnnotateAtomNumbersProperty, value); }
        }

        private static void OnAnnotateAtomNumbersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.AnnotateAtomNumbers = (bool)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty AnnotateAtomValuesProperty =
            DependencyProperty.Register(
                "AnnotateAtomValues",
                typeof(bool),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (bool)(false),
                    new PropertyChangedCallback(OnAnnotateAtomValuesPropertyChanged)));

        public bool AnnotateAtomValues
        {
            get { return (bool)GetValue(AnnotateAtomValuesProperty); }
            set { SetValue(AnnotateAtomValuesProperty, value); }
        }

        private static void OnAnnotateAtomValuesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.AnnotateAtomValues = (bool)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty AnnotateAtomMapNumbersProperty =
            DependencyProperty.Register(
                "AnnotateAtomMapNumbers",
                typeof(bool),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (bool)(false),
                    new PropertyChangedCallback(OnAnnotateAtomMapNumbersPropertyChanged)));

        public bool AnnotateAtomMapNumbers
        {
            get { return (bool)GetValue(AnnotateAtomMapNumbersProperty); }
            set { SetValue(AnnotateAtomMapNumbersProperty, value); }
        }

        private static void OnAnnotateAtomMapNumbersPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.AnnotateAtomMapNumbers = (bool)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty AtomMapColorsProperty =
            DependencyProperty.Register(
                "AtomMapColors",
                typeof(IReadOnlyList<WPF.Media.Color>),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (IReadOnlyList<WPF.Media.Color>)(null),
                    new PropertyChangedCallback(OnAtomMapColorsPropertyChanged)));

        public IReadOnlyList<WPF.Media.Color> AtomMapColors
        {
            get { return (IReadOnlyList<WPF.Media.Color>)GetValue(AtomMapColorsProperty); }
            set { SetValue(AtomMapColorsProperty, value); }
        }

        private static void OnAtomMapColorsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.AtomMapColors = (IReadOnlyList<WPF.Media.Color>)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty AnnotationColorProperty =
            DependencyProperty.Register(
                "AnnotationColor",
                typeof(WPF.Media.Color),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (WPF.Media.Color)(RendererModelTools.DefaultAnnotationColor),
                    new PropertyChangedCallback(OnAnnotationColorPropertyChanged)));

        public WPF.Media.Color AnnotationColor
        {
            get { return (WPF.Media.Color)GetValue(AnnotationColorProperty); }
            set { SetValue(AnnotationColorProperty, value); }
        }

        private static void OnAnnotationColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.AnnotationColor = (WPF.Media.Color)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty AnnotationFontScaleProperty =
            DependencyProperty.Register(
                "AnnotationFontScale",
                typeof(double),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (double)(RendererModelTools.DefaultAnnotationFontScale),
                    new PropertyChangedCallback(OnAnnotationFontScalePropertyChanged)));

        public double AnnotationFontScale
        {
            get { return (double)GetValue(AnnotationFontScaleProperty); }
            set { SetValue(AnnotationFontScaleProperty, value); }
        }

        private static void OnAnnotationFontScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.AnnotationFontScale = (double)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty TitleColorProperty =
            DependencyProperty.Register(
                "TitleColor",
                typeof(WPF.Media.Color),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (WPF.Media.Color)(RendererModelTools.DefaultTitleColor),
                    new PropertyChangedCallback(OnTitleColorPropertyChanged)));

        public WPF.Media.Color TitleColor
        {
            get { return (WPF.Media.Color)GetValue(TitleColorProperty); }
            set { SetValue(TitleColorProperty, value); }
        }

        private static void OnTitleColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.TitleColor = (WPF.Media.Color)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty TitleFontScaleProperty =
            DependencyProperty.Register(
                "TitleFontScale",
                typeof(double),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (double)(RendererModelTools.DefaultTitleFontScale),
                    new PropertyChangedCallback(OnTitleFontScalePropertyChanged)));

        public double TitleFontScale
        {
            get { return (double)GetValue(TitleFontScaleProperty); }
            set { SetValue(TitleFontScaleProperty, value); }
        }

        private static void OnTitleFontScalePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.TitleFontScale = (double)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty SymbolVisibilityProperty =
            DependencyProperty.Register(
                "SymbolVisibility",
                typeof(SymbolVisibility),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (SymbolVisibility)(RendererModelTools.DefaultVisibility),
                    new PropertyChangedCallback(OnSymbolVisibilityPropertyChanged)));

        public SymbolVisibility SymbolVisibility
        {
            get { return (SymbolVisibility)GetValue(SymbolVisibilityProperty); }
            set { SetValue(SymbolVisibilityProperty, value); }
        }

        private static void OnSymbolVisibilityPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.SymbolVisibility = (SymbolVisibility)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty DepictionZoomProperty =
            DependencyProperty.Register(
                "DepictionZoom",
                typeof(double),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (double)(1),
                    new PropertyChangedCallback(OnDepictionZoomPropertyChanged)));

        public double DepictionZoom
        {
            get { return (double)GetValue(DepictionZoomProperty); }
            set { SetValue(DepictionZoomProperty, value); }
        }

        private static void OnDepictionZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.Zoom = (double)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty DepictionPaddingProperty =
            DependencyProperty.Register(
                "DepictionPadding",
                typeof(double),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (double)(RendererModelTools.DefaultPadding),
                    new PropertyChangedCallback(OnDepictionPaddingPropertyChanged)));

        public double DepictionPadding
        {
            get { return (double)GetValue(DepictionPaddingProperty); }
            set { SetValue(DepictionPaddingProperty, value); }
        }

        private static void OnDepictionPaddingPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.Padding = (double)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty DepictionMarginProperty =
            DependencyProperty.Register(
                "DepictionMargin",
                typeof(double),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (double)(RendererModelTools.DefaultMargin),
                    new PropertyChangedCallback(OnDepictionMarginPropertyChanged)));

        public double DepictionMargin
        {
            get { return (double)GetValue(DepictionMarginProperty); }
            set { SetValue(DepictionMarginProperty, value); }
        }

        private static void OnDepictionMarginPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.Margin = (double)e.NewValue;
                o.UpdateVisual();
            }
        }
        public static readonly DependencyProperty DepictionSizeProperty =
            DependencyProperty.Register(
                "DepictionSize",
                typeof(WPF.Size),
                typeof(ChemObjectBox),
                new FrameworkPropertyMetadata(
                    (WPF.Size)(WPF.Size.Empty),
                    new PropertyChangedCallback(OnDepictionSizePropertyChanged)));

        public WPF.Size DepictionSize
        {
            get { return (WPF.Size)GetValue(DepictionSizeProperty); }
            set { SetValue(DepictionSizeProperty, value); }
        }

        private static void OnDepictionSizePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ChemObjectBox o)
            {
                o.Generator.Size = (WPF.Size)e.NewValue;
                o.UpdateVisual();
            }
        }
    }
}
