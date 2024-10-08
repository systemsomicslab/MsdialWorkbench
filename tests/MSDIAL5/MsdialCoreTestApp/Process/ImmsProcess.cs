using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Algorithm.Alignment;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
        return Execute(container, outputFolder, isProjectSaved);
    }

    private int Execute(IMsdialDataStorage<MsdialImmsParameter> storage, string outputFolder, bool isProjectSaved) {
        var projectDataStorage = new ProjectDataStorage(new ProjectParameter(DateTime.Now, outputFolder, Path.ChangeExtension(storage.Parameter.ProjectParam.ProjectFileName, ".mdproject")));
        projectDataStorage.AddStorage(storage);

        var files = storage.AnalysisFiles;
        var evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
        var providerFactory = new ImmsAverageDataProviderFactory(0.001, 0.002, 5, false);
        var factories = storage.CreateAnnotationQueryFactoryStorage().MoleculeQueryFactories;

        // temporary
        var msppair = storage.DataBases.MetabolomicsDataBases.FirstOrDefault(d => d.DataBase.DataBaseSource == DataBaseSource.Msp);
        var mspAnnotator = new ImmsMspAnnotator(msppair?.DataBase, msppair?.Pairs.FirstOrDefault()?.AnnotationQueryFactory.PrepareParameter(), storage.Parameter.TargetOmics, "MspDB", 1);
        var txtpair = storage.DataBases.MetabolomicsDataBases.FirstOrDefault(d => d.DataBase.DataBaseSource == DataBaseSource.Text);
        var textDBAnnotator = new ImmsTextDBAnnotator(txtpair?.DataBase, txtpair?.Pairs.FirstOrDefault()?.AnnotationQueryFactory.PrepareParameter(), "TextDB", 2);

        var processor = new FileProcess(storage, mspAnnotator, textDBAnnotator, evaluator);
        processor.RunAllAsync(files, files.Select(providerFactory.Create), files.Select(_ => (Action<int>)null), storage.Parameter.NumThreads, () => { }).Wait();

        if (storage.Parameter.TogetherWithAlignment) {
            var alignmentFile = storage.AlignmentFiles.First();
            var factory = new ImmsAlignmentProcessFactory(storage, evaluator);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = providerFactory; // TODO: I'll remove this later.
            var result = aligner.Alignment(files, alignmentFile, null);
            result.Save(alignmentFile);

            foreach (var group in result.AlignmentSpotProperties.GroupBy(prop => prop.Ontology)) {
                Console.WriteLine(group.Key);
                foreach (var spot in group.OrderBy(s => s.MassCenter)) {
                    Console.WriteLine($"\t{spot.Name}\t{spot.AdductType.AdductIonName}\t{spot.MassCenter}\t{spot.TimesCenter.Drift.Value}");
                }
            }
        }

        if (isProjectSaved) {
            using (var streamManager = new DirectoryTreeStreamManager(storage.Parameter.ProjectFolderPath)) {
                storage.SaveAsync(streamManager, storage.Parameter.ProjectFileName, string.Empty).Wait();
                ((IStreamManager)streamManager).Complete();
            }
        }

        return 0;
    }
}
