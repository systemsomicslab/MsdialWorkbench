using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialDimsCore.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process {
    public class DimsProcess {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
            var param = ConfigParser.ReadForDimsParameter(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;
            CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
                out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode);

            var container = new MsdialDimsDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialDimsParameter = param
            };

            var providerFactory = new StandardDataProviderFactory();
            Console.WriteLine("Start processing..");
            return Execute(container, providerFactory, outputFolder, isProjectSaved);
        }

        private int Execute(MsdialDimsDataStorage container, IDataProviderFactory<AnalysisFileBean> providerFactory, string outputFolder, bool isProjectSaved) {
            var files = container.AnalysisFiles;
            var mspAnnotator = new DimsMspAnnotator(new MoleculeDataBase(container.MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB), container.MsdialDimsParameter.MspSearchParam, container.MsdialDimsParameter.TargetOmics, "MspDB", -1);
            var textAnnotator = new DimsTextDBAnnotator(new MoleculeDataBase(container.TextDB, "TextDB", DataBaseSource.Text, SourceType.TextDB), container.MsdialDimsParameter.TextDbSearchParam, "TextDB", -1);
            var annotationProcess = new StandardAnnotationProcess<IAnnotationQuery>(
                new AnnotationQueryWithoutIsotopeFactory(),
                new[] { new AnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>(mspAnnotator, container.MsdialDimsParameter.MspSearchParam),
                        new AnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>(textAnnotator, container.MsdialDimsParameter.TextDbSearchParam), }); 
            foreach (var file in files) {
                ProcessFile.Run(file, providerFactory, container, annotationProcess);
            }

            var alignmentFile = container.AlignmentFiles.First();
            var factory = new DimsAlignmentProcessFactory(container.MsdialDimsParameter as MsdialDimsParameter, container.IupacDatabase, container.DataBaseMapper);
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(files, alignmentFile, null);

            foreach (var group in result.AlignmentSpotProperties.GroupBy(prop => prop.Ontology)) {
                Console.WriteLine(group.Key);
                foreach (var spot in group.OrderBy(s => s.MassCenter)) {
                    Console.WriteLine($"\t{spot.Name}\t{spot.AdductType.AdductIonName}\t{spot.MassCenter}");
                }
            }

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            var streamManager = new DirectoryTreeStreamManager(container.MsdialDimsParameter.ProjectFolderPath);
            container.Save(streamManager, container.MsdialDimsParameter.ProjectFileName, string.Empty).Wait();

            return 0;
        }

        private async Task<int> ExecuteAsync(MsdialDimsDataStorage container, IDataProviderFactory<AnalysisFileBean> providerFactory, string outputFolder, bool isProjectSaved) {
            var files = container.AnalysisFiles;
            var mspAnnotator = new DimsMspAnnotator(new MoleculeDataBase(container.MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB), container.MsdialDimsParameter.MspSearchParam, container.MsdialDimsParameter.TargetOmics, "MspDB", -1);
            var textAnnotator = new DimsTextDBAnnotator(new MoleculeDataBase(container.TextDB, "TextDB", DataBaseSource.Text, SourceType.TextDB), container.MsdialDimsParameter.TextDbSearchParam, "TextDB", -1);
            var annotationProcess = new StandardAnnotationProcess<IAnnotationQuery>(
                new AnnotationQueryWithoutIsotopeFactory(),
                new[] { new AnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>(mspAnnotator, container.MsdialDimsParameter.MspSearchParam),
                        new AnnotatorContainer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>(textAnnotator, container.MsdialDimsParameter.TextDbSearchParam), }); 
            var tasks = files.Select(file => Task.Run(() => ProcessFile.Run(file, providerFactory, container, annotationProcess)));
            await Task.WhenAll(tasks);

            var alignmentFile = container.AlignmentFiles.First();
            var factory = new DimsAlignmentProcessFactory(container.MsdialDimsParameter as MsdialDimsParameter, container.IupacDatabase, container.DataBaseMapper);
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(files, alignmentFile, null);

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            var streamManager = new DirectoryTreeStreamManager(container.MsdialDimsParameter.ProjectFolderPath);
            await container.Save(streamManager, container.MsdialDimsParameter.ProjectFileName, string.Empty);
            return 0;
        }
    }
}
