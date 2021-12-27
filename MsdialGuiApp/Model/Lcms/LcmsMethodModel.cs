using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.LC;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Setting;
using CompMs.App.Msdial.View;
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
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.DataObj;
using CompMs.MsdialLcMsApi.Export;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Lcms
{
    sealed class LcmsMethodModel : MethodModelBase
    {
        static LcmsMethodModel() {
            chromatogramSpotSerializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", CompMs.Common.Components.ChromXType.RT);
        }

        public LcmsMethodModel(MsdialLcmsDataStorage storage, IDataProviderFactory<AnalysisFileBean> providerFactory)
            : base(storage.AnalysisFiles, storage.AlignmentFiles) {
            if (storage is null) {
                throw new ArgumentNullException(nameof(storage));
            }

            if (providerFactory is null) {
                throw new ArgumentNullException(nameof(providerFactory));
            }
            Storage = storage;
            this.providerFactory = providerFactory;
        }

        public MsdialLcmsDataStorage Storage {
            get => storage;
            set => SetProperty(ref storage, value);
        }
        private MsdialLcmsDataStorage storage;

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
                Storage.DataBaseMapper,
                Storage.MsdialLcmsParameter,
                Storage.DataBaseMapper.MoleculeAnnotators)
            .AddTo(Disposables);
        }

        protected override void LoadAlignmentFileCore(AlignmentFileBean alignmentFile) {
            if (AlignmentModel != null) {
                AlignmentModel.Dispose();
                Disposables.Remove(AlignmentModel);
            }
            AlignmentModel = new LcmsAlignmentModel(
                alignmentFile,
                Storage.MsdialLcmsParameter,
                Storage.DataBaseMapper,
                Storage.DataBaseMapper.MoleculeAnnotators)
            .AddTo(Disposables);
        }

        public bool ProcessSetAnalysisParameter(Window owner) {
            var parameter = Storage.MsdialLcmsParameter;
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
                        FilePath = System.IO.Path.Combine(Storage.MsdialLcmsParameter.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.arf),
                        EicFilePath = System.IO.Path.Combine(Storage.MsdialLcmsParameter.ProjectFolderPath, filename + ".EIC.aef"),
                        SpectraFilePath = System.IO.Path.Combine(Storage.MsdialLcmsParameter.ProjectFolderPath, filename + "." + MsdialDataStorageFormat.dcl)
                    }
                );
                Storage.AlignmentFiles = AlignmentFiles.ToList();
            }

            annotationProcess = BuildProteoMetabolomicsAnnotationProcess(Storage.DataBases, parameter);
            Storage.DataBaseMapper = CreateDataBaseMapper(Storage.DataBases);
            return true;
        }

        private IAnnotationProcess BuildAnnotationProcess(DataBaseStorage storage, PeakPickBaseParameter parameter) {
            var containers = new List<IAnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>>();
            foreach (var annotators in storage.MetabolomicsDataBases) {
                containers.AddRange(annotators.Pairs.Select(annotator => annotator.ConvertToAnnotatorContainer()));
            }
            return new StandardAnnotationProcess<IAnnotationQuery>(new AnnotationQueryFactory(parameter), containers);
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
                new PepAnnotationQueryFactory(parameter.PeakPickBaseParam, parameter.ProteomicsParam, parameter.MspSearchParam),
                containers, 
                pepContainers);
        }

        private DataBaseMapper CreateDataBaseMapper(DataBaseStorage storage) {
            var mapper = new DataBaseMapper();
            foreach (var db in storage.MetabolomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.SerializableAnnotator, db.DataBase);
                }
            }
            foreach (var db in storage.ProteomicsDataBases) {
                foreach (var pair in db.Pairs) {
                    mapper.Add(pair.SerializableAnnotator, db.DataBase);
                }
            }

            return mapper;
        }

        public bool ProcessAnnotaion(Window owner, MsdialLcmsDataStorage storage) {
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
                    await Task.Run(() => MsdialLcMsApi.Process.FileProcess.Run(analysisfile, provider, storage, annotationProcess, isGuiProcess: true, reportAction: v => pbvm.CurrentValue = v));
                    vm.CurrentValue++;
                }

                pbmcw.Close();
            };

            pbmcw.ShowDialog();

            return true;
        }

        public bool ProcessSeccondAnnotaion4ShotgunProteomics(Window owner, MsdialLcmsDataStorage storage) {
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
                storage.MsdialLcmsParameter.ProteomicsParam, 
                v => vm.CurrentValue = v);

            pbw.Close();

            return true;
        }

        public bool ProcessAlignment(Window owner, MsdialLcmsDataStorage storage) {
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

            var factory = new LcmsAlignmentProcessFactory(storage.MsdialLcmsParameter, storage.IupacDatabase, storage.DataBaseMapper);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = providerFactory; // TODO: I'll remove this later.
            var alignmentFile = storage.AlignmentFiles.Last();
            var result = aligner.Alignment(storage.AnalysisFiles, alignmentFile, chromatogramSpotSerializer);
            MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result?.AlignmentSpotProperties).ToList());

            pbw.Close();

            return true;
        }

        private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(MsdialLcmsDataStorage storage, IReadOnlyList<AlignmentSpotProperty> spots) {
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
            var metadataAccessor = new LcmsMetadataAccessor(container.DataBaseMapper, container.MsdialLcmsParameter);
            var vm = new AlignmentResultExport2VM(AlignmentFile, container.AlignmentFiles, container);
            vm.ExportTypes.AddRange(
                new List<ExportType2>
                {
                    new ExportType2("Raw data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Height", container.MsdialLcmsParameter), "Height", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }, true),
                    new ExportType2("Raw data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Area", container.MsdialLcmsParameter), "Area", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Height)", metadataAccessor, new LegacyQuantValueAccessor("Normalized height", container.MsdialLcmsParameter), "NormalizedHeight", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Normalized data (Area)", metadataAccessor, new LegacyQuantValueAccessor("Normalized area", container.MsdialLcmsParameter), "NormalizedArea", new List<StatsValue>{ StatsValue.Average, StatsValue.Stdev }),
                    new ExportType2("Alignment ID", metadataAccessor, new LegacyQuantValueAccessor("ID", container.MsdialLcmsParameter), "PeakID"),
                    new ExportType2("m/z", metadataAccessor, new LegacyQuantValueAccessor("MZ", container.MsdialLcmsParameter), "Mz"),
                    new ExportType2("S/N", metadataAccessor, new LegacyQuantValueAccessor("SN", container.MsdialLcmsParameter), "SN"),
                    new ExportType2("MS/MS included", metadataAccessor, new LegacyQuantValueAccessor("MSMS", container.MsdialLcmsParameter), "MsmsIncluded"),
                });
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
                // new Export.SpectraType(
                //     ExportspectraType.deconvoluted,
                //     new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.ParameterBase, ExportspectraType.deconvoluted)),
                // new Export.SpectraType(
                //     ExportspectraType.centroid,
                //     new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.ParameterBase, ExportspectraType.centroid)),
                // new Export.SpectraType(
                //     ExportspectraType.profile,
                //     new ImmsAnalysisMetadataAccessor(container.DataBaseMapper, container.ParameterBase, ExportspectraType.profile)),
            };
            var spectraFormats = new List<Export.SpectraFormat>
            {
                new Export.SpectraFormat(ExportSpectraFileFormat.txt, new AnalysisCSVExporter()),
            };

            using (var vm = new AnalysisResultExportViewModel(container.AnalysisFiles, spectraTypes, spectraFormats, providerFactory)) {
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
            var vm = new ChromatogramsViewModel(new ChromatogramsModel("Total ion chromatogram", new DisplayChromatogram(tic, new Pen(Brushes.Black, 1.0), "TIC")));
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
            var vm = new ChromatogramsViewModel(new ChromatogramsModel("Base peak chromatogram", new DisplayChromatogram(bpc, new Pen(Brushes.Red, 1.0), "BPC")));
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

            var param = container.MsdialLcmsParameter;
            var model = new Setting.DisplayEicSettingModel(param);
            var dialog = new EICDisplaySettingView() {
                DataContext = new DisplayEicSettingViewModel(model),
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            if (dialog.ShowDialog() == true) {
                param.AdvancedProcessOptionBaseParam.DiplayEicSettingValues = model.DiplayEicSettingValues.Where(n => n.Mass > 0 && n.MassTolerance > 0).ToList();
                var displayEICs = param.AdvancedProcessOptionBaseParam.DiplayEicSettingValues;
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
            
            var model = new FragmentQuerySettingModel(container.MsdialLcmsParameter, isAlignmentViewSelected);
            var vm = new FragmentQuerySettingViewModel(model);
            var dialog = new FragmentQuerySettingView() {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            if (dialog.ShowDialog() == true) {
                if (model.IsAlignSpotViewSelected.Value && alignmentModel is null) {
                    MessageBox.Show("Please select an alignment result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                if (model.IsAlignSpotViewSelected.Value) {

                }
                else {
                    analysisModel.MsmsFragmentSearcher();
                }
            }
        }
    }
}
