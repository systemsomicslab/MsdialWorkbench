using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

namespace CompMs.MsdialCore.Utility;

/// <summary>
/// Opt-in timing of the processing stages, for speed work. It is off unless the environment variable
/// <see cref="EnvironmentVariable"/> names a CSV file, to which each measured stage then appends one row. Off,
/// <see cref="Measure"/> costs a null check and returns a shared object whose Dispose does nothing.
/// </summary>
/// <remarks>
/// <para>
/// Use it as <c>using (StageTiming.Measure(file.AnalysisFileName, "peak spotting")) { ... }</c>. The row is written
/// when the scope is disposed, so a stage that throws writes none. A scope may span an await: it keeps its own start
/// readings, and it does not matter which thread disposes it.
/// </para>
/// <para>
/// Columns: file (empty for a stage of the whole run), stage, start_utc (ISO 8601), start_ms (since the process
/// started), elapsed_ms, thread_id and end_thread_id (the managed threads that started and ended the stage), gen0,
/// gen1 and gen2 (garbage collections during the stage), cpu_ms (process CPU time during the stage) and pid. The
/// collection counts and the CPU time are the whole process's, so files processed at the same time share them.
/// </para>
/// <para>
/// Writing never throws into the processing: the first failure is reported once on the standard error and later ones
/// are ignored. Give each process its own file; two processes appending to one file can mix their rows.
/// </para>
/// </remarks>
public static class StageTiming
{
    /// <summary>The environment variable that names the CSV file and so turns the timing on.</summary>
    public const string EnvironmentVariable = "MSDIAL_STAGE_TIMING";

    private static readonly IDisposable _off = new NoTiming();
    private static readonly StageTimingLog? _log = StageTimingLog.Open(ReadPath(), Console.Error);

    /// <summary>Whether the timing is on in this process.</summary>
    public static bool IsEnabled => _log is not null;

    /// <summary>Starts timing one stage; disposing the result ends it and writes its row.</summary>
    /// <param name="file">The analysis file the stage works on, or null for a stage of the whole run.</param>
    /// <param name="stage">The name of the stage.</param>
    public static IDisposable Measure(string? file, string stage) {
        return _log is null ? _off : _log.Measure(file, stage);
    }

    private static string? ReadPath() {
        try {
            return Environment.GetEnvironmentVariable(EnvironmentVariable);
        }
        catch (Exception) {
            return null;
        }
    }

    private sealed class NoTiming : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>The CSV file behind <see cref="StageTiming"/>: one row per stage, written under one lock and flushed at once.</summary>
internal sealed class StageTimingLog
{
    internal const string Header = "file,stage,start_utc,start_ms,elapsed_ms,thread_id,end_thread_id,gen0,gen1,gen2,cpu_ms,pid";

    private readonly object _lock = new object();
    private readonly TextWriter _writer;
    private readonly TextWriter _warnings;
    private readonly long _anchorTimestamp;
    private readonly DateTime _anchorUtc;
    private readonly double _anchorMs;
    private readonly string _pid;
    private bool _warned;

    /// <param name="writer">Where the rows go; the header is the caller's.</param>
    /// <param name="warnings">Where the one warning about a failed write goes.</param>
    /// <param name="processStartUtc">The process start, from which start_ms counts.</param>
    internal StageTimingLog(TextWriter writer, TextWriter warnings, DateTime processStartUtc) {
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _warnings = warnings;
        // One reading of the wall clock, then the stopwatch from there: start times keep the stopwatch's resolution.
        _anchorTimestamp = Stopwatch.GetTimestamp();
        _anchorUtc = DateTime.UtcNow;
        _anchorMs = (_anchorUtc - processStartUtc).TotalMilliseconds;
        _pid = CurrentProcessId();
    }

    /// <summary>The log on the CSV file at <paramref name="path"/>, or null when no path is given or the file cannot be opened.</summary>
    internal static StageTimingLog? Open(string? path, TextWriter warnings) {
        if (string.IsNullOrWhiteSpace(path)) {
            return null;
        }
        FileStream? stream = null;
        try {
            stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
            var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            if (stream.Length == 0) {
                writer.WriteLine(Header);
                writer.Flush();
            }
            return new StageTimingLog(writer, warnings, ProcessStartUtc());
        }
        catch (Exception ex) {
            stream?.Dispose();
            Warn(warnings, $"Warning: stage timing is off: {path} could not be opened ({ex.Message}).");
            return null;
        }
    }

