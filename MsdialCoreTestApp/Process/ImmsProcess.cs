using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Algorithm;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Parser;
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
            CommonProcess.ParseLibraries(param, targetMz, out var iupacDB, out var mspDB, out var txtDB, out var isotopeTextDB, out var compoundsInTargetMode);

            var container = new MsdialDataStorage
            {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean> { alignmentFile },
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, ParameterBase = param
            };

            Console.WriteLine("Start processing..");
            return Execute(container, outputFolder, isProjectSaved);
        }

        private int Execute(MsdialDataStorage container, string outputFolder, bool isProjectSaved) {
            var files = container.AnalysisFiles;
            foreach (var file in files) {
                FileProcess.Run(file, container, false);
            }

            var alignmentFile = container.AlignmentFiles.First();
            var factory = new ImmsProcessFactory(container.ParameterBase as MsdialImmsParameter, container.IupacDatabase);
            var aFactory = factory.CreateAlignmentFactory();
            var aligner = aFactory.CreatePeakAligner();
            aligner.ProcessFactory = factory; // TODO: I'll remove this later.
            var result = aligner.Alignment(files, alignmentFile, null);

            foreach (var group in result.AlignmentSpotProperties.GroupBy(prop => prop.Ontology)) {
                Console.WriteLine(group.Key);
                foreach (var spot in group.OrderBy(s => s.MassCenter)) {
                    Console.WriteLine($"\t{spot.Name}\t{spot.AdductType.AdductIonName}\t{spot.MassCenter}\t{spot.TimesCenter.Drift.Value}");
                }
            }

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            new MsdialImmsSerializer().SaveMsdialDataStorage(container.ParameterBase.ProjectFilePath, container);
            return 0;
        }
    }
}
