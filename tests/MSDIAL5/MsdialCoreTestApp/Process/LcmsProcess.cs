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
    {
        var param = ConfigParser.ReadForLcmsParameter(methodFile);
        var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
        if (!isCorrectlyImported) {
            return -1;
        }

        CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
            out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, 
            out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode,
            out List<MoleculeMsReference> lbmDB);

        var container = new MsdialLcmsDataStorage() {
            AnalysisFiles = analysisFiles,
            AlignmentFiles = [alignmentFile],
            IsotopeTextDB = isotopeTextDB,
            IupacDatabase = iupacDB,
            MsdialLcmsParameter = param
        };

        var dbStorage = DataBaseStorage.CreateEmpty();
        if (mspDB.Count > 0) {
            var database = new MoleculeDataBase(mspDB, param.MspFilePath, DataBaseSource.Msp, SourceType.MspDB);
            var annotator = new LcmsMspAnnotator(database, param.MspSearchParam, param.TargetOmics, param.MspFilePath, 1);
            dbStorage.AddMoleculeDataBase(database, [
                new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, param.PeakPickBaseParam, param.MspSearchParam, ignoreIsotopicPeak: true)),
            ]);
        }
        if (lbmDB.Count > 0) {
            var lbmDatabase = new MoleculeDataBase(lbmDB, param.LbmFilePath, DataBaseSource.Lbm, SourceType.MspDB);
            var lbmAnnotator = new LcmsMspAnnotator(lbmDatabase, param.LbmSearchParam, param.TargetOmics, param.LbmFilePath, 1);
            dbStorage.AddMoleculeDataBase(lbmDatabase, [
                new MetabolomicsAnnotatorParameterPair(lbmAnnotator.Save(), new AnnotationQueryFactory(lbmAnnotator, param.PeakPickBaseParam, param.LbmSearchParam, ignoreIsotopicPeak: true)),
            ]);
        }
        if (txtDB.Count > 0) {
            var textdatabase = new MoleculeDataBase(txtDB, param.TextDBFilePath, DataBaseSource.Text, SourceType.TextDB);
            var textannotator = new LcmsTextDBAnnotator(textdatabase, param.TextDbSearchParam, param.TextDBFilePath, 2);
            dbStorage.AddMoleculeDataBase(textdatabase, [
                new MetabolomicsAnnotatorParameterPair(textannotator.Save(), new AnnotationQueryFactory(textannotator, param.PeakPickBaseParam, param.TextDbSearchParam, ignoreIsotopicPeak: false)),
            ]);
        }
        container.DataBases = dbStorage;
        container.DataBaseMapper = dbStorage.CreateDataBaseMapper();

        Console.WriteLine("Start processing..");
        return ExecuteAsync(container, outputFolder, isProjectSaved).Result;
    }

    private async Task<int> ExecuteAsync(IMsdialDataStorage<MsdialLcmsParameter> storage, string outputFolder, bool isProjectSaved) {
        var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, Path.ChangeExtension(storage.Parameter.ProjectParam.ProjectFileName, ".mdproject")));
        projectDataStorage.AddStorage(storage);

        var files = storage.AnalysisFiles;
        var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
        var annotationProcess = new StandardAnnotationProcess(storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories, evaluator, storage.DataBaseMapper);
        var providerFactory = new StandardDataProviderFactory(5, false);
        var process = new FileProcess(providerFactory, storage, annotationProcess, evaluator);
        var runner = new ProcessRunner(process, storage.Parameter.NumThreads / 2);
        await runner.RunAllAsync(files, ProcessOption.All, Enumerable.Repeat(default(IProgress<int>?), files.Count), null, default).ConfigureAwait(false);

        IAnalysisExporter<ChromatogramPeakFeatureCollection> peak_MspExporter = new AnalysisMspExporter(storage.DataBaseMapper, storage.Parameter);
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

        if (storage.Parameter.TogetherWithAlignment) {
            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
            var alignmentFile = storage.AlignmentFiles.First();
            var factory = new LcmsAlignmentProcessFactory(storage, evaluator);
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(files, alignmentFile, serializer);
            result.Save(alignmentFile);
            var align_decResults = LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList();
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, align_decResults);

            var align_outputfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdalign");
            var align_accessor = new LcmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter, false);
            var align_quantAccessor = new LegacyQuantValueAccessor("Height", storage.Parameter);
            var align_stats = new[] { StatsValue.Average, StatsValue.Stdev };
            var align_exporter = new AlignmentCSVExporter();
            using var stream = File.Open(align_outputfile, FileMode.Create, FileAccess.Write);
            align_exporter.Export(stream, result.AlignmentSpotProperties, align_decResults, files, new MulticlassFileMetaAccessor(0), align_accessor, align_quantAccessor, align_stats);

            var align_outputmspfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdmsp");
            using var streammsp = File.Open(align_outputmspfile, FileMode.Create, FileAccess.Write);
            IAlignmentSpectraExporter align_mspexporter = new AlignmentMspExporter(storage.DataBaseMapper, storage.Parameter);
            align_mspexporter.BatchExport(streammsp, result.AlignmentSpotProperties, align_decResults);
        }

        if (isProjectSaved) {
            storage.Parameter.ProjectParam.MsdialVersionNumber = $"Msdial console {Resources.VERSION}";
            storage.Parameter.ProjectParam.FinalSavedDate = DateTime.Now;
            using var stream = File.Open(projectDataStorage.ProjectParameter.FilePath, FileMode.Create);
            using IStreamManager streamManager = new ZipStreamManager(stream, System.IO.Compression.ZipArchiveMode.Create);
            projectDataStorage.Save(streamManager, new MsdialIntegrateSerializer(), file => new DirectoryTreeStreamManager(file), parameter => Console.WriteLine($"Save {parameter.ProjectFileName} failed")).Wait();
            streamManager.Complete();
        }

        return 0;
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
