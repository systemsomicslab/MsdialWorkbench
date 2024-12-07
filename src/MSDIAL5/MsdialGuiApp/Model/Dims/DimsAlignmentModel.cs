using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Dims
{
    internal sealed class DimsAlignmentModel : AlignmentModelBase
    {
        static DimsAlignmentModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz)!;
        }

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;
        private static readonly double MZ_TOLERANCE = 20d;

        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _matchResultEvaluator;
        private readonly ReadOnlyReactivePropertySlim<MSDecResult?> _msdecResult;
        private readonly ParameterBase _parameter;
        private readonly List<AnalysisFileBean> _files;
        private readonly AnalysisFileBeanModelCollection _fileCollection;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;
        private readonly IMessageBroker _broker;
        private readonly UndoManager _undoManager;

        public DimsAlignmentModel(
            AlignmentFileBeanModel alignmentFileModel,
            DataBaseStorage databaseStorage,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseMapper mapper,
            FilePropertiesModel projectBaseParameter,
            ParameterBase parameter,
            List<AnalysisFileBean> files,
            AnalysisFileBeanModelCollection fileCollection,
            PeakFilterModel peakFilterModel,
            PeakSpotFiltering<AlignmentSpotPropertyModel> peakSpotFiltering,
            MsfinderSearcherFactory msfinderSearcherFactory,
            IMessageBroker broker)
            : base(alignmentFileModel, peakSpotFiltering, peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), broker) {
            if (projectBaseParameter is null) {
                throw new ArgumentNullException(nameof(projectBaseParameter));
            }

            _alignmentFile = alignmentFileModel;

            _parameter = parameter;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _fileCollection = fileCollection ?? throw new ArgumentNullException(nameof(fileCollection));
            _broker = broker;
            _undoManager = new UndoManager().AddTo(Disposables);
            _dataBaseMapper = mapper;
            _matchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databaseStorage, mapper);
            _msfinderSearcherFactory = msfinderSearcherFactory;

            var spotsSource = new AlignmentSpotSource(alignmentFileModel, Container, CHROMATOGRAM_SPOT_SERIALIZER).AddTo(Disposables);
            AlignmentSpotSource = spotsSource;
            Ms1Spots = spotsSource.Spots!.Items;
            InternalStandardSetModel = new InternalStandardSetModel(spotsSource.Spots!.Items, TargetMsMethod.Dims).AddTo(Disposables);
            NormalizationSetModel = new NormalizationSetModel(Container, _files, _fileCollection, _dataBaseMapper, _matchResultEvaluator, InternalStandardSetModel, _parameter, _broker).AddTo(Disposables);
            var filterRegistrationManager = new FilterRegistrationManager<AlignmentSpotPropertyModel>(Ms1Spots, peakSpotFiltering).AddTo(Disposables);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;
            filterRegistrationManager.AttachFilter(Ms1Spots, peakFilterModel, evaluator.Contramap<AlignmentSpotPropertyModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e)), status: ~(FilterEnableStatus.Rt | FilterEnableStatus.Dt));

            var brushMapDataSelector = BrushMapDataSelectorFactory.CreateAlignmentSpotBrushes(parameter.TargetOmics);
            var target = new ReactivePropertySlim<AlignmentSpotPropertyModel?>().AddTo(Disposables);
            Target = target;
            CurrentRepresentativeFile = target.Select(t => t is null ? null : fileCollection.FindByID(t.RepresentativeFileID)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            PlotModel = new AlignmentPeakPlotModel(spotsSource, spot => spot.MassCenter, spot => spot.KMD, target, labelSource, brushMapDataSelector.SelectedBrush, brushMapDataSelector.Brushes.ToList(), PeakLinkModel.Build(spotsSource.Spots.Items, spotsSource.Spots.Items.Select(p => p.innerModel.PeakCharacter).ToList()))
            {
                GraphTitle = ((IFileBean)_alignmentFile).FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.KMD),
                HorizontalTitle = "m/z",
                VerticalTitle = "Kendrick mass defect"
            }.AddTo(Disposables);

            MatchResultCandidatesModel = new MatchResultCandidatesModel(target.Select(t => t?.MatchResultsModel), mapper).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference?>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference?>(mapper);
            IMsSpectrumLoader<AlignmentSpotPropertyModel> decSpecLoader = new AlignmentMSDecSpectrumLoader(_alignmentFile);
            var spectraExporter = new NistSpectraExporter<AlignmentSpotProperty?>(Target.Select(t => t?.innerModel), mapper, parameter).AddTo(Disposables);
            GraphLabels ms2GraphLabels = new GraphLabels("Representation vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem upperSpectrumHueItem = new ChartHueItem(projectBaseParameter, Colors.Blue);
            ObservableMsSpectrum upperObservableMsSpectrum = ObservableMsSpectrum.Create(Target, decSpecLoader, spectraExporter).AddTo(Disposables);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.RetryRefer<MoleculeMsReference?>(mapper)).AddTo(Disposables);
            AlignmentSpotSpectraLoader spectraLoader = new AlignmentSpotSpectraLoader(fileCollection, refLoader, _compoundSearchers, fileCollection);
            Ms2SpectrumModel = new AlignmentMs2SpectrumModel(
                target, MatchResultCandidatesModel.SelectedCandidate.Select(rr => rr?.MatchResult), fileCollection,
                new PropertySelector<SpectrumPeak, double>(nameof(SpectrumPeak.Mass), spot => spot.Mass),
                new PropertySelector<SpectrumPeak, double>(nameof(SpectrumPeak.Intensity), spot => spot.Intensity),
                new ChartHueItem(projectBaseParameter, Colors.Blue),
                new ChartHueItem(projectBaseParameter, Colors.Red),
                new GraphLabels(
                    "Representation vs. Reference",
                    "m/z",
                    "Relative abundance",
                    nameof(SpectrumPeak.Mass),
                    nameof(SpectrumPeak.Intensity)),
                Observable.Return(spectraExporter),
                Observable.Return(referenceExporter),
                null,
                spectraLoader).AddTo(Disposables);

            var classBrush = projectBaseParameter.ClassProperties
                .CollectionChangedAsObservable().ToUnit()
                .StartWith(Unit.Default)
                .SelectSwitch(_ => projectBaseParameter.ClassProperties.Select(prop => prop.ObserveProperty(p => p.Color).Select(_2 => prop)).CombineLatest())
                .Select(lst => new KeyBrushMapper<string>(lst.ToDictionary(item => item.Name, item => item.Color)))
                .ToReactiveProperty().AddTo(Disposables);
            var barBrush = classBrush.Select(bm => bm.Contramap((BarItem item) => item.Class));

            var fileIdToClassNameAsObservable = projectBaseParameter.ObserveProperty(p => p.FileIdToClassName).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var peakSpotAxisLabelAsObservable = target.SelectSwitch(t => t?.ObserveProperty(t_ => t_.IonAbundanceUnit).Select(t_ => t_.ToLabel()) ?? Observable.Return(string.Empty)).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
            var normalizedAreaZeroLoader = new BarItemsLoaderData("Normalized peak area above zero", peakSpotAxisLabelAsObservable, new NormalizedAreaAboveZeroBarItemsLoader(fileIdToClassNameAsObservable, fileCollection), NormalizationSetModel.IsNormalized);
            var normalizedAreaBaselineLoader = new BarItemsLoaderData("Normalized peak area above base line", peakSpotAxisLabelAsObservable, new NormalizedAreaAboveBaseLineBarItemsLoader(fileIdToClassNameAsObservable, fileCollection), NormalizationSetModel.IsNormalized);
            var normalizedHeightLoader = new BarItemsLoaderData("Normalized peak height", peakSpotAxisLabelAsObservable, new NormalizedHeightBarItemsLoader(fileIdToClassNameAsObservable, fileCollection), NormalizationSetModel.IsNormalized);
            var areaZeroLoader = new BarItemsLoaderData("Peak area above zero", "Area", new AreaAboveZeroBarItemsLoader(fileIdToClassNameAsObservable, fileCollection));
            var areaBaselineLoader = new BarItemsLoaderData("Peak area above base line", "Area", new AreaAboveBaseLineBarItemsLoader(fileIdToClassNameAsObservable, fileCollection));
            var heightLoader = new BarItemsLoaderData("Peak height", "Height", new HeightBarItemsLoader(fileIdToClassNameAsObservable, fileCollection));
            var barItemLoaderDatas = new[]
            {
                heightLoader, areaBaselineLoader, areaZeroLoader,
                normalizedHeightLoader, normalizedAreaBaselineLoader, normalizedAreaZeroLoader,
            };
            var barItemsLoaderDataProperty = NormalizationSetModel.Normalized.ToConstant(normalizedHeightLoader).ToReactiveProperty(NormalizationSetModel.IsNormalized.Value ? normalizedHeightLoader : heightLoader).AddTo(Disposables);
            BarChartModel = new BarChartModel(target, barItemsLoaderDataProperty, barItemLoaderDatas, barBrush, projectBaseParameter, fileCollection, projectBaseParameter.ClassProperties).AddTo(Disposables);

            var classToColor = parameter.ClassnameToColorBytes
                .ToDictionary(kvp => kvp.Key, kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2]));
            var fileIdToFileName = files.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
            var eicLoader = alignmentFileModel.CreateEicLoader(CHROMATOGRAM_SPOT_SERIALIZER, fileCollection, projectBaseParameter).AddTo(Disposables);
            AlignmentEicModel = AlignmentEicModel.Create(
                target, eicLoader,
                files, parameter,
                spot => spot.Time,
                spot => spot.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "m/z";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            var barItemsLoaderObservable = barItemsLoaderDataProperty.SkipNull().Select(data => data.Loader);
            var filter = peakSpotFiltering.CreateFilter(peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), FilterEnableStatus.All);
            AlignmentSpotTableModel = new DimsAlignmentSpotTableModel(Ms1Spots, target, barBrush, projectBaseParameter.ClassProperties, barItemsLoaderObservable, filter, spectraLoader, _undoManager).AddTo(Disposables);

            _msdecResult = target
                .DefaultIfNull(t => _alignmentFile.LoadMSDecResultByIndexAsync(t.MasterAlignmentID), Task.FromResult<MSDecResult?>(null))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            CanSearchCompound = new[] {
                target.Select(t => t?.innerModel != null),
                _msdecResult.Select(r => r != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var mzSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, MZ_TOLERANCE, target.Select(t => t?.MassCenter ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<AlignmentSpotPropertyModel>(
                target,
                id => Ms1Spots.Argmin(spot => Math.Abs(spot.MasterAlignmentID - id)),
                target.Select(t => t?.MasterAlignmentID ?? 0d),
                "ID",
                (mzSpotFocus, spot => spot.MassCenter)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, mzSpotFocus);

            var peakInformationModel = new PeakInformationAlignmentModel(target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new MzPoint(t?.innerModel.TimesCenter.Mz.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz));
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(target.SkipNull().SelectSwitch(t => t.ObserveProperty(p => p.ScanMatchResult)).Publish().RefCount(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;

            var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            MoleculeStructureModel = moleculeStructureModel;
            target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.innerModel)).AddTo(Disposables);

            MsfinderParameterSetting = MsfinderParameterSetting.CreateSetting(parameter.ProjectParam);
        }

        public UndoManager UndoManager => _undoManager;

        public ReadOnlyObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public override AlignmentSpotSource AlignmentSpotSource { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public AlignmentPeakPlotModel PlotModel { get; }

        public AlignmentMs2SpectrumModel Ms2SpectrumModel { get; }

        public AlignmentEicModel AlignmentEicModel { get; }

        public BarChartModel BarChartModel { get; }

        public DimsAlignmentSpotTableModel AlignmentSpotTableModel { get; }

        public ReactivePropertySlim<AlignmentSpotPropertyModel?> Target { get; }
        public ReadOnlyReactivePropertySlim<AnalysisFileBeanModel?> CurrentRepresentativeFile { get; }
        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }
        public FocusNavigatorModel FocusNavigatorModel { get; }
        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }
        public MsfinderParameterSetting MsfinderParameterSetting { get; }

        public CompoundSearchModel<PeakSpotModel>? BuildCompoundSearchModel() {
            if (Target.Value is not AlignmentSpotPropertyModel spot || _msdecResult.Value is not MSDecResult msdec) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }
            var plotService = new PlotComparedMsSpectrumUsecase(msdec);
            var compoundSearchModel = new CompoundSearchModel<PeakSpotModel>(
                _files[spot.RepresentativeFileID],
                new PeakSpotModel(spot, msdec),
                new DimsCompoundSearchUsecase(_compoundSearchers.Items),
                plotService,
                new SetAnnotationUsecase(spot, spot.MatchResultsModel, _undoManager));
            compoundSearchModel.Disposables.Add(plotService);
            return compoundSearchModel;
        }

        public InternalStandardSetModel InternalStandardSetModel { get; }

        public NormalizationSetModel NormalizationSetModel { get; }

        public IObservable<bool> CanSetUnknown => Target.Select(t => !(t is null));
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public bool CanSaveSpectra() => Target.Value?.innerModel != null && _msdecResult.Value != null;

        public void SaveSpectra(Stream stream, ExportSpectraFileFormat format) {
            if (Target.Value is AlignmentSpotPropertyModel spot && _msdecResult.Value is { } msdec) {
                SpectraExport.SaveSpectraTable(format, stream, spot.innerModel, msdec, _dataBaseMapper, _parameter);
            }
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

        public override void SearchFragment() {
            using (var decLoader = _alignmentFile.CreateTemporaryMSDecLoader()) {
                MsdialCore.Algorithm.FragmentSearcher.Search(Ms1Spots.Select(n => n.innerModel).ToList(), decLoader, _parameter);
            }
        }

        public override void InvokeMsfinder() {
            if (Target.Value is null || _msdecResult.Value is not MSDecResult result || result.Spectrum.IsEmptyOrNull()) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgram(
                _alignmentFile,
                Target.Value.innerModel,
                result,
                _dataBaseMapper,
                _parameter);
        }

        public InternalMsFinderSingleSpot? CreateSingleSearchMsfinderModel() {
            if (Target.Value is not AlignmentSpotPropertyModel spot || _msdecResult.Value is not MSDecResult result || result.Spectrum.IsEmptyOrNull()) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return null;
            }
            return _msfinderSearcherFactory.CreateModelForAlignmentSpot(MsfinderParameterSetting, Target.Value, result, _undoManager);
        }

        public override void InvokeMoleculerNetworkingForTargetSpot() {
            throw new NotImplementedException();
        }
        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();
    }
}
