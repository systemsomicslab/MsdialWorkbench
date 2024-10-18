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
using CompMs.Common.DataStructure;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
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

namespace CompMs.App.Msdial.Model.Lcimms
{
    internal sealed class LcimmsAlignmentModel : AlignmentModelBase
    {
        private static readonly double RtTol = 0.5;
        private static readonly double DtTol = 0.01;
        private static readonly double MzTol = 20;
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> RT_CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT);
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> DRIFT_CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Drift);

        private readonly AlignmentFileBeanModel _alignmentFileBean;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly  MsdialLcImMsParameter _parameter;
        private readonly List<AnalysisFileBean> _files;
        private readonly IMessageBroker _broker;
        private readonly UndoManager _undoManager;

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
            IMessageBroker broker)
            : base(alignmentFileBean, broker) {
            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }
            _alignmentFileBean = alignmentFileBean;
            _dataBaseMapper = mapper;
            _parameter = parameter;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _broker = broker;
            _undoManager = new UndoManager().AddTo(Disposables);

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
            var target = accumulatedTarget.SkipNull()
                .Delay(TimeSpan.FromSeconds(.05d))
                .Select(t =>
                {
                    var idx = orderedProps.IndexOf(t);
                    return propTree.Query(idx, idx + 1).FirstOrDefault();
                })
                .ToReactiveProperty()
                .AddTo(Disposables);
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
                }).AddTo(Disposables);
            Ms1Spots = propModels;

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
            RtMzPlotModel = new AlignmentPeakPlotModel(new ReadOnlyObservableCollection<AlignmentSpotPropertyModel>(accumulatedPropModels), spot => spot.TimesCenter, spot => spot.MassCenter, accumulatedTarget, labelSource, SelectedBrush, Brushes)
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

            DtMzPlotModel = new AlignmentPeakPlotModel(new ReadOnlyObservableCollection<AlignmentSpotPropertyModel>(propModels), spot => spot.TimesCenter, spot => spot.MassCenter, target, labelSource, SelectedBrush, Brushes)
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
            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel)).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference?>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference?>(mapper);
            IMsSpectrumLoader<AlignmentSpotPropertyModel> decLoader = new AlignmentMSDecSpectrumLoader(_alignmentFileBean);
            var spectraExporter = new NistSpectraExporter<AlignmentSpotProperty?>(Target.Select(t => t?.innerModel), mapper, parameter).AddTo(Disposables);
            GraphLabels ms2GraphLabels = new GraphLabels("Representation vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem deconvolutedSpectrumHueItem = new ChartHueItem(projectBaseParameter, Colors.Blue);
            ObservableMsSpectrum upperObservableMsSpectrum = ObservableMsSpectrum.Create(Target, decLoader, spectraExporter).AddTo(Disposables);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.SelectedCandidate.Select(c => mapper.MoleculeMsRefer(c)));
            AlignmentSpotSpectraLoader spectraLoader = new AlignmentSpotSpectraLoader(fileCollection, refLoader, searcherCollection, fileCollection);
            Ms2SpectrumModel = new AlignmentMs2SpectrumModel(
                Target, MatchResultCandidatesModel.SelectedCandidate, fileCollection,
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
                .CombineLatest(MsdecResult, (t, r) => t is not null && r is not null ? CreateCompundSearchModel(t, r, searcherCollection) : null)
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public UndoManager UndoManager => _undoManager;
        public ObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
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

        private CompoundSearchModel<PeakSpotModel>? CreateCompundSearchModel(AlignmentSpotPropertyModel spot, MSDecResult msdec, CompoundSearcherCollection searcherCollection) {
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
        public override void InvokeMoleculerNetworkingForTargetSpot() {
            throw new NotImplementedException();
        }
        public void SaveProject() {
            _alignmentFileBean.SaveAlignmentResultAsync(Container).Wait();
        }

        public void Undo() => _undoManager.Undo();
        public void Redo() => _undoManager.Redo();
    }
}
