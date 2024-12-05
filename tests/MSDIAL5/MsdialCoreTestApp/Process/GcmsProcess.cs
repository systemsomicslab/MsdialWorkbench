using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Properties;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Algorithm;
using CompMs.MsdialGcMsApi.Algorithm.Alignment;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialGcMsApi.Export;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialGcMsApi.Parser;
using CompMs.MsdialGcMsApi.Process;
using CompMs.MsdialIntegrate.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process;

public sealed class GcmsProcess
{
    public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectStore)
    {
        var param = ConfigParser.ReadForGcms(methodFile);
        var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
        if (!isCorrectlyImported) {
            return -1;
        }

        if (param.RiDictionaryFilePath != string.Empty)
        {
            if (!File.Exists(param.RiDictionaryFilePath)) {
                throw new FileNotFoundException(string.Format("File '{0}' does not exist.", param.RiDictionaryFilePath));
            }

            if (!CheckRiDicionaryFiles(analysisFiles, param.RiDictionaryFilePath, out var errorMessage)) {
                throw new FileNotFoundException(string.Format(errorMessage, param.RiDictionaryFilePath));
            }

            //probably, at least in fiehn lab, this has to be automatically set from GCMS raw data.
            var isIncorrectFormat = false;
            var isIncorrectFiehnFormat = false;

            param.FileIdRiInfoDictionary = [];
            foreach (var file in analysisFiles) {
                param.FileIdRiInfoDictionary[file.AnalysisFileId] = new RiDictionaryInfo() {
                    DictionaryFilePath = file.RiDictionaryFilePath,
                    RiDictionary = RetentionIndexHandler.GetRiDictionary(file.RiDictionaryFilePath)
                };

                var dictionary = param.FileIdRiInfoDictionary[file.AnalysisFileId].RiDictionary;
                if (dictionary is null || dictionary.Count == 0) isIncorrectFormat = true;

                if (param.RiCompoundType == RiCompoundType.Fames) {
                    if (!IsFamesContanesMatch(dictionary)) isIncorrectFiehnFormat = true;
                }
            }
            if (isIncorrectFormat) {
                Console.WriteLine(_riDictionaryErrorMessage);
                return -1;
            }
            if (isIncorrectFiehnFormat) {
                Console.WriteLine(_famesIndexErrorMessage);
                return -1;
            }
        }

        CommonProcess.ParseLibraries(param, -1, out IupacDatabase iupacDB,
            out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB,
            out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode,
            out List<MoleculeMsReference> lbmDB);

        
        var container = new MsdialGcmsDataStorage()
        {
            AnalysisFiles = analysisFiles,
            AlignmentFiles = [alignmentFile],
            MspDB = mspDB,
            TextDB = txtDB,
            IsotopeTextDB = isotopeTextDB,
            IupacDatabase = iupacDB,
            MsdialGcmsParameter = param
        };

        var dbStorage = DataBaseStorage.CreateEmpty();
        if (mspDB.Count > 0)
        {
            var database = new MoleculeDataBase(mspDB, param.MspFilePath, DataBaseSource.Msp, SourceType.MspDB);
            var annotator = new MassAnnotator(database, param.MspSearchParam, param.TargetOmics, SourceType.MspDB, "MspDB", 1);
            dbStorage.AddMoleculeDataBase(database, [
                new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, param.PeakPickBaseParam, param.MspSearchParam, ignoreIsotopicPeak: true)),
            ]);
        }
        if (txtDB.Count > 0)
        {
            var textdatabase = new MoleculeDataBase(txtDB, param.TextDBFilePath, DataBaseSource.Text, SourceType.TextDB);
            var textannotator = new MassAnnotator(textdatabase, param.TextDbSearchParam, param.TargetOmics, SourceType.TextDB, "TextDB", 2);
            dbStorage.AddMoleculeDataBase(textdatabase, [
                new MetabolomicsAnnotatorParameterPair(textannotator.Save(), new AnnotationQueryFactory(textannotator, param.PeakPickBaseParam, param.TextDbSearchParam, ignoreIsotopicPeak: false)),
            ]);
        }
        container.DataBases = dbStorage;
        container.DataBaseMapper = dbStorage.CreateDataBaseMapper();

        Console.WriteLine("Start processing..");
        return ExecuteAsync(container, outputFolder, isProjectStore).Result;
    }

    private bool IsFamesContanesMatch(Dictionary<int, float>? riDictionary)
    {
        if (riDictionary is null) {
            return false;
        }
        var fiehnFamesDictionary = RetentionIndexHandler.GetFiehnFamesDictionary();

        if (fiehnFamesDictionary.Count != riDictionary.Count) return false;
        foreach (var fFame in fiehnFamesDictionary) {
            var fiehnCnumber = fFame.Key;
            var flg = false;
            foreach (var dict in riDictionary) {
                if (fiehnCnumber == dict.Key) {
                    flg = true;
                    break;
                }
            }
            if (flg == false) return false;
        }
        return true;
    }


    private bool CheckRiDicionaryFiles(List<AnalysisFileBean> analysisFiles, string riDictionaryFile, out string errorMessage)
    {
        errorMessage = string.Empty;
        using (var sr = new StreamReader(riDictionaryFile, Encoding.ASCII)) {
            while (sr.Peek() > -1) {
                var line = sr.ReadLine();
                if (string.IsNullOrEmpty(line)) continue;
                var lineArray = line.Split('\t');
                if (lineArray.Length < 2) continue;

                var analysisFilePath = lineArray[0];
                var riFilePath = lineArray[1];

                foreach (var file in analysisFiles) {
                    if (file.AnalysisFilePath == analysisFilePath) {
                        file.RiDictionaryFilePath = riFilePath;
                        break;
                    }
                }
            }
        }

        //check if ri file exist correctly
        foreach (var file in analysisFiles) {
            if (file.RiDictionaryFilePath.IsEmptyOrNull()) {
                errorMessage += file.AnalysisFilePath + "\r\n";
            }
        }
        if (errorMessage != string.Empty) {
            errorMessage += "\r\n" + "The RI dictionary file of the above files is not set correctly. Set your RI dictionary file for all imported files.";
            return false;
        }
        else {
            return true;
        }
    }

    private async Task<int> ExecuteAsync(MsdialGcmsDataStorage storage, string outputFolder, bool isProjectSaved) {
        var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, storage.MsdialGcmsParameter.ProjectParam.ProjectFolderPath, Path.ChangeExtension(storage.MsdialGcmsParameter.ProjectParam.ProjectFileName, ".mdproject")));
        projectDataStorage.AddStorage(storage);

        var files = storage.AnalysisFiles;
        var metaAccessor = new GcmsAnalysisMetadataAccessor(storage.DataBaseMapper, new DelegateMsScanPropertyLoader<SpectrumFeature>(s => s.AnnotatedMSDecResult.MSDecResult));
        var providerFactory = new StandardDataProviderFactory(isGuiProcess: false);
        var process = new FileProcess(providerFactory, storage, new CalculateMatchScore(storage.DataBases.MetabolomicsDataBases.FirstOrDefault(), storage.MsdialGcmsParameter.MspSearchParam, storage.MsdialGcmsParameter.RetentionType));
        var runner = new ProcessRunner(process, storage.MsdialGcmsParameter.NumThreads / 2);
        await runner.RunAllAsync(files, ProcessOption.All, Enumerable.Repeat(default(IProgress<int>?), files.Count), null, default).ConfigureAwait(false);

        var tasks = new Task[files.Count];
        using var sem = new SemaphoreSlim(Environment.ProcessorCount / 2);
        var exporterFactory = new AnalysisCSVExporterFactory("\t");
        var scanExporter = exporterFactory.CreateExporter(metaAccessor);
        for (int i = 0; i < files.Count; i++) {
            var file = files[i];
            tasks[i] = Task.Run(() => {
                var sfs = file.LoadSpectrumFeatures();
                using var stream = File.Open(Path.Combine(outputFolder, file.AnalysisFileName + ".mdscan"), FileMode.Create, FileAccess.Write, FileShare.Read);
                scanExporter.Export(stream, file, sfs.Items, new ExportStyle());
            });
        }
        await Task.WhenAll(tasks);

        if (storage.MsdialGcmsParameter.TogetherWithAlignment)
        {
            ChromatogramSerializer<ChromatogramSpotInfo>? serializer;
            switch (storage.MsdialGcmsParameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RI);
                    if (serializer is not null) {
                        serializer = new RIChromatogramSerializerDecorator(serializer, storage.MsdialGcmsParameter.GetRIHandlers());
                    }
                    break;
                case AlignmentIndexType.RT:
                default:
                    serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.RT);
                    break;
            }
            var alignmentFile = storage.AlignmentFiles.First();
            var factory = new GcmsAlignmentProcessFactory(files, storage);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = providerFactory;
            var result = aligner.Alignment(files, alignmentFile, serializer);
            result.Save(alignmentFile);
            var decResults = LoadRepresentativeDeconvolutions(storage, result.AlignmentSpotProperties);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, decResults);

            var accessor = new GcmsAlignmentMetadataAccessor(storage.DataBaseMapper, storage.MsdialGcmsParameter, false);
            var quantAccessor = new LegacyQuantValueAccessor("Height", storage.MsdialGcmsParameter);
            var stats = new[] { StatsValue.Average, StatsValue.Stdev };
            var spotExporter = new AlignmentCSVExporter("\t");
            using var stream = File.Open(Path.Combine(outputFolder, alignmentFile.FileName + ".mdalign"), FileMode.Create, FileAccess.Write, FileShare.Read);
            spotExporter.Export(stream, result.AlignmentSpotProperties, decResults, files, new MulticlassFileMetaAccessor(0), accessor, quantAccessor, stats);
        }

        if (isProjectSaved)
        {
            storage.MsdialGcmsParameter.ProjectParam.MsdialVersionNumber = $"Msdial console {Resources.VERSION}";
            storage.MsdialGcmsParameter.ProjectParam.FinalSavedDate = DateTime.Now;
            using var stream = File.Open(projectDataStorage.ProjectParameter.FilePath, FileMode.Create);
            using IStreamManager streamManager = new ZipStreamManager(stream, System.IO.Compression.ZipArchiveMode.Create);
            projectDataStorage.Save(streamManager, new MsdialIntegrateSerializer(), file => new DirectoryTreeStreamManager(file), parameter => Console.WriteLine($"Save {parameter.ProjectFileName} failed")).Wait();
            streamManager.Complete();
        }

        return 0;
    }

    private static List<MSDecResult> LoadRepresentativeDeconvolutions(IMsdialDataStorage<MsdialGcmsParameter> storage, IReadOnlyList<AlignmentSpotProperty> spots) {
        var files = storage.AnalysisFiles;

        var pointerss = new List<(int version, List<long> pointers, bool isAnnotationInfo)>();
        foreach (var file in files) {
            MsdecResultsReader.GetSeekPointers(file.DeconvolutionFilePath, out var version, out var pointers, out var isAnnotationInfo);
            pointerss.Add((version, pointers, isAnnotationInfo));
        }

        var streams = new List<FileStream>();
        var decs = new List<MSDecResult>();
        try {
            streams = files.Select(file => File.Open(file.DeconvolutionFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)).ToList();
            foreach (var spot in spots.OrEmptyIfNull()) {
                var repID = spot.RepresentativeFileID;
                var peakID = spot.AlignedPeakProperties[repID].MasterPeakID;
                var decResult = MsdecResultsReader.ReadMSDecResult(
                    streams[repID], pointerss[repID].pointers[peakID],
                    pointerss[repID].version, pointerss[repID].isAnnotationInfo);
                decs.Add(decResult);
            }
        }
        finally {
            streams.ForEach(stream => stream.Close());
        }
        return decs;
    }

#region // error code
    private static readonly string _riDictionaryErrorMessage = """
Invalid RI information. Please check that your file follows the following format.
Carbon number\tRT(min)
10\t4.72
11\t5.63
12\t6.81
13\t8.08
14\t9.12
15\t10.33
16\t11.91
18\t14.01
20\t16.15
22\t18.28
24\t20.33
26\t22.17

This information is required for RI calculation.
""";

    private static readonly string _famesIndexErrorMessage = "If you use the FAMEs RI, you have to decide the retention times as minute for C8, C9, C10, C12, C14, C16, C18, C20, C22, C24, C26, C28, C30."; 
#endregion
}
