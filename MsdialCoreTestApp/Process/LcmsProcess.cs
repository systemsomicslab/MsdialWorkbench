using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process {
    public class LcmsProcess
    {
        public int Run(string inputFolder, string outputFolder, string methodFile, bool isProjectSaved, float targetMz)
        {
            Console.WriteLine("Loading library files..");
            Console.WriteLine(String.Format("inputFolder: {0} -- outputFolder: {1} -- method: {2}", inputFolder, outputFolder, methodFile));
            var analysisFiles = AnalysisFilesParser.ReadInput(inputFolder);
            var param = ConfigParser.ReadForLcmsParameter(methodFile);
            var alignmentFile = AlignmentResultParser.GetAlignmentFileBean(inputFolder);
            if (analysisFiles.IsEmptyOrNull()) {
                Console.WriteLine(CommonProcess.NoFileError());
                return -1;
            }
            CommonProcess.SetProjectProperty(param, inputFolder);

            var iupacDB = IupacResourceParser.GetIUPACDatabase();
            var mspDB = new List<MoleculeMsReference>();
           
            if (param.TargetOmics == TargetOmics.Lipidomics) {
                CommonProcess.SetLipidQueries(param);
                if (!ErrorHandler.IsFileExist(param.MspFilePath)) {
                    Console.WriteLine(CommonProcess.NoLibraryFileError());
                    return -1;
                }
                mspDB = LibraryHandler.ReadLipidMsLibrary(param.MspFilePath, param);
            }
            else {
                if (ErrorHandler.IsFileExist(param.MspFilePath)) {
                    mspDB = LibraryHandler.ReadMspLibrary(param.MspFilePath);
                    mspDB = mspDB.OrderBy(n => n.PrecursorMz).ToList();
                    var counter = 0;
                    foreach (var query in mspDB) {
                        query.ScanID = counter; counter++;
                    }
                }
                else {
                    Console.WriteLine(CommonProcess.NoLibraryFileError());
                }
            }
           
            var txtDB = new List<MoleculeMsReference>();
            if (ErrorHandler.IsFileExist(param.TextDBFilePath)) {
                txtDB = TextLibraryParser.TextLibraryReader(param.TextDBFilePath, out string errorInTextDB);
                if (errorInTextDB != string.Empty) Console.WriteLine(errorInTextDB);
            }

            var isotopeTextDB = new List<MoleculeMsReference>();
            if (ErrorHandler.IsFileExist(param.IsotopeTextDBFilePath)) {
                isotopeTextDB = TextLibraryParser.TextLibraryReader(param.IsotopeTextDBFilePath, out string errorInIsitopeTextDB);
                if (errorInIsitopeTextDB != string.Empty) Console.WriteLine(errorInIsitopeTextDB);
            }

            if (ErrorHandler.IsFileExist(param.CompoundListInTargetModePath)) {
                param.CompoundListInTargetMode = TextLibraryParser.CompoundListInTargetModeReader(param.CompoundListInTargetModePath, out string errorInTargetModeLib);
                if (errorInTargetModeLib != string.Empty) Console.WriteLine(errorInTargetModeLib);
            }
            
            if (targetMz > 0) {
                if (param.CompoundListInTargetMode == null || param.CompoundListInTargetMode.Count == 0) {
                    param.CompoundListInTargetMode = new List<MoleculeMsReference>();
                }
                param.CompoundListInTargetMode.Add(new MoleculeMsReference() { Name = "Target", PrecursorMz = targetMz, MassTolerance = param.MassSliceWidth });
            }

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
            return 0;
        }

    }
}
