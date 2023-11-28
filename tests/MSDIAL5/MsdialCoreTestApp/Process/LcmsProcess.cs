using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using CompMs.MsdialLcMsApi.DataObj;
using CompMs.MsdialLcMsApi.Export;
using CompMs.MsdialLcMsApi.Process;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process
{
    public class FileTask {
        public string FilePath { get; set; }
        public string IonModeTask { get; set; } // positive, negative, both, unknown
    }

    public class LcmsProcess
    {
        public List<FileTask> GetFileTasks(string inputfolder) {
            var analysisFiles = AnalysisFilesParser.ReadInput(inputfolder);
            var tasks = new List<FileTask>();
            foreach (var file in analysisFiles) {
                var filetask = new FileTask() { FilePath = file.AnalysisFilePath, IonModeTask = "Unknown" };
                using (var access = new RawDataAccess(file.AnalysisFilePath, 0, false, false, false, null)) {
                    for (var i = 0; i < 5; i++) {
                        var rawObj = access.GetMeasurement();
                        if (rawObj != null) {
                            var spectra = rawObj.SpectrumList;
                            if (!spectra.IsEmptyOrNull()) {
                                var ionmodes = spectra.Select(n => n.ScanPolarity).Distinct().ToList();

                                if (ionmodes.Count == 1) {
                                    var ionmode = ionmodes[0];
                                    switch (ionmode) {
                                        case Common.DataObj.ScanPolarity.Positive:
                                            filetask.IonModeTask = "Positive";
                                            break;
                                        case Common.DataObj.ScanPolarity.Negative:
                                            filetask.IonModeTask = "Negative";
                                            break;
                                        default:
                                            filetask.IonModeTask = "Unknown";
                                            break;
                                    }
                                } 
                                else if (ionmodes.Count == 2) {
                                    var ionmode1 = ionmodes[0];
                                    var ionmode2 = ionmodes[1];
                                    if ((ionmode1 == Common.DataObj.ScanPolarity.Positive && ionmode2 == Common.DataObj.ScanPolarity.Negative) ||
                                        (ionmode1 == Common.DataObj.ScanPolarity.Negative && ionmode2 == Common.DataObj.ScanPolarity.Positive)) {
                                        filetask.IonModeTask = "Both";
                                    }
                                }
                            }
                        }
                        Thread.Sleep(5000);
                    }
                }
                tasks.Add(filetask);
            }
            return tasks;
        }

        public int Run(List<FileTask> filetasks, string inputfolder, string outputfolder, string posmethodfile, string negmethodfile, bool isProjectSaved, float targetMz) {
            var ionmodetasks = filetasks.Select(n => n.IonModeTask).Distinct().ToList();
            if (ionmodetasks.Count == 1) { // pos only, neg only, unknown only
                var ionmodetask = ionmodetasks[0];
                if (ionmodetask == "Unknown") return -1;
                if (ionmodetask == "Positive") {
                    return Run(inputfolder, outputfolder, posmethodfile, isProjectSaved, targetMz);
                }
                else if (ionmodetask == "Negative") {
                    return Run(inputfolder, outputfolder, negmethodfile, isProjectSaved, targetMz);
                }
                else if (ionmodetask == "Both") {

                    Run(inputfolder, outputfolder, posmethodfile, isProjectSaved, targetMz);
                    return Run(inputfolder, outputfolder, negmethodfile, isProjectSaved, targetMz);
                }
                else { return -1; }
            }
            else if (ionmodetasks.Count > 1) { // case: a folder contains several polarity setting files
                foreach (var modetask in ionmodetasks) {
                    switch (modetask) {
                        case "Positive":
                            var ignorefiles_pos = filetasks.Where(n => n.IonModeTask != "Positive").Select(n => n.FilePath).ToList();
                            Run(inputfolder, outputfolder, posmethodfile, isProjectSaved, targetMz, ignorefiles_pos);
                            break;
                        case "Negative":
                            var ignorefiles_neg = filetasks.Where(n => n.IonModeTask != "Negative").Select(n => n.FilePath).ToList();
                            Run(inputfolder, outputfolder, posmethodfile, isProjectSaved, targetMz, ignorefiles_neg);
                            break;
                        case "Both":
                            var ignorefiles_pos_vs2 = filetasks.Where(n => n.IonModeTask != "Positive").Select(n => n.FilePath).ToList();
                            Run(inputfolder, outputfolder, posmethodfile, isProjectSaved, targetMz, ignorefiles_pos_vs2);

                            var ignorefiles_neg_vs2 = filetasks.Where(n => n.IonModeTask != "Negative").Select(n => n.FilePath).ToList();
                            Run(inputfolder, outputfolder, posmethodfile, isProjectSaved, targetMz, ignorefiles_neg_vs2);
                            break;
                        default: break; 
                    }
                }
                return 1;
            }
            return -1;
        }

        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz, List<string> ignorefiles) {
            var param = ConfigParser.ReadForLcmsParameter(methodFile);
            if (param.ProjectParam.AcquisitionType == AcquisitionType.None) param.ProjectParam.AcquisitionType = AcquisitionType.DDA;
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;

            var nAnalysisFiles = new List<AnalysisFileBean>();
            foreach (var file in analysisFiles) {
                var filepath = file.AnalysisFilePath;
                if (!ignorefiles.IsEmptyOrNull()) {
                    var flag = false;
                    foreach (var ifile in ignorefiles) {
                        if (filepath == ifile) { 
                            flag = true;
                            break;
                        }
                    }
                    if (flag) continue;
                    nAnalysisFiles.Add(file);
                }
            }

            return Run(nAnalysisFiles, alignmentFile, param, outputFolder, methodFile, isProjectSaved, targetMz);
        }

        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
            var param = ConfigParser.ReadForLcmsParameter(methodFile);
            if (param.ProjectParam.AcquisitionType == AcquisitionType.None) param.ProjectParam.AcquisitionType = AcquisitionType.DDA;
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;

            return Run(analysisFiles, alignmentFile, param, outputFolder, methodFile, isProjectSaved, targetMz);
        }

        public int AutoRun(string inputFolder, string outputFolder, string posmethod, string negmethod, bool isProjectSaved, float targetMz) {
            var tasks = GetFileTasks(inputFolder);
            return Run(tasks, inputFolder, outputFolder, posmethod, negmethod, isProjectSaved, targetMz);
        }

        public int Run(List<AnalysisFileBean> analysisFiles, AlignmentFileBean alignmentFile, MsdialLcmsParameter param, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
            CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
                out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB,
                out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode,
                out List<MoleculeMsReference> lbmDB);

            var container = new MsdialLcmsDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialLcmsParameter = param
            };

            var database = new MoleculeDataBase(mspDB, param.MspFilePath, DataBaseSource.Msp, SourceType.MspDB);
            var annotator = new LcmsMspAnnotator(database, param.MspSearchParam, param.TargetOmics, param.MspFilePath, 1);
            var dbStorage = DataBaseStorage.CreateEmpty();
            dbStorage.AddMoleculeDataBase(database, new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, param.PeakPickBaseParam, param.MspSearchParam, ignoreIsotopicPeak: true)),
            });
            MoleculeDataBase lbmDatabase = new MoleculeDataBase(lbmDB, param.LbmFilePath, DataBaseSource.Lbm, SourceType.MspDB);
            var lbmAnnotator = new LcmsMspAnnotator(lbmDatabase, param.MspSearchParam, param.TargetOmics, param.LbmFilePath, 1);
            dbStorage.AddMoleculeDataBase(lbmDatabase, new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                new MetabolomicsAnnotatorParameterPair(lbmAnnotator.Save(), new AnnotationQueryFactory(lbmAnnotator, param.PeakPickBaseParam, param.MspSearchParam, ignoreIsotopicPeak: true)),
            });
            var textdatabase = new MoleculeDataBase(txtDB, param.TextDBFilePath, DataBaseSource.Text, SourceType.TextDB);
            var textannotator = new LcmsTextDBAnnotator(textdatabase, param.TextDbSearchParam, param.TextDBFilePath, 2);
            dbStorage.AddMoleculeDataBase(textdatabase, new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                new MetabolomicsAnnotatorParameterPair(textannotator.Save(), new AnnotationQueryFactory(textannotator, param.PeakPickBaseParam, param.TextDbSearchParam, ignoreIsotopicPeak: false)),
            });
            container.DataBases = dbStorage;
            container.DataBaseMapper = dbStorage.CreateDataBaseMapper();

            var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, param.ProjectParam.ProjectFileName + ".mdproject"));
            projectDataStorage.AddStorage(container);

            Console.WriteLine("Start processing..");
            return Execute(projectDataStorage, container, outputFolder, isProjectSaved);
        }

        

        private int Execute(ProjectDataStorage projectDataStorage, IMsdialDataStorage<MsdialLcmsParameter> storage, string outputFolder, bool isProjectSaved) {
            var files = storage.AnalysisFiles;
            var tasks = new Task[files.Count];
            var evaluator = new MsScanMatchResultEvaluator(storage.Parameter.MspSearchParam);
            var annotationProcess = new StandardAnnotationProcess(storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories, evaluator, storage.DataBaseMapper);
            //new EadLipidomicsAnnotationProcess(storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories)
            var providerFactory = new StandardDataProviderFactory(5, false);
            var process = new FileProcess(providerFactory, storage, annotationProcess, evaluator);

            IAnalysisExporter peak_MspExporter = new AnalysisMspExporter(storage.DataBaseMapper, storage.Parameter);
            var peak_accessor = new LcmsAnalysisMetadataAccessor(storage.DataBaseMapper, storage.Parameter, ExportspectraType.deconvoluted);
            var peak_Exporter = new AnalysisCSVExporter();
            var sem = new SemaphoreSlim(Environment.ProcessorCount / 2);
            foreach ((var file, var idx) in files.WithIndex()) {
                tasks[idx] = Task.Run(async () => {
                    await sem.WaitAsync();
                    try {
                        var provider = providerFactory.Create(file);
                        await process.RunAsync(file, provider, null).ConfigureAwait(false);

                        var peak_outputfile = Path.Combine(outputFolder, file.AnalysisFileName + ".mdpeak");
                        var peak_outputmspfile = Path.Combine(outputFolder, file.AnalysisFileName + ".mdmsp");
                        using (var stream = File.Open(peak_outputfile, FileMode.Create, FileAccess.Write))
                        using (var mspstream = File.Open(peak_outputmspfile, FileMode.Create, FileAccess.Write)) { 
                            var peak_container = await ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).ConfigureAwait(false);
                            var peak_decResults = MsdecResultsReader.ReadMSDecResults(file.DeconvolutionFilePath, out _, out _);
                            peak_Exporter.Export(stream, peak_container.Items, peak_decResults, provider, peak_accessor, file);

                            peak_MspExporter.Export(mspstream, file, peak_container);
                        }
                    }
                    finally {
                        sem.Release();
                    }
                });
            }
            Task.WaitAll(tasks);
            if (!storage.Parameter.TogetherWithAlignment) return 0;

            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
            var alignmentFile = storage.AlignmentFiles.First();
            var factory = new LcmsAlignmentProcessFactory(storage, evaluator);
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(files, alignmentFile, serializer);

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);

            var align_outputfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdalign");
            var align_outputmspfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdmsp");
            var align_decResults = LoadRepresentativeDeconvolutions(storage, result?.AlignmentSpotProperties).ToList();
            var align_accessor = new LcmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter, false);
            var align_quantAccessor = new LegacyQuantValueAccessor("Height", storage.Parameter);
            var align_stats = new[] { StatsValue.Average, StatsValue.Stdev };
            using (var stream = File.Open(align_outputfile, FileMode.Create, FileAccess.Write)) {
                using (var streammsp = File.Open(align_outputmspfile, FileMode.Create, FileAccess.Write)) {
                    var align_exporter = new AlignmentCSVExporter();
                    align_exporter.Export(stream, result.AlignmentSpotProperties, align_decResults, files,
                        align_accessor, align_quantAccessor, align_stats);
                    IAlignmentExporter align_mspexporter = new AlignmentMspExporter(storage.DataBaseMapper, storage.Parameter);
                    align_mspexporter.Export(streammsp, result.AlignmentSpotProperties, align_decResults, files, align_accessor, align_quantAccessor, align_stats);
                }
            }

            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, align_decResults);

            if (isProjectSaved) {
                using (var stream = File.Open(projectDataStorage.ProjectParameter.FilePath, FileMode.Create))
                using (var streamManager = new ZipStreamManager(stream, System.IO.Compression.ZipArchiveMode.Create)) {
                    projectDataStorage.Save(streamManager, new MsdialIntegrateSerializer(), file => new DirectoryTreeStreamManager(file), parameter => Console.WriteLine($"Save {parameter.ProjectFileName} failed")).Wait();
                    ((IStreamManager)streamManager).Complete();
                }
            }
            return 0;
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
    }
}
