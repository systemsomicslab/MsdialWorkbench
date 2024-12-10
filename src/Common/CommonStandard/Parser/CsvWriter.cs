using System;
using System.IO;
using System.Text;

namespace CompMs.Common.Parser;

/// <summary>
/// Provides functionality to write tab-separated or custom-delimited CSV files.
/// </summary>
public sealed class CsvWriter : IDisposable
{
    private const int ExcelCellCharacterLimit = 32767;
    private StreamWriter? _writer;

    /// <summary>
    /// Gets or sets the delimiter character used to separate values. The default is the tab character (',').
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Gets or sets a value indicating whether to truncate cell values exceeding Excel's character limit.
    /// </summary>
    public bool CutByExcelLimit { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvWriter"/> class using a stream.
    /// </summary>
    /// <param name="stream">The stream to write data to.</param>
    /// <param name="leaveOpen">Whether to leave the stream open after the writer is disposed.</param>
    public CsvWriter(Stream stream, bool leaveOpen) {
        _writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: leaveOpen);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvWriter"/> class using a file path.
    /// </summary>
    /// <param name="path">The file path where the CSV data will be written.</param>
    public CsvWriter(string path) {
        var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        _writer = new StreamWriter(stream, Encoding.ASCII, bufferSize: 4096, leaveOpen: false);
    }

    /// <summary>
    /// Writes a row of string values separated by the delimiter.
    /// </summary>
    /// <param name="values">The string values to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has been disposed.</exception>
    public void WriteRow(params string?[] values) {
        EnsureWriterIsNotDisposed();

        for (int i = 0; i < values.Length; i++) {
            if (i > 0) {
                _writer.Write(Delimiter);
            }
            WriteValue(values[i]);
        }
        _writer.WriteLine();
    }

    /// <summary>
    /// Writes a row of object values separated by the delimiter.
    /// </summary>
    /// <param name="values">The object values to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has been disposed.</exception>
    public void WriteRow(params object?[] values) {
        EnsureWriterIsNotDisposed();

        for (int i = 0; i < values.Length; i++) {
            if (i > 0) {
                _writer.Write(Delimiter);
            }
            WriteValue(values[i]?.ToString() ?? string.Empty);
        }
        _writer.WriteLine();
    }

    private void WriteValue(string? value) {
        if (CutByExcelLimit && value is { Length: > ExcelCellCharacterLimit }) {
#if NETSTANDARD2_1_OR_GREATER || NET6_0_OR_GREATER
            _writer.Write(value.AsSpan(0, ExcelCellCharacterLimit));
#else
            _writer.Write(value.Substring(0, ExcelCellCharacterLimit));
#endif
        } else {
            _writer.Write(value);
        }
    }

    private void EnsureWriterIsNotDisposed() {
        if (_writer is null) {
            throw new ObjectDisposedException(nameof(CsvWriter));
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="CsvWriter"/>.
    /// </summary>
    public void Dispose() {
        _writer?.Flush();
        _writer?.Dispose();
        _writer = null;
    }
}

