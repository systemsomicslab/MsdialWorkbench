using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace CompMs.App.MsdialConsole.Process;

public static class ConsoleLineFilter
{
    private static readonly object SyncRoot = new object();

    public static IDisposable SuppressExact(params string[] lines) {
        return new SuppressionScope(lines);
    }

    private sealed class SuppressionScope : IDisposable
    {
        private readonly TextWriter _original;
        private bool _disposed;

        public SuppressionScope(IEnumerable<string> lines) {
            Monitor.Enter(SyncRoot);
            try {
                _original = Console.Out;
                Console.SetOut(new ExactLineSuppressingTextWriter(_original, lines));
            }
            catch {
                Monitor.Exit(SyncRoot);
                throw;
            }
        }

        public void Dispose() {
            if (_disposed) {
                return;
            }
            Console.SetOut(_original);
            _disposed = true;
            Monitor.Exit(SyncRoot);
        }
    }
}

internal sealed class ExactLineSuppressingTextWriter : TextWriter
{
    private readonly TextWriter _inner;
    private readonly HashSet<string> _suppressed;

    public ExactLineSuppressingTextWriter(TextWriter inner, IEnumerable<string> suppressed) {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _suppressed = new HashSet<string>(suppressed ?? throw new ArgumentNullException(nameof(suppressed)), StringComparer.Ordinal);
    }

    public override Encoding Encoding => _inner.Encoding;
    public override IFormatProvider FormatProvider => _inner.FormatProvider;

    public override void Flush() => _inner.Flush();
    public override void Write(char value) => _inner.Write(value);
    public override void Write(string? value) => _inner.Write(value);
    public override void WriteLine() => _inner.WriteLine();

    public override void WriteLine(string? value) {
        if (!_suppressed.Contains(value ?? string.Empty)) {
            _inner.WriteLine(value);
        }
    }
}
