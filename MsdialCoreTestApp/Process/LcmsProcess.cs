using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Algorithm.Annotation;
using CompMs.MsdialLcMsApi.DataObj;
using CompMs.MsdialLcMsApi.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process
{
    public class LcmsProcess
    {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz)
        {
            var param = ConfigParser.ReadForLcmsParameter(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;
            CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
                out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode);

            var container = new MsdialLcmsDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialLcmsParameter = param
            };

            var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, param.ProjectParam.ProjectFileName + ".mdproject"));
            projectDataStorage.AddStorage(container);

            Console.WriteLine("Start processing..");
            return Execute(projectDataStorage, container, outputFolder, isProjectSaved);
        }

        private int Execute(ProjectDataStorage projectDataStorage, IMsdialDataStorage<MsdialLcmsParameter> storage, string outputFolder, bool isProjectSaved) {
            var files = storage.AnalysisFiles;
            var tasks = new Task[files.Count];
            var evaluator = MsScanMatchResultEvaluator.CreateEvaluator(storage.Parameter.MspSearchParam);
            var database = new MoleculeDataBase(storage.MspDB, storage.Parameter.MspFilePath, DataBaseSource.Msp, SourceType.MspDB);
            var annotator = new LcmsMspAnnotator(database, storage.Parameter.MspSearchParam, storage.Parameter.TargetOmics, storage.Parameter.MspFilePath, 1);
            var textdatabase = new MoleculeDataBase(storage.TextDB, storage.Parameter.TextDBFilePath, DataBaseSource.Text, SourceType.TextDB);
            var textannotator = new LcmsTextDBAnnotator(database, storage.Parameter.TextDbSearchParam, storage.Parameter.TextDBFilePath, 2);
            var mapper = new DataBaseMapper();
            mapper.Add(annotator);
            mapper.Add(textannotator);
            var annotationProcess = new StandardAnnotationProcess(
                new[]
                {
                    new AnnotationQueryFactory(annotator, storage.Parameter.PeakPickBaseParam, storage.Parameter.MspSearchParam),
                    new AnnotationQueryFactory(annotator, storage.Parameter.PeakPickBaseParam, storage.Parameter.TextDbSearchParam),
                },
                evaluator,
                mapper);
            var process = new FileProcess(new StandardDataProviderFactory(5, false), storage, annotationProcess, evaluator);
            var sem = new SemaphoreSlim(Environment.ProcessorCount / 2);
            foreach ((var file, var idx) in files.WithIndex()) {
                tasks[idx] = Task.Run(async () => {
                    await sem.WaitAsync();
                    try {
                        await process.RunAsync(file, null).ConfigureAwait(false);
                    }
                    finally {
                        sem.Release();
                    }
                });
            }
            Task.WaitAll(tasks);

            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
            var alignmentFile = storage.AlignmentFiles.First();
            var factory = new LcmsAlignmentProcessFactory(storage, evaluator);
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(files, alignmentFile, serializer);

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            MsdecResultsWriter.Write(alignmentFile.SpectraFilePath, LoadRepresentativeDeconvolutions(storage, result?.AlignmentSpotProperties).ToList());

            if (isProjectSaved) {
                using (var stream = File.Open(projectDataStorage.ProjectParameter.FilePath, FileMode.Create))
                using (var streamManager = new ZipStreamManager(stream, System.IO.Compression.ZipArchiveMode.Create)) {
                    projectDataStorage.Save(streamManager, new MsdialIntegrateSerializer(), file => new DirectoryTreeStreamManager(file), parameter => Console.WriteLine($"Save {parameter.ProjectFileName} failed")).Wait();
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
