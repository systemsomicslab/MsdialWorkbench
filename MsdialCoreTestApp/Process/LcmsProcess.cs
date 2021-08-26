using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcMsApi.Parser;
using CompMs.MsdialLcMsApi.Process;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.MsdialConsole.Process {
    public class LcmsProcess
    {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz)
        {
            var param = ConfigParser.ReadForLcmsParameter(methodFile);
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
            var tasks = new Task[files.Count];
            foreach ((var file, var idx) in files.WithIndex()) {
                var provider = new StandardDataProvider(file, false, 5);
                tasks[idx] = Task.Run(() => FileProcess.Run(file, provider, container));
            }
            Task.WaitAll(tasks);

            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");
            var alignmentFile = container.AlignmentFiles.First();
            var factory = new LcmsAlignmentProcessFactory(container.ParameterBase as MsdialLcmsParameter, container.IupacDatabase, container.DataBaseMapper);
            var aligner = factory.CreatePeakAligner();
            var result = aligner.Alignment(files, alignmentFile, serializer);

            Common.MessagePack.MessagePackHandler.SaveToFile(result, alignmentFile.FilePath);
            new MsdialLcmsSerializer().SaveMsdialDataStorage(container.ParameterBase.ProjectFilePath, container);
            return 0;
        }
    }
}
