using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Algorithm.Annotation;
using CompMs.MsdialDimsCore.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        return Execute(container, providerFactory, outputFolder, isProjectSaved);
    }

    private int Execute(MsdialDimsDataStorage storage, IDataProviderFactory<AnalysisFileBean> providerFactory, string outputFolder, bool isProjectSaved) {
        var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, Path.ChangeExtension(storage.MsdialDimsParameter.ProjectParam.ProjectFileName, ".mdproject")));
        projectDataStorage.AddStorage(storage);

        var files = storage.AnalysisFiles;
        var mapper = storage.DataBases.CreateDataBaseMapper();
        var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);

        var annotationProcess = new StandardAnnotationProcess(
            storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories,
            evaluator,
            mapper);
        foreach (var file in files) {
            ProcessFile.Run(file, providerFactory.Create(file), storage, annotationProcess, evaluator);
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
