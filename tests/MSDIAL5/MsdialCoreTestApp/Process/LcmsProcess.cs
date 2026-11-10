using CompMs.Common;
using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Properties;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process;

public sealed class LcmsProcess
{
    /// <summary>
    /// Writes one file's .mdpeak and .mdmsp. Both take the deconvolution results of
    /// <see cref="RepresentativeDeconvolutionReader.LoadPerFileResults"/>, so a multi-energy AIF file exports
    /// each peak at the energy of its representative annotation, or else at the energy whose spectrum has
    /// the most product ions, and never from an unsuffixed .dcl an earlier run left under the same name.
    /// </summary>
    internal static void ExportPeakFile(
        AnalysisFileBean file,
        ChromatogramPeakFeatureCollection peaks,
        string outputFolder,
        AnalysisCSVExporterFactory peakExporterFactory,
        IDataProviderFactory<AnalysisFileBean> providerFactory,
        IAnalysisMetadataAccessor peakAccessor,
        IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?> refer,
        ParameterBase parameter) {
        var msdecResults = RepresentativeDeconvolutionReader.LoadPerFileResults(file, peaks.Items);

        var peak_outputfile = Path.Combine(outputFolder, file.AnalysisFileName + ".mdpeak");
        using (var stream = File.Open(peak_outputfile, FileMode.Create, FileAccess.Write)) {
            peakExporterFactory.CreateExporter(providerFactory, peakAccessor, _ => msdecResults).Export(stream, file, peaks, new ExportStyle());
        }

        var peak_outputmspfile = Path.Combine(outputFolder, file.AnalysisFileName + ".mdmsp");
        using var mspstream = File.Open(peak_outputmspfile, FileMode.Create, FileAccess.Write);
        IAnalysisExporter<ChromatogramPeakFeatureCollection> peak_MspExporter = new AnalysisMspExporter(refer, parameter, _ => new LoadedMSDecResults(msdecResults));
        peak_MspExporter.Export(mspstream, file, peaks, new ExportStyle());
    }

    // Indexes the results as MSDecLoader does, by IChromatogramPeak.ID (MasterPeakID).
    private sealed class LoadedMSDecResults : IMsScanPropertyLoader<ChromatogramPeakFeature>
    {
        private readonly IReadOnlyList<MSDecResult> _results;

        public LoadedMSDecResults(IReadOnlyList<MSDecResult> results) {
            _results = results;
        }

        public IMSScanProperty Load(ChromatogramPeakFeature source) => _results[source.MasterPeakID];
    }