    internal IDisposable Measure(string? file, string stage) {
        return new Scope(this, file, stage);
    }

    internal string FormatRow(string? file, string stage, long startTimestamp, long endTimestamp, int threadId, int endThreadId, int gen0, int gen1, int gen2, TimeSpan? cpu) {
        var sinceAnchor = startTimestamp - _anchorTimestamp;
        var startUtc = _anchorUtc.AddTicks((long)(sinceAnchor * ((double)TimeSpan.TicksPerSecond / Stopwatch.Frequency)));
        var culture = CultureInfo.InvariantCulture;
        return string.Join(",",
            Quote(file ?? string.Empty),
            Quote(stage),
            startUtc.ToString("o", culture),
            (_anchorMs + ToMilliseconds(sinceAnchor)).ToString("F3", culture),
            ToMilliseconds(endTimestamp - startTimestamp).ToString("F3", culture),
            threadId.ToString(culture),
            endThreadId.ToString(culture),
            gen0.ToString(culture),
            gen1.ToString(culture),
            gen2.ToString(culture),
            cpu.HasValue ? cpu.Value.TotalMilliseconds.ToString("F1", culture) : string.Empty,
            _pid);
    }

    private void Write(string row) {
        lock (_lock) {
            try {
                _writer.WriteLine(row);
                _writer.Flush();
            }
            catch (Exception ex) {
                Failed(ex);
            }
        }
    }

    private void Failed(Exception ex) {
        lock (_lock) {
            if (_warned) {
                return;
            }
            _warned = true;
        }
        Warn(_warnings, $"Warning: stage timing could not write a row ({ex.Message}); further failures are not reported.");
    }

    private static void Warn(TextWriter warnings, string message) {
        try {
            warnings?.WriteLine(message);
        }
        catch (Exception) {
        }
    }

    private static double ToMilliseconds(long timestampTicks) {
        return timestampTicks * 1000d / Stopwatch.Frequency;
    }

    private static string Quote(string value) {
        if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0) {
            return value;
        }
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }

    private static TimeSpan? ProcessorTime() {
        try {
            using (var process = Process.GetCurrentProcess()) {
                return process.TotalProcessorTime;
            }
        }
        catch (Exception) {
            return null;
        }
    }

    private static DateTime ProcessStartUtc() {
        try {
            using (var process = Process.GetCurrentProcess()) {
                return process.StartTime.ToUniversalTime();
            }
        }
        catch (Exception) {
            return DateTime.UtcNow;
        }
    }

    private static string CurrentProcessId() {
        try {
            using (var process = Process.GetCurrentProcess()) {
                return process.Id.ToString(CultureInfo.InvariantCulture);
            }
        }
        catch (Exception) {
            return string.Empty;
        }
    }

    private sealed class Scope : IDisposable
    {
        private readonly StageTimingLog _log;
        private readonly string? _file;
        private readonly string _stage;
        private readonly int _threadId;
        private readonly int _gen0, _gen1, _gen2;
        private readonly TimeSpan? _cpu;
        private readonly long _start;
        private int _disposed;

        public Scope(StageTimingLog log, string? file, string stage) {
            _log = log;
            _file = file;
            _stage = stage;
            _threadId = Environment.CurrentManagedThreadId;
            _gen0 = GC.CollectionCount(0);
            _gen1 = GC.CollectionCount(1);
            _gen2 = GC.CollectionCount(2);
            _cpu = ProcessorTime();
            _start = Stopwatch.GetTimestamp();
        }

        public void Dispose() {
            if (Interlocked.Exchange(ref _disposed, 1) != 0) {
                return;
            }
            var end = Stopwatch.GetTimestamp();
            try {
                var cpu = ProcessorTime();
                var row = _log.FormatRow(
                    _file, _stage, _start, end, _threadId, Environment.CurrentManagedThreadId,
                    GC.CollectionCount(0) - _gen0, GC.CollectionCount(1) - _gen1, GC.CollectionCount(2) - _gen2,
                    cpu.HasValue && _cpu.HasValue ? cpu.Value - _cpu.Value : (TimeSpan?)null);
                _log.Write(row);
            }
            catch (Exception ex) {
                _log.Failed(ex);
            }
        }
    }
}
