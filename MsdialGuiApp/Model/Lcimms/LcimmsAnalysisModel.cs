using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcimms
{
    class LcimmsAnalysisModel : AnalysisModelBase
    {
        public LcimmsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider spectrumProvider,
            IDataProvider accSpectrumProvider,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseMapper mapper,
            ParameterBase parameter,
            PeakFilterModel peakFilterModel)
            : base(analysisFile) {
            if (peakFilterModel is null) {
                throw new ArgumentNullException(nameof(peakFilterModel));
            }

            this.spectrumProvider = spectrumProvider;
            this.accSpectrumProvider = accSpectrumProvider;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            Parameter = parameter;

            FileName = analysisFile.AnalysisFileName;

            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(Ms1Peaks, peakFilterModel, evaluator, useRtFilter: true, useDtFilter: true);

            var ontologyBrush = new BrushMapData<ChromatogramPeakFeatureModel>(
                    new KeyBrushMapper<ChromatogramPeakFeatureModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        peak => peak?.Ontology ?? string.Empty,
                        Color.FromArgb(180, 181, 181, 181)),
                    "Ontology");
            var intensityBrush = new BrushMapData<ChromatogramPeakFeatureModel>(
                    new DelegateBrushMapper<ChromatogramPeakFeatureModel>(
                        peak => Color.FromArgb(
                            180,
                            (byte)(255 * peak.InnerModel.PeakShape.AmplitudeScoreValue),
                            (byte)(255 * (1 - Math.Abs(peak.InnerModel.PeakShape.AmplitudeScoreValue - 0.5))),
                            (byte)(255 - 255 * peak.InnerModel.PeakShape.AmplitudeScoreValue)),
                        enableCache: true),
                    "Ontology");
            var brushes = new[] { intensityBrush, ontologyBrush, };
            BrushMapData<ChromatogramPeakFeatureModel> selectedBrush;
            switch (parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    selectedBrush = ontologyBrush;
                    break;
                case TargetOmics.Metabolomics:
                case TargetOmics.Proteomics:
                default:
                    selectedBrush = intensityBrush;
                    break;
            }
            Brush = selectedBrush.Mapper;
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            RtMzPlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass, Target, labelSource, selectedBrush, brushes)
            {
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            var rtEicLoader = EicLoader.BuildForAllRange(accSpectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, Parameter.RetentionTimeBegin, Parameter.RetentionTimeEnd);
            RtEicLoader = EicLoader.BuildForPeakRange(accSpectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, Parameter.RetentionTimeBegin, Parameter.RetentionTimeEnd);
            RtEicModel = new EicModel(Target, rtEicLoader)
            {
                HorizontalTitle = RtMzPlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            }.AddTo(Disposables);

            DtMzPlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass, Target, labelSource, selectedBrush, brushes, verticalAxis: RtMzPlotModel.VerticalAxis)
            {
                HorizontalTitle = "Drift time [1/k0]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            Target.Select(
                t => $"File: {analysisFile.AnalysisFileName}" +
                    (t is null
                        ? string.Empty
                        : $"Spot ID: {t.MasterPeakID} Scan: {t.InnerModel.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5} RT min: {t.InnerModel.ChromXsTop.RT.Value} Drift time msec: {t.InnerModel.ChromXsTop.Drift.Value}"))
                .Subscribe(title => DtMzPlotModel.GraphTitle = title);

            var dtEicLoader = EicLoader.BuildForAllRange(spectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, Parameter.RetentionTimeBegin, Parameter.RetentionTimeEnd);
            DtEicLoader = EicLoader.BuildForPeakRange(spectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, Parameter.RetentionTimeBegin, Parameter.RetentionTimeEnd);
            DtEicModel = new EicModel(Target, dtEicLoader)
            {
                HorizontalTitle = DtMzPlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            }.AddTo(Disposables);

            var upperSpecBrush = new KeyBrushMapper<SpectrumComment, string>(
               parameter.ProjectParam.SpectrumCommentToColorBytes
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
                    var projectParameter = parameter.ProjectParam;
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
            var spectraExporter = new NistSpectraExporter(Target.Select(t => t?.InnerModel), mapper, parameter).AddTo(Disposables);
            Ms2SpectrumModel = new RawDecSpectrumsModel(
                Target,
                new MsRawSpectrumLoader(spectrumProvider, parameter),
                new MsDecSpectrumLoader(decLoader, Ms1Peaks),
                new MsRefSpectrumLoader(mapper),
                new PropertySelector<SpectrumPeak, float>(peak => peak.Mass),
                new PropertySelector<SpectrumPeak, float>(peak => peak.Intensity),
                new GraphLabels("Measure vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush),
                Observable.Return(spectraExporter),
                Observable.Return(spectraExporter),
                Observable.Return((ISpectraExporter)null)).AddTo(Disposables);

            var surveyScanSpectrum = new SurveyScanSpectrum(Target, target => Observable.FromAsync(token => LoadMs1SpectrumAsync(target, token)))
                .AddTo(Disposables);
            SurveyScanModel = new SurveyScanModel(
                surveyScanSpectrum,
                spec => spec.Mass,
                spec => spec.Intensity
            ).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            // PeakTableModel = new ImmsAnalysisPeakTableModel(Ms1Peaks, Target, MassMin, MassMax, ChromMin, ChromMax);

            switch (parameter.TargetOmics) {
                case TargetOmics.Lipidomics:
                    Brush = new KeyBrushMapper<ChromatogramPeakFeatureModel, string>(
                        ChemOntologyColor.Ontology2RgbaBrush,
                        peak => peak?.Ontology ?? string.Empty,
                        Color.FromArgb(180, 181, 181, 181));
                    break;
                case TargetOmics.Metabolomics:
                    Brush = new DelegateBrushMapper<ChromatogramPeakFeatureModel>(
                        peak => Color.FromArgb(
                            180,
                            (byte)(255 * peak.InnerModel.PeakShape.AmplitudeScoreValue),
                            (byte)(255 * (1 - Math.Abs(peak.InnerModel.PeakShape.AmplitudeScoreValue - 0.5))),
                            (byte)(255 - 255 * peak.InnerModel.PeakShape.AmplitudeScoreValue)),
                        enableCache: true);
                    break;
                default:
                    Brush = new ConstantBrushMapper<ChromatogramPeakFeatureModel>(Brushes.Black);
                    break;
            }

            var mzSpotFocus = new ChromSpotFocus(RtMzPlotModel.VerticalAxis, MzTol, Target.Select(t => t?.Mass ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var rtSpotFocus = new ChromSpotFocus(RtMzPlotModel.HorizontalAxis, RtTol, Target.Select(t => t?.ChromXValue ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var dtSpotFocus = new ChromSpotFocus(DtMzPlotModel.HorizontalAxis, DtTol, Target.Select(t => t?.ChromXValue ?? 0d), "F3", "Drift time(1/k0)", isItalic: false).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<ChromatogramPeakFeatureModel>(
                Target,
                id => Ms1Peaks.Argmin(p => Math.Abs(p.MasterPeakID - id)),
                Target.Select(t => t?.MasterPeakID ?? 0d),
                "Region focus by ID",
                (mzSpotFocus, peak => peak.Mass),
                (rtSpotFocus, peak => peak.ChromXValue ?? 0d),
                (dtSpotFocus, peak => peak.ChromXValue ?? 0d)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus, dtSpotFocus);
        }

        private static readonly double RtTol = 0.5;
        private static readonly double MzTol = 20;
        private static readonly double DtTol = 0.01;

        public ParameterBase Parameter { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }

        public IBrushMapper<ChromatogramPeakFeatureModel> Brush { get; }
        public AnalysisPeakPlotModel RtMzPlotModel { get; }
        public EicLoader RtEicLoader { get; }
        public EicModel RtEicModel { get; }
        public AnalysisPeakPlotModel DtMzPlotModel { get; }
        public EicLoader DtEicLoader { get; }
        public EicModel DtEicModel { get; }
        public RawDecSpectrumsModel Ms2SpectrumModel { get; }
        public SurveyScanModel SurveyScanModel { get; }
        public FocusNavigatorModel FocusNavigatorModel { get; }

        private readonly IDataProvider spectrumProvider;
        private readonly IDataProvider accSpectrumProvider;

        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }

        public double Ms1Tolerance => Parameter.CentroidMs1Tolerance;

        public string FileName {
            get => fileName;
            set => SetProperty(ref fileName, value);
        }
        private string fileName;

        public EicLoader EicLoader { get; } // TODO

        private Task<List<SpectrumPeakWrapper>> LoadMs1SpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target is null || target.MS1RawSpectrumIdTop < 0) {
                return Task.FromResult(new List<SpectrumPeakWrapper>(0));
            }

            return Task.Run(async () =>
            {
                var ms1Spectra = await spectrumProvider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var spectra = DataAccess.GetCentroidMassSpectra(ms1Spectra[target.MS1RawSpectrumIdTop], Parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                token.ThrowIfCancellationRequested();
                return spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
            }, token);
        }
    }
}
