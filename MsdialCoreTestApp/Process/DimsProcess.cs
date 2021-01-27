using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore;
using CompMs.MsdialDimsCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialDimsCore.Parser;
using System;
using System.Collections.Generic;
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
            param.FileID_ClassName = analysisFiles.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileClass);

            var container = new MsdialDataStorage() {
                AnalysisFiles = analysisFiles, AlignmentFiles = new List<AlignmentFileBean>() { alignmentFile },
                MspDB = mspDB, TextDB = txtDB, IsotopeTextDB = isotopeTextDB, IupacDatabase = iupacDB, ParameterBase = param
            };

            Console.WriteLine("Start processing..");
            return Execute(container, outputFolder, isProjectSaved);
        }

        private int Execute(MsdialDataStorage container, string outputFolder, bool isProjectSaved) {
            var files = container.AnalysisFiles;
            foreach (var file in files) {
                ProcessFile.Run(file, container);
            }

            var alignmentFile = container.AlignmentFiles.First();
            var factory = new DimsAlignmentProcessFactory(container.ParameterBase as MsdialDimsParameter, container.IupacDatabase);
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(files, alignmentFile, null);

            foreach (var group in result.AlignmentSpotProperties.GroupBy(prop => prop.Ontology)) {
                Console.WriteLine(group.Key);
                foreach (var spot in group.OrderBy(s => s.MassCenter)) {
                    Console.WriteLine($"\t{spot.Name}\t{spot.AdductType.AdductIonName}\t{spot.MassCenter}");
                }
            }

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            new MsdialDimsSerializer().SaveMsdialDataStorage(container.ParameterBase.ProjectFilePath, container);

            return 0;
        }

        private async Task<int> ExecuteAsync(MsdialDataStorage container, string outputFolder, bool isProjectSaved) {
            var files = container.AnalysisFiles;
            var tasks = files.Select(file => Task.Run(() => ProcessFile.Run(file, container)));
            await Task.WhenAll(tasks);

            var alignmentFile = container.AlignmentFiles.First();
            var factory = new DimsAlignmentProcessFactory(container.ParameterBase as MsdialDimsParameter, container.IupacDatabase);
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(files, alignmentFile, null);

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            new MsdialDimsSerializer().SaveMsdialDataStorage(container.ParameterBase.ProjectFilePath, container);
            return 0;
        }
    }
}
