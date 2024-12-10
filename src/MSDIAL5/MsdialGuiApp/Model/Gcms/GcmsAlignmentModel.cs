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
using CompMs.Common.Utility;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Algorithm;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialGcMsApi.Parser;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Gcms
{
    internal sealed class GcmsAlignmentModel : AlignmentModelBase
    {
        private static readonly double _rt_tol = .5, _ri_tol = 20d, _mz_tol = 20d;
        private readonly ProjectBaseParameter _projectParameter;
        private readonly AnalysisFileBeanModelCollection _fileCollection;
        private readonly CalculateMatchScore? _calculateMatchScore;
        private readonly IMessageBroker _broker;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;
        private readonly ReactivePropertySlim<AlignmentSpotPropertyModel?> _target;
        private readonly ReadOnlyReactivePropertySlim<MSDecResult?> _msdecResult;

        public GcmsAlignmentModel(
            AlignmentFileBeanModel alignmentFileBean,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            PeakSpotFiltering<AlignmentSpotPropertyModel> peakSpotFiltering,
            PeakFilterModel peakFilterModel,
            DataBaseMapper mapper,
            MsdialGcmsParameter parameter,
            FilePropertiesModel projectBaseParameter,
            List<AnalysisFileBean> files,
            AnalysisFileBeanModelCollection fileCollection,
            CalculateMatchScore? calculateMatchScore,
            MsfinderSearcherFactory msfinderSearcherFactory,
            IMessageBroker broker)
            : base(alignmentFileBean, peakSpotFiltering, peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), broker) {
            _projectParameter = parameter.ProjectParam;
            _fileCollection = fileCollection ?? throw new ArgumentNullException(nameof(fileCollection));
            _calculateMatchScore = calculateMatchScore;
            _broker = broker;
            UndoManager = new UndoManager().AddTo(Disposables);
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper);
            _msfinderSearcherFactory = msfinderSearcherFactory;

            ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer = default!;
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RI)!;
                    chromatogramSpotSerializer = new RIChromatogramSerializerDecorator(chromatogramSpotSerializer, parameter.GetRIHandlers());
                    break;
                case AlignmentIndexType.RT:
                default:
                    chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT)!;
                    break;
            }
            var target = new ReactivePropertySlim<AlignmentSpotPropertyModel?>().AddTo(Disposables);
            _target = target;

            var spotsSource = new AlignmentSpotSource(alignmentFileBean, Container, chromatogramSpotSerializer).AddTo(Disposables);
            AlignmentSpotSource = spotsSource;

            InternalStandardSetModel = new InternalStandardSetModel(spotsSource.Spots!.Items, TargetMsMethod.Gcms).AddTo(Disposables);
            NormalizationSetModel = new NormalizationSetModel(Container, files, fileCollection, mapper, evaluator, InternalStandardSetModel, parameter, broker).AddTo(Disposables);

            _msdecResult = target.DefaultIfNull(t => alignmentFileBean.LoadMSDecResultByIndexAsync(t.MasterAlignmentID), Task.FromResult<MSDecResult?>(null))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            var filterRegistrationManager = new FilterRegistrationManager<AlignmentSpotPropertyModel>(spotsSource.Spots!.Items, peakSpotFiltering).AddTo(Disposables);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;
            filterRegistrationManager.AttachFilter(spotsSource.Spots!.Items, peakFilterModel, evaluator.Contramap<AlignmentSpotPropertyModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e)), status: FilterEnableStatus.All & ~FilterEnableStatus.Dt);

            // Peak scatter plot
            var brushMapDataSelector = BrushMapDataSelectorFactory.CreateAlignmentSpotBrushes(parameter.TargetOmics);
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            PlotModel = new AlignmentPeakPlotModel(spotsSource, spot => spot.TimesCenter, spot => spot.MassCenter, target, labelSource, brushMapDataSelector.SelectedBrush, brushMapDataSelector.Brushes, PeakLinkModel.Build(spotsSource.Spots.Items, spotsSource.Spots.Items.Select(p => p.innerModel.PeakCharacter).ToList()))
            {
                GraphTitle = alignmentFileBean.FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                VerticalTitle = "m/z",
            }.AddTo(Disposables);
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    PlotModel.HorizontalTitle = "Retention index";
                    break;
                case AlignmentIndexType.RT:
                default:
                    PlotModel.HorizontalTitle = "Retention time (min)";
                    break;
            }
            GcgcPlotModel = new GcgcAlignmentPeakPlotModel(spotsSource, spot => spot.TimesCenter, spot => spot.RT, target, labelSource, brushMapDataSelector.SelectedBrush, brushMapDataSelector.Brushes, PeakLinkModel.Build(spotsSource.Spots.Items, spotsSource.Spots.Items.Select(p => p.innerModel.PeakCharacter).ToList()), PlotModel.HorizontalAxis)
            {
                GraphTitle = alignmentFileBean.FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.RT),
                HorizontalTitle = PlotModel.HorizontalTitle,
                VerticalTitle = "2nd column retention time (min)",
            }.AddTo(Disposables);

            MatchResultCandidatesModel = new MatchResultCandidatesModel(target.Select(t => t?.MatchResultsModel), mapper).AddTo(Disposables);

            // MS spectrum
            var refLoader = new ReferenceSpectrumLoader<MoleculeMsReference?>(mapper);
            IMsSpectrumLoader<AlignmentSpotPropertyModel> msDecSpectrumLoader = new AlignmentMSDecSpectrumLoader(alignmentFileBean);
            var spectraExporter = new NistSpectraExporter<AlignmentSpotProperty?>(target.Select(t => t?.innerModel), mapper, parameter).AddTo(Disposables);
            GraphLabels msGraphLabels = new GraphLabels("Representative vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem deconvolutedSpectrumHueItem = new ChartHueItem(projectBaseParameter, Colors.Blue);
            ObservableMsSpectrum deconvolutedObservableMsSpectrum = ObservableMsSpectrum.Create(target, msDecSpectrumLoader, spectraExporter).AddTo(Disposables);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.RetryRefer<MoleculeMsReference?>(mapper)).AddTo(Disposables);
            AlignmentSpotSpectraLoader spectraLoader = new AlignmentSpotSpectraLoader(fileCollection, refLoader, _compoundSearchers, fileCollection);
            MsSpectrumModel = new AlignmentMs2SpectrumModel(
                target, MatchResultCandidatesModel.SelectedCandidate.Select(rr => rr?.MatchResult), fileCollection,
                new PropertySelector<SpectrumPeak, double>(nameof(SpectrumPeak.Mass), peak => peak.Mass),
                new PropertySelector<SpectrumPeak, double>(nameof(SpectrumPeak.Intensity), peak => peak.Intensity),
                new ChartHueItem(projectBaseParameter, Colors.Blue),
                new ChartHueItem(projectBaseParameter, Colors.Red),
                new GraphLabels("Representative vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity)),
                Observable.Return(spectraExporter),
                Observable.Return(referenceExporter),
                null,
                spectraLoader).AddTo(Disposables);

            // Class intensity bar chart
            var classBrush = projectBaseParameter.ClassProperties
                .CollectionChangedAsObservable().ToUnit()
                .StartWith(Unit.Default)
                .SelectSwitch(_ => projectBaseParameter.ClassProperties.Select(prop => prop.ObserveProperty(p => p.Color).Select(_2 => prop)).CombineLatest())
                .Select(lst => new KeyBrushMapper<string>(lst.ToDictionary(item => item.Name, item => item.Color)))
                .ToReactiveProperty().AddTo(Disposables);
            var barBrush = classBrush.Select(bm => bm.Contramap((BarItem item) => item.Class));

            var fileIdToClassNameAsObservable = projectBaseParameter.ObserveProperty(p => p.FileIdToClassName).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var peakSpotAxisLabelAsObservable = target.DefaultIfNull(t => t.ObserveProperty(t_ => t_.IonAbundanceUnit).Select(t_ => t_.ToLabel()), Observable.Return(string.Empty)).Switch().ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
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

            // Class eic
            var fileIdToFileName = files.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
            AlignmentEicModel = AlignmentEicModel.Create(
                target,
                alignmentFileBean.CreateEicLoader(chromatogramSpotSerializer, fileCollection, projectBaseParameter).AddTo(Disposables),
                files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "EIC";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    AlignmentEicModel.Elements.HorizontalTitle = "Retention index";
                    break;
                case AlignmentIndexType.RT:
                default:
                    AlignmentEicModel.Elements.HorizontalTitle = "Retention time [min]";
                    break;
            }

            var barItemsLoaderProperty = barItemsLoaderDataProperty.Select(data => data.Loader).ToReactiveProperty<IBarItemsLoader>().AddTo(Disposables);
            var filter = peakSpotFiltering.CreateFilter(peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), FilterEnableStatus.All);
            AlignmentSpotTableModel = new GcmsAlignmentSpotTableModel(spotsSource.Spots!.Items, target, barBrush, projectBaseParameter.ClassProperties, barItemsLoaderProperty, filter, spectraLoader, UndoManager).AddTo(Disposables);

            var peakInformationModel = new PeakInformationAlignmentModel(target).AddTo(Disposables);
            peakInformationModel.Add(t => new QuantMassPoint(t?.MassCenter ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz));
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    peakInformationModel.Add(t => new RiPoint(t?.innerModel.TimesCenter.RI.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RI.Value));
                    peakInformationModel.Add(t => new RtPoint(t?.innerModel.TimesCenter.RT.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RT.Value));
                    break;
                case AlignmentIndexType.RT:
                    peakInformationModel.Add(t => new RtPoint(t?.innerModel.TimesCenter.RT.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RT.Value));
                    if (parameter.RefSpecMatchBaseParam.MayRiDictionaryImported) {
                        peakInformationModel.Add(t => new RiPoint(t?.innerModel.TimesCenter.RI.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RI.Value));
                    }
                    break;
            }
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(target.DefaultIfNull(t => t.ObserveProperty(p => p.ScanMatchResult), Observable.Return<MsScanMatchResult?>(null)).Switch(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d));
            switch (parameter.RetentionType) {
                case RetentionType.RI:
                    compoundDetailModel.Add(r_ => new RiSimilarity(r_?.RiSimilarity ?? 0d));
                    break;
                case RetentionType.RT:
                    compoundDetailModel.Add(r_ => new RtSimilarity(r_?.RtSimilarity ?? 0d));
                    break;
            }
            compoundDetailModel.Add(r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;

            var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
            MoleculeStructureModel = moleculeStructureModel;
            target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.innerModel)).AddTo(Disposables);

            ISpotFocus timeSpotFocus;
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    timeSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, _ri_tol, target.DefaultIfNull(t => t.TimesCenter), "F0", "RI", isItalic: false).AddTo(Disposables);
                    break;
                case AlignmentIndexType.RT:
                default:
                    timeSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, _rt_tol, target.DefaultIfNull(t => t.TimesCenter), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
                    break;
            }
            var mzSpotFocus = new ChromSpotFocus(PlotModel.VerticalAxis, _mz_tol, target.DefaultIfNull(t => t.MassCenter), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<AlignmentSpotPropertyModel>(
                target,
                id => spotsSource.Spots!.Items.Argmin(spot => Math.Abs(spot.MasterAlignmentID - id)),
                target.DefaultIfNull(t => (double)t.MasterAlignmentID),
                "ID",
                ((ISpotFocus, Func<AlignmentSpotPropertyModel, double>))(timeSpotFocus, spot => spot.TimesCenter),
                ((ISpotFocus, Func<AlignmentSpotPropertyModel, double>))(mzSpotFocus, spot => spot.MassCenter)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, timeSpotFocus, mzSpotFocus);

            MultivariateAnalysisSettingModel = new MultivariateAnalysisSettingModel(parameter, spotsSource.Spots.Items, evaluator, files, classBrush).AddTo(Disposables);

            MsfinderParameterSetting = MsfinderParameterSetting.CreateSetting(_projectParameter);
        }

        public override AlignmentSpotSource AlignmentSpotSource { get; }
        public AlignmentPeakPlotModel PlotModel { get; }
        public GcgcAlignmentPeakPlotModel GcgcPlotModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }
        public BarChartModel BarChartModel { get; }
        public InternalStandardSetModel InternalStandardSetModel { get; }
        public NormalizationSetModel NormalizationSetModel { get; }
        public MultivariateAnalysisSettingModel MultivariateAnalysisSettingModel { get; }
        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel MoleculeStructureModel { get; }
        public AlignmentEicModel AlignmentEicModel { get; }
        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public AlignmentMs2SpectrumModel MsSpectrumModel { get; }
        public GcmsAlignmentSpotTableModel AlignmentSpotTableModel { get; }
        public FocusNavigatorModel FocusNavigatorModel { get; }
        public UndoManager UndoManager { get; }
        public MsfinderParameterSetting MsfinderParameterSetting { get; }

        public IObservable<bool> CanSetUnknown => _target.Select(t => !(t is null));
        public void SetUnknown() => _target.Value?.SetUnknown(UndoManager);

        public CompoundSearchModel<PeakSpotModel>? CreateCompoundSearchModel() {
            if (!(_target.Value is AlignmentSpotPropertyModel spot && _msdecResult.Value is MSDecResult scan)) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }
            var plotService = new PlotComparedMsSpectrumUsecase(scan);
            var compoundSearch = new CompoundSearchModel<PeakSpotModel>(
                _fileCollection.FindByID(spot.RepresentativeFileID),
                new PeakSpotModel(spot, scan),
                new GcmsAlignmentCompoundSearchUsecase(_calculateMatchScore),
                plotService,
                new SetAnnotationUsecase(spot, spot.MatchResultsModel, UndoManager));
            compoundSearch.Disposables.Add(plotService);
            return compoundSearch;
        }

        public override void InvokeMoleculerNetworkingForTargetSpot() {
            throw new NotImplementedException();
        }

        public override void InvokeMsfinder() {
            if (!(_target.Value is AlignmentSpotPropertyModel spot && _msdecResult.Value is MSDecResult scan)) {
                return;
            }
            MsDialToExternalApps.SendToMsFinderProgramForGcms(_alignmentFileModel, spot.innerModel, scan, _projectParameter);
        }

        public InternalMsFinderSingleSpot? CreateSingleSearchMsfinderModel() {
            if (_target.Value is not AlignmentSpotPropertyModel spot || _msdecResult.Value is not MSDecResult result || result.Spectrum.IsEmptyOrNull()) {
                _broker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return null;
            }
            return _msfinderSearcherFactory.CreateModelForAlignmentSpot(MsfinderParameterSetting, spot, result, UndoManager);
        }

        public override void SearchFragment() {
            throw new NotImplementedException();
        }
    }
}
