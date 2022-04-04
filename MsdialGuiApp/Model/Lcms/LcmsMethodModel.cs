using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.LC;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.Lcms;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.Common.Parameter;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Graphics.UI.Message;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Export;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcms
{
    sealed class LcmsMethodModel : MethodModelBase
    {
        static LcmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT);
        }

        public LcmsMethodModel(
            IMsdialDataStorage<MsdialLcmsParameter> storage,
            IDataProviderFactory<AnalysisFileBean> providerFactory,
            IObservable<ParameterBase> parameterAsObservable,
            IObservable<IBarItemsLoader> barItemsLoader)
            : base(storage.AnalysisFiles, storage.AlignmentFiles) {
            if (storage is null) {
                throw new ArgumentNullException(nameof(storage));
            }

            if (providerFactory is null) {
                throw new ArgumentNullException(nameof(providerFactory));
            }
            Storage = storage;
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(Storage.DataBases);
            this.providerFactory = providerFactory;
            this.parameterAsObservable = parameterAsObservable;
            this.barItemsLoader = barItemsLoader;
            PeakFilterModel = new PeakFilterModel(DisplayFilter.All & ~DisplayFilter.CcsMatched);
        }

        public IMsdialDataStorage<MsdialLcmsParameter> Storage { get; }

        private FacadeMatchResultEvaluator matchResultEvaluator;

        public PeakFilterModel PeakFilterModel { get; }

        public LcmsAnalysisModel AnalysisModel {
            get => analysisModel;
            private set => SetProperty(ref analysisModel, value);
        }
        private LcmsAnalysisModel analysisModel;

        public LcmsAlignmentModel AlignmentModel {
            get => alignmentModel;
            set => SetProperty(ref alignmentModel, value);
        }
        private LcmsAlignmentModel alignmentModel;

        private static readonly ChromatogramSerializer<ChromatogramSpotInfo> chromatogramSpotSerializer;
        private readonly IDataProviderFactory<AnalysisFileBean> providerFactory;
        private readonly IObservable<ParameterBase> parameterAsObservable;
        private readonly IObservable<IBarItemsLoader> barItemsLoader;
        private IAnnotationProcess annotationProcess;


        protected override void LoadAnalysisFileCore(AnalysisFileBean analysisFile) {
            if (AnalysisModel != null) {
                AnalysisModel.Dispose();
                Disposables.Remove(AnalysisModel);
            }
            var provider = providerFactory.Create(analysisFile);
            AnalysisModel = new LcmsAnalysisModel(
                analysisFile,
                provider,
                Storage.DataBases,
                Storage.DataBaseMapper,
                matchResultEvaluator,
                Storage.Parameter,
                PeakFilterModel)
            .AddTo(Disposables);
        }

        protected override AlignmentModelBase LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            return AlignmentModel = new LcmsAlignmentModel(
                alignmentFile,
                matchResultEvaluator,
                Storage.DataBases,
                PeakFilterModel,
                Storage.DataBaseMapper,
                Storage.Parameter,
                parameterAsObservable,
                barItemsLoader,
                Storage.AnalysisFiles)
            .AddTo(Disposables);
        }

        public override void Run(ProcessOption option) {
            // Set analysis param
            var parameter = Storage.Parameter;
            // matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(Storage.DataBases);
            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                annotationProcess = BuildProteoMetabolomicsAnnotationProcess(Storage.DataBases, parameter);
            }
            else if(parameter.TargetOmics == TargetOmics.Lipidomics && parameter.CollistionType == CollisionType.EIEIO) {
                annotationProcess = BuildEadLipidomicsAnnotationProcess(Storage.DataBases, Storage.DataBaseMapper, parameter);
            }
            else {
                annotationProcess = BuildAnnotationProcess(Storage.DataBases, parameter.PeakPickBaseParam);
            }

            var processOption = option;
            // Run Identification
            if (processOption.HasFlag(ProcessOption.Identification) || processOption.HasFlag(ProcessOption.PeakSpotting)) {
                if (!ProcessAnnotaion(null, Storage))
                    return;
            }

            // Run second process
            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                if (!ProcessSeccondAnnotaion4ShotgunProteomics(null, Storage))
                    return;
            } 

            // Run Alignment
            if (processOption.HasFlag(ProcessOption.Alignment)) {
                if (!ProcessAlignment(null, Storage))
                    return;
            }

            LoadAnalysisFile(Storage.AnalysisFiles.FirstOrDefault());

