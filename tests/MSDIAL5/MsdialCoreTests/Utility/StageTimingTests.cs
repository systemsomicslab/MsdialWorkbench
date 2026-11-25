using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Utility.Tests;

[TestClass()]
public class StageTimingTests
{
    [TestMethod()]
    public void OffMeasureReturnsTheSharedScopeTest() {
        if (StageTiming.IsEnabled) {
            Assert.Inconclusive($"{StageTiming.EnvironmentVariable} is set in this test process.");
        }
        var first = StageTiming.Measure("file", "stage");
        var second = StageTiming.Measure(null, "other stage");
        Assert.AreSame(first, second);
        first.Dispose();
        first.Dispose();
    }

    [TestMethod()]
    public void RowTest() {
        var writer = new StringWriter();
        var log = new StageTimingLog(writer, TextWriter.Null, DateTime.UtcNow.AddSeconds(-1));
        var culture = CultureInfo.CurrentCulture;
        try {
            // Decimal comma: the row must not follow the culture.
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var scope = log.Measure("a,\"b\"", "peak spotting");
            Thread.Sleep(5);
            scope.Dispose();
            scope.Dispose();
        }
        finally {
            CultureInfo.CurrentCulture = culture;
        }

        var lines = Lines(writer);
        Assert.AreEqual(1, lines.Length, "One row, however often the scope is disposed.");
        Assert.IsTrue(lines[0].StartsWith("\"a,\"\"b\"\"\",peak spotting,"), lines[0]);
        var fields = lines[0].Substring(lines[0].IndexOf(",peak spotting,") + 1).Split(',');
        Assert.AreEqual(StageTimingLog.Header.Split(',').Length - 1, fields.Length);
        var startUtc = DateTime.Parse(fields[1], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        Assert.AreEqual(DateTimeKind.Utc, startUtc.Kind);
        Assert.IsTrue(double.Parse(fields[2], CultureInfo.InvariantCulture) >= 1000d, "start_ms counts from the process start.");
        Assert.IsTrue(double.Parse(fields[3], CultureInfo.InvariantCulture) >= 4d);
        Assert.AreEqual(Environment.CurrentManagedThreadId, int.Parse(fields[4], CultureInfo.InvariantCulture));
    }

    [TestMethod()]
    public void ConcurrentScopesTest() {
        var writer = new StringWriter();
        var log = new StageTimingLog(writer, TextWriter.Null, DateTime.UtcNow);
        Parallel.For(0, 200, i => {
            using (log.Measure($"file {i}", "stage")) {
            }
        });

        var lines = Lines(writer);
        Assert.AreEqual(200, lines.Length);
        Assert.AreEqual(200, lines.Select(line => line.Split(',')[0]).Distinct().Count());
        Assert.IsTrue(lines.All(line => line.Split(',').Length == StageTimingLog.Header.Split(',').Length));
    }

    [TestMethod()]
    public void FailedWriteDoesNotThrowTest() {
        var warnings = new StringWriter();
        var log = new StageTimingLog(new FailingWriter(), warnings, DateTime.UtcNow);
        log.Measure("file", "stage").Dispose();
        log.Measure("file", "stage").Dispose();

        Assert.AreEqual(1, Lines(warnings).Length, "One warning for any number of failures.");
    }

    [TestMethod()]
    public void OpenTest() {
        var folder = Path.Combine(Path.GetTempPath(), $"msdial-stagetiming-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        try {
            var path = Path.Combine(folder, "timing.csv");
            var first = StageTimingLog.Open(path, TextWriter.Null);
            Assert.IsNotNull(first);
            first.Measure("file", "stage").Dispose();
            var second = StageTimingLog.Open(path, TextWriter.Null);
            Assert.IsNotNull(second);
            second.Measure("file", "stage").Dispose();

            string text;
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            using (var reader = new StreamReader(stream)) {
                text = reader.ReadToEnd();
            }
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(3, lines.Length, "The header once, then a row from each log.");
            Assert.AreEqual(StageTimingLog.Header, lines[0]);

            Assert.IsNull(StageTimingLog.Open(null, TextWriter.Null));
            Assert.IsNull(StageTimingLog.Open(" ", TextWriter.Null));
            var warnings = new StringWriter();
            Assert.IsNull(StageTimingLog.Open(Path.Combine(folder, "missing", "timing.csv"), warnings));
            Assert.AreEqual(1, Lines(warnings).Length);
        }
        finally {
            try {
                // The logs keep their file open, shared for deletion.
                Directory.Delete(folder, recursive: true);
            }
            catch (IOException) {
            }
        }
    }

    private static string[] Lines(StringWriter writer) {
        return writer.ToString().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }

    private sealed class FailingWriter : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void WriteLine(string? value) {
            throw new IOException("disk full");
        }
    }
}
