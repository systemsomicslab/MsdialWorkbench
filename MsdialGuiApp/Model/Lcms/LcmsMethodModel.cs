using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Setting;
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
using System.Windows;
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
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
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

        public bool ProcessAnnotaion(IMsdialDataStorage<MsdialLcmsParameter> storage) {
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

                    if (!storage.DataBaseMapper.PeptideAnnotators.IsEmptyOrNull()) {
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
                    var peakID = spot.AlignedPeakProperties[repID].MSDecResultIdUsed;

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

        public void ExportAlignment(Window owner) {
            var container = _storage;
            var vm = new AlignmentResultExport2VM(AlignmentFile, container.AlignmentFiles, container, _broker);

            if (container.Parameter.TargetOmics == TargetOmics.Proteomics) {
                var metadataAccessor = new LcmsProteomicsMetadataAccessor(container.DataBaseMapper, container.Parameter);
                vm.ExportTypes.AddRange(
               new List<ExportType2>
               {
                    new ExportType2("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", container.Parameter), "Height", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType2("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", container.Parameter), "Area", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", container.Parameter), "NormalizedHeight", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", container.Parameter), "NormalizedArea", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Peak ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.Parameter), "PeakID"),
                    new ExportType2("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", container.Parameter), "Mz"),
                    new ExportType2("Retention time", metadataAccessor, new LegacyQuantValueAccessor("RT", container.Parameter), "Rt"),
                    new ExportType2("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", container.Parameter), "SN"),
                    new ExportType2("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", container.Parameter), "MsmsIncluded"),
                    new ExportType2("Protein assembled", metadataAccessor, new LegacyQuantValueAccessor("Protein", container.Parameter), "Protein"),

               });
            }
            else {
                var metadataAccessor = new LcmsMetadataAccessor(container.DataBaseMapper, container.Parameter);
                vm.ExportTypes.AddRange(
                new List<ExportType2>
                {
                    new ExportType2("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", container.Parameter), "Height", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType2("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", container.Parameter), "Area", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", container.Parameter), "NormalizedHeight", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", container.Parameter), "NormalizedArea", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Peak ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.Parameter), "PeakID"),
                    new ExportType2("Retention time", metadataAccessor, new LegacyQuantValueAccessor("RT", container.Parameter), "Rt"),
                    new ExportType2("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", container.Parameter), "Mz"),
                    new ExportType2("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", container.Parameter), "SN"),
                    new ExportType2("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", container.Parameter), "MsmsIncluded"),

                });
            }
            
            var dialog = new AlignmentResultExportWin
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            dialog.ShowDialog();
        }

        public void ExportAnalysis(Window owner) {
            var container = _storage;
            var spectraTypes = new List<Export.SpectraType>
            {
                new Export.SpectraType(
                    ExportspectraType.deconvoluted,
                    new LcmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.deconvoluted)),
                new Export.SpectraType(
                    ExportspectraType.centroid,
                    new LcmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.centroid)),
                new Export.SpectraType(
                    ExportspectraType.profile,
                    new LcmsAnalysisMetadataAccessor(container.DataBaseMapper, container.Parameter, ExportspectraType.profile)),
            };
            var spectraFormats = new List<Export.SpectraFormat>
            {
                new Export.SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
            };

            using (var vm = new AnalysisResultExportViewModel(
                container.AnalysisFiles, 
                spectraTypes, 
                spectraFormats, 
                _providerFactory)) {
                var dialog = new AnalysisResultExportWin
                {
                    DataContext = vm,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };

                dialog.ShowDialog();
            }
        }

        public void ShowTIC(Window owner) {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var tic = analysisModel.EicLoader.LoadTic();
            var vm = new ChromatogramsViewModel(
                new ChromatogramsModel("Total ion chromatogram", 
                new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"),
                "Total ion chromatogram", "Retention time", "Absolute ion abundance"));
            var view = new DisplayChromatogramsView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.Show();
        }

        public void ShowBPC(Window owner) {
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var bpc = analysisModel.EicLoader.LoadBpc();
            var vm = new ChromatogramsViewModel(
                new ChromatogramsModel("Base peak chromatogram",
                new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"),
                "Base peak chromatogram", "Retention time", "Absolute ion abundance"));
            var view = new DisplayChromatogramsView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.Show();
        }

        public void ShowEIC(Window owner) {
            var container = _storage;
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var param = container.Parameter;
            var model = new DisplayEicSettingModel(param);
            var dialog = new EICDisplaySettingView() {
                DataContext = new DisplayEicSettingViewModel(model),
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (dialog.ShowDialog() == true) {
                param.DiplayEicSettingValues = model.DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
                var displayEICs = param.DiplayEicSettingValues;
                if (!displayEICs.IsEmptyOrNull()) {
                    var displayChroms = new List<DisplayChromatogram>();
                    var counter = 0;
                    foreach (var set in displayEICs.Where(n => n.Mass > 0 && n.MassTolerance > 0)) {
                        var eic = analysisModel.EicLoader.LoadEicTrace(set.Mass, set.MassTolerance);
                        var subtitle = "[" + Math.Round(set.Mass - set.MassTolerance, 4).ToString() + "-" + Math.Round(set.Mass + set.MassTolerance, 4).ToString() + "]";
                        var chrom = new DisplayChromatogram(eic, new Pen(ChartBrushes.GetChartBrush(counter), 1.0), set.Title + "; " + subtitle);
                        counter++;
                        displayChroms.Add(chrom);
                    }
                    var vm = new ChromatogramsViewModel(new ChromatogramsModel("EIC", displayChroms, "EIC", "Retention time [min]", "Absolute ion abundance"));
                    var view = new DisplayChromatogramsView() {
                        DataContext = vm,
                        Owner = owner,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner
                    };
                    view.Show();
                }
            }
        }

        public void ShowTicBpcRepEIC(Window owner) {
            var container = _storage;
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;

            var tic = analysisModel.EicLoader.LoadTic();
            var bpc = analysisModel.EicLoader.LoadBpc();
            var eic = analysisModel.EicLoader.LoadHighestEicTrace(analysisModel.Ms1Peaks.ToList());

            var maxPeakMz = analysisModel.Ms1Peaks.Argmax(n => n.Intensity).Mass;


            var displayChroms = new List<DisplayChromatogram>() {
                new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC"),
                new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC"),
                new DisplayChromatogram(eic, new Pen(Brushes.Blue, 1.0), "EIC of m/z " + Math.Round(maxPeakMz, 5).ToString())
            };

            var vm = new ChromatogramsViewModel(new ChromatogramsModel("TIC, BPC, and highest peak m/z's EIC", displayChroms, "TIC, BPC, and highest peak m/z's EIC", "Retention time [min]", "Absolute ion abundance"));
            var view = new DisplayChromatogramsView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            view.Show();
        }

        public void ShowShowFragmentSearchSettingView(Window owner, bool isAlignmentViewSelected) {
            var container = _storage;
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;
            var alignmentModel = AlignmentModel;
            var param = container.Parameter;
            
            var model = new FragmentQuerySettingModel(container.Parameter, isAlignmentViewSelected);
            var vm = new FragmentQuerySettingViewModel(model);
            var dialog = new FragmentQuerySettingView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (dialog.ShowDialog() == true) {
                param.FragmentSearchSettingValues = model.FragmentQuerySettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0 && n.RelativeIntensityCutoff > 0).ToList();
                param.AndOrAtFragmentSearch = model.SearchOption.Value;
                if (model.IsAlignSpotViewSelected.Value && alignmentModel is null) {
                    MessageBox.Show("Please select an alignment result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (model.IsAlignSpotViewSelected.Value) {
                    alignmentModel.FragmentSearcher();
                } else {
                    analysisModel.FragmentSearcher();
                }
            }
        }

        public void ShowShowMassqlSearchSettingView(Window owner, bool isAlignmentViewSelected) {
            var container = _storage;
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;
            var alignmentModel = AlignmentModel;
            var param = container.Parameter;

            MassqlSettingModel model;
            if (isAlignmentViewSelected) {
                model = new MassqlSettingModel(container.Parameter, alignmentModel.FragmentSearcher);
            }
            else {
                model = new MassqlSettingModel(container.Parameter, analysisModel.FragmentSearcher);
            }
            var vm = new MassqlSettingViewModel(model);
            var dialog = new MassqlSettingView()
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.Show();
        }

        public void ShowShowMscleanrFilterSettingView(Window owner) {
            var container = _storage;
            var analysisModel = AnalysisModel;
            if (analysisModel is null) return;
            var alignmentModel = AlignmentModel;
            var spotprops = alignmentModel.Ms1Spots;

            MscleanrSettingModel model;
            model = new MscleanrSettingModel(container.Parameter, spotprops);
            var vm = new MscleanrSettingViewModel(model);
            var dialog = new MscleanrSettingView()
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            dialog.Show();

            //param.FragmentSearchSettingValues = model.FragmentQuerySettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0 && n.RelativeIntensityCutoff > 0).ToList();
            //param.AndOrAtFragmentSearch = model.SearchOption.Value;
        }

        public void GoToMsfinderMethod(bool isAlignmentView) {
            if (isAlignmentView) {
                AlignmentModel.GoToMsfinderMethod();
            }
            else {
                AnalysisModel.GoToMsfinderMethod();
            }
        }
    }
}
