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
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsAnalysisModel : AnalysisModelBase {
        private static readonly double MZ_TOLELANCE = 20;
        private static readonly double DT_TOLELANCE = 0.01;

        private readonly MsdialImmsParameter _parameter;
        private readonly IMessageBroker _broker;
        private readonly UndoManager _undoManager;
        private readonly IDataProvider _provider;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly RawSpectra _rawSpectra;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;

        public ImmsAnalysisModel(
            AnalysisFileBeanModel analysisFileModel,
            IDataProvider provider,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            DataBaseMapper mapper,
            MsdialImmsParameter parameter,
            PeakFilterModel peakFilterModel,
            PeakSpotFiltering<ChromatogramPeakFeatureModel> peakFiltering,
            FilePropertiesModel projectBaseParameterModel,
            MsfinderSearcherFactory msfinderSearcherFactory,
            IMessageBroker broker)
            : base(analysisFileModel, parameter.MolecularSpectrumNetworkingBaseParam, peakFiltering, peakFilterModel, evaluator.Contramap((ChromatogramPeakFeatureModel peak) => peak.ScanMatchResult), broker) {
            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }

            _provider = provider;
            _dataBaseMapper = mapper;
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper);
            _parameter = parameter;
            _broker = broker;
            _undoManager = new UndoManager().AddTo(Disposables);
            CompoundSearcher = new ImmsCompoundSearchUsecase(_compoundSearchers.Items);
            _msfinderSearcherFactory = msfinderSearcherFactory;

            var filterRegistrationManager = new FilterRegistrationManager<ChromatogramPeakFeatureModel>(Ms1Peaks, peakFiltering).AddTo(Disposables);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;
            filterRegistrationManager.AttachFilter(Ms1Peaks, peakFilterModel, evaluator.Contramap<ChromatogramPeakFeatureModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e)), status: ~FilterEnableStatus.Rt);

            var brushMapDataSelector = BrushMapDataSelectorFactory.CreatePeakFeatureBrushes(parameter.TargetOmics);
            var labelsource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass, Target, labelsource, brushMapDataSelector.SelectedBrush, brushMapDataSelector.Brushes, PeakLinkModel.Build(Ms1Peaks, Ms1Peaks.Select(p => p.InnerModel.PeakCharacter).ToList()))
            {
                HorizontalTitle = "Mobility [1/K0]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            Target.Select(
                t => $"File: {analysisFileModel.AnalysisFileName}" +
                    (t is null
                        ? string.Empty
                        : $"Spot ID: {t.InnerModel.MasterPeakID} Scan: {t.InnerModel.MS1RawSpectrumIdTop} Mass m/z: {t.InnerModel.PeakFeature.Mass:N5}"))
                .Subscribe(title => PlotModel.GraphTitle = title);

            var eicLoader = EicLoader.BuildForAllRange(analysisFileModel.File, provider, parameter, ChromXType.Drift, ChromXUnit.Msec, parameter.DriftTimeBegin, parameter.DriftTimeEnd);
            EicLoader = EicLoader.BuildForPeakRange(analysisFileModel.File, provider, parameter, ChromXType.Drift, ChromXUnit.Msec, parameter.DriftTimeBegin, parameter.DriftTimeEnd);
            _rawSpectra = new RawSpectra(provider, parameter.IonMode, analysisFileModel.File.AcquisitionType);
            EicModel = new EicModel(Target, eicLoader)
            {
                HorizontalTitle = PlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            }.AddTo(Disposables);

            var spectraExporter = new NistSpectraExporter<ChromatogramPeakFeature?>(Target.Select(t => t?.InnerModel), mapper, parameter).AddTo(Disposables);
            var rawLoader = new MultiMsmsRawSpectrumLoader(provider, parameter).AddTo(Disposables);
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

            var decLoader_ = new MsDecSpectrumFromFileLoader(analysisFileModel);
            var decGraphLabels = new GraphLabels("Deconvoluted spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ObservableMsSpectrum decObservableMsSpectrum = ObservableMsSpectrum.Create(Target, decLoader_, spectraExporter).AddTo(Disposables);
            SingleSpectrumModel decSpectrumModel = new SingleSpectrumModel(decObservableMsSpectrum, decObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), decObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredHueItem, decGraphLabels).AddTo(Disposables);

            var refGraphLabels = new GraphLabels("Reference spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem referenceSpectrumHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Red);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.RetryRefer<MoleculeMsReference?>(mapper)).AddTo(Disposables);
            ObservableMsSpectrum refObservableMsSpectrum = ObservableMsSpectrum.Create(MatchResultCandidatesModel.SelectedCandidate.Select(rr => rr?.MatchResult), refLoader, referenceExporter).AddTo(Disposables);
            SingleSpectrumModel referenceSpectrumModel = new SingleSpectrumModel(refObservableMsSpectrum, refObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), refObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), referenceSpectrumHueItem, refGraphLabels).AddTo(Disposables);

            var ms2ScanMatching = MatchResultCandidatesModel.GetCandidatesScorer(_compoundSearchers).Publish();
            Ms2SpectrumModel = new RawDecSpectrumsModel(rawSpectrumModel, decSpectrumModel, referenceSpectrumModel, ms2ScanMatching, rawLoader).AddTo(Disposables);
            Disposables.Add(ms2ScanMatching.Connect());

            // Ms2 chromatogram
            Ms2ChromatogramsModel = new Ms2ChromatogramsModel(Target, MsdecResult, rawLoader, provider, parameter, analysisFileModel.AcquisitionType, broker).AddTo(Disposables);

            var surveyScanSpectrum = SurveyScanSpectrum.Create(Target, target => Observable.FromAsync(token => LoadMsSpectrumAsync(target, token))).AddTo(Disposables);
            SurveyScanModel = new SurveyScanModel(surveyScanSpectrum, spec => spec.Mass, spec => spec.Intensity).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            PeakTableModel = new ImmsAnalysisPeakTableModel(Ms1Peaks, Target, PeakSpotNavigatorModel, _undoManager).AddTo(Disposables);

            var mzSpotFocus = new ChromSpotFocus(PlotModel.VerticalAxis, MZ_TOLELANCE, Target.Select(t => t?.Mass ?? 0d), "F5", "m/z", isItalic: true).AddTo(Disposables);
            var dtSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, DT_TOLELANCE, Target.Select(t => t?.ChromXValue ?? 0d), "F4", "Mobility[1/k0]", isItalic: false).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<ChromatogramPeakFeatureModel>(
                Target,
                id => Ms1Peaks.Argmin(p => Math.Abs(p.MasterPeakID - id)),
                Target.Select(t => t?.MasterPeakID ?? 0d),
                "ID",
                (dtSpotFocus, peak => peak.ChromXValue ?? 0d),
                (mzSpotFocus, peak => peak.Mass)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, dtSpotFocus, mzSpotFocus);

            var peakInformationModel = new PeakInformationAnalysisModel(Target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new MzPoint(t?.Mass ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz),
                t => new DriftPoint(t?.InnerModel.ChromXs.Drift.Value ?? 0d),
                t => new CcsPoint(t?.CollisionCrossSection ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.CollisionCrossSection));
            peakInformationModel.Add(
                t => new HeightAmount(t?.Intensity ?? 0d),
                t => new AreaAmount(t?.PeakArea ?? 0d));
            PeakInformationModel = peakInformationModel;
            var compoundDetailModel = new CompoundDetailModel(Target.SkipNull().SelectSwitch(t => t.ObserveProperty(p => p.ScanMatchResult)).Publish().RefCount(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new CcsSimilarity(r_?.CcsSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;
            var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            MoleculeStructureModel = moleculeStructureModel;
            Target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.InnerModel)).AddTo(Disposables);

            AccumulateSpectraUsecase = new AccumulateSpectraUsecase(provider, parameter.PeakPickBaseParam, parameter.ProjectParam.IonMode);

            MsfinderParameterSetting = MsfinderParameterSetting.CreateSetting(parameter.ProjectParam);
        }

        public UndoManager UndoManager => _undoManager;

        public AnalysisPeakPlotModel PlotModel { get; }

        public EicModel EicModel { get; }

        public RawDecSpectrumsModel Ms2SpectrumModel { get; }
        public Ms2ChromatogramsModel Ms2ChromatogramsModel { get; }
        public SurveyScanModel SurveyScanModel { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }

        public ImmsAnalysisPeakTableModel PeakTableModel { get; }

        public FocusNavigatorModel FocusNavigatorModel { get; }

        public PeakInformationAnalysisModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }

        public EicLoader EicLoader { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }

        public AccumulateSpectraUsecase AccumulateSpectraUsecase { get; }

        public ImmsCompoundSearchUsecase CompoundSearcher { get; }
        public MsfinderParameterSetting MsfinderParameterSetting { get; }

        public LoadChromatogramsUsecase LoadChromatogramsUsecase() {
            ChromatogramRange chromatogramRange = new ChromatogramRange(_parameter.DriftTimeBegin, _parameter.DriftTimeEnd, ChromXType.Drift, ChromXUnit.Msec);
            return new LoadChromatogramsUsecase(_rawSpectra, chromatogramRange, _parameter.PeakPickBaseParam, _parameter.ProjectParam.IonMode, Ms1Peaks);
        }

        private Task<List<SpectrumPeakWrapper>> LoadMsSpectrumAsync(ChromatogramPeakFeatureModel? target, CancellationToken token) {
            if (target is null || target.MS1RawSpectrumIdTop < 0) {
                return Task.FromResult(new List<SpectrumPeakWrapper>(0));
            }

            return Task.Run(async () =>
            {
                var msSpectra = await _provider.LoadMsSpectrumsAsync(token).ConfigureAwait(false);
                token.ThrowIfCancellationRequested();
                var spectra = DataAccess.GetCentroidMassSpectra(msSpectra[target.MS1RawSpectrumIdTop], _parameter.MSDataType, 0, float.MinValue, float.MaxValue);
                token.ThrowIfCancellationRequested();
                return spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
            }, token);
        }

        public CompoundSearchModel<PeakSpotModel>? CreateCompoundSearchModel() {
            if (Target.Value?.InnerModel is null || MsdecResult.Value is null) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }

            PlotComparedMsSpectrumUsecase plotService = new PlotComparedMsSpectrumUsecase(MsdecResult.Value);
            var compoundSearchModel = new CompoundSearchModel<PeakSpotModel>(
                AnalysisFileModel,
                new PeakSpotModel(Target.Value, MsdecResult.Value),
                new ImmsCompoundSearchUsecase(_compoundSearchers.Items),
                plotService,
                new SetAnnotationUsecase(Target.Value, Target.Value.MatchResultsModel, _undoManager));
            compoundSearchModel.Disposables.Add(plotService);
            return compoundSearchModel;
        }

        public InternalMsFinderSingleSpot? CreateSingleSearchMsfinderModel() {
            if (Target.Value is not ChromatogramPeakFeatureModel peak || MsdecResult.Value is not { } msdec) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return null;
            } try {
                return _msfinderSearcherFactory.CreateModelForAnalysisPeak(MsfinderParameterSetting, peak, msdec, _provider, _undoManager);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public IObservable<bool> CanSetUnknown => Target.Select(t => t is not null);
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public override void SearchFragment() {
            FragmentSearcher.Search(Ms1Peaks.Select(n => n.InnerModel).ToList(), AnalysisFileModel.MSDecLoader, _parameter);
        }

        public override void InvokeMsfinder() {
            if (Target.Value is null || MsdecResult.Value is null || MsdecResult.Value.Spectrum.IsEmptyOrNull()) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgram(
                AnalysisFileModel,
                Target.Value.InnerModel,
                MsdecResult.Value,
                _provider.LoadMs1Spectrums(),
                _dataBaseMapper,
                _parameter);
        }

        public void SaveSpectra(string filename) {
            if (Target.Value is null || MsdecResult.Value is null) {
                return;
            }
            using var file = File.Open(filename, FileMode.Create);
            SpectraExport.SaveSpectraTable(
                (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                file,
                Target.Value.InnerModel,
                MsdecResult.Value,
                _provider.LoadMs1Spectrums(),
                _dataBaseMapper,
                _parameter);
        }

        public bool CanSaveSpectra() => Target.Value?.InnerModel != null && MsdecResult.Value != null;

        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();
    }
}
