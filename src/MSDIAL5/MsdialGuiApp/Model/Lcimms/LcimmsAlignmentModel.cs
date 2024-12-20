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
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.DataStructure;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Algorithm.Function;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Algorithm;
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
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Media;
using CompMs.MsdialCore.Parameter;

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsAlignmentModel : AlignmentModelBase
    {
        private static readonly double RtTol = 0.5;
        private static readonly double DtTol = 0.01;
        private static readonly double MzTol = 20;
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> RT_CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT)!;
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> DRIFT_CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift)!;

        private readonly AlignmentFileBeanModel _alignmentFileBean;
        private readonly FilePropertiesModel _projectBaseParameter;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly  MsdialLcImMsParameter _parameter;
        private readonly List<AnalysisFileBean> _files;
        private readonly IMessageBroker _broker;
        private readonly UndoManager _undoManager;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;
        private readonly ReactiveProperty<BarItemsLoaderData> _barItemsLoaderDataProperty;
        private readonly PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter _filter;

        public LcimmsAlignmentModel(
            AlignmentFileBeanModel alignmentFileBean,
            AnalysisFileBeanModelCollection fileCollection,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            DataBaseMapper mapper,
            FilePropertiesModel projectBaseParameter,
            MsdialLcImMsParameter parameter,
            List<AnalysisFileBean> files,
            PeakSpotFiltering<AlignmentSpotPropertyModel> peakSpotFiltering,
            PeakFilterModel peakFilterModel,
            PeakFilterModel accumulatedPeakFilterModel,
            MsfinderSearcherFactory msfinderSearcherFactory,
            IMessageBroker broker)
            : base(alignmentFileBean, peakSpotFiltering, peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), broker) {
            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }
            _alignmentFileBean = alignmentFileBean;
            _dataBaseMapper = mapper;
            _parameter = parameter;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _broker = broker;
            _undoManager = new UndoManager().AddTo(Disposables);
            _projectBaseParameter = projectBaseParameter;
            _msfinderSearcherFactory = msfinderSearcherFactory;

            BarItemsLoader = new HeightBarItemsLoader(parameter.FileID_ClassName, fileCollection);

            var props = Container.AlignmentSpotProperties;
            var orderedProps = props.OrderBy(prop => prop.TimesCenter.RT.Value).Select(prop => new AlignmentSpotPropertyModel(prop).AddTo(Disposables)).ToArray();
            var propTree = new SegmentTree<IEnumerable<AlignmentSpotPropertyModel>>(props.Count, Enumerable.Empty<AlignmentSpotPropertyModel>(), Enumerable.Concat);
            using (propTree.LazyUpdate()) {
                foreach (var (prop, index) in orderedProps.WithIndex()) {
                    propTree[index] = prop.innerModel.AlignmentDriftSpotFeatures.Select(dprop => new AlignmentSpotPropertyModel(dprop).AddTo(Disposables)).ToArray();
                }
            }
            var driftProps = new ObservableCollection<AlignmentSpotPropertyModel>(propTree.Query(0, props.Count));
            var propRanges = new Dictionary<AlignmentSpotPropertyModel, (int, int)>();
            {
                var j = 0;
                var k = 0;
                foreach (var orderedProp in orderedProps) {
                    while (j < orderedProps.Length && orderedProps[j].innerModel.TimesCenter.RT.Value < orderedProp.innerModel.TimesCenter.RT.Value - parameter.AccumulatedRtRange / 2) {
                        j++;
                    }
                    while (k < orderedProps.Length && orderedProps[k].innerModel.TimesCenter.RT.Value < orderedProp.innerModel.TimesCenter.RT.Value + parameter.AccumulatedRtRange / 2) {
                        k++;
                    }
                    propRanges[orderedProp] = (j, k);
                }
            }
            var accumulatedTarget = new ReactivePropertySlim<AlignmentSpotPropertyModel?>().AddTo(Disposables);
            var target = new ReactiveProperty<AlignmentSpotPropertyModel?>().AddTo(Disposables);
                //accumulatedTarget.SkipNull()
                //.Delay(TimeSpan.FromSeconds(.05d))
                //.Select(t =>
                //{
                //    var idx = orderedProps.IndexOf(t);
                //    return propTree.Query(idx, idx + 1).FirstOrDefault();
                //})
                //.ToReactiveProperty()
                //.AddTo(Disposables);
            Target = target;
            CurrentRepresentativeFile = Target.Select(t => t is null ? null : fileCollection.FindByID(t.RepresentativeFileID)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var accumulatedPropModels = new ObservableCollection<AlignmentSpotPropertyModel>(orderedProps);
            var propModels = new ReactiveCollection<AlignmentSpotPropertyModel>(UIDispatcherScheduler.Default).AddTo(Disposables);
            accumulatedTarget.SkipNull()
                .Select(t =>
                {
                    var (lo, hi) = propRanges[t];
                    return propTree.Query(lo, hi);
                })
                .Subscribe(props_ =>
                {
                    using (System.Windows.Data.CollectionViewSource.GetDefaultView(propModels).DeferRefresh()) {
                        propModels.ClearOnScheduler();
                        propModels.AddRangeOnScheduler(props_);
                    }
                    target.Value = propModels.FirstOrDefault();
                }).AddTo(Disposables);
            Ms1Spots = propModels;
            AlignmentSpotSource = new AlignmentSpotSource(alignmentFileBean, Container, DRIFT_CHROMATOGRAM_SPOT_SERIALIZER).AddTo(Disposables);

            var filterRegistrationManager = new FilterRegistrationManager<AlignmentSpotPropertyModel>(driftProps, peakSpotFiltering).AddTo(Disposables);
            var filterableEvaluator = evaluator.Contramap<AlignmentSpotPropertyModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e));
            filterRegistrationManager.AttachFilter(driftProps, peakFilterModel, filterableEvaluator, status: FilterEnableStatus.All);
            filterRegistrationManager.AttachFilter(propModels, peakFilterModel, evaluator: filterableEvaluator, status: FilterEnableStatus.All);
            var accEvaluator = new AccumulatedPeakEvaluator(evaluator);
            var accFilterableEvaluator = accEvaluator.Contramap<AlignmentSpotPropertyModel, AlignmentSpotProperty>(filterable => filterable.innerModel);
            filterRegistrationManager.AttachFilter(accumulatedPropModels, accumulatedPeakFilterModel, evaluator: accFilterableEvaluator, status: FilterEnableStatus.None);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;

            InternalStandardSetModel = new InternalStandardSetModel(driftProps, TargetMsMethod.Lcimms).AddTo(Disposables);
            NormalizationSetModel = new NormalizationSetModel(Container, files, fileCollection, mapper, evaluator, InternalStandardSetModel, parameter, broker).AddTo(Disposables);

            var brushMapDataSelector = BrushMapDataSelectorFactory.CreateAlignmentSpotBrushes(parameter.TargetOmics);
            Brushes = brushMapDataSelector.Brushes.ToList();
            SelectedBrush = brushMapDataSelector.SelectedBrush;
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel);
            RtMzPlotModel = new AlignmentPeakPlotModel(new ReadOnlyObservableCollection<AlignmentSpotPropertyModel>(accumulatedPropModels), spot => spot.TimesCenter, spot => spot.MassCenter, accumulatedTarget, labelSource, SelectedBrush, Brushes, PeakLinkModel.Build(accumulatedPropModels, accumulatedPropModels.Select(p => p.innerModel.PeakCharacter).ToList()))
            {
                GraphTitle = ((IFileBean)alignmentFileBean).FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
            }.AddTo(Disposables);
            accumulatedTarget.Select(
                t => $"Alignment: {((IFileBean)alignmentFileBean).FileName}" +
                    (t is null
                        ? string.Empty
                        : $"Spot ID: {t.MasterAlignmentID} Mass m/z: {t.MassCenter:F5} RT: {t.innerModel.TimesCenter.RT.Value:F2} min"))
                .Subscribe(title => RtMzPlotModel.GraphTitle = title)
                .AddTo(Disposables);
            var classToColor = parameter.ClassnameToColorBytes
                .ToDictionary(kvp => kvp.Key, kvp => Color.FromRgb(kvp.Value[0], kvp.Value[1], kvp.Value[2]));
            var fileIdToFileName = files.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
            var eicLoader = alignmentFileBean.CreateEicLoader(RT_CHROMATOGRAM_SPOT_SERIALIZER, fileCollection, projectBaseParameter).AddTo(Disposables);
            RtAlignmentEicModel = AlignmentEicModel.Create(
                accumulatedTarget, eicLoader, files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            RtAlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            RtAlignmentEicModel.Elements.VerticalTitle = "Abundance";
            RtAlignmentEicModel.Elements.HorizontalTitle = "Retention time [min]";
            RtAlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            RtAlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            DtMzPlotModel = new AlignmentPeakPlotModel(new ReadOnlyObservableCollection<AlignmentSpotPropertyModel>(propModels), spot => spot.TimesCenter, spot => spot.MassCenter, target, labelSource, SelectedBrush, Brushes, PeakLinkModel.Build(propModels, propModels.Select(p => p.innerModel.PeakCharacter).ToList()))
            {
                GraphTitle = ((IFileBean)alignmentFileBean).FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Mobility [1/K0]",
                VerticalTitle = "m/z",
            }.AddTo(Disposables);
            accumulatedTarget.Select(
                t => $"{((IFileBean)alignmentFileBean).FileName}" +
                    (t is null
                        ? string.Empty
                        : $"Spot ID: {t.MasterAlignmentID} Mass m/z: {t.MassCenter:F5} Mobility [1/K0]: {t.innerModel.TimesCenter.Drift.Value:F4}"))
                .Subscribe(title => DtMzPlotModel.GraphTitle = title)
                .AddTo(Disposables);
            var dtEicLoader = alignmentFileBean.CreateEicLoader(DRIFT_CHROMATOGRAM_SPOT_SERIALIZER, fileCollection, projectBaseParameter).AddTo(Disposables);
            DtAlignmentEicModel = AlignmentEicModel.Create(
                target, dtEicLoader, files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            DtAlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            DtAlignmentEicModel.Elements.VerticalTitle = "Abundance";
            DtAlignmentEicModel.Elements.HorizontalTitle = "Mobility [1/K0]";
            DtAlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            DtAlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            var searcherCollection = CompoundSearcherCollection.BuildSearchers(databases, mapper);
            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel), mapper).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference?>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference?>(mapper);
            IMsSpectrumLoader<AlignmentSpotPropertyModel> decLoader = new AlignmentMSDecSpectrumLoader(_alignmentFileBean);
            var spectraExporter = new NistSpectraExporter<AlignmentSpotProperty?>(Target.Select(t => t?.innerModel), mapper, parameter).AddTo(Disposables);
            GraphLabels ms2GraphLabels = new GraphLabels("Representation vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem deconvolutedSpectrumHueItem = new ChartHueItem(projectBaseParameter, Colors.Blue);
            ObservableMsSpectrum upperObservableMsSpectrum = ObservableMsSpectrum.Create(Target, decLoader, spectraExporter).AddTo(Disposables);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.RetryRefer<MoleculeMsReference?>(mapper)).AddTo(Disposables);
            AlignmentSpotSpectraLoader spectraLoader = new AlignmentSpotSpectraLoader(fileCollection, refLoader, searcherCollection, fileCollection);
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
            var peakSpotAxisLabelAsObservable = target!.OfType<AlignmentSpotPropertyModel>().SelectSwitch(t => t.ObserveProperty(t_ => t_.IonAbundanceUnit).Select(t_ => t_.ToLabel())).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
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
            _barItemsLoaderDataProperty = barItemsLoaderDataProperty;
            _filter = peakSpotFiltering.CreateFilter(peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), FilterEnableStatus.All);

            RtBarChartModel = new BarChartModel(accumulatedTarget, barItemsLoaderDataProperty, barItemLoaderDatas, barBrush, projectBaseParameter, fileCollection, projectBaseParameter.ClassProperties).AddTo(Disposables);
            DtBarChartModel = new BarChartModel(target, barItemsLoaderDataProperty, barItemLoaderDatas, barBrush, projectBaseParameter, fileCollection, projectBaseParameter.ClassProperties).AddTo(Disposables);

            var barItemsLoaderProperty = barItemsLoaderDataProperty.SkipNull().Select(data => data.Loader);
            var filter = peakSpotFiltering.CreateFilter(peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), FilterEnableStatus.All);
            AlignmentSpotTableModel = new LcimmsAlignmentSpotTableModel(driftProps, target, barBrush, projectBaseParameter.ClassProperties, barItemsLoaderProperty, filter, spectraLoader, _undoManager).AddTo(Disposables);

            MsdecResult = target
                .DefaultIfNull(t => _alignmentFileBean.LoadMSDecResultByIndexAsync(t.MasterAlignmentID), Task.FromResult<MSDecResult?>(null))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            var mzSpotFocus = new ChromSpotFocus(DtMzPlotModel.VerticalAxis, MzTol, target.Select(t => t?.MassCenter ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var rtSpotFocus = new ChromSpotFocus(RtMzPlotModel.HorizontalAxis, RtTol, accumulatedTarget.Select(t => t?.innerModel.TimesCenter.RT.Value ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var dtSpotFocus = new ChromSpotFocus(DtMzPlotModel.HorizontalAxis, DtTol, target.Select(t => t?.innerModel.TimesCenter.Drift.Value ?? 0d), "F3", "Mobility [1/K0]", isItalic: false).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<AlignmentSpotPropertyModel>(
                target,
                id => Ms1Spots.Argmin(spot => Math.Abs(spot.MasterAlignmentID - id)),
                target.Select(t => t?.MasterAlignmentID ?? 0d),
                "ID",
                (mzSpotFocus, spot => spot.MassCenter),
                (rtSpotFocus, spot => spot.innerModel.TimesCenter.RT.Value),
                (dtSpotFocus, spot => spot.innerModel.TimesCenter.Drift.Value)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, mzSpotFocus, rtSpotFocus, dtSpotFocus);

            var peakInformationModel = new PeakInformationAlignmentModel(target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new RtPoint(t?.innerModel.TimesCenter.RT.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RT.Value),
                t => new MzPoint(t?.MassCenter ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz),
                t => new DriftPoint(t?.innerModel.TimesCenter.Drift.Value ?? 0d),
                t => new CcsPoint(t?.CollisionCrossSection ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.CollisionCrossSection));
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
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
            Target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.innerModel)).AddTo(Disposables);

            CompoundSearchModel = target
                .CombineLatest(MsdecResult, (t, r) => t is not null && r is not null ? CreateCompoundSearchModel(t, r, searcherCollection) : null)
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            MsfinderParameterSetting = MsfinderParameterSetting.CreateSetting(parameter.ProjectParam);
        }

        public UndoManager UndoManager => _undoManager;
        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public override AlignmentSpotSource AlignmentSpotSource { get; }
        public ReactiveProperty<AlignmentSpotPropertyModel?> Target { get; }
        public ReadOnlyReactivePropertySlim<AnalysisFileBeanModel?> CurrentRepresentativeFile { get; }
        public ReadOnlyReactivePropertySlim<MSDecResult?> MsdecResult { get; }

        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public InternalStandardSetModel InternalStandardSetModel { get; }
        public NormalizationSetModel NormalizationSetModel { get; }
        public AlignmentPeakPlotModel RtMzPlotModel { get; }
        public AlignmentPeakPlotModel DtMzPlotModel { get; }

        public AlignmentMs2SpectrumModel Ms2SpectrumModel { get; }

        public BarChartModel RtBarChartModel { get; }
        public BarChartModel DtBarChartModel { get; }
        public LcimmsAlignmentSpotTableModel AlignmentSpotTableModel { get; }
        public AlignmentEicModel RtAlignmentEicModel { get; }
        public AlignmentEicModel DtAlignmentEicModel { get; }

        public FocusNavigatorModel FocusNavigatorModel { get; }

        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }
        public MsfinderParameterSetting MsfinderParameterSetting { get; }
        public List<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public BrushMapData<AlignmentSpotPropertyModel>? SelectedBrush {
            get => selectedBrush;
            set => SetProperty(ref selectedBrush, value);
        }
        private BrushMapData<AlignmentSpotPropertyModel>? selectedBrush;

        public IBarItemsLoader BarItemsLoader { get; }

        public IObservable<bool> CanSetUnknown => Target.Select(t => !(t is null));
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public ReadOnlyReactivePropertySlim<CompoundSearchModel<PeakSpotModel>> CompoundSearchModel { get; }

        private CompoundSearchModel<PeakSpotModel>? CreateCompoundSearchModel(AlignmentSpotPropertyModel spot, MSDecResult msdec, CompoundSearcherCollection searcherCollection) {
            if (spot is null || msdec is null) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }
            var plotService = new PlotComparedMsSpectrumUsecase(msdec);
            var compoundSearchModel = new CompoundSearchModel<PeakSpotModel>(
                _files[spot.RepresentativeFileID],
                new PeakSpotModel(spot, msdec),
                new LcimmsCompoundSearchUsecase(searcherCollection.Items),
                plotService,
                new SetAnnotationUsecase(spot, spot.MatchResultsModel, _undoManager));
            compoundSearchModel.Disposables.Add(plotService);
            return compoundSearchModel;
        }

        public override void SearchFragment() {
            using (var decLoader = _alignmentFileBean.CreateTemporaryMSDecLoader()) {
                MsdialCore.Algorithm.FragmentSearcher.Search(Ms1Spots.Select(n => n.innerModel).ToList(), decLoader, _parameter);
            }
        }

        public InternalMsFinderSingleSpot? CreateSingleSearchMsfinderModel() {
            if (Target.Value is not AlignmentSpotPropertyModel spot || MsdecResult.Value is not MSDecResult result || result.Spectrum.IsEmptyOrNull()) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return null;
            }
            return _msfinderSearcherFactory.CreateModelForAlignmentSpot(MsfinderParameterSetting, spot, result, _undoManager);
        }

        public override void InvokeMsfinder() {
            if (Target.Value is null || MsdecResult.Value is null || MsdecResult.Value.Spectrum.IsEmptyOrNull()) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgram(
                _alignmentFileBean,
                Target.Value.innerModel,
                MsdecResult.Value,
                _dataBaseMapper,
                _parameter);
        }

        public override void ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
            network.ExportNodeEdgeFiles(parameter.ExportFolderPath);
        }

        public override void InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var network = GetMolecularNetworkInstance(parameter, useCurrentFiltering);
            CytoscapejsModel.SendToCytoscapeJs(network);
        }

        public override void InvokeMoleculerNetworkingForTargetSpot() {
            var network = GetMolecularNetworkInstanceForTargetSpot(_parameter.MolecularSpectrumNetworkingBaseParam);
            CytoscapejsModel.SendToCytoscapeJs(network);
        }
        public void SaveProject() {
            _alignmentFileBean.SaveAlignmentResultAsync(Container).Wait();
        }

        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();

        private MolecularNetworkInstance GetMolecularNetworkInstance(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var param = _projectBaseParameter;
            var loaderProperty = _barItemsLoaderDataProperty.Value;
            var loader = loaderProperty.Loader;
            var publisher = new TaskProgressPublisher(_broker, $"Exporting MN results in {parameter.ExportFolderPath}");

            using (publisher.Start()) {
                IReadOnlyList<AlignmentSpotPropertyModel> spots = Ms1Spots;
                if (useCurrentFiltering) {
                    spots = _filter.Filter(Ms1Spots).ToList();
                }
                var flatten = spots.Select(n => n.innerModel).SelectMany(s => s.IsMultiLayeredData() ? s.AlignmentDriftSpotFeatures : [s]).ToList();
                var flattenmodel = flatten.Select(n => new AlignmentSpotPropertyModel(n)).ToList();
                var peaks = _alignmentFileModel.LoadMSDecResults();
                var flattenpeaks = flatten.Select(n => peaks[n.MasterAlignmentID]).ToList();

                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Exporting MN results in {parameter.ExportFolderPath}");
                }
                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMolecularNetworkInstance(flatten, flattenpeaks, query, notify);
                var rootObj = network.Root;
                for (int i = 0; i < rootObj.nodes.Count; i++) {
                    var node = rootObj.nodes[i];
                    node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(flattenmodel[i], loader, param.ClassProperties.ClassToColor);
                }
                var ionfeature_edges = MolecularNetworking.GenerateFeatureLinkedEdges(flatten, flatten.ToDictionary(s => s.MasterAlignmentID, s => s.PeakCharacter));
                rootObj.edges.AddRange(ionfeature_edges);

                if (parameter.MnIsExportIonCorrelation && _alignmentFileModel.CountRawFiles >= 6) {
                    var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(flatten, parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode);
                    rootObj.edges.AddRange(ion_edges);
                }

                return network;
            }
        }

        private MolecularNetworkInstance GetMolecularNetworkInstanceForTargetSpot(MolecularSpectrumNetworkingBaseParameter parameter) {
            if (parameter.MaxEdgeNumberPerNode == 0) {
                parameter.MinimumPeakMatch = 3;
                parameter.MaxEdgeNumberPerNode = 6;
                parameter.MaxPrecursorDifference = 400;
            }
            if (Target.Value is null) {
                return new MolecularNetworkInstance(new CompMs.Common.DataObj.NodeEdge.RootObject());
            }

            var param = _projectBaseParameter;
            var loaderProperty = _barItemsLoaderDataProperty.Value;
            var loader = loaderProperty.Loader;
            var publisher = new TaskProgressPublisher(_broker, $"Preparing MN results");

            using (publisher.Start()) {
                var spots = Ms1Spots;
                var flatten = spots.Select(n => n.innerModel).SelectMany(s => s.IsMultiLayeredData() ? s.AlignmentDriftSpotFeatures : [s]).ToList();
                var peaks = _alignmentFileModel.LoadMSDecResults();
                var flattenpeaks = flatten.Select(n => peaks[n.MasterAlignmentID]).ToList();
                var id2index = flatten.Select((spot, index) => new { spot.MasterAlignmentID, Index = index }).ToDictionary(item => item.MasterAlignmentID, item => item.Index);

                var targetSpot = Target.Value;
                var targetPeak = peaks[targetSpot.MasterAlignmentID];

                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Preparing MN results");
                }
                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMoleculerNetworkInstanceForTargetSpot(targetSpot.innerModel, targetPeak, flatten, flattenpeaks, query, notify);
                var rootObj = network.Root;

                for (int i = 0; i < rootObj.nodes.Count; i++) {
                    var node = rootObj.nodes[i];
                    node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(spots[id2index[node.data.id]], loader, param.ClassProperties.ClassToColor);
                }

                if (parameter.MnIsExportIonCorrelation && _alignmentFileModel.CountRawFiles >= 6) {
                    var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(spots.Select(n => n.innerModel).ToList(), parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode);
                    rootObj.edges.AddRange(ion_edges.Where(e => e.data.source == targetSpot.MasterAlignmentID || e.data.target == targetSpot.MasterAlignmentID));
                }

                return network;
            }
        }
    }
}
