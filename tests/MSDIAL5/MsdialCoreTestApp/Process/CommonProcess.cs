using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
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
    public sealed class MspDataBaseAnnotatorSetting
    {
        public MspDataBaseAnnotatorSetting(MoleculeDataBase dataBase, List<MspAnnotatorSetting> annotatorSettings) {
            DataBase = dataBase;
            AnnotatorSettings = annotatorSettings;
        }

        public MoleculeDataBase DataBase { get; }
        public List<MspAnnotatorSetting> AnnotatorSettings { get; }
    }

    public sealed class TextDataBaseAnnotatorSetting
    {
        public TextDataBaseAnnotatorSetting(MoleculeDataBase dataBase, List<TextAnnotatorSetting> annotatorSettings) {
            DataBase = dataBase;
            AnnotatorSettings = annotatorSettings;
        }

        public MoleculeDataBase DataBase { get; }
        public List<TextAnnotatorSetting> AnnotatorSettings { get; }
    }

    public static class CommonProcess {

        public static bool SetProjectProperty(ParameterBase param, string input, out List<AnalysisFileBean> analysisFiles, out AlignmentFileBean alignmentFile) {

            Console.WriteLine("Loading analysis files..");
            analysisFiles = AnalysisFilesParser.ReadInput(input);
            if (analysisFiles.IsEmptyOrNull()) {
                alignmentFile = new AlignmentFileBean();
                Console.WriteLine(CommonProcess.NoFileError());
                return false;
            }
            var alignmentFolder = Path.GetDirectoryName(analysisFiles[0].AnalysisFilePath);
            if (!Directory.Exists(alignmentFolder)) {
                alignmentFile = new AlignmentFileBean();
                Console.WriteLine(CommonProcess.NoFileError());
                return false;
            }
            alignmentFile = AlignmentResultParser.GetAlignmentFileBean(alignmentFolder);
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
            if (!AnalysisFilesParser.isCsv(input)) {
                foreach (var analysisFile in analysisFiles) {
                    analysisFile.AcquisitionType = param.ProjectParam.AcquisitionType;
                }
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
            out IupacDatabase iupacDB, out MoleculeDataBase? mspDB, out MoleculeDataBase? txtDB,
            out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode,
            out MoleculeDataBase? lbmDB) {

            mspDB = null;
            txtDB = null;
            lbmDB = null;
            iupacDB = IupacResourceParser.GetIUPACDatabase();
            isotopeTextDB = new List<MoleculeMsReference>();
            compoundsInTargetMode = new List<MoleculeMsReference>();

            if (ErrorHandler.IsFileExist(param.MspFilePath)) { 
                var mspList = LibraryHandler.ReadMsLibrary(param.MspFilePath, param, out var mspError);
                mspDB = new MoleculeDataBase(mspList, "MspDB", DataBaseSource.Msp, SourceType.MspDB, param.MspFilePath);
                if (mspError != string.Empty) {
                    Console.WriteLine(mspError);
                }
            }
            if (ErrorHandler.IsFileExist(param.LbmFilePath)) {
                var lbmList = LibraryHandler.ReadMsLibrary(param.LbmFilePath, param, out var lbmError);
                lbmDB = new MoleculeDataBase(lbmList, "LbmDB", DataBaseSource.Lbm, SourceType.MspDB, param.LbmFilePath);
                if (lbmError != string.Empty) {
                    Console.WriteLine(lbmError);
                }
            }

            if (ErrorHandler.IsFileExist(param.TextDBFilePath)) {
                var txtList = LibraryHandler.ReadMsLibrary(param.TextDBFilePath, param, out var txtError);
                txtDB = new MoleculeDataBase(txtList, "TextDB", DataBaseSource.Text, SourceType.TextDB, param.TextDBFilePath);
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

        public static void ParseLibraries(ParameterBase param, float targetMz, IReadOnlyList<MspAnnotatorSetting> mspAnnotatorSettings, IReadOnlyList<TextAnnotatorSetting> textAnnotatorSettings,
            out IupacDatabase iupacDB, out List<MspDataBaseAnnotatorSetting> mspDBs, out List<TextDataBaseAnnotatorSetting> textDBs,
            out List<MoleculeMsReference> isotopeTextDB, out List<MoleculeMsReference> compoundsInTargetMode,
            out MoleculeDataBase? lbmDB) {

            mspDBs = new List<MspDataBaseAnnotatorSetting>();
            textDBs = new List<TextDataBaseAnnotatorSetting>();
            lbmDB = null;
            iupacDB = IupacResourceParser.GetIUPACDatabase();
            isotopeTextDB = new List<MoleculeMsReference>();
            compoundsInTargetMode = new List<MoleculeMsReference>();

            var effectiveMspSettings = mspAnnotatorSettings?
                .Where(setting => !setting.MspFilePath.IsEmptyOrNull())
                .ToList() ?? new List<MspAnnotatorSetting>();
            if (effectiveMspSettings.Count == 0 && ErrorHandler.IsFileExist(param.MspFilePath)) {
                effectiveMspSettings.Add(new MspAnnotatorSetting(param.MspFilePath, param.MspFilePath, 1, param.MspSearchParam));
            }

            var mspFileGroups = effectiveMspSettings
                .GroupBy(setting => Path.GetFullPath(setting.MspFilePath), StringComparer.OrdinalIgnoreCase)
                .ToList();
            for (var i = 0; i < mspFileGroups.Count; i++) {
                var group = mspFileGroups[i];
                var mspFilePath = group.Key;
                if (!ErrorHandler.IsFileExist(mspFilePath)) {
                    Console.WriteLine($"MSP file was not found: {mspFilePath}");
                    continue;
                }

                var mspList = LibraryHandler.ReadMsLibrary(mspFilePath, param, out var mspError);
                var dbId = mspFileGroups.Count == 1 && effectiveMspSettings.Count == 1 && effectiveMspSettings[0].AnnotatorId == param.MspFilePath
                    ? "MspDB"
                    : GetSafeDataBaseId("MspDB", mspFilePath, i + 1);
                var mspDB = new MoleculeDataBase(mspList, dbId, DataBaseSource.Msp, SourceType.MspDB, mspFilePath);
                if (mspError != string.Empty) {
                    Console.WriteLine(mspError);
                }
                mspDBs.Add(new MspDataBaseAnnotatorSetting(mspDB, group.ToList()));
            }

            if (ErrorHandler.IsFileExist(param.LbmFilePath)) {
                var lbmList = LibraryHandler.ReadMsLibrary(param.LbmFilePath, param, out var lbmError);
                lbmDB = new MoleculeDataBase(lbmList, "LbmDB", DataBaseSource.Lbm, SourceType.MspDB, param.LbmFilePath);
                if (lbmError != string.Empty) {
                    Console.WriteLine(lbmError);
                }
            }

            var effectiveTextSettings = textAnnotatorSettings?
                .Where(setting => !setting.TextDbFilePath.IsEmptyOrNull())
                .ToList() ?? new List<TextAnnotatorSetting>();
            if (effectiveTextSettings.Count == 0 && ErrorHandler.IsFileExist(param.TextDBFilePath)) {
                effectiveTextSettings.Add(new TextAnnotatorSetting(param.TextDBFilePath, param.TextDBFilePath, 2, param.TextDbSearchParam));
            }

            var textFileGroups = effectiveTextSettings
                .GroupBy(setting => Path.GetFullPath(setting.TextDbFilePath), StringComparer.OrdinalIgnoreCase)
                .ToList();
            for (var i = 0; i < textFileGroups.Count; i++) {
                var group = textFileGroups[i];
                var textDbFilePath = group.Key;
                if (!ErrorHandler.IsFileExist(textDbFilePath)) {
                    Console.WriteLine($"Text DB file was not found: {textDbFilePath}");
                    continue;
                }

                var txtList = LibraryHandler.ReadMsLibrary(textDbFilePath, param, out var txtError);
                var dbId = textFileGroups.Count == 1 && effectiveTextSettings.Count == 1 && effectiveTextSettings[0].AnnotatorId == param.TextDBFilePath
                    ? "TextDB"
                    : GetSafeDataBaseId("TextDB", textDbFilePath, i + 1);
                var txtDB = new MoleculeDataBase(txtList, dbId, DataBaseSource.Text, SourceType.TextDB, textDbFilePath);
                if (txtError != string.Empty) {
                    Console.WriteLine(txtError);
                }
                textDBs.Add(new TextDataBaseAnnotatorSetting(txtDB, group.ToList()));
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

        private static string GetSafeDataBaseId(string prefix, string filePath, int index) {
            var name = Path.GetFileNameWithoutExtension(filePath);
            var safeName = new string(name.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray());
            if (safeName.Length > 64) {
                safeName = safeName.Substring(0, 64);
            }
            if (safeName.IsEmptyOrNull()) {
                safeName = "library";
            }
            return $"{prefix}_{index}_{safeName}";
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
