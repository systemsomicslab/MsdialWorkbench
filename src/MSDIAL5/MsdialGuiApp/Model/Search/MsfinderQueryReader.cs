using CompMs.Common.DataObj;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using System;
using System.IO;

namespace CompMs.App.Msdial.Model.Search {
    internal static class MsfinderQueryReader {
        public static RawData Read(MsfinderQueryFile queryFile, AnalysisParamOfMsfinder parameter) {
            try {
                return RawDataParcer.RawDataFileReader(queryFile.RawDataFilePath, parameter);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is FormatException || ex is ArgumentException || ex is IndexOutOfRangeException) {
                throw new InvalidDataException($"Could not read MS-FINDER input file '{queryFile.RawDataFilePath}': {ex.Message}", ex);
            }
        }

        public static void EnsureNotEmpty(int queryCount, string folderPath) {
            if (queryCount == 0) {
                throw new InvalidDataException($"No MS-FINDER input files (*.mat) were found in '{folderPath}'.");
            }
        }
    }
}
