using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsAnalysisModel : DisposableModelBase, IAnalysisModel
    {
        private static readonly double RT_TOLELANCE = 0.5;
        private static readonly double MZ_TOLELANCE = 20;
        private static readonly double DT_TOLELANCE = 0.01;

        private readonly ChromatogramPeakFeatureCollection _peakCollection;
        private readonly AnalysisFileBean _analysisFile;
        private readonly IDataProvider _spectrumProvider;
        private readonly IDataProvider _accSpectrumProvider;
        private readonly MsdialLcImMsParameter _parameter;

        public LcimmsAnalysisModel(
            AnalysisFileBean analysisFile,
            IDataProvider spectrumProvider,
            IDataProvider accSpectrumProvider,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseMapper mapper,
            MsdialLcImMsParameter parameter,
            PeakFilterModel peakFilterModel,
            PeakFilterModel accumulatedPeakFilterModel) {
            if (peakFilterModel is null) {
                throw new ArgumentNullException(nameof(peakFilterModel));
            }
            _analysisFile = analysisFile ?? throw new ArgumentNullException(nameof(analysisFile));
            _spectrumProvider = spectrumProvider;
            _accSpectrumProvider = accSpectrumProvider;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _parameter = parameter;

            var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            _peakCollection = new ChromatogramPeakFeatureCollection(peaks);

            var accumulatedPeakModels = new ObservableCollection<ChromatogramPeakFeatureModel>(peaks.Select(peak => new ChromatogramPeakFeatureModel(peak)));
            var peakModels = new ObservableCollection<ChromatogramPeakFeatureModel>(peaks.SelectMany(peak => peak.DriftChromFeatures, (_, peak) => new ChromatogramPeakFeatureModel(peak)));
            Ms1Peaks = peakModels;

            var accumulatedPeakSpotNavigator = new PeakSpotNavigatorModel(accumulatedPeakModels, accumulatedPeakFilterModel, evaluator, useRtFilter: true, useDtFilter: false);
            var peakSpotNavigator = new PeakSpotNavigatorModel(peakModels, accumulatedPeakFilterModel, evaluator, useRtFilter: true, useDtFilter: true);
            PeakSpotNavigatorModel = peakSpotNavigator;

            var target = new ReactivePropertySlim<ChromatogramPeakFeatureModel>().AddTo(Disposables);
            var accumulatedTarget = new ReactivePropertySlim<ChromatogramPeakFeatureModel>().AddTo(Disposables);
            Target = target;

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
            RtMzPlotModel = new AnalysisPeakPlotModel(accumulatedPeakModels, peak => peak.ChromXValue ?? 0, peak => peak.Mass, accumulatedTarget, labelSource, selectedBrush, brushes)
            {
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            var rtEicLoader = EicLoader.BuildForAllRange(accSpectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, parameter.RetentionTimeBegin, parameter.RetentionTimeEnd);
            RtEicLoader = EicLoader.BuildForPeakRange(accSpectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, parameter.RetentionTimeBegin, parameter.RetentionTimeEnd);
            RtEicModel = new EicModel(accumulatedTarget, rtEicLoader)
            {
                HorizontalTitle = RtMzPlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            }.AddTo(Disposables);

            DtMzPlotModel = new AnalysisPeakPlotModel(peakModels, peak => peak.ChromXValue ?? 0, peak => peak.Mass, target, labelSource, selectedBrush, brushes, verticalAxis: RtMzPlotModel.VerticalAxis)
            {
                HorizontalTitle = "Drift time [1/k0]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            target.Select(
                t => $"File: {analysisFile.AnalysisFileName}" +
                    (t is null
                        ? string.Empty
                        : $"Spot ID: {t.MasterPeakID} Scan: {t.InnerModel.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5} RT min: {t.InnerModel.ChromXsTop.RT.Value} Drift time msec: {t.InnerModel.ChromXsTop.Drift.Value}"))
                .Subscribe(title => DtMzPlotModel.GraphTitle = title)
                .AddTo(Disposables);

            var dtEicLoader = EicLoader.BuildForAllRange(spectrumProvider, parameter, ChromXType.Drift, ChromXUnit.Msec, parameter.DriftTimeBegin, parameter.DriftTimeEnd);
            DtEicLoader = EicLoader.BuildForPeakRange(spectrumProvider, parameter, ChromXType.Drift, ChromXUnit.Msec, parameter.DriftTimeBegin, parameter.DriftTimeEnd);
            DtEicModel = new EicModel(target, dtEicLoader)
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
            var spectraExporter = new NistSpectraExporter(target.Select(t => t?.InnerModel), mapper, parameter).AddTo(Disposables);
            target = new ReactivePropertySlim<ChromatogramPeakFeatureModel>().AddTo(Disposables);
            var decLoader = new MSDecLoader(analysisFile.DeconvolutionFilePath).AddTo(Disposables);
            var msdecResult = target.Where(t => !(t is null))
                .Select(t => decLoader.LoadMSDecResult(t.MSDecResultIDUsedForAnnotation))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            Ms2SpectrumModel = new RawDecSpectrumsModel(
                target,
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

            var surveyScanSpectrum = new SurveyScanSpectrum(target, t => Observable.FromAsync(token => LoadMs1SpectrumAsync(t, token)))
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

            var mzSpotFocus = new ChromSpotFocus(RtMzPlotModel.VerticalAxis, MZ_TOLELANCE, target.Select(t => t?.Mass ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var rtSpotFocus = new ChromSpotFocus(RtMzPlotModel.HorizontalAxis, RT_TOLELANCE, accumulatedTarget.Select(t => t?.ChromXValue ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var dtSpotFocus = new ChromSpotFocus(DtMzPlotModel.HorizontalAxis, DT_TOLELANCE, target.Select(t => t?.ChromXValue ?? 0d), "F3", "Drift time(1/k0)", isItalic: false).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<ChromatogramPeakFeatureModel>(
                target,
                id => Ms1Peaks.Argmin(p => Math.Abs(p.MasterPeakID - id)),
                target.Select(t => t?.MasterPeakID ?? 0d),
                "Region focus by ID",
                (mzSpotFocus, peak => peak.Mass),
                // (rtSpotFocus, peak => peak.ChromXValue ?? 0d),
                (dtSpotFocus, peak => peak.ChromXValue ?? 0d)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus, dtSpotFocus);

            CompoundSearchModel = target.Where(t => t != null)
                .CombineLatest(msdecResult.Where(r => r != null), (t, r) => new CompoundSearchModel<ChromatogramPeakFeature>(analysisFile, t.InnerModel, r, new CompoundSearcher[0]))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            CanSearchCompound = new[]
            {
                target.Select(t => t?.InnerModel != null),
                msdecResult.Select(d => d != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
        }

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

        public IObservable<CompoundSearchModel<ChromatogramPeakFeature>> CompoundSearchModel { get; }
        public IObservable<bool> CanSearchCompound { get; }


        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }

        public EicLoader EicLoader { get; } // TODO

        private Task<List<SpectrumPeakWrapper>> LoadMs1SpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target is null || target.MS1RawSpectrumIdTop < 0) {
                return Task.FromResult(new List<SpectrumPeakWrapper>(0));
            }

            return Task.Run(async () =>
            {
                var ms1Spectra = await _spectrumProvider.LoadMs1SpectrumsAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var spectra = DataAccess.GetCentroidMassSpectra(ms1Spectra[target.MS1RawSpectrumIdTop], _parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                token.ThrowIfCancellationRequested();
                return spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
            }, token);
        }

        // IAnalysisModel
        public ObservableCollection<ChromatogramPeakFeatureModel> Ms1Peaks { get; }

        public ReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

        public string DisplayLabel {
            get => _displayLabel;
            set => SetProperty(ref _displayLabel, value);
        }
        private string _displayLabel = string.Empty;

        public Task SaveAsync(CancellationToken token) {
            return _peakCollection.SerializeAsync(_analysisFile.PeakAreaBeanInformationFilePath, token);
        }
    }
}
