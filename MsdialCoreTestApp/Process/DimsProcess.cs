using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.DataObj;
using CompMs.Common.Extension;
using System;

using System.Collections.Generic;
using System.Text;
using CompMs.MsdialDimsCore.Parser;

namespace CompMs.App.MsdialConsole.Process {
    public class DimsProcess {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz) {
            var param = ConfigParser.ReadForDimsParameter(methodFile);
            var isCorrectlyImported = CommonProcess.SetProjectProperty(param, inputFolder, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile);
            if (!isCorrectlyImported) return -1;
            CommonProcess.ParseLibraries(param, targetMz, out IupacDatabase iupacDB,
                out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode);

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

            }
            new MsdialDimsSerializer().SaveMsdialDataStorage(container.ParameterBase.ProjectFilePath, container);
            return 0;
        }
    }
}
