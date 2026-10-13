using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Service;
using Reactive.Bindings.Notifiers;
using System;
using System.IO;

namespace CompMs.App.Msdial.Model.Export
{
    /// <summary>
    /// Says out loud that an export did not finish.
    ///
    /// Exports run inside a Task that nothing awaits, so an exception thrown in one used to be
    /// captured by that Task and never looked at again: the progress entry stayed on screen, no
    /// files appeared, and nothing said why. Reports of that reach us as "MS-DIAL closes when we
    /// export", because a process that has just failed to allocate tends not to survive long.
    /// </summary>
    internal static class ExportFailure
    {
        public static void Report(IMessageBroker broker, Exception error) {
            broker.Publish(new ErrorMessageBoxRequest
            {
                Caption = "Export failed",
                Content = Describe(error),
            });
        }

        public static string Describe(Exception error) {
            // A synchronous read of the raw data throws the AggregateException of the task it waited on.
            if (error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Count == 1) {
                error = aggregate.Flatten().InnerExceptions[0];
            }
            switch (error) {
                case OutOfMemoryException _:
                    return "MS-DIAL ran out of memory while exporting and the export was not completed.\n\n"
                        + $"This build runs as a {(Environment.Is64BitProcess ? "64-bit" : "32-bit")} process"
                        + (Environment.Is64BitProcess
                            ? ", so the limit is the memory available on this computer.\n\n"
                            : ", which cannot use more than about 4 GB however much memory this computer has.\n\n")
                        + "Please export fewer files or fewer formats at a time, or narrow the exported peaks with the filter, and try again.";
                case UnauthorizedAccessException _:
                    return $"MS-DIAL is not allowed to write to the export folder.\n\n{error.Message}";
                case RawDataNotAvailableException e:
                    return $"The export was not completed because the raw data file of {e.AnalysisFileName} was not found:\n  {e.FileName}\n\n"
                        + "Centroid spectra are read from the raw data. The peak table and the deconvoluted spectra (MSP, MGF, SDF) are read from the project: "
                        + "choose the deconvoluted spectra to export them. The export stopped at this sample.";
                case FileNotFoundException e:
                    return MissingInput(e.FileName, e.Message);
                case FileLoadException e:
                    return MissingInput(e.FileName, e.Message);
                case IOException _:
                    return $"MS-DIAL could not write the exported files. The disk may be full, or a file may be open in another application.\n\n{error.Message}";
                default:
                    return $"The export was not completed.\n\n{error.GetType().Name}: {error.Message}";
            }
        }

        private static string MissingInput(string? fileName, string message) {
            return $"The export was not completed because a file it reads was not found or could not be read:\n  {(string.IsNullOrEmpty(fileName) ? message : fileName)}";
        }
    }
}
