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

        public static string LocateDatasetFileTitle(string datasetFileName) {
            return $"Locate the dataset file {datasetFileName}";
        }
    }
}
