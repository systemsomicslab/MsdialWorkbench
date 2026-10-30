using FlaUI.Core.AutomationElements;
using FlaUI.Core.Capturing;
using FlaUI.Core.Definitions;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using FlaUIApplication = FlaUI.Core.Application;

namespace CompMs.App.Msdial.UiTests.Infrastructure
{
    /// <summary>
    /// One running MSDIAL.exe under UI Automation, with the checks that decide whether it crashed.
    ///
    /// MS-DIAL handles its own unhandled exceptions: it shows a dialog, writes a crash report and
    /// keeps running. So "the process is still alive" says nothing, and <see cref="AssertHealthy"/>
    /// looks for that dialog and that report as well. Call it after every step a test takes, so a
    /// failure names the step that caused it.
    /// </summary>
    internal sealed class MsdialSession : IDisposable
    {
        // The caption App.LogUnhandledException gives its MessageBox, typo included.
        private const string CrashDialogTitle = "Unexpected exception occured.";

        private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan ResponsiveTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan ExitTimeout = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan SettleTime = TimeSpan.FromSeconds(2);

        private readonly TestContext _context;
        private readonly CrashReportWatcher _crashReports;
        // Kept alongside _application because only the Process object that started MS-DIAL can
        // report its exit code; FlaUI's Application does not hold on to it.
        private readonly Process _process;
        private readonly FlaUIApplication _application;
        private readonly UIA3Automation _automation;

        private MsdialSession(TestContext context, CrashReportWatcher crashReports, Process process, FlaUIApplication application, UIA3Automation automation, Window mainWindow) {
            _context = context;
            _crashReports = crashReports;
            _process = process;
            _application = application;
            _automation = automation;
            MainWindow = mainWindow;
        }

        public Window MainWindow { get; }

        public UIA3Automation Automation => _automation;

