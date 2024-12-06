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
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imms
{
    internal sealed class ImmsAlignmentModel : AlignmentModelBase
    {
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;

        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly List<AnalysisFileBean> _files;
        private readonly IMessageBroker _broker;
        private readonly ParameterBase _parameter;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly UndoManager _undoManager;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;

        public ImmsAlignmentModel(
            AlignmentFileBeanModel alignmentFileModel,
            AnalysisFileBeanModelCollection fileCollection,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            DataBaseMapper mapper,
            PeakSpotFiltering<AlignmentSpotPropertyModel> peakSpotFiltering,
            PeakFilterModel peakFilterModel,
            FilePropertiesModel projectBaseParameter,
            ParameterBase parameter,
            List<AnalysisFileBean> files,
            MsfinderSearcherFactory msfinderSearcherFactory,
            IMessageBroker broker)
            : base(alignmentFileModel, peakSpotFiltering, peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), broker) {

            _alignmentFile = alignmentFileModel;
            _parameter = parameter;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _broker = broker;
            _dataBaseMapper = mapper;
            MatchResultEvaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper);
            _undoManager = new UndoManager().AddTo(Disposables);
            _msfinderSearcherFactory = msfinderSearcherFactory;

            var spotsSource = new AlignmentSpotSource(alignmentFileModel, Container, CHROMATOGRAM_SPOT_SERIALIZER).AddTo(Disposables);
            AlignmentSpotSource = spotsSource;
            Ms1Spots = spotsSource.Spots!.Items;
            InternalStandardSetModel = new InternalStandardSetModel(spotsSource.Spots!.Items, TargetMsMethod.Imms).AddTo(Disposables);
            NormalizationSetModel = new NormalizationSetModel(Container, _files, fileCollection, _dataBaseMapper, MatchResultEvaluator, InternalStandardSetModel, _parameter, broker).AddTo(Disposables);

            var brushMapDataSelector = BrushMapDataSelectorFactory.CreateAlignmentSpotBrushes(parameter.TargetOmics);
            Brushes = brushMapDataSelector.Brushes.ToList();
            SelectedBrush = brushMapDataSelector.SelectedBrush;
            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel?>().AddTo(Disposables);
            CurrentRepresentativeFile = Target.Select(t => t is null ? null : fileCollection.FindByID(t.RepresentativeFileID)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var filterRegistrationManager = new FilterRegistrationManager<AlignmentSpotPropertyModel>(spotsSource.Spots!.Items, peakSpotFiltering).AddTo(Disposables);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;
            filterRegistrationManager.AttachFilter(spotsSource.Spots!.Items, peakFilterModel, evaluator.Contramap<AlignmentSpotPropertyModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e)), status: ~FilterEnableStatus.Rt);

            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel);
            PlotModel = new AlignmentPeakPlotModel(spotsSource, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource, SelectedBrush, Brushes, PeakLinkModel.Build(spotsSource.Spots.Items, spotsSource.Spots.Items.Select(p => p.innerModel.PeakCharacter).ToList()))
            {
                GraphTitle = ((IFileBean)alignmentFileModel).FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Mobility [1/k0]",
                VerticalTitle = "m/z",
            }.AddTo(Disposables);

            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel), mapper).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference?>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference?>(mapper);
            IMsSpectrumLoader<AlignmentSpotPropertyModel> decLoader = new AlignmentMSDecSpectrumLoader(_alignmentFile);
            var spectraExporter = new NistSpectraExporter<AlignmentSpotProperty?>(Target.Select(t => t?.innerModel), mapper, parameter).AddTo(Disposables);
            GraphLabels ms2GraphLabels = new GraphLabels("Representation vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem deconvolutedSpectrumHueItem = new ChartHueItem(projectBaseParameter, Colors.Blue);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.RetryRefer<MoleculeMsReference?>(mapper)).AddTo(Disposables);
            AlignmentSpotSpectraLoader spectraLoader = new AlignmentSpotSpectraLoader(fileCollection, refLoader, _compoundSearchers, fileCollection);
            Ms2SpectrumModel = new AlignmentMs2SpectrumModel(
                Target, MatchResultCandidatesModel.SelectedCandidate.Select(rr => rr?.MatchResult), fileCollection,
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
            var peakSpotAxisLabelAsObservable = Target.DefaultIfNull(t => t.ObserveProperty(t_ => t_.IonAbundanceUnit).Select(t_ => t_.ToLabel()), Observable.Return(string.Empty)).Switch().ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
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
            BarChartModel = new BarChartModel(Target, barItemsLoaderDataProperty, barItemLoaderDatas, barBrush, projectBaseParameter, fileCollection, projectBaseParameter.ClassProperties).AddTo(Disposables);

            var classToColor = parameter.ClassnameToColorBytes
                .ToDictionary(kvp => kvp.Key, kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2]));
            var fileIdToFileName = files.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
            var eicLoader = alignmentFileModel.CreateEicLoader(CHROMATOGRAM_SPOT_SERIALIZER, fileCollection, projectBaseParameter).AddTo(Disposables);
            AlignmentEicModel = AlignmentEicModel.Create(
                Target, eicLoader, files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Mobility [1/k0]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            var barItemsLoaderProperty = barItemsLoaderDataProperty.SkipNull().Select(data => data.Loader);
            var filter = peakSpotFiltering.CreateFilter(peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), FilterEnableStatus.All);
            AlignmentSpotTableModel = new ImmsAlignmentSpotTableModel(spotsSource.Spots!.Items, Target, barBrush, projectBaseParameter.ClassProperties, barItemsLoaderProperty, filter, spectraLoader, _undoManager).AddTo(Disposables);

            MsdecResult = Target
                .DefaultIfNull(t => _alignmentFile.LoadMSDecResultByIndexAsync(t.MasterAlignmentID), Task.FromResult((MSDecResult?)null))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            CanSearchCompound = new[]
            {
                Target.Select(t => t?.innerModel is null),
                MsdecResult.Select(r => r is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var peakInformationModel = new PeakInformationAlignmentModel(Target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new MzPoint(t?.MassCenter ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz),
                t => new DriftPoint(t?.innerModel.TimesCenter.Drift.Value ?? 0d),
                t => new CcsPoint(t?.CollisionCrossSection ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.CollisionCrossSection));
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(Target.SkipNull().SelectSwitch(t => t.ObserveProperty(p => p.ScanMatchResult)).Publish().RefCount(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new CcsSimilarity(r_?.CcsSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;

            var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            MoleculeStructureModel = moleculeStructureModel;
            Target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.innerModel)).AddTo(Disposables);

            MsfinderParameterSetting = MsfinderParameterSetting.CreateSetting(parameter.ProjectParam);
        }

        static ImmsAlignmentModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);
        }

        public UndoManager UndoManager => _undoManager;

        public ReadOnlyObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public override AlignmentSpotSource AlignmentSpotSource { get; }
        public InternalStandardSetModel InternalStandardSetModel { get; }
        public NormalizationSetModel NormalizationSetModel { get; }
        public ReactivePropertySlim<AlignmentSpotPropertyModel?> Target { get; }
        public ReadOnlyReactivePropertySlim<AnalysisFileBeanModel?> CurrentRepresentativeFile { get; }
        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public ReadOnlyReactivePropertySlim<MSDecResult?> MsdecResult { get; }

        public AlignmentPeakPlotModel PlotModel { get; }

        public AlignmentMs2SpectrumModel Ms2SpectrumModel { get; }

        public BarChartModel BarChartModel { get; }

        public AlignmentEicModel AlignmentEicModel { get; }

        public ImmsAlignmentSpotTableModel AlignmentSpotTableModel { get; }

        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }

        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }

        public MsfinderParameterSetting MsfinderParameterSetting { get; }

        public CompoundSearchModel<PeakSpotModel>? CreateCompoundSearchModel() {
            if (Target.Value?.innerModel is null || MsdecResult.Value is null) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }

            PlotComparedMsSpectrumUsecase plotService = new PlotComparedMsSpectrumUsecase(MsdecResult.Value);
            var compoundSearchModel = new CompoundSearchModel<PeakSpotModel>(
                _files[Target.Value.RepresentativeFileID],
                new PeakSpotModel(Target.Value, MsdecResult.Value),
                new ImmsCompoundSearchUsecase(_compoundSearchers.Items),
                plotService,
                new SetAnnotationUsecase(Target.Value, Target.Value.MatchResultsModel, _undoManager));
            compoundSearchModel.Disposables.Add(plotService);
            return compoundSearchModel;
        }

        public InternalMsFinderSingleSpot? CreateSingleSearchMsfinderModel() {
            if (Target.Value is not AlignmentSpotPropertyModel spot || MsdecResult.Value is not MSDecResult result || result.Spectrum.IsEmptyOrNull()) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return null;
            }
            return _msfinderSearcherFactory.CreateModelForAlignmentSpot(MsfinderParameterSetting, spot, result, _undoManager);
        }

        public List<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public BrushMapData<AlignmentSpotPropertyModel>? SelectedBrush {
            get => _selectedBrush;
            set => SetProperty(ref _selectedBrush, value);
        }
        private BrushMapData<AlignmentSpotPropertyModel>? _selectedBrush;

        public IMatchResultEvaluator<MsScanMatchResult> MatchResultEvaluator { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }

        public void SaveSpectra(string filename) {
            if (Target.Value is not AlignmentSpotPropertyModel spot || MsdecResult.Value is not { } msdec) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return;
            }
            using var file = File.Open(filename, FileMode.Create);
            SpectraExport.SaveSpectraTable(
                (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                file,
                spot.innerModel,
                msdec,
                _dataBaseMapper,
                _parameter);
        }

        public bool CanSaveSpectra() => Target.Value?.innerModel != null && MsdecResult.Value != null;

        public IObservable<bool> CanSetUnknown => Target.Select(t => !(t is null));
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public override void SearchFragment() {
            using (var decLoader = _alignmentFile.CreateTemporaryMSDecLoader()) {
                MsdialCore.Algorithm.FragmentSearcher.Search(Ms1Spots.Select(n => n.innerModel).ToList(), decLoader, _parameter);
            }
        }

        public override void InvokeMsfinder() {
            if (Target.Value is null || MsdecResult.Value is not MSDecResult result || result.Spectrum.IsEmptyOrNull()) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgram(
                _alignmentFile,
                Target.Value.innerModel,
                MsdecResult.Value,
                _dataBaseMapper,
                _parameter);
        }
        public override void InvokeMoleculerNetworkingForTargetSpot() {
            throw new NotImplementedException();
        }

        public void SaveProject() {
            _alignmentFile.SaveAlignmentResultAsync(Container).Wait();
        }

        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();
    }
}
