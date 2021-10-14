using CompMs.App.MsdialConsole.Parser;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Algorithm.Alignment;
using CompMs.MsdialImmsCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Process;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process
{
    public class ImmsProcess
    {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
            var param = ConfigParser.ReadForImmsParameter(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out var analysisFiles, out var alignmentFile);
            if (!isCorrectlyImported) return -1;
            CommonProcess.ParseLibraries(param, targetMz, out var iupacDB, out var mspDB, out var txtDB, out var isotopeTextDB, out var compoundsInTargetMode);

            var container = new MsdialImmsDataStorage
            {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean> { alignmentFile },
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, MsdialImmsParameter = param
            };

            Console.WriteLine("Start processing..");
            return Execute(container, outputFolder, isProjectSaved);
        }

        private int Execute(IMsdialDataStorage<MsdialImmsParameter> container, string outputFolder, bool isProjectSaved) {
            var files = container.AnalysisFiles;
            foreach (var file in files) {
                FileProcess.Run(file, container, false);
            }

            var alignmentFile = container.AlignmentFiles.First();
            var factory = new ImmsAlignmentProcessFactory(container.Parameter, container.IupacDatabase, container.DataBaseMapper);
            var aligner = factory.CreatePeakAligner();
            aligner.ProviderFactory = new ImmsAverageDataProviderFactory(0.001, 0.002, 5, false); // TODO: I'll remove this later.
            var result = aligner.Alignment(files, alignmentFile, null);

            foreach (var group in result.AlignmentSpotProperties.GroupBy(prop => prop.Ontology)) {
                Console.WriteLine(group.Key);
                foreach (var spot in group.OrderBy(s => s.MassCenter)) {
                    Console.WriteLine($"\t{spot.Name}\t{spot.AdductType.AdductIonName}\t{spot.MassCenter}\t{spot.TimesCenter.Drift.Value}");
                }
            }

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            var streamManager = new DirectoryTreeStreamManager(container.Parameter.ProjectFolderPath);
            container.Save(streamManager, Path.GetFileName(container.Parameter.ProjectFilePath), string.Empty).Wait();

            return 0;
        }
    }
}
