using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Algorithm.Alignment;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Export;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using CompMs.MsdialIntegrate.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CompMs.App.MsdialConsole.Properties;

namespace CompMs.App.MsdialConsole.Process;

public sealed class ImmsProcess
{
    public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
        var param = ConfigParser.ReadForImmsParameter(methodFile);
        var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out var analysisFiles, out var alignmentFile);
        if (!isCorrectlyImported) {
            return -1;
        }

        CommonProcess.ParseLibraries(param, targetMz, out var iupacDB, out var mspDB, out var txtDB, out var isotopeTextDB, out var compoundsInTargetMode, out var lbmDB);

        var container = new MsdialImmsDataStorage
        {
            AnalysisFiles = analysisFiles,
            AlignmentFiles = [alignmentFile],
            MspDB = mspDB,
            TextDB = txtDB,
            IsotopeTextDB = isotopeTextDB,
            IupacDatabase = iupacDB,
            MsdialImmsParameter = param,
        };

        var dbStorage = DataBaseStorage.CreateEmpty();
        if (mspDB.Count > 0) {
            var database = new MoleculeDataBase(mspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB);
            var mspAnnotator = new ImmsMspAnnotator(database, param.MspSearchParam, param.TargetOmics, "MspDB", 1);
            dbStorage.AddMoleculeDataBase(database, [
                new MetabolomicsAnnotatorParameterPair(mspAnnotator.Save(), new AnnotationQueryFactory(mspAnnotator, param.PeakPickBaseParam, param.MspSearchParam, ignoreIsotopicPeak: true)),
            ]);
        }
        if (lbmDB.Count > 0) {
            var database = new MoleculeDataBase(lbmDB, "LbmDB", DataBaseSource.Lbm, SourceType.MspDB);
            var annotator = new ImmsMspAnnotator(database, param.LbmSearchParam, param.TargetOmics, "LbmDB", 1);
            dbStorage.AddMoleculeDataBase(database, [
                new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, param.PeakPickBaseParam, param.LbmSearchParam, ignoreIsotopicPeak: true)),
            ]);
        }
        if (txtDB.Count > 0) {
            var database = new MoleculeDataBase(txtDB, "TextDB", DataBaseSource.Text, SourceType.TextDB);
            var textDBAnnotator = new ImmsTextDBAnnotator(database, param.TextDbSearchParam, "TextDB", 2);
            dbStorage.AddMoleculeDataBase(database, [
                new MetabolomicsAnnotatorParameterPair(textDBAnnotator.Save(), new AnnotationQueryFactory(textDBAnnotator, param.PeakPickBaseParam, param.TextDbSearchParam, ignoreIsotopicPeak: false))
            ]);
        }
        container.DataBases = dbStorage;
        container.DataBaseMapper = dbStorage.CreateDataBaseMapper();

        Console.WriteLine("Start processing..");
        return ExecuteAsync(container, outputFolder, isProjectSaved).Result;
    }

    private async Task<int> ExecuteAsync(IMsdialDataStorage<MsdialImmsParameter> storage, string outputFolder, bool isProjectSaved) {
        var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, Path.ChangeExtension(storage.Parameter.ProjectParam.ProjectFileName, ".mdproject")));
        projectDataStorage.AddStorage(storage);

        var files = storage.AnalysisFiles;
        var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
        var providerFactory = new ImmsAverageDataProviderFactory(0.001, 0.002, 5, false);
        var factories = storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories;

        // temporary
        ImmsMspAnnotator? mspAnnotator = null;
        var mspdb = storage.DataBases.MetabolomicsDataBases.FirstOrDefault(d => d.DataBase.DataBaseSource == DataBaseSource.Msp);
        if (mspdb is { } && mspdb.Pairs.FirstOrDefault() is { } msppair) {
            mspAnnotator = new ImmsMspAnnotator(mspdb.DataBase, msppair.AnnotationQueryFactory.PrepareParameter(), storage.Parameter.TargetOmics, "MspDB", 1);
        }
        ImmsTextDBAnnotator? txtDBAnnotator = null;
        var txtdb = storage.DataBases.MetabolomicsDataBases.FirstOrDefault(d => d.DataBase.DataBaseSource == DataBaseSource.Text);
        if (txtdb is { } && txtdb.Pairs.FirstOrDefault() is { } txtpair) {
            txtDBAnnotator = new ImmsTextDBAnnotator(txtdb.DataBase, txtpair.AnnotationQueryFactory.PrepareParameter(), "TextDB", 2);
        }

        var processor = new FileProcess(storage, providerFactory, mspAnnotator, txtDBAnnotator, evaluator);
        var runner = new ProcessRunner(processor, storage.Parameter.NumThreads / 2);
        await runner.RunAllAsync(files, ProcessOption.All, Enumerable.Repeat(default(IProgress<int>?), files.Count), null, default).ConfigureAwait(false);

        IAnalysisExporter<ChromatogramPeakFeatureCollection> peak_MspExporter = new AnalysisMspExporter(storage.DataBaseMapper, storage.Parameter);
        var peak_accessor = new ImmsAnalysisMetadataAccessor(storage.DataBaseMapper, storage.Parameter, ExportspectraType.deconvoluted);
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

        if (storage.Parameter.TogetherWithAlignment) {
            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
            var alignmentFile = storage.AlignmentFiles.First();
            var factory = new ImmsAlignmentProcessFactory(storage, evaluator);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = providerFactory;
            aligner.ProviderFactory = providerFactory; // TODO: I'll remove this later.
            var result = aligner.Alignment(files, alignmentFile, null);
            result.Save(alignmentFile);
            var align_decResults = LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties).ToList();
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, align_decResults);

            var align_stats = new[] { StatsValue.Average, StatsValue.Stdev };
            var align_exporter = new AlignmentCSVExporter();
            var align_quantAccessor = new LegacyQuantValueAccessor("Height", storage.Parameter);
            var align_accessor = new ImmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter, false);
            var align_outputfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdalign");
            using FileStream stream = File.Open(align_outputfile, FileMode.Create, FileAccess.Write);
            align_exporter.Export(stream, result.AlignmentSpotProperties, align_decResults, files, new MulticlassFileMetaAccessor(0), align_accessor, align_quantAccessor, align_stats);

            var align_outputmspfile = Path.Combine(outputFolder, alignmentFile.FileName + ".mdmsp");
            IAlignmentSpectraExporter align_mspexporter = new AlignmentMspExporter(storage.DataBaseMapper, storage.Parameter);
            using var streammsp = File.Open(align_outputmspfile, FileMode.Create, FileAccess.Write);
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

    private static IEnumerable<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialImmsParameter> storage, IReadOnlyList<AlignmentSpotProperty> spots) {
        var files = storage.AnalysisFiles;

        var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
        foreach (var file in files) {
            MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
            pointerss.Add((version, pointers, isAnnotationInfo));
        }

        var streams = new List<FileStream>();
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
