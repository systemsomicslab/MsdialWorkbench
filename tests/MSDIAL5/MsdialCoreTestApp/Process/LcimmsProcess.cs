using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.Algorithm;
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Process;
using CompMs.MsdialLcMsApi.Export;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace CompMs.App.MsdialConsole.Process;

public sealed class LcimmsProcess {
    public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
        var param = ConfigParser.ReadForLcImMsParameter(methodFile);
        var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
        if (!isCorrectlyImported) {
            return -1;
        }

        CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
            out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode, out var lbmDB);

        var container = new MsdialLcImMsDataStorage() {
            AnalysisFiles = analysisFiles,
            AlignmentFiles = [alignmentFile],
            MspDB = mspDB,
            TextDB = txtDB,
            IsotopeTextDB = isotopeTextDB,
            IupacDatabase = iupacDB,
            MsdialLcImMsParameter = param
        };

        var dbStorage = DataBaseStorage.CreateEmpty();
        if (File.Exists(param.MspFilePath)) {
            MoleculeDataBase database = new MoleculeDataBase(mspDB, param.MspFilePath, DataBaseSource.Msp, SourceType.MspDB);
            var annotator = new LcimmsMspAnnotator(database, param.MspSearchParam, param.TargetOmics, param.MspFilePath, 1);
            dbStorage.AddMoleculeDataBase(database, new List<IAnnotatorParameterPair<MoleculeDataBase>> {
            new MetabolomicsAnnotatorParameterPair(annotator.Save(), new AnnotationQueryFactory(annotator, param.PeakPickBaseParam, param.MspSearchParam, ignoreIsotopicPeak: true)),
            });
        }
        if (File.Exists(param.LbmFilePath)) {
            MoleculeDataBase lbmDatabase = new MoleculeDataBase(lbmDB, param.LbmFilePath, DataBaseSource.Lbm, SourceType.MspDB);
            var lbmAnnotator = new LcimmsMspAnnotator(lbmDatabase, param.LbmSearchParam, param.TargetOmics, param.LbmFilePath, 1);
            dbStorage.AddMoleculeDataBase(lbmDatabase, new List<IAnnotatorParameterPair<MoleculeDataBase>> {
            new MetabolomicsAnnotatorParameterPair(lbmAnnotator.Save(), new AnnotationQueryFactory(lbmAnnotator, param.PeakPickBaseParam, param.LbmSearchParam, ignoreIsotopicPeak: true)),
            });
        }
        if (File.Exists(param.TextDBFilePath)) {
            var textdatabase = new MoleculeDataBase(txtDB, param.TextDBFilePath, DataBaseSource.Text, SourceType.TextDB);
            var textannotator = new LcimmsTextDBAnnotator(textdatabase, param.TextDbSearchParam, param.TextDBFilePath, 2);
            dbStorage.AddMoleculeDataBase(textdatabase, new List<IAnnotatorParameterPair<MoleculeDataBase>> {
            new MetabolomicsAnnotatorParameterPair(textannotator.Save(), new AnnotationQueryFactory(textannotator, param.PeakPickBaseParam, param.TextDbSearchParam, ignoreIsotopicPeak: false)),
            });
        }
        container.DataBases = dbStorage;
        container.DataBaseMapper = dbStorage.CreateDataBaseMapper();

        var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, param.ProjectParam.ProjectFileName + ".mdproject"));
        projectDataStorage.AddStorage(container);

        Console.WriteLine("Start processing..");
        return Execute(projectDataStorage, container, outputFolder, isProjectSaved);
    }

    private int Execute(ProjectDataStorage projectDataStorage, IMsdialDataStorage<MsdialLcImMsParameter> storage, string outputFolder, bool isProjectSaved) {

        var files = storage.AnalysisFiles;
        var tasks = new Task[files.Count];
        var evaluator = new MsScanMatchResultEvaluator(storage.Parameter.MspSearchParam);
        var annotationProcess = new StandardAnnotationProcess(storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories, evaluator, storage.DataBaseMapper);
        var providerFactory = new StandardDataProviderFactory(5, false);
        var accProviderFactory = new LcimmsAccumulateDataProviderFactory();
        var process = new FileProcess(providerFactory, accProviderFactory, annotationProcess, evaluator, storage, false);

        IAnalysisExporter<ChromatogramPeakFeatureCollection> peak_MspExporter = new AnalysisMspExporter(storage.DataBaseMapper, storage.Parameter);
        var peak_accessor = new LcmsAnalysisMetadataAccessor(storage.DataBaseMapper, storage.Parameter, ExportspectraType.deconvoluted);
        var peakExporterFactory = new AnalysisCSVExporterFactory("\t");
        var sem = new SemaphoreSlim(Environment.ProcessorCount / 2);
        foreach ((var file, var idx) in files.WithIndex()) {
            tasks[idx] = Task.Run(async () => {
                await sem.WaitAsync();
                try {
                    var provider = providerFactory.Create(file);
                    await process.RunAsync(file, null).ConfigureAwait(false);

                    var peak_outputfile = Path.Combine(outputFolder, file.AnalysisFileName + ".mdpeak");
                    var peak_outputmspfile = Path.Combine(outputFolder, file.AnalysisFileName + ".mdmsp");
                    using (var stream = File.Open(peak_outputfile, FileMode.Create, FileAccess.Write))
                    using (var mspstream = File.Open(peak_outputmspfile, FileMode.Create, FileAccess.Write)) {
                        var peak_container = await ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).ConfigureAwait(false);
                        var peak_decResults = MsdecResultsReader.ReadMSDecResults(file.DeconvolutionFilePath, out _, out _);
                        peakExporterFactory.CreateExporter(provider.AsFactory(), peak_accessor).Export(stream, file, peak_container, new ExportStyle());
                        peak_MspExporter.Export(mspstream, file, peak_container, new ExportStyle());
                    }
                }
                finally {
                    sem.Release();
                }
            });
        }
        Task.WaitAll(tasks);
        if (!storage.Parameter.TogetherWithAlignment) return 0;
        return 0;






        //var database = new MoleculeDataBase(container.MspDB, container.MsdialLcImMsParameter.MspFilePath, DataBaseSource.Msp, SourceType.MspDB);
        //var annotator = new LcimmsMspAnnotator(database, container.MsdialLcImMsParameter.MspSearchParam, container.MsdialLcImMsParameter.TargetOmics, container.MsdialLcImMsParameter.MspFilePath, 1);



        //        var evaluator = FacadeMatchResultEvaluator.FromDataBases(container.DataBases);
        //        var annotationProcess = new StandardAnnotationProcess(
        //            new[]
        //            {
        //                new AnnotationQueryFactory(annotator, container.MsdialLcImMsParameter.PeakPickBaseParam, container.MsdialLcImMsParameter.MspSearchParam, ignoreIsotopicPeak: true),
        //            },
        //            evaluator,
        //            annotator);
        //        var exporterFactory = new AnalysisCSVExporterFactory("\t");
        //        var metadata = new LcmsAnalysisMetadataAccessor(annotator, container.MsdialLcImMsParameter);
        //        using (var streamManager = new DirectoryTreeStreamManager(outputFolder)) {
        //            foreach (var file in files) {
        //                FileProcess.Run(file, spectrumProviderFactory, accProviderFactory, annotationProcess, annotator, container);
        //                var features = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).Result;
        //                var msdecs = MsdecResultsReader.ReadMSDecResults(file.DeconvolutionFilePath, out _, out _);
        //                using (var stream = streamManager.Create(file.AnalysisFileName + ".txt").Result) {
        //                    exporterFactory.CreateExporter(spectrumProviderFactory, metadata).Export(stream, file, features, new ExportStyle());
        //                }
        //#if DEBUG
        //                Console.WriteLine($"Test: {features.Items.SelectMany(feature => feature.DriftChromFeatures, (feature, drift) => (feature.Mass, feature.PeakHeightTop, drift.Mass, drift.PeakHeightTop).GetHashCode()).Aggregate((a, b) => a ^ b)}");
        //#endif
        //            }
        //            if (isProjectSaved) {
        //                container.SaveAsync(streamManager, container.MsdialLcImMsParameter.ProjectFileName, string.Empty).Wait();
        //            }
        //            ((IStreamManager)streamManager).Complete();
        //        }
        //        if (!container.MsdialLcImMsParameter.TogetherWithAlignment) return 0;

        //        return 0;
    }
}
