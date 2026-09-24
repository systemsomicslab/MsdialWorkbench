using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Properties;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process;

public sealed class LcmsProcess
{
    public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz)
        => RunCore(inputFolder, outputFolder, methodFile, isProjectSaved, targetMz, null);

    public int RunWithMolecularNetworking(string inputFolder, string outputFolder, string methodFile, string msnMethodFile, bool isProjectSaved, float targetMz, bool resume = false)
        => RunCore(inputFolder, outputFolder, methodFile, isProjectSaved, targetMz, ConfigParser.ReadForMoleculerNetworkingParameter(msnMethodFile), resume);

    private int RunCore(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz, MolecularSpectrumNetworkingBaseParameter? networkingParameter, bool resume = false)
    {
        var param = ConfigParser.ReadForLcmsParameter(methodFile);
        var isAlignmentLightMode = ConfigParser.ReadAlignmentLightMode(methodFile);
        var exportDetailedAlignmentProvenance = ConfigParser.ReadDetailedAlignmentProvenance(methodFile);
        var exportAnnotationCandidates = ConfigParser.ReadAnnotationCandidateExport(methodFile);
        var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile, confirmMixedFormats: networkingParameter is null);
        if (!isCorrectlyImported) {
            return -1;
        }

        if (networkingParameter is not null) {
#if NET8_0_OR_GREATER
            if (analysisFiles.Any(file => string.Equals(Path.GetExtension(file.AnalysisFilePath), ".wiff", StringComparison.OrdinalIgnoreCase))) {
                Console.Error.WriteLine("WIFF input requires the .NET Framework build with the bundled SCIEX SDK. Run bin/Debug/net48/MSDIALCUI.exe (or the Release/net48 build).");
                return -1;
            }
#endif
            Directory.CreateDirectory(outputFolder);
            param.TogetherWithAlignment = true;
            param.MolecularSpectrumNetworkingBaseParam = networkingParameter;
            param.ProjectFolderPath = outputFolder;
            param.ExportFolderPath = outputFolder;
            // Networking requires the full alignment peak matrix, including ion correlations.
            if (isAlignmentLightMode) {
                Console.WriteLine("lcms-msn uses full alignment for molecular networking; alignment light mode is disabled.");
                isAlignmentLightMode = false;
            }
            alignmentFile = AlignmentResultParser.GetAlignmentFileBean(outputFolder);
            foreach (var file in analysisFiles) {
                var prefix = Path.Combine(outputFolder, $"analysis-{file.AnalysisFileId}");
                file.DeconvolutionFilePath = prefix + ".dcl";
                file.PeakAreaBeanInformationFilePath = prefix + ".pai";
                file.RetentionTimeCorrectionBean = new RetentionTimeCorrectionBean(prefix + ".rtc");
            }
        }

        var resumeFromAnalysis = false;
        if (resume && networkingParameter is not null) {
            if (TryResumeMolecularNetworking(outputFolder, networkingParameter, analysisFiles.Count)) {
                return 0;
            }
            resumeFromAnalysis = HasValidAnalysisResults(analysisFiles);
            Console.WriteLine(resumeFromAnalysis
                ? "Resume: valid analysis results found; starting from alignment."
                : "Resume: no complete valid intermediate results found; starting from analysis.");
        }

        try {
            if (resumeFromAnalysis) {
                Console.WriteLine("Resume: retention-time correction preparation is skipped.");
            }
            else {
                RetentionTimeCorrectionProcess.Prepare(analysisFiles, param, outputFolder);
            }
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"RT correction failed: {ex}");
            return -1;
        }

        var mspAnnotatorSettings = ConfigParser.ReadMspAnnotatorSettings(methodFile, param);
        var textAnnotatorSettings = ConfigParser.ReadTextAnnotatorSettings(methodFile, param);
        var lbmAnnotatorPriority = ConfigParser.ReadLbmAnnotatorPriority(methodFile);
        CommonProcess.ParseLibraries(param, targetMz, mspAnnotatorSettings, textAnnotatorSettings, out IupacDatabase iupacDB,
            out var mspDBs, out var textDBs,
            out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode,
            out var lbmDB);

        var container = new MsdialLcmsDataStorage() {
            AnalysisFiles = analysisFiles,
            AlignmentFiles = [alignmentFile],
            IsotopeTextDB = isotopeTextDB,
            IupacDatabase = iupacDB,
            MsdialLcmsParameter = param
        };

        var dbStorage = DataBaseStorage.CreateEmpty();
        foreach (var mspDB in mspDBs.Where(db => db.DataBase is { Database.Count: > 0 })) {
            var annotatorPairs = new List<IAnnotatorParameterPair<MoleculeDataBase>>();
            foreach (var setting in mspDB.AnnotatorSettings) {
                var targetOmics = setting.TargetOmics ?? param.TargetOmics;
                var annotator = new LcmsMspAnnotator(mspDB.DataBase, setting.SearchParameter, targetOmics, setting.AnnotatorId, setting.Priority);
                annotatorPairs.Add(new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, param.PeakPickBaseParam, setting.SearchParameter, ignoreIsotopicPeak: true)));
            }
            if (annotatorPairs.Count > 0) {
                dbStorage.AddMoleculeDataBase(mspDB.DataBase, annotatorPairs);
            }
        }
        if (lbmDB is { Database.Count: > 0 }) {
            var lbmAnnotator = new LcmsMspAnnotator(lbmDB, param.LbmSearchParam, TargetOmics.Lipidomics, CommonProcess.LbmAnnotatorId(param.LbmFilePath), lbmAnnotatorPriority);
            dbStorage.AddMoleculeDataBase(lbmDB, [
                new MetabolomicsAnnotatorParameterPair(lbmAnnotator.Save(), new AnnotationQueryFactory(lbmAnnotator, param.PeakPickBaseParam, param.LbmSearchParam, ignoreIsotopicPeak: true)),
            ]);
        }
        foreach (var textDB in textDBs.Where(db => db.DataBase is { Database.Count: > 0 })) {
            var annotatorPairs = new List<IAnnotatorParameterPair<MoleculeDataBase>>();
            foreach (var setting in textDB.AnnotatorSettings) {
                var annotator = new LcmsTextDBAnnotator(textDB.DataBase, setting.SearchParameter, setting.AnnotatorId, setting.Priority);
                annotatorPairs.Add(new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, param.PeakPickBaseParam, setting.SearchParameter, ignoreIsotopicPeak: false)));
            }
            if (annotatorPairs.Count > 0) {
                dbStorage.AddMoleculeDataBase(textDB.DataBase, annotatorPairs);
            }
        }
        container.DataBaseMapper = new DataBaseMapper();
        container.DataBases = dbStorage;
        container.DataBases.SetDataBaseMapper(container.DataBaseMapper);

        Console.WriteLine("Start processing..");
        return ExecuteAsync(container, outputFolder, isProjectSaved, isAlignmentLightMode, exportDetailedAlignmentProvenance, exportAnnotationCandidates, networkingParameter, resumeFromAnalysis).Result;
    }

    private async Task<int> ExecuteAsync(
        IMsdialDataStorage<MsdialLcmsParameter> storage,
        string outputFolder,
        bool isProjectSaved,
        bool isAlignmentLightMode,
        bool exportDetailedAlignmentProvenance,
        bool exportAnnotationCandidates,
        MolecularSpectrumNetworkingBaseParameter? networkingParameter,
        bool resumeFromAnalysis) {
        var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, Path.ChangeExtension(storage.Parameter.ProjectParam.ProjectFileName, ".mdproject")));
        projectDataStorage.AddStorage(storage);

        var files = storage.AnalysisFiles;
        var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
        var annotationProcess = new StandardAnnotationProcess(storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories, evaluator, storage.DataBaseMapper);
        var providerFactory = new StandardDataProviderFactory(5, false);
        if (!resumeFromAnalysis) {
            var process = new FileProcess(providerFactory, storage, annotationProcess, evaluator);
            var runner = new ProcessRunner(process, Math.Max(1, storage.Parameter.NumThreads / 2));
            await runner.RunAllAsync(files, ProcessOption.All, Enumerable.Repeat(default(IProgress<int>?), files.Count), null, default).ConfigureAwait(false);
        }

        IAnalysisExporter<ChromatogramPeakFeatureCollection> peak_MspExporter = new AnalysisMspExporter(storage.DataBaseMapper, storage.Parameter);
        var peak_accessor = new LcmsAnalysisMetadataAccessor(storage.DataBaseMapper, storage.Parameter, ExportspectraType.deconvoluted);
        var peakExporterFactory = new AnalysisCSVExporterFactory("\t");
        var sem = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount / 2));
        var tasks = resumeFromAnalysis ? Array.Empty<Task>() : new Task[files.Count];
        for (int i = 0; i < tasks.Length; i++) {
            var file = files[i];
            tasks[i] = Task.Run(async () => {
                await sem.WaitAsync();
                try {
                    var peak_container = await file.LoadChromatogramPeakFeatureCollectionAsync().ConfigureAwait(false);

                    var peak_outputfile = Path.Combine(outputFolder, file.AnalysisFileName + ".mdpeak");
                    using var stream = File.Open(peak_outputfile, FileMode.Create, FileAccess.Write);
                    peakExporterFactory.CreateExporter(providerFactory, peak_accessor).Export(stream, file, peak_container, new ExportStyle());

                    var peak_outputmspfile = Path.Combine(outputFolder, file.AnalysisFileName + ".mdmsp");
                    using var mspstream = File.Open(peak_outputmspfile, FileMode.Create, FileAccess.Write);
                    peak_MspExporter.Export(mspstream, file, peak_container, new ExportStyle());
                }
                finally {
                    sem.Release();
                }
            });
        }
        await Task.WhenAll(tasks);

        storage.Parameter.ProjectParam.MsdialVersionNumber = $"Msdial console {Resources.VERSION}";
        if (storage.Parameter.TogetherWithAlignment) {
            var alignmentFile = storage.AlignmentFiles.First();
            using var alignmentLightPeakStore = isAlignmentLightMode ? AlignmentLightPeakStore.CreateTemp() : null;
            AlignmentResultContainer result;
            IReadOnlyList<MSDecResult> align_decResults;
            IDisposable? alignmentLightMsdecResults = null;
            if (isAlignmentLightMode) {
                Console.WriteLine("Alignment light mode: streaming peak matrix, file-backed alignment deconvolution access, GUI chromatogram serialization, GUI alignment object serialization, and ion-abundance correlation links are disabled; text exports remain enabled.");
                Console.WriteLine("Alignment started.");
                var lightRunner = new LcmsAlignmentLightRunner(storage, evaluator, providerFactory, CreateConsoleProgressReporter("Alignment"));
                LcmsAlignmentLightResult lightResult;
                using (ConsoleLineFilter.SuppressExact("Reading data...")) {
                    lightResult = lightRunner.Run(files, alignmentFile, alignmentLightPeakStore!);
                }
                result = lightResult.Container;
                align_decResults = lightResult.MsdecResults;
                alignmentLightMsdecResults = lightResult.MsdecResults as IDisposable;
                Console.WriteLine("Alignment finished.");
            }
            else {
                var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
                var factory = new LcmsAlignmentProcessFactory(storage, evaluator);
                factory.Progress = CreateConsoleProgressReporter("Alignment");
                var aligner = factory.CreatePeakAligner();
                Console.WriteLine("Alignment started.");
                using (ConsoleLineFilter.SuppressExact("Reading data...")) {
                    result = aligner.Alignment(files, alignmentFile, serializer);
                }
                Console.WriteLine("Alignment finished.");
                result.Save(alignmentFile);
                align_decResults = LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList();
                MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, align_decResults);
            }

            var align_outputfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdalign");
            var align_accessor = new LcmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter, false);
            IQuantValueAccessor CreateQuantAccessor(string exportType) => alignmentLightPeakStore != null
                ? new AlignmentLightQuantValueAccessor(exportType, storage.Parameter, alignmentLightPeakStore)
                : new LegacyQuantValueAccessor(exportType, storage.Parameter);
            IQuantValueAccessor align_quantAccessor = CreateQuantAccessor("Height");
            var align_stats = new[] { StatsValue.Average, StatsValue.Stdev };
            var align_exporter = new AlignmentCSVExporter();
            using var stream = File.Open(align_outputfile, FileMode.Create, FileAccess.Write);
            align_exporter.Export(stream, result.AlignmentSpotProperties, align_decResults, files, new MulticlassFileMetaAccessor(0), align_accessor, align_quantAccessor, align_stats);
            CollectAlignmentLightExportGarbage(isAlignmentLightMode);

            var peakIdOutputFile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdpeakid.tsv");
            using (var peakIdStream = File.Open(peakIdOutputFile, FileMode.Create, FileAccess.Write)) {
                var peakIdExporter = new AlignmentPeakIdMatrixExporter();
                if (alignmentLightPeakStore is null) {
                    peakIdExporter.Export(peakIdStream, result.AlignmentSpotProperties, files);
                }
                else {
                    peakIdExporter.Export(peakIdStream, result.AlignmentSpotProperties, files, alignmentLightPeakStore);
                }
            }
            Console.WriteLine($"Alignment peak ID matrix: {peakIdOutputFile}");

            if (exportAnnotationCandidates) {
                var candidateOutputFile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdcandidate.tsv");
                using (var candidateStream = File.Open(candidateOutputFile, FileMode.Create, FileAccess.Write)) {
                    new AlignmentCandidateExporter(storage.DataBaseMapper, storage.DataBases, storage.Parameter.MachineCategory)
                        .Export(candidateStream, result.AlignmentSpotProperties);
                }
                Console.WriteLine($"Annotation candidates: {candidateOutputFile}");
            }

            if (exportDetailedAlignmentProvenance) {
                var provenanceOutputFile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdprovenance.tsv");
                using (var provenanceStream = File.Open(provenanceOutputFile, FileMode.Create, FileAccess.Write)) {
                    var provenanceExporter = new AlignmentProvenanceExporter();
                    if (alignmentLightPeakStore is null) {
                        provenanceExporter.Export(provenanceStream, result.AlignmentSpotProperties);
                    }
                    else {
                        provenanceExporter.Export(provenanceStream, result.AlignmentSpotProperties, alignmentLightPeakStore);
                    }
                }
                Console.WriteLine($"Detailed alignment provenance: {provenanceOutputFile}");
            }

            // The parameter file offers a family of matrix-export flags and is portable
            // into the GUI, where each means what it says. The Console read exactly one
            // of them, and used it to gate an unrelated artifact: a run that asked for a
            // height matrix got a long-format quality-assurance table and no matrix, with
            // nothing said about either. The flags are honoured here.
            var matrixFolder = String.IsNullOrWhiteSpace(storage.Parameter.ExportFolderPath)
                ? outputFolder
                : storage.Parameter.ExportFolderPath;
            var requestedMatrices = new List<(bool Requested, string ExportType, string Suffix)> {
                (storage.Parameter.IsHeightMatrixExport, "Height", "_Height.txt"),
                (storage.Parameter.IsNormalizedMatrixExport, "Normalized height", "_NormalizedHeight.txt"),
                (storage.Parameter.IsPeakAreaMatrixExport, "Area", "_Area.txt"),
                (storage.Parameter.IsRetentionTimeMatrixExport, "RT", "_RT.txt"),
                (storage.Parameter.IsMassMatrixExport, "MZ", "_MZ.txt"),
                (storage.Parameter.IsSnMatrixExport, "SN", "_SN.txt"),
            };
            if (requestedMatrices.Any(item => item.Requested)) {
                Directory.CreateDirectory(matrixFolder);
                var matrixStats = new[] { StatsValue.Average, StatsValue.Stdev };
                foreach (var (_, exportType, suffix) in requestedMatrices.Where(item => item.Requested)) {
                    var matrixFile = Path.Combine(matrixFolder, alignmentFile.FileName + suffix);
                    using (var matrixStream = File.Open(matrixFile, FileMode.Create, FileAccess.Write)) {
                        new AlignmentCSVExporter().Export(
                            matrixStream, result.AlignmentSpotProperties, align_decResults, files,
                            new MulticlassFileMetaAccessor(0), align_accessor,
                            new LegacyQuantValueAccessor(exportType, storage.Parameter), matrixStats);
                    }
                    Console.WriteLine($"{exportType} matrix: {matrixFile}");
                }
            }

            if (storage.Parameter.IsHeightMatrixExport) {
                var qaOutputFolder = matrixFolder;
                Directory.CreateDirectory(qaOutputFolder);
                var qaOutputFile = Path.Combine(qaOutputFolder, alignmentFile.FileName + ".qa.tsv");
                using var qaStream = File.Open(qaOutputFile, FileMode.Create, FileAccess.Write);
                new AlignmentLongCSVExporter().ExportValueWithFileMetadata(
                    qaStream,
                    result.AlignmentSpotProperties,
                    files,
                    new MulticlassFileMetaAccessor(0),
                    ("Height", CreateQuantAccessor("Height")),
                    ("RT", CreateQuantAccessor("RT")),
                    ("MZ", CreateQuantAccessor("MZ")),
                    ("SN", CreateQuantAccessor("SN")),
                    ("MSMS", CreateQuantAccessor("MSMS")),
                    ("Reference matched", CreateQuantAccessor("Reference matched")));
                // Written beside the height matrix rather than instead of it: it is the
                // same peak heights in long form, with the per-file columns the QA step
                // reads. It follows the height request because no parameter names it.
                Console.WriteLine($"LC-MS quality-assurance matrix: {qaOutputFile}");
            }

            var align_outputmspfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdmsp");
            using var streammsp = File.Open(align_outputmspfile, FileMode.Create, FileAccess.Write);
            IAlignmentSpectraExporter align_mspexporter = new AlignmentMspExporter(storage.DataBaseMapper, storage.Parameter);
            align_mspexporter.BatchExport(streammsp, result.AlignmentSpotProperties, align_decResults);
            CollectAlignmentLightExportGarbage(isAlignmentLightMode);

            var mztabm_filename = alignmentFile.FileName + ".mzTab";
            var mztabm_outputfile = Path.Combine(outputFolder, mztabm_filename);
            var spots = result.AlignmentSpotProperties; // TODO: cancellation
            var msdecs = align_decResults;
            var accessor = align_accessor;
            var mztabM_exporter = new MztabFormatExporter(storage.DataBases, alignmentLightPeakStore);

            using var tabmstream = File.Open(mztabm_outputfile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
            mztabM_exporter.MztabFormatExporterCore(
                tabmstream,
                spots,
                msdecs,
                files,
                accessor,
                align_quantAccessor,
                align_stats,
                mztabm_filename
            );
            CollectAlignmentLightExportGarbage(isAlignmentLightMode);
            if (networkingParameter is not null) {
                Console.WriteLine("Generating molecular network from alignment results.");
                AlignmentMolecularNetworkExporter.Export(result.AlignmentSpotProperties, align_decResults, networkingParameter, files.Count, Path.Combine(outputFolder, "msn"));
            }
            alignmentLightMsdecResults?.Dispose();
        }

        if (isProjectSaved && isAlignmentLightMode) {
            Console.WriteLine("Alignment light mode: project saving (-p) is skipped because GUI-compatible alignment objects are intentionally not serialized.");
        }
        else if (isProjectSaved) {
            storage.Parameter.ProjectParam.FinalSavedDate = DateTime.Now;
            using var stream = File.Open(projectDataStorage.ProjectParameter.FilePath, FileMode.Create);
            using IStreamManager streamManager = new ZipStreamManager(stream, System.IO.Compression.ZipArchiveMode.Create);
            projectDataStorage.Save(streamManager, new MsdialIntegrateSerializer(), file => new DirectoryTreeStreamManager(file), parameter => Console.WriteLine($"Save {parameter.ProjectFileName} failed")).Wait();
            streamManager.Complete();
        }

        return 0;
    }

    private static bool TryResumeMolecularNetworking(
        string outputFolder,
        MolecularSpectrumNetworkingBaseParameter parameter,
        int fileCount) {
        if (!Directory.Exists(outputFolder)) {
            return false;
        }
        var candidates = Directory.EnumerateFiles(outputFolder, "AlignResult-*.arf*", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetExtension(path).Equals(".arf", StringComparison.OrdinalIgnoreCase)
                || Path.GetExtension(path).Equals(".arf2", StringComparison.OrdinalIgnoreCase))
            .Where(path => Path.GetFileNameWithoutExtension(path).IndexOf('_') < 0)
            .OrderByDescending(File.GetLastWriteTimeUtc);
        foreach (var candidate in candidates) {
            var logicalPath = candidate.EndsWith(".arf2", StringComparison.OrdinalIgnoreCase)
                ? candidate.Substring(0, candidate.Length - 1)
                : candidate;
            var alignmentFile = new AlignmentFileBean {
                FilePath = logicalPath,
                FileName = Path.GetFileNameWithoutExtension(logicalPath),
                SpectraFilePath = Path.ChangeExtension(logicalPath, ".dcl"),
            };
            if (!File.Exists(alignmentFile.SpectraFilePath)) {
                continue;
            }
            try {
                var result = AlignmentResultContainer.Load(alignmentFile);
                if (result?.AlignmentSpotProperties is null) {
                    continue;
                }
                MsdecResultsReader.GetSeekPointers(alignmentFile.SpectraFilePath, out _, out var pointers, out _);
                using var loader = new MSDecLoader(alignmentFile.SpectraFilePath, new List<string>());
                var spectra = new List<MSDecResult>(result.AlignmentSpotProperties.Count);
                foreach (var spot in result.AlignmentSpotProperties) {
                    if (spot.MasterAlignmentID < 0 || spot.MasterAlignmentID >= pointers.Count) {
                        throw new InvalidDataException($"Alignment spectrum ID {spot.MasterAlignmentID} is out of range.");
                    }
                    spectra.Add(loader.LoadMSDecResult(spot.MasterAlignmentID)
                        ?? throw new InvalidDataException($"Alignment spectrum ID {spot.MasterAlignmentID} could not be loaded."));
                }
                Console.WriteLine($"Resume: valid alignment result found; generating molecular network from {Path.GetFileName(candidate)}.");
                AlignmentMolecularNetworkExporter.Export(
                    result.AlignmentSpotProperties,
                    spectra,
                    parameter,
                    fileCount,
                    Path.Combine(outputFolder, "msn"));
                return true;
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Resume: ignored invalid alignment result {Path.GetFileName(candidate)}: {ex.Message}");
            }
        }
        return false;
    }

    private static bool HasValidAnalysisResults(IReadOnlyList<AnalysisFileBean> files) {
        if (files.Count == 0) {
            return false;
        }
        foreach (var file in files) {
            var peakPath = File.Exists(file.PeakAreaBeanInformationFilePath)
                ? file.PeakAreaBeanInformationFilePath
                : file.PeakAreaBeanInformationFilePath + "2";
            if (!File.Exists(peakPath) || !File.Exists(file.DeconvolutionFilePath)) {
                return false;
            }
            try {
                var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
                MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out _, out var pointers, out _);
                if (peaks is null || peaks.Any(peak => peak.GetMSDecResultID() < 0 || peak.GetMSDecResultID() >= pointers.Count)) {
                    return false;
                }
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Resume: invalid analysis result for {file.AnalysisFileName}: {ex.Message}");
                return false;
            }
        }
        return true;
    }

    private static IEnumerable<AlignmentSpotProperty> FlattenSpots(IEnumerable<AlignmentSpotProperty> spots) {
        foreach (var spot in spots) {
            yield return spot;
            foreach (var driftSpot in spot.AlignmentDriftSpotFeatures.OrEmptyIfNull()) {
                yield return driftSpot;
            }
        }
    }

    private static IProgress<int> CreateConsoleProgressReporter(string label) {
        var sync = new object();
        var lastReported = -1;
        var nextBucket = 0;
        return new Progress<int>(value => {
            var percent = Math.Max(0, Math.Min(100, value));
            lock (sync) {
                if (percent == lastReported) {
                    return;
                }
                if (percent < 100 && percent < nextBucket) {
                    return;
                }
                Console.WriteLine($"{label} progress: {percent}%");
                lastReported = percent;
                nextBucket = percent < 100 ? Math.Min(100, ((percent / 10) + 1) * 10) : 101;
            }
        });
    }

    private static void CollectAlignmentLightExportGarbage(bool isAlignmentLightMode) {
        if (!isAlignmentLightMode) {
            return;
        }
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialLcmsParameter> storage, IReadOnlyList<AlignmentSpotProperty>? spots) {
        var files = storage.AnalysisFiles;

        var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
        foreach (var file in files) {
            MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
            pointerss.Add((version, pointers, isAnnotationInfo));
        }

        var streams = new List<FileStream>();
        try {
            streams = files.Select(file => File.OpenRead(file.DeconvolutionFilePath)).ToList();
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
