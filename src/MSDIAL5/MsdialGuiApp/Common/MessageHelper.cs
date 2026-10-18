using System;
using System.IO;

namespace CompMs.App.Msdial.Common
{
    internal static class MessageHelper
    {
        public const string ProjectIsNotCreated = "Project is not created.";
        public const string DatasetIsNotSelected = "Dataset is not selected.";
        public const string NoPeakSelected = "No peak spot selected.";
        public const string SelectPeakBeforeExport = "No peak spot selected.\nPlease select peak before export.";

        public static string DatasetFileNotFound(string datasetFileName, string missingPath) {
            return $"The dataset \"{datasetFileName}\" could not be loaded because this file was not found:\n  {missingPath}";
        }

        /// <summary>
        /// Why a dataset of a project could not be loaded: the file that was not found, or else the exception that stopped it.
        /// </summary>
        public static string DatasetNotLoaded(string datasetFileName, Exception error) {
            if (error is FileNotFoundException notFound) {
                return DatasetFileNotFound(datasetFileName, notFound.FileName ?? notFound.Message);
            }
            var cause = error.GetBaseException();
            return $"The dataset \"{datasetFileName}\" could not be loaded:\n  {cause.GetType().Name}: {cause.Message}";
        }

        /// <summary>
        /// The title of the picker offered for a dataset a file of which was not found. When that file is beside the dataset file,
        /// a dataset file picked elsewhere is read with the files beside it, so what to look for is a copy that has that file too.
        /// </summary>
        public static string LocateDatasetFileTitle(string datasetFileName, string? missingPath = null) {
            var missingFile = FileNameOf(missingPath);
            if (missingFile is null || string.Equals(missingFile, datasetFileName, StringComparison.OrdinalIgnoreCase)) {
                return $"Locate the dataset file {datasetFileName}";
            }
            return $"Locate a copy of the dataset file {datasetFileName} that has {missingFile} beside it";
        }

        private static string? FileNameOf(string? path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            try {
                return Path.GetFileName(path);
            }
            catch (ArgumentException) {
                return null;
            }
        }
    }
}
