using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Export;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Export;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcms
{
    internal sealed class LcmsMethodModel : MethodModelBase
    {
        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> CHROMATOGRAM_SPOT_SERIALIZER;

        static LcmsMethodModel() {
            CHROMATOGRAM_SPOT_SERIALIZER = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT);
        }

        private readonly IDataProviderFactory<AnalysisFileBean> _providerFactory;
        private readonly ProjectBaseParameterModel _projectBaseParameter;
        private readonly IMessageBroker _broker;
        private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;
        private readonly FacadeMatchResultEvaluator _matchResultEvaluator;
        private IAnnotationProcess _annotationProcess;

        public LcmsMethodModel(
            AnalysisFileBeanModelCollection analysisFileBeanModelCollection,
            IMsdialDataStorage<MsdialLcmsParameter> storage,
            IDataProviderFactory<AnalysisFileBean> providerFactory,
            ProjectBaseParameterModel projectBaseParameter, 
            IMessageBroker broker)
            : base(analysisFileBeanModelCollection, storage.AlignmentFiles, projectBaseParameter) {

            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
            _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
            _projectBaseParameter = projectBaseParameter ?? throw new ArgumentNullException(nameof(projectBaseParameter));
            _broker = broker;
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);
            CanShowProteinGroupTable = Observable.Return(storage.Parameter.TargetOmics == TargetOmics.Proteomics);

            List<AnalysisFileBean> analysisFiles = analysisFileBeanModelCollection.AnalysisFiles.Select(f => f.File).ToList();
            var stats = new List<StatsValue> { StatsValue.Average, StatsValue.Stdev, };
            var  metadataAccessor = storage.Parameter.TargetOmics == TargetOmics.Proteomics
                ? (IMetadataAccessor)new LcmsProteomicsMetadataAccessor(storage.DataBaseMapper, storage.Parameter)
                : (IMetadataAccessor)new LcmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter);
            var peakGroup = new AlignmentExportGroupModel(
                "Peaks",
                new ExportMethod(
                    analysisFiles,
                    new ExportFormat("txt", "txt", new AlignmentCSVExporter()),
                    new ExportFormat("csv", "csv", new AlignmentCSVExporter(separator: ","))
                ),
                new[]
                {
                    new ExportType("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", storage.Parameter), "Height", stats, true),
                    new ExportType("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", storage.Parameter), "Area", stats),
                    new ExportType("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", storage.Parameter), "NormalizedHeight", stats),
                    new ExportType("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", storage.Parameter), "NormalizedArea", stats),
                    new ExportType("Peak ID", metadataAccessor, new LegacyQuantValueAccessor("ID", storage.Parameter), "PeakID"),
                    new ExportType("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", storage.Parameter), "Mz"),
                    new ExportType("Retention time", metadataAccessor, new LegacyQuantValueAccessor("RT", storage.Parameter), "Rt"),
                    new ExportType("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", storage.Parameter), "SN"),
                    new ExportType("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", storage.Parameter), "MsmsIncluded")
                },
                new[]
                {
                    ExportspectraType.deconvoluted,
                });
            var spectraGroup = new AlignmentSpectraExportGroupModel(
                new AlignmentMspExporter(storage.DataBaseMapper, storage.Parameter),
                new[]
                {
                    ExportspectraType.deconvoluted,
                });
            var exportGroups = new List<IAlignmentResultExportModel> { peakGroup, spectraGroup, };
            if (storage.Parameter.TargetOmics == TargetOmics.Proteomics) {
                exportGroups.Add(new ProteinGroupExportModel(new ProteinGroupExporter(), analysisFiles));
            }

            AlignmentResultExportModel = new AlignmentResultExportModel(exportGroups, AlignmentFile, storage.AlignmentFiles);
            this.ObserveProperty(m => m.AlignmentFile)
                .Subscribe(file => AlignmentResultExportModel.AlignmentFile = file)
                .AddTo(Disposables);
        }

        public PeakFilterModel PeakFilterModel { get; }

        public IObservable<bool> CanShowProteinGroupTable { get; }

        public LcmsAnalysisModel AnalysisModel {
            get => _analysisModel;
            private set => SetProperty(ref _analysisModel, value);
        }
        private LcmsAnalysisModel _analysisModel;

        public LcmsAlignmentModel AlignmentModel {
            get => _alignmentModel;
            set => SetProperty(ref _alignmentModel, value);
        }

        private LcmsAlignmentModel _alignmentModel;

        public AlignmentResultExportModel AlignmentResultExportModel { get; }

        protected override IAnalysisModel LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            var provider = _providerFactory.Create(analysisFile.File);
            return AnalysisModel = new LcmsAnalysisModel(
                analysisFile,
                provider,
                _storage.DataBases,
                _storage.DataBaseMapper,
                _matchResultEvaluator,
                _storage.Parameter,
                PeakFilterModel)
            .AddTo(Disposables);
        }

        protected override IAlignmentModel LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }

            return AlignmentModel = new LcmsAlignmentModel(
                alignmentFile,
                _matchResultEvaluator,
                _storage.DataBases,
                PeakFilterModel,
                _storage.DataBaseMapper,
                _storage.Parameter,
                _projectBaseParameter,
                _storage.AnalysisFiles,
                AnalysisFileModelCollection,
                _broker)
            .AddTo(Disposables);
        }

        public override async Task RunAsync(ProcessOption option, CancellationToken token) {
            // Set analysis param
            var parameter = _storage.Parameter;
            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                _annotationProcess = BuildProteoMetabolomicsAnnotationProcess(_storage.DataBases, parameter);
            }
            else if(parameter.TargetOmics == TargetOmics.Lipidomics && (parameter.CollistionType == CollisionType.EIEIO || parameter.CollistionType == CollisionType.OAD)) {
                _annotationProcess = BuildEadLipidomicsAnnotationProcess(_storage.DataBases, _storage.DataBaseMapper, parameter);
            }
            else {
                _annotationProcess = BuildAnnotationProcess(_storage.DataBases, parameter.PeakPickBaseParam);
            }

            var processOption = option;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification | ProcessOption.PeakSpotting)) {
                if (!ProcessPickAndAnnotaion(_storage))
                    return;
            }
            else if (processOption.HasFlag(ProcessOption.Identification)) {
                if (!ProcessAnnotaion(_storage))
                    return;
            }

            // Run second process
            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                if (!ProcessSeccondAnnotaion4ShotgunProteomics(_storage))
                    return;
            } 
            
            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!ProcessAlignment(_storage))
                    return;
            }

            await LoadAnalysisFileAsync(AnalysisFileModelCollection.AnalysisFiles.FirstOrDefault(), token).ConfigureAwait(false);

