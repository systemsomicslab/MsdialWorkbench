using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialCore.Parser;
using System.IO;
using CompMs.MsdialLcImMsApi.Process;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialLcImMsApi.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialLcImMsApi.Algorithm.Annotation;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Export;
using CompMs.MsdialLcMsApi.Export;

namespace CompMs.App.MsdialConsole.Process {
    public class LcimmsProcess {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
            var param = ConfigParser.ReadForLcImMsParameter(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;
            CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
                out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode);

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
            var annotationProcess = new StandardAnnotationProcess<AnnotationQuery>(
                new AnnotationQueryWithoutIsotopeFactory(annotator),
                new IAnnotatorContainer<AnnotationQuery, MoleculeMsReference, MsScanMatchResult>[] {
                    new AnnotatorContainer<AnnotationQuery, MoleculeMsReference, MsScanMatchResult>(annotator, container.MsdialLcImMsParameter.MspSearchParam)
                });
            var streamManager = new DirectoryTreeStreamManager(outputFolder);
            var exporter = new AnalysisCSVExporter("\t");
            var metadata = new LcmsAnalysisMetadataAccessor(annotator, container.MsdialLcImMsParameter);
            foreach (var file in files) {
                FileProcess.Run(file, spectrumProviderFactory, accProviderFactory, annotationProcess, annotator, container);
                var features = MsdialPeakSerializer.LoadChromatogramPeakFeatures(file.PeakAreaBeanInformationFilePath);
                var msdecs = MsdecResultsReader.ReadMSDecResults(file.DeconvolutionFilePath, out _, out _);
                using (var stream = streamManager.Create(file.AnalysisFileName + ".txt").Result) {
                    exporter.Export(stream, features, msdecs, spectrumProviderFactory.Create(file), metadata);
                }
#if DEBUG
                Console.WriteLine($"Test: {features.SelectMany(feature => feature.DriftChromFeatures, (feature, drift) => (feature.Mass, feature.PeakHeightTop, drift.Mass, drift.PeakHeightTop).GetHashCode()).Aggregate((a, b) => a ^ b)}");
#endif
            }
            if (isProjectSaved) {
                container.SaveAsync(streamManager, container.MsdialLcImMsParameter.ProjectFileName, string.Empty).Wait();
            }
            return 0;
        }
    }
}
