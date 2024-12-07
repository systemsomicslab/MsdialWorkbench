using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.ExternalApp;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Information;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Model.Statistics;
using CompMs.App.Msdial.Model.Table;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Algorithm.Function;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Graphics.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
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
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcms
{
    internal sealed class LcmsAlignmentModel : AlignmentModelBase
    {
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;
        private static readonly double RT_TOL = 0.5;
        private static readonly double MZ_TOL = 20;

        static LcmsAlignmentModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT)!;
        }

        private readonly AlignmentFileBeanModel _alignmentFile;
        private readonly DataBaseMapper _dataBaseMapper;
        private readonly FilePropertiesModel _projectBaseParameter;
        private readonly List<AnalysisFileBean> _files;
        private readonly CompoundSearcherCollection _compoundSearchers;
        private readonly UndoManager _undoManager;
        private readonly ReadOnlyReactivePropertySlim<MSDecResult?> _msdecResult;
        private readonly IMessageBroker _messageBroker;
        private readonly ReactiveProperty<BarItemsLoaderData> _barItemsLoaderDataProperty;
        private readonly ParameterBase _parameter;
        private readonly PeakSpotFiltering<AlignmentSpotPropertyModel>.PeakSpotFilter _filter;
        private readonly MsfinderSearcherFactory _msfinderSearcherFactory;

        public LcmsAlignmentModel(
            AlignmentFileBeanModel alignmentFileBean,
            IMatchResultEvaluator<MsScanMatchResult> evaluator,
            DataBaseStorage databases,
            PeakSpotFiltering<AlignmentSpotPropertyModel> peakSpotFiltering,
            PeakFilterModel peakFilterModel,
            DataBaseMapper mapper,
            MsdialLcmsParameter parameter,
            FilePropertiesModel projectBaseParameter,
            List<AnalysisFileBean> files,
            AnalysisFileBeanModelCollection fileCollection,
            MsfinderSearcherFactory msfinderSearcherFactory,
            IMessageBroker messageBroker)
            : base(alignmentFileBean, peakSpotFiltering, peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), messageBroker) {
            if (evaluator is null) {
                throw new ArgumentNullException(nameof(evaluator));
            }

            if (databases is null) {
                throw new ArgumentNullException(nameof(databases));
            }

            if (projectBaseParameter is null) {
                throw new ArgumentNullException(nameof(projectBaseParameter));
            }

            _alignmentFile = alignmentFileBean;
            _parameter = parameter;
            _projectBaseParameter = projectBaseParameter;
            _msfinderSearcherFactory = msfinderSearcherFactory;
            _files = files ?? throw new ArgumentNullException(nameof(files));
            _dataBaseMapper = mapper;
            _compoundSearchers = CompoundSearcherCollection.BuildSearchers(databases, mapper);
            _undoManager = new UndoManager().AddTo(Disposables);
            _messageBroker = messageBroker;

            var spotsSource = new AlignmentSpotSource(alignmentFileBean, Container, CHROMATOGRAM_SPOT_SERIALIZER).AddTo(Disposables);
            AlignmentSpotSource = spotsSource;
            Ms1Spots = spotsSource.Spots!.Items;
            Target = new ReactivePropertySlim<AlignmentSpotPropertyModel?>().AddTo(Disposables);
            CurrentRepresentativeFile = Target.Select(t => t is null ? null : fileCollection.FindByID(t.RepresentativeFileID)).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            InternalStandardSetModel = new InternalStandardSetModel(spotsSource.Spots!.Items, TargetMsMethod.Lcms).AddTo(Disposables);
            NormalizationSetModel = new NormalizationSetModel(Container, files, fileCollection, mapper, evaluator, InternalStandardSetModel, parameter, messageBroker).AddTo(Disposables);

            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                var proteinResultContainer = alignmentFileBean.LoadProteinResult();
                var proteinResultContainerModel = new ProteinResultContainerModel(proteinResultContainer, Ms1Spots, Target);
                ProteinResultContainerModel = proteinResultContainerModel;
            }

            _msdecResult = Target
                .DefaultIfNull(t => Observable.FromAsync(() => _alignmentFile.LoadMSDecResultByIndexAsync(t.MasterAlignmentID)), Observable.Return<MSDecResult?>(null))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            var filterRegistrationManager = new FilterRegistrationManager<AlignmentSpotPropertyModel>(Ms1Spots, peakSpotFiltering).AddTo(Disposables);
            PeakSpotNavigatorModel = filterRegistrationManager.PeakSpotNavigatorModel;
            filterRegistrationManager.AttachFilter(Ms1Spots, peakFilterModel, evaluator.Contramap<AlignmentSpotPropertyModel, MsScanMatchResult>(filterable => filterable.ScanMatchResult, (e, f) => f.IsRefMatched(e), (e, f) => f.IsSuggested(e)), status: FilterEnableStatus.All & ~FilterEnableStatus.Dt);

            // Peak scatter plot
            var brushMapDataSelector = BrushMapDataSelectorFactory.CreateAlignmentSpotBrushes(parameter.TargetOmics);
            var labelSource = PeakSpotNavigatorModel.ObserveProperty(m => m.SelectedAnnotationLabel)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            PlotModel = new AlignmentPeakPlotModel(spotsSource, spot => spot.TimesCenter, spot => spot.MassCenter, Target, labelSource, brushMapDataSelector.SelectedBrush, brushMapDataSelector.Brushes, PeakLinkModel.Build(spotsSource.Spots.Items, spotsSource.Spots.Items.Select(p => p.innerModel.PeakCharacter).ToList()))
            {
                GraphTitle = ((IFileBean)_alignmentFile).FileName,
                HorizontalProperty = nameof(AlignmentSpotPropertyModel.TimesCenter),
                VerticalProperty = nameof(AlignmentSpotPropertyModel.MassCenter),
                HorizontalTitle = "Retention time [min]",
                VerticalTitle = "m/z",
            }.AddTo(Disposables);
            var mrmprobsExporter = new EsiMrmprobsExporter(evaluator, mapper);
            var usecase = new AlignmentSpotExportMrmprobsUsecase(parameter.MrmprobsExportBaseParam, spotsSource, alignmentFileBean, _compoundSearchers, mrmprobsExporter, Target, messageBroker);
            PlotModel.ExportMrmprobs = usecase;

            // Ms2 spectrum
            MatchResultCandidatesModel = new MatchResultCandidatesModel(Target.Select(t => t?.MatchResultsModel), mapper).AddTo(Disposables);
            var refLoader = (parameter.ProjectParam.TargetOmics == TargetOmics.Proteomics)
                ? (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<PeptideMsReference?>(mapper)
                : (IMsSpectrumLoader<MsScanMatchResult>)new ReferenceSpectrumLoader<MoleculeMsReference?>(mapper);
            IMsSpectrumLoader<AlignmentSpotPropertyModel> msDecSpectrumLoader = new AlignmentMSDecSpectrumLoader(_alignmentFile);
            var spectraExporter = new NistSpectraExporter<AlignmentSpotProperty?>(Target.Select(t => t?.innerModel), mapper, parameter).AddTo(Disposables);
            GraphLabels ms2GraphLabels = new GraphLabels("Representative vs. Reference", "m/z", "Relative abundance", nameof(SpectrumPeak.Mass), nameof(SpectrumPeak.Intensity));
            ChartHueItem deconvolutedSpectrumHueItem = new ChartHueItem(projectBaseParameter, Colors.Blue);
            ObservableMsSpectrum deconvolutedObservableMsSpectrum = ObservableMsSpectrum.Create(Target, msDecSpectrumLoader, spectraExporter).AddTo(Disposables);
            var referenceExporter = new MoleculeMsReferenceExporter(MatchResultCandidatesModel.RetryRefer<MoleculeMsReference?>(mapper)).AddTo(Disposables);
            AlignmentSpotSpectraLoader spectraLoader = new AlignmentSpotSpectraLoader(fileCollection, refLoader, _compoundSearchers, fileCollection);
            Ms2SpectrumModel = new AlignmentMs2SpectrumModel(
                Target, MatchResultCandidatesModel.SelectedCandidate.Select(rr => rr?.MatchResult), fileCollection,
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
            var peakSpotAxisLabelAsObservable = Target.SelectSwitch(t => t?.ObserveProperty(t_ => t_.IonAbundanceUnit).Select(t_ => t_.ToLabel()) ?? Observable.Return(string.Empty)).ToReadOnlyReactivePropertySlim(string.Empty).AddTo(Disposables);
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
            BarChartModel = new BarChartModel(Target, barItemsLoaderDataProperty, barItemLoaderDatas, barBrush, projectBaseParameter, fileCollection, projectBaseParameter.ClassProperties).AddTo(Disposables);

            // Class eic
            var fileIdToFileName = files.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileName);
            AlignmentEicModel = AlignmentEicModel.Create(
                Target,
                alignmentFileBean.CreateEicLoader(CHROMATOGRAM_SPOT_SERIALIZER, fileCollection, projectBaseParameter).AddTo(Disposables),
                files, parameter,
                peak => peak.Time,
                peak => peak.Intensity).AddTo(Disposables);
            AlignmentEicModel.Elements.GraphTitle = "TIC, EIC, or BPC chromatograms";
            AlignmentEicModel.Elements.HorizontalTitle = "Retention time [min]";
            AlignmentEicModel.Elements.VerticalTitle = "Abundance";
            AlignmentEicModel.Elements.HorizontalProperty = nameof(PeakItem.Time);
            AlignmentEicModel.Elements.VerticalProperty = nameof(PeakItem.Intensity);

            var barItemsLoaderProperty = barItemsLoaderDataProperty.SkipNull().Select(data => data.Loader);
            _filter = peakSpotFiltering.CreateFilter(peakFilterModel, evaluator.Contramap((AlignmentSpotPropertyModel spot) => spot.ScanMatchResult), FilterEnableStatus.All);
            AlignmentSpotTableModel = new LcmsAlignmentSpotTableModel(Ms1Spots, Target, barBrush, projectBaseParameter.ClassProperties, barItemsLoaderProperty, parameter.ProjectParam.TargetOmics, _filter, spectraLoader, _undoManager).AddTo(Disposables);

            CanSearchCompound = new[]
            {
                Target.Select(t => t?.innerModel is null),
                _msdecResult.Select(r => r is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            var rtSpotFocus = new ChromSpotFocus(PlotModel.HorizontalAxis, RT_TOL, Target.Select(t => t?.TimesCenter ?? 0d), "F2", "RT(min)", isItalic: false).AddTo(Disposables);
            var mzSpotFocus = new ChromSpotFocus(PlotModel.VerticalAxis, MZ_TOL, Target.Select(t => t?.MassCenter ?? 0d), "F3", "m/z", isItalic: true).AddTo(Disposables);
            var idSpotFocus = new IdSpotFocus<AlignmentSpotPropertyModel>(
                Target,
                id => Ms1Spots.Argmin(spot => Math.Abs(spot.MasterAlignmentID - id)),
                Target.Select(t => t?.MasterAlignmentID ?? 0d),
                "ID",
                ((ISpotFocus, Func<AlignmentSpotPropertyModel, double>))(rtSpotFocus, spot => spot.TimesCenter),
                ((ISpotFocus, Func<AlignmentSpotPropertyModel, double>))(mzSpotFocus, spot => spot.MassCenter)).AddTo(Disposables);
            FocusNavigatorModel = new FocusNavigatorModel(idSpotFocus, rtSpotFocus, mzSpotFocus);

            var peakInformationModel = new PeakInformationAlignmentModel(Target).AddTo(Disposables);
            peakInformationModel.Add(
                t => new RtPoint(t?.innerModel.TimesCenter.RT.Value ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.ChromXs.RT.Value),
                t => new MzPoint(t?.MassCenter ?? 0d, t.Refer<MoleculeMsReference>(mapper)?.PrecursorMz));
            peakInformationModel.Add(t => new HeightAmount(t?.HeightAverage ?? 0d));
            PeakInformationModel = peakInformationModel;

            var compoundDetailModel = new CompoundDetailModel(Target.SkipNull().SelectSwitch(t => t.ObserveProperty(p => p.ScanMatchResult)).Publish().RefCount(), mapper).AddTo(Disposables);
            compoundDetailModel.Add(
                r_ => new MzSimilarity(r_?.AcurateMassSimilarity ?? 0d),
                r_ => new RtSimilarity(r_?.RtSimilarity ?? 0d),
                r_ => new SpectrumSimilarity(r_?.WeightedDotProduct ?? 0d, r_?.ReverseDotProduct ?? 0d));
            CompoundDetailModel = compoundDetailModel;

            if (parameter.ProjectParam.TargetOmics != TargetOmics.Proteomics) {
                var moleculeStructureModel = new MoleculeStructureModel().AddTo(Disposables);
                MoleculeStructureModel = moleculeStructureModel;
                Target.Subscribe(t => moleculeStructureModel.UpdateMolecule(t?.innerModel)).AddTo(Disposables);
            }

            MultivariateAnalysisSettingModel = new MultivariateAnalysisSettingModel(parameter, Ms1Spots, evaluator, files, classBrush).AddTo(Disposables);

            FindTargetCompoundSpotModel = new FindTargetCompoundsSpotModel(spotsSource.Spots.Items, Target, messageBroker).AddTo(Disposables);
            MsfinderParameterSetting = MsfinderParameterSetting.CreateSetting(parameter.ProjectParam);
        }

        public UndoManager UndoManager => _undoManager;

        public ReadOnlyObservableCollection<AlignmentSpotPropertyModel> Ms1Spots { get; }
        public ReactivePropertySlim<AlignmentSpotPropertyModel?> Target { get; }
        public ReadOnlyReactivePropertySlim<AnalysisFileBeanModel?> CurrentRepresentativeFile { get; }
        public InternalStandardSetModel InternalStandardSetModel { get; }
        public PeakSpotNavigatorModel PeakSpotNavigatorModel { get; }
        public FocusNavigatorModel FocusNavigatorModel { get; }
        public AlignmentPeakPlotModel PlotModel { get; }
        public AlignmentMs2SpectrumModel Ms2SpectrumModel { get; }
        public BarChartModel BarChartModel { get; }
        public AlignmentEicModel AlignmentEicModel { get; }
        public LcmsAlignmentSpotTableModel AlignmentSpotTableModel { get; }
        public NormalizationSetModel NormalizationSetModel { get; }
        public MultivariateAnalysisSettingModel MultivariateAnalysisSettingModel { get; }
        public FindTargetCompoundsSpotModel FindTargetCompoundSpotModel { get; }
        public ReadOnlyReactivePropertySlim<bool> CanSearchCompound { get; }
        public PeakInformationAlignmentModel PeakInformationModel { get; }
        public CompoundDetailModel CompoundDetailModel { get; }
        public MoleculeStructureModel? MoleculeStructureModel { get; }
        public MatchResultCandidatesModel MatchResultCandidatesModel { get; }
        public ProteinResultContainerModel? ProteinResultContainerModel { get; }
        public override AlignmentSpotSource AlignmentSpotSource { get; }
        public MsfinderParameterSetting MsfinderParameterSetting { get; }

        public IObservable<bool> CanSetUnknown => Target.Select(t => !(t is null));
        public void SetUnknown() => Target.Value?.SetUnknown(_undoManager);

        public CompoundSearchModel<PeakSpotModel>? CreateCompoundSearchModel() {
            if (Target.Value is not AlignmentSpotPropertyModel spot || _msdecResult.Value is not MSDecResult decResult) {
                _messageBroker.Publish(new ShortMessageRequest(MessageHelper.NoPeakSelected));
                return null;
            }
            PlotComparedMsSpectrumUsecase plotService = new PlotComparedMsSpectrumUsecase(decResult);
            var compoundSearch =  new CompoundSearchModel<PeakSpotModel>(
                _files[spot.RepresentativeFileID],
                new PeakSpotModel(spot, _msdecResult.Value),
                new LcmsCompoundSearchUsecase(_compoundSearchers.Items),
                plotService,
                new SetAnnotationUsecase(spot, spot.MatchResultsModel, _undoManager));
            compoundSearch.Disposables.Add(plotService);
            return compoundSearch;
        }

        public void SaveSpectra(string filename) {
            if (Target.Value is not AlignmentSpotPropertyModel spot || _msdecResult.Value is not MSDecResult decResult) {
                _messageBroker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return;
            }
            using var file = File.Open(filename, FileMode.Create);
            SpectraExport.SaveSpectraTable(
                (ExportSpectraFileFormat)Enum.Parse(typeof(ExportSpectraFileFormat), Path.GetExtension(filename).Trim('.')),
                file,
                spot.innerModel,
                decResult,
                _dataBaseMapper,
                _parameter);
        }

        public bool CanSaveSpectra() => Target.Value?.innerModel != null && _msdecResult.Value != null;

        public override void SearchFragment() {
            using (var decLoader = _alignmentFile.CreateTemporaryMSDecLoader()) {
                FragmentSearcher.Search(Ms1Spots.Select(n => n.innerModel).ToList(), decLoader, _parameter);
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
                _messageBroker.Publish(new ShortMessageRequest(MessageHelper.SelectPeakBeforeExport));
                return null;
            }
            return _msfinderSearcherFactory.CreateModelForAlignmentSpot(MsfinderParameterSetting, spot, result, _undoManager);
        }

        private MolecularNetworkInstance GetMolecularNetworkInstance(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) {
            var param = _projectBaseParameter;
            var loaderProperty = _barItemsLoaderDataProperty.Value;
            var loader = loaderProperty.Loader;
            var publisher = new TaskProgressPublisher(_messageBroker, $"Exporting MN results in {parameter.ExportFolderPath}");

            using (publisher.Start()) {
                IReadOnlyList<AlignmentSpotPropertyModel> spots = Ms1Spots;
                if (useCurrentFiltering) {
                    spots = _filter.Filter(Ms1Spots).ToList();
                }
                var peaks = _alignmentFileModel.LoadMSDecResults();

                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Exporting MN results in {parameter.ExportFolderPath}");
                }
                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMolecularNetworkInstance(spots, peaks, query, notify);
                var rootObj = network.Root;
                for (int i = 0; i < rootObj.nodes.Count; i++) {
                    var node = rootObj.nodes[i];
                    node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(spots[i], loader, param.ClassProperties.ClassToColor);
                }
                var ionfeature_edges = MolecularNetworking.GenerateFeatureLinkedEdges(spots, spots.ToDictionary(s => s.MasterAlignmentID, s => s.innerModel.PeakCharacter));
                rootObj.edges.AddRange(ionfeature_edges);

                if (parameter.MnIsExportIonCorrelation && _alignmentFileModel.CountRawFiles >= 6) {
                    var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(spots.Select(n => n.innerModel).ToList(), parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode);
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
            var publisher = new TaskProgressPublisher(_messageBroker, $"Preparing MN results");

            using (publisher.Start()) {
                var spots = Ms1Spots;
                var peaks = _alignmentFileModel.LoadMSDecResults();
              
                var targetSpot = Target.Value;
                var targetPeak = peaks[targetSpot.MasterAlignmentID];

                void notify(double progressRate) {
                    publisher.Progress(progressRate, $"Preparing MN results");
                }
                var query = CytoscapejsModel.ConvertToMolecularNetworkingQuery(parameter);
                var builder = new MoleculerNetworkingBase();
                var network = builder.GetMoleculerNetworkInstanceForTargetSpot(targetSpot, targetPeak, spots, peaks, query, notify);
                var rootObj = network.Root;

                for (int i = 0; i < rootObj.nodes.Count; i++) {
                    var node = rootObj.nodes[i];
                    node.data.BarGraph = CytoscapejsModel.GetBarGraphProperty(spots[node.data.id], loader, param.ClassProperties.ClassToColor);
                }

                if (parameter.MnIsExportIonCorrelation && _alignmentFileModel.CountRawFiles >= 6) {
                    var ion_edges = MolecularNetworking.GenerateEdgesByIonValues(spots.Select(n => n.innerModel).ToList(), parameter.MnIonCorrelationSimilarityCutOff, parameter.MaxEdgeNumberPerNode);
                    rootObj.edges.AddRange(ion_edges.Where(e => e.data.source == targetSpot.MasterAlignmentID || e.data.target == targetSpot.MasterAlignmentID));
                }

                return network;
            }
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
    }
}
