using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parser;
using CompMs.Common.Query;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.Process {
    public static class CommonProcess {

        public static void SetProjectProperty(ParameterBase param, string inputfolder) {
            var dt = DateTime.Now;
            var projectFileName = "Project-" + dt.Year.ToString() + dt.Month.ToString() + dt.Day.ToString() + dt.Hour.ToString() + dt.Minute.ToString() + ".mtd2";
            param.ProjectFolderPath = inputfolder;
            param.ProjectFilePath = inputfolder + "\t" + projectFileName;

            if (param.GetType() == typeof(MsdialGcmsParameter)) {
                param.Ionization = Common.Enum.Ionization.EI;
            }
            else {
                param.Ionization = Common.Enum.Ionization.ESI;
            }
        }

        public static void ParseLibraries(ParameterBase param, out List<MoleculeMsReference> mspDB, out List<MoleculeMsReference> txtDB, out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode) {

            mspDB = new List<MoleculeMsReference>();
            txtDB = new List<MoleculeMsReference>();
            isotopeTextDB = new List<MoleculeMsReference>();
            compoundsInTargetMode = new List<MoleculeMsReference>();

            if (param.TargetOmics == TargetOmics.Lipidomics) {
                CommonProcess.SetLipidQueries(param);
                if (!ErrorHandler.IsFileExist(param.MspFilePath)) {
                    Console.WriteLine(CommonProcess.NoLibraryFileError());
                    return;
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

            if (ErrorHandler.IsFileExist(param.TextDBFilePath)) {
                txtDB = TextLibraryParser.TextLibraryReader(param.TextDBFilePath, out string errorInTextDB);
                if (errorInTextDB != string.Empty) Console.WriteLine(errorInTextDB);
            }

            if (ErrorHandler.IsFileExist(param.IsotopeTextDBFilePath)) {
                isotopeTextDB = TextLibraryParser.TextLibraryReader(param.IsotopeTextDBFilePath, out string errorInIsitopeTextDB);
                if (errorInIsitopeTextDB != string.Empty) Console.WriteLine(errorInIsitopeTextDB);
            }

            if (ErrorHandler.IsFileExist(param.CompoundListInTargetModePath)) {
                compoundsInTargetMode = TextLibraryParser.CompoundListInTargetModeReader(param.CompoundListInTargetModePath, out string errorInTargetModeLib);
                if (errorInTargetModeLib != string.Empty) Console.WriteLine(errorInTargetModeLib);
            }
        }

        public static void SetLipidQueries(ParameterBase param) {
            var exeDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var iniLipidPath = exeDir + "LipidQueries.INI";
            var solvent = param.LipidQueryContainer.SolventType;
            if (!System.IO.File.Exists(iniLipidPath)) {
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
