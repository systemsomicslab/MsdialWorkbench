using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.MsResult;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Result;
using CompMs.Common.DataStructure;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Algorithm.Function;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using CompMs.App.Msdial.ViewModel.Service;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsAnalysisModel : DisposableModelBase, IAnalysisModel
    {
        private static readonly double RT_TOLELANCE = 0.5;
        private static readonly double MZ_TOLELANCE = 20;
        private static readonly double DT_TOLELANCE = 0.01;

        private readonly ChromatogramPeakFeatureCollection _peakCollection;
        private readonly ObservableCollection<ChromatogramPeakFeatureModel> _driftPeaks;
        private readonly AnalysisFileBeanModel _analysisFileModel;
        private readonly IDataProvider _spectrumProvider;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly MsdialLcImMsParameter _parameter;
        private readonly IMessageBroker _broker;
        private readonly UndoManager _undoManager;
        private readonly MSDecLoader _decLoader;
        private readonly ReadOnlyReactivePropertySlim<MSDecResult?> _msdecResult;
        private readonly ObservableCollection<ChromatogramPeakFeatureModel> _accumulatedPeakModels;
        private readonly RawSpectra _rawSpectra;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;

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
            FilePropertiesModel projectBaseParameterModel,
            MsfinderSearcherFactory msfinderSearcherFactory,
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
            _broker = broker;
            _undoManager = new UndoManager().AddTo(Disposables);
            _msfinderSearcherFactory = msfinderSearcherFactory;

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
            _driftPeaks = driftPeaks;
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
            var accumulatedTarget = new ReactivePropertySlim<ChromatogramPeakFeatureModel?>().AddTo(Disposables);
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
            _accumulatedPeakModels = accumulatedPeakModels;
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

            var filterEnabled = FilterEnableStatus.All & ~FilterEnableStatus.Protein;
            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                filterEnabled |= FilterEnableStatus.Protein;
            }
            var filterRegistrationManager = new FilterRegistrationManager<ChromatogramPeakFeatureModel>(driftPeaks, new PeakSpotFiltering<ChromatogramPeakFeatureModel>(filterEnabled)).AddTo(Disposables);
            IMatchResultEvaluator<ChromatogramPeakFeatureModel> driftEvaluator = evaluator.Contramap<ChromatogramPeakFeatureModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e));
            filterRegistrationManager.AttachFilter(driftPeaks, peakFilterModel, driftEvaluator, status: FilterEnableStatus.All);
            var accEvaluator = new AccumulatedPeakEvaluator(evaluator);
            filterRegistrationManager.AttachFilter(accumulatedPeakModels, accumulatedPeakFilterModel, evaluator: accEvaluator.Contramap<ChromatogramPeakFeatureModel, ChromatogramPeakFeature>(filterable => filterable.InnerModel), status: FilterEnableStatus.None);
            filterRegistrationManager.AttachFilter(peakModels, peakFilterModel, evaluator: driftEvaluator, status: FilterEnableStatus.All);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;

            var brushMapDataSelector = BrushMapDataSelectorFactory.CreatePeakFeatureBrushes(parameter.TargetOmics);
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            RtMzPlotModel = new AnalysisPeakPlotModel(accumulatedPeakModels, peak => peak.ChromXValue ?? 0, peak => peak.Mass, accumulatedTarget, labelSource, brushMapDataSelector.SelectedBrush, brushMapDataSelector.Brushes, PeakLinkModel.Build(accumulatedPeakModels, accumulatedPeakModels.Select(p => p.InnerModel.PeakCharacter).ToList()))
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

            _rawSpectra = new RawSpectra(accSpectrumProvider.LoadMsSpectrums(), parameter.IonMode, analysisFileModel.File.AcquisitionType);
            var rtEicLoader = EicLoader.BuildForAllRange(analysisFileModel.File, accSpectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, parameter.RetentionTimeBegin, parameter.RetentionTimeEnd);
            RtEicLoader = EicLoader.BuildForPeakRange(analysisFileModel.File, accSpectrumProvider, parameter, ChromXType.RT, ChromXUnit.Min, parameter.RetentionTimeBegin, parameter.RetentionTimeEnd);
            RtEicModel = new EicModel(accumulatedTarget, rtEicLoader)
            {
                HorizontalTitle = RtMzPlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            }.AddTo(Disposables);

            DtMzPlotModel = new AnalysisPeakPlotModel(peakModels, peak => peak?.ChromXValue ?? 0d, peak => peak?.Mass ?? 0d, target, labelSource, brushMapDataSelector.SelectedBrush, brushMapDataSelector.Brushes, PeakLinkModel.Build([], []), verticalAxis: RtMzPlotModel.VerticalAxis)
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

            var spectraExporter = new NistSpectraExporter<ChromatogramPeakFeature?>(target.Select(t => t?.InnerModel), mapper, parameter).AddTo(Disposables);
            var decLoader = analysisFileModel.MSDecLoader;
            _decLoader = decLoader;
            var msdecResult = target
                .DefaultIfNull(t => decLoader.LoadMSDecResult(t.MSDecResultIDUsedForAnnotation))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            _msdecResult = msdecResult;

            var compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper);
            CompoundSearcher = new LcimmsCompoundSearchUsecase(compoundSearchers.Items);
            var rawLoader = new MultiMsmsRawSpectrumLoader(spectrumProvider, parameter);
            var decSpecLoader = new MsDecSpectrumLoader(decLoader, Ms1Peaks);
            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel), mapper).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference?>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference?>(mapper);
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
            PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);

            var rawGraphLabels = new GraphLabels("Raw spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem measuredHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Blue);
            ObservableMsSpectrum rawObservableMsSpectrum = ObservableMsSpectrum.Create(Target, rawLoader, spectraExporter).AddTo(Disposables);
            SingleSpectrumModel rawSpectrumModel = new SingleSpectrumModel(rawObservableMsSpectrum, rawObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), rawObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredHueItem, rawGraphLabels).AddTo(Disposables);

            var decLoader_ = new MsDecSpectrumLoader(decLoader, Ms1Peaks);
            var decGraphLabels = new GraphLabels("Deconvoluted spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ObservableMsSpectrum decObservableMsSpectrum = ObservableMsSpectrum.Create(Target, decLoader_, spectraExporter).AddTo(Disposables);
            SingleSpectrumModel decSpectrumModel = new SingleSpectrumModel(decObservableMsSpectrum, decObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), decObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredHueItem, decGraphLabels).AddTo(Disposables);

            var refGraphLabels = new GraphLabels("Reference spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem referenceSpectrumHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Red);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.RetryRefer<MoleculeMsReference?>(mapper)).AddTo(Disposables);
            ObservableMsSpectrum refObservableMsSpectrum = ObservableMsSpectrum.Create(MatchResultCandidatesModel.SelectedCandidate.Select(rr => rr?.MatchResult), refLoader, referenceExporter).AddTo(Disposables);
            SingleSpectrumModel referenceSpectrumModel = new SingleSpectrumModel(refObservableMsSpectrum, refObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), refObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), referenceSpectrumHueItem, refGraphLabels).AddTo(Disposables);

            var ms2ScanMatching = MatchResultCandidatesModel.GetCandidatesScorer(compoundSearchers).Publish();
            Ms2SpectrumModel = new RawDecSpectrumsModel(rawSpectrumModel, decSpectrumModel, referenceSpectrumModel, ms2ScanMatching, rawLoader).AddTo(Disposables);
            Disposables.Add(ms2ScanMatching.Connect());

            // Ms2 chromatogram
            Ms2ChromatogramsModel = new Ms2ChromatogramsModel(target, target.DefaultIfNull(t => decLoader.LoadMSDecResult(t.MSDecResultIDUsedForAnnotation)), rawLoader, spectrumProvider, parameter, analysisFileModel.AcquisitionType, broker).AddTo(Disposables);

            // Raw vs Purified spectrum model
            RawPurifiedSpectrumsModel = new RawPurifiedSpectrumsModel(Ms2SpectrumModel.RawRefSpectrumModels.UpperSpectrumModel, Ms2SpectrumModel.DecRefSpectrumModels.UpperSpectrumModel).AddTo(Disposables);

            var surveyScanSpectrum = SurveyScanSpectrum.Create(target, t => Observable.FromAsync(token => LoadMsSpectrumAsync(t, token))).AddTo(Disposables);
            SurveyScanModel = new SurveyScanModel(surveyScanSpectrum, spec => spec.Mass, spec => spec.Intensity).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            PeakTableModel = new LcimmsAnalysisPeakTableModel(new ReadOnlyObservableCollection<ChromatogramPeakFeatureModel>(driftPeaks), target, PeakSpotNavigatorModel, _undoManager).AddTo(Disposables);

            var mzSpotFocus = new ChromSpotFocus(DtMzPlotModel.VerticalAxis, MZ_TOLELANCE, target.Select(t => t?.Mass ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var rtSpotFocus = new ChromSpotFocus(RtMzPlotModel.HorizontalAxis, RT_TOLELANCE, accumulatedTarget.Select(t => t?.InnerModel.PeakFeature.ChromXsTop.RT.Value ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var dtSpotFocus = new ChromSpotFocus(DtMzPlotModel.HorizontalAxis, DT_TOLELANCE, target.Select(t => t?.InnerModel.PeakFeature.ChromXsTop.Drift.Value ?? 0d), "F4", "Mobility[1/K0]", isItalic: false).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<ChromatogramPeakFeatureModel>(
                target,
                id => Ms1Peaks.Argmin(p => Math.Abs(p.MasterPeakID - id)),
                target.Select(t => t?.MasterPeakID ?? 0d),
                "ID",
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
            var compoundDetailModel = new CompoundDetailModel(target.SkipNull().SelectSwitch(t => t.ObserveProperty(p => p.ScanMatchResult)).Publish().RefCount(), mapper).AddTo(Disposables);
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
                .CombineLatest(msdecResult, (t, r) => t is null || r is null ? null : CreateCompoundearchModel(t, r))
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

            AccumulateSpectraUsecase = new AccumulateSpectraUsecase(spectrumProvider, parameter.PeakPickBaseParam, parameter.ProjectParam.IonMode);

            MsfinderParameterSetting = MsfinderParameterSetting.CreateSetting(parameter.ProjectParam);
        }

        public AnalysisFileBeanModel AnalysisFileModel => _analysisFileModel;
        public UndoManager UndoManager => _undoManager;
        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
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
        public MsfinderParameterSetting MsfinderParameterSetting { get; }

        public ReadOnlyReactivePropertySlim<CompoundSearchModel<PeakSpotModel>?> CompoundSearchModel { get; }
        public LcimmsCompoundSearchUsecase CompoundSearcher { get; }
        public IObservable<bool> CanSearchCompound { get; }

        private CompoundSearchModel<PeakSpotModel>? CreateCompoundearchModel(ChromatogramPeakFeatureModel peak, MSDecResult msdec) {
            if (peak is null || msdec is null) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }

            PlotComparedMsSpectrumUsecase plotService = new PlotComparedMsSpectrumUsecase(msdec);
            var compoundSearchModel = new CompoundSearchModel<PeakSpotModel>(
                _analysisFileModel,
                new PeakSpotModel(peak, msdec),
                CompoundSearcher,
                plotService,
                new SetAnnotationUsecase(peak, peak.MatchResultsModel, _undoManager));
            compoundSearchModel.Disposables.Add(plotService);
            return compoundSearchModel;
        }

        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }

        public AccumulateSpectraUsecase AccumulateSpectraUsecase { get; }

        public LoadChromatogramsUsecase LoadChromatogramsUsecase() {
            ChromatogramRange chromatogramRange = new ChromatogramRange(_parameter.PeakPickBaseParam.RetentionTimeBegin, _parameter.PeakPickBaseParam.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            return new LoadChromatogramsUsecase(_rawSpectra, chromatogramRange, _parameter.PeakPickBaseParam, _parameter.ProjectParam.IonMode, _accumulatedPeakModels);
        }

        private Task<List<SpectrumPeakWrapper>> LoadMsSpectrumAsync(ChromatogramPeakFeatureModel? target, CancellationToken token) {
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

        public IReactiveProperty<ChromatogramPeakFeatureModel?> Target { get; }

        public LcimmsAnalysisPeakTableModel PeakTableModel { get; }

        public IObservable<bool> CanSetUnknown => Target.Select(t => !(t is null));

        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public void SearchFragment() {
            FragmentSearcher.Search(Ms1Peaks.Select(n => n.InnerModel).ToList(), _decLoader, _parameter);
        }

        public InternalMsFinderSingleSpot? CreateSingleSearchMsfinderModel() {
            if (Target.Value is not ChromatogramPeakFeatureModel peak || _msdecResult.Value is not { } msdec) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return null;
            } try {
                return _msfinderSearcherFactory.CreateModelForAnalysisPeak(MsfinderParameterSetting, peak, msdec, _spectrumProvider, _undoManager);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public void InvokeMsfinder() {
            if (Target.Value is null || _msdecResult.Value is not MSDecResult result || result.Spectrum.IsEmptyOrNull()) {
                return;
            }
        }

        public Task SaveAsync(CancellationToken token) {
            return _peakCollection.SerializeAsync(_analysisFileModel.File, token);
        }

        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();

        void IResultModel.ExportMoleculerNetworkingData(MsdialCore.Parameter.MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
            network.ExportNodeEdgeFiles(parameter.ExportFolderPath);
        }

        void IResultModel.InvokeMoleculerNetworking(MsdialCore.Parameter.MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
            CytoscapejsModel.SendToCytoscapeJs(network);
        }

        public void InvokeMoleculerNetworkingForTargetSpot() {
            var network = GetMolecularNetworkingInstanceForTargetSpot(_parameter.MolecularSpectrumNetworkingBaseParam);
            if (network is null) {
                _broker.Publish(new ShortMessageRequest("Failed to calculate molecular network.\nPlease check selected peak spot."));
                return;
            }
            CytoscapejsModel.SendToCytoscapeJs(network);
        }

        private MolecularNetworkInstance GetMolecularNetworkInstance(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var publisher = new TaskProgressPublisher(_broker, $"Exporting MN results in {parameter.ExportFolderPath}");
            using (publisher.Start()) {
                IReadOnlyList<ChromatogramPeakFeatureModel> spots = Ms1Peaks;
                if (useCurrentFiltering) {
                    //spots = _filter.Filter(spots).ToList();
                }
                var loader = AnalysisFileModel.MSDecLoader;
                var peaks = loader.LoadMSDecResults();

                var flatten = spots.Select(n => n.InnerModel).SelectMany(s => s.IsMultiLayeredData() ? s.DriftChromFeatures : [s]).ToList();
                var flattenmodel = flatten.Select(n => new ChromatogramPeakFeatureModel(n)).ToList();
                var flattenpeaks = flatten.Select(n => peaks[n.MasterPeakID]).ToList();

                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Exporting MN results in {parameter.ExportFolderPath}");
                }

                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMolecularNetworkInstance(flatten, flattenpeaks, query, notify);
                var rootObj = network.Root;

                var ionfeature_edges = MolecularNetworking.GenerateFeatureLinkedEdges(flatten, flatten.ToDictionary(s => s.MasterPeakID, s => s.PeakCharacter));
                rootObj.edges.AddRange(ionfeature_edges);

                for (int i = 0; i < rootObj.nodes.Count; i++) {
                    var node = rootObj.nodes[i];
                    node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(flattenmodel[i], AnalysisFileModel.AnalysisFileName);
                }

                return network;
            }
        }

        private MolecularNetworkInstance? GetMolecularNetworkingInstanceForTargetSpot(MolecularSpectrumNetworkingBaseParameter parameter) {
            if (Target.Value is not ChromatogramPeakFeatureModel targetSpot) {
                return null;
            }
            if (parameter.MaxEdgeNumberPerNode == 0) {
                parameter.MinimumPeakMatch = 3;
                parameter.MaxEdgeNumberPerNode = 6;
                parameter.MaxPrecursorDifference = 400;
            }
            var publisher = new TaskProgressPublisher(_broker, $"Preparing MN results");
            using (publisher.Start()) {
                var spots = Ms1Peaks;
                var flatten = _driftPeaks;
                var peaks = AnalysisFileModel.MSDecLoader.LoadMSDecResults();

                var targetPeak = peaks[targetSpot.MasterPeakID];
                var flattenpeaks = flatten.Select(n => peaks[n.MasterPeakID]).ToList();
                var id2index = flatten.Select((spot, index) => new { spot.MasterPeakID, Index = index }).ToDictionary(item => item.MasterPeakID, item => item.Index);


                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Preparing MN results");
                }
                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMoleculerNetworkInstanceForTargetSpot(targetSpot, targetPeak, flatten, flattenpeaks, query, notify);
                var rootObj = network.Root;

                for (int i = 0; i < rootObj.nodes.Count; i++) {
                    var node = rootObj.nodes[i];
                    node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(flatten[node.data.id], AnalysisFileModel.AnalysisFileName);
                }

                return network;
            }
        }
    }
}
