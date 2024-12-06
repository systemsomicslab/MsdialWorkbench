using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
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
using CompMs.Graphics.Base;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcms
{
    internal sealed class LcmsAnalysisModel : AnalysisModelBase {
        private readonly IDataProvider _provider;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly ParameterBase _parameter;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;
        private readonly IMessageBroker _broker;

        public LcmsAnalysisModel(
            AnalysisFileBeanModel analysisFileModel,
            IDataProvider provider,
            DataBaseStorage databases,
            DataBaseMapper mapper,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            ParameterBase parameter,
            PeakFilterModel peakFilterModel,
            PeakSpotFiltering<ChromatogramPeakFeatureModel> peakFiltering,
            FilePropertiesModel projectBaseParameterModel,
            MsfinderSearcherFactory msfinderSearcherFactory,
            IMessageBroker broker)
            : base(analysisFileModel, parameter.MolecularSpectrumNetworkingBaseParam, peakFiltering, peakFilterModel, evaluator.Contramap((ChromatogramPeakFeatureModel peak) => peak.ScanMatchResult), broker) {
            if (provider is null) {
                throw new ArgumentNullException(nameof(provider));
            }

            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }

            if (parameter is null) {
                throw new ArgumentNullException(nameof(parameter));
            }

            _provider = provider;
            DataBaseMapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _parameter = parameter;
            _msfinderSearcherFactory = msfinderSearcherFactory;
            _broker = broker;
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, DataBaseMapper);
            _undoManager = new UndoManager().AddTo(Disposables);
            CompoundSearcher = new LcmsCompoundSearchUsecase(_compoundSearchers.Items);

            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                // These 3 lines must be moved to somewhere for swithcing/updating the alignment result
                var proteinResultContainer = MsdialProteomicsSerializer.LoadProteinResultContainer(analysisFileModel.ProteinAssembledResultFilePath);
                var proteinResultContainerModel = new ProteinResultContainerModel(proteinResultContainer, Ms1Peaks, Target).AddTo(Disposables);
                ProteinResultContainerModel = proteinResultContainerModel;
            }

            var filterRegistrationManager = new FilterRegistrationManager<ChromatogramPeakFeatureModel>(Ms1Peaks, peakFiltering).AddTo(Disposables);
            filterRegistrationManager.AttachFilter(Ms1Peaks, peakFilterModel, evaluator.Contramap<ChromatogramPeakFeatureModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e)), status: ~FilterEnableStatus.Dt);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;

            // Peak scatter plot
            var brushSelector = BrushMapDataSelectorFactory.CreatePeakFeatureBrushes(parameter.TargetOmics);
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass, Target, labelSource, brushSelector.SelectedBrush, brushSelector.Brushes, PeakLinkModel.Build(Ms1Peaks, Ms1Peaks.Select(p => p.InnerModel.PeakCharacter).ToList()))
            {
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
                HorizontalProperty = nameof(ChromatogramPeakFeatureModel.ChromXValue),
                VerticalProperty = nameof(ChromatogramPeakFeatureModel.Mass),
            }.AddTo(Disposables);
            Target.Select(
                t =>  $"File: {analysisFileModel.AnalysisFileName}" +
                    (t is null
                        ? string.Empty
                        : $" Spot ID: {t.MasterPeakID} Scan: {t.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5}"))
                .Subscribe(title => PlotModel.GraphTitle = title)
                .AddTo(Disposables);
            var mrmprobsExporter = new EsiMrmprobsExporter(evaluator, mapper);
            var usecase = new ChromatogramPeakExportMrmprobsUsecase(parameter.MrmprobsExportBaseParam, Ms1Peaks, analysisFileModel, _compoundSearchers, mrmprobsExporter, Target);
            PlotModel.ExportMrmprobs = usecase;

            // Eic chart
            var eicLoader = EicLoader.BuildForAllRange(analysisFileModel.File, provider, parameter, ChromXType.RT, ChromXUnit.Min, parameter.RetentionTimeBegin, parameter.RetentionTimeEnd);
            EicLoader = EicLoader.BuildForPeakRange(analysisFileModel.File, provider, parameter, ChromXType.RT, ChromXUnit.Min, parameter.RetentionTimeBegin, parameter.RetentionTimeEnd);
            _rawSpectra = new RawSpectra(provider.LoadMsSpectrums(), parameter.IonMode, analysisFileModel.File.AcquisitionType);
            EicModel = new EicModel(Target, eicLoader) {
                HorizontalTitle = PlotModel.HorizontalTitle,
                VerticalTitle = "Abundance",
            }.AddTo(Disposables);

            ExperimentSpectrumModel = EicModel.Chromatogram
                .DefaultIfNull(chromatogram => new ChromatogramsModel("Experiment chromatogram", new ObservableCollection<DisplayChromatogram>(new[] { chromatogram.ConvertToDisplayChromatogram() }), string.Empty, string.Empty, string.Empty))
                .DisposePreviousValue()
                .DefaultIfNull(chromatogram => new RangeSelectableChromatogramModel(chromatogram))
                .DisposePreviousValue()
                .CombineLatest(
                    Target,
                    (model, t) => model is null || t is null ? null : new ExperimentSpectrumModel(model, AnalysisFileModel, provider, t.InnerModel, DataBaseMapper, parameter))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            var rawSpectrumLoader = new MultiMsmsRawSpectrumLoader(provider, parameter).AddTo(Disposables);
            _rawSpectrumLoader = rawSpectrumLoader;

            // Ms2 spectrum
            var spectraExporter = new NistSpectraExporter<ChromatogramPeakFeature?>(Target.Select(t => t?.InnerModel), mapper, parameter).AddTo(Disposables);
            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel), mapper).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference?>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference?>(mapper);
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Mass);
            PropertySelector<SpectrumPeak, double> verticalPropertySelector = new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity);

            var rawGraphLabels = new GraphLabels("Raw spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem measuredHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Blue);
            ObservableMsSpectrum rawObservableMsSpectrum = ObservableMsSpectrum.Create(Target, rawSpectrumLoader, spectraExporter).AddTo(Disposables);
            SingleSpectrumModel rawSpectrumModel = new SingleSpectrumModel(rawObservableMsSpectrum, rawObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), rawObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredHueItem, rawGraphLabels).AddTo(Disposables);

            var decLoader = new MsDecSpectrumFromFileLoader(analysisFileModel);
            var decGraphLabels = new GraphLabels("Deconvoluted spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ObservableMsSpectrum decObservableMsSpectrum = ObservableMsSpectrum.Create(Target, decLoader, spectraExporter).AddTo(Disposables);
            SingleSpectrumModel decSpectrumModel = new SingleSpectrumModel(decObservableMsSpectrum, decObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), decObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), measuredHueItem, decGraphLabels).AddTo(Disposables);

            var refGraphLabels = new GraphLabels("Reference spectrum", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem referenceSpectrumHueItem = new ChartHueItem(projectBaseParameterModel, Colors.Red);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.RetryRefer<MoleculeMsReference?>(mapper)).AddTo(Disposables);
            ObservableMsSpectrum refObservableMsSpectrum = ObservableMsSpectrum.Create(MatchResultCandidatesModel.SelectedCandidate.Select(rr => rr?.MatchResult), refLoader, referenceExporter).AddTo(Disposables);
            SingleSpectrumModel referenceSpectrumModel = new SingleSpectrumModel(refObservableMsSpectrum, refObservableMsSpectrum.CreateAxisPropertySelectors(horizontalPropertySelector, "m/z", "m/z"), refObservableMsSpectrum.CreateAxisPropertySelectors2(verticalPropertySelector, "abundance"), referenceSpectrumHueItem, refGraphLabels).AddTo(Disposables);

            var ms2ScanMatching = MatchResultCandidatesModel.GetCandidatesScorer(_compoundSearchers).Publish();
            Ms2SpectrumModel = new RawDecSpectrumsModel(rawSpectrumModel, decSpectrumModel, referenceSpectrumModel, ms2ScanMatching, rawSpectrumLoader).AddTo(Disposables);
            Disposables.Add(ms2ScanMatching.Connect());
            
            // Raw vs Purified spectrum model
            RawPurifiedSpectrumsModel = new RawPurifiedSpectrumsModel(Ms2SpectrumModel.RawRefSpectrumModels.UpperSpectrumModel, Ms2SpectrumModel.DecRefSpectrumModels.UpperSpectrumModel).AddTo(Disposables);

            // Ms2 chromatogram
            Ms2ChromatogramsModel = new Ms2ChromatogramsModel(Target, MsdecResult, rawSpectrumLoader, provider, parameter, analysisFileModel.AcquisitionType, broker).AddTo(Disposables);

            // SurveyScan
            var msdataType = parameter.MSDataType;
            var surveyScanSpectrum = new SurveyScanSpectrum(Target, t =>
            {
                if (t is null) {
                    return Observable.Return(new List<SpectrumPeakWrapper>());
                }
                return Observable.FromAsync(provider.LoadMsSpectrumsAsync)
                    .Select(spectrums =>
                        {
                            var spectra = DataAccess.GetCentroidMassSpectra(spectrums[t.MS1RawSpectrumIdTop], msdataType, 0, float.MinValue, float.MaxValue);
                            return spectra.Select(peak => new SpectrumPeakWrapper(peak)).ToList();
                        });
            }).AddTo(Disposables);
            SurveyScanModel = new SurveyScanModel(surveyScanSpectrum, spec => spec.Mass, spec => spec.Intensity).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            // Peak table
            PeakTableModel = new LcmsAnalysisPeakTableModel(Ms1Peaks, Target, PeakSpotNavigatorModel, parameter.ProjectParam.TargetOmics, _undoManager).AddTo(Disposables);

            var rtSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, RtTol, Target.Select(t => t?.ChromXValue ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var mzSpotFocus = new ChromSpotFocus(PlotModel.VerticalAxis, MzTol, Target.Select(t => t?.Mass ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<ChromatogramPeakFeatureModel>(
                Target,
                id => Ms1Peaks.Argmin(p => Math.Abs(p.MasterPeakID - id)),
                Target.Select(t => t?.MasterPeakID ?? 0d),
                "ID",
                (rtSpotFocus, peak => peak.ChromXValue ?? 0d),
                (mzSpotFocus, peak => peak.Mass)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus);

            CanSaveRawSpectra = Target.Select(t => t?.InnerModel != null)
                .ToReadOnlyReactivePropertySlim(initialValue: false)
                .AddTo(Disposables);

            var peakInformationModel = new PeakInformationAnalysisModel(Target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new RtPoint(t?.InnerModel.ChromXs.RT.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RT.Value),
                t => new MzPoint(t?.Mass ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz));
            peakInformationModel.Add(
                t => new HeightAmount(t?.Intensity ?? 0d),
                t => new AreaAmount(t?.PeakArea ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(Target.SkipNull().SelectSwitch(t => t.ObserveProperty(p => p.ScanMatchResult)).Publish().RefCount(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new RtSimilarity(r_?.RtSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;
            if (parameter.ProjectParam.TargetOmics == TargetOmics.Metabolomics || parameter.ProjectParam.TargetOmics == TargetOmics.Lipidomics) {
                var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
                MoleculeStructureModel = moleculeStructureModel;
                Target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.InnerModel)).AddTo(Disposables);
            }

            AccumulateSpectraUsecase = new AccumulateSpectraUsecase(provider, parameter.PeakPickBaseParam, parameter.ProjectParam.IonMode);
            analysisFile = analysisFileModel;

            MsfinderParameterSetting = MsfinderParameterSetting.CreateSetting(parameter.ProjectParam);
        }

        private static readonly double RtTol = 0.5;
        private static readonly double MzTol = 20;
        private readonly UndoManager _undoManager;
        public AnalysisFileBeanModel analysisFile { get; }
        public UndoManager UndoManager => _undoManager;

        public DataBaseMapper DataBaseMapper { get; }

        public EicLoader EicLoader { get; }

        public AnalysisPeakPlotModel PlotModel { get; }

        public EicModel EicModel { get; }
        public ReadOnlyReactivePropertySlim<ExperimentSpectrumModel?> ExperimentSpectrumModel { get; }

        private readonly IMsSpectrumLoader<ChromatogramPeakFeatureModel> _rawSpectrumLoader;
        private readonly RawSpectra _rawSpectra;

        public RawDecSpectrumsModel Ms2SpectrumModel { get; }
        public RawPurifiedSpectrumsModel RawPurifiedSpectrumsModel { get; }
        public Ms2ChromatogramsModel Ms2ChromatogramsModel { get; }
        public SurveyScanModel SurveyScanModel { get; }

        public LcmsAnalysisPeakTableModel PeakTableModel { get; }

        public FocusNavigatorModel FocusNavigatorModel { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }

        public AccumulateSpectraUsecase AccumulateSpectraUsecase { get; }

        public LcmsCompoundSearchUsecase CompoundSearcher { get; }

        public MsfinderParameterSetting MsfinderParameterSetting { get; }

        public LoadChromatogramsUsecase LoadChromatogramsUsecase() {
            var chromatogramRange = new ChromatogramRange(_parameter.PeakPickBaseParam.RetentionTimeBegin, _parameter.PeakPickBaseParam.RetentionTimeEnd, ChromXType.RT, ChromXUnit.Min);
            return new LoadChromatogramsUsecase(_rawSpectra, chromatogramRange, _parameter.PeakPickBaseParam, _parameter.ProjectParam.IonMode, Ms1Peaks);
        }

        public CompoundSearchModel<PeakSpotModel>? CreateCompoundSearchModel() {
            if (Target.Value?.InnerModel is null || MsdecResult.Value is null) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }

            PlotComparedMsSpectrumUsecase plotService = new PlotComparedMsSpectrumUsecase(MsdecResult.Value);
            var compoundSearch = new CompoundSearchModel<PeakSpotModel>(
                AnalysisFileModel,
                new PeakSpotModel(Target.Value, MsdecResult.Value),
                CompoundSearcher,
                plotService,
                new SetAnnotationUsecase(Target.Value, Target.Value.MatchResultsModel, _undoManager));
            compoundSearch.Disposables.Add(plotService);
            return compoundSearch;
        }

        public InternalMsFinderSingleSpot? CreateSingleSearchMsfinderModel() {
            if (Target.Value is not ChromatogramPeakFeatureModel peak || MsdecResult.Value is not { } msdec) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return null;
            }
            try {
                return _msfinderSearcherFactory.CreateModelForAnalysisPeak(MsfinderParameterSetting, peak, msdec, _provider, _undoManager);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return null;
            }
        }

        public IObservable<bool> CanSetUnknown => Target.Select(t => !(t is null));
        
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public override void SearchFragment() {
            FragmentSearcher.Search(Ms1Peaks.Select(n => n.InnerModel).ToList(), AnalysisFileModel.MSDecLoader, _parameter);
        }

        public void SaveSpectra(string filename) {
            if (Target.Value is not ChromatogramPeakFeatureModel peak || MsdecResult.Value is not { } msdec) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return;
            }
            using var file = File.Open(filename, FileMode.Create);
            SpectraExport.SaveSpectraTable(
                (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                file,
                peak.InnerModel,
                msdec,
                _provider.LoadMs1Spectrums(),
                DataBaseMapper,
                _parameter);
        }

        public bool CanSaveSpectra() => Target.Value?.InnerModel != null && MsdecResult.Value != null;

        public async Task SaveRawSpectra(string filename) {
            if (!(Target.Value is ChromatogramPeakFeatureModel target)) {
                return;
            }
            using (var file = File.Open(filename, FileMode.Create)) {
                var scan = await _rawSpectrumLoader.LoadScanAsObservable(target).FirstAsync();
                SpectraExport.SaveSpectraTable(
                    (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                    file,
                    target.InnerModel,
                    scan,
                    _provider.LoadMs1Spectrums(),
                    DataBaseMapper,
                    _parameter);
            }
        }

        public ReadOnlyReactivePropertySlim<bool> CanSaveRawSpectra { get; }
        public PeakInformationAnalysisModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel? MoleculeStructureModel { get; }
        public ProteinResultContainerModel? ProteinResultContainerModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }

        public override void InvokeMsfinder() {
            if (Target.Value is null || MsdecResult.Value is null || MsdecResult.Value.Spectrum.IsEmptyOrNull()) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgram(
                AnalysisFileModel,
                Target.Value.InnerModel,
                MsdecResult.Value,
                _provider.LoadMs1Spectrums(),
                DataBaseMapper,
                _parameter);
        }

        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();
    }
}
