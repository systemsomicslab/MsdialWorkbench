using CompMs.Common;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);

            AppDomain.CurrentDomain.UnhandledException += (s, e) => LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            DispatcherUnhandledException += (s, e) => {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };
            TaskScheduler.UnobservedTaskException += (s, e) => {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        /// <summary>
        /// Reports a crash in a form the person who has to fix it can act on.
        ///
        /// The dialog used to print the outermost exception only. That is close to useless for the
        /// failures users actually hit: when anything throws inside the MainWindow constructor, WPF
        /// hands us a XamlParseException whose message is always "The invocation of the constructor
        /// on type ... threw an exception" and whose stack trace is entirely WPF's own. Everything
        /// that says what went wrong sits in InnerException, which we were discarding, so those
        /// screenshots arrive in the mailbox unanswerable.
        ///
        /// The whole chain is reported now, and the same report is written to a file the user can
        /// attach, because a screenshot cannot be scrolled or copied out of.
        /// </summary>
        private void LogUnhandledException(Exception exception, string source) {
            if (exception is AggregateException aggregate) {
                exception = aggregate.Flatten().InnerException ?? aggregate;
            }

            var report = BuildReport(exception, source);
            var reportPath = TryWriteReport(report);

            System.Diagnostics.Debug.Write(report);
            MessageBox.Show(BuildDialogMessage(exception, reportPath), "Unexpected exception occured.");
        }

        private static string BuildDialogMessage(Exception exception, string? reportPath) {
            var cause = InnermostCause(exception);
            var builder = new StringBuilder();
            builder.AppendLine($"Unhandled exception in MSDIAL v{TryGetVersion()}")
                .AppendLine()
                .AppendLine($"{cause.GetType().Name}: {cause.Message}")
                .AppendLine();
            if (reportPath is null) {
                builder.AppendLine("The full report could not be saved to a file. Please send a screenshot of this dialog.");
            }
            else {
                builder.AppendLine("A full report was saved to:")
                    .AppendLine(reportPath)
                    .AppendLine("Please attach that file when you report this problem.");
            }
            return builder.AppendLine()
                .AppendLine("Details below:")
                .AppendLine()
                .Append(DescribeExceptionChain(exception))
                .ToString();
        }

        private static string BuildReport(Exception exception, string source) {
            var builder = new StringBuilder();
            builder.AppendLine($"MS-DIAL crash report {DateTime.Now:yyyy-MM-dd HH:mm:ss zzz}")
                .AppendLine($"Version        : {TryGetVersion()}")
                .AppendLine($"Reported by    : {source}")
                .AppendLine($"Process        : {(Environment.Is64BitProcess ? "64-bit" : "32-bit")} on {(Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit")} Windows")
                .AppendLine($"OS             : {Environment.OSVersion}")
                .AppendLine($"CLR            : {Environment.Version}")
                .AppendLine($"Working set    : {Environment.WorkingSet / (1024 * 1024)} MB")
                .AppendLine($"Executable     : {TryGetExecutablePath()}")
                .AppendLine()
                .Append(DescribeExceptionChain(exception));
            return builder.ToString();
        }

        private static string DescribeExceptionChain(Exception exception) {
            var builder = new StringBuilder();
            var depth = 0;
            for (Exception? current = exception; current is not null; current = current.InnerException, depth++) {
                builder.AppendLine(depth == 0 ? "Exception:" : $"Caused by ({depth}):")
                    .AppendLine($"  {current.GetType().FullName}: {current.Message}");

                // The two startup failures that reach us most often each carry the one fact that
                // identifies them, and neither of them puts it in the message.
                if (current is System.Configuration.ConfigurationErrorsException configuration && !string.IsNullOrEmpty(configuration.Filename)) {
                    builder.AppendLine($"  Configuration file: {configuration.Filename} (line {configuration.Line})");
                }
                if (current is FileNotFoundException notFound) {
                    builder.AppendLine($"  Missing assembly or file: {notFound.FileName}");
                    if (!string.IsNullOrEmpty(notFound.FusionLog)) {
                        builder.AppendLine($"  Fusion log: {notFound.FusionLog}");
                    }
                }

                builder.AppendLine(current.StackTrace ?? "  (no stack trace)");
            }
            return builder.ToString();
        }

        private static Exception InnermostCause(Exception exception) {
            while (exception.InnerException is Exception inner) {
                exception = inner;
            }
            return exception;
        }

        private static string? TryWriteReport(string report) {
            foreach (var directory in ReportDirectoryCandidates()) {
                try {
                    Directory.CreateDirectory(directory);
                    var path = Path.Combine(directory, $"MSDIAL-crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}.log");
                    File.WriteAllText(path, report, Encoding.UTF8);
                    return path;
                }
                catch (Exception writeFailure) {
                    System.Diagnostics.Debug.Write($"Could not write the crash report to {directory}\n\n{writeFailure.Message}");
                }
            }
            return null;
        }

        private static string[] ReportDirectoryCandidates() {
            // The folder the executable was unpacked into is often read-only, so the report goes
            // where the user settings already go, with the temporary directory as a last resort.
            try {
                return new[] {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSDIAL", "crash"),
                    Path.Combine(Path.GetTempPath(), "MSDIAL", "crash"),
                };
            }
            catch (Exception) {
                return Array.Empty<string>();
            }
        }

        private static string TryGetVersion() {
            try {
                return MsdialBuildIdentity.FullIdentity;
            }
            catch (Exception) {
                return "unknown";
            }
        }

        private static string TryGetExecutablePath() {
            try {
                return System.Reflection.Assembly.GetExecutingAssembly().Location;
            }
            catch (Exception) {
                return "unknown";
            }
        }
    }
}
