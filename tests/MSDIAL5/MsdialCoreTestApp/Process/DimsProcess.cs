using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process
{
    public class DimsProcess {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
            var param = ConfigParser.ReadForDimsParameter(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;
            CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
                out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode, out var lbmDB);

            var container = new MsdialDimsDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialDimsParameter = param
            };

            var providerFactory = new StandardDataProviderFactory();
            Console.WriteLine("Start processing..");
            return Execute(container, providerFactory, outputFolder, isProjectSaved);
        }

        private int Execute(MsdialDimsDataStorage storage, IDataProviderFactory<AnalysisFileBean> providerFactory, string outputFolder, bool isProjectSaved) {
            var files = storage.AnalysisFiles;
            var mspAnnotator = new DimsMspAnnotator(new MoleculeDataBase(storage.MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB), storage.MsdialDimsParameter.MspSearchParam, storage.MsdialDimsParameter.TargetOmics, "MspDB", -1);
            var textAnnotator = new DimsTextDBAnnotator(new MoleculeDataBase(storage.TextDB, "TextDB", DataBaseSource.Text, SourceType.TextDB), storage.MsdialDimsParameter.TextDbSearchParam, "TextDB", -1);
            var mapper = new DataBaseMapper();
            mapper.Add(mspAnnotator);
            mapper.Add(textAnnotator);
            var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);

            var annotationProcess = new StandardAnnotationProcess(
                new[]
                {
                    new AnnotationQueryWithoutIsotopeFactory(mspAnnotator, storage.MsdialDimsParameter.MspSearchParam),
                    new AnnotationQueryWithoutIsotopeFactory(textAnnotator, storage.MsdialDimsParameter.TextDbSearchParam)
                },
                evaluator,
                mapper);
            var processor = new ProcessFile(providerFactory, storage, annotationProcess, evaluator);
            foreach (var file in files) {
                processor.RunAsync(file, ProcessOption.PeakSpotting | ProcessOption.Identification, null, default).Wait();
            }

            if (storage.MsdialDimsParameter.TogetherWithAlignment) {
                var alignmentFile = storage.AlignmentFiles.First();
                var factory = new DimsAlignmentProcessFactory(storage, evaluator);
                var aligner = factory.CreatePeakAligner();
                var result = aligner.Alignment(files, alignmentFile, null);

                foreach (var group in result.AlignmentSpotProperties.GroupBy(prop => prop.Ontology)) {
                    Console.WriteLine(group.Key);
                    foreach (var spot in group.OrderBy(s => s.MassCenter)) {
                        Console.WriteLine($"\t{spot.Name}\t{spot.AdductType.AdductIonName}\t{spot.MassCenter}");
                    }
                }
                result.Save(alignmentFile);
            }

            if (isProjectSaved) {
                using (var streamManager = new DirectoryTreeStreamManager(storage.MsdialDimsParameter.ProjectFolderPath)) {
                    storage.SaveAsync(streamManager, storage.MsdialDimsParameter.ProjectFileName, string.Empty).Wait();
                    ((IStreamManager)streamManager).Complete();
                }
            }

            return 0;
        }
    }
}