        public static MsdialSession Launch(TestContext context, string exePath) {
            var crashReports = CrashReportWatcher.Start();
            var startInfo = new ProcessStartInfo(exePath) {
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = false,
            };
            var process = Process.Start(startInfo);
            var application = FlaUIApplication.Attach(process.Id);
            var automation = new UIA3Automation();
            Window? mainWindow;
            try {
                mainWindow = application.GetMainWindow(automation, StartupTimeout);
            }
            catch (Exception) {
                mainWindow = null;
            }
            var session = new MsdialSession(context, crashReports, process, application, automation, mainWindow!);
            try {
                if (mainWindow is null) {
                    session.Fail("startup", new[] { $"No main window appeared within {StartupTimeout.TotalSeconds:0} s." });
                }
                session.WaitUntilResponsive("startup");
                return session;
            }
            catch {
                session.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Fails the test when MS-DIAL has crashed, hung or exited since it was launched.
        /// </summary>
        /// <param name="step">What the test just did, for the failure message.</param>
        public void AssertHealthy(string step) {
            var problems = FindProblems();
            if (problems.Count > 0) {
                Fail(step, problems);
            }
        }

        /// <summary>
        /// Polls <paramref name="find"/> until it returns something, checking all the while that
        /// MS-DIAL is healthy and that no dialog has opened that the test did not ask for. An
        /// unexpected dialog fails the test with its text: it is either an error MS-DIAL is
        /// reporting, or a question the test cannot answer and would otherwise wait out.
        /// </summary>
        /// <param name="step">What the test is waiting for, for the failure message.</param>
        /// <param name="allowDialog">Which open dialogs, by title, belong to this step.</param>
        public T WaitFor<T>(string step, Func<T?> find, TimeSpan timeout, Func<string, bool>? allowDialog = null) where T : class {
            var stopwatch = Stopwatch.StartNew();
            while (true) {
                AssertHealthy(step);
                var unexpected = OpenDialogs().Where(dialog => !(allowDialog?.Invoke(SafeTitle(dialog) ?? string.Empty) ?? false)).ToList();
                if (unexpected.Count > 0) {
                    Fail(step, unexpected.Select(dialog => $"An unexpected dialog '{SafeTitle(dialog)}' is open:{Environment.NewLine}{DescribeText(dialog)}").ToList());
                }
                T? found;
                try {
                    found = find();
                }
                catch (Exception) {
                    // The tree changes under us while MS-DIAL works; try again on the next round.
                    found = null;
                }
                if (found is not null) {
                    return found;
                }
                if (stopwatch.Elapsed > timeout) {
                    Fail(step, new[] { $"It did not happen within {timeout.TotalSeconds:0} s." });
                }
                System.Threading.Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Waits for the dialog titled <paramref name="title"/>, while the dialogs in
        /// <paramref name="alsoOpen"/> may stay open beneath it.
        /// </summary>
        public Window WaitForDialog(string title, TimeSpan timeout, params string[] alsoOpen) {
            return WaitFor($"waiting for the '{title}' dialog",
                () => OpenDialogs().FirstOrDefault(dialog => SafeTitle(dialog) == title),
                timeout,
                open => open == title || alsoOpen.Contains(open));
        }

        public bool IsDialogOpen(string title) {
            return OpenDialogs().Any(dialog => SafeTitle(dialog) == title);
        }

        /// <summary>
        /// Gives MS-DIAL a moment to react to the last action while still watching for a crash, so
        /// one that happens shortly after an action is blamed on it rather than on the next step.
        /// </summary>
        public void Settle(string step, params string[] allowedDialogs) {
            Settle(step, allowedDialogs.Contains);
        }

        public void Settle(string step, Func<string, bool> allowDialog) {
            var stopwatch = Stopwatch.StartNew();
            WaitFor(step, () => stopwatch.Elapsed > SettleTime ? string.Empty : null, SettleTime + ResponsiveTimeout, allowDialog);
        }

        /// <summary>
        /// Fails the test as if MS-DIAL were unhealthy, attaching the same evidence.
        /// </summary>
        public void Fail(string step, string problem) {
            Fail(step, new[] { problem });
        }

        /// <summary>
        /// Closes the main window the way a user would and fails if MS-DIAL does not exit cleanly.
        /// </summary>
        public void Close() {
            AssertHealthy("before closing");
            MainWindow.Close();
            if (!_process.WaitForExit((int)ExitTimeout.TotalMilliseconds)) {
                Fail("closing", new[] { $"MS-DIAL was still running {ExitTimeout.TotalSeconds:0} s after its main window was closed." }.Concat(FindProblems()).ToList());
            }
            var problems = new List<string>();
            if (_process.ExitCode != 0) {
                problems.Add($"MS-DIAL exited with code {_process.ExitCode}.");
            }
            problems.AddRange(DescribeNewCrashReports());
            if (problems.Count > 0) {
                Fail("closing", problems);
            }
        }

        public void Dispose() {
            if (!_process.HasExited) {
                _process.Kill();
            }
            _automation.Dispose();
            _application.Dispose();
            _process.Dispose();
        }

        private void WaitUntilResponsive(string step) {
            try {
                FlaUI.Core.Input.Wait.UntilResponsive(MainWindow, ResponsiveTimeout);
            }
            catch (Exception) {
                // FindProblems reports the hang below.
            }
            AssertHealthy(step);
        }

        private List<string> FindProblems() {
            var problems = new List<string>();
            _process.Refresh();
            if (_process.HasExited) {
                problems.Add($"MS-DIAL exited with code {_process.ExitCode}.");
            }
            else {
                if (!_process.Responding) {
                    problems.Add("MS-DIAL is not responding.");
                }
                problems.AddRange(DescribeCrashDialogs());
            }
            problems.AddRange(DescribeNewCrashReports());
            return problems;
        }

        private IEnumerable<string> DescribeCrashDialogs() {
            return OpenDialogs()
                .Where(dialog => SafeTitle(dialog) == CrashDialogTitle)
                .Select(dialog => "The unexpected-exception dialog is open:" + Environment.NewLine + DescribeText(dialog));
        }

        /// <summary>
        /// Every window of MS-DIAL's other than the main window. A dialog shown while another
        /// window is active is owned by it and sits under it in the tree rather than at the top,
        /// so owned windows are followed down a few levels (a file dialog over the project wizard
        /// over the main window, for one).
        /// </summary>
        private List<Window> OpenDialogs() {
            var dialogs = new List<Window>();
            Window[] topLevel;
            try {
                topLevel = _application.GetAllTopLevelWindows(_automation);
            }
            catch (Exception) {
                return dialogs;
            }
            var pending = new Queue<(Window Window, int Depth)>(topLevel.Select(window => (window, 0)));
            while (pending.Count > 0) {
                var (window, depth) = pending.Dequeue();
                if (!window.Equals(MainWindow) && !dialogs.Contains(window)) {
                    dialogs.Add(window);
                }
                if (depth < 4) {
                    foreach (var owned in SafeModalWindows(window)) {
                        pending.Enqueue((owned, depth + 1));
                    }
                }
            }
            return dialogs;
        }

        private IEnumerable<string> DescribeNewCrashReports() {
            return _crashReports.NewReports().Select(path => $"A crash report was written: {path}");
        }

        private void Fail(string step, IReadOnlyList<string> problems) {
            AttachEvidence(step);
            var message = new StringBuilder()
                .AppendLine($"MS-DIAL is not healthy after '{step}':");
            foreach (var problem in problems) {
                message.AppendLine("- " + problem);
            }
            Assert.Fail(message.ToString());
        }

        private void AttachEvidence(string step) {
            try {
                var directory = _context.TestResultsDirectory ?? Path.GetTempPath();
                var screenshot = Path.Combine(directory, $"{_context.TestName}-{Sanitize(step)}-{DateTime.Now:HHmmss}.png");
                Capture.Screen().ToFile(screenshot);
                _context.AddResultFile(screenshot);
            }
            catch (Exception exception) {
                _context.WriteLine($"Could not take a screenshot: {exception.Message}");
            }
            try {
                var directory = _context.TestResultsDirectory ?? Path.GetTempPath();
                var tree = Path.Combine(directory, $"{_context.TestName}-{Sanitize(step)}-{DateTime.Now:HHmmss}-uia.txt");
                var windows = _process.HasExited ? Array.Empty<Window>() : _application.GetAllTopLevelWindows(_automation);
                File.WriteAllText(tree, string.Join(Environment.NewLine, windows.Select(window => UiTree.Dump(window))));
                _context.AddResultFile(tree);
            }
            catch (Exception exception) {
                _context.WriteLine($"Could not dump the UI Automation tree: {exception.Message}");
            }
            foreach (var report in _crashReports.NewReports()) {
                _context.AddResultFile(report);
            }
        }

        private static Window[] SafeModalWindows(Window window) {
            try {
                return window.ModalWindows;
            }
            catch (Exception) {
                return Array.Empty<Window>();
            }
        }

        private static string? SafeTitle(Window window) {
            try {
                return window.Title;
            }
            catch (Exception) {
                return null;
            }
        }

        private static string DescribeText(Window window) {
            try {
                var texts = window.FindAllDescendants(condition => condition.ByControlType(ControlType.Text))
                    .Select(text => text.Name)
                    .Where(name => !string.IsNullOrWhiteSpace(name));
                return string.Join(Environment.NewLine, texts);
            }
            catch (Exception) {
                return "(the dialog text could not be read)";
            }
        }

        private static string Sanitize(string value) {
            var invalid = Path.GetInvalidFileNameChars();
            return new string(value.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c).ToArray());
        }
    }
}
