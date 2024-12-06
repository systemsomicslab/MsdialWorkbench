using CompMs.App.Msdial.Common;
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
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Dims
{
    internal sealed class DimsAnalysisModel : AnalysisModelBase {
        private static readonly double MZ_TOLERANCE = 20;

        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly UndoManager _undoManager;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly IDataProvider _provider;
        private readonly ParameterBase _parameter;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;
        private readonly IMessageBroker _broker;

        public DimsAnalysisModel(
            AnalysisFileBeanModel analysisFileModel,
            IDataProvider provider,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databaseStorage,
            DataBaseMapper mapper,
            ParameterBase parameter,
            PeakFilterModel peakFilterModel,
            PeakSpotFiltering<ChromatogramPeakFeatureModel> peakFiltering,
            FilePropertiesModel projectBaseParameterModel,
            MsfinderSearcherFactory msfinderSearcherFactory,
            IMessageBroker broker)
            : base(analysisFileModel, parameter.MolecularSpectrumNetworkingBaseParam, peakFiltering, peakFilterModel, evaluator.Contramap((ChromatogramPeakFeatureModel peak) => peak.ScanMatchResult), broker) {
            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }

            if (peakFilterModel is null) {
                throw new ArgumentNullException(nameof(peakFilterModel));
            }

            _dataBaseMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _msfinderSearcherFactory = msfinderSearcherFactory;
            _broker = broker;
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databaseStorage, mapper);

            _undoManager = new UndoManager().AddTo(Disposables);

            var filterRegistrationManager = new FilterRegistrationManager<ChromatogramPeakFeatureModel>(Ms1Peaks, peakFiltering).AddTo(Disposables);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;
            filterRegistrationManager.AttachFilter(Ms1Peaks, peakFilterModel, evaluator.Contramap<ChromatogramPeakFeatureModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e)), status: ~(FilterEnableStatus.Rt | FilterEnableStatus.Dt));

            var brushSelector = BrushMapDataSelectorFactory.CreatePeakFeatureBrushes(parameter.TargetOmics);
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var vAxis = Observable.Return(new AxisRange(-0.5, 0.5))
                .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0.05))
                .AddTo(Disposables);
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.Mass, peak => peak.KMD, Target, labelSource, brushSelector.SelectedBrush, brushSelector.Brushes, PeakLinkModel.Build(Ms1Peaks, Ms1Peaks.Select(p => p.InnerModel.PeakCharacter).ToList()), verticalAxis: vAxis)
            {
                VerticalTitle = "Kendrick mass defect",
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.KMD),
                HorizontalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            Target.Select(t => $"File: {analysisFileModel.AnalysisFileName}" + (t is null ? string.Empty : $"Spot ID: {t.MasterPeakID} Scan: {t.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5}"))
                .Subscribe(title => PlotModel.GraphTitle = title);

            var eicLoader = DimsEicLoader.BuildForEicView(analysisFileModel.File, provider, parameter);
            EicModel = new EicModel(Target, eicLoader)
            {
                HorizontalTitle = "m/z",
                VerticalTitle = "Abundance"
            }.AddTo(Disposables);

            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel), mapper).AddTo(Disposables);

            PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
            PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);

            var spectraExporter = new NistSpectraExporter<ChromatogramPeakFeature?>(Target.Select(t => t?.InnerModel), mapper, parameter).AddTo(Disposables);
            var rawLoader = new MultiMsmsRawSpectrumLoader(provider, parameter).AddTo(Disposables);
            var rawGraphLabels = new GraphLabels("Raw spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem measuredSpectrumHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Blue);
            ObservableMsSpectrum rawObservableMsSpectrum = ObservableMsSpectrum.Create(Target, rawLoader, spectraExporter).AddTo(Disposables);
            SingleSpectrumModel rawSpectrumModel = new SingleSpectrumModel(rawObservableMsSpectrum, rawObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), rawObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredSpectrumHueItem, rawGraphLabels).AddTo(Disposables);

            var decLoader_ = new MsDecSpectrumFromFileLoader(analysisFileModel);
            var decGraphLabels = new GraphLabels("Deconvoluted spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ObservableMsSpectrum decObservableMsSpectrum = ObservableMsSpectrum.Create(Target, decLoader_, spectraExporter).AddTo(Disposables);
            SingleSpectrumModel decSpectrumModel = new SingleSpectrumModel(decObservableMsSpectrum, decObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), decObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredSpectrumHueItem, decGraphLabels).AddTo(Disposables);

            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference?>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference?>(mapper);
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

            EicLoader = DimsEicLoader.BuildForPeakTable(analysisFileModel.File, provider, parameter);
            PeakTableModel = new DimsAnalysisPeakTableModel(Ms1Peaks, Target, PeakSpotNavigatorModel, _undoManager).AddTo(Disposables);

            var mzSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, MZ_TOLERANCE, Target.Select(t => t?.Mass ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<ChromatogramPeakFeatureModel>(
                Target,
                id => Ms1Peaks.Argmin(p => Math.Abs(p.MasterPeakID - id)),
                Target.Select(t => t?.MasterPeakID ?? 0d),
                "ID",
                (mzSpotFocus, peak => peak.Mass)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, mzSpotFocus);

            var peakInformationModel = new PeakInformationAnalysisModel(Target).AddTo(Disposables);
            peakInformationModel.Add(t => new MzPoint(t?.InnerModel.ChromXs.Mz.Value ?? 0d, t?.Refer<MoleculeMsReference>(mapper)?.PrecursorMz));
            peakInformationModel.Add(
                t => new HeightAmount(t?.Intensity ?? 0d),
                t => new AreaAmount(t?.PeakArea ?? 0d));
            PeakInformationModel = peakInformationModel;
            var compoundDetailModel = new CompoundDetailModel(Target.SkipNull().SelectSwitch(t => t.ObserveProperty(p => p.ScanMatchResult)).Publish().RefCount(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;
            var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            MoleculeStructureModel = moleculeStructureModel;
            Target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.InnerModel)).AddTo(Disposables);
        }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }

        public UndoManager UndoManager => _undoManager;

        public AnalysisPeakPlotModel PlotModel { get; }

        public EicModel EicModel { get; }
        public RawDecSpectrumsModel Ms2SpectrumModel { get; }
        public Ms2ChromatogramsModel Ms2ChromatogramsModel { get; }
        public DimsAnalysisPeakTableModel PeakTableModel { get; }

        public EicLoader EicLoader { get; }
        public PeakInformationAnalysisModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }
        public FocusNavigatorModel FocusNavigatorModel { get; }
        public MsfinderParameterSetting MsfinderParameterSetting { get; }

        public CompoundSearchModel<PeakSpotModel>? BuildCompoundSearchModel() {
            if (Target.Value is not ChromatogramPeakFeatureModel peak || MsdecResult.Value is not MSDecResult msdecResult) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }
            PlotComparedMsSpectrumUsecase plotService = new PlotComparedMsSpectrumUsecase(msdecResult);
            var compoundSearchModel = new CompoundSearchModel<PeakSpotModel>(
                AnalysisFileModel,
                new PeakSpotModel(peak, msdecResult),
                new DimsCompoundSearchUsecase(_compoundSearchers.Items),
                plotService,
                new SetAnnotationUsecase(peak, peak.MatchResultsModel, _undoManager));
            compoundSearchModel.Disposables.Add(plotService);
            return compoundSearchModel;
        }

        public IObservable<bool> CanSetUnknown => Target.Select(t => t is not null);
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public bool CanSaveSpectra() => Target.Value?.InnerModel != null && MsdecResult.Value != null;
        public void SaveSpectra(Stream stream, ExportSpectraFileFormat format) {
            if (Target.Value is null || MsdecResult.Value is null) {
                return;
            }
            SpectraExport.SaveSpectraTable(
                format,
                stream,
                Target.Value.InnerModel,
                MsdecResult.Value,
                _provider.LoadMs1Spectrums(),
                _dataBaseMapper,
                _parameter);
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

        public override void SearchFragment() {
            FragmentSearcher.Search(Ms1Peaks.Select(n => n.InnerModel).ToList(), AnalysisFileModel.MSDecLoader, _parameter);
        }

        public override void InvokeMsfinder() {
            if (Target.Value is null || MsdecResult.Value is not MSDecResult result || result.Spectrum.IsEmptyOrNull()) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgram(
                AnalysisFileModel,
                Target.Value.InnerModel,
                result,
                _provider.LoadMs1Spectrums(),
                _dataBaseMapper,
                _parameter);
        }

        public void SaveSpectra(string filename) {
            var format = (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.'));
            using (var file = File.Open(filename, FileMode.Create)) {
                SaveSpectra(file, format);
            }
        }

        public void CopySpectrum() {
            var memory = new MemoryStream();
            SaveSpectra(memory, ExportSpectraFileFormat.msp);
            Clipboard.SetText(System.Text.Encoding.UTF8.GetString(memory.ToArray()));
        }

        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();
    }
}
