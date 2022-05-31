using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcimms
{
    class LcimmsAlignmentModel : AlignmentModelBase
    {
        public LcimmsAlignmentModel(
            AlignmentFileBean alignmentFileBean,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseMapper mapper,
            ParameterBase parameter,
            List<AnalysisFileBean> files,
            PeakFilterModel peakFilterModel)
            : base(alignmentFileBean.FilePath) {
            Parameter = parameter;
            AlignmentFile = alignmentFileBean;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            ResultFile = alignmentFileBean.FilePath;

            BarItemsLoader = new HeightBarItemsLoader(parameter.FileID_ClassName);
            var observableBarItemsLoader = Observable.Return(BarItemsLoader);
            Ms1Spots = new ObservableCollection<AlignmentSpotPropertyModel>(Container.AlignmentSpotProperties.Select(prop => new AlignmentSpotPropertyModel(prop, observableBarItemsLoader)));
            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(Ms1Spots, peakFilterModel, evaluator, useRtFilter: true);

            Brushes = new List<BrushMapData<AlignmentSpotPropertyModel>>
            {
                new BrushMapData<AlignmentSpotPropertyModel>(
                    new KeyBrushMapper<AlignmentSpotPropertyModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        spot => spot?.Ontology ?? string.Empty,
                        Color.FromArgb(180, 181, 181, 181)),
                    "Ontology"),
                new BrushMapData<AlignmentSpotPropertyModel>(
                    new DelegateBrushMapper<AlignmentSpotPropertyModel>(
                        spot => Color.FromArgb(
                            180,
                            (byte)(255 * spot.innerModel.RelativeAmplitudeValue),
                            (byte)(255 * (1 - Math.Abs(spot.innerModel.RelativeAmplitudeValue - 0.5))),
                            (byte)(255 - 255 * spot.innerModel.RelativeAmplitudeValue)),
                        enableCache: true),
                    "Amplitude"),
            };
            switch (parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    SelectedBrush = Brushes[0];
                    break;
                case TargetOmics.Proteomics:
                case TargetOmics.Metabolomics:
                    SelectedBrush = Brushes[1];
                    break;
            }

            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel>().AddTo(Disposables);
            var fileName = alignmentFileBean.FileName;
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel);
            PlotModel = new AlignmentPeakPlotModel(Ms1Spots, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource, SelectedBrush, Brushes)
            {
                GraphTitle = fileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Drift time [1/k0]",
                VerticalTitle = "m/z",
            }.AddTo(Disposables);

            var loader = new MSDecLoader(alignmentFileBean.SpectraFilePath);
            var decLoader = new MsDecSpectrumLoader(loader, Ms1Spots);
            var refLoader = new MsRefSpectrumLoader(mapper);
            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               Parameter.ProjectParam.SpectrumCommentToColorBytes
               .ToDictionary(
                   kvp => kvp.Key,
                   kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
               ),
               item => item.ToString(),
               Colors.Blue);
            var lowerSpecBrush = new DelegateBrushMapper<SpectrumComment>(
                comment =>
                {
                    var commentString = comment.ToString();
                    var projectParameter = Parameter.ProjectParam;
                    if (projectParameter.SpectrumCommentToColorBytes.TryGetValue(commentString, out var color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else if ((comment & SpectrumComment.doublebond) == SpectrumComment.doublebond
                        && projectParameter.SpectrumCommentToColorBytes.TryGetValue(SpectrumComment.doublebond.ToString(), out color)) {
                        return Color.FromRgb(color[0], color[1], color[2]);
                    }
                    else {
                        return Colors.Red;
                    }
                },
                true);
            Ms2SpectrumModel = MsSpectrumModel.Create(
                Target, decLoader, refLoader,
                peak => peak.Mass,
                peak => peak.Intensity,
                "Representation vs. Reference",
                "m/z",
                "Abundance",
                nameof(SpectrumPeak.Mass),
                nameof(SpectrumPeak.Intensity),
                nameof(SpectrumPeak.Mass),
                nameof(SpectrumPeak.Intensity),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush)).AddTo(Disposables);

            var classBrush = new KeyBrushMapper<BarItem, string>(
                Parameter.ProjectParam.ClassnameToColorBytes
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2])
                ),
                item => item.Class,
                Colors.Blue);
            BarChartModel = BarChartModel.Create(Target, Observable.Return(BarItemsLoader), Observable.Return(classBrush)).AddTo(Disposables);
            BarChartModel.Elements.HorizontalTitle = "Class";
            BarChartModel.Elements.VerticalTitle = "Height";
            BarChartModel.Elements.HorizontalProperty = nameof(BarItem.Class);
            BarChartModel.Elements.VerticalProperty = nameof(BarItem.Height);

            var eicFile = alignmentFileBean.EicFilePath;
            var classToColor = parameter.ClassnameToColorBytes
                .ToDictionary(kvp => kvp.Key, kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2]));
            var eicLoader = new AlignmentEicLoader(chromatogramSpotSerializer, eicFile, Observable.Return(parameter.FileID_ClassName), Observable.Return(classToColor));
            AlignmentEicModel = AlignmentEicModel.Create(
                Target, eicLoader, files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Drift time [1/k0]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            // AlignmentSpotTableModel = new LcimmsAlignmentSpotTableModel(Ms1Spots, TargetZZZ, MassMin, MassMax, DriftMin, DriftMax);

            MsdecResult = Target.Where(t => t != null)
                .Select(t => loader.LoadMSDecResult(t.MasterAlignmentID))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            var rtSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, RtTol, Target.Select(t => t?.TimesCenter ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var dtSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, DtTol, Target.Select(t => t?.TimesCenter ?? 0d), "F2", "Drift time(min)", isItalic: false).AddTo(Disposables);
            var mzSpotFocus = new ChromSpotFocus(PlotModel.VerticalAxis, MzTol, Target.Select(t => t?.MassCenter ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<AlignmentSpotPropertyModel>(
                Target,
                id => Ms1Spots.Argmin(spot => Math.Abs(spot.MasterAlignmentID - id)),
                Target.Select(t => t?.MasterAlignmentID ?? 0d),
                "Region focus by ID",
                (mzSpotFocus, spot => spot.MassCenter),
                (rtSpotFocus, spot => spot.TimesCenter),
                (dtSpotFocus, spot => spot.TimesCenter)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, mzSpotFocus, rtSpotFocus, dtSpotFocus);
        }

        private static readonly double RtTol = 0.5;
        private static readonly double DtTol = 0.01;
        private static readonly double MzTol = 20;

        static LcimmsAlignmentModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        public ParameterBase Parameter { get; }
        public AlignmentFileBean AlignmentFile { get; }
        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }
        public string ResultFile { get; }
        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public ReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }
        public ReadOnlyReactivePropertySlim<MSDecResult> MsdecResult { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }

        public AlignmentPeakPlotModel PlotModel { get; }

        public MsSpectrumModel Ms2SpectrumModel { get; }

        public BarChartModel BarChartModel { get; }

        public AlignmentEicModel AlignmentEicModel { get; }

        public FocusNavigatorModel FocusNavigatorModel { get; }

        // public LcimmsAlignmentSpotTableModel AlignmentSpotTableModel { get; }

        public List<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public BrushMapData<AlignmentSpotPropertyModel> SelectedBrush {
            get => selectedBrush;
            set => SetProperty(ref selectedBrush, value);
        }
        private BrushMapData<AlignmentSpotPropertyModel> selectedBrush;

        public IBarItemsLoader BarItemsLoader {
            get => barItemsLoader;
            set => SetProperty(ref barItemsLoader, value);
        }
        private IBarItemsLoader barItemsLoader;

        public void SaveProject() {
            MessagePackHandler.SaveToFile(Container, ResultFile);
        }
    }
}
