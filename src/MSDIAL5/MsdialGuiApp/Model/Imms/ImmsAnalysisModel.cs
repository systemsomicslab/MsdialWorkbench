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
using CompMs.CommonMVVM.ChemView;
using CompMs.Graphics.Design;
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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsAnalysisModel : AnalysisModelBase {
        private static readonly double MZ_TOLELANCE = 20;
        private static readonly double DT_TOLELANCE = 0.01;

        private readonly MsdialImmsParameter _parameter;
        private readonly UndoManager _undoManager;
        private readonly IDataProvider _provider;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly CompoundSearcherCollection _compoundSearchers;

        public ImmsAnalysisModel(
            AnalysisFileBeanModel analysisFileModel,
            IDataProvider provider,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            DataBaseMapper mapper,
            MsdialImmsParameter parameter,
            PeakFilterModel peakFilterModel,
            IMessageBroker broker)
            : base(analysisFileModel) {
            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }

            _provider = provider;
            _dataBaseMapper = mapper;
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper);
            _parameter = parameter;
            _undoManager = new UndoManager().AddTo(Disposables);

            PeakSpotNavigatorModel = new PeakSpotNavigatorModel(Ms1Peaks, peakFilterModel, evaluator, status: ~FilterEnableStatus.Rt).AddTo(Disposables);

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
            var labelsource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            PlotModel = new AnalysisPeakPlotModel(Ms1Peaks, peak => peak.ChromXValue ?? 0, peak => peak.Mass, Target, labelsource, selectedBrush, brushes, new PeakLinkModel(Ms1Peaks))
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
            EicModel = new EicModel(Target, eicLoader)
            {
                HorizontalTitle = PlotModel.HorizontalTitle,
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
            var spectraExporter = new NistSpectraExporter<ChromatogramPeakFeature>(Target.Select(t => t?.InnerModel), mapper, parameter).AddTo(Disposables);
            var rawLoader = new MultiMsRawSpectrumLoader(provider, parameter).AddTo(Disposables);
            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel)).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference>(mapper);
            IConnectableObservable<List<SpectrumPeak>> refSpectrum = MatchResultCandidatesModel.LoadSpectrumObservable(refLoader).Publish();
            Disposables.Add(refSpectrum.Connect());
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.SelectedCandidate.Select(c => mapper.MoleculeMsRefer(c)));
            Ms2SpectrumModel = new RawDecSpectrumsModel(
                Target,
                rawLoader,
                new MsDecSpectrumLoader(decLoader, Ms1Peaks),
                refSpectrum,
                new PropertySelector<SpectrumPeak, double>(peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(peak => peak.Intensity),
                new GraphLabels("Measure vs. Reference", "m/z", "Relative aubndance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                nameof(SpectrumPeak.SpectrumComment),
                Observable.Return(upperSpecBrush),
                Observable.Return(lowerSpecBrush),
                Observable.Return(spectraExporter),
                Observable.Return(spectraExporter),
                Observable.Return(referenceExporter),
                MatchResultCandidatesModel.GetCandidatesScorer(_compoundSearchers)).AddTo(Disposables);

            // Ms2 chromatogram
            Ms2ChromatogramsModel = new Ms2ChromatogramsModel(Target, MsdecResult, rawLoader, provider, parameter, analysisFileModel.AcquisitionType, broker).AddTo(Disposables);

            var surveyScanSpectrum = new SurveyScanSpectrum(Target, target => Observable.FromAsync(token => LoadMsSpectrumAsync(target, token)))
                .AddTo(Disposables);
            SurveyScanModel = new SurveyScanModel(
                surveyScanSpectrum,
                spec => spec.Mass,
                spec => spec.Intensity
            ).AddTo(Disposables);
            SurveyScanModel.Elements.VerticalTitle = "Abundance";
            SurveyScanModel.Elements.HorizontalProperty = nameof(SpectrumPeakWrapper.Mass);
            SurveyScanModel.Elements.VerticalProperty = nameof(SpectrumPeakWrapper.Intensity);

            PeakTableModel = new ImmsAnalysisPeakTableModel(new ReadOnlyObservableCollection<ChromatogramPeakFeatureModel>(Ms1Peaks), Target, PeakSpotNavigatorModel).AddTo(Disposables);

            var mzSpotFocus = new ChromSpotFocus(PlotModel.VerticalAxis, MZ_TOLELANCE, Target.Select(t => t?.Mass ?? 0d), "F5", "m/z", isItalic: true).AddTo(Disposables);
            var dtSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, DT_TOLELANCE, Target.Select(t => t?.ChromXValue ?? 0d), "F4", "Mobility[1/k0]", isItalic: false).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<ChromatogramPeakFeatureModel>(
                Target,
                id => Ms1Peaks.Argmin(p => Math.Abs(p.MasterPeakID - id)),
                Target.Select(t => t?.MasterPeakID ?? 0d),
                "Region focus by ID",
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

        private Task<List<SpectrumPeakWrapper>> LoadMsSpectrumAsync(ChromatogramPeakFeatureModel target, CancellationToken token) {
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

        public ImmsCompoundSearchModel CreateCompoundSearchModel() {
            if (Target.Value?.InnerModel is null || MsdecResult.Value is null) {
                return null;
            }

            return new ImmsCompoundSearchModel(
                AnalysisFileModel,
                Target.Value,
                MsdecResult.Value,
                _compoundSearchers.Items,
                _undoManager);
        }

        public IObservable<bool> CanSetUnknown => Target.Select(t => !(t is null));
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public override void SearchFragment() {
            FragmentSearcher.Search(Ms1Peaks.Select(n => n.InnerModel).ToList(), decLoader, _parameter);
        }

        public override void InvokeMsfinder() {
            if (Target.Value is null || (MsdecResult.Value?.Spectrum).IsEmptyOrNull()) {
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
            using (var file = File.Open(filename, FileMode.Create)) {
                SpectraExport.SaveSpectraTable(
                    (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                    file,
                    Target.Value.InnerModel,
                    MsdecResult.Value,
                    _provider.LoadMs1Spectrums(),
                    _dataBaseMapper,
                    _parameter);
            }
        }

        public bool CanSaveSpectra() => Target.Value.InnerModel != null && MsdecResult.Value != null;

        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();
    }
}
