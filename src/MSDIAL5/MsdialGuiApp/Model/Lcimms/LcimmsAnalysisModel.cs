using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.DataStructure;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
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
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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
        private readonly AnalysisFileBeanModel _analysisFileModel;
        private readonly IDataProvider _spectrumProvider;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly MsdialLcImMsParameter _parameter;
        private readonly UndoManager _undoManager;
        private readonly MSDecLoader _decLoader;
        private readonly ReadOnlyReactivePropertySlim<MSDecResult> _msdecResult;

        public LcimmsAnalysisModel(
            AnalysisFileBeanModel analysisFileModel,
            IDataProvider spectrumProvider,
            IDataProvider accSpectrumProvider,
            DataBaseStorage databases,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseMapper mapper,
            MsdialLcImMsParameter parameter,
            PeakFilterModel peakFilterModel,
            PeakFilterModel accumulatedPeakFilterModel,
            IMessageBroker broker) {
            if (analysisFileModel is null) {
                throw new ArgumentNullException(nameof(analysisFileModel));
            }

            if (peakFilterModel is null) {
                throw new ArgumentNullException(nameof(peakFilterModel));
            }

            _analysisFileModel = analysisFileModel;
            _spectrumProvider = spectrumProvider;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _dataBaseMapper = mapper;
            _parameter = parameter;
            _undoManager = new UndoManager().AddTo(Disposables);

            var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(analysisFileModel.PeakAreaBeanInformationFilePath);
            _peakCollection = new ChromatogramPeakFeatureCollection(peaks);

            var orderedPeaks = peaks.OrderBy(peak => peak.ChromXs.RT.Value).Select(peak => new ChromatogramPeakFeatureModel(peak).AddTo(Disposables)).ToArray();
            var peakTree = new SegmentTree<IEnumerable<ChromatogramPeakFeatureModel>>(peaks.Count, Enumerable.Empty<ChromatogramPeakFeatureModel>(), Enumerable.Concat);
            using (peakTree.LazyUpdate()) {
                foreach (var (peak, index) in orderedPeaks.WithIndex()) {
                    peakTree[index] = peak.InnerModel.DriftChromFeatures.Select(dpeak => new ChromatogramPeakFeatureModel(dpeak).AddTo(Disposables)).ToArray();
                }
            }
            var driftPeaks = new ObservableCollection<ChromatogramPeakFeatureModel>(peakTree.Query(0, orderedPeaks.Length));
            var peakRanges = new Dictionary<ChromatogramPeakFeatureModel, (int, int)>();
            {
                var j = 0;
                var k = 0;
                foreach (var orderedPeak in orderedPeaks) {
                    while (j < orderedPeaks.Length && orderedPeaks[j].InnerModel.PeakFeature.ChromXsTop.RT.Value < orderedPeak.InnerModel.PeakFeature.ChromXsTop.RT.Value - parameter.AccumulatedRtRange / 2) {
                        j++;
                    }

                    while (k < orderedPeaks.Length && orderedPeaks[k].InnerModel.PeakFeature.ChromXsTop.RT.Value <= orderedPeak.InnerModel.PeakFeature.ChromXsTop.RT.Value + parameter.AccumulatedRtRange / 2) {
                        k++;
                    }
                    peakRanges[orderedPeak] = (j, k);
                }
            }
            var accumulatedTarget = new ReactivePropertySlim<ChromatogramPeakFeatureModel>().AddTo(Disposables);
            var target = accumulatedTarget.SkipNull()
                .Delay(TimeSpan.FromSeconds(.05d))
                .Select(t => {
                    var idx = orderedPeaks.IndexOf(t);
                    return peakTree.Query(idx, idx + 1).FirstOrDefault();
                })
                .ToReactiveProperty()
                .AddTo(Disposables);
            Target = target;

            var accumulatedPeakModels = new ObservableCollection<ChromatogramPeakFeatureModel>(orderedPeaks);
            var peakModels = new ReactiveCollection<ChromatogramPeakFeatureModel>(UIDispatcherScheduler.Default).AddTo(Disposables);
            //peakModels.AddRangeOnScheduler(peakTree.Query(0, orderedPeaks.Length));
            accumulatedTarget.SkipNull()
                .Select(t => {
                    var (lo, hi) = peakRanges[t];
                    return peakTree.Query(lo, hi);
                })
                .Subscribe(peaks_ => {
                    using (System.Windows.Data.CollectionViewSource.GetDefaultView(peakModels).DeferRefresh()) {
                        peakModels.ClearOnScheduler();
                        peakModels.AddRangeOnScheduler(peaks_);
                    }
                }).AddTo(Disposables);
            Ms1Peaks = peakModels;

            var peakSpotNavigator = new PeakSpotNavigatorModel(driftPeaks, peakFilterModel, evaluator, status: FilterEnableStatus.All).AddTo(Disposables);
            var accEvaluator = new AccumulatedPeakEvaluator(evaluator);
            peakSpotNavigator.AttachFilter(accumulatedPeakModels, accumulatedPeakFilterModel, status: FilterEnableStatus.None, evaluator: accEvaluator?.Contramap<IFilterable, ChromatogramPeakFeature>(filterable => ((ChromatogramPeakFeatureModel)filterable).InnerModel));
            peakSpotNavigator.AttachFilter(peakModels, peakFilterModel, status: FilterEnableStatus.All, evaluator: evaluator.Contramap<IFilterable, MsScanMatchResult>(filterable => filterable.MatchResults.Representative));
            PeakSpotNavigatorModel = peakSpotNavigator;

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
                    "Intensity");
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
            RtMzPlotModel = new AnalysisPeakPlotModel(accumulatedPeakModels, peak => peak.ChromXValue ?? 0, peak => peak.Mass, accumulatedTarget, labelSource, selectedBrush, brushes, new PeakLinkModel(accumulatedPeakModels))
            {
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            accumulatedTarget.Select(
                t => $"File: {analysisFileModel.AnalysisFileName}" +
                    (t is null
                        ? string.Empty
                        : $"Spot ID: {t.MasterPeakID} Mass m/z: {t.Mass:F5} RT: {t.InnerModel.PeakFeature.ChromXsTop.RT.Value:F2} min"))
                .Subscribe(title => RtMzPlotModel.GraphTitle = title)
                .AddTo(Disposables);
            var rtEicLoader = EicLoader.BuildForAllRange(analysisFileModel.File, accSpectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, parameter.RetentionTimeBegin, parameter.RetentionTimeEnd);
            RtEicLoader = EicLoader.BuildForPeakRange(analysisFileModel.File, accSpectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, parameter.RetentionTimeBegin, parameter.RetentionTimeEnd);
            RtEicModel = new EicModel(accumulatedTarget, rtEicLoader)
            {
                HorizontalTitle = RtMzPlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            }.AddTo(Disposables);

            DtMzPlotModel = new AnalysisPeakPlotModel(peakModels, peak => peak?.ChromXValue ?? 0d, peak => peak?.Mass ?? 0d, target, labelSource, selectedBrush, brushes, new PeakLinkModel(new ChromatogramPeakFeatureModel[0]), verticalAxis: RtMzPlotModel.VerticalAxis)
            {
                HorizontalTitle = "Mobility [1/K0]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            target.Select(
                t => t is null
                        ? string.Empty
                        : $"Spot ID: {t.MasterPeakID} Scan: {t.InnerModel.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:F5} Mobility [1/K0]: {t.InnerModel.PeakFeature.ChromXsTop.Drift.Value:F4}")
                .Subscribe(title => DtMzPlotModel.GraphTitle = title)
                .AddTo(Disposables);

            var dtEicLoader = new LcimmsEicLoader(spectrumProvider, parameter, new RawSpectra(spectrumProvider, parameter.IonMode, analysisFileModel.AcquisitionType));
            DtEicLoader = EicLoader.BuildForPeakRange(analysisFileModel.File, spectrumProvider, parameter, ChromXType.Drift, ChromXUnit.Msec, parameter.DriftTimeBegin, parameter.DriftTimeEnd);
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
            var spectraExporter = new NistSpectraExporter<ChromatogramPeakFeature>(target.Select(t => t?.InnerModel), mapper, parameter).AddTo(Disposables);
            var decLoader = new MSDecLoader(analysisFileModel.DeconvolutionFilePath).AddTo(Disposables);
            _decLoader = decLoader;
            var msdecResult = target.SkipNull()
                .Select(t => decLoader.LoadMSDecResult(t.MSDecResultIDUsedForAnnotation))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            _msdecResult = msdecResult;

            var searcherCollection = CompoundSearcherCollection.BuildSearchers(databases, mapper);
            var rawLoader = new MultiMsRawSpectrumLoader(spectrumProvider, parameter);
            var decSpecLoader = new MsDecSpectrumLoader(decLoader, Ms1Peaks);
            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel)).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference>(mapper);
            IConnectableObservable<List<SpectrumPeak>> refSpectrum = MatchResultCandidatesModel.LoadSpectrumObservable(refLoader).Publish();
            Disposables.Add(refSpectrum.Connect());
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.SelectedCandidate.Select(c => mapper.MoleculeMsRefer(c)));
            Ms2SpectrumModel = new RawDecSpectrumsModel(
                target,
                rawLoader,
                decSpecLoader,
                refSpectrum,
                new PropertySelector<SpectrumPeak, double>(peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity),
                new GraphLabels("Measure vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush),
                Observable.Return(spectraExporter),
                Observable.Return(spectraExporter),
                Observable.Return(referenceExporter),
                MatchResultCandidatesModel.GetCandidatesScorer(searcherCollection)).AddTo(Disposables);

            // Ms2 chromatogram
            Ms2ChromatogramsModel = new Ms2ChromatogramsModel(target, target.Select(t => decLoader.LoadMSDecResult(t.MSDecResultIDUsedForAnnotation)), rawLoader, spectrumProvider, parameter, analysisFileModel.AcquisitionType, broker).AddTo(Disposables);

            // Raw vs Purified spectrum model
            RawPurifiedSpectrumsModel = new RawPurifiedSpectrumsModel(
                target,
                rawLoader,
                decSpecLoader,
                peak => peak.Mass,
                peak => peak.Intensity,
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush)) {
                GraphTitle = "Raw vs. Purified spectrum",
                HorizontalTitle = "m/z",
                VerticalTitle = "Absolute abundance",
                HorizontalProperty = nameof(SpectrumPeak.Mass),
                VerticalProperty = nameof(SpectrumPeak.Intensity),
                LabelProperty = nameof(SpectrumPeak.Mass),
                OrderingProperty = nameof(SpectrumPeak.Intensity),
            }.AddTo(Disposables);

            var surveyScanSpectrum = new SurveyScanSpectrum(target, t => Observable.FromAsync(token => LoadMsSpectrumAsync(t, token)))
                .AddTo(Disposables);
            SurveyScanModel = new SurveyScanModel(
                surveyScanSpectrum,
                spec => spec.Mass,
                spec => spec.Intensity
            ).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            PeakTableModel = new LcimmsAnalysisPeakTableModel(new ReadOnlyObservableCollection<ChromatogramPeakFeatureModel>(driftPeaks), target, PeakSpotNavigatorModel).AddTo(Disposables);

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

            var mzSpotFocus = new ChromSpotFocus(DtMzPlotModel.VerticalAxis, MZ_TOLELANCE, target.Select(t => t?.Mass ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var rtSpotFocus = new ChromSpotFocus(RtMzPlotModel.HorizontalAxis, RT_TOLELANCE, accumulatedTarget.Select(t => t?.InnerModel.PeakFeature.ChromXsTop.RT.Value ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var dtSpotFocus = new ChromSpotFocus(DtMzPlotModel.HorizontalAxis, DT_TOLELANCE, target.Select(t => t?.InnerModel.PeakFeature.ChromXsTop.Drift.Value ?? 0d), "F4", "Mobility[1/K0]", isItalic: false).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<ChromatogramPeakFeatureModel>(
                target,
                id => Ms1Peaks.Argmin(p => Math.Abs(p.MasterPeakID - id)),
                target.Select(t => t?.MasterPeakID ?? 0d),
                "Region focus by ID",
                (mzSpotFocus, peak => peak.Mass),
                (rtSpotFocus, peak => peak.InnerModel.ChromXs.RT.Value),
                (dtSpotFocus, peak => peak.InnerModel.ChromXs.Drift.Value)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus, dtSpotFocus);

            var peakInformationModel = new PeakInformationAnalysisModel(target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new RtPoint(t?.InnerModel.ChromXs.RT.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RT.Value),
                t => new MzPoint(t?.Mass ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz),
                t => new DriftPoint(t?.InnerModel.ChromXs.Drift.Value ?? 0d),
                t => new CcsPoint(t?.CollisionCrossSection ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.CollisionCrossSection));
            peakInformationModel.Add(
                t => new HeightAmount(t?.Intensity ?? 0d),
                t => new AreaAmount(t?.PeakArea ?? 0d));
            PeakInformationModel = peakInformationModel;
            var compoundDetailModel = new CompoundDetailModel(target.SkipNull().SelectSwitch(t => t?.ObserveProperty(p => p.ScanMatchResult)).Publish().RefCount(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new RtSimilarity(r_?.RtSimilarity ?? 0d),
                r_ => new CcsSimilarity(r_?.CcsSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;
            var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            MoleculeStructureModel = moleculeStructureModel;
            target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.InnerModel)).AddTo(Disposables);

            CompoundSearchModel = target
                .CombineLatest(msdecResult, (t, r) => t is null || r is null ? null : new LcimmsCompoundSearchModel(analysisFileModel, t, r, searcherCollection.Items, _undoManager))
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

        public UndoManager UndoManager => _undoManager;
        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public IBrushMapper<ChromatogramPeakFeatureModel> Brush { get; }
        public AnalysisPeakPlotModel RtMzPlotModel { get; }
        public EicLoader RtEicLoader { get; }
        public EicModel RtEicModel { get; }
        public AnalysisPeakPlotModel DtMzPlotModel { get; }
        public EicLoader DtEicLoader { get; }
        public EicModel DtEicModel { get; }
        public RawDecSpectrumsModel Ms2SpectrumModel { get; }
        public Ms2ChromatogramsModel Ms2ChromatogramsModel { get; }
        public RawPurifiedSpectrumsModel RawPurifiedSpectrumsModel { get; }
        public SurveyScanModel SurveyScanModel { get; }
        public FocusNavigatorModel FocusNavigatorModel { get; }
        public PeakInformationAnalysisModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }

        public ReadOnlyReactivePropertySlim<LcimmsCompoundSearchModel> CompoundSearchModel { get; }
        public IObservable<bool> CanSearchCompound { get; }

        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }

        public EicLoader EicLoader { get; } // TODO

        private Task<List<SpectrumPeakWrapper>> LoadMsSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
            if (target is null || target.MS1RawSpectrumIdTop < 0) {
                return Task.FromResult(new List<SpectrumPeakWrapper>(0));
            }

            return Task.Run(async () =>
            {
                var msSpectra = await _spectrumProvider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var spectra = DataAccess.GetCentroidMassSpectra(msSpectra[target.MS1RawSpectrumIdTop], _parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                token.ThrowIfCancellationRequested();
                return spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
            }, token);
        }

        // IAnalysisModel
        public ObservableCollection<ChromatogramPeakFeatureModel> Ms1Peaks { get; }

        public IReactiveProperty<ChromatogramPeakFeatureModel> Target { get; }

        public LcimmsAnalysisPeakTableModel PeakTableModel { get; }

        public IObservable<bool> CanSetUnknown => Target.Select(t => !(t is null));
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public void SearchFragment() {
            FragmentSearcher.Search(Ms1Peaks.Select(n => n.InnerModel).ToList(), _decLoader, _parameter);
        }

        public void InvokeMsfinder() {
            if (Target.Value is null || (_msdecResult.Value?.Spectrum).IsEmptyOrNull()) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgram(
                _analysisFileModel,
                Target.Value.InnerModel,
                _msdecResult.Value,
                _spectrumProvider.LoadMs1Spectrums(),
                _dataBaseMapper,
                _parameter);
        }

        public Task SaveAsync(CancellationToken token) {
            return _peakCollection.SerializeAsync(_analysisFileModel.File, token);
        }

        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();
    }
}