    public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz)
    {
        var param = ConfigParser.ReadForLcmsParameter(methodFile);
        var isAlignmentLightMode = ConfigParser.ReadAlignmentLightMode(methodFile);
        var exportDetailedAlignmentProvenance = ConfigParser.ReadDetailedAlignmentProvenance(methodFile);
        var exportAnnotationCandidates = ConfigParser.ReadAnnotationCandidateExport(methodFile);
        var automaticAlignmentRtCorrection = param.AlignmentBaseParam.AutomaticRtCorrection;
        if (AutomaticAlignmentRtCorrectionWorkflow.ExclusiveWithLegacyRtCorrection(param) is string exclusive) {
            Console.Error.WriteLine(exclusive);
            return -1;
        }
        if (automaticAlignmentRtCorrection.Execute && !param.TogetherWithAlignment) {
            Console.Error.WriteLine("Automatic alignment RT correction requires Together with alignment: True.");
            return -1;
        }
        var automaticRtCorrectionPreviewOnly = ConfigParser.ReadAutomaticRtCorrectionPreviewOnly(methodFile, out var unusablePreviewValue);
        if (unusablePreviewValue is not null) {
            // Read as False, as an unusable value of any other setting is, this would turn the
            // preview someone asked for into the production run they meant to review first.
            Console.Error.WriteLine($"'Automatic RT correction preview only: {unusablePreviewValue}' is neither True nor False; the run is refused rather than run in full.");
            return -1;
        }
        if (automaticRtCorrectionPreviewOnly && !automaticAlignmentRtCorrection.Execute) {
            Console.Error.WriteLine("Automatic RT correction preview only requires Execute automatic RT correction for alignment: True.");
            return -1;
        }
        // The settings and the anchor library are checked before anything heavy is read, as the
        // RT correction library below is; the library is handed to the correction once the peaks
        // are picked.
        var automaticRtCorrectionInputs = AutomaticAlignmentRtCorrectionWorkflow.PrepareInputs(param, ConsoleRtCorrectionReporter.Instance);
        if (automaticRtCorrectionInputs is null) {
            return -1;
        }
        // The RT correction library is checked here, before the analysis files, the annotation
        // libraries and the raw data are read: it is small and easy to get wrong, and a mistake
        // in it should stop the run in seconds. Prepare uses what this loaded.
        List<MoleculeMsReference> rtCorrectionStandards = [];
        if (param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection) {
            try {
                rtCorrectionStandards = RetentionTimeCorrectionProcess.LoadStandards(param);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"RT correction library could not be used: {ex.Message}");
                return -1;
            }
        }
        else if (!param.CompoundListForRtCorrectionPath.IsEmptyOrNull()) {
            // Not read and not checked: the library has no effect without RT correction. A path
            // left in place usually means the switch was meant to be on, so it is said out loud.
            Console.WriteLine($"Warning: 'Compounds library file path for RT correction' is set ({param.CompoundListForRtCorrectionPath}) but 'Execute RT correction' is False, so the library is not used.");
        }
        // The MSP annotator table is read here for the same reason: a value it refuses should stop
        // the run before the analysis files and the raw data are read. The libraries it names are
        // read later, with the others. A preview searches no library, so it does not read the table.
        List<MspAnnotatorSetting> mspAnnotatorSettings = [];
        if (!automaticRtCorrectionPreviewOnly) {
            try {
                mspAnnotatorSettings = ConfigParser.ReadMspAnnotatorSettings(methodFile, param);
            }
            catch (FormatException ex) {
                Console.Error.WriteLine($"MSP annotator settings cannot be used: {ex.Message}");
                return -1;
            }
        }
        var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
        if (!isCorrectlyImported) {
            return -1;
        }

        try {
            RetentionTimeCorrectionProcess.Prepare(analysisFiles, param, rtCorrectionStandards, outputFolder);
        }
        catch (Exception ex) {
            Console.Error.WriteLine($"RT correction failed: {ex}");
            return -1;
        }

        // An earlier run's audit of the automatic correction must not stay in this run's output
        // folder looking like this run's, whether this run corrects or fails on the way. It is
        // removed only now that this run has its files, so a run that stops at the import leaves
        // it alone; and a file someone has open is said before anything is removed.
        if (!RemoveEarlierAudit(outputFolder)) {
            return -1;
        }

        if (automaticRtCorrectionPreviewOnly) {
            // The anchors come from the picked peaks and the raw data alone, so the preview neither
            // reads nor searches the annotation libraries.
            var previewStorage = new MsdialLcmsDataStorage() {
                AnalysisFiles = analysisFiles,
                AlignmentFiles = [alignmentFile],
                IsotopeTextDB = [],
                IupacDatabase = CompMs.Common.Parser.IupacResourceParser.GetIUPACDatabase(),
                MsdialLcmsParameter = param
            };
            Console.WriteLine("Automatic alignment RT correction preview: peak picking without deconvolution or annotation, then anchor selection; nothing is aligned, and nothing is saved beside the raw data.");
            return PreviewAutomaticRtCorrection(previewStorage, outputFolder, automaticRtCorrectionInputs);
        }

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
                var annotator = new LcmsMspAnnotator(mspDB.DataBase, setting.SearchParameter, targetOmics, setting.AnnotatorId, setting.Priority, setting.AllowsSuggestions);
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
        return ExecuteAsync(container, outputFolder, isProjectSaved, isAlignmentLightMode, exportDetailedAlignmentProvenance, exportAnnotationCandidates, automaticRtCorrectionInputs).Result;
    }

    /// <summary>
    /// Picks every file's peaks, chooses the automatic alignment RT correction's anchors and
    /// writes its audit, and stops: the run a person reviews before the run that aligns.
    /// </summary>
    /// <remarks>
    /// The peaks are picked as the full run picks them (PeakPickProcess, the same code and
    /// settings), so the automatic anchors are the ones the full run will choose. Nothing reads
    /// the deconvolution or the annotation, so neither is run, and nothing is saved beside the
    /// raw data: each file's peaks go to a scratch folder inside the output folder, one file at a
    /// time so that the peak lists of a large batch are not all held in memory, and the folder
    /// is removed once the audit is written. Each file's raw data is read once, for its peaks,
    /// its MS1 scan times and its user anchors.
    /// </remarks>
    private static int PreviewAutomaticRtCorrection(
        IMsdialDataStorage<MsdialLcmsParameter> storage,
        string outputFolder,
        AutomaticRtCorrectionInputs inputs) {
        var files = storage.AnalysisFiles;
        var parameter = storage.Parameter;
        var correction = parameter.AlignmentBaseParam.AutomaticRtCorrection;
        var anchors = correction.AnchorMode == AutomaticRtCorrectionAnchorMode.Automatic
            ? new List<MoleculeMsReference>()
            : AutomaticRtCorrectionAnchorLibrary.Anchors(inputs.AnchorLibrary);
        var providerFactory = new StandardDataProviderFactory(5, false);
        var picker = new PeakPickProcess(storage);
        var readings = new ConcurrentDictionary<int, AutomaticRtCorrectionRawFileReading>();
        // A name of its own, so two previews into one output folder never share or delete each
        // other's peaks.
        var scratch = Path.Combine(outputFolder, "automatic_alignment_rt_correction_preview_peaks_" + Guid.NewGuid().ToString("N"));
        string PeaksOf(AnalysisFileBean file) => Path.Combine(scratch, file.AnalysisFileId.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".pai");
        var completed = 0;
        try {
            Directory.CreateDirectory(scratch);
            try {
                // Files in parallel as ProcessRunner takes them, Math.Max(1, ...) for the reason
                // ExecuteAsync gives.
                Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parameter.NumThreads / 2) }, file => {
                    var provider = providerFactory.Create(file);
                    MsdialPeakSerializer.SaveChromatogramPeakFeatures(PeaksOf(file), picker.Pick(file, provider, null, default).Items.ToList());
                    readings[file.AnalysisFileId] = AutomaticAlignmentRtCorrectionWorkflow.ReadRawFile(file, provider, parameter, anchors);
                    Console.WriteLine($"Automatic alignment RT correction preview: {Interlocked.Increment(ref completed)}/{files.Count} files read ({file.AnalysisFileName}).");
                });
            }
            catch (AggregateException ex) {
                Console.Error.WriteLine($"Automatic alignment RT correction preview failed: {ex.InnerException?.Message ?? ex.Message}");
                return -1;
            }

            var result = AutomaticAlignmentRtCorrectionWorkflow.Build(
                storage.AnalysisFiles,
                storage.Parameter,
                inputs,
                outputFolder,
                file => MsdialPeakSerializer.LoadChromatogramPeakFeatures(PeaksOf(file)),
                (file, given) => given.SequenceEqual(anchors)
                    ? readings[file.AnalysisFileId]
                    : throw new InvalidOperationException("The preview read other anchors than the correction asked for."),
                "Preview",
                ConsoleRtCorrectionReporter.Instance);
            if (result is null) {
                return -1;
            }
            var reference = result.Files.Single(file => file.FileId == result.Correction.ReferenceFileId);
            var used = result.Anchors.Where(anchor => anchor.FileId == reference.FileId && anchor.Used).ToList();
            Console.WriteLine($"Automatic alignment RT correction preview: {used.Count} anchor(s) in reference file {reference.FileName} "
                + $"({used.Count(anchor => anchor.Source == AutomaticRtCorrectionAnchorSource.User)} user, {used.Count(anchor => anchor.Source == AutomaticRtCorrectionAnchorSource.Automatic)} automatic). "
                + "Review the audit, then run again without 'Automatic RT correction preview only'.");
            return 0;
        }
        finally {
            RemoveScratch(scratch);
        }
    }

    // Best effort: a virus scanner or the indexer can hold a file just written for a moment, and
    // a folder left behind must not turn the preview's result into a failure.
    private static void RemoveScratch(string scratch) {
        for (var attempt = 1; Directory.Exists(scratch); attempt++) {
            try {
                Directory.Delete(scratch, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                if (attempt >= 5) {
                    Console.WriteLine($"Warning: the preview's scratch folder {scratch} could not be removed ({ex.Message}); it may be deleted by hand.");
                    return;
                }
                Thread.Sleep(200 * attempt);
            }
        }
    }

    // Removes an earlier run's automatic RT correction audit from the output folder; false, with
    // the file named, when one is open elsewhere (an audit under review in a spreadsheet), in
    // which case nothing is removed.
    private static bool RemoveEarlierAudit(string outputFolder) {
        var names = new[] {
            AutomaticAlignmentRetentionTimeCorrectionResult.SummaryFileName,
            AutomaticAlignmentRetentionTimeCorrectionResult.AnchorsFileName,
            AutomaticAlignmentRetentionTimeCorrectionResult.ExclusionsFileName,
        };
        foreach (var path in names.Select(name => Path.Combine(outputFolder, name)).Where(File.Exists)) {
            try {
                using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) {
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
                Console.Error.WriteLine($"The earlier automatic RT correction audit {path} cannot be replaced: {ex.Message} Close it and run again.");
                return false;
            }
        }
        try {
            var removed = AutomaticAlignmentRetentionTimeCorrectionResult.RemoveAudit(outputFolder);
            if (removed > 0) {
                Console.WriteLine($"Removed {removed} automatic alignment RT correction audit file(s) of an earlier run from the output folder.");
            }
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) {
            Console.Error.WriteLine($"The earlier automatic RT correction audit in {outputFolder} cannot be replaced: {ex.Message}");
            return false;
        }
    }

    // The Console's voice for the shared automatic alignment RT correction: what it says goes to
    // the standard output, why it refuses or fails to the standard error, as it always did.
    private sealed class ConsoleRtCorrectionReporter : IAutomaticRtCorrectionReporter
    {
        public static ConsoleRtCorrectionReporter Instance { get; } = new ConsoleRtCorrectionReporter();

        public void Info(string line) => Console.WriteLine(line);

        public void Error(string line) => Console.Error.WriteLine(line);
    }

    private async Task<int> ExecuteAsync(
        IMsdialDataStorage<MsdialLcmsParameter> storage,
        string outputFolder,
        bool isProjectSaved,
        bool isAlignmentLightMode,
        bool exportDetailedAlignmentProvenance,
        bool exportAnnotationCandidates,
        AutomaticRtCorrectionInputs automaticRtCorrectionInputs) {
        var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, Path.ChangeExtension(storage.Parameter.ProjectParam.ProjectFileName, ".mdproject")));
        projectDataStorage.AddStorage(storage);

        var files = storage.AnalysisFiles;
        var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
        var annotationProcess = new StandardAnnotationProcess(storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories, evaluator, storage.DataBaseMapper);
        var providerFactory = new StandardDataProviderFactory(5, false);
        var process = new FileProcess(providerFactory, storage, annotationProcess, evaluator);
        // Math.Max(1, ...) as the GUI does (LcmsMethodModel.cs and its four siblings all use
        // Math.Max(1, UsableNumThreads / 2)). Without it "number of threads: 1" -- a value
        // ConfigParser accepts -- gives 1/2 = 0 workers, ProcessRunner builds an empty Task array,
        // and RunAllAsync returns having peak-picked nothing. The export stage then either crashes
        // on a missing .pai or, when the raw data already carries .pai files from an earlier run,
        // SILENTLY EXPORTS THE PREVIOUS RUN'S PEAKS AND PEAK IDS under this run's parameters,
        // library list and version string. That is the join-breaking case: the artifacts describe a
        // run that never happened.
        var runner = new ProcessRunner(process, Math.Max(1, storage.Parameter.NumThreads / 2));
        await runner.RunAllAsync(files, ProcessOption.All, Enumerable.Repeat(default(IProgress<int>?), files.Count), null, default).ConfigureAwait(false);

        var peak_accessor = new LcmsAnalysisMetadataAccessor(storage.DataBaseMapper, storage.Parameter, ExportspectraType.deconvoluted);
        var peakExporterFactory = new AnalysisCSVExporterFactory("\t");
        var sem = new SemaphoreSlim(Environment.ProcessorCount / 2);
        var tasks = new Task[files.Count];
        for (int i = 0; i < files.Count; i++) {
            var file = files[i];
            tasks[i] = Task.Run(async () => {
                await sem.WaitAsync();
                try {
                    var peak_container = await file.LoadChromatogramPeakFeatureCollectionAsync().ConfigureAwait(false);
                    ExportPeakFile(file, peak_container, outputFolder, peakExporterFactory, providerFactory, peak_accessor, storage.DataBaseMapper, storage.Parameter);
                }
                finally {
                    sem.Release();
                }
            });
        }
        await Task.WhenAll(tasks);

        AutomaticAlignmentRetentionTimeCorrectionResult? automaticRtCorrectionResult = null;
        if (storage.Parameter.TogetherWithAlignment && storage.Parameter.AlignmentBaseParam.AutomaticRtCorrection.Execute) {
            automaticRtCorrectionResult = AutomaticAlignmentRtCorrectionWorkflow.Build(
                storage.AnalysisFiles,
                storage.Parameter,
                automaticRtCorrectionInputs,
                outputFolder,
                file => MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath),
                (file, anchors) => AutomaticAlignmentRtCorrectionWorkflow.ReadRawFile(file, providerFactory.Create(file), storage.Parameter, anchors),
                "Run",
                ConsoleRtCorrectionReporter.Instance);
            if (automaticRtCorrectionResult is null) {
                return -1;
            }
            storage.Parameter.AlignmentReferenceFileID = automaticRtCorrectionResult.Correction.ReferenceFileId;
        }

        // The identity of the build, without the host application's name: this field is the
        // VERSION, and mzTab-M already wraps it as "MS-DIAL, <this>" -- "MS-DIAL, Msdial console
        // 5.5.241113" said the application twice and the version once, staleness included.
        storage.Parameter.ProjectParam.MsdialVersionNumber = MsdialBuildIdentity.FullIdentity;
        if (storage.Parameter.TogetherWithAlignment) {
            var alignmentFile = storage.AlignmentFiles.First();
            using var alignmentLightPeakStore = isAlignmentLightMode ? AlignmentLightPeakStore.CreateTemp() : null;
            AlignmentResultContainer result;
            IReadOnlyList<MSDecResult> align_decResults;
            IDisposable? alignmentLightMsdecResults = null;
            LipidRtUnitRun? lipidRtRun;
            IReadOnlyList<RemovedIsotopeSpot>? removedIsotopeSpots;
            if (isAlignmentLightMode) {
                Console.WriteLine("Alignment light mode: streaming peak matrix, file-backed alignment deconvolution access, GUI chromatogram serialization, GUI alignment object serialization, and ion-abundance correlation links are disabled; text exports remain enabled.");
                Console.WriteLine("Alignment started.");
                var lightRunner = new LcmsAlignmentLightRunner(
                    storage,
                    evaluator,
                    providerFactory,
                    CreateConsoleProgressReporter("Alignment"),
                    automaticRtCorrectionResult?.Correction);
                LcmsAlignmentLightResult lightResult;
                using (ConsoleLineFilter.SuppressExact("Reading data...")) {
                    lightResult = lightRunner.Run(files, alignmentFile, alignmentLightPeakStore!);
                }
                result = lightResult.Container;
                align_decResults = lightResult.MsdecResults;
                alignmentLightMsdecResults = lightResult.MsdecResults as IDisposable;
                lipidRtRun = lightResult.LipidRtRun;
                removedIsotopeSpots = lightResult.RemovedIsotopeSpots;
                Console.WriteLine("Alignment finished.");
            }
            else {
                var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
                if (automaticRtCorrectionResult is not null) {
                    Console.WriteLine("Automatic alignment RT correction: GUI EICs retain original sample RT; extraction and gap filling use corrected alignment RT windows mapped back to raw data.");
                }
                var factory = new LcmsAlignmentProcessFactory(storage, evaluator) {
                    AlignmentRtCorrection = automaticRtCorrectionResult?.Correction,
                };
                factory.Progress = CreateConsoleProgressReporter("Alignment");
                var aligner = factory.CreatePeakAligner();
                Console.WriteLine("Alignment started.");
                using (ConsoleLineFilter.SuppressExact("Reading data...")) {
                    result = aligner.Alignment(files, alignmentFile, serializer);
                }
                Console.WriteLine("Alignment finished.");
                lipidRtRun = aligner.LipidRtRun;
                removedIsotopeSpots = aligner.RemovedIsotopeSpots;
                result.Save(alignmentFile);
                align_decResults = LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList();
                MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, align_decResults);
            }
            WriteLipidRtReview(lipidRtRun, outputFolder, alignmentFile.FileName);
            if (removedIsotopeSpots is not null) {
                // Spots whose representative peak is an isotope (M + n) and that are not reference-matched are left out
                // of the alignment result; this list says which, with the ion each was taken as an isotope of.
                var removedIsotopeFile = RemovedIsotopeSpotWriter.PathFor(outputFolder, alignmentFile.FileName);
                RemovedIsotopeSpotWriter.Write(removedIsotopeFile, removedIsotopeSpots);
                Console.WriteLine($"Alignment spots removed as isotopes: {removedIsotopeSpots.Count}, listed in {removedIsotopeFile}");
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
                        ExportAlignmentMatrix(
                            matrixStream, result.AlignmentSpotProperties, align_decResults, files,
                            align_accessor, exportType, storage.Parameter, alignmentLightPeakStore, matrixStats);
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
                    ("Reference matched", CreateQuantAccessor("Reference matched")),
                    (CompMs.MsdialCore.Utility.DataAccess.QaInSilicoMatchedColumn, CreateQuantAccessor(CompMs.MsdialCore.Utility.DataAccess.QaInSilicoMatchedColumn)));
                // Written beside the height matrix rather than instead of it: it is the
                // same peak heights in long form, with the per-file columns the QA step
                // reads. It follows the height request because no parameter names it.
                // "Reference matched" leaves out a peak whose accepted annotation is an
                // in-silico match, which "In-silico matched" marks instead (the author's
                // decision of 2026-10-28: an in-silico match is never an identification).
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

    /// <summary>
    /// Says what the lipid RT check did on the alignment, and writes its review list as
    /// "&lt;alignment stem&gt;_lipidrt_review.tsv" next to the alignment exports.
    /// </summary>
    /// <remarks>
    /// The review list is written whenever the check ran, also with no rows, so that an empty
    /// file means "ran and found nothing to review" and a missing one means "did not run". The
    /// Confirmed tags themselves are in the alignment's tag file and in the Comment column of the
    /// exports, and the verdicts in the mzTab-M opt_global_lipid_rt_* columns.
    /// </remarks>
    internal static string? WriteLipidRtReview(LipidRtUnitRun? run, string outputFolder, string alignmentStem) {
        if (run is null) {
            return null;
        }
        if (run.Result is null) {
            if (run.Error is not null || run.Tags.Withdrawn > 0) {
                Console.WriteLine($"Lipid RT check (alignment): {run.Describe()}");
            }
            return null;
        }
        Console.WriteLine($"Lipid RT check (alignment): {run.Describe()}");
        var items = LipidRtReviewList.Build(run.Result);
        var reviewFile = Path.Combine(outputFolder, alignmentStem + "_lipidrt_review.tsv");
        using (var writer = new StreamWriter(reviewFile, append: false, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false))) {
            LipidRtReviewList.Write(writer, items);
        }
        Console.WriteLine($"Lipid RT check review list: {reviewFile} ({items.Count} rows)");
        return reviewFile;
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

    internal static void ExportAlignmentMatrix(
        Stream stream,
        IReadOnlyList<AlignmentSpotProperty> spots,
        IReadOnlyList<MSDecResult> msdecResults,
        IReadOnlyList<AnalysisFileBean> files,
        IMetadataAccessor metadataAccessor,
        string exportType,
        ParameterBase parameter,
        AlignmentLightPeakStore? alignmentLightPeakStore,
        IReadOnlyList<StatsValue> stats) {
        IQuantValueAccessor quantAccessor = alignmentLightPeakStore is null
            ? new LegacyQuantValueAccessor(exportType, parameter)
            : new AlignmentLightQuantValueAccessor(exportType, parameter, alignmentLightPeakStore);
        new AlignmentCSVExporter().Export(
            stream, spots, msdecResults, files,
            new MulticlassFileMetaAccessor(0), metadataAccessor, quantAccessor, stats);
    }

    private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialLcmsParameter> storage, IReadOnlyList<AlignmentSpotProperty>? spots) {
        using var reader = new RepresentativeDeconvolutionReader(storage.AnalysisFiles);
        foreach (var spot in spots.OrEmptyIfNull()) {
            yield return reader.Read(spot.AlignedPeakProperties[spot.RepresentativeFileID]);
        }
    }
}
