using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.Algorithm;
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Process;
using CompMs.MsdialLcMsApi.Export;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process
{
    public class LcimmsProcess {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
            var param = ConfigParser.ReadForLcImMsParameter(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;
            CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
                out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode, out var lbmDB);

            var container = new MsdialLcImMsDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialLcImMsParameter = param
            };

            Console.WriteLine("Start processing..");
            return Execute(container, outputFolder, isProjectSaved);
        }

        private int Execute(MsdialLcImMsDataStorage container, string outputFolder, bool isProjectSaved) {
            var files = container.AnalysisFiles;
            var spectrumProviderFactory = new StandardDataProviderFactory();
            var accProviderFactory = new LcimmsAccumulateDataProviderFactory();
            var database = new MoleculeDataBase(container.MspDB, container.MsdialLcImMsParameter.MspFilePath, DataBaseSource.Msp, SourceType.MspDB);
            var annotator = new LcimmsMspAnnotator(database, container.MsdialLcImMsParameter.MspSearchParam, container.MsdialLcImMsParameter.TargetOmics, container.MsdialLcImMsParameter.MspFilePath, 1);
            var evaluator = FacadeMatchResultEvaluator.FromDataBases(container.DataBases);
            var annotationProcess = new StandardAnnotationProcess(
                new[]
                {
                    new AnnotationQueryFactory(annotator, container.MsdialLcImMsParameter.PeakPickBaseParam, container.MsdialLcImMsParameter.MspSearchParam, ignoreIsotopicPeak: true),
                },
                evaluator,
                annotator);
            var exporterFactory = new AnalysisCSVExporterFactory("\t");
            var metadata = new LcmsAnalysisMetadataAccessor(annotator, container.MsdialLcImMsParameter);
            using (var streamManager = new DirectoryTreeStreamManager(outputFolder)) {
                var processor = new FileProcess(spectrumProviderFactory, accProviderFactory, annotationProcess, annotator, container, isGuiProcess: false);
                foreach (var file in files) {
                    processor.RunAsync(file, ProcessOption.PeakSpotting | ProcessOption.Identification).Wait();
                    var features = ChromatogramPeakFeatureCollection.LoadAsync(file.PeakAreaBeanInformationFilePath).Result;
                    var msdecs = MsdecResultsReader.ReadMSDecResults(file.DeconvolutionFilePath, out _, out _);
                    using (var stream = streamManager.Create(file.AnalysisFileName + ".txt").Result) {
                        exporterFactory.CreateExporter(spectrumProviderFactory, metadata).Export(stream, file, features, new ExportStyle());
                    }
    #if DEBUG
                    Console.WriteLine($"Test: {features.Items.SelectMany(feature => feature.DriftChromFeatures, (feature, drift) => (feature.Mass, feature.PeakHeightTop, drift.Mass, drift.PeakHeightTop).GetHashCode()).Aggregate((a, b) => a ^ b)}");
    #endif
                }
                if (isProjectSaved) {
                    container.SaveAsync(streamManager, container.MsdialLcImMsParameter.ProjectFileName, string.Empty).Wait();
                }
                ((IStreamManager)streamManager).Complete();
            }
            if (!container.MsdialLcImMsParameter.TogetherWithAlignment) return 0;

            return 0;
        }
    }
}
