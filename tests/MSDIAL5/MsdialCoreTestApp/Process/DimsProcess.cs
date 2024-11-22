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
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialDimsCore.Export;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialIntegrate.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process;

public sealed class DimsProcess {
    public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
        var param = ConfigParser.ReadForDimsParameter(methodFile);
        var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
        if (!isCorrectlyImported) {
            return -1;
        }
        CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
            out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode, out var lbmDB);

        var container = new MsdialDimsDataStorage() {
            AnalysisFiles = analysisFiles,
            AlignmentFiles = [alignmentFile],
            MspDB = mspDB,
            TextDB = txtDB,
            IsotopeTextDB = isotopeTextDB,
            IupacDatabase = iupacDB,
            MsdialDimsParameter = param
        };

        var dbStorage = DataBaseStorage.CreateEmpty();
        if (mspDB.Count > 0) {
            var database = new MoleculeDataBase(mspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB);
            var mspAnnotator = new DimsMspAnnotator(database, param.MspSearchParam, param.TargetOmics, "MspDB", 1);
            dbStorage.AddMoleculeDataBase(database, [
                new MetabolomicsAnnotatorParameterPair(mspAnnotator.Save(), new AnnotationQueryWithoutIsotopeFactory(mspAnnotator, param.MspSearchParam)),
            ]);
        }
        if (lbmDB.Count > 0) {
            var database = new MoleculeDataBase(lbmDB, "LbmDB", DataBaseSource.Lbm, SourceType.MspDB);
            var annotator = new DimsMspAnnotator(database, param.MspSearchParam, param.TargetOmics, "LbmDB", 1);
            dbStorage.AddMoleculeDataBase(database, [
                new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryWithoutIsotopeFactory(annotator, param.LbmSearchParam)),
            ]);
        }
        if (txtDB.Count > 0) {
            var database = new MoleculeDataBase(txtDB, "TextDB", DataBaseSource.Text, SourceType.TextDB);
            var textAnnotator = new DimsTextDBAnnotator(database, param.TextDbSearchParam, "TextDB", 2);
            dbStorage.AddMoleculeDataBase(database, [
                new MetabolomicsAnnotatorParameterPair(textAnnotator.Save(), new AnnotationQueryWithoutIsotopeFactory(textAnnotator, param.TextDbSearchParam)),
            ]);
        }
        container.DataBases = dbStorage;
        container.DataBaseMapper = dbStorage.CreateDataBaseMapper();

        var providerFactory = new StandardDataProviderFactory();
        Console.WriteLine("Start processing..");
        return ExecuteAsync(container, providerFactory, outputFolder, isProjectSaved).Result;
    }

    private async Task<int> ExecuteAsync(MsdialDimsDataStorage storage, IDataProviderFactory<AnalysisFileBean> providerFactory, string outputFolder, bool isProjectSaved) {
        var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, Path.ChangeExtension(storage.MsdialDimsParameter.ProjectParam.ProjectFileName, ".mdproject")));
        projectDataStorage.AddStorage(storage);

        var files = storage.AnalysisFiles;
        var mapper = storage.DataBases.CreateDataBaseMapper();
        var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);

        var annotationProcess = new StandardAnnotationProcess(
            storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories,
            evaluator,
            mapper);
        var process = new ProcessFile(providerFactory, storage, annotationProcess, evaluator);
        var runner = new ProcessRunner(process, storage.MsdialDimsParameter.NumThreads / 2);
        await runner.RunAllAsync(files, ProcessOption.All, Enumerable.Repeat(default(IProgress<int>?), files.Count), null, default).ConfigureAwait(false);

        IAnalysisExporter<ChromatogramPeakFeatureCollection> peak_MspExporter = new AnalysisMspExporter(storage.DataBaseMapper, storage.MsdialDimsParameter);
        var peak_accessor = new DimsAnalysisMetadataAccessor(storage.DataBaseMapper, storage.MsdialDimsParameter, ExportspectraType.deconvoluted);
        var peakExporterFactory = new AnalysisCSVExporterFactory("\t");
        var sem = new SemaphoreSlim(Environment.ProcessorCount / 2);
        var tasks = new Task[files.Count];
        foreach ((var file, var idx) in files.WithIndex()) {
            tasks[idx] = Task.Run(async () => {
                await sem.WaitAsync();
                try {
                    var peak_container = await ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).ConfigureAwait(false);

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

        if (storage.MsdialDimsParameter.TogetherWithAlignment) {
            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
            var alignmentFile = storage.AlignmentFiles.First();
            var factory = new DimsAlignmentProcessFactory(storage, evaluator);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = providerFactory;
            var result = aligner.Alignment(files, alignmentFile, serializer);
            result.Save(alignmentFile);
            var align_decResults = LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList();
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, align_decResults);

            var align_accessor = new DimsMetadataAccessor(storage.DataBaseMapper, storage.MsdialDimsParameter, false);
            var align_quantAccessor = new LegacyQuantValueAccessor("Height", storage.MsdialDimsParameter);
            var align_stats = new[] { StatsValue.Average, StatsValue.Stdev };
            var align_exporter = new AlignmentCSVExporter();
            var align_outputfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdalign");
            using var stream = File.Open(align_outputfile, FileMode.Create, FileAccess.Write);
            align_exporter.Export(stream, result.AlignmentSpotProperties, align_decResults, files, new MulticlassFileMetaAccessor(0), align_accessor, align_quantAccessor, align_stats);

            IAlignmentSpectraExporter align_mspexporter = new AlignmentMspExporter(storage.DataBaseMapper, storage.MsdialDimsParameter);
            var align_outputmspfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdmsp");
            using var streammsp = File.Open(align_outputmspfile, FileMode.Create, FileAccess.Write);
            align_mspexporter.BatchExport(streammsp, result.AlignmentSpotProperties, align_decResults);
        }

        if (isProjectSaved) {
            storage.MsdialDimsParameter.ProjectParam.MsdialVersionNumber = $"Msdial console {Resources.VERSION}";
            storage.MsdialDimsParameter.ProjectParam.FinalSavedDate = DateTime.Now;
            using var stream = File.Open(projectDataStorage.ProjectParameter.FilePath, FileMode.Create);
            using IStreamManager streamManager = new ZipStreamManager(stream, System.IO.Compression.ZipArchiveMode.Create);
            projectDataStorage.Save(streamManager, new MsdialIntegrateSerializer(), file => new DirectoryTreeStreamManager(file), parameter => Console.WriteLine($"Save {parameter.ProjectFileName} failed")).Wait();
            streamManager.Complete();
        }

        return 0;
    }

    private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialDimsParameter> storage, IReadOnlyList<AlignmentSpotProperty> spots) {
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
