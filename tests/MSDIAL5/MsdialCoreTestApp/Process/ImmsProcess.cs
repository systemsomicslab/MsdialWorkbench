using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Algorithm.Alignment;
using CompMs.MsdialImmsCore.Algorithm.Annotation;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process
{
    public class ImmsProcess
    {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
            var param = ConfigParser.ReadForImmsParameter(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out var analysisFiles, out var alignmentFile);
            if (!isCorrectlyImported) return -1;
            CommonProcess.ParseLibraries(param, targetMz, out var iupacDB, out var mspDB, out var txtDB, out var isotopeTextDB, out var compoundsInTargetMode, out var lbmDB);

            var container = new MsdialImmsDataStorage
            {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean> { alignmentFile },
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialImmsParameter = param,
            };

            Console.WriteLine("Start processing..");
            return Execute(container, outputFolder, isProjectSaved);
        }

        private int Execute(IMsdialDataStorage<MsdialImmsParameter> storage, string outputFolder, bool isProjectSaved) {
            var files = storage.AnalysisFiles;
            var db = DataBaseStorage.CreateEmpty();
            var mdb = new MoleculeDataBase(storage.MspDB, "MspDB", DataBaseSource.Msp, SourceType.MspDB);
            var mspAnnotator = new ImmsMspAnnotator(mdb, storage.Parameter.MspSearchParam, storage.Parameter.TargetOmics, "MspDB", -1);
            db.AddMoleculeDataBase(
                mdb,
                new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(mspAnnotator.Save(), new AnnotationQueryFactory(mspAnnotator, storage.Parameter.PeakPickBaseParam, storage.Parameter.MspSearchParam, ignoreIsotopicPeak: true))
                });
            var tdb = new MoleculeDataBase(storage.TextDB, "TextDB", DataBaseSource.Text, SourceType.TextDB);
            var textDBAnnotator = new ImmsTextDBAnnotator(tdb, storage.Parameter.TextDbSearchParam, "TextDB", -1);
            db.AddMoleculeDataBase(
                tdb,
                new List<IAnnotatorParameterPair<MoleculeDataBase>> {
                    new MetabolomicsAnnotatorParameterPair(textDBAnnotator.Save(), new AnnotationQueryFactory(textDBAnnotator, storage.Parameter.PeakPickBaseParam, storage.Parameter.TextDbSearchParam, ignoreIsotopicPeak: false))
                });
            var evaluator = FacadeMatchResultEvaluator.FromDataBases(db);
            storage.DataBases = db;
            var providerFactory = new ImmsAverageDataProviderFactory(0.001, 0.002, 5, false);
            var processor = new FileProcess(storage, providerFactory, mspAnnotator, textDBAnnotator, evaluator);
            var runner = new ProcessRunner(processor, storage.Parameter.NumThreads);
            runner.RunAllAsync(files, ProcessOption.PeakSpotting | ProcessOption.Identification, files.Select(_ => (IProgress<int>)null), () => { }, default).Wait();

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
}
