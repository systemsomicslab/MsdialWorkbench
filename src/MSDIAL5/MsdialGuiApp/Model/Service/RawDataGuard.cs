using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.ViewModel.Service;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CompMs.App.Msdial.Model.Service
{
    /// <summary>
    /// What MS-DIAL says when the raw data files of a project are missing: the prompt shown when a dataset opens,
    /// why a sample or a command is refused, and the titles of the panels that stay empty.
    /// Every message here names a path or an exception, so each goes through <see cref="ErrorMessageBoxRequest"/>,
    /// a message box that fits them.
    /// </summary>
    internal static class RawDataGuard
    {
        internal const int MaxListedPaths = 10;

        public const string FileTitleSuffix = " (raw data not found)";
        public const string EicTitle = "EIC unavailable: the raw data file of this sample was not found.";
        public const string Ms1SpectrumTitle = "MS1 spectrum unavailable: the raw data file of this sample was not found.";
        public const string RawSpectrumTitle = "Raw spectrum unavailable: the raw data file of this sample was not found.";
        public const string MeasureVsReferenceTitle = "Measure vs. Reference (raw MS/MS spectrum unavailable: the raw data file of this sample was not found)";
        public const string RawMs2ChromatogramTitle = "Raw MS/MS chromatogram unavailable: the raw data file of this sample was not found.";

        private const string RawDataNotFoundCaption = "Raw data not found";
        private const string RawDataNotAvailableCaption = "Raw data not available";

        /// <summary>
        /// Asks once, before a dataset whose raw data files are missing is opened, whether to open it with what the project stores.
        /// </summary>
        /// <returns>true to open the dataset: nothing is missing, or the user answered Yes.</returns>
        public static bool ConfirmOpen(IMessageBroker broker, BrowsingWithoutRawData mode, IReadOnlyList<AnalysisFileBeanModel> missing, int sampleCount) {
            if (missing.Count == 0) {
                return true;
            }
            var counts = $"Raw data are not available for {missing.Count} of {sampleCount} samples in this dataset";
            var list = "Raw data files not found:\n" + FormatPaths(missing.Select(file => file.AnalysisFilePath).ToList());
            ErrorMessageBoxRequest request;
            switch (mode) {
                case BrowsingWithoutRawData.Supported:
                    request = new ErrorMessageBoxRequest
                    {
                        Caption = RawDataNotFoundCaption,
                        Content = $"{counts}, so only the information saved in the project will be drawn for them.\n\n"
                            + "Still available: peak spots, peak information, deconvoluted MS/MS spectra, deconvoluted MS/MS chromatograms (SWATH/AIF), "
                            + "the peak list export (its MS1 isotope column is written as null) and alignment results.\n"
                            + "Not available for these samples: MS1 spectra, EICs, raw MS/MS spectra and chromatograms, TIC/BPC, the experiment spectrum, "
                            + "the export of centroid spectra, and reprocessing.\n\n"
                            + $"{list}\n\n"
                            + "Continue?",
                        ButtonType = MessageBoxButton.YesNo,
                        Icon = MessageBoxImage.Warning,
                    };
                    break;
                case BrowsingWithoutRawData.AlignmentsOnly:
                    request = new ErrorMessageBoxRequest
                    {
                        Caption = RawDataNotFoundCaption,
                        Content = $"{counts}, so only the information saved in the project will be drawn.\n\n"
                            + "Alignment results can be browsed. For this type of project the peak spot viewer reads the raw data, "
                            + "so these samples cannot be opened in it, and reprocessing is not available.\n\n"
                            + $"{list}\n\n"
                            + "Continue?",
                        ButtonType = MessageBoxButton.YesNo,
                        Icon = MessageBoxImage.Warning,
                    };
                    break;
                default:
                    broker.Publish(new ErrorMessageBoxRequest
                    {
                        Caption = RawDataNotFoundCaption,
                        Content = $"{counts}.\n\n"
                            + "Imaging projects draw their ion images from the raw data, so this project cannot be opened without them.\n\n"
                            + $"{list}\n\n"
                            + "Put the files back at these paths and open the project again.",
                        ButtonType = MessageBoxButton.OK,
                        Icon = MessageBoxImage.Error,
                    });
                    return false;
            }
            broker.Publish(request);
            return request.Result == MessageBoxResult.Yes;
        }

        /// <summary>
        /// Says that <paramref name="feature"/> needs the raw data of the open sample, when they are missing.
        /// </summary>
        /// <returns>true when the raw data are available and the feature can run.</returns>
        public static bool Ensure(bool isRawDataAvailable, AnalysisFileBeanModel file, IMessageBroker broker, string feature) {
            if (isRawDataAvailable) {
                return true;
            }
            broker.Publish(new ErrorMessageBoxRequest
            {
                Caption = RawDataNotAvailableCaption,
                Content = $"{feature} needs the raw data, and the raw data file of {file.AnalysisFileName} was not found:\n"
                    + $"  {file.AnalysisFilePath}\n\n"
                    + "Put the file back at this path, then open another sample and this one again to reload it.",
                ButtonType = MessageBoxButton.OK,
                Icon = MessageBoxImage.Information,
            });
            return false;
        }

        /// <summary>
        /// Checks again that the raw data of every sample are present, and says that <paramref name="feature"/> cannot run when some are not.
        /// </summary>
        /// <returns>true when every raw data file is present.</returns>
        public static bool EnsureAll(IEnumerable<AnalysisFileBeanModel> files, IMessageBroker broker, string feature) {
            var all = files.ToList();
            var missing = all.Where(file => !file.RefreshRawDataAvailability()).ToList();
            if (missing.Count == 0) {
                return true;
            }
            broker.Publish(new ErrorMessageBoxRequest
            {
                Caption = RawDataNotAvailableCaption,
                Content = $"{feature} cannot run because the raw data files of {missing.Count} of {all.Count} samples were not found. "
                    + $"{feature} reads the raw data of every sample again.\n\n"
                    + "Raw data files not found:\n"
                    + $"{FormatPaths(missing.Select(file => file.AnalysisFilePath).ToList())}\n\n"
                    + "Put the files back at these paths to run it. The saved results can still be browsed.",
                ButtonType = MessageBoxButton.OK,
                Icon = MessageBoxImage.Information,
            });
            return false;
        }

        public static ErrorMessageBoxRequest SampleNeedsRawData(AnalysisFileBeanModel file) {
            return new ErrorMessageBoxRequest
            {
                Caption = RawDataNotAvailableCaption,
                Content = $"{file.AnalysisFileName} cannot be opened in the peak spot viewer: for this type of project the viewer reads the raw data, "
                    + "and the raw data file was not found:\n"
                    + $"  {file.AnalysisFilePath}\n\n"
                    + "Alignment results can still be browsed.",
                ButtonType = MessageBoxButton.OK,
                Icon = MessageBoxImage.Information,
            };
        }

        public static ErrorMessageBoxRequest PeakListMissing(AnalysisFileBeanModel file) {
            return new ErrorMessageBoxRequest
            {
                Caption = "Saved results not found",
                Content = $"{file.AnalysisFileName} cannot be opened in the peak spot viewer because its saved peak list was not found:\n"
                    + $"  {file.PeakListFilePath}",
                ButtonType = MessageBoxButton.OK,
                Icon = MessageBoxImage.Information,
            };
        }

        public static ErrorMessageBoxRequest SampleOpenFailed(AnalysisFileBeanModel file, Exception error) {
            return new ErrorMessageBoxRequest
            {
                Caption = "Sample could not be opened",
                Content = $"{file.AnalysisFileName} could not be opened in the peak spot viewer.\n\n"
                    + $"{error.GetType().Name}: {error.Message}\n\n"
                    + "The project is still open. You can select another sample or an alignment result.",
                ButtonType = MessageBoxButton.OK,
                Icon = MessageBoxImage.Error,
            };
        }

        public static ErrorMessageBoxRequest AlignmentOpenFailed(AlignmentFileBeanModel file, Exception error) {
            return new ErrorMessageBoxRequest
            {
                Caption = "Alignment result could not be opened",
                Content = $"{file.FileName} could not be opened in the alignment viewer.\n\n"
                    + $"{error.GetType().Name}: {error.Message}\n\n"
                    + "The project is still open. You can select a sample or another alignment result.",
                ButtonType = MessageBoxButton.OK,
                Icon = MessageBoxImage.Error,
            };
        }

        public static ErrorMessageBoxRequest MsfinderWithoutMs1(AnalysisFileBeanModel file) {
            return new ErrorMessageBoxRequest
            {
                Caption = RawDataNotAvailableCaption,
                Content = $"The raw data file of {file.AnalysisFileName} was not found, so its MS1 isotope pattern cannot be read. "
                    + "MS-FINDER receives the MS/MS spectrum only, and its formula ranking may differ from a run with the raw data.\n\n"
                    + "Sending the same feature from the alignment result includes the isotope pattern saved with the alignment spot.",
                ButtonType = MessageBoxButton.OK,
                Icon = MessageBoxImage.Information,
            };
        }

        /// <summary>
        /// One path per line, indented, at most <see cref="MaxListedPaths"/> of them and then how many more there are.
        /// </summary>
        internal static string FormatPaths(IReadOnlyCollection<string> paths) {
            var lines = paths.Take(MaxListedPaths).Select(path => $"  {path}").ToList();
            if (paths.Count > MaxListedPaths) {
                lines.Add($"  ... and {paths.Count - MaxListedPaths} more");
            }
            return string.Join("\n", lines);
        }
    }
}