#if DEBUG
            Console.WriteLine(string.Join("\n", Storage.Parameter.ParametersAsText()));
#endif
        }

        public bool ProcessSetAnalysisParameter(Window owner) {
            var parameter = Storage.Parameter;
            var analysisParamSetModel = new LcmsAnalysisParameterSetModel(parameter, AnalysisFiles, Storage.DataBases);
            using (var analysisParamSetVM = new LcmsAnalysisParameterSetViewModel(analysisParamSetModel)) {
                var apsw = new AnalysisParamSetForLcWindow
                {
                    DataContext = analysisParamSetVM,
                    Owner = owner,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                if (apsw.ShowDialog() != true) {
                    return false;
                }
            }

            var message = new ShortMessageWindow() {
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = "Loading library files..",
            };
            message.Show();
            Storage.DataBases = analysisParamSetModel.IdentitySettingModel.Create();
            message.Close();

            if (parameter.TogetherWithAlignment) {
                var filename = analysisParamSetModel.AlignmentResultFileName;
                AlignmentFiles.Add(
                    new AlignmentFileBean
                    {
                        FileID = AlignmentFiles.Count,
                        FileName = filename,
                        FilePath = System.IO.Path.Combine(Storage.Parameter.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.arf),
                        EicFilePath = System.IO.Path.Combine(Storage.Parameter.ProjectFolderPath, filename + ".EIC.aef"),
                        SpectraFilePath = System.IO.Path.Combine(Storage.Parameter.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.dcl),
                        ProteinAssembledResultFilePath = System.IO.Path.Combine(Storage.Parameter.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.prf),
                    }
                );
                Storage.AlignmentFiles = AlignmentFiles.ToList();
            }

            Storage.DataBaseMapper = Storage.DataBases.CreateDataBaseMapper();
            matchResultEvaluator = FacadeMatchResultEvaluator.FromDataBases(Storage.DataBases);
            if (parameter.TargetOmics == TargetOmics.Proteomics) {
                annotationProcess = BuildProteoMetabolomicsAnnotationProcess(Storage.DataBases, parameter);
            }
            else if(parameter.TargetOmics == TargetOmics.Lipidomics && parameter.CollistionType == CollisionType.EIEIO) {
                annotationProcess = BuildEadLipidomicsAnnotationProcess(Storage.DataBases, Storage.DataBaseMapper, parameter);
            }
            else {
                annotationProcess = BuildAnnotationProcess(Storage.DataBases, parameter.PeakPickBaseParam);
            }
            return true;
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
            var eadAnnotationQueryFactoryTriple = new List<(IAnnotationQueryFactory<IAnnotationQueryZZZ<MsScanMatchResult>>, IMatchResultEvaluator<MsScanMatchResult>, MsRefSearchParameterBase)>();
            foreach (var annotators in storage.EadLipidomicsDatabases) {
                eadAnnotationQueryFactoryTriple.AddRange(annotators.Pairs.Select(annotator => (new AnnotationQueryWithReferenceFactory(mapper, annotator.SerializableAnnotator, parameter.PeakPickBaseParam) as IAnnotationQueryFactory<IAnnotationQueryZZZ<MsScanMatchResult>>, annotator.SerializableAnnotator as IMatchResultEvaluator<MsScanMatchResult>, annotator.SearchParameter)));
            }
            return new EadLipidomicsAnnotationProcess<IAnnotationQuery>(containerPairs, eadAnnotationQueryFactoryTriple, mapper);
        }

        public bool ProcessAnnotaion(Window owner, IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var vm = new ProgressBarMultiContainerVM
            {
                MaxValue = storage.AnalysisFiles.Count,
                CurrentValue = 0,
                ProgressBarVMs = new ObservableCollection<ProgressBarVM>(
                        storage.AnalysisFiles.Select(file => new ProgressBarVM { Label = file.AnalysisFileName })
                    ),
            };
            var pbmcw = new ProgressBarMultiContainerWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            pbmcw.Loaded += async (s, e) => {
                foreach ((var analysisfile, var pbvm) in storage.AnalysisFiles.Zip(vm.ProgressBarVMs)) {
                    var provider = providerFactory.Create(analysisfile);
                    await Task.Run(() => MsdialLcMsApi.Process.FileProcess.Run(analysisfile, provider, storage, annotationProcess, matchResultEvaluator, isGuiProcess: true, reportAction: v => pbvm.CurrentValue = v));
                    vm.CurrentValue++;
                }

                pbmcw.Close();
            };

            pbmcw.ShowDialog();

            return true;
        }

        public bool ProcessSeccondAnnotaion4ShotgunProteomics(Window owner, IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var vm = new ProgressBarVM {
                IsIndeterminate = true,
                Label = "Process second annotation..",
            };
            var pbw = new ProgressBarWindow {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            pbw.Show();

            var proteomicsAnnotator = new ProteomeDataAnnotator();
            proteomicsAnnotator.ExecuteSecondRoundAnnotationProcess(
                storage.AnalysisFiles,
                storage.DataBaseMapper,
                matchResultEvaluator,
                storage.DataBases,
                storage.Parameter,
                v => vm.CurrentValue = v);

            pbw.Close();

            return true;
        }

        public bool ProcessAlignment(Window owner, IMsdialDataStorage<MsdialLcmsParameter> storage) {
            var vm = new ProgressBarVM
            {
                IsIndeterminate = true,
                Label = "Process alignment..",
            };
            var pbw = new ProgressBarWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            pbw.Show();

            var factory = new LcmsAlignmentProcessFactory(storage, matchResultEvaluator);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = providerFactory; // TODO: I'll remove this later.
            var alignmentFile = storage.AlignmentFiles.Last();
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);

            if (!storage.DataBaseMapper.PeptideAnnotators.IsEmptyOrNull()) {
                new ProteomeDataAnnotator().MappingToProteinDatabase(
                    alignmentFile.ProteinAssembledResultFilePath,
                    result,
                    storage.DataBases.ProteomicsDataBases,
                    storage.DataBaseMapper,
                    matchResultEvaluator,
                    storage.Parameter);
            }

            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result?.AlignmentSpotProperties).ToList());

            pbw.Close();

            return true;
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
                    var peakID = spot.AlignedPeakProperties[repID].MasterPeakID;
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
            var container = Storage;
            var vm = new AlignmentResultExport2VM(AlignmentFile, container.AlignmentFiles, container);

            if (container.Parameter.TargetOmics == TargetOmics.Proteomics) {
                var metadataAccessor = new LcmsProteomicsMetadataAccessor(container.DataBaseMapper, container.Parameter);
                vm.ExportTypes.AddRange(
               new List<ExportType2>
               {
                    new ExportType2("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", container.Parameter), "Height", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType2("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", container.Parameter), "Area", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", container.Parameter), "NormalizedHeight", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", container.Parameter), "NormalizedArea", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Alignment ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.Parameter), "PeakID"),
                    new ExportType2("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", container.Parameter), "Mz"),
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
                    new ExportType2("Alignment ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.Parameter), "PeakID"),
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
            var container = Storage;
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
                providerFactory)) {
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
            var container = Storage;
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
            var container = Storage;
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
            var container = Storage;
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
            var container = Storage;
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
            var container = Storage;
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
                }
                else {
                    analysisModel.FragmentSearcher();
                }
            }
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
