using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.ReleaseTests
{
    /// <summary>
    /// Prepares a folder for one case, runs the console over it and judges the result.
    ///
    /// Layout: <c>in</c> holds copies of the raw data (the console writes intermediate files next
    /// to them, so the demo data is never processed in place), <c>extra</c> the auxiliary files,
    /// <c>out</c> the results, and the method file and the console's output sit beside them.
    /// </summary>
    internal sealed class ConsoleRun
    {
        private static readonly TimeSpan Timeout = TimeSpan.FromMinutes(60);

        private readonly TestContext _context;
        private readonly string _exe;
        private readonly string _demoDataDir;
        private readonly ConsoleCase _case;

        public ConsoleRun(TestContext context, string exe, string demoDataDir, ConsoleCase consoleCase, string directory) {
            _context = context;
            _exe = exe;
            _demoDataDir = demoDataDir;
            _case = consoleCase;
            Directory = directory;
        }

        public string Directory { get; }
        private string Input => Path.Combine(Directory, "in");
        private string Extra => Path.Combine(Directory, "extra");
        private string Output => Path.Combine(Directory, "out");
        private string MethodFile => Path.Combine(Directory, "method.txt");
        private string StandardOutput => Path.Combine(Directory, "console.log");
        private string StandardError => Path.Combine(Directory, "console.err.log");

        public void Prepare() {
            System.IO.Directory.CreateDirectory(Input);
            System.IO.Directory.CreateDirectory(Extra);
            System.IO.Directory.CreateDirectory(Output);
            foreach (var relative in _case.RawData) {
                Copy(Path.Combine(_demoDataDir, relative), Path.Combine(Input, Path.GetFileName(relative)));
            }
            WriteMethodFile();
        }

        /// <summary>
        /// Starts from the console's own template so the defaults are tested as shipped, then
        /// applies the case's entries: a line "Key: value" is replaced, or appended when missing.
        /// </summary>
        private void WriteMethodFile() {
            var (exitCode, output) = Execute("template", _case.Mode, "--ionmode", _case.IonMode.ToString(), "-o", MethodFile, "-f");
            if (exitCode != 0 || !File.Exists(MethodFile)) {
                Assert.Fail($"'template {_case.Mode}' failed with exit code {exitCode}:{Environment.NewLine}{output}");
            }

            var entries = _case.Method.ToDictionary(entry => entry.Key, entry => entry.Value.Replace("{DemoDataDir}", _demoDataDir));
            if (_case.RiCompoundFile is { } compounds) {
                entries["RI dictionary file path"] = WriteRiDictionary(Path.Combine(_demoDataDir, compounds));
            }

            var lines = File.ReadAllLines(MethodFile).ToList();
            foreach (var entry in entries) {
                var index = lines.FindIndex(line => line.StartsWith(entry.Key + ":", StringComparison.Ordinal));
                var line = $"{entry.Key}: {entry.Value}";
                if (index >= 0) {
                    lines[index] = line;
                }
                else {
                    lines.Add(line);
                }
            }
            File.WriteAllLines(MethodFile, lines);
        }

        private string WriteRiDictionary(string compoundFile) {
            var compounds = Path.Combine(Extra, Path.GetFileName(compoundFile));
            File.Copy(compoundFile, compounds, overwrite: true);
            var dictionary = Path.Combine(Extra, "ri_dictionary.txt");
            File.WriteAllLines(dictionary, System.IO.Directory.EnumerateFileSystemEntries(Input).Select(file => $"{file}\t{compounds}"));
            return dictionary;
        }

        /// <summary>Runs the processing and fails the test unless it produced results.</summary>
        public void RunAndCheck() {
            var stopwatch = Stopwatch.StartNew();
            var (exitCode, _) = Execute(_case.Mode, "-i", Input, "-o", Output, "-m", MethodFile, "-p");
            _context.WriteLine($"{_case.Name}: exit code {exitCode} after {stopwatch.Elapsed.TotalSeconds:0} s.");

            var problems = new List<string>();
            if (exitCode != 0) {
                problems.Add($"The console exited with code {exitCode}.");
            }
            var errors = File.ReadAllLines(StandardError).Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
            if (errors.Any(line => line.Contains("Exception") || line.TrimStart().StartsWith("at "))) {
                problems.Add("The console wrote an exception to standard error:" + Environment.NewLine + string.Join(Environment.NewLine, errors.Take(15)));
            }
            if (problems.Count == 0) {
                problems.AddRange(CheckResults());
            }
            if (problems.Count > 0) {
                Attach();
                Assert.Fail($"'{_case.Name}' did not process cleanly (files kept in {Directory}):{Environment.NewLine}- " + string.Join(Environment.NewLine + "- ", problems));
            }
        }

        /// <summary>
        /// A run can exit 0 and still have found nothing, for one when the minimum peak height is
        /// above every peak in a file. Every input must have a result with at least one peak, and
        /// with more than one input the alignment must have at least one spot.
        /// </summary>
        private IEnumerable<string> CheckResults() {
            var inputs = _case.RawData.Select(Path.GetFileName).Where(IsMeasurement).Select(Path.GetFileNameWithoutExtension).ToList();
            foreach (var input in inputs) {
                var result = new[] { ".mdpeak", ".mdscan" }.Select(extension => Path.Combine(Output, input + extension)).FirstOrDefault(File.Exists);
                if (result is null) {
                    yield return $"No peak list (.mdpeak or .mdscan) was written for {input}.";
                }
                else if (DataRows(result) == 0) {
                    yield return $"The peak list for {input} is empty: {result}";
                }
            }
            if (inputs.Count > 1) {
                var alignments = System.IO.Directory.GetFiles(Output, "AlignResult-*.mdalign");
                if (alignments.Length == 0) {
                    yield return "No alignment result (AlignResult-*.mdalign) was written.";
                }
                else if (alignments.All(alignment => DataRows(alignment) == 0)) {
                    yield return $"The alignment result is empty: {alignments[0]}";
                }
            }
        }

        // A run's companions (.wiff.scan, .timeseries.data) are not measurements of their own.
        private static bool IsMeasurement(string file) {
            return !file.EndsWith(".scan", StringComparison.OrdinalIgnoreCase)
                && !file.EndsWith(".data", StringComparison.OrdinalIgnoreCase);
        }

        private static int DataRows(string tabSeparated) {
            return File.ReadLines(tabSeparated).Skip(1).Count(line => !string.IsNullOrWhiteSpace(line));
        }

        private void Attach() {
            foreach (var file in new[] { MethodFile, StandardOutput, StandardError }.Where(File.Exists)) {
                _context.AddResultFile(file);
            }
        }

        /// <summary>
        /// Runs the console from the case's folder rather than from the executable's: users start
        /// it from wherever their data is, and a reader that only works from the executable's
        /// folder is a bug a release check should see.
        /// </summary>
        private (int ExitCode, string Output) Execute(params string[] arguments) {
            var startInfo = new ProcessStartInfo(_exe, string.Join(" ", arguments.Select(Quote))) {
                WorkingDirectory = Directory,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            using var process = new Process { StartInfo = startInfo };
            using var standardOutput = new StreamWriter(StandardOutput, append: true);
            using var standardError = new StreamWriter(StandardError, append: true);
            var lastLines = new Queue<string>();
            process.OutputDataReceived += (s, e) => { if (e.Data is not null) { lock (standardOutput) { standardOutput.WriteLine(e.Data); } } };
            process.ErrorDataReceived += (s, e) => { if (e.Data is not null) { lock (standardError) { standardError.WriteLine(e.Data); lastLines.Enqueue(e.Data); } } };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            if (!process.WaitForExit((int)Timeout.TotalMilliseconds)) {
                process.Kill();
                process.WaitForExit();
                Attach();
                Assert.Fail($"'{string.Join(" ", arguments.Take(1))}' did not finish within {Timeout.TotalMinutes:0} minutes (files kept in {Directory}).");
            }
            process.WaitForExit();
            return (process.ExitCode, string.Join(Environment.NewLine, lastLines));
        }

        private static string Quote(string argument) {
            return argument.Contains(' ') || argument.Contains('\\') ? $"\"{argument.TrimEnd('\\')}\"" : argument;
        }

        private static void Copy(string source, string destination) {
            if (System.IO.Directory.Exists(source)) {
                System.IO.Directory.CreateDirectory(destination);
                foreach (var file in System.IO.Directory.EnumerateFiles(source)) {
                    File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
                }
                foreach (var directory in System.IO.Directory.EnumerateDirectories(source)) {
                    Copy(directory, Path.Combine(destination, Path.GetFileName(directory)));
                }
            }
            else {
                File.Copy(source, destination, overwrite: true);
            }
        }
    }
}