#if DEBUG
            Console.WriteLine(string.Join("\n", _storage.Parameter.ParametersAsText()));
#endif
        }

        private IAnnotationProcess BuildAnnotationProcess(DataBaseStorage storage, PeakPickBaseParameter parameter) {
            var containerPairs = new List<(IAnnotationQueryFactory<IAnnotationQuery>, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>)>();
            foreach (var annotators in storage.MetabolomicsDataBases) {
                containerPairs.AddRange(annotators.Pairs.Select(annotator => (new AnnotationQueryFactory(annotator.SerializableAnnotator, parameter) as IAnnotationQueryFactory<IAnnotationQuery>, annotator.ConvertToAnnotatorContainer())));
            }
            return new StandardAnnotationProcess<IAnnotationQuery>(containerPairs);
        }

        private IAnnotationProcess BuildProteoMetabolomicsAnnotationProcess(DataBaseStorage storage, ParameterBase parameter) {
            var containers = new List<IAnnotatorContainer<IPepAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();
            foreach (var annotators in storage.MetabolomicsDataBases) {
                containers.AddRange(annotators.Pairs.Select(annotator => annotator.ConvertToAnnotatorContainer()));
            }
            var pepContainers = new List<IAnnotatorContainer<IPepAnnotationQuery, PeptideMsReference, MsScanMatchResult>>();
            foreach (var annotators in storage.ProteomicsDataBases) {
                pepContainers.AddRange(annotators.Pairs.Select(annotator => annotator.ConvertToAnnotatorContainer()));
            }
            return new AnnotationProcessOfProteoMetabolomics<IPepAnnotationQuery>(
                containers.Select(container => (
                    (IAnnotationQueryFactory<IPepAnnotationQuery>)new PepAnnotationQueryFactory(container.Annotator, parameter.PeakPickBaseParam, parameter.ProteomicsParam),
                    container
                )).ToList(),
                pepContainers.Select(container => (
                    (IAnnotationQueryFactory<IPepAnnotationQuery>)new PepAnnotationQueryFactory(container.Annotator, parameter.PeakPickBaseParam, parameter.ProteomicsParam),
                    container
                )).ToList());
        }

        private IAnnotationProcess BuildEadLipidomicsAnnotationProcess(DataBaseStorage storage, DataBaseMapper mapper, ParameterBase parameter) {
            var containerPairs = new List<(IAnnotationQueryFactory<IAnnotationQuery>, IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>)>();
            foreach (var annotators in storage.MetabolomicsDataBases) {
                containerPairs.AddRange(annotators.Pairs.Select(annotator => (new AnnotationQueryFactory(annotator.SerializableAnnotator, parameter.PeakPickBaseParam) as IAnnotationQueryFactory<IAnnotationQuery>, annotator.ConvertToAnnotatorContainer())));
            }
            var eadAnnotationQueryFactoryTriple = new List<(IAnnotationQueryFactory<ICallableAnnotationQuery<MsScanMatchResult>>, IMatchResultEvaluator<MsScanMatchResult>, MsRefSearchParameterBase)>();
            foreach (var annotators in storage.EadLipidomicsDatabases) {
                eadAnnotationQueryFactoryTriple.AddRange(annotators.Pairs.Select(annotator => (new AnnotationQueryWithReferenceFactory(mapper, annotator.SerializableAnnotator, parameter.PeakPickBaseParam) as IAnnotationQueryFactory<ICallableAnnotationQuery<MsScanMatchResult>>, annotator.SerializableAnnotator as IMatchResultEvaluator<MsScanMatchResult>, annotator.SearchParameter)));
            }
            return new EadLipidomicsAnnotationProcess<IAnnotationQuery>(containerPairs, eadAnnotationQueryFactoryTriple, mapper);
        }

        public bool ProcessPickAndAnnotaion(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var request = new ProgressBarMultiContainerRequest(
                vm_ =>
                {
                    var processor = new MsdialLcMsApi.Process.FileProcess(_providerFactory, storage, _annotationProcess, _matchResultEvaluator);
                    return processor.RunAllAsync(
                        storage.AnalysisFiles,
                        vm_.ProgressBarVMs.Select(pbvm => (Action<int>)((int v) => pbvm.CurrentValue = v)),
                        Math.Max(1, storage.Parameter.ProcessBaseParam.UsableNumThreads / 2),
                        vm_.Increment);
                },
                storage.AnalysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        public bool ProcessAnnotaion(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var request = new ProgressBarMultiContainerRequest(
                vm_ =>
                {
                    var processor = new MsdialLcMsApi.Process.FileProcess(_providerFactory, storage, _annotationProcess, _matchResultEvaluator);
                    return processor.AnnotateAllAsync(
                        storage.AnalysisFiles,
                        vm_.ProgressBarVMs.Select(pbvm => (Action<int>)((int v) => pbvm.CurrentValue = v)),
                        Math.Max(1, storage.Parameter.ProcessBaseParam.UsableNumThreads / 2),
                        vm_.Increment);
                },
                storage.AnalysisFiles.Select(file => file.AnalysisFileName).ToArray());
            _broker.Publish(request);
            return request.Result ?? false;
        }

        public bool ProcessSeccondAnnotaion4ShotgunProteomics(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var request = new ProgressBarRequest("Process second annotation..", isIndeterminate: false,
                async vm =>
                {
                    var proteomicsAnnotator = new ProteomeDataAnnotator();
                    await Task.Run(() => proteomicsAnnotator.ExecuteSecondRoundAnnotationProcess(
                        storage.AnalysisFiles,
                        storage.DataBaseMapper,
                        _matchResultEvaluator,
                        storage.DataBases,
                        storage.Parameter,
                        v => vm.CurrentValue = v)).ConfigureAwait(false);
                });
            _broker.Publish(request);
            return request.Result ?? false;
        }

        public bool ProcessAlignment(IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var request = new ProgressBarRequest("Process alignment..", isIndeterminate: false,
                async vm =>
                {
                    var factory = new LcmsAlignmentProcessFactory(storage, _matchResultEvaluator)
                    {
                        ReportAction = v => vm.CurrentValue = v
                    };

                    var aligner = factory.CreatePeakAligner();
                    aligner.ProviderFactory = _providerFactory; // TODO: I'll remove this later.

                    var alignmentFile = storage.AlignmentFiles.Last();
                    var result = await Task.Run(() => aligner.Alignment(storage.AnalysisFiles, alignmentFile, CHROMATOGRAM_SPOT_SERIALIZER)).ConfigureAwait(false);

                    if (storage.DataBases.ProteomicsDataBases.Any()) {
                        new ProteomeDataAnnotator().MappingToProteinDatabase(
                            alignmentFile.ProteinAssembledResultFilePath,
                            result,
                            storage.DataBases.ProteomicsDataBases,
                            storage.DataBaseMapper,
                            _matchResultEvaluator,
                            storage.Parameter);
                    }

                    result.Save(alignmentFile);
                    MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result?.AlignmentSpotProperties).ToList());
                });
            _broker.Publish(request);
            return request.Result ?? false;
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialLcmsParameter> storage, IReadOnlyList<AlignmentSpotProperty> spots) {
            var files = storage.AnalysisFiles;

            var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
            foreach (var file in files) {
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
                pointerss.Add((version, pointers, isAnnotationInfo));
            }

            var streams = new List<System.IO.FileStream>();
            try {
                streams = files.Select(file => System.IO.File.OpenRead(file.DeconvolutionFilePath)).ToList();
                foreach (var spot in spots.OrEmptyIfNull()) {
                    var repID = spot.RepresentativeFileID;
                    var peakID = spot.AlignedPeakProperties[repID].GetMSDecResultID();

                    Console.WriteLine("RepID {0}, Peak ID {1}", repID, peakID);

                    var decResult = MsdecResultsReader.ReadMSDecResult(
                        streams[repID], pointerss[repID].pointers[peakID],
                        pointerss[repID].version, pointerss[repID].isAnnotationInfo);
                    yield return decResult;
                }
            }
            finally {
                streams.ForEach(stream => stream.Close());
            }
        }

        public AnalysisResultExportModel ExportAnalysis() {
            var container = _storage;
            var spectraTypes = new List<SpectraType>
            {
                new SpectraType(
                    ExportspectraType.deconvoluted,
                    new LcmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.deconvoluted)),
                new SpectraType(
                    ExportspectraType.centroid,
                    new LcmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.centroid)),
                new SpectraType(
                    ExportspectraType.profile,
                    new LcmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.profile)),
            };
            var spectraFormats = new List<SpectraFormat>
            {
                new SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
            };

            return new AnalysisResultExportModel(container.AnalysisFiles, spectraTypes, spectraFormats, _providerFactory);
        }

        public ChromatogramsModel ShowTIC() {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }

            var tic = analysisModel.EicLoader.LoadTic();
            var chromatogram = new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC");
            return new ChromatogramsModel("Total ion chromatogram", chromatogram, "Total ion chromatogram", "Retention time", "Absolute ion abundance");
        }

        public ChromatogramsModel ShowBPC() {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }
            var bpc = analysisModel.EicLoader.LoadBpc();
            var chromatogram = new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC");
            return new ChromatogramsModel("Base peak chromatogram", chromatogram, "Base peak chromatogram", "Retention time", "Absolute ion abundance");
        }

        public DisplayEicSettingModel ShowEIC() {
            if (AnalysisModel is null) {
                return null;
            }
            return new DisplayEicSettingModel(AnalysisModel.EicLoader, _storage.Parameter);
        }

        public ChromatogramsModel ShowTicBpcRepEIC() {
            var container = _storage;
            var analysisModel = AnalysisModel;
            if (analysisModel is null) {
                return null;
            }

            var tic = analysisModel.EicLoader.LoadTic();
            var bpc = analysisModel.EicLoader.LoadBpc();
            var eic = analysisModel.EicLoader.LoadHighestEicTrace(analysisModel.Ms1Peaks.ToList());

            var maxPeakMz = analysisModel.Ms1Peaks.Argmax(n => n.Intensity).Mass;

            var displayChroms = new List<DisplayChromatogram>() {
                new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"),
                new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"),
                new DisplayChromatogram(eic, new Pen(Brushes.Blue, 1.0), "EIC of m/z " + Math.Round(maxPeakMz, 5).ToString())
            };

            return new ChromatogramsModel("TIC, BPC, and highest peak m/z's EIC", displayChroms, "TIC, BPC, and highest peak m/z's EIC", "Retention time [min]", "Absolute ion abundance");
        }

        public FragmentQuerySettingModel ShowShowFragmentSearchSettingView() {
            return new FragmentQuerySettingModel(_storage.Parameter.AdvancedProcessOptionBaseParam, AnalysisModel, AlignmentModel);
        }

        public MassqlSettingModel ShowShowMassqlSearchSettingView(IResultModel model) {
            if (model is null) {
                return null;
            }
            return new MassqlSettingModel(model, _storage.Parameter.AdvancedProcessOptionBaseParam);
        }

        public MscleanrSettingModel ShowShowMscleanrFilterSettingView() {
            if (AlignmentModel is null) {
                return null;
            }
            return new MscleanrSettingModel(_storage.Parameter, AlignmentModel.Ms1Spots);
        }
    }
}
