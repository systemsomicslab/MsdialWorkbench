using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart;

/// <summary>
/// The EIC list of the "Display chromatograms" dialog.
/// It holds only the rows the user entered, plus one empty row at the end to type a new one into.
/// </summary>
internal sealed class EicQueryListModel
{
    private readonly ObservableCollection<EicQueryModel> _queries;

    public EicQueryListModel(IEnumerable<PeakFeatureSearchValue>? savedValues) {
        _queries = [];
        Queries = new ReadOnlyObservableCollection<EicQueryModel>(_queries);
        foreach (var value in savedValues ?? []) {
            if (value is { Mass: > 0d, MassTolerance: > 0d }) {
                Insert(_queries.Count, EicQueryModel.From(value));
            }
        }
        EnsureTrailingEmptyRow();
    }

    public ReadOnlyObservableCollection<EicQueryModel> Queries { get; }

    /// <summary>The rows that will be drawn.</summary>
    public List<PeakFeatureSearchValue> ToSearchValues() {
        return _queries.Where(q => q.IsValid).Select(q => q.ToSearchValue()).ToList();
    }

    public void Remove(EicQueryModel query) {
        if (_queries.Remove(query)) {
            query.PropertyChanged -= OnQueryPropertyChanged;
            EnsureTrailingEmptyRow();
        }
    }

    public void Clear() {
        foreach (var query in _queries) {
            query.PropertyChanged -= OnQueryPropertyChanged;
        }
        _queries.Clear();
        EnsureTrailingEmptyRow();
    }

    /// <summary>
    /// Pastes tab-separated lines (e.g. copied from Excel) starting at a row and column
    /// (0 = name, 1 = m/z, 2 = tolerance). Rows are added as needed; blank lines and a header line are skipped.
    /// </summary>
    /// <returns>The 1-based line numbers that could not be read.</returns>
    public List<int> Paste(int startIndex, int startColumn, string text) {
        var failed = new List<int>();
        var lines = (text ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Trim('\n').Split('\n');
        var index = System.Math.Max(0, System.Math.Min(startIndex, _queries.Count - 1));
        for (int i = 0; i < lines.Length; i++) {
            if (string.IsNullOrWhiteSpace(lines[i])) {
                continue;
            }
            var cells = lines[i].Split('\t');
            string? title = null;
            double? mass = null, tolerance = null;
            bool hasMass = false, hasTolerance = false, ok = true;
            for (int j = 0; j < cells.Length; j++) {
                var cell = cells[j].Trim();
                switch (startColumn + j) {
                    case 0:
                        title = cell;
                        break;
                    case 1:
                        hasMass = true;
                        ok &= TryParsePositive(cell, out mass);
                        break;
                    case 2:
                        hasTolerance = true;
                        ok &= TryParsePositive(cell, out tolerance);
                        break;
                }
            }
            if (!ok) {
                if (i != 0) { // A first line that does not parse is taken as a header.
                    failed.Add(i + 1);
                }
                continue;
            }
            var query = GetOrAddRow(index++);
            if (title is not null) query.Title = title;
            if (hasTolerance) query.MassTolerance = tolerance;
            if (hasMass) query.Mass = mass;
        }
        return failed;
    }

    private static bool TryParsePositive(string text, out double? value) {
        value = null;
        if (text.Length == 0) {
            return true;
        }
        if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v) && v > 0d) {
            value = v;
            return true;
        }
        return false;
    }

    private EicQueryModel GetOrAddRow(int index) {
        while (_queries.Count <= index) {
            Insert(_queries.Count, new EicQueryModel(massTolerance: _queries.LastOrDefault(q => q.MassTolerance > 0d)?.MassTolerance ?? EicQueryModel.DefaultMassTolerance));
        }
        return _queries[index];
    }

    /// <summary>The entered rows as tab-separated text with a header line.</summary>
    public string ToTableText() {
        var builder = new System.Text.StringBuilder();
        builder.Append("Name\tExact mass\tMass tolerance [Da]\r\n");
        foreach (var query in _queries.Where(q => !q.IsEmpty)) {
            builder.Append(query.Title).Append('\t')
                .Append(query.Mass?.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append('\t')
                .Append(query.MassTolerance?.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append("\r\n");
        }
        return builder.ToString();
    }

    private void Insert(int index, EicQueryModel query) {
        query.PropertyChanged += OnQueryPropertyChanged;
        _queries.Insert(index, query);
    }

    private void OnQueryPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(EicQueryModel.IsEmpty)) {
            EnsureTrailingEmptyRow();
        }
    }

    // Keep exactly one empty row at the end: add one when the last row gets filled,
    // drop the extra one when the row above it is emptied again.
    private void EnsureTrailingEmptyRow() {
        while (_queries.Count >= 2 && _queries[_queries.Count - 1].IsEmpty && _queries[_queries.Count - 2].IsEmpty) {
            var last = _queries[_queries.Count - 1];
            last.PropertyChanged -= OnQueryPropertyChanged;
            _queries.RemoveAt(_queries.Count - 1);
        }
        if (_queries.Count == 0 || !_queries[_queries.Count - 1].IsEmpty) {
            var tolerance = _queries.LastOrDefault(q => q.MassTolerance > 0d)?.MassTolerance ?? EicQueryModel.DefaultMassTolerance;
            Insert(_queries.Count, new EicQueryModel(massTolerance: tolerance));
        }
    }
}
