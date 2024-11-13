using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process
{
    public static class CommonProcess {

        public static bool SetProjectProperty(ParameterBase param, string input, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile) {

            Console.WriteLine("Loading analysis files..");
            analysisFiles = AnalysisFilesParser.ReadInput(input);
            alignmentFile = AlignmentResultParser.GetAlignmentFileBean(input);
            if (analysisFiles.IsEmptyOrNull()) {
                Console.WriteLine(CommonProcess.NoFileError());
                return false;
            }

            var dt = DateTime.Now;
            var projectFileName = $"Project-{dt:yyMMddhhmm}.mddata";
            var inputfolder = Directory.Exists(input) ? input : Path.GetDirectoryName(input);
            param.ProjectFolderPath = inputfolder;
            param.ProjectFileName = projectFileName;

            param.FileID_ClassName = analysisFiles.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileClass);
            param.FileID_AnalysisFileType = analysisFiles.ToDictionary(file => file.AnalysisFileId, file => file.AnalysisFileType);

#pragma warning disable CS0618 // Type or member is obsolete
            if (param.ProjectParam.AcquisitionType == AcquisitionType.None) {
                param.ProjectParam.AcquisitionType = AcquisitionType.DDA;
            }
            foreach (var analysisFile in analysisFiles) {
                // ProjectBaseParameter.AcquisitionType is obsolete, but is used because it is not possible to set the AcquisitionType of individual files in the Console application.
                analysisFile.AcquisitionType = param.ProjectParam.AcquisitionType;
            }
#pragma warning restore CS0618 // Type or member is obsolete
            if (param.GetType() == typeof(MsdialGcmsParameter)) {
                param.Ionization = Ionization.EI;
            }
            else {
                param.Ionization = Ionization.ESI;
            }
            return true;
        }

        public static void ParseLibraries(ParameterBase param, float targetMz,
            out IupacDatabase iupacDB, out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB,
            out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode,
            out List<MoleculeMsReference> lbmDB) {

            iupacDB = IupacResourceParser.GetIUPACDatabase();
            mspDB = new List<MoleculeMsReference>();
            txtDB = new List<MoleculeMsReference>();
            lbmDB = new List<MoleculeMsReference>();
            isotopeTextDB = new List<MoleculeMsReference>();
            compoundsInTargetMode = new List<MoleculeMsReference>();

            if (ErrorHandler.IsFileExist(param.MspFilePath)) { 
                mspDB = LibraryHandler.ReadMsLibrary(param.MspFilePath, param, out var mspError);
                if (mspError != string.Empty) {
                    Console.WriteLine(mspError);
                }
            }
            if (ErrorHandler.IsFileExist(param.LbmFilePath)) {
                lbmDB = LibraryHandler.ReadMsLibrary(param.LbmFilePath, param, out var lbmError);
                if (lbmError != string.Empty) {
                    Console.WriteLine(lbmError);
                }
            }

            if (ErrorHandler.IsFileExist(param.TextDBFilePath)) {
                txtDB = LibraryHandler.ReadMsLibrary(param.TextDBFilePath, param, out var txtError);
                if (txtError != string.Empty) {
                    Console.WriteLine(txtError);
                }
            }

            if (ErrorHandler.IsFileExist(param.IsotopeTextDBFilePath)) {
                isotopeTextDB = TextLibraryParser.TextLibraryReader(param.IsotopeTextDBFilePath, out string errorInIsitopeTextDB);
                if (errorInIsitopeTextDB != string.Empty) Console.WriteLine(errorInIsitopeTextDB);
            }

            if (ErrorHandler.IsFileExist(param.CompoundListInTargetModePath)) {
                compoundsInTargetMode = TextLibraryParser.CompoundListInTargetModeReader(param.CompoundListInTargetModePath, out string errorInTargetModeLib);
                if (errorInTargetModeLib != string.Empty) Console.WriteLine(errorInTargetModeLib);
            }

            if (targetMz > 0) {
                if (compoundsInTargetMode.IsEmptyOrNull()) {
                    compoundsInTargetMode = new List<MoleculeMsReference>();
                }
                compoundsInTargetMode.Add(new MoleculeMsReference() { Name = "Target", PrecursorMz = targetMz, MassTolerance = param.MassSliceWidth });
            }
        }

        public static void SetLipidQueries(ParameterBase param) {
            var exeDir = AppDomain.CurrentDomain.BaseDirectory;
            var iniLipidPath = exeDir + "LipidQueries.INI";
            if (!File.Exists(iniLipidPath)) {
                using (var sw = new StreamWriter(iniLipidPath, false, Encoding.ASCII)) {
                    sw.WriteLine("Class\tAdduct\tIon mode\tIsSelected");
                    var queries = param.LipidQueryContainer.LbmQueries;
                    foreach (var query in queries) {
                        sw.WriteLine(query.LbmClass + "\t" + query.AdductType.AdductIonName + "\t" + query.IonMode.ToString() + "\t" + query.IsSelected.ToString());
                    }
                }
            }
            param.LipidQueryContainer.LbmQueries = LbmQueryParcer.GetLbmQueries(iniLipidPath, false);
        }

        public static string NoFileError() {
            return "No input file existed.";
        }

        public static string NoLibraryFileError() {
            return "No library file";
        }
    }
}
