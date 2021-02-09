using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parser;
using CompMs.MsdialImmsCore.Process;
using System;
using System.Collections.Generic;

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
            new MsdialImmsSerializer().SaveMsdialDataStorage(container.ParameterBase.ProjectFilePath, container);
            return 0;
        }
    }
}
